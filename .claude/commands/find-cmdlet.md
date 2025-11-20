---
description: Locate cmdlet implementation files in the codebase
---

Find the implementation files for a specific PowerShell cmdlet.

**Usage:** `/find-cmdlet <cmdlet-name>`

Example: `/find-cmdlet Get-Date` or `/find-cmdlet ConvertFrom-Json`

**Steps to locate cmdlet:**

1. Parse the cmdlet name (e.g., "Get-Date" → verb: Get, noun: Date)

2. Search for the cmdlet class implementation:
   - Pattern: `<Verb><Noun>Command.cs` (e.g., `GetDateCommand.cs`)
   - Use Grep tool to search for: `class <Verb><Noun>Command`
   - Common locations:
     - `src/Microsoft.PowerShell.Commands.Utility/` - Utility cmdlets (Get-Date, ConvertFrom-Json, Select-Object, etc.)
     - `src/Microsoft.PowerShell.Commands.Management/` - Management cmdlets (Get-Process, Get-Service, Get-Item, etc.)
     - `src/Microsoft.PowerShell.Security/` - Security cmdlets (Get-ExecutionPolicy, ConvertFrom-SecureString, etc.)
     - `src/Microsoft.PowerShell.Commands.Diagnostics/` - Diagnostic cmdlets (Get-EventLog, Get-Counter, etc.)
     - `src/Microsoft.WSMan.Management/` - WSMan cmdlets
     - `src/Microsoft.Management.Infrastructure.CimCmdlets/` - CIM cmdlets

3. Also search for related files:
   - Tests: `test/powershell/**/*<Noun>*.Tests.ps1`
   - Help documentation: Look for PlatyPS markdown files

4. Report findings:
   - Main implementation file with line count
   - Related test files
   - Brief description of what the cmdlet does (from file comments if available)
   - Show the class signature and key properties/methods

**Cmdlet Organization by Category:**

**Utility Commands** (`Microsoft.PowerShell.Commands.Utility/`):
- Get-Date, Get-Random, Get-Unique
- ConvertFrom-Json, ConvertTo-Json, ConvertFrom-Csv, ConvertTo-Csv
- Select-Object, Where-Object, ForEach-Object
- Write-Host, Write-Output, Write-Error
- Measure-Object, Compare-Object, Group-Object
- Format-Table, Format-List

**Management Commands** (`Microsoft.PowerShell.Commands.Management/`):
- Get-Process, Get-Service, Get-Item, Get-ChildItem
- Start-Process, Stop-Process
- Get-Content, Set-Content
- Copy-Item, Move-Item, Remove-Item
- Test-Path, Split-Path, Join-Path

**Security Commands** (`Microsoft.PowerShell.Security/`):
- Get-ExecutionPolicy, Set-ExecutionPolicy
- ConvertFrom-SecureString, ConvertTo-SecureString
- Get-Credential
- Certificate provider cmdlets

**Note:** If cmdlet not found, suggest it might be:
- A function/alias (not a compiled cmdlet)
- Part of an external module
- Available only on specific platforms (Windows/Linux)
