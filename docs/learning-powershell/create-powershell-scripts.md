# How to Create and Run PowerShell Scripts

You can combine a series of commands in a text file and save it with the file extension '.ps1', and the file will become a PowerShell script.
This would begin by opening your favorite text editor and pasting in the following example.

```powershell
# Script to return current IPv4 addresses on a Linux or MacOS host
$ipInfo = ifconfig | Select-String 'inet'
$ipInfo = [regex]::matches($ipInfo,"addr:\b(?:\d{1,3}\.){3}\d{1,3}\b") | ForEach-Object value
foreach ($ip in $ipInfo)
{
    $ip.Replace('addr:','')
}
```

Then save the file to something memorable, such as .\NetIP.ps1.
In the future when you need to get the IP addresses for the node, you can simplify this task by executing the script.

```powershell
.\NetIP.ps1
10.0.0.1
127.0.0.1
```

You can accomplish this same task on Windows.

```powershell
# One line script to return current IPv4 addresses on a Windows host
Get-NetIPAddress | Where-Object {$_.AddressFamily -eq 'IPv4'} | ForEach-Object IPAddress
```

As before, save the file as .\NetIP.ps1 and execute within a PowerShell environment.
Note: If you are using Windows, make sure you set the PowerShell's execution policy to "RemoteSigned" in this case.
See [Running PowerShell Scripts Is as Easy as 1-2-3][run-ps] for more details.

```powershell
NetIP.ps1
127.0.0.1
10.0.0.1
```

## Creating a script that can accomplish the same task on multiple operating systems

If you would like to author one script that will return the IP address across Linux, MacOS, or Windows, you could accomplish this using an IF statement.

```powershell
# Script to return current IPv4 addresses for Linux, MacOS, or Windows
$IP = if ($IsLinux -or $IsMacOS)
{
    $ipInfo = ifconfig | Select-String 'inet'
    $ipInfo = [regex]::matches($ipInfo,"addr:\b(?:\d{1,3}\.){3}\d{1,3}\b") | ForEach-Object value
    foreach ($ip in $ipInfo) {
        $ip.Replace('addr:','')
    }
}
else
{
    Get-NetIPAddress | Where-Object {$_.AddressFamily -eq 'IPv4'} | ForEach-Object IPAddress
}

# Remove loopback address from output regardless of platform
$IP | Where-Object {$_ -ne '127.0.0.1'}
```

[run-ps]:https://www.itprotoday.com/powershell/running-powershell-scripts-easy-1-2-3
