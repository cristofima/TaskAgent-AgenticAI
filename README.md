# Task Agent - AI-Powered Task Management

An intelligent task management assistant built with Microsoft Agentic AI Framework and Azure OpenAI, demonstrating Clean Architecture and autonomous AI agent capabilities with Azure Content Safety protection.

---

## 🚀 Quick Start

```bash
# Navigate to the project
cd TaskAgentWeb

# Restore dependencies
dotnet restore

# Configure your Azure OpenAI and Content Safety credentials in appsettings.Development.json
# Run the application
dotnet run
```

Visit `http://localhost:5000` and start managing your tasks with natural language!

---

## ✨ Features

- 💬 **Natural Language Interface**: Talk to your task manager like a person
- 🛡️ **Multi-Layer Security**: Azure Content Safety protection (Prompt Shield + Content Moderation)
- ✅ **Complete CRUD**: Create, read, update, and delete tasks
- 📊 **Smart Filtering**: Filter by status and priority
- 📈 **Task Analytics**: Get summaries and statistics
- 🎨 **Beautiful Tables**: Markdown-formatted responses with emojis
- 💡 **Contextual Suggestions**: Agent provides helpful next actions
- 🗄️ **SQL Server Persistence**: Entity Framework Core with LocalDB

---

## 🛡️ Content Safety

This application implements **2-layer defense** using Azure AI Content Safety with **parallel execution**:

### Layer 1: Prompt Shield

- Detects prompt injection attacks (jailbreaks, instruction override, role manipulation)
- REST API: `/contentsafety/text:shieldPrompt` (API version 2024-09-01)
- Blocks malicious attempts to manipulate the AI system
- **Optimized**: Trusts Azure's pre-trained model without system context (reduces false positives)

### Layer 2: Content Moderation

- Analyzes text for harmful content (Hate, Violence, Sexual, Self-Harm)
- SDK: `Azure.AI.ContentSafety` v1.0.0
- Configurable severity thresholds (0-6 scale)

**Architecture**: Content safety checks run automatically via middleware before any AI processing.

**Performance**:

- **Parallel Execution**: Both layers validate simultaneously using `Task.WhenAll` (~50% faster)
- **IHttpClientFactory**: Named HttpClient for optimal connection pooling and DNS refresh
- **Response Time**: ~200-400ms for safe prompts (vs ~400-800ms sequential)

**Testing**: See [CONTENT_SAFETY.md](CONTENT_SAFETY.md) for 75+ test cases, known limitations, and troubleshooting guide.

---

## 🏗️ Architecture

Built with **Clean Architecture** for maintainability and testability:

```
TaskAgent.Domain (Entities, Business Logic)
    ↓
TaskAgent.Application (Use Cases, Interfaces)
    ↓
TaskAgent.Infrastructure (Data Access, Azure Services)
    ↓
TaskAgent.WebApp (UI, Controllers, AI Agent)
```

**Key Components**:

- **Domain**: `TaskItem` entity with business rules, Status/Priority enums
- **Application**: DTOs (using record types), `ITaskRepository`, 6 AI function tools
- **Infrastructure**: `TaskDbContext`, `TaskRepository`, `ContentSafetyService` with HttpClientFactory
- **Presentation**: MVC controllers, Razor views, `TaskAgentService`, configuration validation extensions

---

## 🛠️ Tech Stack

| Technology                     | Purpose               |
| ------------------------------ | --------------------- |
| .NET 9.0                       | Modern web framework  |
| ASP.NET Core MVC               | Web application       |
| Entity Framework Core 9.0      | Database ORM          |
| SQL Server LocalDB             | Data persistence      |
| Microsoft Agentic AI Framework | Autonomous agents     |
| Azure OpenAI (GPT-4o-mini)     | Language model        |
| Azure AI Content Safety        | Security & moderation |
| Bootstrap 5                    | Responsive UI         |
| Marked.js                      | Markdown rendering    |

---

## ⚙️ Setup

### Prerequisites

- .NET 9.0 SDK
- SQL Server LocalDB (included with Visual Studio)
- Azure OpenAI resource with deployed model
- Azure AI Content Safety resource

### Configuration

1. **Update `appsettings.Development.json`**:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-openai-resource.openai.azure.com/",
    "ApiKey": "your-openai-api-key",
    "ModelDeployment": "gpt-4o-mini"
  },
  "ContentSafety": {
    "Endpoint": "https://your-contentsafety-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-contentsafety-api-key",
    "HateThreshold": 2,
    "ViolenceThreshold": 2,
    "SexualThreshold": 2,
    "SelfHarmThreshold": 2
  }
}
```

2. **Database** (auto-created on first run, or manually):

```bash
dotnet ef database update
```

3. **Run**:

```bash
dotnet run
```

---

## 🤖 AI Agent Capabilities

The Task Agent provides 6 function tools:

| Function         | Description                                        |
| ---------------- | -------------------------------------------------- |
| `CreateTask`     | Create new tasks with title, description, priority |
| `ListTasks`      | Show all tasks with optional filters               |
| `GetTaskDetails` | Get detailed info about a specific task            |
| `UpdateTask`     | Modify task status or priority                     |
| `DeleteTask`     | Remove tasks                                       |
| `GetTaskSummary` | View statistics and analytics                      |

**Example Interactions**:

```
You: Create a high priority task to review the quarterly report
Agent: ✅ Task created! ID: 1, Priority: High

You: Show me all my tasks
Agent: [Displays beautiful Markdown table with all tasks]
      💡 Suggestions: • Filter by priority • Update oldest task

You: Mark task 1 as in progress
Agent: ✅ Task updated! Status changed to InProgress
```

---

## 🎨 Agent Features

### Markdown Tables

Lists 2+ tasks in beautiful formatted tables with emojis:

- Status: ⏳ Pending, 🔄 InProgress, ✅ Completed
- Priority: 🟢 Low, 🟡 Medium, 🔴 High

### Contextual Suggestions

Agent provides 1-2 smart suggestions after each operation:

- After creating: "View all tasks" or "Create follow-up"
- After listing: "Filter by priority" or "Update oldest task"
- After completing: "View remaining tasks" or "Get summary"

### Smart Insights

- Detects many pending tasks → suggests prioritizing
- Celebrates milestones → "🎉 Great! You've completed 5 tasks!"
- Encourages progress

---

## 📂 Project Structure

```
TaskAgentWeb/
├── TaskAgent.Domain/          # Entities, Enums
├── TaskAgent.Application/     # DTOs (record types), Interfaces, Functions
├── TaskAgent.Infrastructure/  # DbContext, Repositories, Azure Services
│   ├── Services/             # ContentSafetyService (HttpClientFactory)
│   ├── Models/               # ContentSafetyConfig, PromptShieldResponse
│   └── DependencyInjection.cs # Named HttpClient registration
└── TaskAgent.WebApp/          # Controllers, Views, Services
    ├── Controllers/           # ChatController, HomeController
    ├── Services/             # TaskAgentService, ErrorResponseFactory
    ├── Middleware/           # ContentSafetyMiddleware
    ├── Extensions/           # ConfigurationValidationExtensions, etc.
    ├── Views/                # Razor UI
    └── wwwroot/              # CSS, JavaScript
```

---

## 🔒 Security

### Content Safety

- **2-Layer Defense**: Automatic Prompt Shield + Content Moderation
- **Fail-Secure**: Blocks requests on Prompt Shield errors; Fail-Open on Content Moderation for availability
- **Optimized Detection**: Prompt Shield uses pre-trained model (no system context) to reduce false positives
- **Performance**: HttpClientFactory with Named HttpClient for connection pooling and DNS refresh
- **Immutable DTOs**: Record types for thread-safety and proper equality semantics
- **See**: [CONTENT_SAFETY.md](CONTENT_SAFETY.md) for testing guide and limitations

### Application Security

- Input validation via EF Core parameterized queries
- XSS protection with DOMPurify sanitization
- Configuration validation on startup (via extension methods)
- Never commit API keys to source control
- Use environment variables or Azure Key Vault in production

---

## � License

Educational sample project for learning Microsoft Agentic AI Framework.

---

**Built with ❤️ using .NET 9, Microsoft Agentic AI Framework, and Clean Architecture**
