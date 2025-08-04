# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Group policy settings tests' -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('BypassGroupPolicyCaching', $true)
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        if ( $IsWindows ) {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('BypassGroupPolicyCaching', $false)
        }
    }

    Context 'Group policy settings tests' {

        BeforeEach {
            $KeyRoot = 'HKCU:\Software\Policies\Microsoft\PowerShellCore'
            if (-not (Test-Path $KeyRoot)) {$null = New-Item $KeyRoot}

            $WinPSKeyRoot = 'HKCU:\Software\Policies\Microsoft\Windows\PowerShell'
            if (-not (Test-Path $WinPSKeyRoot)) {$null = New-Item $WinPSKeyRoot}
        }

        AfterEach {
            Remove-Item $KeyRoot -Recurse -Force > $null
            Remove-Item $WinPSKeyRoot -Recurse -Force > $null
        }

        It 'Execution policy test' {
            function TestFeature
            {
                param([string]$KeyPath)

                Set-ItemProperty -Path $KeyPath -Name EnableScripts -Value 1 -Force

                Set-ItemProperty -Path $KeyPath -Name ExecutionPolicy -Value 'Unrestricted' -Force
                (Get-ExecutionPolicy) | Should -Be 'Unrestricted'
                Set-ItemProperty -Path $KeyPath -Name ExecutionPolicy -Value 'AllSigned' -Force
                (Get-ExecutionPolicy) | Should -Be 'AllSigned'
                Set-ItemProperty -Path $KeyPath -Name ExecutionPolicy -Value 'RemoteSigned' -Force
                (Get-ExecutionPolicy) | Should -Be 'RemoteSigned'

                Remove-ItemProperty -Path $KeyPath -Name ExecutionPolicy -Force
            }

            TestFeature -KeyPath $KeyRoot

            Set-ItemProperty -Path $KeyRoot -Name UseWindowsPowerShellPolicySetting -Value 1 -Force
            TestFeature -KeyPath $WinPSKeyRoot
        }

        It 'Module logging policy test' {
            if (Test-IsWindowsArm64) {
                Set-ItResult -Pending -Because "There is no PowerShellCore event provider on ARM64 until we have an MSI"
            }

            function TestFeature
            {
                param([string]$KeyPath)

                $ModuleToLog = 'Microsoft.PowerShell.Utility'
                $ModuleNamesKeyPath = Join-Path $KeyPath 'ModuleNames'
                if (-not (Test-Path $ModuleNamesKeyPath)) {$null = New-Item $ModuleNamesKeyPath}

                Remove-Module $ModuleToLog -ErrorAction SilentlyContinue
                Import-Module $ModuleToLog
                (Get-Module $ModuleToLog).LogPipelineExecutionDetails | Should -BeFalse # without GP logging for the module should be OFF

                # enable GP
                [string]$RareCommand = Get-Random
                Set-ItemProperty -Path $KeyPath -Name EnableModuleLogging -Value 1 -Force
                Set-ItemProperty -Path $ModuleNamesKeyPath -Name $ModuleToLog -Value $ModuleToLog -Force

                Remove-Module $ModuleToLog -ErrorAction SilentlyContinue
                Import-Module $ModuleToLog # this will read and start using GP setting
                (Get-Module $ModuleToLog).LogPipelineExecutionDetails | Should -BeTrue # with GP logging for the module should be ON

                Get-Alias $RareCommand -ErrorAction SilentlyContinue | Out-Null

                (Get-Module $ModuleToLog).LogPipelineExecutionDetails = $false # turn off logging
                Remove-ItemProperty -Path $KeyPath -Name EnableModuleLogging -Force # turn off GP setting
                Remove-Item $ModuleNamesKeyPath -Recurse -Force
                # usually event becomes visible in the log after ~500 ms
                # set timeout for 5 seconds
                Wait-UntilTrue -sb { Get-WinEvent -FilterHashtable @{ ProviderName="PowerShellCore"; Id = 4103 } -MaxEvents 5 |
                    Where-Object {$_.Message.Contains($RareCommand)} } -TimeoutInMilliseconds (10*1000) -IntervalInMilliseconds 100 |
                        Should -BeTrue
            }

            $KeyPath = Join-Path $KeyRoot 'ModuleLogging'
            if (-not (Test-Path $KeyPath)) {$null = New-Item $KeyPath}

            TestFeature -KeyPath $KeyPath

            Set-ItemProperty -Path $KeyPath -Name UseWindowsPowerShellPolicySetting -Value 1 -Force
            $WinKeyPath = Join-Path $WinPSKeyRoot 'ModuleLogging'
            if (-not (Test-Path $WinKeyPath)) {$null = New-Item $WinKeyPath}

            TestFeature -KeyPath $WinKeyPath
        }

        It 'ScriptBlock logging policy test' {
            if (Test-IsWindowsArm64) {
                Set-ItResult -Pending -Because "There is no PowerShellCore event provider on ARM64 until we have an MSI"
            }

            function TestFeature
            {
                param([string]$KeyPath)

                [string]$RareCommand = Get-Random
                Set-ItemProperty -Path $KeyPath -Name EnableScriptBlockLogging -Value 1 -Force
                Set-ItemProperty -Path $KeyPath -Name EnableScriptBlockInvocationLogging -Value 1 -Force
                Invoke-Expression "$RareCommand | Out-Null"
                Remove-ItemProperty -Path $KeyPath -Name EnableScriptBlockLogging -Force
                Remove-ItemProperty -Path $KeyPath -Name EnableScriptBlockInvocationLogging -Force
                # usually event becomes visible in the log after ~500 ms
                # set timeout for 5 seconds
                Wait-UntilTrue -sb { $script:CreatingScriptblockEvent = Get-WinEvent -FilterHashtable @{ ProviderName="PowerShellCore"; Id = 4104 } -MaxEvents 5 | ? {$_.Message.Contains($RareCommand)}; $script:CreatingScriptblockEvent } -TimeoutInMilliseconds (5*1000) -IntervalInMilliseconds 100 | Should -BeTrue

                $sbStringStart = $script:CreatingScriptblockEvent.Message.IndexOf('ScriptBlock ID:')
                $sbStringEnd = $script:CreatingScriptblockEvent.Message.IndexOf(0x0D, $sbStringStart)
                $sbString = $script:CreatingScriptblockEvent.Message.Substring($sbStringStart, $sbStringEnd - $sbStringStart)

                $StartedScriptBlockInvocationEvent = Get-WinEvent -FilterHashtable @{ ProviderName="PowerShellCore"; Id = 4105 } -MaxEvents 5 | ? {$_.Message.Contains($sbString)}
                $StartedScriptBlockInvocationEvent | Should -Not -BeNullOrEmpty
                $CompletedScriptBlockInvocationEvent = Get-WinEvent -FilterHashtable @{ ProviderName="PowerShellCore"; Id = 4106 } -MaxEvents 5 | ? {$_.Message.Contains($sbString)}
                $CompletedScriptBlockInvocationEvent | Should -Not -BeNullOrEmpty
            }

            $KeyPath = Join-Path $KeyRoot 'ScriptBlockLogging'
            if (-not (Test-Path $KeyPath)) {$null = New-Item $KeyPath}

            TestFeature -KeyPath $KeyPath

            Set-ItemProperty -Path $KeyPath -Name UseWindowsPowerShellPolicySetting -Value 1 -Force
            $WinKeyPath = Join-Path $WinPSKeyRoot 'ScriptBlockLogging'
            if (-not (Test-Path $WinKeyPath)) {$null = New-Item $WinKeyPath}

            TestFeature -KeyPath $WinKeyPath
        }

        It 'Transcription policy test' {

            function TestFeature
            {
                param([string]$KeyPath)

                $OutputDirectory = Join-Path $([System.IO.Path]::GetTempPath()) $(Get-Random)
                $null = New-Item -Type Directory -Path $OutputDirectory -Force

                Set-ItemProperty -Path $KeyPath -Name EnableTranscripting -Value 1 -Force
                Set-ItemProperty -Path $KeyPath -Name OutputDirectory -Value $OutputDirectory -Force
                Set-ItemProperty -Path $KeyPath -Name EnableInvocationHeader -Value 1 -Force

                $number = Get-Random
                $null = & "$PSHOME/pwsh" -NoProfile -NonInteractive -c "$number"

                Remove-ItemProperty -Path $KeyPath -Name OutputDirectory -Force
                Remove-ItemProperty -Path $KeyPath -Name EnableInvocationHeader -Force

                $LogPath = (gci -Path $OutputDirectory -Filter "PowerShell_transcript*.txt" -Recurse).FullName
                $Log = Get-Content $LogPath -Raw

                $Log.Contains("$number") | Should -BeTrue # verifies that Transcription policy works
                $Log.Contains("Command start time:") | Should -BeTrue # verifies that EnableInvocationHeader works

                Remove-Item -Path $OutputDirectory -Recurse -Force
            }

            $KeyPath = Join-Path $KeyRoot 'Transcription'
            if (-not (Test-Path $KeyPath)) {$null = New-Item $KeyPath}

            TestFeature -KeyPath $KeyPath

            Set-ItemProperty -Path $KeyPath -Name UseWindowsPowerShellPolicySetting -Value 1 -Force
            $WinKeyPath = Join-Path $WinPSKeyRoot 'Transcription'
            if (-not (Test-Path $WinKeyPath)) {$null = New-Item $WinKeyPath}

            TestFeature -KeyPath $WinKeyPath
        }

        It 'Default SourcePath on Update-Help policy test' {
            function TestFeature
            {
                param([string]$KeyPath)

                $HelpPath = Join-Path 'TestDrive:\' $(Get-Random)
                $null = New-Item -Type Directory -Path $HelpPath -ErrorAction SilentlyContinue
                $ModuleName = 'Microsoft.PowerShell.Utility'
                Save-Help -Module $ModuleName -DestinationPath $HelpPath -Force

                Set-ItemProperty -Path $KeyPath -Name EnableUpdateHelpDefaultSourcePath -Value 1 -Force
                Set-ItemProperty -Path $KeyPath -Name DefaultSourcePath -Value $HelpPath -Force

                # this should throw error cause we didn't save the help for this module locally;
                # this ensures that Update-Help is not going to Internet to download help
                { Update-Help -Module Microsoft.PowerShell.Management -Force -ErrorAction Stop } | Should -Throw -ErrorId "UnableToRetrieveHelpInfoXml,Microsoft.PowerShell.Commands.UpdateHelpCommand"

                # this should use saved help in location specified in the policy and should NOT throw error
                Update-Help -Module Microsoft.PowerShell.Utility -Force
            }

            $HKLM_KeyRoot = 'HKLM:\Software\Policies\Microsoft\PowerShellCore'
            if (-not (Test-Path $HKLM_KeyRoot)) {$null = New-Item $HKLM_KeyRoot}
            $KeyPath = Join-Path $HKLM_KeyRoot 'UpdatableHelp'
            if (-not (Test-Path $KeyPath)) {$null = New-Item $KeyPath}

            TestFeature -KeyPath $KeyPath

            Set-ItemProperty -Path $KeyPath -Name UseWindowsPowerShellPolicySetting -Value 1 -Force
            $HKLM_WinPSKeyRoot = 'HKLM:\Software\Policies\Microsoft\Windows\PowerShell'
            if (-not (Test-Path $HKLM_WinPSKeyRoot)) {$null = New-Item $HKLM_WinPSKeyRoot}
            $WinKeyPath = Join-Path $HKLM_WinPSKeyRoot 'UpdatableHelp'
            if (-not (Test-Path $WinKeyPath)) {$null = New-Item $WinKeyPath}

            TestFeature -KeyPath $WinKeyPath

            Remove-Item $HKLM_KeyRoot -Recurse -Force
            Remove-Item $HKLM_WinPSKeyRoot -Recurse -Force
        }

        It 'Session configuration policy test' {
            function TestFeature
            {
                param([string]$KeyPath)

                # set policy to use unique non-existing configuration session name
                $SessionName = "TestSessionConfiguration-$(Get-Random)"
                Set-ItemProperty -Path $KeyPath -Name EnableConsoleSessionConfiguration -Value 1 -Force
                Set-ItemProperty -Path $KeyPath -Name ConsoleSessionConfigurationName -Value $SessionName -Force

                $LogPath = (New-TemporaryFile).FullName
                & "$PSHOME/pwsh" -NoProfile -NonInteractive -c "1" *> $LogPath # this implicitly uses SessionConfiguration from the policy

                # Log should have an error that has our configuration session name; e.g.:
                # 'The shell cannot be started. A failure occurred during initialization:
                # Cannot create or open the configuration session 116337267.'

                $Log = Get-Content $LogPath -Raw
                $Log.Contains("$SessionName") | Should -BeTrue
                Remove-Item -Path $LogPath -Force
            }

            $KeyPath = Join-Path $KeyRoot 'ConsoleSessionConfiguration'
            if (-not (Test-Path $KeyPath)) {$null = New-Item $KeyPath}

            TestFeature -KeyPath $KeyPath
        }
    }
}
