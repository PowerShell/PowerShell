# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties scalar configuration' -Tag 'CI' {
    $scalarDefaultCases = @(
        @{ SettingPath = 'Formatting.ErrorView'; LegacyName = 'ErrorView'; ExpectedValue = [System.Management.Automation.ErrorView]::ConciseView; ExpectedType = [System.Management.Automation.ErrorView]; CheckExpected = $true }
        @{ SettingPath = 'Formatting.EnumerationLimit'; LegacyName = 'FormatEnumerationLimit'; ExpectedValue = 4; ExpectedType = [int]; CheckExpected = $true }
        @{ SettingPath = 'Formatting.FieldSeparator'; LegacyName = 'OFS'; ExpectedValue = ' '; ExpectedType = [string]; CheckExpected = $true }
        @{ SettingPath = 'NativeCommand.Encoding.ToProcess'; LegacyName = 'OutputEncoding'; ExpectedValue = $null; ExpectedType = [System.Text.Encoding]; CheckExpected = $false }
        @{ SettingPath = 'NativeCommand.Encoding.FromProcess'; LegacyName = 'PSApplicationOutputEncoding'; ExpectedValue = $null; ExpectedType = [System.Text.Encoding]; CheckExpected = $true }
        @{ SettingPath = 'NativeCommand.ArgumentPassing'; LegacyName = 'PSNativeCommandArgumentPassing'; ExpectedValue = $(if ($IsWindows) { [System.Management.Automation.NativeArgumentPassingStyle]::Windows } else { [System.Management.Automation.NativeArgumentPassingStyle]::Standard }); ExpectedType = [System.Management.Automation.NativeArgumentPassingStyle]; CheckExpected = $true }
        @{ SettingPath = 'NativeCommand.UseErrorActionPreference'; LegacyName = 'PSNativeCommandUseErrorActionPreference'; ExpectedValue = $false; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Module.AutoLoadingPreference'; LegacyName = 'PSModuleAutoLoadingPreference'; ExpectedValue = [System.Management.Automation.PSModuleAutoLoadingPreference]::All; ExpectedType = [System.Management.Automation.PSModuleAutoLoadingPreference]; CheckExpected = $true }
        @{ SettingPath = 'History.MaximumCount'; LegacyName = 'MaximumHistoryCount'; ExpectedValue = 4096; ExpectedType = [int]; CheckExpected = $true }
        @{ SettingPath = 'Transcription.Path'; LegacyName = 'TRANSCRIPT'; ExpectedValue = $null; ExpectedType = [string]; CheckExpected = $true }
        @{ SettingPath = 'Mail.Server'; LegacyName = 'PSEmailServer'; ExpectedValue = ''; ExpectedType = [string]; CheckExpected = $true }
        @{ SettingPath = 'Help.VerboseErrors'; LegacyName = 'VerboseHelpErrors'; ExpectedValue = $false; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.EngineHealthEvent'; LegacyName = 'LogEngineHealthEvent'; ExpectedValue = $true; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.EngineLifecycleEvent'; LegacyName = 'LogEngineLifecycleEvent'; ExpectedValue = $true; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.CommandHealthEvent'; LegacyName = 'LogCommandHealthEvent'; ExpectedValue = $false; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.CommandLifecycleEvent'; LegacyName = 'LogCommandLifecycleEvent'; ExpectedValue = $false; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.ProviderHealthEvent'; LegacyName = 'LogProviderHealthEvent'; ExpectedValue = $true; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.ProviderLifecycleEvent'; LegacyName = 'LogProviderLifecycleEvent'; ExpectedValue = $true; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.SettingsEvent'; LegacyName = 'LogSettingsEvent'; ExpectedValue = $true; ExpectedType = [bool]; CheckExpected = $true }
        @{ SettingPath = 'Logging.UserData'; LegacyName = 'PSLogUserData'; ExpectedValue = $null; ExpectedType = [object]; CheckExpected = $true }
        @{ SettingPath = 'Remoting.ConfigurationName'; LegacyName = 'PSSessionConfigurationName'; ExpectedValue = 'http://schemas.microsoft.com/powershell/Microsoft.PowerShell'; ExpectedType = [string]; CheckExpected = $true }
        @{ SettingPath = 'Remoting.ApplicationName'; LegacyName = 'PSSessionApplicationName'; ExpectedValue = 'wsman'; ExpectedType = [string]; CheckExpected = $true }
    )

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSScopedProperties')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'uses the expected default for <SettingPath> without materializing a legacy binding' -TestCases $scalarDefaultCases {
        param($SettingPath, $LegacyName, $ExpectedValue, $ExpectedType, $CheckExpected)

        $environmentValue = [Environment]::GetEnvironmentVariable('PSModuleAutoLoadingPreference')
        [Environment]::SetEnvironmentVariable('PSModuleAutoLoadingPreference', $null)
        $shell = [PowerShell]::Create()
        try {
            $snapshots = $shell.AddScript({
                param($path, $variableName)
                $before = $null -ne (Get-Variable -Name $variableName -Scope Global -ErrorAction Ignore)
                $value = $PS
                foreach ($segment in $path.Split('.')) { $value = $value.$segment }
                $variable = Get-Variable -Name $variableName -Scope Global -ErrorAction Ignore
                [pscustomobject]@{
                    Value = $value
                    Type = if ($null -eq $value) { $null } else { $value.GetType() }
                    BeforePresent = $before
                    AfterPresent = $null -ne $variable
                    LegacyValue = if ($null -ne $variable) { $variable.Value } else { $null }
                }
            }).AddArgument($SettingPath).AddArgument($LegacyName).Invoke()
            $snapshots.Count | Should -Be 1
            $snapshot = $snapshots[0]
            $shell.Streams.Error.Count | Should -Be 0
        }
        finally {
            $shell.Dispose()
            [Environment]::SetEnvironmentVariable('PSModuleAutoLoadingPreference', $environmentValue)
        }

        if ($null -ne $snapshot.Value) { ($snapshot.Value -is $ExpectedType) | Should -BeTrue }
        $snapshot.AfterPresent | Should -Be $snapshot.BeforePresent -Because "reading $SettingPath must not materialize $LegacyName"
        if ($CheckExpected) {
            if ($null -eq $ExpectedValue) { $snapshot.Value | Should -BeNullOrEmpty }
            else { $snapshot.Value | Should -Be $ExpectedValue }
        }
        if ($snapshot.AfterPresent) { $snapshot.LegacyValue | Should -Be $snapshot.Value }
    }

    It 'keeps concise error formatting after removing the ErrorView legacy variable' {
        $shell = [PowerShell]::Create()
        try {
            $results = $shell.AddScript({
                $ErrorView = [System.Management.Automation.ErrorView]::ConciseView
                $errorRecord = try { Write-Error 'stable scoped ErrorView marker' -ErrorAction Stop } catch { $_ }
                $renderError = { param($record) $record | Out-String }
                $baseline = & $renderError $errorRecord
                Remove-Variable -Name ErrorView -Scope Global -Force
                $bindingAbsentBeforeRead = $null -eq (Get-Variable -Name ErrorView -Scope Global -ErrorAction Ignore)
                $effectiveView = $PS.Formatting.ErrorView
                $actual = & $renderError $errorRecord
                [pscustomobject]@{
                    EffectiveView = $effectiveView
                    BeforeReadMissing = $bindingAbsentBeforeRead
                    AfterReadMissing = $null -eq (Get-Variable -Name ErrorView -Scope Global -ErrorAction Ignore)
                    Baseline = $baseline
                    Actual = $actual
                }
            }).Invoke()
            $shell.Streams.Error.Count | Should -Be 0
            $results.Count | Should -Be 1
            $results[0].EffectiveView | Should -Be ([System.Management.Automation.ErrorView]::ConciseView)
            $results[0].BeforeReadMissing | Should -BeTrue
            $results[0].AfterReadMissing | Should -BeTrue
            $results[0].Actual | Should -BeExactly $results[0].Baseline
        }
        finally {
            $shell.Dispose()
        }
    }

    It 'shares scalar values in both directions and keeps failed conversions atomic' {
        $PS.Formatting.ErrorView = 'NormalView'
        $ErrorView | Should -Be ([System.Management.Automation.ErrorView]::NormalView)
        $ErrorView = 'DetailedView'
        $PS.Formatting.ErrorView | Should -Be ([System.Management.Automation.ErrorView]::DetailedView)
        { $PS.Formatting.ErrorView = 'not an ErrorView' } | Should -Throw
        $PS.Formatting.ErrorView | Should -Be ([System.Management.Automation.ErrorView]::DetailedView)

        $PS.Formatting.EnumerationLimit = 2
        $FormatEnumerationLimit | Should -Be 2
        $FormatEnumerationLimit = 3
        $PS.Formatting.EnumerationLimit | Should -Be 3
        { $PS.Formatting.EnumerationLimit = 'not an integer' } | Should -Throw
        $PS.Formatting.EnumerationLimit | Should -Be 3

        $PS.Formatting.FieldSeparator = '::'
        $OFS | Should -BeExactly '::'
        $OFS = 17
        $PS.Formatting.FieldSeparator | Should -Be 17

        $PS.NativeCommand.Encoding.ToProcess = [System.Text.Encoding]::Unicode
        $OutputEncoding | Should -Be ([System.Text.Encoding]::Unicode)
        $OutputEncoding = [System.Text.Encoding]::UTF8
        $PS.NativeCommand.Encoding.ToProcess | Should -Be ([System.Text.Encoding]::UTF8)
        { $PS.NativeCommand.Encoding.ToProcess = 'not an encoding' } | Should -Throw
        $PS.NativeCommand.Encoding.ToProcess | Should -Be ([System.Text.Encoding]::UTF8)

        $PS.NativeCommand.Encoding.FromProcess = [System.Text.Encoding]::Unicode
        $PSApplicationOutputEncoding | Should -Be ([System.Text.Encoding]::Unicode)
        $PSApplicationOutputEncoding = [System.Text.Encoding]::UTF8
        $PS.NativeCommand.Encoding.FromProcess | Should -Be ([System.Text.Encoding]::UTF8)
        { $PS.NativeCommand.Encoding.FromProcess = 'not an encoding' } | Should -Throw
        $PS.NativeCommand.Encoding.FromProcess | Should -Be ([System.Text.Encoding]::UTF8)
        $PS.NativeCommand.Encoding.FromProcess = $null
        $PS.NativeCommand.Encoding.FromProcess | Should -BeNullOrEmpty
        $PSApplicationOutputEncoding | Should -BeNullOrEmpty

        $PS.NativeCommand.ArgumentPassing = 'Legacy'
        $PSNativeCommandArgumentPassing | Should -Be ([System.Management.Automation.NativeArgumentPassingStyle]::Legacy)
        { $PS.NativeCommand.ArgumentPassing = 'invalid style' } | Should -Throw
        $PS.NativeCommand.ArgumentPassing | Should -Be ([System.Management.Automation.NativeArgumentPassingStyle]::Legacy)
        $PS.NativeCommand.UseErrorActionPreference = $true
        $PSNativeCommandUseErrorActionPreference | Should -BeTrue
        $PSNativeCommandUseErrorActionPreference = $false
        $PS.NativeCommand.UseErrorActionPreference | Should -BeFalse

        $PS.Module.AutoLoadingPreference = 'None'
        $PSModuleAutoLoadingPreference | Should -Be ([System.Management.Automation.PSModuleAutoLoadingPreference]::None)
        $PSModuleAutoLoadingPreference = 'All'
        $PS.Module.AutoLoadingPreference | Should -Be ([System.Management.Automation.PSModuleAutoLoadingPreference]::All)
        { $PS.Module.AutoLoadingPreference = 'invalid preference' } | Should -Throw
        $PS.Module.AutoLoadingPreference | Should -Be ([System.Management.Automation.PSModuleAutoLoadingPreference]::All)
        $PS.Module.AutoLoadingPreference = $null
        $PS.Module.AutoLoadingPreference | Should -BeNullOrEmpty
        $PSModuleAutoLoadingPreference | Should -BeNullOrEmpty

        $PS.History.MaximumCount = 10
        $MaximumHistoryCount | Should -Be 10
        $MaximumHistoryCount = 11
        $PS.History.MaximumCount | Should -Be 11
        { $PS.History.MaximumCount = 0 } | Should -Throw
        $PS.History.MaximumCount | Should -Be 11

        $PS.Transcription.Path = 'transcript.txt'
        $TRANSCRIPT | Should -BeExactly 'transcript.txt'
        $TRANSCRIPT = 'legacy-transcript.txt'
        $PS.Transcription.Path | Should -BeExactly 'legacy-transcript.txt'
        $PS.Mail.Server = 'smtp.example.test'
        $PSEmailServer | Should -BeExactly 'smtp.example.test'
        $PSEmailServer = 'smtp2.example.test'
        $PS.Mail.Server | Should -BeExactly 'smtp2.example.test'
        $PS.Help.VerboseErrors = $true
        $VerboseHelpErrors | Should -BeTrue
        $VerboseHelpErrors = $false
        $PS.Help.VerboseErrors | Should -BeFalse

        $PS.Logging.EngineHealthEvent = $false
        $PS.Logging.EngineLifecycleEvent = $false
        $PS.Logging.CommandHealthEvent = $false
        $PS.Logging.CommandLifecycleEvent = $false
        $PS.Logging.ProviderHealthEvent = $false
        $PS.Logging.ProviderLifecycleEvent = $false
        $PS.Logging.SettingsEvent = $false
        $LogEngineHealthEvent | Should -BeFalse
        $LogEngineLifecycleEvent | Should -BeFalse
        $LogCommandHealthEvent | Should -BeFalse
        $LogCommandLifecycleEvent | Should -BeFalse
        $LogProviderHealthEvent | Should -BeFalse
        $LogProviderLifecycleEvent | Should -BeFalse
        $LogSettingsEvent | Should -BeFalse
        $LogEngineHealthEvent = $true
        $PS.Logging.EngineHealthEvent | Should -BeTrue

        $PS.Logging.EngineHealthEvent = $null
        $PS.Logging.EngineLifecycleEvent = $null
        $PS.Logging.CommandHealthEvent = $null
        $PS.Logging.CommandLifecycleEvent = $null
        $PS.Logging.ProviderHealthEvent = $null
        $PS.Logging.ProviderLifecycleEvent = $null
        $PS.Logging.SettingsEvent = $null
        $PS.Logging.EngineHealthEvent | Should -BeNullOrEmpty
        $PS.Logging.EngineLifecycleEvent | Should -BeNullOrEmpty
        $PS.Logging.CommandHealthEvent | Should -BeNullOrEmpty
        $PS.Logging.CommandLifecycleEvent | Should -BeNullOrEmpty
        $PS.Logging.ProviderHealthEvent | Should -BeNullOrEmpty
        $PS.Logging.ProviderLifecycleEvent | Should -BeNullOrEmpty
        $PS.Logging.SettingsEvent | Should -BeNullOrEmpty
        $LogEngineHealthEvent | Should -BeNullOrEmpty
        $LogEngineLifecycleEvent | Should -BeNullOrEmpty
        $LogCommandHealthEvent | Should -BeNullOrEmpty
        $LogCommandLifecycleEvent | Should -BeNullOrEmpty
        $LogProviderHealthEvent | Should -BeNullOrEmpty
        $LogProviderLifecycleEvent | Should -BeNullOrEmpty
        $LogSettingsEvent | Should -BeNullOrEmpty

        $userData = [pscustomobject]@{ Marker = 'reference' }
        $PS.Logging.UserData = $userData
        [object]::ReferenceEquals($PSLogUserData, $userData) | Should -BeTrue
        $PSLogUserData = $null
        $PS.Logging.UserData | Should -BeNullOrEmpty

        $PS.Remoting.ConfigurationName = 'TestConfiguration'
        $PSSessionConfigurationName | Should -BeExactly 'TestConfiguration'
        $PSSessionConfigurationName = 'OtherConfiguration'
        $PS.Remoting.ConfigurationName | Should -BeExactly 'OtherConfiguration'
        $PS.Remoting.ApplicationName = 'TestApplication'
        $PSSessionApplicationName | Should -BeExactly 'TestApplication'
        $PSSessionApplicationName = 'OtherApplication'
        $PS.Remoting.ApplicationName | Should -BeExactly 'OtherApplication'
        $PS.Remoting.ConfigurationName = $null
        $PS.Remoting.ConfigurationName | Should -BeNullOrEmpty
        $PSSessionConfigurationName | Should -BeNullOrEmpty
        $PS.Remoting.ApplicationName = $null
        $PS.Remoting.ApplicationName | Should -BeNullOrEmpty
        $PSSessionApplicationName | Should -BeNullOrEmpty
    }

    It 'inherits and resets scalar overrides through local, script, and global views' {
        $globalView = $PS.AtScope('Global')
        $scriptView = $null
        $originalGlobalLimit = Get-Variable -Name FormatEnumerationLimit -Scope Global -ErrorAction Ignore
        $originalGlobalLimitValue = if ($null -ne $originalGlobalLimit) { $originalGlobalLimit.Value } else { $null }
        try {
            $globalView.Formatting.EnumerationLimit = 7
            $scriptView = $PS.AtScope('Script')
            $scriptView.Formatting.EnumerationLimit = 5
            & {
                $localView = $PS.AtScope('Local')
                $localView.Formatting.EnumerationLimit = 2
                $localView.Formatting.EnumerationLimit | Should -Be 2
                $localView.Reset('Formatting.EnumerationLimit')
                $localView.Formatting.EnumerationLimit | Should -Be 5
                $scriptView.Reset('Formatting.EnumerationLimit')
                $localView.Formatting.EnumerationLimit | Should -Be 7
                $globalView.Reset('Formatting.EnumerationLimit')
                $localView.Formatting.EnumerationLimit | Should -Be 4
            }
        }
        finally {
            $globalView.Reset('Formatting.EnumerationLimit')
            if ($null -ne $scriptView) { $scriptView.Reset('Formatting.EnumerationLimit') }
            if ($null -ne $originalGlobalLimit) {
                $global:FormatEnumerationLimit = $originalGlobalLimitValue
            }
            else {
                Remove-Variable -Name FormatEnumerationLimit -Scope Global -Force -ErrorAction Ignore
            }
        }

        $PS.NativeCommand.Encoding.ToProcess = [System.Text.Encoding]::ASCII
        $captured = $PS.NativeCommand.Encoding
        & {
            $captured.ToProcess = [System.Text.Encoding]::Unicode
            $PS.NativeCommand.Encoding.ToProcess | Should -Be ([System.Text.Encoding]::Unicode)
        }
        $PS.NativeCommand.Encoding.ToProcess | Should -Be ([System.Text.Encoding]::ASCII)
        $PS.Reset('NativeCommand.Encoding.ToProcess')
    }

    It 'uses FieldSeparator and EnumerationLimit in actual formatting' {
        $joined = & {
            $PS.Formatting.FieldSeparator = '::'
            $items = 1, 2
            "$items"
        }
        $joined | Should -BeExactly '1::2'

        $rendered = & {
            $inheritedLimit = $PS.Formatting.EnumerationLimit
            $PS.Formatting.EnumerationLimit = 1
            $output = Format-Table -InputObject @{ test = 1, 2 } | Out-String
            [pscustomobject]@{ Text = $output; InheritedLimit = $inheritedLimit }
        }
        $rendered.Text | Should -Match 'test\s+\{1(?:\.\.\.|…)\}'
        $PS.Formatting.EnumerationLimit | Should -Be $rendered.InheritedLimit

        $errorText = & {
            $PS.Formatting.ErrorView = 'NormalView'
            Write-Error 'scoped error view marker' -ErrorAction Continue 2>&1 | Out-String
        }
        $errorText | Should -Match 'CategoryInfo'
    }

    It 'uses independent non-ASCII native stdin and stdout encodings' {
        $pwshName = if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }
        $pwsh = Join-Path $PSHOME $pwshName
        $inputProbe = Join-Path $TestDrive 'ReadNativeInput.ps1'
        $outputProbe = Join-Path $TestDrive 'WriteNativeOutput.ps1'
        @'
$memory = [IO.MemoryStream]::new()
[Console]::OpenStandardInput().CopyTo($memory)
[Console]::Write([Convert]::ToHexString($memory.ToArray()))
'@ | Set-Content -LiteralPath $inputProbe
        @'
$bytes = [Text.Encoding]::Unicode.GetBytes('café')
[Console]::OpenStandardOutput().Write($bytes, 0, $bytes.Length)
$errorBytes = [Text.Encoding]::Unicode.GetBytes('naïve')
[Console]::OpenStandardError().Write($errorBytes, 0, $errorBytes.Length)
'@ | Set-Content -LiteralPath $outputProbe

        $text = 'café'
        $noBomUnicode = [Text.UnicodeEncoding]::new($false, $false)
        $expectedPrefix = [Convert]::ToHexString($noBomUnicode.GetBytes($text))
        $stdinHex = & {
            $PS.NativeCommand.Encoding.ToProcess = $noBomUnicode
            $text | & $pwsh -NoLogo -NoProfile -File $inputProbe
        } | Out-String
        $stdinHex.Trim().StartsWith($expectedPrefix, [StringComparison]::Ordinal) | Should -BeTrue

        $combinedOutput = & {
            $PS.NativeCommand.Encoding.ToProcess = [Text.Encoding]::ASCII
            $PS.NativeCommand.Encoding.FromProcess = [Text.Encoding]::Unicode
            & $pwsh -NoLogo -NoProfile -File $outputProbe 2>&1
        }
        $stdoutText = [string]::Join('', @($combinedOutput | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }))
        $stderrText = [string]::Join('', @($combinedOutput | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] } | ForEach-Object { $_.Exception.Message }))
        $stdoutText | Should -BeExactly $text
        $stderrText | Should -BeExactly 'naïve'
    }

    It 'uses a scoped Transcription.Path as the Start-Transcript default' {
        $transcriptPath = Join-Path $TestDrive 'scoped-transcript.txt'
        & {
            $PS.Transcription.Path = $transcriptPath
            Start-Transcript | Out-Null
            Write-Host 'scoped transcription marker'
            Stop-Transcript | Out-Null
        }
        Test-Path -LiteralPath $transcriptPath | Should -BeTrue
        Get-Content -LiteralPath $transcriptPath -Raw | Should -Match 'scoped transcription marker'

        $generatedDirectory = Join-Path $TestDrive 'generated-transcript'
        New-Item -ItemType Directory -Path $generatedDirectory -Force | Out-Null
        & {
            $PS.Transcription.Path = $null
            $PS.Transcription.Path | Should -BeNullOrEmpty
            $TRANSCRIPT | Should -BeNullOrEmpty
            Start-Transcript -OutputDirectory $generatedDirectory | Out-Null
            Write-Host 'generated transcription marker'
            Stop-Transcript | Out-Null
        }
        $generatedTranscript = Get-ChildItem -LiteralPath $generatedDirectory -Filter 'PowerShell_transcript*' | Select-Object -First 1
        $generatedTranscript | Should -Not -BeNullOrEmpty
        Get-Content -LiteralPath $generatedTranscript.FullName -Raw | Should -Match 'generated transcription marker'
    }

    It 'uses AutoLoadingPreference overrides for real module discovery' {
        $moduleName = 'ScopedPropertiesAutoLoad' + [guid]::NewGuid().ToString('N')
        $moduleRoot = Join-Path $TestDrive $moduleName
        $moduleDirectory = Join-Path $moduleRoot $moduleName
        New-Item -ItemType Directory -Path $moduleDirectory -Force | Out-Null
        @'
function Get-ScopedPropertiesAutoLoadProbe { 'module loaded' }
Export-ModuleMember -Function Get-ScopedPropertiesAutoLoadProbe
'@ | Set-Content -LiteralPath (Join-Path $moduleDirectory ($moduleName + '.psm1'))

        $originalModulePath = $env:PSModulePath
        $originalAutoLoadingPreference = $env:PSModuleAutoLoadingPreference
        try {
            $env:PSModulePath = $moduleRoot + [IO.Path]::PathSeparator + $originalModulePath
            & {
                $PS.Module.AutoLoadingPreference = $null
                $PS.Module.AutoLoadingPreference | Should -BeNullOrEmpty
                $env:PSModuleAutoLoadingPreference = 'None'
                { Get-ScopedPropertiesAutoLoadProbe } | Should -Throw
                $env:PSModuleAutoLoadingPreference = 'All'
                Get-ScopedPropertiesAutoLoadProbe | Should -BeExactly 'module loaded'
                $PS.Module.AutoLoadingPreference | Should -BeNullOrEmpty
            }
        }
        finally {
            $env:PSModulePath = $originalModulePath
            $env:PSModuleAutoLoadingPreference = $originalAutoLoadingPreference
            Remove-Module -Name $moduleName -Force -ErrorAction Ignore
        }
    }
}
