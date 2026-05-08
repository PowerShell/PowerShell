---
applyTo:
  - "**/*.ps1"
  - "**/*.psm1"
---

# Script and Module File Format

These instructions define required file-level formatting for PowerShell scripts and module files in this repository.

## Copyright Header

Every `.ps1` file and `.psm1` file must start with this exact two-line header at the top of the file:

```powershell
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
```

Do not place blank lines, comments, or code before this header.

## Requirement

- Add the copyright header when creating a new `.ps1` or `.psm1` file.
- Preserve the header when editing an existing `.ps1` or `.psm1` file.
- If a file is missing the header, add it at the very top of the file.
