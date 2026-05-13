using EnterpriseAI.Api.Services;
using EnterpriseAI.Shared.Abstractions;
using EnterpriseAI.Shared.Options;
using EnterpriseAI.Shared.Providers;
using EnterpriseAI.Shared.Providers.Gemini;
using EnterpriseAI.Shared.Providers.LiteLlm;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.Extensions.FileProviders;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
    {
        new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
        Array.Empty<string>()
    }});
});

builder.Services.Configure<AiOptions>(builder.Configuration.GetSection(AiOptions.SectionName));
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.Configure<LiteLlmOptions>(builder.Configuration.GetSection(LiteLlmOptions.SectionName));
builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection(OpenAiOptions.SectionName));
builder.Services.Configure<ClaudeOptions>(builder.Configuration.GetSection(ClaudeOptions.SectionName));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection(DatabaseOptions.SectionName));
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<SearchOptions>(builder.Configuration.GetSection(SearchOptions.SectionName));

string activeProvider = builder.Configuration[AiOptions.SectionName + ":ActiveProvider"]
    ?? throw new InvalidOperationException("AiSettings:ActiveProvider is not configured.");

var sslBypassHandler = new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
};

if (activeProvider == "LiteLLM")
{
    builder.Services.AddHttpClient<IAiProvider, LiteLlmAiProvider>()
           .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);

    builder.Services.AddHttpClient<IEmbeddingProvider, LiteLlmEmbeddingProvider>()
           .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);

    builder.Services.AddHttpClient<IVisionProvider, LiteLlmVisionProvider>()
           .ConfigurePrimaryHttpMessageHandler(() => sslBypassHandler);
}
else if (activeProvider == "Gemini")
{
    builder.Services.AddHttpClient<IAiProvider, GeminiAiProvider>();
    builder.Services.AddHttpClient<IEmbeddingProvider, GeminiEmbeddingProvider>();
    builder.Services.AddHttpClient<IVisionProvider, GeminiVisionProvider>();
}
else if (activeProvider == "OpenAI")
{
    builder.Services.AddHttpClient<IAiProvider, OpenAiAiProvider>();
}
else if (activeProvider == "Claude")
{
    builder.Services.AddHttpClient<IAiProvider, ClaudeAiProvider>();
}
else
{
    throw new InvalidOperationException($"Invalid AI Provider: {activeProvider}. Supported: Gemini, LiteLLM, OpenAI, Claude");
}

builder.Services.AddScoped<KnowledgeBaseService>();

var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");

var key = Encoding.ASCII.GetBytes(jwtSecret);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}



var dbOptions = builder.Configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>();
var imageStoragePath = EnterpriseAI.Shared.Utils.PathHelper.GetAbsolutePathRelativeToSolution(dbOptions?.ImageStoragePath ?? "Data/Images");
Directory.CreateDirectory(imageStoragePath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(imageStoragePath),
    RequestPath = "/images"
});

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
