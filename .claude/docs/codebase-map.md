# PowerShell Codebase Map

Quick reference guide for navigating the PowerShell repository.

## 📁 Top-Level Directory Structure

```
/home/user/PowerShell/
├── src/                    # All C# source code
├── test/                   # Test suites (Pester + xUnit)
├── docs/                   # Developer documentation
├── tools/                  # Build and utility scripts
├── .github/                # GitHub Actions CI/CD
├── .vscode/                # VS Code configuration
├── build.psm1              # PRIMARY BUILD MODULE (4,023 lines)
├── PowerShell.sln          # Visual Studio solution
└── global.json             # .NET SDK version (10.0.100)
```

---

## 🔧 Core Source Code (`src/`)

### Entry Points
| Project | Platform | Description |
|---------|----------|-------------|
| `src/powershell-win-core/` | Windows | Windows console host entry point |
| `src/powershell-unix/` | Linux/macOS | Unix console host entry point |
| `src/Microsoft.PowerShell.SDK/` | Cross-platform | Public SDK NuGet package |

### Core Engine
**`src/System.Management.Automation/`** (24MB - The Heart of PowerShell)

Key subdirectories:
```
System.Management.Automation/
├── engine/              # Core execution engine (147 C# files)
├── CoreCLR/             # CoreCLR-specific implementation
├── FormatAndOutput/     # Output formatting (Format-Table, Format-List)
├── help/                # Help system implementation
├── security/            # ExecutionPolicy, signing, etc.
├── logging/             # Logging infrastructure
├── DscSupport/          # Desired State Configuration
├── namespaces/          # PowerShell namespace implementations
└── utils/               # Utility classes
```

**Key Engine Components:**
- Parser/Lexer - Language parsing
- Pipeline - Object pipeline architecture
- Type System - .NET type integration
- Provider System - Filesystem, registry abstraction
- Remoting - PowerShell Remoting Protocol (PSRP)

---

## 📦 Cmdlet Implementations

### Utility Cmdlets
**`src/Microsoft.PowerShell.Commands.Utility/`** (2.0MB)

Common cmdlets here:
- **Data:** Get-Date, Get-Random, Get-Unique
- **JSON:** ConvertFrom-Json, ConvertTo-Json
- **CSV:** ConvertFrom-Csv, ConvertTo-Csv, Import-Csv, Export-Csv
- **XML:** ConvertTo-Xml, Select-Xml
- **Object:** Select-Object, Where-Object, ForEach-Object, Sort-Object
- **Measure:** Measure-Object, Compare-Object, Group-Object
- **Output:** Write-Host, Write-Output, Write-Error, Write-Verbose
- **Format:** Format-Table, Format-List, Format-Wide, Format-Custom

### Management Cmdlets
**`src/Microsoft.PowerShell.Commands.Management/`** (1.7MB)

Common cmdlets here:
- **Process:** Get-Process, Start-Process, Stop-Process, Wait-Process
- **Service:** Get-Service, Start-Service, Stop-Service, Restart-Service
- **File System:** Get-Item, Get-ChildItem, Copy-Item, Move-Item, Remove-Item
- **Content:** Get-Content, Set-Content, Add-Content, Clear-Content
- **Path:** Test-Path, Split-Path, Join-Path, Resolve-Path
- **Location:** Get-Location, Set-Location, Push-Location, Pop-Location
- **Computer:** Restart-Computer, Stop-Computer

### Security Cmdlets
**`src/Microsoft.PowerShell.Security/`**

Cmdlets:
- Get-ExecutionPolicy, Set-ExecutionPolicy
- Get-Credential
- ConvertFrom-SecureString, ConvertTo-SecureString
- Get-Acl, Set-Acl
- Certificate provider cmdlets

### Diagnostic Cmdlets
**`src/Microsoft.PowerShell.Commands.Diagnostics/`**

Cmdlets:
- Get-EventLog (Windows)
- Get-Counter (Performance counters)
- Get-WinEvent (Windows Event Log)

### Other Command Modules
| Module | Description |
|--------|-------------|
| `Microsoft.WSMan.Management/` | WinRM/WSMan cmdlets (Windows Remote Management) |
| `Microsoft.Management.Infrastructure.CimCmdlets/` | CIM cmdlets (Get-CimInstance, etc.) |
| `Microsoft.PowerShell.LocalAccounts/` | Local user/group management (Windows only) |

---

## 🎭 Console and Hosting

**`src/Microsoft.PowerShell.ConsoleHost/`**
- Interactive shell implementation
- Console UI (PSReadLine integration)
- Command-line parsing
- REPL (Read-Eval-Print Loop)

**`src/Microsoft.Management.UI.Internal/`** (1.4MB)
- GUI components (Show-Command)
- Out-GridView implementation

---

## 📚 Built-in Modules

**`src/Modules/`**

Structure:
```
Modules/
├── Windows/          # Windows-only modules (ComputerManagementDsc, etc.)
├── Unix/             # Unix-only modules
└── Shared/           # Cross-platform modules (PSDesiredStateConfiguration, etc.)
```

---

## 🔨 Build Tools

### Pre-Build Code Generators
| Tool | Purpose | When It Runs |
|------|---------|--------------|
| `src/ResGen/` | Generates strongly-typed C# resource classes from .resx files | Pre-build (automatic) |
| `src/TypeCatalogGen/` | Generates `CorePsTypeCatalog.cs` for type resolution | Pre-build (automatic) |

### Native Code
| Directory | Purpose |
|-----------|---------|
| `src/powershell-native/` | Native code wrappers |
| `src/libpsl-native/` | Native library bindings for Unix platforms |

---

## 🧪 Test Structure (`test/`)

### Pester Tests (PowerShell)
**`test/powershell/`** - Main test suite

Test categories:
```
powershell/
├── Language/           # Language feature tests (operators, syntax, etc.)
├── engine/             # Engine behavior tests
├── Modules/            # Module tests
├── Host/               # Console host tests
├── Provider/           # Provider tests (FileSystem, Registry, etc.)
├── dsc/                # DSC tests
├── SDK/                # SDK tests
└── [20+ categories]    # Various test areas
```

**Test Tags:**
- `[CI]` - Fast tests, run on every PR
- `[Feature]` - Slower tests, run daily
- `[Scenario]` - Integration tests
- `[Slow]` - Tests taking >1 second

### xUnit Tests (C#)
**`test/xUnit/`** - C# unit tests

Growing test suite for C# unit testing (alternative to Pester for low-level tests).

### Test Helper Modules
**`test/tools/Modules/`**

Helper modules used by tests:
- `HelpersCommon` - Common test utilities
- `HelpersLanguage` - Language test helpers
- `HelpersRemoting` - Remoting test utilities
- `HttpListener`, `WebListener` - Web testing
- `UnixSocket` - Unix socket testing

---

## 📖 Documentation (`docs/`)

### Build Documentation
**`docs/building/`**
- `internals.md` - Build process internals
- `windows-core.md` - Windows build guide
- `linux.md` - Linux build guide
- `macos.md` - macOS build guide

### Development Process
**`docs/dev-process/`**
- `coding-guidelines.md` - C# coding standards
- `breaking-change-contract.md` - Breaking change policy
- `resx-files.md` - Resource file handling

### Testing Guidelines
**`docs/testing-guidelines/`**
- `testing-guidelines.md` - Testing overview
- `WritingPesterTests.md` - Pester best practices
- `getting-code-coverage.md` - Code coverage guide

---

## 🤖 CI/CD (`.github/`)

### GitHub Actions Workflows
**`.github/workflows/`**

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `windows-ci.yml` | Push/PR to master | Windows build & test |
| `linux-ci.yml` | Push/PR to master | Linux build & test |
| `macos-ci.yml` | Push/PR to master | macOS build & test |
| `xunit-tests.yml` | PR | C# xUnit tests |
| `dependency-review.yml` | PR | Dependency checks |
| `verify-markdown-links.yml` | Push | Doc link validation |

### GitHub Actions
**`.github/actions/`**

Reusable actions:
```
actions/
├── build/ci/           # Build action implementations
├── test/               # Test runners (linux, windows, macos)
└── infrastructure/     # Infrastructure actions
    ├── path-filters/         # Change detection
    ├── get-changed-files/    # File change tracking
    └── markdownlinks/        # Link validation
```

---

## ⚙️ Configuration Files

### Build Configuration
| File | Purpose |
|------|---------|
| `global.json` | .NET SDK version specification (10.0.100) |
| `PowerShell.Common.props` | Shared MSBuild properties |
| `DotnetRuntimeMetadata.json` | Runtime and SDK configuration |
| `PowerShell.sln` | Visual Studio solution file |

### Code Quality
| File | Purpose |
|------|---------|
| `.editorconfig` | IDE editor settings |
| `.globalconfig` | EditorConfig aggregator (104KB) |
| `Settings.StyleCop` | StyleCop analyzer rules |
| `stylecop.json` | StyleCop configuration |
| `Analyzers.props` | Code analyzer settings |
| `codecov.yml` | Code coverage configuration |

### IDE Configuration
| File | Purpose |
|------|---------|
| `.vscode/tasks.json` | VS Code build tasks |
| `.vscode/launch.json` | VS Code debugger configuration |
| `.vscode/extensions.json` | Recommended VS Code extensions |
| `.devcontainer/` | Docker dev container setup |

---

## 🚀 Build Automation

### Primary Build Module
**`build.psm1`** (4,023 lines)

Key functions:
- `Start-PSBootstrap` - Install .NET SDK and dependencies
- `Start-PSBuild` - Build PowerShell
- `Start-PSPester` - Run Pester tests
- `Start-PSxUnit` - Run xUnit tests
- `Get-PSOutput` - Get path to built executable
- `Start-PSPackage` - Create distribution packages

### CI Module
**`tools/ci.psm1`** (1,241 lines)

CI-specific functions:
- `Invoke-CIFull` - Full CI pipeline
- `Invoke-CIBuild` - CI build only
- `Invoke-CITest` - CI test only
- `New-CodeCoverageAndTestPackage` - Generate coverage reports

---

## 🎯 Common File Locations Quick Reference

### "Where do I find...?"

| What | Where |
|------|-------|
| Core execution engine | `src/System.Management.Automation/engine/` |
| Parser/Lexer | `src/System.Management.Automation/engine/parser/` |
| Cmdlet: Get-Date | `src/Microsoft.PowerShell.Commands.Utility/commands/utility/GetDateCommand.cs` |
| Cmdlet: Get-Process | `src/Microsoft.PowerShell.Commands.Management/commands/management/GetProcessCommand.cs` |
| Cmdlet: ConvertFrom-Json | `src/Microsoft.PowerShell.Commands.Utility/commands/utility/WebCmdlet/` |
| Console host | `src/Microsoft.PowerShell.ConsoleHost/` |
| Help system | `src/System.Management.Automation/help/` |
| Security subsystem | `src/System.Management.Automation/security/` |
| Formatting system | `src/System.Management.Automation/FormatAndOutput/` |
| Language tests | `test/powershell/Language/` |
| Engine tests | `test/powershell/engine/` |
| Build documentation | `docs/building/` |
| Test documentation | `docs/testing-guidelines/` |
| Coding guidelines | `docs/dev-process/coding-guidelines.md` |
| CI workflows | `.github/workflows/` |

---

## 🔍 Search Strategies

### Finding a Cmdlet Implementation
1. **By verb-noun:** Search for `<Verb><Noun>Command.cs`
   - Example: `Get-Date` → `GetDateCommand.cs`
2. **By category:** Check the likely module directory
   - Utility → `Microsoft.PowerShell.Commands.Utility/`
   - Management → `Microsoft.PowerShell.Commands.Management/`
   - Security → `Microsoft.PowerShell.Security/`

### Finding Tests
1. **Cmdlet tests:** Search `test/powershell/` for `*<Noun>*.Tests.ps1`
2. **Feature tests:** Check `test/powershell/Language/` or `test/powershell/engine/`
3. **Module tests:** Look in `test/powershell/Modules/<ModuleName>/`

### Finding Documentation
1. **Build guides:** `docs/building/<platform>.md`
2. **API/development:** `docs/dev-process/`
3. **Testing:** `docs/testing-guidelines/`

---

## 💡 Architecture Patterns

### Cmdlet Design Pattern
```csharp
namespace Microsoft.PowerShell.Commands
{
    [Cmdlet(VerbsCommon.Get, "Example")]
    [OutputType(typeof(ExampleObject))]
    public sealed class GetExampleCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            // Implementation
            WriteObject(result);
        }
    }
}
```

### Test Design Pattern (Pester)
```powershell
Describe "Get-Example" -Tags "CI" {
    It "Should return object when name is provided" {
        $result = Get-Example -Name "test"
        $result | Should -Not -BeNullOrEmpty
    }
}
```

---

## 🎓 Key Concepts

### Build Flow
1. **Bootstrap** → Install .NET SDK
2. **ResGen** → Generate resource classes
3. **TypeCatalogGen** → Generate type catalog
4. **MSBuild** → Compile C# projects
5. **Output** → Self-contained executable in `src/powershell-*/bin/`

### Platform Handling
- **Conditional compilation:** `#if UNIX`, `#if WINDOWS`
- **Platform-specific projects:** `powershell-win-core` vs `powershell-unix`
- **Native libraries:** `libpsl-native` for Unix integration

### Module System
- **Built-in modules:** In `src/Modules/`
- **Platform-specific:** Separate Windows/Unix/Shared directories
- **Module manifests:** `.psd1` files define module metadata

---

## 📝 Notes for AI Tools

### What to Know
1. This is a **large codebase** (24MB core engine alone)
2. **Multi-platform** support is critical (Windows, Linux, macOS)
3. **Extensive testing** required (Pester + xUnit)
4. **Breaking changes** need RFC documentation
5. **Build tools** (ResGen, TypeCatalogGen) run automatically

### Common Tasks
- **Add cmdlet:** Create class in appropriate `Commands.*` project
- **Modify cmdlet:** Find in `src/Microsoft.PowerShell.Commands.*/`
- **Add test:** Create or modify Pester test in `test/powershell/`
- **Update docs:** Use PlatyPS for cmdlet help

### Pitfalls to Avoid
- Not running ResGen after modifying .resx files
- Forgetting platform-specific test skips
- Missing test coverage (especially `[CI]` tagged tests)
- Breaking changes without proper documentation
- Ignoring StyleCop warnings

---

**Last Updated:** Based on codebase audit - .NET SDK 10.0.100 target
