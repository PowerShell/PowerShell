# (Get-Process -Id:(Where-Object({$_.processname -match 'outlook'}))|Format-List -GroupBy Id)

# Get-process -ComputerName C015A8201 -Name Procbook,OUTLOOK | Format-List Id

# Start-Process -FilePath '\\C015A8201\C$\Windows\System32\WindowsPowerShell\v1.0\powershell.exe'

Get-PSProvider -PSProvider *