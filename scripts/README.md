# 📝 Release Notes Generator

Semi-automated system for generating professional release notes using **GitHub Copilot Prompt Files** in VS Code.

## 🎯 Overview

This tool analyzes your Git commits since a specific tag or release, classifies them automatically, suggests the next semantic version, and helps you generate professional release notes using GitHub Copilot.

**Key Features:**

- ✅ Analyzes commits from a specific tag/release to HEAD
- ✅ Classifies changes by type (feat, fix, docs, etc.)
- ✅ **Auto-suggests semantic version** (X.Y.Z)
- ✅ Uses GitHub Copilot Prompt File for consistent formatting
- ✅ Copies formatted data to clipboard automatically

---

## 🚀 How to Use

### Prerequisites

1. **Enable Prompt Files in VS Code** (one-time setup):

   - Open Settings (`Ctrl+,`)
   - Search: **"Chat: Prompt Files"**
   - Enable checkbox ✅
   - Restart VS Code

2. **Verify it works**:
   - Open Copilot Chat (`Ctrl+Shift+I`)
   - Type `/` - you should see `/generate-release-notes` in the list

### Step 1: Analyze Commits

Run the PowerShell script from the project root:

```powershell
# Analyze commits since the last tag/release
.\scripts\Analyze-Commits.ps1
```

**What happens:**

- Detects the latest Git tag (e.g., `v1.0.0`)
- Analyzes all commits from that tag to HEAD
- Classifies commits by type
- Suggests next version based on semantic versioning rules
- **Automatically copies formatted data to clipboard**

**Example Output:**

```
🚀 Release Notes Commit Analyzer
=================================

📌 Detected latest tag: v1.0.0 (version: 1.0.0)
✅ Found 15 commits since v1.0.0

📊 Quick Summary:
  Total commits: 15
  Contributors: 2
  - feat: 5 commits
  - fix: 3 commits
  - docs: 4 commits
  - refactor: 3 commits

📦 Version Suggestion:
  Current: 1.0.0
  Suggested: 1.1.0 (MINOR bump)

  Reason: New features detected (5 feat commits)

✅ Commit data copied to clipboard!
📋 Ready to paste in Copilot Chat
```

### Step 2: Generate Release Notes with Copilot

1. Open **GitHub Copilot Chat** in VS Code (`Ctrl+Shift+I`)
2. Type: **`/generate-release-notes`**
3. When prompted, **paste the commit data** (already in your clipboard from Step 1)
4. Copilot generates professional release notes following the template
5. Review and save as `RELEASE_NOTES_v1.1.0.md`

### Step 3: Create Release

```powershell
# Create and push the tag
git tag v1.1.0
git push origin v1.1.0

# Then create a GitHub release with the generated notes
```

---

## 📋 Practical Examples

### Example 1: First Release (No Previous Tags)

```powershell
PS> .\scripts\Analyze-Commits.ps1

🚀 Release Notes Commit Analyzer
=================================

📌 No tags found. This will be the first release (version: 1.0.0)
✅ Found 7 commits

📊 Quick Summary:
  Total commits: 7
  - feat: 2
  - docs: 3
  - refactor: 2

📦 Version Suggestion:
  Suggested: 1.0.0 (FIRST release)
```

**What to do:**

1. Copy data to clipboard (done automatically)
2. Use `/generate-release-notes` in Copilot Chat
3. Save as `RELEASE_NOTES_v1.0.0.md`
4. Create first tag: `git tag v1.0.0`

### Example 2: Regular Release (From v1.0.0 to v1.1.0)

```powershell
PS> .\scripts\Analyze-Commits.ps1

📌 Detected latest tag: v1.0.0
✅ Found 12 commits since v1.0.0

📊 Quick Summary:
  Total commits: 12
  - feat: 4 commits
  - fix: 3 commits
  - refactor: 3 commits
  - docs: 2 commits

📦 Version Suggestion:
  Current: 1.0.0
  Suggested: 1.1.0 (MINOR bump)
```

### Example 3: Analyzing Between Specific Tags

If you want to generate release notes for a previous release:

```powershell
# Analyze commits between v1.0.0 and v1.1.0
.\scripts\Analyze-Commits.ps1 -FromTag "v1.0.0" -ToRef "v1.1.0"
```

### Example 4: Manually Specify Current Version

```powershell
# Override auto-detection and specify current version
.\scripts\Analyze-Commits.ps1 -CurrentVersion "1.2.3"
```

---

## 🎨 Script Options

```powershell
# Basic usage - auto-detects latest tag
.\scripts\Analyze-Commits.ps1

# Between specific tags
.\scripts\Analyze-Commits.ps1 -FromTag "v1.0.0" -ToRef "v1.1.0"

# Override current version
.\scripts\Analyze-Commits.ps1 -CurrentVersion "1.2.3"

# Analyze from specific tag to HEAD
.\scripts\Analyze-Commits.ps1 -FromTag "v1.5.0" -ToRef "HEAD"
```

---

## 📊 Semantic Versioning Rules (Automatic)

The script automatically suggests the next version based on commit types (configured in `config.json`):

### MAJOR (X.0.0) - Breaking Changes

- Commits with `BREAKING CHANGE` in body
- Commits with `!` before `:` (e.g., `feat!: major change`)

### MINOR (x.Y.0) - New Features

- Commits type `feat:`
- Commits type `feature:`

### PATCH (x.y.Z) - Fixes & Improvements

- `fix:` - Bug fixes
- `refactor:` - Code refactoring
- `perf:` - Performance improvements
- `docs:` - Documentation
- `chore:` - Maintenance tasks

---

## 📝 Release Notes Template Structure

The generated release notes follow this structure (defined in `release-notes-template.md`):

1. **Header** - Version, date, release type
2. **🎯 Overview** - 2-3 sentence summary
3. **✨ What's New** - Features, fixes, improvements, performance, documentation
4. **⚠️ Breaking Changes** - If applicable
5. **📊 Statistics** - Commit count, contributors
6. **👥 Contributors** - List of contributors
7. **🔗 Links** - Repository and documentation links
8. **📦 Installation** - Installation instructions

---

## 💡 Best Practices for Commit Messages

To get better release notes, use **Conventional Commits** format:

### Good Commit Examples

```bash
# Features
feat: add Google OAuth authentication
feat: implement notification system

# Bug fixes
fix: resolve memory leak in cache
fix: correct validation in user registration

# Documentation
docs: update installation guide
docs: add API endpoint examples

# Performance
perf: optimize database queries
perf: reduce response time by caching

# Refactoring
refactor: simplify validation logic
refactor: extract common utilities
```

### Commits with Detailed Descriptions

The script extracts both title and body for better context:

```bash
git commit -m "feat: implement distributed cache" -m "
Complete Redis caching implementation:
- Azure Redis Cache configuration
- Automatic caching middleware
- Invalidation strategies
- Performance monitoring

Improves response time by 60%"
```

### Breaking Changes

Mark breaking changes clearly for MAJOR version bumps:

```bash
# Option 1: With ! before :
feat!: change authentication API structure

# Option 2: In commit body
feat: update permission system

BREAKING CHANGE: Roles now require explicit confirmation
```

---

## 🐛 Troubleshooting

### "No commits found"

- Verify you're in the project root directory
- Ensure commits exist in current branch
- Check if the tag/range specified is correct

### "Could not copy to clipboard"

- Data is still displayed in terminal - copy manually
- Use the formatted output shown in console

### Wrong version suggested

- Review commit message format
- Ensure using Conventional Commits format correctly
- Specify manually: `.\scripts\Analyze-Commits.ps1 -CurrentVersion "X.Y.Z"`

### Prompt file not appearing in Copilot

- Check that `.github/prompts/generate-release-notes.prompt.md` exists
- Enable in Settings: `Chat: Prompt Files` = ✅
- Restart VS Code

---

## 📁 Files Structure

```
.github/
└── prompts/
    └── generate-release-notes.prompt.md  ← Copilot Prompt File

scripts/
├── Analyze-Commits.ps1                   ← Commit analyzer
├── config.json                            ← Versioning rules
├── release-notes-template.md             ← Template structure
└── README.md                              ← This guide
```

---

## 🎯 Why This Approach?

✅ **No external APIs** - Uses your GitHub Copilot subscription  
✅ **Modern VS Code feature** - Built-in Prompt Files support  
✅ **Smart versioning** - Auto-suggests based on Semantic Versioning  
✅ **Consistent structure** - Fixed template for all releases  
✅ **Full context** - Includes detailed commit descriptions  
✅ **Semi-automated** - You control the final content  
✅ **Simple workflow** - PowerShell script + Copilot command  
✅ **Independent** - Doesn't modify project code

---

## 📚 Additional Resources

- [Conventional Commits](https://www.conventionalcommits.org/)
- [Semantic Versioning](https://semver.org/)
- [VS Code Prompt Files](https://code.visualstudio.com/docs/copilot/customization/prompt-files)
- [GitHub Copilot Documentation](https://docs.github.com/en/copilot)

---

**Ready to start?** Run `.\scripts\Analyze-Commits.ps1` and use `/generate-release-notes` in Copilot Chat!
