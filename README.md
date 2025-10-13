# Task Agent - AI-Powered Task Management

An intelligent task management assistant built with Microsoft Agentic AI Framework and Azure OpenAI, demonstrating Clean Architecture and autonomous AI agent capabilities.

---

## 🚀 Quick Start

```bash
# Navigate to the project
cd TaskAgentWeb

# Restore dependencies
dotnet restore

# Configure your Azure OpenAI credentials in appsettings.Development.json
# Run the application
dotnet run
```

Visit `http://localhost:5000` and start managing your tasks with natural language!

---

## ✨ Features

- 💬 **Natural Language Interface**: Talk to your task manager like a person
- ✅ **Complete CRUD**: Create, read, update, and delete tasks
- 📊 **Smart Filtering**: Filter by status and priority
- 📈 **Task Analytics**: Get summaries and statistics
- 🎨 **Beautiful Tables**: Markdown-formatted responses with emojis
- 💡 **Contextual Suggestions**: Agent provides helpful next actions
- 🗄️ **SQL Server Persistence**: Entity Framework Core with LocalDB

---

## �️ Architecture

Built with **Clean Architecture** for maintainability and testability:

```
TaskAgent.Domain (Entities, Business Logic)
    ↓
TaskAgent.Application (Use Cases, Interfaces)
    ↓
TaskAgent.Infrastructure (Data Access, EF Core)
    ↓
TaskAgent.WebApp (UI, Controllers, AI Agent)
```

**Key Components**:

- **Domain**: `TaskItem` entity with business rules, Status/Priority enums
- **Application**: DTOs, `ITaskRepository`, 6 AI function tools
- **Infrastructure**: `TaskDbContext`, `TaskRepository` implementation
- **Presentation**: MVC controllers, Razor views, `TaskAgentService`

---

## 🛠️ Tech Stack

| Technology                     | Purpose              |
| ------------------------------ | -------------------- |
| .NET 9.0                       | Modern web framework |
| ASP.NET Core MVC               | Web application      |
| Entity Framework Core 9.0      | Database ORM         |
| SQL Server LocalDB             | Data persistence     |
| Microsoft Agentic AI Framework | Autonomous agents    |
| Azure OpenAI (GPT-4o-mini)     | Language model       |
| Bootstrap 5                    | Responsive UI        |
| Marked.js                      | Markdown rendering   |

---

## ⚙️ Setup

### Prerequisites

- .NET 9.0 SDK
- SQL Server LocalDB (included with Visual Studio)
- Azure OpenAI resource with deployed model

### Configuration

1. **Update `appsettings.Development.json`**:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key-here",
    "ModelDeployment": "gpt-4o-mini"
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
├── TaskAgent.Application/     # DTOs, Interfaces, Functions
├── TaskAgent.Infrastructure/  # DbContext, Repositories
└── TaskAgent.WebApp/          # Controllers, Views, Services
    ├── Controllers/           # ChatController, HomeController
    ├── Services/             # TaskAgentService
    ├── Views/                # Razor UI
    └── wwwroot/              # CSS, JavaScript
```

---

## 🔒 Security Notes

- ⚠️ Never commit API keys to source control
- Use environment variables or Azure Key Vault in production
- Input validation via EF Core parameterized queries
- XSS protection with DOMPurify sanitization

---

## � License

Educational sample project for learning Microsoft Agentic AI Framework.

---

**Built with ❤️ using .NET 9, Microsoft Agentic AI Framework, and Clean Architecture**
