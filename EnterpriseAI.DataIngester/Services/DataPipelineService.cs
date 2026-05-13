using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Models;
using EnterpriseAI.Shared.Options;
using LiteDB;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

using EnterpriseAI.Shared.Utils;

namespace EnterpriseAI.DataIngester.Services
{
    public class DataPipelineService
    {
        private readonly IEmbeddingProvider _embeddingProvider;
        private readonly IVisionProvider _visionProvider;
        private readonly string _dbPath;
        private readonly ChunkingOptions _chunkingOptions;
        private readonly string _imageStoragePath;

        public DataPipelineService(
            IEmbeddingProvider embeddingProvider,
            IVisionProvider visionProvider,
            IOptions<DatabaseOptions> dbOptions,
            IOptions<ChunkingOptions> chunkingOptions)
        {
            _embeddingProvider = embeddingProvider;
            _visionProvider = visionProvider;
            _dbPath = PathHelper.GetAbsolutePathRelativeToSolution(dbOptions.Value.LiteDbPath);
            _imageStoragePath = PathHelper.GetAbsolutePathRelativeToSolution(dbOptions.Value.ImageStoragePath);
            _chunkingOptions = chunkingOptions.Value;
        }

        public async Task ProcessPdfAndSaveToDatabaseAsync(string pdfPath)
        {
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=================================================");
            Console.WriteLine("  ENTERPRISE RAG — KNOWLEDGE BASE BUILDER STARTED");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();

            Console.WriteLine($"[SYSTEM] Preparing database: {_dbPath}");
            Console.WriteLine($"[CONFIG] Chunk Size: {_chunkingOptions.ChunkSize} chars | Overlap: {_chunkingOptions.ChunkOverlap} chars | Min Length: {_chunkingOptions.MinChunkLength} chars");

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<DocumentChunk>("KnowledgeBase");

            Console.WriteLine("[SYSTEM] Clearing existing records...");
            collection.DeleteAll();

            int totalChunksCreated = 0;

            using (PdfDocument document = PdfDocument.Open(pdfPath))
            {
                int totalPages = document.NumberOfPages;
                Console.WriteLine($"[INFO] PDF opened successfully. Total Pages: {totalPages}\n");

                foreach (Page page in document.GetPages())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n---> PROCESSING PAGE {page.Number} / {totalPages} <---");
                    Console.ResetColor();


                    string pageText = ContentOrderTextExtractor.GetText(page);
                    var chunks = CreateOverlappingChunks(pageText, page.Number);

                    int textSaveCount = 0;

                    foreach (var chunk in chunks)
                    {
                        chunk.Embedding = await _embeddingProvider.GenerateEmbeddingAsync(chunk.Content);
                        collection.Insert(chunk);
                        textSaveCount++;

                        // Delay to avoid hitting API rate limits (e.g. Google 429)
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
                            await ProcessImageChunk(collection, page.Number, imgCount, imageBytes, "image/png");
                            Console.WriteLine("Success!");
                            imgCount++;
                        }
                        else if (image.TryGetBytesAsMemory(out var memoryBytes))
                        {
                            await ProcessImageChunk(collection, page.Number, imgCount, memoryBytes.ToArray(), "image/jpeg");
                            Console.WriteLine("Success!");
                            imgCount++;
                        }
                        else
                        {
                            Console.WriteLine("Unsupported format, skipped.");
                        }

                        // Delay for Vision API rate limiting
                        await Task.Delay(3000);
                    }
                    Console.WriteLine(new string('-', 50));
                }
            }

            collection.EnsureIndex(x => x.ApplicationCode);
            collection.EnsureIndex(x => x.PageNumber);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("\n=================================================");
            Console.WriteLine($"  COMPLETED — {totalChunksCreated} total chunks in database");
            Console.WriteLine("=================================================\n");
            Console.ResetColor();
        }

        /// <summary>
        /// Creates overlapping text chunks using a Sliding Window approach.
        /// Each chunk is up to ChunkSize characters, with ChunkOverlap characters
        /// carried over from the previous chunk to preserve context at boundaries.
        /// </summary>
        private List<DocumentChunk> CreateOverlappingChunks(string pageText, int pageNumber)
        {
            var chunks = new List<DocumentChunk>();

            if (string.IsNullOrWhiteSpace(pageText))
                return chunks;

            // Clean and normalize whitespace
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

                // Try to break at a sentence or word boundary (avoid mid-word cuts)
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
                    chunks.Add(new DocumentChunk
                    {
                        ApplicationCode = "DEMO",
                        ModuleCode = $"Page_{pageNumber}_Chunk_{chunkIndex}",
                        Content = finalChunk,
                        PageNumber = pageNumber,
                        IsImageDescription = false
                    });
                    chunkIndex++;
                }

                position += step;
            }

            return chunks;
        }

        /// <summary>
        /// Finds the last sentence-ending or word-boundary position in the text
        /// to avoid cutting chunks in the middle of a word/sentence.
        /// </summary>
        private static int FindLastBreakPoint(string text)
        {
            // Prefer sentence boundaries first
            for (int i = text.Length - 1; i >= text.Length / 2; i--)
            {
                char c = text[i];
                if (c == '.' || c == '!' || c == '?' || c == '\n')
                    return i;
            }

            // Fall back to word boundaries
            for (int i = text.Length - 1; i >= text.Length / 2; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                    return i;
            }

            return text.Length - 1; // No good break point found, use full length
        }

        private async Task ProcessImageChunk(ILiteCollection<DocumentChunk> collection, int pageNum, int imgCount, byte[] bytes, string mime)
        {
            string description = await _visionProvider.DescribeImageAsync(bytes, mime);

            // Save image to disk
            Directory.CreateDirectory(_imageStoragePath);
            string extension = mime == "image/png" ? ".png" : ".jpg";
            string fileName = $"{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(_imageStoragePath, fileName);
            await File.WriteAllBytesAsync(filePath, bytes);

            // Format markdown reference with static file path
            string imageUrl = $"/images/{fileName}";
            string finalContent = $"![Ekran Görüntüsü]({imageUrl})\n\n[SCREENSHOT DETAIL] {description}";

            var imgChunk = new DocumentChunk
            {
                ApplicationCode = "DEMO",
                ModuleCode = $"Page_{pageNum}_Image_{imgCount}",
                Content = finalContent,
                PageNumber = pageNum,
                IsImageDescription = true,
                Embedding = await _embeddingProvider.GenerateEmbeddingAsync(finalContent)
            };
            collection.Insert(imgChunk);
        }
    }
}