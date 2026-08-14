---
applyTo:
  - ".github/instructions/**/*.instructions.md"
---

# Instruction File Format Guide

This document describes the format and guidelines for creating custom instruction files for GitHub Copilot in the PowerShell repository.

## File Naming Convention

All instruction files must use the `.instructions.md` suffix:
- ✅ Correct: `build-checkout-prerequisites.instructions.md`
- ✅ Correct: `start-psbuild-basics.instructions.md`
- ❌ Incorrect: `build-guide.md`
- ❌ Incorrect: `instructions.md`

## Required Frontmatter

Every instruction file must start with YAML frontmatter containing an `applyTo` section:

```yaml
---
applyTo:
  - "path/to/files/**/*.ext"
  - "specific-file.ext"
---
```

### applyTo Patterns

Specify which files or directories these instructions apply to:

**For workflow files:**
```yaml
applyTo:
  - ".github/**/*.yml"
  - ".github/**/*.yaml"
```

**For build scripts:**
```yaml
applyTo:
  - "build.psm1"
  - "tools/ci.psm1"
```

**For multiple contexts:**
```yaml
applyTo:
  - "build.psm1"
  - "tools/**/*.psm1"
  - ".github/**/*.yml"
```

## Content Structure

### 1. Clear Title

Use a descriptive H1 heading after the frontmatter:

```markdown
# Build Configuration Guide
```

### 2. Purpose or Overview

Start with a brief explanation of what the instructions cover:

```markdown
## Purpose

This guide explains how to configure PowerShell builds for different scenarios.
```

### 3. Actionable Content

Provide clear, actionable guidance:

**✅ Good - Specific and actionable:**
```markdown
## Default Usage

Use `Start-PSBuild` with no parameters for testing:

```powershell
Import-Module ./tools/ci.psm1
Start-PSBuild
```
```

**❌ Bad - Vague and unclear:**
```markdown
## Usage

You can use Start-PSBuild to build stuff.
```

### 4. Code Examples

Include working code examples with proper syntax highlighting:

```markdown
```yaml
- name: Build PowerShell
  shell: pwsh
  run: |
    Import-Module ./tools/ci.psm1
    Start-PSBuild
```
```

### 5. Context and Rationale

Explain why things are done a certain way:

```markdown
**Why fetch-depth: 1000?**
- The build system needs Git history for version calculation
- Shallow clones would break versioning logic
```

## Best Practices

### Be Concise

- Focus on essential information
- Remove redundant explanations
- Use bullet points for lists

### Be Specific

- Provide exact commands and parameters
- Include file paths and line numbers when relevant
- Show concrete examples, not abstract concepts

### Avoid Duplication

- Don't repeat information from other instruction files
- Reference other files when appropriate
- Keep each file focused on one topic

### Use Proper Formatting

**Headers:**
- Use H1 (`#`) for the main title
- Use H2 (`##`) for major sections
- Use H3 (`###`) for subsections

**Code blocks:**
- Always specify the language: ` ```yaml `, ` ```powershell `, ` ```bash `
- Keep examples short and focused
- Test examples before including them

**Lists:**
- Use `-` for unordered lists
- Use `1.` for ordered lists
- Keep list items concise

## Example Structure

```markdown
---
applyTo:
  - "relevant/files/**/*.ext"
---

# Title of Instructions

Brief description of what these instructions cover.

## Section 1

Content with examples.

```language
code example
```

## Section 2

More specific guidance.

### Subsection

Detailed information when needed.

## Best Practices

- Actionable tip 1
- Actionable tip 2
```

## Maintaining Instructions

### When to Create a New File

Create a new instruction file when:
- Covering a distinct topic not addressed elsewhere
- The content is substantial enough to warrant its own file
- The `applyTo` scope is different from existing files

### When to Update an Existing File

Update an existing file when:
- Information is outdated
- New best practices emerge
- Examples need correction

### When to Merge or Delete

Merge or delete files when:
- Content is duplicated across multiple files
- A file is too small to be useful standalone
- Information is no longer relevant

## Reference

For more details, see:
- [GitHub Copilot Custom Instructions Documentation](https://docs.github.com/en/copilot/how-tos/configure-custom-instructions/add-repository-instructions)
