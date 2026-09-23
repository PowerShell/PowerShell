---
applyTo:
  - "**/*.ps1"
  - "**/*.psm1"
---

# Script and Module File Format

These instructions define required file-level formatting for PowerShell scripts and module files in this repository.

## Copyright Header

If a change adds a new `.ps1` file or `.psm1` file or touches an existing one, the file should start with the copyright and license header and have an empty line after it, as shown in the example below:

```powershell
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

```

Do not place blank lines, comments, or code before this header.

## Requirements

- Add the copyright header when creating a new `.ps1` or `.psm1` file.
- Preserve the header when editing an existing `.ps1` or `.psm1` file.
- If an existing `.ps1` or `.psm1` file is missing the header, only modify that file to add the header if a change touches that file. Do not make a change to add the header if the file is not being modified.
