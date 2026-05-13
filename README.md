# Enterprise AI Gateway (Multimodal RAG Architecture)

Welcome to the **Enterprise AI Gateway** – a powerful, production-ready Multimodal Retrieval-Augmented Generation (RAG) architecture. This system bridges the gap between raw corporate data (PDFs, images, screenshots) and intelligent, conversational AI, offering a standalone chatbot widget powered by a robust .NET 8 backend.

## 🚀 Key Features & Architecture
- **Sliding Window Chunking:** Intelligently splits large documents into context-aware chunks with overlaps, ensuring no semantic meaning is lost at chunk boundaries.
- **In-Memory Cosine Similarity:** Fast, lightweight vector search to find the most relevant context blocks for the AI without heavy infrastructure dependencies.
- **Multimodal Capabilities:** Utilizes Vision AI to extract, analyze, and store visual context (screenshots/images) from documents, serving them locally.
- **Pluggable AI Providers:** Easily switch between Google Gemini, OpenAI, Claude, or local On-Premise models (via LiteLLM) using simple configuration changes.
- **Standalone Chatbot Widget:** A sleek, responsive, and easily embeddable Angular 19 UI component with Markdown and image rendering support.
- **Local Image Serving:** Images extracted from PDFs are stored locally and served statically, preserving data privacy and ensuring lightning-fast load times in the UI.

## 🛠️ Technology Stack
- **Backend:** .NET 8 (Web API & Console Applications)
- **Architecture Patterns:** Clean Architecture principles, CQRS *(Prepared)*
- **Database / Storage:** LiteDB (NoSQL Embedded Database for Vectors and Metadata)
- **Document Processing:** PdfPig (Advanced PDF parsing and image extraction)
- **Frontend:** Angular 19 (Standalone Components, Signals, SCSS)
- **AI Integration:** Native Google Gemini integration, LiteLLM (Proxy for OpenAI, Claude, Llama 3)

---

## ⚙️ Installation & Setup

### 1. Configuration (`appsettings.json`)
Before running the system, configure your AI Provider keys. Update `appsettings.json` in both `EnterpriseAI.Api` and `EnterpriseAI.DataIngester` projects:

```json
"AiSettings": {
  "ActiveProvider": "Gemini", // Options: Gemini, LiteLLM, OpenAI, Claude
  "Gemini": {
    "ApiKey": "YOUR_API_KEY_HERE",
    "EmbeddingModel": "gemini-embedding-001",
    "ChatModel": "gemini-flash-latest"
  }
}
```

### 2. Running the Data Ingester (Knowledge Base Builder)
To process your enterprise PDFs and build the initial vector database:
1. Navigate to `EnterpriseAI.DataIngester`.
2. Run the console application: `dotnet run`
3. When prompted, provide the absolute path to your PDF file. The system will extract text, analyze images via Vision AI, generate embeddings, and save everything into the unified `Data/` folder.

### 3. Running the API
To start the backend server that will serve the RAG pipeline and static images:
1. Navigate to `EnterpriseAI.Api`.
2. Run the API: `dotnet run`
3. Ensure the API is running (default port is usually `http://localhost:5102` or `https://localhost:7114`).

### 4. Running the Chatbot Widget (Angular UI)
1. Navigate to the `EnterpriseAI.Web` directory.
2. Install dependencies: `npm install`
3. Start the development server: `npm start`
4. Access the UI at `http://localhost:4200` and start chatting with your corporate data!

---

## 🗺️ ROADMAP (Phase 2)
The current `v1.0 (Phase 1)` master version establishes a flawless foundational architecture. The next phase will introduce massive enterprise-scale enhancements:

- **1. Dedicated Vector Database (Qdrant / Milvus):** Transitioning from In-Memory LiteDB search to an industrial-grade Vector DB to support millions of document chunks with sub-millisecond retrieval times.
- **2. SSE Streaming Responses:** Upgrading the Chatbot UX to stream AI responses word-by-word (Server-Sent Events) for a ChatGPT-like real-time experience.
- **3. Hybrid Search & Re-ranking:** Combining BM25 keyword search with Vector Semantic Search, backed by a Cross-Encoder (Re-ranker) to achieve 99.9% accuracy and eliminate AI hallucinations.
- **4. Document-Level RBAC (Role-Based Access Control):** Implementing strict security layers so employees can only query documents (e.g., HR, Finance) they are explicitly authorized to view based on their JWT claims.
- **5. Asynchronous Message Queues (RabbitMQ & Hangfire):** Upgrading the document ingestion pipeline from a console app to a robust, background worker system, allowing parallel processing of massive PDF uploads without blocking the UI.
