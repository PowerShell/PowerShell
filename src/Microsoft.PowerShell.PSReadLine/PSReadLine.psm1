function PSConsoleHostReadline
{
    Microsoft.PowerShell.Core\Set-StrictMode -Off
    [Microsoft.PowerShell.PSConsoleReadLine]::ReadLine($host.Runspace, $ExecutionContext)
}
