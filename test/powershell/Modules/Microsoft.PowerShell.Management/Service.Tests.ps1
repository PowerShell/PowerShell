# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Unix Service cmdlets" -Tags "CI" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ($IsWindows) {
            # The service cmdlets are only compiled on Unix
            $PSDefaultParameterValues["it:skip"] = $true
            return
        }

        $script:isRoot = ([int](& id -u)) -eq 0
        $script:shimDir = Join-Path ([System.IO.Path]::GetTempPath()) "pwsh-service-shims-$([guid]::NewGuid().ToString('N'))"
        $script:emptyDir = Join-Path $script:shimDir "empty"
        $script:logFile = Join-Path $script:shimDir "invocations.log"
        $script:originalPath = $env:PATH

        New-Item -ItemType Directory -Path $script:shimDir -Force | Out-Null
        New-Item -ItemType Directory -Path $script:emptyDir -Force | Out-Null

        # The shim uses '/bin/bash' directly (not '/usr/bin/env bash') so that it also
        # works when PATH is restricted to the shim directory. Enumeration commands
        # always exit 0 unless PS_SERVICE_SHIM_ENUM_FAILS is set, so that operation
        # failures can be tested independently of service enumeration.
        $managerShimBody = @'
#!/bin/bash
printf '%s\n' "$*" >> "$PS_SERVICE_SHIM_LOG"
case "$1" in
    list-unit-files) stdout="$PS_SERVICE_SHIM_UF_STDOUT"; enum=1 ;;
    list-dependencies) stdout="$PS_SERVICE_SHIM_DEP_STDOUT"; enum=1 ;;
    list-units|list|--all|--status-all) stdout="$PS_SERVICE_SHIM_STDOUT"; enum=1 ;;
    *) stdout="$PS_SERVICE_SHIM_STDOUT" ;;
esac
if [ -n "$stdout" ]; then printf '%s' "$stdout"; fi
if [ -n "$PS_SERVICE_SHIM_STDERR" ]; then printf '%s' "$PS_SERVICE_SHIM_STDERR" >&2; fi
code="${PS_SERVICE_SHIM_EXITCODE:-0}"
if [ "$enum" = "1" ] && [ -z "$PS_SERVICE_SHIM_ENUM_FAILS" ]; then code=0; fi
exit "$code"
'@

        $sudoShimBody = @'
#!/bin/bash
printf '%s\n' "$*" >> "$PS_SERVICE_SHIM_LOG"
exec "${@:2}"
'@

        foreach ($name in "systemctl", "launchctl", "service", "rc-service") {
            $shimPath = Join-Path $script:shimDir $name
            Set-Content -Path $shimPath -Value ($managerShimBody -replace "`r", "")
            $null = & chmod +x $shimPath
        }

        $sudoPath = Join-Path $script:shimDir "sudo"
        Set-Content -Path $sudoPath -Value ($sudoShimBody -replace "`r", "")
        $null = & chmod +x $sudoPath

        $env:PATH = "$script:shimDir$([System.IO.Path]::PathSeparator)$env:PATH"
        $env:PS_SERVICE_SHIM_LOG = $script:logFile

        function Reset-Shim {
            param(
                [string]$Stdout = "",
                [string]$UnitFilesStdout = "",
                [string]$DependencyStdout = "",
                [string]$Stderr = "",
                [int]$ExitCode = 0
            )

            $env:PS_SERVICE_SHIM_STDOUT = $Stdout
            $env:PS_SERVICE_SHIM_UF_STDOUT = $UnitFilesStdout
            $env:PS_SERVICE_SHIM_DEP_STDOUT = $DependencyStdout
            $env:PS_SERVICE_SHIM_STDERR = $Stderr
            $env:PS_SERVICE_SHIM_EXITCODE = "$ExitCode"
            $env:PS_SERVICE_SHIM_ENUM_FAILS = ""
            if (Test-Path $script:logFile) {
                Remove-Item $script:logFile -Force
            }
        }

        function Get-InvocationLog {
            if (Test-Path $script:logFile) {
                # the unary comma preserves the array when the file has a single line
                return , @(Get-Content $script:logFile)
            }

            return , @()
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        if ($script:originalPath) {
            $env:PATH = $script:originalPath
        }

        Remove-Item Env:\PS_SERVICE_SHIM_LOG -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_STDOUT -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_UF_STDOUT -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_DEP_STDOUT -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_STDERR -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_EXITCODE -ErrorAction SilentlyContinue
        Remove-Item Env:\PS_SERVICE_SHIM_ENUM_FAILS -ErrorAction SilentlyContinue
        if ($script:shimDir -and (Test-Path $script:shimDir)) {
            Remove-Item $script:shimDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    BeforeEach {
        Reset-Shim
    }

    Context "systemd (systemctl)" {
        It "Get-Service enumerates units with status, start type and description" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon`nfoo.service loaded inactive dead Foo service`ntemplate@.service loaded active running Template unit" `
                -UnitFilesStdout "sshd.service enabled enabled`nfoo.service disabled enabled`nmasked.service masked enabled"

            $services = Get-Service

            $sshd = $services | Where-Object { $_.Name -eq "sshd" }
            $sshd | Should -Not -BeNullOrEmpty
            $sshd.DisplayName | Should -Be "OpenSSH server daemon"
            $sshd.Status.ToString() | Should -Be "Running"
            $sshd.StartType.ToString() | Should -Be "Automatic"
            $sshd.UnitFileState | Should -Be "enabled"
            $sshd.SubState | Should -Be "running"

            $foo = $services | Where-Object { $_.Name -eq "foo" }
            $foo.Status.ToString() | Should -Be "Stopped"
            $foo.StartType.ToString() | Should -Be "Disabled"

            # template units are not real services
            $services.Name | Should -Not -Contain "template@"

            # masked services come from list-unit-files only
            $masked = $services | Where-Object { $_.Name -eq "masked" }
            $masked.StartType.ToString() | Should -Be "Disabled"

            $log = Get-InvocationLog
            $log[0] | Should -Be "list-units --type=service --all --no-legend --no-pager"
            $log[1] | Should -Be "list-unit-files --type=service --no-legend --no-pager"
        }

        It "Get-Service finds a single service by name with or without the unit suffix" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon" -UnitFilesStdout "sshd.service enabled enabled"

            (Get-Service -Name "sshd").Name | Should -Be "sshd"
            (Get-Service -Name "sshd.service").Name | Should -Be "sshd"
        }

        It "Get-Service supports wildcard patterns in -Name" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon`nsyslog.service loaded inactive dead Syslog"

            $services = Get-Service -Name "ssh*"
            $services.Name | Should -Be "sshd"

            # the '.service'-suffixed pattern also matches
            (Get-Service -Name "sshd.service").Name | Should -Be "sshd"
        }

        It "Get-Service supports -DisplayName with wildcards" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            $services = Get-Service -DisplayName "OpenSSH*"
            $services.Name | Should -Be "sshd"
        }

        It "Get-Service supports -Include and -Exclude" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon`ncron.service loaded inactive dead Cron"

            (Get-Service -Include "ssh*").Name | Should -Be "sshd"
            (Get-Service -Exclude "ssh*").Name | Should -Be "cron"
        }

        It "Get-Service writes a non-terminating error for a missing service" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            { Get-Service -Name "nosuchservice" -ErrorAction Stop } |
                Should -Throw -ErrorId "NoServiceFoundForGivenName,Microsoft.PowerShell.Commands.GetServiceCommand"
        }

        It "Stop-Service calls 'systemctl stop <unit>'" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Stop-Service -Name "sshd"

            $log = Get-InvocationLog
            $expected = "stop sshd.service"
            if ($script:isRoot) {
                $log[-1] | Should -Be $expected
            } else {
                # the operation is wrapped with 'sudo -n' when not running as root
                $log[-2] | Should -Be "-n $script:shimDir/systemctl $expected"
                $log[-1] | Should -Be $expected
            }
        }

        It "Start-Service calls 'systemctl start <unit>' and supports -PassThru" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            $service = Start-Service -Name "sshd" -PassThru
            $service.Name | Should -Be "sshd"
            $service.Status.ToString() | Should -Be "Running"

            (Get-InvocationLog)[-1] | Should -Match "start sshd\.service"
        }

        It "Restart-Service calls 'systemctl restart <unit>'" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Restart-Service -Name "sshd"

            (Get-InvocationLog)[-1] | Should -Match "restart sshd\.service"
        }

        It "Stop-Service -NoWait prepends --no-block" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Stop-Service -Name "sshd" -NoWait

            (Get-InvocationLog)[-1] | Should -Match "--no-block stop sshd\.service"
        }

        It "Stop-Service supports the pipeline and -WhatIf does not invoke the manager" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Get-Service -Name "sshd" | Stop-Service
            (Get-InvocationLog)[-1] | Should -Match "stop sshd\.service"

            $services = Get-Service -Name "sshd"
            $countAfterEnumeration = (Get-InvocationLog).Count
            $services | Stop-Service -WhatIf
            (Get-InvocationLog).Count | Should -Be $countAfterEnumeration
        }

        It "reports a non-terminating error with stderr when the command fails" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "Failed to stop sshd.service" -ExitCode 1

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "CouldNotStopService,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "throws a terminating error when the service manager enumeration fails" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "System has not been booted with systemd" -ExitCode 1
            $env:PS_SERVICE_SHIM_ENUM_FAILS = "1"

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "ServiceEnumerationFailed,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "reports a clear error when sudo requires a password" {
            if ($script:isRoot) {
                Set-ItResult -Skipped -Because "sudo is not used when running as root"
                return
            }

            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "sudo: a password is required" -ExitCode 1

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "SudoRequired,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "Get-Service -DependentServices lists services depending on the service" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon`nnetwork.service loaded active running Network" `
                -UnitFilesStdout "sshd.service enabled enabled" `
                -DependencyStdout "network.service`nsshd.socket"

            $dependents = Get-Service -Name "sshd" -DependentServices
            $dependents.Name | Should -Contain "network"
        }

        It "Get-Service -RequiredServices lists services the service depends on" {
            Reset-Shim -Stdout "sshd.service loaded active running OpenSSH server daemon`nnetwork.service loaded active running Network" `
                -UnitFilesStdout "sshd.service enabled enabled" `
                -DependencyStdout "network.service"

            $required = Get-Service -Name "sshd" -RequiredServices
            $required.Name | Should -Contain "network"
        }
    }

    Context "fallback service managers" {
        It "uses launchctl when systemctl is not available" {
            $systemctlShim = Join-Path $script:shimDir "systemctl"
            Move-Item $systemctlShim "$systemctlShim.bak"
            # restrict PATH to the shim directory so that the real systemctl on the
            # system is not found either
            $env:PATH = $script:shimDir
            try {
                Reset-Shim -Stdout "PID`tStatus`tLabel`n-`t0`tcom.example.stopped`n123`t0`tcom.example.running"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "com.example.running" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "com.example.stopped" }).Status.ToString() | Should -Be "Stopped"

                (Get-InvocationLog)[0] | Should -Be "list"
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                $env:PATH = "$script:shimDir$([System.IO.Path]::PathSeparator)$script:originalPath"
            }
        }

        It "uses rc-service when systemctl, launchctl and service are not available" {
            $systemctlShim = Join-Path $script:shimDir "systemctl"
            $launchctlShim = Join-Path $script:shimDir "launchctl"
            $serviceShim = Join-Path $script:shimDir "service"
            Move-Item $systemctlShim "$systemctlShim.bak"
            Move-Item $launchctlShim "$launchctlShim.bak"
            Move-Item $serviceShim "$serviceShim.bak"
            $env:PATH = $script:shimDir
            try {
                Reset-Shim -Stdout " * sshd                                                                                    [ started ]`n * cron                                                                                     [ stopped ]"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "sshd" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "cron" }).Status.ToString() | Should -Be "Stopped"

                (Get-InvocationLog)[0] | Should -Be "--all"
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                Move-Item "$launchctlShim.bak" $launchctlShim
                Move-Item "$serviceShim.bak" $serviceShim
                $env:PATH = "$script:shimDir$([System.IO.Path]::PathSeparator)$script:originalPath"
            }
        }

        It "parses the output of 'service --status-all' written to stderr" {
            $systemctlShim = Join-Path $script:shimDir "systemctl"
            $launchctlShim = Join-Path $script:shimDir "launchctl"
            $rcServiceShim = Join-Path $script:shimDir "rc-service"
            Move-Item $systemctlShim "$systemctlShim.bak"
            Move-Item $launchctlShim "$launchctlShim.bak"
            Move-Item $rcServiceShim "$rcServiceShim.bak"
            $env:PATH = $script:shimDir
            try {
                Reset-Shim -Stderr "[ + ]  sshd`n[ - ]  cron"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "sshd" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "cron" }).Status.ToString() | Should -Be "Stopped"

                (Get-InvocationLog)[0] | Should -Be "--status-all"
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                Move-Item "$launchctlShim.bak" $launchctlShim
                Move-Item "$rcServiceShim.bak" $rcServiceShim
                $env:PATH = "$script:shimDir$([System.IO.Path]::PathSeparator)$script:originalPath"
            }
        }

        It "throws a terminating error when no service manager is found" {
            $savedPath = $env:PATH
            try {
                $env:PATH = $script:emptyDir

                { Get-Service -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.GetServiceCommand"
            } finally {
                $env:PATH = $savedPath
            }
        }
    }

    Context "aliases" {
        It "gsv, sasv and spsv are available on Unix" {
            (Get-Alias gsv).Definition | Should -Be "Get-Service"
            (Get-Alias sasv).Definition | Should -Be "Start-Service"
            (Get-Alias spsv).Definition | Should -Be "Stop-Service"
        }
    }
}
