function Start-DevPSGitHub
{
    param(
        [switch]$ZapDisable,
        [string[]]$ArgumentList = '',
        [switch]$LoadProfile,
        [string]$binDir = "$PSScriptRoot\binFull",
        [switch]$NoNewWindow
    )

    try
    {
        if ($LoadProfile -eq $false)
        {
            $ArgumentList = @('-noprofile') + $ArgumentList
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
        
        # splatting for the win      
        $startProcessArgs = @{
            FilePath = "$binDir\powershell.exe"
            ArgumentList = "$ArgumentList"
        }  

        if ($NoNewWindow) {
            $startProcessArgs.NoNewWindow = $true
            $startProcessArgs.Wait = $true
        }
        
        Start-Process @startProcessArgs
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
