# ClrSlate MCP (Model Context Protocol) Server

A comprehensive MCP server implementation with semantic search capabilities, KeyCloak integration, and vector embeddings support.

## 🚀 Project Overview

This project implements a Model Context Protocol (MCP) server that provides:

- **Semantic Search**: Vector-based search using Ollama embeddings and Qdrant vector database
- **KeyCloak Integration**: Authentication and authorization capabilities
- **Catalog Management**: Package and activity management with AI-powered search
- **Aspire Hosting**: Modern .NET hosting with observability
- **MCP Inspector**: Web-based debugging interface for MCP tools

## 📁 Project Structure

```
ClrSlate.Mcp/
├── McpClientPlayground/           # MCP client testing playground
├── src/
│   ├── aspire/
│   │   ├── ClrSlate.Mcp.ServiceDefaults/     # Shared Aspire services
│   │   └── hosting/
│   │       └── ClrSlate.Aspire.Hosting.McpInspector/  # MCP Inspector hosting
│   ├── ClrSlate.Mcp.AppHost/                 # Main Aspire app host
│   └── mcps/
│       └── ClrSlate.Mcp.KeyCloakServer/      # Main MCP server
│           ├── Models/                        # Data models
│           ├── Services/                      # Core services
│           ├── Tools/                         # MCP tools
│           ├── Prompts/                       # MCP prompts
│           └── Resources/                     # MCP resources
└── package-embeddings/                       # POC reference implementation
```

## 🛠️ Prerequisites

Before setting up the project, ensure you have:

1. **.NET 9.0 SDK** or later
2. **Docker** (for Qdrant vector database)
3. **Ollama** (for embeddings generation)
4. **Visual Studio 2022** or **VS Code** (recommended)

## 🔧 Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd clrswarm/experiments/ClrSlate.Mcp
```

### 2. Install Ollama and Models

```bash
# Install Ollama (visit https://ollama.ai for platform-specific instructions)
# Then pull the required embedding model:
ollama pull nomic-embed-text
```

### 3. Start Qdrant Vector Database

```bash
# Start Qdrant using Docker
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

### 4. Configure Application Settings

Update the hardcoded constants and configurations:

#### `src/mcps/ClrSlate.Mcp.KeyCloakServer/appsettings.json`

```json
{
  "Search": {
    "CatalogApiBaseUrl": "https://store.beta.clrslate.app",
    "OllamaBaseUrl": "http://localhost:11434",
    "OllamaModel": "nomic-embed-text",
    "QdrantHost": "127.0.0.1",
    "QdrantPort": 6334,
    "QdrantCollection": "catalog-embeddings",
    "VectorDimensions": 768,
    "BatchSize": 10
  },
  "keycloak": {
    "admin": {
      "username": "admin",
      "password": "admin"
    }
  }
}
```

### 5. Build and Run

```bash
# Build the solution
dotnet build

# Run the Aspire app host (recommended)
cd src/ClrSlate.Mcp.AppHost
dotnet run

# OR run the MCP server directly
cd src/mcps/ClrSlate.Mcp.KeyCloakServer
dotnet run
```

### 6. Initialize Vector Database

Once the server is running, use the MCP tools to populate the vector database:

1. **Check database status**: Use `getDatabaseStatus` tool
2. **Get ingestion progress**: Use `getIngestionProgress` tool
3. **Populate incrementally**: Use `populateIncrementally` tool (processes 5 records safely)
4. **Repeat step 3** until all data is indexed

## 🔍 Available MCP Tools

### Search Tools

- **`semanticSearch`**: AI-powered semantic search with configurable parameters
- **`searchPackages`**: Package-specific semantic search
- **`searchActivities`**: Activity-specific semantic search
- **`keywordSearch`**: Fast keyword-based search without embeddings

### Data Management Tools

- **`populateVectorDatabase`**: Full database population (with timeout protection)
- **`populateIncrementally`**: Safe incremental population (5 records at a time)
- **`quickPopulateDatabase`**: Quick testing population (10 records)
- **`populateMediumBatch`**: Medium batch population (15 records)
- **`getIngestionProgress`**: Shows indexing progress and recommendations
- **`getDatabaseStatus`**: Vector database health and statistics
- **`clearVectorDatabase`**: Reset database (use with caution)

### Legacy Tools

- **`Echo`**: Simple echo tool for testing
- **`getTinyImage`**: Returns a small test image

## 📊 Architecture Overview

### Core Services

1. **SearchService**: Orchestrates semantic and keyword searches
2. **EmbeddingService**: Generates vector embeddings using Ollama
3. **VectorStorageService**: Manages Qdrant vector database operations
4. **CatalogApiService**: Fetches data from catalog API

### Data Flow

```
Catalog API → CatalogApiService → EmbeddingService (Ollama) → VectorStorageService (Qdrant) → SearchService → MCP Tools
```

### Vector Search Process

1. **Ingestion**: Fetch packages/activities from catalog API
2. **Embedding**: Generate vectors using Ollama's nomic-embed-text model
3. **Storage**: Store vectors in Qdrant with metadata
4. **Search**: Query vectors for semantic similarity
5. **Results**: Return ranked results with similarity scores

## 🔄 Changes Made in This Branch

### New Projects Added

1. **McpClientPlayground**: Testing environment for MCP client interactions
2. **ClrSlate.Mcp.ServiceDefaults**: Shared Aspire services (telemetry, health checks)
3. **ClrSlate.Aspire.Hosting.McpInspector**: Web-based MCP debugging interface
4. **ClrSlate.Mcp.AppHost**: Main Aspire application host
5. **ClrSlate.Mcp.KeyCloakServer**: Core MCP server with semantic search

### Core Features Implemented

#### Semantic Search System

- **Vector Embeddings**: Integration with Ollama for text-to-vector conversion
- **Vector Database**: Qdrant integration for similarity search
- **Hybrid Search**: Combination of semantic and keyword search
- **Batch Processing**: Efficient processing of large datasets

#### MCP Server Tools

- **Search Tools**: Multiple search interfaces with different capabilities
- **Data Ingestion**: Automated catalog data processing and indexing
- **Database Management**: Health monitoring and statistics
- **Timeout Protection**: Robust handling of long-running operations

#### Infrastructure

- **Aspire Integration**: Modern .NET hosting with observability
- **KeyCloak Support**: Authentication and authorization framework
- **Docker Support**: Containerized vector database
- **Health Monitoring**: Comprehensive health checks and metrics

## 📍 Hardcoded Constants and Configuration

### Critical Configuration Files

#### 1. `appsettings.json` (ClrSlate.Mcp.KeyCloakServer)

```json
{
  "Search": {
    "CatalogApiBaseUrl": "https://store.beta.clrslate.app", // Catalog API endpoint
    "OllamaBaseUrl": "http://localhost:11434", // Ollama server URL
    "OllamaModel": "nomic-embed-text", // Embedding model name
    "QdrantHost": "127.0.0.1", // Qdrant host
    "QdrantPort": 6334, // Qdrant gRPC port
    "QdrantCollection": "catalog-embeddings", // Collection name
    "VectorDimensions": 768, // Vector dimensions
    "BatchSize": 10 // Processing batch size
  },
  "keycloak": {
    "admin": {
      "username": "admin", // KeyCloak admin user
      "password": "admin" // KeyCloak admin password
    }
  }
}
```

#### 2. Launch Settings (`Properties/launchSettings.json`)

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "launchBrowser": true,
      "applicationUrl": "https://localhost:58749;http://localhost:58750", // Server URLs
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

#### 3. Program.cs Configuration

```csharp
// HTTP Client Base URLs
client.BaseAddress = new Uri("https://store.beta.clrslate.app");           // Catalog API
client.BaseAddress = new Uri("http://localhost:11434");                    // Ollama API

// Server Information
Name = "ClrSlateKeyCloakServer",
Title = "ClrSlate MCP KeyCloak Server with Semantic Search",
Version = "1.1.0"
```

#### 4. Model Constants

```csharp
// Vector Dimensions (Models/SearchConfiguration.cs)
public int VectorDimensions { get; set; } = 768;                          // nomic-embed-text dimensions

// Timeout Settings (Tools/DataIngestionTool.cs)
var timeoutAfter = TimeSpan.FromMinutes(2);                               // Operation timeout
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));    // Batch timeout
```

### Important URLs and Endpoints

- **Catalog API**: `https://store.beta.clrslate.app`
- **Catalog Packages**: `https://store.beta.clrslate.app/api/catalog-packages`
- **Catalog Actions**: `https://store.beta.clrslate.app/api/catalog-actions`
- **Ollama Server**: `http://localhost:11434`
- **Ollama Embeddings**: `http://localhost:11434/api/embeddings`
- **Qdrant Database**: `127.0.0.1:6334` (gRPC), `127.0.0.1:6333` (HTTP)

## 🧪 Testing and Validation

### 1. Test MCP Tools

Use the MCP Inspector web interface (available when running via Aspire) to test tools:

- Navigate to the MCP Inspector URL (shown in Aspire dashboard)
- Test search tools with various queries
- Monitor database status and ingestion progress

### 2. Validate Search Results

```bash
# Example search queries to test:
- "web development framework"
- "logging utilities"
- "data processing"
- "machine learning"
- "database orm"
```

### 3. Monitor Health

- Use `getDatabaseStatus` to check Qdrant connection
- Use `getIngestionProgress` to monitor data indexing
- Check Aspire dashboard for service health

## 🔧 Troubleshooting

### Common Issues

1. **Ollama Model Not Available**

   ```bash
   ollama pull nomic-embed-text
   ollama serve
   ```

2. **Qdrant Connection Failed**

   ```bash
   docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
   ```

3. **Vector Database Timeout**

   - Use `populateIncrementally` instead of full population
   - Check Qdrant is running and accessible
   - Verify network connectivity

4. **Search Returns No Results**
   - Use `getIngestionProgress` to check if data is indexed
   - Run `populateIncrementally` to add data
   - Verify catalog API is accessible

### Performance Optimization

- **Batch Size**: Adjust `BatchSize` in configuration for optimal performance
- **Vector Dimensions**: Ensure matches the Ollama model dimensions
- **Timeout Settings**: Increase timeouts for large datasets
- **Memory**: Allocate sufficient memory for Qdrant and Ollama

## 📝 License

This project is licensed under the Apache License, Version 2.0. See the [LICENSE](http://www.apache.org/licenses/LICENSE-2.0) for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## 📞 Support

For issues and questions:

- Create an issue in the repository
- Check the troubleshooting section above
- Review the MCP documentation: https://spec.modelcontextprotocol.io/
