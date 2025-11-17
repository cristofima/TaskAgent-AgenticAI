# Documentation

Complete documentation for the Task Agent project.

## 📁 Structure

```
docs/
├── architecture/                    # Architecture decisions and diagrams
│   └── FOLDER_STRUCTURE.md         # Project organization guide
├── screenshots/                     # Application screenshots
│   └── README.md                   # Screenshots reference
├── CONTENT_SAFETY.md               # Security testing guide (75+ cases)
├── DUAL_DATABASE_ARCHITECTURE.md   # Dual-database rationale
├── FRONTEND_E2E_TESTING.md         # Frontend E2E testing scenarios
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

### 🛡️ Security & Testing

- **[CONTENT_SAFETY.md](CONTENT_SAFETY.md)** - Azure AI Content Safety guide

  - 2-layer defense architecture (Prompt Shield + Content Moderation)
  - 75+ test cases (prompt injection, harmful content)
  - **NEW**: Blocked message handling (v2.1) - ChatGPT-like UX
  - Smart title regeneration and efficient sidebar updates
  - Known limitations and false positives
  - Troubleshooting guide
  - Cross-reference: [FRONTEND_E2E_TESTING.md](FRONTEND_E2E_TESTING.md)

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
- **Backend Streaming Roadmap**: `src/backend/services/TaskAgent/STREAMING_ROADMAP.md`
- **Frontend Streaming Roadmap**: `src/frontend/task-agent-web/STREAMING_ROADMAP.md`

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
