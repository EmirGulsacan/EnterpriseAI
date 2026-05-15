# Enterprise RAG Framework

A robust, enterprise-grade Retrieval-Augmented Generation (RAG) framework built with **.NET 8** and **Angular 19**. Designed for multi-tenant isolation, massive scale, and secure enterprise environments.

## 🌟 Key Features

* **Strict Clean Architecture:** Completely dismantled generic Shared structures in favor of pure **Domain**, **Application**, and **Infrastructure** boundaries. High cohesion, low coupling.
* **No-Repository Anti-Pattern:** Direct, strongly-typed generic abstraction mapping via `IApplicationDbContext` directly to `EF Core Set<T>()`, eliminating redundant repository layers.
* **Advanced Multi-Tenancy:** Secure data isolation using pre-filtering on pgvector metadata. Queries are strictly bounded to the requesting tenant (`ApplicationCode`).
* **Server-Sent Events (SSE) Streaming:** Real-time AI response streaming directly to the UI, coupled with an initial citations payload for instant references.
* **Hybrid Multimodal RAG:** Not just text. Analyzes and embeds PDFs with complex layouts, and physically extracts images (saved to disk, tracked via JSONB) for vision-based queries.
* **Modern Angular Citations UI:** Includes a dynamic Angular Web Component featuring a Perplexity-style collapsible citation list, typing animations, and a Lightbox for image reference previews.

## 🏗️ Architecture

```
├── EnterpriseAI.Domain/          # Core entities (DocumentChunk), Interfaces
├── EnterpriseAI.Application/     # Business logic, Services, Interfaces (IApplicationDbContext)
├── EnterpriseAI.Infrastructure/  # EF Core, PgVector, AI Providers (Gemini, Claude, etc.)
├── EnterpriseAI.Api/             # API entry points, Auth, SSE Streaming
├── EnterpriseAI.DataIngester/    # Console app for PDF processing and vectorization
└── EnterpriseAI.Web/             # Angular 19 Frontend Web Component
```

## 🚀 Getting Started

1. **Database Setup:** Ensure PostgreSQL with `pgvector` extension is running.
2. **Configuration:** Copy `appsettings.example.json` to `appsettings.json` and fill in your connection strings and AI API Keys.
3. **Ingestion:** Run the `EnterpriseAI.DataIngester` project to upload PDFs and generate vectors.
4. **API:** Run `EnterpriseAI.Api`.
5. **Frontend:** Serve the Angular component via `EnterpriseAI.Web` or use the pre-built `example.html` in `EnterpriseAI.Example`.

## 🛡️ Security
- `appsettings.json` and SSL certificates are strictly `.gitignore`'d.
- Physical visual assets extracted from PDFs are isolated per tenant in local storage.
