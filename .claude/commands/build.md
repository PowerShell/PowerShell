---
description: Clean build PowerShell with module restore
---

Build PowerShell from source with a clean build and module restore.

Follow these steps:

1. Import the build module: `Import-Module ./build.psm1 -Force`
2. Run clean build: `Start-PSBuild -Clean -PSModuleRestore`
3. Report the build output path using `Get-PSOutput`
4. Report any build errors or warnings that occurred
5. Provide next steps (e.g., "Run `/test` to validate the build")

**Important:**
- The build process will take several minutes
- ResGen and TypeCatalogGen will run automatically during pre-build
- Output will be in `src/powershell-unix/bin/` (Linux/Mac) or `src/powershell-win-core/bin/` (Windows)
- If build fails, analyze the error and suggest fixes
