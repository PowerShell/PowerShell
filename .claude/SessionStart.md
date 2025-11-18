# PowerShell Repository - Development Environment Setup

You are working in the **PowerShell** open-source repository. This is a large, cross-platform automation framework written in C# targeting .NET 10.0.

## Quick Repository Context

**Current Build Target:** .NET SDK 10.0.100
**Primary Language:** C# (.NET Core) with PowerShell build scripts
**Test Frameworks:** Pester (PowerShell tests), xUnit (C# unit tests)
**Platforms:** Windows, Linux, macOS

---

## Directory Structure Overview

```
/home/user/PowerShell/
├── src/                          # C# source code (core engine + cmdlets)
│   ├── System.Management.Automation/  # Core PowerShell engine (24MB, 147 engine files)
│   ├── Microsoft.PowerShell.Commands.*/  # Cmdlet implementations
│   ├── Modules/                  # Built-in modules (Windows/Unix/Shared)
│   ├── ResGen/                   # Resource file generator (pre-build tool)
│   └── TypeCatalogGen/           # Type catalog generator (pre-build tool)
├── test/
│   ├── powershell/               # Pester tests (PowerShell scripts)
│   └── xUnit/                    # C# unit tests
├── docs/
│   ├── building/                 # Build guides per platform
│   ├── testing-guidelines/       # Test documentation
│   └── dev-process/              # Development workflows
├── tools/
│   └── ci.psm1                   # CI orchestration (1,241 lines)
├── .github/workflows/            # GitHub Actions CI/CD
├── build.psm1                    # PRIMARY BUILD MODULE (4,023 lines)
└── PowerShell.sln                # Visual Studio solution
```

---

## Available Build Commands

The repository uses PowerShell modules for build automation. Key commands:

### Build System
```powershell
# Import build module (required first step)
Import-Module ./build.psm1

# One-time bootstrap (installs .NET SDK and dependencies)
Start-PSBootstrap -Scope Dotnet

# Build PowerShell (clean build with module restore)
Start-PSBuild -Clean -PSModuleRestore

# Get path to built executable
Get-PSOutput  # Returns path like: src/powershell-unix/bin/Debug/net10.0/linux-x64/publish/pwsh
```

### Testing
```powershell
# Run Pester tests (PowerShell script tests)
Start-PSPester -UseNuGetOrg

# Run xUnit tests (C# unit tests)
Start-PSxUnit

# Run specific test pattern
Start-PSPester -Tests "SomeTestPattern*"

# Tags: [CI] (fast), [Feature] (slower), [Scenario] (integration)
```

### CI Commands
```powershell
Import-Module ./tools/ci.psm1

# Full CI build and test
Invoke-CIFull

# Just build
Invoke-CIBuild

# Just test
Invoke-CITest
```

---

## Custom Slash Commands Available

- `/build` - Clean build PowerShell with module restore
- `/test [pattern]` - Run Pester tests (optional: filter by pattern)
- `/find-cmdlet <name>` - Locate cmdlet implementation files

---

## Key Architecture Notes

### Core Components
1. **Parser/Lexer** - PowerShell language parsing
2. **Execution Engine** - Command execution pipeline (`src/System.Management.Automation/engine/`)
3. **Type System** - .NET type integration
4. **Provider System** - Filesystem, registry abstraction
5. **Remoting** - PowerShell Remoting Protocol (PSRP)

### Build Process Flow
1. **Bootstrap** - Install .NET SDK and dependencies
2. **ResGen** - Generate C# resource classes from .resx files
3. **TypeCatalogGen** - Generate type catalog for type resolution
4. **MSBuild** - Compile C# projects
5. **Output** - Self-contained executable in `src/powershell-*/bin/`

### Testing Requirements
- New features require Pester tests with `[CI]` tag
- Code coverage tracking active in CI
- Platform-specific tests need `[Skip]` directives for unsupported platforms
- Breaking changes require RFC documentation

---

## Common Development Patterns

### Finding Cmdlet Implementations
- **Utility cmdlets** (Get-Date, ConvertFrom-Json): `src/Microsoft.PowerShell.Commands.Utility/`
- **Management cmdlets** (Get-Process, Get-Service): `src/Microsoft.PowerShell.Commands.Management/`
- **Security cmdlets**: `src/Microsoft.PowerShell.Security/`
- **Diagnostic cmdlets**: `src/Microsoft.PowerShell.Commands.Diagnostics/`

### Modifying Cmdlets
1. Find cmdlet class (usually ends with `Command.cs`)
2. Make changes following coding guidelines
3. Update help documentation (if parameters change)
4. Add/update Pester tests in `test/powershell/`
5. Run tests: `Start-PSPester`

### Adding New Cmdlets
1. Create cmdlet class inheriting from `PSCmdlet` or `Cmdlet`
2. Add to appropriate project in `src/Microsoft.PowerShell.Commands.*/`
3. Create help documentation
4. Write comprehensive Pester tests
5. Update module manifest if needed

---

## Platform-Specific Considerations

**Windows:**
- Entry point: `src/powershell-win-core/powershell-win-core.csproj`
- Supports: Event logs, WMI/CIM, registry, Windows services

**Linux/macOS:**
- Entry point: `src/powershell-unix/powershell-unix.csproj`
- Uses: libpsl-native for OS integration
- Conditional compilation with `#if UNIX`

---

## Important Files for AI Coding Tools

### Must-Know Files
1. `build.psm1` - Master build control (4,023 lines)
2. `tools/ci.psm1` - CI orchestration
3. `src/System.Management.Automation/engine/` - Core execution engine
4. `PowerShell.Common.props` - Shared MSBuild properties
5. `global.json` - .NET SDK version specification

### Configuration Files
- `.editorconfig` - Code formatting rules
- `Settings.StyleCop` - StyleCop analyzer configuration
- `.globalconfig` - EditorConfig aggregator (104KB)
- `codecov.yml` - Code coverage configuration

---

## Development Workflow

### Typical Change Workflow
1. **Build first** - Ensure clean baseline: `Start-PSBuild -Clean`
2. **Make changes** - Edit C# code
3. **Rebuild** - Incremental build: `Start-PSBuild`
4. **Test** - Run relevant tests: `Start-PSPester -Tests "YourTest*"`
5. **Verify** - Check code coverage if needed
6. **Commit** - Use descriptive commit messages
7. **CI validation** - GitHub Actions runs on push

### CI/CD Triggers
- Commit message prefixes affect CI behavior:
  - `[Feature]` - Runs Feature-tagged tests (slower suite)
  - `[Package]` - Validates packaging changes

---

## Debugging Tips

### Run Built PowerShell
```powershell
& (Get-PSOutput) -NoProfile  # Start clean session with built binary
```

### Debug in VS Code
- Use `.vscode/launch.json` configurations
- Attach to running pwsh process
- Set breakpoints in C# code

---

## Common Pitfalls to Avoid

1. ❌ Not running ResGen after adding/modifying .resx files
2. ❌ Forgetting to regenerate type catalog when adding types
3. ❌ Missing platform-specific test skips (`[Skip]` directive)
4. ❌ Breaking changes without RFC documentation
5. ❌ Not updating cmdlet help documentation
6. ❌ Ignoring StyleCop warnings

---

## Quick Reference: File Locations

| What | Where |
|------|-------|
| Core engine | `src/System.Management.Automation/engine/` |
| Utility cmdlets | `src/Microsoft.PowerShell.Commands.Utility/` |
| Management cmdlets | `src/Microsoft.PowerShell.Commands.Management/` |
| Pester tests | `test/powershell/` |
| xUnit tests | `test/xUnit/` |
| Build docs | `docs/building/` |
| Test docs | `docs/testing-guidelines/` |
| CI workflows | `.github/workflows/` |

---

## Resources

- **Contributing Guide:** `.github/CONTRIBUTING.md`
- **Coding Guidelines:** `docs/dev-process/coding-guidelines.md`
- **Testing Guidelines:** `docs/testing-guidelines/testing-guidelines.md`
- **Building Internals:** `docs/building/internals.md`

---

**Ready to code!** Use slash commands (`/build`, `/test`, `/find-cmdlet`) for common tasks.
