# DSC MOF Compilation Demo

[PowerShell Desired State Configuration](https://learn.microsoft.com/powershell/dsc/overview) is a declarative configuration platform for Windows and Linux.
DSC configurations can be authored in PowerShell and compiled into the resultant MOF document.

This demo shows use of PowerShell to author a DSC configuration to set the configuration of an Apache web server. PowerShell scripting is used to assess distribution and version-specific properties,
such as the service controller and repo manager tools, for use in the configuration.

## Prerequisites

- PowerShell >= 6.0.0-alpha.8 [https://github.com/PowerShell/PowerShell/releases](https://github.com/PowerShell/PowerShell/releases)
- OMI: >= 1.1.0  [https://www.github.com/microsoft/omi/releases](https://www.github.com/microsoft/omi/releases)
- Desired State Configuration for Linux >= 1.1.1-278 [https://github.com/Microsoft/PowerShell-DSC-for-Linux/releases](https://github.com/Microsoft/PowerShell-DSC-for-Linux/releases)

> Note: applying the DSC configuration requires privileges. The user must have sudo authorization capabilities. You will be prompted for a sudo password when running the demo.
