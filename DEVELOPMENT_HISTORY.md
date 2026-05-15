# Development History

## 2026-05-16
### Enterprise Architecture & UX Modernization

* **Multi-Tenancy Pre-filtering:** Implemented strict tenant isolation. Queries are now securely bounded by `ApplicationCode` directly at the EF Core level before vector similarity scans occur, drastically increasing both security and search performance across large datasets.
* **JSONB Metadata & Disk Isolation:** Refactored PDF image extraction. Images are now securely saved to physical disk (organized by Tenant ID) while their paths and descriptions are stored within a high-performance JSONB column. This prevents database bloat while maintaining strict access control.
* **SSE Streaming & Angular Fetch API:** Upgraded the AI response payload from blocking HTTP requests to real-time `IAsyncEnumerable` Server-Sent Events (SSE). The Angular Web Component now utilizes native `fetch()` and `ReadableStream` to produce a typewriter effect, dramatically improving UX.
* **Generic DbSet Strategy:** Destroyed the anti-pattern Repository interfaces (e.g., `IKnowledgeBaseRepository`). Transitioned the framework to strongly-typed `IApplicationDbContext` which directly maps to EF Core's `Set<T>()`. This eliminates boilerplate code and standardizes enterprise data access.
* **Clean Architecture Migration:** Eradicated the monolithic `EnterpriseAI.Shared` project. The solution is now strictly partitioned into `Domain`, `Application`, and `Infrastructure`, enforcing domain-driven design and unidirectional dependency mapping.
