# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Clipboard cmdlet tests' -Tag CI {
    BeforeAll {
        $xclip = Get-Command xclip -CommandType Application -ErrorAction Ignore
    }

    Context 'Text' {
        BeforeAll {
            $defaultParamValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = ($IsWindows -and $env:PROCESSOR_ARCHITECTURE.Contains("arm")) -or ($IsLinux -and $xclip -eq $null)
        }

        AfterAll {
            $global:PSDefaultParameterValues = $defaultParamValues
        }

        It 'Get-Clipboard returns what is in Set-Clipboard' {
            $guid = New-Guid
            Set-Clipboard -Value $guid
            Get-Clipboard | Should -BeExactly $guid
        }

        It 'Get-Clipboard returns an array' {
            1,2 | Set-Clipboard
            $out = Get-Clipboard
            $out.Count | Should -Be 2
            $out[0] | Should -Be 1
            $out[1] | Should -Be 2
        }

        It 'Get-Clipboard -Raw returns one item' {
            1,2 | Set-Clipboard
            (Get-Clipboard -Raw).Count | Should -Be 1
            Get-Clipboard -Raw | Should -BeExactly "1$([Environment]::NewLine)2"
        }

        It 'Get-Clipboard -Delimiter should return items based on the delimiter' {
            Set-Clipboard -Value "Line1`r`nLine2`nLine3"
            $result = Get-Clipboard -Delimiter "`r`n", "`n"
            $result.Count | Should -Be 3
            $result | ForEach-Object -Process {$_.Length | Should -Be "LineX".Length}
        }

        It 'Set-Clipboard -Append will add text' {
            'hello' | Set-Clipboard
            'world' | Set-Clipboard -Append
            Get-Clipboard -Raw | Should -BeExactly "hello$([Environment]::NewLine)world"
        }

        It 'Set-Clipboard accepts <value> string' -TestCases @(
            @{ value = 'empty'; text = "" }
            @{ value = 'null' ; text = $null }
        ){
            param ($text)

            $text | Set-Clipboard
            Get-Clipboard -Raw | Should -BeNullOrEmpty
        }

        It 'Set-Clipboard should not return object' {
            $result = 'hello' | Set-Clipboard
            $result | Should -BeNullOrEmpty
        }

        It 'Set-Clipboard -PassThru returns single object with -Append = <Append>' -TestCases @(
            @{ Append = $false }
            @{ Append = $true }
        ){
            param ($append)

            $params = @{ PassThru = $true; Append = $append }

            Set-Clipboard -Value 'world'
            $result = 'hello' | Set-Clipboard @params
            $result | Should -BeExactly 'hello'
        }

        It 'Set-Clipboard -PassThru returns multiple objects with -Append = <Append>' -TestCases @(
            @{ Append = $false }
            @{ Append = $true }
        ){
            param ($append)

            $params = @{ PassThru = $true; Append = $append }

            Set-Clipboard -Value 'world'
            $result = 'hello', 'world' | Set-Clipboard @params
            $result | Should -BeExactly @('hello', 'world')
        }
    }

    Context 'External clipboard process handling' {
        BeforeAll {
            $clipboardAssembly = (Get-Command Set-Clipboard).ImplementingType.Assembly
            $clipboardType = $clipboardAssembly.GetType('Microsoft.PowerShell.Commands.Internal.Clipboard')
            $bindingFlags = [System.Reflection.BindingFlags]'NonPublic, Static'
            $startProcess = $clipboardType.GetMethods($bindingFlags) |
                Where-Object { $_.Name -eq 'StartProcess' -and $_.GetParameters().Count -eq 5 }
            $clipboardSupported = $clipboardType.GetField('_clipboardSupported', $bindingFlags)
            $pwsh = Join-Path $PSHOME 'pwsh'
        }

        BeforeEach {
            $originalClipboardSupported = $clipboardSupported.GetValue($null)
        }

        AfterEach {
            $clipboardSupported.SetValue($null, $originalClipboardSupported)
        }

        It 'terminates a clipboard helper that times out' {
            $arguments = '-NoProfile -Command "Start-Sleep -Seconds 30"'
            $invokeArguments = [object[]]@(
                $pwsh.PSObject.BaseObject
                $arguments.PSObject.BaseObject
                [string]::Empty
                $false
                0
            )
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

            try {
                $result = $startProcess.Invoke($null, $invokeArguments)
                $result | Should -BeNullOrEmpty
                $helperPid = [int]$invokeArguments[4]
                $helperPid | Should -BeGreaterThan 0
                Get-Process -Id $helperPid -ErrorAction Ignore | Should -BeNullOrEmpty
                $stopwatch.Elapsed | Should -BeLessThan ([TimeSpan]::FromSeconds(5))
                $clipboardSupported.GetValue($null) | Should -BeFalse
            }
            finally {
                $stopwatch.Stop()
                if ($helperPid) {
                    Stop-Process -Id $helperPid -Force -ErrorAction Ignore
                }
            }
        }

        It 'does not wait for a descendant that retains stdout' -Skip:$IsWindows {
            $childPidPath = Join-Path $TestDrive 'child.pid'
            $helperPath = Join-Path $TestDrive 'retained-pipe-helper.sh'
            @'
sleep 30 &
echo $! > "$1"
'@ | Set-Content -LiteralPath $helperPath

            $arguments = '"{0}" "{1}"' -f $helperPath, $childPidPath
            $invokeArguments = [object[]]@(
                '/bin/sh'
                $arguments.PSObject.BaseObject
                [string]::Empty
                $true
                0
            )
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

            try {
                $result = $startProcess.Invoke($null, $invokeArguments)
                $result | Should -BeNullOrEmpty
                $childPidPath | Should -Exist
                $childPid = [int](Get-Content -LiteralPath $childPidPath)
                Get-Process -Id $childPid -ErrorAction Stop | Should -Not -BeNullOrEmpty
                $stopwatch.Elapsed | Should -BeLessThan ([TimeSpan]::FromSeconds(5))
                $clipboardSupported.GetValue($null) | Should -BeFalse
            }
            finally {
                $stopwatch.Stop()
                if ($childPid) {
                    Stop-Process -Id $childPid -Force -ErrorAction Ignore
                }
            }
        }
    }
}
