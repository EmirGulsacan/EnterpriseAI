using EnterpriseAI.Domain.Entities;
using EnterpriseAI.Domain.Options;
using EnterpriseAI.Application.Interfaces;
using EnterpriseAI.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Security.Cryptography;
using System.Text.Json;

using EnterpriseAI.Domain.Utils;

namespace EnterpriseAI.Application.Services
{
    public class DataPipelineService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IVisionProvider _visionProvider;
        private readonly ChunkingOptions _chunkingOptions;
        private readonly string _imageStoragePath;

        public DataPipelineService(
            IApplicationDbContext dbContext,
            IEmbeddingProvider embeddingProvider,
            IVisionProvider visionProvider,
            IOptions<DatabaseOptions> dbOptions,
            IOptions<ChunkingOptions> chunkingOptions)
        {
            _dbContext = dbContext;
            _embeddingProvider = embeddingProvider;
            _visionProvider = visionProvider;
            _imageStoragePath = PathHelper.GetAbsolutePathRelativeToSolution(dbOptions.Value.ImageStoragePath);
            _chunkingOptions = chunkingOptions.Value;
        }

        public async Task ProcessPdfAndSaveToDatabaseAsync(string pdfPath, string applicationCode, string category)
        {

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  ENTERPRISE RAG — KNOWLEDGE BASE BUILDER STARTED");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();

            Console.WriteLine($"[SYSTEM] Preparing PostgreSQL database insertion...");
            Console.WriteLine($"[CONFIG] Chunk Size: {_chunkingOptions.ChunkSize} chars | Overlap: {_chunkingOptions.ChunkOverlap} chars | Min Length: {_chunkingOptions.MinChunkLength} chars");

            var fileBytes = await File.ReadAllBytesAsync(pdfPath);
            string fileHash = ComputeSha256Hash(fileBytes);
            string fileName = Path.GetFileName(pdfPath);

            var existingHash = await _dbContext.Set<DocumentChunk>().FirstOrDefaultAsync(x => x.FileHash == fileHash && x.ApplicationCode == applicationCode);
            if (existingHash != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[ERROR] UPLOAD REJECTED!");
                Console.WriteLine($"A document with the exact same content already exists for this tenant ({applicationCode}).");
                Console.WriteLine($"Even if you renamed the file, the contents are identical (Hash: {fileHash}).");
                Console.ResetColor();
                return;
            }

            var existingName = await _dbContext.Set<DocumentChunk>().FirstOrDefaultAsync(x => x.SourceDocument == fileName && x.ApplicationCode == applicationCode);
            if (existingName != null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n[WARNING] UPLOAD REJECTED!");
                Console.WriteLine($"A document named '{fileName}' is already in the database for tenant '{applicationCode}'.");
                Console.WriteLine($"If this is a new version of the document, please rename it (e.g., '{Path.GetFileNameWithoutExtension(fileName)}_v2{Path.GetExtension(fileName)}') and try again.");
                Console.ResetColor();
                return;
            }

            int totalChunksCreated = 0;

            using (PdfDocument document = PdfDocument.Open(fileBytes))
            {
                int totalPages = document.NumberOfPages;
                Console.WriteLine($"[INFO] PDF opened successfully. Total Pages: {totalPages}\n");

                foreach (Page page in document.GetPages())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n---> PROCESSING PAGE {page.Number} / {totalPages} <---");
                    Console.ResetColor();


                    string pageText = ContentOrderTextExtractor.GetText(page);
                    var chunks = CreateOverlappingChunks(pageText, page.Number, fileName, fileHash, applicationCode, category);

                    int textSaveCount = 0;

                    foreach (var chunk in chunks)
                    {
                        var embedding = await _embeddingProvider.GenerateEmbeddingAsync(chunk.Content);
                        if (embedding == null || embedding.Length == 0)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"\n[FATAL ERROR] Failed to generate vector embedding for text chunk.");
                            Console.WriteLine("This is usually caused by an invalid API Key, network issue, or API quota limit.");
                            Console.WriteLine("Ingestion aborted. Please check your appsettings.json API Key and try again.");
                            Console.ResetColor();
                            return;
                        }

                        chunk.Embedding = new Pgvector.Vector(embedding);
                        _dbContext.Set<DocumentChunk>().Add(chunk);
                        await _dbContext.SaveChangesAsync();
                        textSaveCount++;

                        await Task.Delay(2000);
                    }

                    totalChunksCreated += textSaveCount;
                    Console.WriteLine($"[TEXT] {textSaveCount} chunks (sliding window) vectorized and saved.");


                    var images = page.GetImages();
                    int imgCount = 1;

                    if (images.Any())
                    {
                        Console.WriteLine($"[IMAGE] {images.Count()} screenshots found. Starting Vision AI analysis...");
                    }

                    foreach (var image in images)
                    {
                        Console.Write($"         Image {imgCount} analyzing... ");
                        if (image.TryGetPng(out byte[] imageBytes))
                        {
                            await ProcessImageChunk(page.Number, imgCount, imageBytes, "image/png", fileName, fileHash, applicationCode, category);
                            Console.WriteLine("Success!");
                            imgCount++;
                        }
                        else if (image.TryGetBytes(out var memoryBytes))
                        {
                            await ProcessImageChunk(page.Number, imgCount, memoryBytes.ToArray(), "image/jpeg", fileName, fileHash, applicationCode, category);
                            Console.WriteLine("Success!");
                            imgCount++;
                        }
                        else
                        {

                            if (image.ImageDictionary.TryGet(UglyToad.PdfPig.Tokens.NameToken.Filter, out UglyToad.PdfPig.Tokens.IToken filterToken))
                            {
                                string filterName = filterToken.ToString();
                                string ext = "image/jpeg"; // Default fallback
                                if (filterName.Contains("JPXDecode")) ext = "image/jp2";
                                else if (filterName.Contains("DCTDecode")) ext = "image/jpeg";
                                else if (filterName.Contains("FlateDecode")) ext = "image/png";

                                var rawBytes = image.RawBytes.ToArray();
                                if (rawBytes.Length > 0)
                                {
                                    await ProcessImageChunk(page.Number, imgCount, rawBytes, ext, fileName, fileHash, applicationCode, category);
                                    Console.WriteLine($"Success (Fallback: {filterName})!");
                                    imgCount++;
                                    continue;
                                }
                            }

                            Console.WriteLine("Unsupported format, skipped.");
                        }

                        await Task.Delay(3000);
                    }
                    Console.WriteLine(new string('-', 50));
                }
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=================================================");
            Console.WriteLine($"  COMPLETED — {totalChunksCreated} total chunks in database");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();
        }





        private List<DocumentChunk> CreateOverlappingChunks(string pageText, int pageNumber, string sourceDocument, string fileHash, string applicationCode, string category)
        {
            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(pageText))
                return chunks;

            string cleanText = pageText.Replace("\r\n", "\n").Trim();

            if (cleanText.Length < _chunkingOptions.MinChunkLength)
                return chunks;

            int chunkSize = _chunkingOptions.ChunkSize;
            int overlap = _chunkingOptions.ChunkOverlap;
            int step = chunkSize - overlap; // How far to advance the window each iteration

            if (step <= 0) step = chunkSize / 2; // Safety: ensure forward progress

            int position = 0;
            int chunkIndex = 0;

            while (position < cleanText.Length)
            {
                int length = Math.Min(chunkSize, cleanText.Length - position);
                string rawChunk = cleanText.Substring(position, length);

                if (position + length < cleanText.Length)
                {
                    int lastBreak = FindLastBreakPoint(rawChunk);
                    if (lastBreak > chunkSize / 2) // Only adjust if we found a reasonable break point
                    {
                        rawChunk = rawChunk.Substring(0, lastBreak + 1);
                    }
                }

                string finalChunk = rawChunk.Trim();

                if (finalChunk.Length >= _chunkingOptions.MinChunkLength)
                {
                    var metadata = JsonSerializer.SerializeToDocument(new 
                    { 
                        Category = category, 
                        PageNumber = pageNumber,
                        ImagePath = (string?)null
                    });

                    chunks.Add(new DocumentChunk
                    {
                        ApplicationCode = applicationCode,
                        ModuleCode = $"Page_{pageNumber}_Chunk_{chunkIndex}",
                        Content = finalChunk,
                        PageNumber = pageNumber,
                        IsImageDescription = false,
                        SourceDocument = sourceDocument,
                        FileHash = fileHash,
                        Metadata = metadata
                    });
                    chunkIndex++;
                }

                position += step;
            }

            return chunks;
        }




        private static int FindLastBreakPoint(string text)
        {

            for (int i = text.Length - 1; i >= text.Length / 2; i--)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?' || c == '\n')
                    return i;
            }

            for (int i = text.Length - 1; i >= text.Length / 2; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                    return i;
            }

            return text.Length - 1; // No good break point found, use full length
        }

        private string ComputeSha256Hash(byte[] rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(rawData);
                return Convert.ToHexString(bytes).ToLowerInvariant();
            }
        }

        private async Task ProcessImageChunk(int pageNum, int imgCount, byte[] bytes, string mime, string sourceDocument, string fileHash, string applicationCode, string category)
        {
            string description = await _visionProvider.DescribeImageAsync(bytes, mime);

            string tenantFolderPath = Path.Combine(_imageStoragePath, applicationCode);
            Directory.CreateDirectory(tenantFolderPath);
            
            string extension = mime == "image/png" ? ".png" : ".jpg";
            string fileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(tenantFolderPath, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

            string imageUrl = $"/images/{applicationCode}/{fileName}";
            string finalContent = $"![Ekran Görüntüsü]({imageUrl})\n\n[SCREENSHOT DETAIL] {description}";

            var embedding = await _embeddingProvider.GenerateEmbeddingAsync(finalContent);
            if (embedding == null || embedding.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL ERROR] Failed to generate vector embedding for image description.");
                Console.WriteLine("Ingestion aborted. Please check your appsettings.json API Key and try again.");
                Console.ResetColor();
                throw new InvalidOperationException("Failed to generate embedding for image description. Invalid API Key.");
            }

            var metadata = JsonSerializer.SerializeToDocument(new 
            { 
                Category = category, 
                PageNumber = pageNum,
                ImagePath = imageUrl
            });

            var imgChunk = new DocumentChunk
            {
                ApplicationCode = applicationCode,
                ModuleCode = $"Page_{pageNum}_Image_{imgCount}",
                Content = finalContent,
                PageNumber = pageNum,
                IsImageDescription = true,
                SourceDocument = sourceDocument,
                FileHash = fileHash,
                Embedding = new Pgvector.Vector(embedding),
                Metadata = metadata
            };
            _dbContext.Set<DocumentChunk>().Add(imgChunk);
            await _dbContext.SaveChangesAsync();
        }
    }
}





