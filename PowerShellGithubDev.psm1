function Start-DevPSGithub
{
    param(
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = "$PSScriptRoot\binFull"
    )

    try
    {
        if ($LoadProfile -eq $false)
        {
            $ArgumentList += '-noprofile'
        }

        $env:DEVPATH = $binDir
        if ($ZapDisable)
        {
            $env:COMPLUS_ZapDisable = 1 
        }

        if (!(Test-Path $binDir\powershell.exe.config))
        {
            $configContents = @"
<?xml version="1.0" encoding="utf-8" ?> 
<configuration> 
    <runtime>
        <developmentMode developerInstallation="true"/>
    </runtime>
</configuration>
"@
            $configContents | Out-File -Encoding Ascii $binDir\powershell.exe.config
        }

        Start-Process -FilePath $binDir\powershell.exe -ArgumentList "$ArgumentList"
    }
    finally
    {
        ri env:DEVPATH
        if ($ZapDisable)
        {
            ri env:COMPLUS_ZapDisable
        }
    }
}