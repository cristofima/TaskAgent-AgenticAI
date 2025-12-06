# Documentation

Complete documentation for the Task Agent project.

## 📁 Structure

```
docs/
├── architecture/                    # Architecture decisions and diagrams
│   └── FOLDER_STRUCTURE.md         # Project organization guide
├── screenshots/                     # Application screenshots
│   └── README.md                   # Screenshots reference
├── CONTENT_SAFETY.md               # Security testing guide
├── DUAL_DATABASE_ARCHITECTURE.md   # Dual-database rationale
├── FRONTEND_E2E_TESTING.md         # Frontend E2E testing scenarios
├── LESSONS_LEARNED.md              # Project-wide lessons and best practices
└── POSTGRESQL_MIGRATION.md         # PostgreSQL setup guide
```

## 📚 Documentation Index

### 🏗️ Architecture & Design

- **[DUAL_DATABASE_ARCHITECTURE.md](DUAL_DATABASE_ARCHITECTURE.md)** - Architectural decision for using SQL Server + PostgreSQL

  - Why two databases?
  - Schema comparisons
  - Implementation patterns
  - Performance considerations

- **[architecture/FOLDER_STRUCTURE.md](architecture/FOLDER_STRUCTURE.md)** - Monorepo organization
  - Backend structure (Clean Architecture)
  - Frontend structure (Next.js)
  - Aspire orchestration
  - IDE contexts separation

- **[LESSONS_LEARNED.md](LESSONS_LEARNED.md)** - Project-wide lessons and best practices
  - Clean Architecture challenges and solutions
  - Content Safety migration from custom to built-in
  - Dual database architecture patterns
  - Preview package management
  - SSE error handling patterns

### 🛡️ Security & Testing

- **[CONTENT_SAFETY.md](CONTENT_SAFETY.md)** - Azure OpenAI Content Safety guide

  - Azure OpenAI built-in content filtering
  - Test cases (prompt injection, harmful content)
  - Blocked message handling - ChatGPT-like UX
  - Troubleshooting guide
  - Cross-reference: [FRONTEND_E2E_TESTING.md](FRONTEND_E2E_TESTING.md)

- **[CONTENT_SAFETY_MIGRATION.md](CONTENT_SAFETY_MIGRATION.md)** - Lessons learned from Content Safety migration
  - Migration from Azure.AI.ContentSafety SDK to Azure OpenAI built-in filtering
  - Trade-offs analysis
  - Best practices identified
  - Files impacted

- **[FRONTEND_E2E_TESTING.md](FRONTEND_E2E_TESTING.md)** - Frontend testing scenarios
  - Manual test cases for UI components
  - Suggestions UI testing
  - Loading states validation
  - **Content Safety blocked message flow testing**
  - Sidebar update behavior verification
  - Expected behaviors and visual checks
  - Cross-reference: [CONTENT_SAFETY.md](CONTENT_SAFETY.md)

### 🗄️ Database & Infrastructure

- **[POSTGRESQL_MIGRATION.md](POSTGRESQL_MIGRATION.md)** - PostgreSQL setup guide
  - Installation instructions (Windows/macOS/Linux)
  - Database creation
  - Connection string configuration
  - Migration commands
  - Troubleshooting

### 📸 Visual Reference

- **[screenshots/README.md](screenshots/README.md)** - Application screenshots
  - Chat interface
  - .NET Aspire Dashboard
  - Azure Application Insights views

## 🔗 Related Documentation

### Main READMEs

- **[Root README](../README.md)** - Project overview, quick start, features
- **[Backend README](../src/backend/services/TaskAgent/README.md)** - Clean Architecture, API endpoints, observability
- **[Frontend README](../src/frontend/task-agent-web/README.md)** - Component structure, hooks, API integration

### Specialized Guides

- **[.github/copilot-instructions.md](../.github/copilot-instructions.md)** - GitHub Copilot project-specific instructions

## 📝 Documentation Guidelines

When adding new documentation:

1. **Choose the right location**:

   - Root `README.md` → Overview, quick start, high-level architecture
   - `docs/` → Detailed guides, decisions, testing scenarios
   - Component READMEs → Specific subsystem documentation

2. **Use clear naming**:

   - `UPPERCASE_WITH_UNDERSCORES.md` for guides
   - `lowercase-with-dashes.md` for technical specs
   - `README.md` for directory indexes

3. **Include**:

   - Clear purpose statement at the top
   - Table of contents for long documents
   - Code examples with syntax highlighting
   - Cross-references to related docs

4. **Avoid**:
   - Duplicating information across files
   - Implementation details better suited for code comments
   - Historical documentation (clean up after migration)
