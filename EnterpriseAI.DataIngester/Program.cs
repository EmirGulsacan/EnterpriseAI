using EnterpriseAI.DataIngester.Services;
using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using EnterpriseAI.Shared.Providers.Gemini;
using EnterpriseAI.Shared.Providers.LiteLlm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(configuration);

services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
services.Configure<GeminiOptions>(configuration.GetSection(GeminiOptions.SectionName));
services.Configure<LiteLlmOptions>(configuration.GetSection(LiteLlmOptions.SectionName));
services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
services.Configure<ChunkingOptions>(configuration.GetSection(ChunkingOptions.SectionName));

var activeProvider = configuration[AiOptions.SectionName + ":ActiveProvider"]
    ?? throw new InvalidOperationException("AiSettings:ActiveProvider is not configured.");

// SSL bypass handler for on-premise/corporate networks
var sslBypassHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
};

if (activeProvider == "LiteLLM")
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[STARTUP] Initializing with LiteLLM (On-Premise) Provider...");
    Console.ResetColor();

    services.AddHttpClient<IEmbeddingProvider, LiteLlmEmbeddingProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);

    services.AddHttpClient<IVisionProvider, LiteLlmVisionProvider>()
            .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);
}
else if (activeProvider == "Gemini")
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("[STARTUP] Initializing with Google Gemini (Cloud) Provider...");
    Console.ResetColor();

    services.AddHttpClient<IEmbeddingProvider, GeminiEmbeddingProvider>();
    services.AddHttpClient<IVisionProvider, GeminiVisionProvider>();
}
else
{
    Console.WriteLine($"[ERROR] Invalid ActiveProvider: {activeProvider}. Supported: Gemini, LiteLLM");
    return;
}

services.AddTransient<DataPipelineService>();

using var serviceProvider = services.BuildServiceProvider();
var pipelineService = serviceProvider.GetRequiredService<DataPipelineService>();

Console.Write("Enter the full path of the PDF file to process: ");
string? pdfPath = Console.ReadLine();

if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
{
    Console.WriteLine("[ERROR] File not found or invalid path.");
    return;
}

await pipelineService.ProcessPdfAndSaveToDatabaseAsync(pdfPath);