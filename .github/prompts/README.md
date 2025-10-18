# GitHub Copilot Prompt Files

This folder contains reusable prompt files for GitHub Copilot in VS Code.

## 📁 Available Prompts

### `/generate-release-notes`

**File**: `generate-release-notes.prompt.md`

**Description**: Generates professional release notes from Git commit history with semantic versioning.

**Usage**:

1. Run commit analyzer: `.\scripts\Analyze-Commits.ps1`
2. Open Copilot Chat (`Ctrl+Shift+I`)
3. Type: `/generate-release-notes`
4. Paste commit data from clipboard

**Features**:

- ✅ Uses semantic versioning rules
- ✅ Fixed template structure
- ✅ Transforms commits into user-friendly descriptions
- ✅ Supports input variables
- ✅ References workspace files

**Variables**:

- `${input:version}` - New version number
- `${input:previousVersion}` - Current version number

**Referenced Files**:

- `scripts/release-notes-template.md` - Template structure
- `scripts/config.json` - Versioning configuration

---

## 🛠️ Setup

### Enable Prompt Files (One Time)

1. Open VS Code Settings (`Ctrl+,`)
2. Search: **"Chat: Prompt Files"**
3. Enable checkbox ✅
4. Restart VS Code if needed

### Verify It Works

1. Open Copilot Chat (`Ctrl+Shift+I`)
2. Type `/` to see available prompts
3. You should see `/generate-release-notes` in the list

---

## 📚 Learn More

- [VS Code Prompt Files Documentation](https://code.visualstudio.com/docs/copilot/customization/prompt-files)
- [Release Notes Generator Documentation](../../scripts/README.md)

---

## 🎯 Creating New Prompt Files

To create additional prompt files:

1. Create a new `.prompt.md` file in this folder
2. Add YAML frontmatter:
   ```yaml
   ---
   description: Your prompt description
   mode: agent
   tools:
     - codebase
   ---
   ```
3. Write your prompt instructions in Markdown
4. Use variables with `${input:variableName}` syntax
5. Reference files with Markdown links

**Example**:

```markdown
---
description: Generate API documentation
mode: agent
---

# Generate API Documentation

Generate comprehensive API documentation for ${input:endpoint}.

[Reference template](../../docs/api-template.md)
```

Then use it: `/generate-api-documentation` in Copilot Chat.

---

**Note**: Prompt files are workspace-specific and stored in `.github/prompts/` by default.
