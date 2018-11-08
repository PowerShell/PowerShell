Describe 'Default Cmdlet ConfirmImpact Ratings' -Tags 'CI' {
    BeforeAll {
        $DefaultCommands = @(
        # ConfirmImpact.High
            # No default cmdlets currently use High ConfirmImpact

        # ConfirmImpact.Medium
            @{ Cmdlet = 'Add-Content'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Add-History'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Add-Member'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Add-Type'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Clear-Content'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Clear-History'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Clear-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Clear-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Clear-Variable'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Compare-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Connect-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertFrom-Csv'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertFrom-Json'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertFrom-Markdown'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertFrom-StringData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Convert-Path'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertTo-Csv'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertTo-Html'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertTo-Json'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ConvertTo-Xml'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Copy-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Copy-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Debug-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Debug-Process'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Debug-Runspace'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Disable-PSBreakpoint'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Disable-PSRemoting'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Disable-RunspaceDebug'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Disconnect-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enable-PSBreakpoint'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enable-PSRemoting'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enable-PSSessionConfig'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enable-RunspaceDebug'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enter-PSHostProcess'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Enter-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Exit-PSHostProcess'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Exit-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-Clixml'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-Csv'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-FormatData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-ModuleMember'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Export-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'ForEach-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Format-Custom'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Format-Hex'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Format-List'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Format-Table'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Format-Wide'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-ChildItem'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Command'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-ComputerInfo'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Content'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Culture'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Date'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Event'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-EventSubscriber'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-ExperimentalFeatur'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-FileHash'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-FormatData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Help'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-History'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Host'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-ItemPropertyValue'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Location'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-MarkdownOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Member'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Module'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Process'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSBreakpoint'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSCallStack'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSDrive'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSHostProcessInfo'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSProvider'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSReadLineKeyHandl'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSReadLineOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSSessionCapabilit'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-PSSessionConfigura'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Random'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Runspace'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-RunspaceDebug'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-TimeZone'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-TraceSource'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-TypeData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-UICulture'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Unique'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Uptime'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Variable'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Get-Verb'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Group-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-Clixml'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-Csv'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-LocalizedData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-Module'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-PowerShellDataFile'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Import-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-Command'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-Expression'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-History'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-RestMethod'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Invoke-WebRequest'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Join-Path'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Measure-Command'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Measure-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Move-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Move-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Event'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Guid'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Module'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-ModuleManifest'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSDrive'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSRoleCapabilityFile'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSSessionConfiguration'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSSessionOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-PSTransportOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-TemporaryFile'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-TimeSpan'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'New-Variable'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Out-Default'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Out-File'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Out-Host'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Out-Null'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Out-String'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Pop-Location'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Push-Location'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Read-Host'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Receive-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Receive-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Register-ArgumentCompleter'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Register-EngineEvent'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Register-ObjectEvent'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Register-PSSessionConfiguration'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Event'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Module'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-PSBreakpoint'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-PSDrive'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-PSReadLineKeyHandler'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-PSSession'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-TypeData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Remove-Variable'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Rename-Computer'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Rename-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Rename-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Resolve-Path'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Restart-Computer'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Restart-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Resume-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Save-Help'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Select-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Select-String'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Select-Xml'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Send-MailMessage'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Alias'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Content'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Date'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Item'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-ItemProperty'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Location'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-MarkdownOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-PSBreakpoint'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-PSDebug'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-PSReadLineKeyHandler'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-PSReadLineOption'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-PSSessionConfiguration'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-StrictMode'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-TimeZone'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-TraceSource'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Set-Variable'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Show-Markdown'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Sort-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Split-Path'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Start-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Start-Process'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Start-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Start-Sleep'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Stop-Computer'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Stop-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Stop-Process'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Stop-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Suspend-Service'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Tee-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Test-Connection'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Test-Json'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Test-ModuleManifest'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Test-Path'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Test-PSSessionConfigurationFile'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Trace-Command'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Unblock-File'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Unregister-Event'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Update-FormatData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Update-Help'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Update-TypeData'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Wait-Debugger'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Wait-Event'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Wait-Job'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Wait-Process'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Where-Object'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Debug'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Error'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Host'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Information'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Output'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Progress'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Verbose'; ConfirmImpact = 'Medium' }
            @{ Cmdlet = 'Write-Warning'; ConfirmImpact = 'Medium' }

        # ConfirmImpact.Low
            @{ Cmdlet = 'Disable-PSSessionConfiguration'; ConfirmImpact = 'Low' }
            @{ Cmdlet = 'Unregister-PSSessionConfiguration'; ConfirmImpact = 'Low' }

        # ConfirmImpact.None
            # No default cmdlets currently will report ConfirmImpact.None
        ) # $DefaultCommands
    }

    It 'List of cmdlets should match available commands' {
        $Commands = (Get-Command -CommandType Cmdlet).Where{$_.ImplementingType}.Count
        $DefaultCommands | Should -HaveCount $Commands
    }

    It '<Cmdlet> should have a ConfirmImpact rating of <Rating>' -TestCases $TestCases {
        param($Cmdlet, $ConfirmImpact)

        (Get-Command -Name $Cmdlet).ImplementingType.GetCustomAttributes($true).Where{$_.VerbName -ne $null}.ConfirmImpact |
            Should -Be $ConfirmImpact
    }
}
