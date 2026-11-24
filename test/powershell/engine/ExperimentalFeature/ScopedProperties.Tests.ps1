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

    It 'uses typed conversion for the built-in preference and child-scope assignments' {
        $result = Invoke-ScopedPropertiesTest {
            $ErrorActionPreference = 'sToP'
            "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
            try { $ErrorActionPreference = 'invalid preference' } catch { 'built-in rejected write' }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            & {
                try { $ErrorActionPreference = 'invalid preference' } catch { 'initial child write rejected' }
                $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                $ErrorActionPreference = 'sIlEnTlYcOnTiNuE'
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
                foreach ($invalid in @($null, 500, 'invalid preference')) {
                    try { $ErrorActionPreference = $invalid } catch { 'child write rejected' }
                    "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                }
                $ErrorActionPreference = 2
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/Stop', 'built-in rejected write', 'Stop/Stop',
            'initial child write rejected', 'True', 'Stop/Stop',
            'ActionPreference/SilentlyContinue/SilentlyContinue',
            'child write rejected', 'SilentlyContinue/SilentlyContinue',
            'child write rejected', 'SilentlyContinue/SilentlyContinue',
            'child write rejected', 'SilentlyContinue/SilentlyContinue',
            'ActionPreference/Continue/Continue', 'Stop/Stop'
        )
    }

    It 'normalizes variable cmdlet writes and resumes inheritance after removal' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                $created = New-Variable ErrorActionPreference 'cOnTiNuE' -Scope Local -PassThru
                "$($created.Value.GetType().Name)/$($created.Value)/$($PS.ActionPreference.Error)"
                Set-Variable ErrorActionPreference 0 -Scope Local
                $legacyValue = (Get-Variable ErrorActionPreference -Scope Local).Value
                "$($legacyValue.GetType().Name)/$legacyValue/$($PS.ActionPreference.Error)"
                try { New-Variable ErrorActionPreference 500 -Scope Local -Force -ErrorAction Stop } catch { 'replacement rejected' }
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                foreach ($invalid in @($null, 500, 'invalid preference')) {
                    try { Set-Variable ErrorActionPreference $invalid -Scope Local -ErrorAction Stop } catch { 'set rejected' }
                    "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                }
                try { Clear-Variable ErrorActionPreference -Scope Local -ErrorAction Stop } catch { 'clear rejected' }
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"

                $PS.ActionPreference.Error = 'Continue'
                (Get-Variable ErrorActionPreference -Scope Local).Value.GetType().Name
                $PS.ActionPreference.Error
                Remove-Variable ErrorActionPreference -Scope Local
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                $local:ErrorActionPreference = 'silentlycontinue'
                "$($local:ErrorActionPreference.GetType().Name)/$($PS.ActionPreference.Error)/$global:ErrorActionPreference"
                $PS.Reset('ActionPreference.Error')
                $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
            }
        }
        $result | Should -BeExactly @(
            'ActionPreference/Continue/Continue', 'ActionPreference/SilentlyContinue/SilentlyContinue',
            'replacement rejected', 'SilentlyContinue/SilentlyContinue',
            'set rejected', 'SilentlyContinue/SilentlyContinue', 'set rejected', 'SilentlyContinue/SilentlyContinue',
            'set rejected', 'SilentlyContinue/SilentlyContinue', 'clear rejected', 'SilentlyContinue/SilentlyContinue',
            'ActionPreference', 'Continue', 'Stop/Stop', 'ActionPreference/SilentlyContinue/Stop', 'True'
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

    It 'preserves read-only options and the value on failed forced writes' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            & {
                New-Variable ErrorActionPreference 'Stop' -Option ReadOnly
                try { Set-Variable ErrorActionPreference 500 -Force -ErrorAction Stop } catch { 'forced set rejected' }
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($PS.ActionPreference.Error)"
                try { Clear-Variable ErrorActionPreference -Scope Local -Force -ErrorAction Stop } catch { 'forced clear rejected' }
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$ErrorActionPreference"
                Set-Variable ErrorActionPreference 'silentlyContinue' -Force
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($PS.ActionPreference.Error)"
                Set-Variable ErrorActionPreference 'Stop' -Force -Option None
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($PS.ActionPreference.Error)"
                Set-Variable ErrorActionPreference 'SilentlyContinue' -Option ReadOnly
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($PS.ActionPreference.Error)"
                try {
                    Set-Variable ErrorActionPreference 'Stop' -Force -Option Constant -ErrorAction Stop
                } catch { 'constant option rejected' }
                "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($PS.ActionPreference.Error)"
                try { Remove-Variable ErrorActionPreference -Scope Local -ErrorAction Stop } catch { 'remove rejected' }
                Remove-Variable ErrorActionPreference -Scope Local -Force
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
        }
        $result | Should -BeExactly @(
            'forced set rejected', 'ReadOnly/Stop', 'forced clear rejected', 'ReadOnly/Stop',
            'ReadOnly/SilentlyContinue', 'None/Stop', 'ReadOnly/SilentlyContinue',
            'constant option rejected', 'ReadOnly/SilentlyContinue', 'remove rejected', 'Continue/Continue'
        )
    }

    It 'shares the canonical error preference with variable-provider item operations' {
        $result = Invoke-ScopedPropertiesTest {
            $path = 'Variable:ErrorActionPreference'
            Set-Item -LiteralPath $path -Value 'sToP'
            $value = (Get-Item -LiteralPath $path).Value
            "$($value.GetType().Name)/$value/$($PS.ActionPreference.Error)"
            try { Set-Item -LiteralPath $path -Value 500 -ErrorAction Stop } catch { 'provider set rejected' }
            try { Clear-Item -LiteralPath $path -ErrorAction Stop } catch { 'provider clear rejected' }
            "$((Get-Item -LiteralPath $path).Value)/$($PS.ActionPreference.Error)"

            Remove-Item -LiteralPath $path
            "$(Test-Path -LiteralPath $path)/$($PS.ActionPreference.Error)"
            $created = New-Item -Path $path -Value 'silentlycontinue'
            "$($created.Value.GetType().Name)/$($created.Value)/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/Stop', 'provider set rejected', 'provider clear rejected', 'Stop/Stop',
            'False/Continue', 'ActionPreference/SilentlyContinue/SilentlyContinue'
        )
    }

    It 'updates retained variables through the provider and detaches them on removal' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            $path = 'Variable:ErrorActionPreference'
            $retained = Get-Item -LiteralPath $path

            Set-Content -LiteralPath $path -Value 'sIlEnTlYcOnTiNuE'
            "$($retained.Value.GetType().Name)/$($retained.Value)/$(Get-Content -LiteralPath $path)"
            try { Set-Content -LiteralPath $path -Value 500 -ErrorAction Stop } catch { 'invalid content rejected' }
            try { Clear-Content -LiteralPath $path -ErrorAction Stop } catch { 'clear content rejected' }
            try { Clear-Item -LiteralPath $path -ErrorAction Stop } catch { 'clear item rejected' }
            "$($retained.Value)/$($PS.ActionPreference.Error)"

            Remove-Item -LiteralPath $path
            "$($null -eq $retained.Value)/$($PS.ActionPreference.Error)/$(Test-Path -LiteralPath $path)"
            $retained.Value = 'detached'
            "$($retained.Value)/$(Test-Path -LiteralPath $path)"
        }
        $result | Should -BeExactly @(
            'ActionPreference/SilentlyContinue/SilentlyContinue', 'invalid content rejected', 'clear content rejected', 'clear item rejected',
            'SilentlyContinue/SilentlyContinue', 'True/Continue/False', 'detached/False'
        )
    }

    It 'shares AllScope writes and removes only the target scope binding' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                New-Variable ErrorActionPreference 'Continue' -Option AllScope
                $parent = $PS.AtScope('Local')
                & {
                    $ErrorActionPreference = 'SilentlyContinue'
                    "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$($parent.ActionPreference.Error)"
                    $PS.Reset('ActionPreference.Error')
                    $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
                    "$ErrorActionPreference/$($PS.ActionPreference.Error)"

                    $PS.ActionPreference.Error = 'Continue'
                    "$((Get-Variable ErrorActionPreference -Scope Local).Options)/$ErrorActionPreference/$($parent.ActionPreference.Error)"
                }
                & {
                    Remove-Variable ErrorActionPreference -Scope Local
                    $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
                    "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                }
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'AllScope/SilentlyContinue', 'True', 'SilentlyContinue/SilentlyContinue',
            'None/Continue/SilentlyContinue', 'True', 'SilentlyContinue/SilentlyContinue',
            'SilentlyContinue/SilentlyContinue', 'Stop/Stop'
        )
    }

    It 'uses existing preference validation for assignments of the reserved Suspend value' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'SilentlyContinue'
            try { $ErrorActionPreference = 'Suspend' } catch { $_.Exception.Message.Contains('Suspend') }
            try { $PS.ActionPreference.Error = 'Suspend' } catch { $_.Exception.Message.Contains('Suspend') }
            "$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -Be @('True', 'True', 'SilentlyContinue/SilentlyContinue')
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

    It 'resets implicit advanced-function preferences and keeps explicit command-level precedence' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-AdvancedPreference {
                [CmdletBinding()]
                param()
                "entry: $($PS.ActionPreference.Error)"
                $PS.Reset('ActionPreference.Error')
                "after reset: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                $null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore)
                & {
                    Write-Error 'expected' 2>$null
                    'nested command used inherited preference'
                }
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
            'entry: Stop', 'after reset: Continue/Continue', 'True', 'nested command used inherited preference', 'ignored',
            'assigned: SilentlyContinue', 'continued with facade preference',
            'command stopped', 'caller: Continue', 'facade stopped command'
        )
    }

    It 'uses the inherited policy for a nested command immediately after either form of reset' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-PreferenceReset {
                [CmdletBinding()]
                param([switch] $Dynamic)

                if ($Dynamic) {
                    $method = 'Reset'
                    $PS.$method('ActionPreference.Error')
                } else {
                    $PS.Reset('ActionPreference.Error')
                }

                Write-Error 'expected'
                "continued with $ErrorActionPreference"
            }

            $PS.ActionPreference.Error = 'SilentlyContinue'
            Invoke-PreferenceReset -ErrorAction Stop
            Invoke-PreferenceReset -Dynamic -ErrorAction Stop
        }
        $result | Should -BeExactly @('continued with SilentlyContinue', 'continued with SilentlyContinue')
    }

    It 'keeps an error preference cached for an existing command and uses new values for later commands' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'SilentlyContinue'
            'first', 'second' | ForEach-Object {
                if ($_ -eq 'second') {
                    $global:ErrorActionPreference = 'Stop'
                }
                $_
            } | Write-Error -ErrorVariable captured
            "completed: $($captured.Count)"
            try { Write-Error 'expected' } catch { 'new command stopped' }
        }
        $result | Should -BeExactly @('completed: 2', 'new command stopped')
    }

    It 'keeps an advanced invocation error preference separate from an inherited AllScope entry' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-ImplicitPreference {
                [CmdletBinding()]
                param()
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
                $PS.Reset('ActionPreference.Error')
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            & {
                New-Variable ErrorActionPreference 'Continue' -Option AllScope
                Invoke-ImplicitPreference -ErrorAction Stop
                "caller: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                & {
                    "child: $ErrorActionPreference/$($PS.ActionPreference.Error)/$((Get-Variable ErrorActionPreference -Scope Local).Options)"
                }
            }
        }
        $result | Should -BeExactly @('Stop/Stop', 'Continue/Continue', 'caller: Continue/Continue', 'child: Continue/Continue/AllScope')
    }

    It 'uses typed entries for error preference parameters and retains their validation' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Continue'
            $block = {
                param([ValidateSet('Stop', 'Continue')]$ErrorActionPreference)
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
                try { $PS.ActionPreference.Error = 'SilentlyContinue' } catch { 'parameter validation rejected write' }
                $PS.Reset('ActionPreference.Error')
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            & $block 'sToP'
            $block.Invoke('sToP')
            function Invoke-AdvancedParameter {
                [CmdletBinding()]
                param([ValidateSet('Stop', 'Continue')]$ErrorActionPreference)
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
                try { $PS.ActionPreference.Error = 'SilentlyContinue' } catch { 'advanced parameter validation rejected write' }
                $PS.Reset('ActionPreference.Error')
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            Invoke-AdvancedParameter -ErrorActionPreference 'sToP'
            "caller: $ErrorActionPreference"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/Stop', 'parameter validation rejected write', 'Continue/Continue',
            'ActionPreference/Stop/Stop', 'parameter validation rejected write', 'Continue/Continue',
            'ActionPreference/Stop/Stop', 'advanced parameter validation rejected write', 'Continue/Continue',
            'caller: Continue'
        )
    }

    It 'uses explicit ErrorAction for advanced invocations with a named error preference parameter' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-NamedPreference {
                [CmdletBinding()]
                param([ValidateSet('Continue')][string]$ErrorActionPreference)
                "$($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            $PS.ActionPreference.Error = 'SilentlyContinue'
            Invoke-NamedPreference Continue -ErrorAction Stop
            "caller: $ErrorActionPreference"
            . Invoke-NamedPreference Continue
            "caller: $ErrorActionPreference"
            . Invoke-NamedPreference Continue -ErrorAction Stop
            "caller: $ErrorActionPreference"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/Stop', 'caller: SilentlyContinue',
            'ActionPreference/Continue/Continue', 'caller: SilentlyContinue',
            'ActionPreference/Stop/Stop', 'caller: SilentlyContinue'
        )
    }

    It 'keeps the implicit error preference of a dot-sourced advanced invocation temporary' {
        $result = Invoke-ScopedPropertiesTest {
            function Invoke-DottedPreference {
                [CmdletBinding()]
                param()
                if ($PSBoundParameters.ContainsKey('ErrorAction')) {
                    "entry: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                    $PS.Reset('ActionPreference.Error')
                    "reset: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                    try { Clear-Variable ErrorActionPreference -Scope Local -Force -ErrorAction Stop } catch { 'clear rejected' }
                    "after clear: $ErrorActionPreference/$($PS.ActionPreference.Error)"
                    $described = Set-Variable ErrorActionPreference -Description 'temporary' -PassThru
                    "metadata: $($described.Value)/$($described.Description)"
                    $PS.Reset('ActionPreference.Error')
                    $written = Set-Variable ErrorActionPreference 'sToP' -Scope Local -PassThru
                    "cmdlet: $($written.Value.GetType().Name)/$($written.Value)/$($PS.ActionPreference.Error)"
                }
                $ErrorActionPreference = 'SilentlyContinue'
                "assigned: $ErrorActionPreference/$($PS.ActionPreference.Error)"
            }
            $PS.ActionPreference.Error = 'Continue'
            (Get-Variable ErrorActionPreference -Scope Local).Description = 'caller preference'
            . Invoke-DottedPreference -ErrorAction Stop
            "caller: $ErrorActionPreference/$($PS.ActionPreference.Error)/$((Get-Variable ErrorActionPreference -Scope Local).Description)"
            . Invoke-DottedPreference
            "caller: $ErrorActionPreference/$($PS.ActionPreference.Error)"
            . {
                param($ErrorActionPreference)
                "parameter: $($ErrorActionPreference.GetType().Name)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
            } 'sToP'
            "caller: $ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'entry: Stop/Stop', 'reset: Continue/Continue', 'clear rejected', 'after clear: Continue/Continue',
            'metadata: Continue/temporary', 'cmdlet: ActionPreference/Stop/Stop', 'assigned: SilentlyContinue/SilentlyContinue',
            'caller: Continue/Continue/caller preference', 'assigned: SilentlyContinue/SilentlyContinue', 'caller: SilentlyContinue/SilentlyContinue',
            'parameter: ActionPreference/Stop/Stop', 'caller: Stop/Stop'
        )
    }

    It 'resets error overrides in optimized functions with and without a variable breakpoint' {
        $result = Invoke-ScopedPropertiesTest {
            function Test-PreferenceVariable {
                $ErrorActionPreference = 'Stop'
                $PS.ActionPreference.Error
                $PS.ActionPreference.Error = 'SilentlyContinue'
                $ErrorActionPreference
                $method = 'Reset'
                $PS.$method('ActionPreference.Error')
                $ErrorActionPreference
            }
            $PS.ActionPreference.Error = 'Continue'
            Test-PreferenceVariable
            $global:preferenceBreakpointHits = 0
            $breakpoint = Set-PSBreakpoint -Variable ErrorActionPreference -Mode Write -Action {
                $global:preferenceBreakpointHits++
            }
            Test-PreferenceVariable
            Remove-PSBreakpoint $breakpoint
            $global:preferenceBreakpointHits
        }
        $result | Should -Be @(
            'Stop', 'SilentlyContinue', 'Continue', 'Stop', 'SilentlyContinue', 'Continue', '2'
        )
    }

    It 'enumerates the canonical error preference and the other preferences in debugger frames' {
        $result = Invoke-ScopedPropertiesTest {
            function Get-PreferenceFrame {
                [CmdletBinding(SupportsShouldProcess)]
                param()

                $frame = (Get-PSCallStack)[0]
                $value = $frame.GetFrameVariables()['ErrorActionPreference'].Value
                "$($value.GetType().Name)/$value"
                $PS.Reset('ActionPreference.Error')
                $entries = $frame.GetFrameVariables()
                "$($entries.ContainsKey('ErrorActionPreference'))/$ErrorActionPreference"

                $preferences = [ordered]@{
                    DebugPreference = $DebugPreference
                    VerbosePreference = $VerbosePreference
                    WarningPreference = $WarningPreference
                    InformationPreference = $InformationPreference
                    ProgressPreference = $ProgressPreference
                    WhatIfPreference = $WhatIfPreference
                    ConfirmPreference = $ConfirmPreference
                }
                foreach ($name in $preferences.Keys) {
                    $value = $preferences[$name]
                    $frameValue = $entries[$name].Value
                    "$name/$($value.GetType().Name)/$value/$($frameValue.GetType().Name)/$frameValue"
                }
            }

            $PS.ActionPreference.Error = 'Continue'
            $options = @{
                ErrorAction = 'Stop'
                Debug = $true
                Verbose = $true
                WarningAction = 'Stop'
                InformationAction = 'Continue'
                ProgressAction = 'SilentlyContinue'
                WhatIf = $true
                Confirm = $false
            }
            Get-PreferenceFrame @options
            . Get-PreferenceFrame @options
        }
        $expected = @(
            'ActionPreference/Stop', 'False/Continue',
            'DebugPreference/ActionPreference/Continue/ActionPreference/Continue',
            'VerbosePreference/ActionPreference/Continue/ActionPreference/Continue',
            'WarningPreference/ActionPreference/Stop/ActionPreference/Stop',
            'InformationPreference/ActionPreference/Continue/ActionPreference/Continue',
            'ProgressPreference/ActionPreference/SilentlyContinue/ActionPreference/SilentlyContinue',
            'WhatIfPreference/SwitchParameter/True/SwitchParameter/True',
            'ConfirmPreference/ConfirmImpact/None/ConfirmImpact/None'
        )
        $result | Should -BeExactly ($expected + $expected)
    }

    It 'uses the actual script, local, and global scopes and expires local views' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            $scriptPath = Join-Path ([IO.Path]::GetTempPath()) ("ScopedProperties-{0}.ps1" -f [guid]::NewGuid())
            try {
                @'
$script:ErrorActionPreference = 'sToP'
$scriptSettings = $PS.AtScope('Script')
& {
    $localSettings = $PS.AtScope('Local')
    $local:ErrorActionPreference = 'silentlycontinue'
    $global:ErrorActionPreference = 'continue'
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

    It 'keeps a captured error variable synchronized until its last binding is reset' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                $ErrorActionPreference = 'Continue'
                $captured = Get-Variable ErrorActionPreference -Scope Local
                $PS.ActionPreference.Error = 'SilentlyContinue'
                "$($captured.Value.GetType().Name)/$($captured.Value)"
                $captured.Value = 'cOnTiNuE'
                "$ErrorActionPreference/$($PS.ActionPreference.Error)"

                $PS.Reset('ActionPreference.Error')
                "$($null -eq $captured.Value)/$ErrorActionPreference"
                $captured.Value = 'detached'
                "$($captured.Value)/$($null -eq (Get-Variable ErrorActionPreference -Scope Local -ErrorAction Ignore))/$($PS.ActionPreference.Error)"
            }
        }
        $result | Should -BeExactly @('ActionPreference/SilentlyContinue', 'Continue/Continue', 'True/Stop', 'detached/True/Stop')
    }

    It 'retains a captured error variable value after its scope exits' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            $captured = & {
                $ErrorActionPreference = 'SilentlyContinue'
                Get-Variable ErrorActionPreference -Scope Local
            }

            "$($captured.Value.GetType().Name)/$($captured.Value)/$($PS.ActionPreference.Error)"
            $captured.Value = 'cOnTiNuE'
            "$($captured.Value)/$ErrorActionPreference/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @('ActionPreference/SilentlyContinue/Stop', 'Continue/Stop/Stop')
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

    It 'copies ordinary and foreign variable objects into the target setting' {
        $result = Invoke-ScopedPropertiesTest {
            $PS.ActionPreference.Error = 'Stop'
            & {
                $supplied = [psvariable]::new('ErrorActionPreference', 'SilentlyContinue')
                $supplied.Description = 'supplied'
                $ExecutionContext.SessionState.PSVariable.Set($supplied)
                $stored = Get-Variable ErrorActionPreference -Scope Local
                "$([object]::ReferenceEquals($supplied, $stored))/$($stored.Value.GetType().Name)/$($stored.Description)"
                $supplied.Value = 'Stop'
                "$($supplied.Value)/$($stored.Value)/$($PS.ActionPreference.Error)"
            }

            $source = Get-Variable ErrorActionPreference -Scope Global
            $source.Description = 'source runspace'
            $target = [powershell]::Create()
            try {
                $null = $target.AddScript({
                    param($foreign)
                    & {
                        $variables = $ExecutionContext.SessionState.PSVariable
                        $variables.Set($foreign)
                        $imported = $variables.Get('ErrorActionPreference')
                        "$([object]::ReferenceEquals($foreign, $imported))/$($imported.Value)/$($imported.Description)"
                        $PS.ActionPreference.Error = 'SilentlyContinue'
                        $imported.Description = 'destination runspace'
                        "$($imported.Value)/$($foreign.Value)/$($foreign.Description)"
                    }
                }).AddArgument($source)
                $target.Invoke()
                $target.HadErrors
            } finally {
                $target.Dispose()
            }

            "$($source.Value)/$($source.Description)/$($PS.ActionPreference.Error)"
        }
        $result | Should -BeExactly @(
            'False/ActionPreference/supplied', 'Stop/SilentlyContinue/SilentlyContinue',
            'False/Stop/source runspace', 'SilentlyContinue/Stop/source runspace', 'False', 'Stop/source runspace/Stop'
        )
    }

    It 'shares explicitly exported module preferences only within the same runspace' {
        $result = Invoke-ScopedPropertiesTest {
            $module = New-Module -Name ScopedPreferenceExport {
                $ErrorActionPreference = 'Stop'
                function Set-ScopedExportedPreference {
                    $script:ErrorActionPreference = 'SilentlyContinue'
                }
                function Get-ScopedExportedPreference {
                    "$($PS.ActionPreference.Error)/$((Get-Variable ErrorActionPreference -Scope Script).Description)"
                }
                Export-ModuleMember -Variable ErrorActionPreference -Function Set-ScopedExportedPreference, Get-ScopedExportedPreference
            }
            Import-Module $module
            $exported = $module.ExportedVariables['ErrorActionPreference']
            $imported = Get-Variable ErrorActionPreference -Scope Global
            [object]::ReferenceEquals($exported, $imported)
            Set-ScopedExportedPreference
            $imported.Description = 'same runspace'
            "$($imported.Value)/$($PS.ActionPreference.Error)"
            Get-ScopedExportedPreference

            $foreignShell = [powershell]::Create()
            try {
                $null = $foreignShell.AddScript({
                    param($foreignModule)
                    Import-Module $foreignModule
                    $foreignExport = $foreignModule.ExportedVariables['ErrorActionPreference']
                    $foreignImport = Get-Variable ErrorActionPreference -Scope Global
                    [object]::ReferenceEquals($foreignExport, $foreignImport)
                    $PS.ActionPreference.Error = 'Stop'
                    $foreignImport.Description = 'other runspace'
                    "$($foreignImport.Value)/$($foreignExport.Value)/$($foreignExport.Description)"
                    Remove-Module $foreignModule.Name
                    "$($null -eq (Get-Variable ErrorActionPreference -Scope Global -ErrorAction Ignore))/$($PS.ActionPreference.Error)"
                }).AddArgument($module)
                $foreignShell.Invoke()
                $foreignShell.Streams.Error.Count
            } finally {
                $foreignShell.Dispose()
            }

            Remove-Module $module.Name
            "$($null -eq (Get-Variable ErrorActionPreference -Scope Global -ErrorAction Ignore))/$($PS.ActionPreference.Error)/$($exported.Value)"
        }
        $result | Should -BeExactly @(
            'True', 'SilentlyContinue/SilentlyContinue', 'SilentlyContinue/same runspace',
            'False', 'Stop/SilentlyContinue/same runspace', 'True/Continue', '0',
            'True/Continue/SilentlyContinue'
        )
    }

    It 'normalizes host initialization and session-state API writes in the same entry' {
        $result = Invoke-ScopedPropertiesTest {
            $iss = [System.Management.Automation.Runspaces.InitialSessionState]::CreateDefault2()
            $iss.Variables.Add([System.Management.Automation.Runspaces.SessionStateVariableEntry]::new(
                'ErrorActionPreference', 'sIlEnTlYcOnTiNuE', 'Host preference'))
            $runspace = [runspacefactory]::CreateRunspace($iss)
            $controller = [powershell]::Create()
            try {
                $runspace.Open()
                $controller.Runspace = $runspace
                $proxy = $runspace.SessionStateProxy
                $value = $proxy.GetVariable('ErrorActionPreference')
                "$($value.GetType().Name)/$value"
                $proxy.SetVariable('ErrorActionPreference', 'sToP')
                try { $proxy.SetVariable('ErrorActionPreference', $null) } catch { 'host write rejected' }

                $null = $controller.AddScript({
                    $variables = $ExecutionContext.SessionState.PSVariable
                    "$ErrorActionPreference/$($PS.ActionPreference.Error)/$($variables.Get('ErrorActionPreference').Description)"
                    $variables.Set([psvariable]::new('ErrorActionPreference', 'cOnTiNuE'))
                    $value = $variables.GetValue('ErrorActionPreference')
                    "$($value.GetType().Name)/$value/$($PS.ActionPreference.Error)"
                    $variables.Remove('ErrorActionPreference')
                    "$($null -eq $variables.Get('ErrorActionPreference'))/$($PS.ActionPreference.Error)"
                })
                $controller.Invoke()
                $controller.HadErrors
            } finally {
                $controller.Dispose()
                $runspace.Dispose()
            }
        }
        $result | Should -BeExactly @(
            'ActionPreference/SilentlyContinue', 'host write rejected', 'Stop/Stop/Host preference',
            'ActionPreference/Continue/Continue', 'True/Continue', 'False'
        )
    }

    It 'rejects an invalid host initial preference without replacing the preceding entry' {
        $result = Invoke-ScopedPropertiesTest {
            $iss = [initialsessionstate]::CreateDefault2()
            $iss.Variables.Add([System.Management.Automation.Runspaces.SessionStateVariableEntry]::new(
                'ErrorActionPreference', 'sToP', 'valid host entry'))
            $iss.Variables.Add([System.Management.Automation.Runspaces.SessionStateVariableEntry]::new(
                'ErrorActionPreference', 'invalid host preference', 'invalid host entry'))

            $relaxed = [runspacefactory]::CreateRunspace($iss)
            try {
                $relaxed.Open()
                $variable = $relaxed.SessionStateProxy.PSVariable.Get('ErrorActionPreference')
                "$($variable.Value.GetType().Name)/$($variable.Value)/$($variable.Description)"
            } finally {
                $relaxed.Dispose()
            }

            $iss.ThrowOnRunspaceOpenError = $true
            $strict = [runspacefactory]::CreateRunspace($iss)
            try {
                try { $strict.Open() } catch { $_.Exception.GetBaseException().Message.Contains('invalid host preference') }
                $strict.RunspaceStateInfo.State
            } finally {
                $strict.Dispose()
            }
        }
        $result | Should -BeExactly @('ActionPreference/Stop/valid host entry', 'True', 'Broken')
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

    It 'shares typed Warning and Information values with legacy variables and captured adapters' {
        $result = Invoke-ScopedPropertiesTest {
            foreach ($name in 'Warning', 'Information') {
                & {
                    $variableName = "${name}Preference"
                    $view = $PS.AtScope('Local')
                    "$name/$($view.ActionPreference.$name)"
                    $view.ActionPreference.$name = 'Stop'
                    $variable = Get-Variable $variableName -Scope Local
                    "$($variable.Value.GetType().Name)/$($variable.Value)"
                    Set-Variable $variableName 'iGnOrE' -Scope Local
                    $view.ActionPreference.$name
                    $variable.Value = 'Continue'

                    foreach ($invalid in 'Suspend', 'invalid preference', 500, $null) {
                        try { $variable.Value = $invalid } catch { "rejected/$($view.ActionPreference.$name)" }
                    }

                    $view.Reset("actionpreference.$($name.ToUpperInvariant())")
                    "$($null -eq $variable.Value)/$($view.ActionPreference.$name)"
                }
            }
        }
        $result | Should -BeExactly @(
            'Warning/Continue', 'ActionPreference/Stop', 'Ignore',
            'rejected/Continue', 'rejected/Continue', 'rejected/Continue', 'rejected/Continue', 'True/Continue',
            'Information/SilentlyContinue', 'ActionPreference/Stop', 'Ignore',
            'rejected/Continue', 'rejected/Continue', 'rejected/Continue', 'rejected/Continue', 'True/SilentlyContinue'
        )
    }

    It 'keeps dotted Error, Warning, and Information overrides independent across command stages' {
        $result = Invoke-ScopedPropertiesTest {
            function Test-NamedPreferences {
                param($WarningPreference, $InformationPreference)
                "$($WarningPreference.GetType().Name)/$($PS.ActionPreference.Warning)/$($InformationPreference.GetType().Name)/$($PS.ActionPreference.Information)"
            }

            function Test-DottedPreferences {
                [CmdletBinding(SupportsShouldProcess)]
                param(
                    [ValidateSet('SilentlyContinue')][string] $InformationPreference,
                    [ValidateSet('Continue')][string] $WarningPreference
                )
                begin {
                    "begin/$ErrorActionPreference/$WarningPreference/$InformationPreference/$ProgressPreference/$WhatIfPreference/$ConfirmPreference"
                    $frame = (Get-PSCallStack)[0]
                    $entries = $frame.GetFrameVariables()
                    "frame/$($entries.ErrorActionPreference.Value)/$($entries.WarningPreference.Value)/$($entries.InformationPreference.Value)"

                    $PS.Reset('actionpreference.WARNING')
                    "reset/$($frame.GetFrameVariables().ContainsKey('WarningPreference'))/$($PS.ActionPreference.Warning)"

                    $variables = @{}
                    Get-Variable -Scope Local | ForEach-Object { $variables[$_.Name] = $_ }
                    "variables/$($variables.ErrorActionPreference.Value)/$($variables.WarningPreference.Value)/$($variables.InformationPreference.Value)"
                    $WarningPreference = 'Ignore'
                }
                process { "process/$($PS.ActionPreference.Error)/$($PS.ActionPreference.Warning)/$($PS.ActionPreference.Information)" }
                end { "end/$ErrorActionPreference/$WarningPreference/$InformationPreference" }
                clean { $global:scopedWarningInformationCleanup = "clean/$ErrorActionPreference/$WarningPreference/$InformationPreference" }
            }

            $PS.ActionPreference.Error = 'Continue'
            $PS.ActionPreference.Warning = 'SilentlyContinue'
            $PS.ActionPreference.Information = 'SilentlyContinue'
            Test-NamedPreferences 'sToP' 'iGnOrE'
            . Test-DottedPreferences SilentlyContinue Continue -ErrorAction Stop -WarningAction Stop -InformationAction Continue -ProgressAction SilentlyContinue -WhatIf -Confirm:$false
            $global:scopedWarningInformationCleanup
            "caller/$ErrorActionPreference/$WarningPreference/$InformationPreference"
        }
        $result | Should -BeExactly @(
            'ActionPreference/Stop/ActionPreference/Ignore',
            'begin/Stop/Stop/Continue/SilentlyContinue/True/None',
            'frame/Stop/Stop/Continue', 'reset/False/SilentlyContinue', 'variables/Stop/SilentlyContinue/Continue',
            'process/Stop/Ignore/Continue', 'end/Stop/Ignore/Continue', 'clean/Stop/Ignore/Continue',
            'caller/Continue/SilentlyContinue/SilentlyContinue'
        )
    }

    It 'uses inherited Warning and Information preferences for stream output and explicit overrides' {
        $result = Invoke-ScopedPropertiesTest {
            function Write-ScopedRecords {
                [CmdletBinding()]
                param()
                Write-Warning 'warning record'
                Write-Information 'information record'
            }

            $PS.ActionPreference.Warning = 'Ignore'
            $PS.ActionPreference.Information = 'Ignore'
            "ignored/$(@(Write-ScopedRecords 3>&1 6>&1).Count)"
            Write-ScopedRecords -WarningAction Continue -InformationAction Continue 3>&1 6>&1 |
                ForEach-Object { "override/$($_.GetType().Name)/$_" }

            $PS.ActionPreference.Warning = 'Continue'
            $PS.ActionPreference.Information = 'Continue'
            Write-ScopedRecords 3>&1 6>&1 | ForEach-Object { "inherited/$($_.GetType().Name)/$_" }

            $PS.ActionPreference.Warning = 'Stop'
            try { & { Write-Warning 'stopping warning' 3>$null } } catch { 'warning stopped' }
            $PS.ActionPreference.Information = 'Stop'
            try { & { Write-Information 'stopping information' 6>$null } } catch { 'information stopped' }
        }
        $result | Should -BeExactly @(
            'ignored/0', 'override/WarningRecord/warning record', 'override/InformationRecord/information record',
            'inherited/WarningRecord/warning record', 'inherited/InformationRecord/information record',
            'warning stopped', 'information stopped'
        )
    }
}
