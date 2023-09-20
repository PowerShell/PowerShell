# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module HelpersCommon

function GetRandomString()
{
    return [System.IO.Path]::GetFileNameWithoutExtension([System.IO.Path]::GetRandomFileName())
}

Describe "New-PSSession basic test" -Tag @("CI") {
    It "New-PSSession should not crash powershell" {
        if ( -not (Get-WsManSupport)) {
            Set-ItResult -Skipped -Because "MI library not available for this platform"
            return
        }

        { New-PSSession -ComputerName (GetRandomString) -Authentication Basic } |
           Should -Throw -ErrorId "InvalidOperation,Microsoft.PowerShell.Commands.NewPSSessionCommand"
    }
}

Describe "Basic Auth over HTTP not allowed on Unix" -Tag @("CI") {
    It "New-PSSession should throw when specifying Basic Auth over HTTP on Unix" -Skip:($IsWindows) {
        if ( -not (Get-WsManSupport)) {
            Set-ItResult -Skipped -Because "MI library not available for this platform"
            return
        }

        $password = ConvertTo-SecureString -String (GetRandomString) -AsPlainText -Force
        $credential = [PSCredential]::new('username', $password)

        $err = ({New-PSSession -ComputerName 'localhost' -Credential $credential -Authentication Basic}  | Should -Throw -PassThru  -ErrorId 'System.Management.Automation.Remoting.PSRemotingDataStructureException,Microsoft.PowerShell.Commands.NewPSSessionCommand')
        $err.Exception | Should -BeOfType System.Management.Automation.Remoting.PSRemotingTransportException
        # Should be PSRemotingErrorId.ConnectFailed
        # Ensures we are looking at the expected instance
        $err.Exception.ErrorCode | Should -Be 801
    }

    # Skip this test for macOS because the latest OS release is incompatible with our shipped libmi for WinRM/OMI.
    It "New-PSSession should NOT throw a ConnectFailed exception when specifying Basic Auth over HTTPS on Unix" -Skip:($IsWindows) {
        if ( -not (Get-WsManSupport)) {
            Set-ItResult -Skipped -Because "MI library not available for this platform"
            return
        }

        $password = ConvertTo-SecureString -String (GetRandomString) -AsPlainText -Force
        $credential = [PSCredential]::new('username', $password)

        # use a Uri that specifies HTTPS to test Basic Auth logic.
        # NOTE: The connection is expected to fail but not with a  ConnectFailed exception
        $uri = "https://localhost"
        New-PSSession -Uri $uri -Credential $credential -Authentication Basic -ErrorVariable err
        $err.Exception | Should -BeOfType System.Management.Automation.Remoting.PSRemotingTransportException
        $err.FullyQualifiedErrorId | Should -Be '1,PSSessionOpenFailed'
        $err.Exception.HResult | Should -Be 0x80131501
    }
}

Describe "JEA session Transcript script test" -Tag @("Feature", 'RequireAdminOnWindows') {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ( ! $IsWindows -or !(Test-CanWriteToPsHome))
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            Enable-PSRemoting -SkipNetworkProfileCheck
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Configuration name should be in the transcript header" {
        [string] $RoleCapDirectory = (New-Item -Path "$TestDrive\RoleCapability" -ItemType Directory -Force).FullName
        [string] $PSSessionConfigFile = "$RoleCapDirectory\TestConfig.pssc"
        [string] $transScriptFile = "$RoleCapDirectory\*.txt"
        try
        {
            New-PSSessionConfigurationFile -Path $PSSessionConfigFile -TranscriptDirectory $RoleCapDirectory -SessionType RestrictedRemoteServer
            Register-PSSessionConfiguration -Name JEA -Path $PSSessionConfigFile -Force -ErrorAction SilentlyContinue
            $scriptBlock = {Enter-RemoteSession -ComputerName Localhost -ConfigurationName JEA; Exit-PSSession}
            # Invoke the script block in a different PowerShell instance so that when TestDrive tries to delete $RoleCapDirectory,
            # the transcription has finished and the files are not locked.
            [powershell]::Create().AddScript($scriptBlock).Invoke()
            $headerFile = Get-ChildItem $transScriptFile | Sort-Object LastWriteTime | Select-Object -Last 1
            $header = Get-Content $headerFile | Out-String
            $header | Should -Match "Configuration Name: JEA"
        }
        finally
        {
            Unregister-PSSessionConfiguration -Name JEA -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "JEA session Get-Help test" -Tag @("CI", 'RequireAdminOnWindows') {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ( ! $IsWindows -or !(Test-CanWriteToPsHome))
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            Enable-PSRemoting -SkipNetworkProfileCheck
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "Get-Help should work in JEA sessions" {
        [string] $RoleCapDirectory = (New-Item -Path "$TestDrive\RoleCapability" -ItemType Directory -Force).FullName
        [string] $PSSessionConfigFile = "$RoleCapDirectory\TestConfig.pssc"
        try
        {
            New-PSSessionConfigurationFile -Path $PSSessionConfigFile -TranscriptDirectory $RoleCapDirectory -SessionType RestrictedRemoteServer
            Register-PSSessionConfiguration -Name JEA -Path $PSSessionConfigFile -Force -ErrorAction SilentlyContinue
            $scriptBlock = {Enter-RemoteSession -ComputerName Localhost -ConfigurationName JEA; Get-Help Get-Command; Exit-PSSession}
            # Invoke the script block in a different PowerShell instance so that when TestDrive tries to delete $RoleCapDirectory,
            # the transcription has finished and the files are not locked.
            $helpContent = [powershell]::Create().AddScript($scriptBlock).Invoke()
            $helpContent | Should -Not -BeNullOrEmpty
        }
        finally
        {
            Unregister-PSSessionConfiguration -Name JEA -Force -ErrorAction SilentlyContinue
            Remove-Item $RoleCapDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It "Get-Help should throw <ExpectedError> on <command>" -TestCases (Get-HelpNetworkTestCases) {
        param(
            $Command,
            $ExpectedError
        )

        [string] $RoleCapDirectory = (New-Item -Path "$TestDrive\RoleCapability" -ItemType Directory -Force).FullName
        [string] $PSSessionConfigFile = "$RoleCapDirectory\TestConfig.pssc"
        $configurationName = 'RestrictedWithNoGetHelpProxy'
        try
        {
            New-PSSessionConfigurationFile -Path $PSSessionConfigFile `
                -SessionType Empty `
                -LanguageMode NoLanguage `
                -ModulesToImport 'Microsoft.PowerShell.Utility', 'Microsoft.PowerShell.Core' `
                -VisibleCmdlets 'Get-command', 'measure-object', 'select-object', 'enter-pssession', 'get-formatdata', 'out-default', 'out-file', 'exit-pssession', 'get-help'
            Register-PSSessionConfiguration -Name $configurationName -Path $PSSessionConfigFile -Force -ErrorAction SilentlyContinue
            $scriptBlock = [scriptblock]::Create("Get-Help -Name $Command")
            {Invoke-Command -ConfigurationName $configurationName -ComputerName localhost -ScriptBlock $scriptBlock -ErrorAction Stop} |
                Should -Throw -ErrorId $ExpectedError
        }
        finally
        {
            Unregister-PSSessionConfiguration -Name $configurationName -Force -ErrorAction SilentlyContinue
            Remove-Item $RoleCapDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "Remoting loopback tests" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {

        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()

        if ( ! $IsWindows )
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            Enable-PSRemoting -SkipNetworkProfileCheck
            $endPoint = (Get-PSSessionConfiguration -Name "PowerShell.$(${PSVersionTable}.GitCommitId)").Name
            $disconnectedSession = New-RemoteSession -ConfigurationName $endPoint -ComputerName localhost | Disconnect-PSSession
            $closedSession = New-RemoteSession -ConfigurationName $endPoint -ComputerName localhost
            $closedSession.Runspace.Close()
            $openSession = New-RemoteSession -ConfigurationName $endPoint

            $ParameterError = @(
                @{
                    parameters    = @{
                        'InDisconnectedSession' = $true
                        'AsJob'                 = $true
                        'ScriptBlock'           = {1}
                        'ComputerName'          = 'localhost'
                        'ConfigurationName'     = $endpoint
                    }
                    expectedError = 'System.InvalidOperationException,Microsoft.PowerShell.Commands.InvokeCommandCommand'
                    title         = 'Cannot use InDisconnectedState and AsJob together'
                },
                @{
                    parameters    = @{
                        'ScriptBlock' = {1}
                        'SessionName' = 'SomeSessionName'
                    }
                    expectedError = 'System.InvalidOperationException,Microsoft.PowerShell.Commands.InvokeCommandCommand'
                    title         = 'Cannot use SessionName without InDisconnectedSession'
                },
                @{
                    parameters    = @{
                        'ScriptBlock' = { 1 }
                        'Session'     = $disconnectedSession
                        'ErrorAction' = 'Stop'
                    }
                    expectedError = 'InvokeCommandCommandInvalidSessionState,Microsoft.PowerShell.Commands.InvokeCommandCommand'
                    title         = 'Cannot use Invoke-Command on a disconnected session'
                }
                @{
                    parameters    = @{
                        'ScriptBlock' = { 1 }
                        'Session'     = $closedSession
                        'ErrorAction' = 'Stop'
                    }
                    expectedError = 'InvokeCommandCommandInvalidSessionState,Microsoft.PowerShell.Commands.InvokeCommandCommand'
                    title         = 'Cannot use Invoke-Command on a closed session'
                }
            )

            function script:ValidateSessionInfo($session, $state)
            {
                $session.ComputerName | Should -BeExactly 'localhost'
                $session.ConfigurationName | Should -BeExactly $endPoint
                $session.State | Should -Be $state
            }
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues

        if($IsWindows)
        {
            Remove-PSSession $disconnectedSession,$closedSession,$openSession -ErrorAction SilentlyContinue
        }
    }

    It 'Can connect to default endpoint' {
        $session = New-RemoteSession -ConfigurationName $endPoint

        try
        {
            ValidateSessionInfo -session $session -state 'Opened'
        }
        finally
        {
            $session | Remove-PSSession -ErrorAction SilentlyContinue
        }
    }

    It 'Can execute command in a disconnected session' {
        $session = Invoke-RemoteCommand -InDisconnectedSession -ComputerName 'localhost' -ScriptBlock { 1 + 1 } -ConfigurationName $endPoint
        try
        {
            ValidateSessionInfo -session $session -state 'Disconnected'

            $result = Receive-PSSession -Session $session
            $result | Should -Be 2
            $result.PSComputerName | Should -BeExactly 'localhost'
        }
        finally
        {
            $session | Remove-PSSession -ErrorAction SilentlyContinue
        }
    }

    It 'Can disconnect and connect to PSSession' {
        $session = New-RemoteSession -ConfigurationName $endPoint
        try
        {
            ValidateSessionInfo -session $session -state 'Opened'
            Disconnect-PSSession -Session $session

            ValidateSessionInfo -session $session -state 'Disconnected'
            Connect-RemoteSession -Session $session

            $result = Invoke-Command -Session $session -ScriptBlock { 1 + 1 }
            $result | Should -Be 2
            $result.PSComputerName | Should -BeExactly 'localhost'
        }
        finally
        {
            $session | Remove-PSSession -ErrorAction SilentlyContinue
        }
    }

    It "<title>" -TestCases $ParameterError {
        param($parameters, $expectedError)

        { Invoke-Command @parameters } | Should -Throw -ErrorId $expectedError
    }

    It 'Can execute command if one of the sessions is available' {
        try
        {
            $result = Invoke-Command -Session $openSession,$disconnectedSession,$closedSession -ScriptBlock { 1+1 } -ErrorAction SilentlyContinue
        }
        catch
        {
            if($_.FullyQualifiedErrorId -ne 'InvokeCommandCommandInvalidSessionState,Microsoft.PowerShell.Commands.InvokeCommandCommand')
            {
                # We expect the error from $disconnectedSession and $closedSession. Hence, throw otherwise.
                throw $_
            }
        }

        $result.Count | Should -Be 1
        $result | Should -Be 2
    }

    It 'Can execute command without creating new scope' {
        Invoke-Command -NoNewScope -ScriptBlock { $sameScopeVariable = 'SetInCurrentScope' }
        $sameScopeVariable | Should -BeExactly 'SetInCurrentScope'
    }

    It 'Can execute command from a file' {
        $fileName = "$testdrive/remotingscript.ps1"
        '1 + 1' | Out-File $fileName
        $result = Invoke-Command -FilePath $fileName -Session $openSession
        $result | Should -Be 2
    }

    It 'Can invoke-command as job' {
        $result = Invoke-Command -ScriptBlock { 1 + 1 } -Session $openSession -AsJob | Receive-Job -AutoRemoveJob -Wait -ErrorAction SilentlyContinue
        $result | Should -Be 2
    }

    It 'Can connect to all disconnected sessions by name' {
        $connectionNames = @("DiscPSS$(Get-Random)", "DiscPSS$(Get-Random)")
        $connectionNames | ForEach-Object { $null = New-RemoteSession -ComputerName localhost -ConfigurationName $endpoint -Name $_ | Disconnect-PSSession}

        Connect-RemoteSession -ComputerName localhost -Name $connectionNames -ConfigurationName $endpoint
        $sessions = Get-PSSession -Name $connectionNames
        try
        {
            $sessions | ForEach-Object {
                ValidateSessionInfo -session $_ -state 'Opened'
            }
        }
        finally
        {
            $sessions | Remove-PSSession -ErrorAction SilentlyContinue
        }
    }

    It 'Can pass values through $using' {
        $number = 100
        $result = Invoke-Command -Session $openSession -ScriptBlock { $using:number }
        $result | Should -Be 100
    }

    It '$Host.Version should be PSVersion' {
        $session = New-RemoteSession -ConfigurationName $endPoint
        try {
            $result = Invoke-Command -Session $session -ScriptBlock { $Host.Version }
            Write-Verbose "host version: $result" -Verbose
            $result | Should -Be $PSVersionTable.PSVersion
        }
        finally {
            $session | Remove-PSSession -ErrorAction Ignore
        }
    }

    It 'Language Mode is FullLanguage by default' {
        $session = New-RemoteSession -ConfigurationName $endPoint
        try {
            $result = Invoke-Command -Session $session -ScriptBlock { $ExecutionContext.SessionState.LanguageMode }
            $result | Should -Be ([System.Management.Automation.PSLanguageMode]::FullLanguage)
        } finally {
            $session | Remove-PSSession -ErrorAction Ignore
        }
    }
}
