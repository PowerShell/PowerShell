# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties' -Tag 'CI' {
    BeforeAll {
        $enabledConfig = Join-Path $TestDrive 'enabled.json'
        $disabledConfig = Join-Path $TestDrive 'disabled.json'
        '{"ExperimentalFeatures":["PSScopedProperties"]}' | Set-Content -LiteralPath $enabledConfig
        '{"ExperimentalFeatures":[]}' | Set-Content -LiteralPath $disabledConfig
        $pwshName = if ($IsWindows) { 'pwsh.exe' } else { 'pwsh' }
        $pwsh = Join-Path $PSHOME $pwshName
        $hookSource = Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets/ScopedPropertiesConfigStartupHook.cs') -Destination $TestDrive -PassThru
        $hookAssembly = Join-Path $TestDrive 'ScopedPropertiesConfigStartupHook.dll'
        Add-Type -Path $hookSource.FullName -OutputAssembly $hookAssembly

        function Invoke-ScopedPropertiesTest {
            param([scriptblock] $Script, [switch] $Disabled)

            $config = if ($Disabled) { $disabledConfig } else { $enabledConfig }
            $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($Script.ToString()))
            $originalHooks = $env:DOTNET_STARTUP_HOOKS
            try {
                $env:DOTNET_STARTUP_HOOKS = $hookAssembly
                $output = & $pwsh -NoLogo -NoProfile -NonInteractive -SettingsFile $config -OutputFormat Text -EncodedCommand $encodedCommand 2>&1
            } finally {
                $env:DOTNET_STARTUP_HOOKS = $originalHooks
            }
            $LASTEXITCODE | Should -Be 0 -Because ($output -join [Environment]::NewLine)
            @($output | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }) | Should -BeNullOrEmpty
            $output
        }
    }

    It 'leaves PS available as an ordinary variable when disabled' {
        $result = Invoke-ScopedPropertiesTest -Disabled {
            $null -eq (Get-Variable PS -ErrorAction Ignore)
            . ([scriptblock]::Create('$global:PS = "user value"; $global:PS'))
            $PSStyle.Progress.View = 'Classic'
            & {
                $PSStyle.Progress.View = 'Minimal'
            }
            $PSStyle.Progress.View
        }
        $result | Should -Be @('True', 'user value', 'Minimal')
    }

    It 'retains legacy error preferences and untyped child shadows when disabled' {
        $result = Invoke-ScopedPropertiesTest -Disabled {
            $EnabledExperimentalFeatures.Contains('PSScopedProperties')
            $ErrorActionPreference = 'sToP'
            "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference"
            try { $ErrorActionPreference = 'invalid preference' } catch { 'built-in rejected write' }
            $ErrorActionPreference
            & {
                $ErrorActionPreference = 'cOnTiNuE'
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference"
                $ErrorActionPreference = 'invalid preference'
                $ErrorActionPreference
                Write-Error 'expected' -ErrorAction Ignore
                'ignored'
            }
            function Invoke-DisabledAdvancedPreference {
                [CmdletBinding()]
                param()
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference"
                Write-Error 'expected' -ErrorAction Ignore
                'ignored'
            }
            Invoke-DisabledAdvancedPreference -ErrorAction SilentlyContinue
            $ErrorActionPreference
        }
        $result | Should -BeExactly @(
            'False', 'ActionPreference/Stop', 'built-in rejected write', 'Stop',
            'String/cOnTiNuE', 'invalid preference', 'ignored', 'ActionPreference/SilentlyContinue', 'ignored', 'Stop'
        )
    }

    It 'registers a constant AllScope facade with read-only grouping properties' {
        $result = Invoke-ScopedPropertiesTest {
            $EnabledExperimentalFeatures.Contains('PSScopedProperties')
            (Get-Variable PS).Options
            & { (Get-Variable PS -Scope Local).Options }
            foreach ($name in 'ActionPreference', 'Style') {
                $PS.GetType().GetProperty($name).CanWrite
            }
            $PS.Style.GetType().GetProperty('Progress').CanWrite
            $PS.ActionPreference.Error.GetType().Name
            $PS.Style.Progress.View.GetType().Name
            try { Set-Variable PS 'replacement' -Force -ErrorAction Stop } catch { $_.FullyQualifiedErrorId }
        }
        $result | Should -Be @(
            'True', 'Constant, AllScope', 'Constant, AllScope',
            'False', 'False', 'False', 'ActionPreference', 'ProgressView',
            'VariableNotWritable,Microsoft.PowerShell.Commands.SetVariableCommand'
        )
    }

    It 'resets a local override created through either error preference access path' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            & {
                $ErrorActionPreference = 'Stop'
                $PS.ActionPreference.Error

                $PS.ActionPreference.Error = 'SilentlyContinue'
                $ErrorActionPreference

                $PS.Reset('ActionPreference.Error')
                $PS.ActionPreference.Error
            }
            $PS.ActionPreference.Error
        }
        $result | Should -Be @('Stop', 'SilentlyContinue', 'Continue', 'Continue')
    }

    It 'inherits each leaf independently and resolves captured groups at each use' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            $PS.Style.Progress.View = 'Minimal'
            $preferences = $PS.ActionPreference
            $progress = $PS.Style.Progress
            & {
                $ErrorActionPreference = 'Stop'
                "$($preferences.Error)/$($progress.View)"
                $progress.View = 'Classic'
                & {
                    $preferences.Error = 'SilentlyContinue'
                    "$ErrorActionPreference/$($PSStyle.Progress.View)"
                    $PS.Reset('Style.Progress.View')
                    $PS.Reset('ActionPreference.Error')
                    "$($PS.ActionPreference.Error)/$($PSStyle.Progress.View)"
                }
                $PS.Reset('ActionPreference.Error')
                "$($PS.ActionPreference.Error)/$($PSStyle.Progress.View)"
                . { $PSStyle.Progress.View = 'Minimal' }
                $PS.Style.Progress.View
            }
            "$($PS.ActionPreference.Error)/$($PSStyle.Progress.View)"
        }
        $result | Should -Be @('Stop/Minimal', 'SilentlyContinue/Classic', 'Stop/Classic', 'Continue/Classic', 'Minimal', 'Continue/Minimal')
    }

    # Canonical cutover: child assignments will use the same conversion and validation as the built-in variable.
    It 'converts the built-in preference but permits untyped child-scope shadows' {
        $result = Invoke-ScopedPropertiesTest {
            $ErrorActionPreference = 'sToP'
            "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
            try { $ErrorActionPreference = 'invalid preference' } catch { 'built-in rejected write' }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            & {
                $ErrorActionPreference = 'sIlEnTlYcOnTiNuE'
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
                $ErrorActionPreference = 'invalid preference'
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/Stop', 'built-in rejected write', 'Stop/Stop',
            'String/sIlEnTlYcOnTiNuE/SilentlyContinue', 'invalid preference/Continue', 'Stop/Stop'
        )
    }

    # Canonical cutover: Set-Variable will normalize convertible values and preserve the previous value on failure.
    It 'shares legacy variable operations and preserves raw legacy values' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                foreach ($value in ('cOnTiNuE', 2, $null, 'invalid preference')) {
                    Set-Variable ErrorActionPreference $value -Scope Local
                    $legacyValue = (Get-Variable ErrorActionPreference -Scope Local).Value
                    $legacyType = if ($null -eq $legacyValue) { 'null' } else { $legacyValue.GetType().Name }
                    "$legacyType/$legacyValue/$($PS.ActionPreference.Error)"
                }

                $PS.ActionPreference.Error = 'SilentlyContinue'
                (Get-Variable ErrorActionPreference -Scope Local).Value.GetType().Name
                $PS.ActionPreference.Error
                Remove-Variable ErrorActionPreference -Scope Local
                $PS.ActionPreference.Error
                $local:ErrorActionPreference = 'Continue'
                "$($PS.ActionPreference.Error)/$global:ErrorActionPreference"
                $PS.Reset('ActionPreference.Error')
                $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
            }
        }
        $result | Should -BeExactly @(
            'String/cOnTiNuE/Continue', 'Int32/2/Continue', 'null//Continue', 'String/invalid preference/Continue',
            'ActionPreference', 'SilentlyContinue', 'Stop', 'Continue/Stop', 'True'
        )
    }

    It 'preserves variable protection and rejects invalid typed assignments' {
        $result = Invoke-ScopedPropertiesTest {
            foreach ($option in 'ReadOnly', 'Constant') {
                & {
                    New-Variable ErrorActionPreference -Value 'Continue' -Option $option
                    try { $PS.ActionPreference.Error = 'Stop' } catch { 'write rejected' }
                    try { $PS.Reset('ActionPreference.Error') } catch { 'reset rejected' }
                    "$((Get-Variable ErrorActionPreference).Options)/$($PS.ActionPreference.Error)"
                }
            }
            try { $PS.ActionPreference.Error = 'bad action' } catch { 'invalid action rejected' }
            try { $PS.Style.Progress.View = 'bad view' } catch { 'invalid view rejected' }
            try { $PS.ActionPreference.Error = 500 } catch { 'invalid action number rejected' }
            try { $PS.Style.Progress.View = 500 } catch { 'invalid view number rejected' }
        }
        $result | Should -Be @(
            'write rejected', 'reset rejected', 'ReadOnly/Continue',
            'write rejected', 'reset rejected', 'Constant/Continue',
            'invalid action rejected', 'invalid view rejected',
            'invalid action number rejected', 'invalid view number rejected'
        )
    }

    It 'uses existing preference validation for assignments of the reserved Suspend value' {
        $result = Invoke-ScopedPropertiesTest {
            try { $ErrorActionPreference = 'Suspend' } catch { $_.Exception.Message.Contains('Suspend') }
            try { $PS.ActionPreference.Error = 'Suspend' } catch { $_.Exception.Message.Contains('Suspend') }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -Be @('True', 'True', 'Continue/Continue')
    }

    It 'honors legacy validation attributes and private variable visibility' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                [ValidateSet('Continue', 'Stop')]$ErrorActionPreference = 'Continue'
                try { $PS.ActionPreference.Error = 'SilentlyContinue' } catch { 'validation rejected write' }
                $PS.ActionPreference.Error
                $PS.ActionPreference.Error = 'Stop'
                (Get-Variable ErrorActionPreference).Attributes[0].GetType().Name
                $ErrorActionPreference
            }
            & {
                New-Variable ErrorActionPreference -Value 'SilentlyContinue' -Option Private
                $PS.ActionPreference.Error
                & { $PS.ActionPreference.Error }
                $PS.ActionPreference.Error = 'Continue'
                (Get-Variable ErrorActionPreference).Options
                $PS.ActionPreference.Error
            }
        }
        $result | Should -Be @(
            'validation rejected write', 'Continue', 'ValidateSetAttribute', 'Stop',
            'SilentlyContinue', 'Stop', 'Private', 'Continue'
        )
    }

    # Canonical cutover: Reset will remove the implicit function preference and reveal the caller's preference.
    It 'observes advanced function preferences and command-level ErrorAction precedence' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-AdvancedPreference {
                [CmdletBinding()]
                param()
                "entry: $($PS.ActionPreference.Error)"
                try { $PS.Reset('ActionPreference.Error') } catch { 'implicit preference cannot be removed' }
                "after reset: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                Write-Error 'expected' -ErrorAction Ignore
                'ignored'
                $PS.ActionPreference.Error = 'SilentlyContinue'
                "assigned: $ErrorActionPreference"
                Write-Error 'expected'
                'continued with facade preference'
                try { Write-Error 'expected' -ErrorAction Stop } catch { 'command stopped' }
            }
            $PS.ActionPreference.Error = 'Continue'
            Invoke-AdvancedPreference -ErrorAction Stop
            "caller: $($PS.ActionPreference.Error)"
            $PS.ActionPreference.Error = 'Stop'
            try { Write-Error 'expected' } catch { 'facade stopped command' }
        }
        $result | Should -Be @(
            'entry: Stop', 'implicit preference cannot be removed', 'after reset: Stop/Stop', 'ignored',
            'assigned: SilentlyContinue', 'continued with facade preference',
            'command stopped', 'caller: Continue', 'facade stopped command'
        )
    }

    # Canonical cutover: dynamic Reset will also remove overrides when no debugger is attached.
    It 'honors variable removal rules when Reset is invoked dynamically' {
        $result = Invoke-ScopedPropertiesTest {
            function Test-PreferenceVariable {
                $ErrorActionPreference = 'Stop'
                $PS.ActionPreference.Error
                $PS.ActionPreference.Error = 'SilentlyContinue'
                $ErrorActionPreference
                $method = 'Reset'
                try {
                    $PS.$method('ActionPreference.Error')
                } catch {
                    $_.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId
                }
                $ErrorActionPreference
            }
            $PS.ActionPreference.Error = 'Continue'
            Test-PreferenceVariable
            $breakpoint = Set-PSBreakpoint -Variable ErrorActionPreference -Mode Write -Action {
                $global:preferenceBreakpointHit = $true
            }
            Test-PreferenceVariable
            Remove-PSBreakpoint $breakpoint
            $global:preferenceBreakpointHit
        }
        $result | Should -Be @(
            'Stop', 'SilentlyContinue', 'VariableNotRemovableRare', 'SilentlyContinue',
            'Stop', 'SilentlyContinue', 'Continue', 'True'
        )
    }

    It 'uses the actual script, local, and global scopes and expires local views' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            $scriptPath = Join-Path ([IO.Path]::GetTempPath()) ("ScopedProperties-{0}.ps1" -f [guid]::NewGuid())
            try {
                @'
$PS.ActionPreference.Error = 'Stop'
$scriptSettings = $PS.AtScope('Script')
& {
    $localSettings = $PS.AtScope('Local')
    $localSettings.ActionPreference.Error = 'SilentlyContinue'
    "$($localSettings.ActionPreference.Error)/$($scriptSettings.ActionPreference.Error)/$($PS.AtScope('Global').ActionPreference.Error)"
    $scriptSettings.Reset('ActionPreference.Error')
    $localSettings.Reset('ActionPreference.Error')
    $PS.ActionPreference.Error
}
'@ | Set-Content -LiteralPath $scriptPath
                & $scriptPath
            } finally {
                Remove-Item -LiteralPath $scriptPath -Force
            }
            $expiredView = & { $PS.AtScope('Local') }
            try { $expiredView.ActionPreference.Error } catch { 'expired read rejected' }
            try { $expiredView.ActionPreference.Error = 'Stop' } catch { 'expired assignment rejected' }
            try { $expiredView.Reset('ActionPreference.Error') } catch { 'expired reset rejected' }
            try { $PS.AtScope('Private') } catch { 'unsupported scope rejected' }
            try { $PS.Reset('Style.Progress.Unknown') } catch { 'unknown path rejected' }
        }
        $result | Should -Be @(
            'SilentlyContinue/Stop/Continue', 'Continue', 'expired read rejected',
            'expired assignment rejected', 'expired reset rejected', 'unsupported scope rejected', 'unknown path rejected'
        )
    }

    It 'resets progress overrides in a bound scope and expires captured bound groups' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.Style.Progress.View = 'Minimal'
            $expiredProgress = & {
                $scopeView = $PS.AtScope('Local')
                $scopeView.Style.Progress.View = 'Classic'
                & {
                    $PSStyle.Progress.View = 'Classic'
                    $scopeView.Reset('Style.Progress.View')
                    Write-Host "$($scopeView.Style.Progress.View)/$($PSStyle.Progress.View)"
                }
                Write-Host $PSStyle.Progress.View
                $scopeView.Style.Progress
            }
            try { $expiredProgress.View } catch { 'expired progress read rejected' }
        }
        $result | Should -Be @('Minimal/Classic', 'Minimal', 'expired progress read rejected')
    }

    It 'resolves configured settings in module scopes rather than importing callers' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            $PS.Style.Progress.View = 'Minimal'
            $progress = $PS.Style.Progress
            $module = New-Module {
                $PS.ActionPreference.Error = 'Stop'
                $PSStyle.Progress.View = 'Classic'
                function Get-ModuleSetting($capturedProgress) {
                    "$($PS.ActionPreference.Error)/$($capturedProgress.View)"
                    $PS.ActionPreference.Error = 'SilentlyContinue'
                    "$ErrorActionPreference/$($PS.AtScope('Script').ActionPreference.Error)"
                }
            }
            & {
                $PS.ActionPreference.Error = 'SilentlyContinue'
                & $module { param($capturedProgress) Get-ModuleSetting $capturedProgress } $progress
            }
            "$($PS.ActionPreference.Error)/$($PSStyle.Progress.View)"
        }
        $result | Should -Be @('Stop/Classic', 'SilentlyContinue/Stop', 'Continue/Minimal')
    }

    It 'isolates progress settings across runspaces and rejects foreign bound views' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            $PSStyle.Progress.View = 'Classic'
            $runspacePowerShell = [powershell]::Create()
            try {
                $null = $runspacePowerShell.AddScript({
                    param($foreignView, $unboundProgress)
                    "$($PS.ActionPreference.Error)/$($unboundProgress.View)"
                    try { $foreignView.Style.Progress.View } catch { 'foreign view rejected' }
                    $PSStyle.Progress.View = 'Classic'
                    $PS.Reset('Style.Progress.View')
                    $PSStyle.Progress.View
                }).AddArgument($PS.AtScope('Global')).AddArgument($PS.Style.Progress)
                $runspacePowerShell.Invoke()
                $runspacePowerShell.HadErrors
            } finally {
                $runspacePowerShell.Dispose()
            }
            $PSStyle.Progress.View
        }
        $result | Should -Be @('Continue/Minimal', 'foreign view rejected', 'Minimal', 'False', 'Classic')
    }

    It 'rejects <Expression> and directs users to AtScope' -TestCases @(
        @{ Expression = '$global:PS' }
        @{ Expression = '${script:PS}' }
        @{ Expression = '$local:PS.Style.Progress.View' }
        @{ Expression = '"$private:PS"' }
    ) {
        param($Expression)

        $template = @'
            try {
                & ([scriptblock]::Create('__EXPRESSION__'))
            } catch {
                $_.Exception.Message.Contains('AtScope')
            }
'@
        $script = [scriptblock]::Create($template.Replace('__EXPRESSION__', $Expression))
        Invoke-ScopedPropertiesTest $script | Should -Be 'True'
    }

    It 'enforces constrained language restrictions on facade methods and setters' {
        $result = Invoke-ScopedPropertiesTest {
            $runspace = [runspacefactory]::CreateRunspace()
            $runspace.Open()
            $runspace.SessionStateProxy.LanguageMode = 'ConstrainedLanguage'
            $runspacePowerShell = [powershell]::Create()
            try {
                $runspacePowerShell.Runspace = $runspace
                $null = $runspacePowerShell.AddScript(@'
try { $PS.AtScope('Local') } catch { $_.FullyQualifiedErrorId }
try { $PS.Style.Progress.View = 'Classic' } catch { $_.FullyQualifiedErrorId }
'@)
                $runspacePowerShell.Invoke()
            } finally {
                $runspacePowerShell.Dispose()
                $runspace.Dispose()
            }
        }
        $result | Should -Be @(
            'MethodInvocationNotSupportedInConstrainedLanguage',
            'PropertySetterNotSupportedInConstrainedLanguage'
        )
    }
}
