## SystemD: journalctl demo

This demo shows use of a PowerShell script module to wrap a native tool (journalctl) so that the output is structured for filtering and presentation control. `journalctl` is expressed as a cmdlet: Get-SystemDJournal, and the JSON output of journalctl is converted to a PowerShell object. 

## Prerequisites ##
- Requires a SystemD-based operating system (Red Hat or CentOS 7, Ubuntu 16.04)
- Install PowerShell


Note: Accessing the SystemD journal requires privileges. The user must have authorization to elevate with sudo. You will be prompted for a sudo password when running the demo.