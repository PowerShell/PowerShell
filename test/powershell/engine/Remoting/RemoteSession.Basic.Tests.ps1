
Describe "New-PSSession basic test" -Tag @("CI") {
    It "New-PSSession should not crash powershell" {
        try {
            New-PSSession -ComputerName nonexistcomputer -Authentication Basic
            throw "New-PSSession should throw"
        } catch {
            $_.FullyQualifiedErrorId | Should Be "InvalidOperation,Microsoft.PowerShell.Commands.NewPSSessionCommand"
        }
    }
}

Describe "JEA session Transcprit script test" -Tag @("Feature", 'RequireAdminOnWindows') {
    It "Configuration name should be in the transcript header" {
        [string] $RoleCapDirectory = (New-Item -Path "$TestDrive\RoleCapability" -ItemType Directory -Force).FullName
        [string] $PSSessionConfigFile = "$RoleCapDirectory\TestConfig.pssc"
        [string] $transScriptFile = "$RoleCapDirectory\*.txt"
        try
        {
            New-PSSessionConfigurationFile -Path $PSSessionConfigFile -TranscriptDirectory $RoleCapDirectory -SessionType RestrictedRemoteServer
            Register-PSSessionConfiguration -Name JEA -Path $PSSessionConfigFile -Force -ErrorAction SilentlyContinue 
            $scriptBlock = {Enter-PSSession -ComputerName Localhost -ConfigurationName JEA; Exit-PSSession}
            & $scriptBlock
            $headerFile = Get-ChildItem $transScriptFile | Sort-Object LastWriteTime | Select-Object -Last 1
            $header = Get-Content $headerFile | Out-String
            $header | Should BeLike "Configuration Name: JEA"
        }
        finally
        {
            Unregister-PSSessionConfiguration -Name JEA -Force -ErrorAction SilentlyContinue
        }
    }

}