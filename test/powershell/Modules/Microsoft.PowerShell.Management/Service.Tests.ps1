# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# The suite is written for the Pester version restored by the CI harness (4.x) as well as
# Pester 5: setup runs at script scope instead of BeforeAll, conditional tests use It -Skip,
# and $WhatIfPreference (set by Pester 5 during discovery) keeps the setup from running twice.
$script:savedDefaultParameterValues = $PSDefaultParameterValues.Clone()
if (-not $WhatIfPreference) {
    if ($IsWindows) {
        # The service cmdlets are only compiled on Unix
        $PSDefaultParameterValues["it:skip"] = $true
    } else {
        $script:isRoot = ([int](& id -u)) -eq 0
        $script:shimDir = Join-Path ([System.IO.Path]::GetTempPath()) "pwsh-service-shims-$([guid]::NewGuid().ToString('N'))"
        $script:utilityDir = Join-Path $script:shimDir "util"
        $script:emptyDir = Join-Path $script:shimDir "empty"
        $script:logFile = Join-Path $script:shimDir "invocations.log"
        $script:originalPath = $env:PATH

        New-Item -ItemType Directory -Path $script:shimDir -Force | Out-Null
        New-Item -ItemType Directory -Path $script:utilityDir -Force | Out-Null
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
    show) stdout="$PS_SERVICE_SHIM_DEP_STDOUT"; enum=1 ;;
    --list) stdout="$PS_SERVICE_SHIM_RC_LIST_STDOUT"; enum=1 ;;
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

        $env:PS_SERVICE_SHIM_LOG = $script:logFile

        foreach ($name in "systemctl", "launchctl", "service", "rc-service", "rc-status") {
            $shimPath = Join-Path $script:shimDir $name
            Set-Content -Path $shimPath -Value ($managerShimBody -replace "`r", "")
            $null = & chmod +x $shimPath

            # Fail the setup when a shim cannot be executed (for example on a 'noexec'
            # mount). Otherwise manager discovery could fall through to the host's real
            # service manager and an elevated test run would modify real services.
            try {
                & $shimPath setup-probe 2>&1 | Out-Null
            } catch {
                throw "The service manager shim '$shimPath' is not executable: $_"
            }

            if ($LASTEXITCODE -ne 0) {
                throw "The service manager shim '$shimPath' is not executable (exit code $LASTEXITCODE)."
            }
        }

        $sudoPath = Join-Path $script:shimDir "sudo"
        Set-Content -Path $sudoPath -Value ($sudoShimBody -replace "`r", "")
        $null = & chmod +x $sudoPath

        # The tests must never reach the host's service manager or sudo, so PATH only
        # contains the shim directory and the few utility commands the shims and tests need.
        foreach ($utility in "chmod", "sleep") {
            $source = (Get-Command $utility -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
            $null = & ln -s $source (Join-Path $script:utilityDir $utility)
        }

        $script:testPath = "$script:shimDir$([System.IO.Path]::PathSeparator)$script:utilityDir"
        $env:PATH = $script:testPath
    }
}

Describe "Unix Service cmdlets" -Tags "CI" {
    # The helpers are defined in the Describe scope: script-level functions of a test
    # file are not visible from BeforeEach and It blocks under either Pester version.
    function Reset-ServiceShim {
        [CmdletBinding(SupportsShouldProcess)]
        param(
            [string]$Stdout = "",
            [string]$UnitFilesStdout = "",
            [string]$DependencyStdout = "",
            [string]$RcListStdout = "",
            [string]$Stderr = "",
            [int]$ExitCode = 0
        )

        if (-not $PSCmdlet.ShouldProcess("service manager shim invocation log and environment", "Reset shim state")) {
            return
        }

        $env:PS_SERVICE_SHIM_STDOUT = $Stdout
        $env:PS_SERVICE_SHIM_UF_STDOUT = $UnitFilesStdout
        $env:PS_SERVICE_SHIM_DEP_STDOUT = $DependencyStdout
        $env:PS_SERVICE_SHIM_RC_LIST_STDOUT = $RcListStdout
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

    function Test-SkipAsRoot {
        param([string]$Because)

        # Pester 4 evaluates 'It -Skip' when the block is created, so the flag on the test is
        # enough there. Pester 5 fixes the flag during discovery, before the setup above ran,
        # so the body requests the skip at run time instead.
        if (-not $script:isRoot) {
            return $false
        }

        if (Get-Command Set-ItResult -ErrorAction SilentlyContinue) {
            Set-ItResult -Skipped -Because $Because
        }

        return $true
    }

    AfterAll {
        $global:PSDefaultParameterValues = $script:savedDefaultParameterValues
        if ($script:originalPath) {
            $env:PATH = $script:originalPath
        }

        foreach ($variable in "PS_SERVICE_SHIM_LOG", "PS_SERVICE_SHIM_STDOUT", "PS_SERVICE_SHIM_UF_STDOUT",
            "PS_SERVICE_SHIM_DEP_STDOUT", "PS_SERVICE_SHIM_RC_LIST_STDOUT", "PS_SERVICE_SHIM_STDERR",
            "PS_SERVICE_SHIM_EXITCODE", "PS_SERVICE_SHIM_ENUM_FAILS") {
            Remove-Item "Env:\$variable" -ErrorAction SilentlyContinue
        }

        if ($script:shimDir -and (Test-Path $script:shimDir)) {
            Remove-Item $script:shimDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    BeforeEach {
        Reset-ServiceShim
        if ($script:testPath) {
            $env:PATH = $script:testPath
        }
    }

    Context "systemd (systemctl)" {
        It "Get-Service enumerates units with status, start type and description" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`nfoo.service loaded inactive dead Foo service`nfailed.service loaded failed failed Failed service`nreloading.service loaded reloading reloading Reload service`ntemplate@.service loaded active running Template unit" `
                -UnitFilesStdout "sshd.service enabled enabled`nfoo.service disabled enabled`nmasked.service masked enabled`nsshd-alias.service alias enabled"

            $services = Get-Service

            $sshd = $services | Where-Object { $_.Name -eq "sshd" }
            $sshd | Should -Not -BeNullOrEmpty
            $sshd.DisplayName | Should -Be "OpenSSH server daemon"
            $sshd.Status.ToString() | Should -Be "Running"
            $sshd.StartType.ToString() | Should -Be "Automatic"
            $sshd.UnitFileState | Should -Be "enabled"
            $sshd.SubState | Should -Be "running"

            # the default display property set is attached through PSStandardMembers
            $sshd.PSObject.Members["PSStandardMembers"] | Should -Not -BeNullOrEmpty

            $foo = $services | Where-Object { $_.Name -eq "foo" }
            $foo.Status.ToString() | Should -Be "Stopped"
            $foo.StartType.ToString() | Should -Be "Manual"

            # template units are not real services
            $services.Name | Should -Not -Contain "template@"

            # failed units are enumerated and reported as stopped
            $failed = $services | Where-Object { $_.Name -eq "failed" }
            $failed.Status.ToString() | Should -Be "Stopped"
            $failed.SubState | Should -Be "failed"

            # a unit reloading its configuration is still running
            $reloading = $services | Where-Object { $_.Name -eq "reloading" }
            $reloading.Status.ToString() | Should -Be "Running"

            # masked services come from list-unit-files only
            $masked = $services | Where-Object { $_.Name -eq "masked" }
            $masked.StartType.ToString() | Should -Be "Disabled"

            # an alias unit file is another name for a unit, not a service of its own
            $services.Name | Should -Not -Contain "sshd-alias"

            $log = Get-InvocationLog
            $log[0] | Should -Be "list-units --type=service --all --plain --no-legend --no-pager"
            $log[1] | Should -Be "list-unit-files --type=service --no-legend --no-pager"
        }

        It "Get-Service finds a single service by name with or without the unit suffix" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -UnitFilesStdout "sshd.service enabled enabled"

            (Get-Service -Name "sshd").Name | Should -Be "sshd"
            (Get-Service -Name "sshd.service").Name | Should -Be "sshd"

            # case-insensitive matches resolve to the canonical native name, so that
            # subsequent operations target the service systemd actually knows about
            (Get-Service -Name "SSHD").Name | Should -Be "sshd"
        }

        It "Get-Service supports wildcard patterns in -Name" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`nsyslog.service loaded inactive dead Syslog"

            $services = Get-Service -Name "ssh*"
            $services.Name | Should -Be "sshd"

            # the '.service'-suffixed pattern also matches
            (Get-Service -Name "sshd.service").Name | Should -Be "sshd"
        }

        It "Get-Service supports -DisplayName with wildcards" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            $services = Get-Service -DisplayName "OpenSSH*"
            $services.Name | Should -Be "sshd"
        }

        It "Get-Service supports -Include and -Exclude" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`ncron.service loaded inactive dead Cron"

            (Get-Service -Include "ssh*").Name | Should -Be "sshd"
            (Get-Service -Exclude "ssh*").Name | Should -Be "cron"
        }

        It "Get-Service writes a non-terminating error for a missing service" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            { Get-Service -Name "nosuchservice" -ErrorAction Stop } |
                Should -Throw -ErrorId "NoServiceFoundForGivenName,Microsoft.PowerShell.Commands.GetServiceCommand"
        }

        It "Stop-Service calls 'systemctl stop <unit>'" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

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

        It "Stop-Service does not invoke the manager for a service that is not running" {
            Reset-ServiceShim -Stdout "cron.service loaded inactive dead Cron"

            Stop-Service -Name "cron"

            $log = Get-InvocationLog
            @($log | Where-Object { $_ -match "stop cron\.service$" }).Count | Should -Be 0
        }

        It "Start-Service calls 'systemctl start <unit>' and supports -PassThru" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            $service = Start-Service -Name "sshd" -PassThru
            $service.Name | Should -Be "sshd"
            $service.Status.ToString() | Should -Be "Running"

            (Get-InvocationLog)[-1] | Should -Match "start sshd\.service"
        }

        It "Restart-Service calls the manager's native restart operation" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Restart-Service -Name "sshd"

            # A native restart preserves the manager's dependency semantics, for example
            # services stopped through PartOf= are restarted as well.
            $log = Get-InvocationLog
            $log | Should -Contain "restart sshd.service"
            @($log | Where-Object { $_ -match "^(stop|start) sshd\.service$" }).Count | Should -Be 0
        }

        It "Restart-Service supports -PassThru and reports manager failures" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            $service = Restart-Service -Name "sshd" -PassThru
            $service.Name | Should -Be "sshd"
            $service.Status.ToString() | Should -Be "Running"

            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "Failed to restart sshd.service" -ExitCode 1
            { Restart-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "CouldNotRestartService,Microsoft.PowerShell.Commands.RestartServiceCommand"
        }

        It "Stop-Service -NoWait stops without waiting" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Stop-Service -Name "sshd" -NoWait

            (Get-InvocationLog)[-1] | Should -Match "stop sshd\.service"
        }

        It "Stop-Service supports the pipeline and -WhatIf does not stop the service" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"

            Get-Service -Name "sshd" | Stop-Service
            (Get-InvocationLog)[-1] | Should -Match "^stop sshd\.service$"

            $services = Get-Service -Name "sshd"
            $services | Stop-Service -WhatIf
            # -WhatIf must not invoke the stop operation again; assign the log to a
            # variable first so that Where-Object counts individual invocations
            $log = Get-InvocationLog
            @($log | Where-Object { $_ -match "^stop sshd\.service$" }).Count | Should -Be 1
        }

        It "Get-Service -InputObject observes the current state of a saved service object" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon"
            $saved = Get-Service -Name "sshd"
            $saved.Status.ToString() | Should -Be "Running"

            # the service is stopped outside of PowerShell
            Reset-ServiceShim -Stdout "sshd.service loaded inactive dead OpenSSH server daemon"
            (Get-Service -InputObject $saved).Status.ToString() | Should -Be "Stopped"
        }

        It "reports a non-terminating error with stderr when the command fails" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "Failed to stop sshd.service" -ExitCode 1

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "CouldNotStopService,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "reports a manager failure mentioning a password as a manager failure" -Skip:$script:isRoot {
            if (Test-SkipAsRoot -Because "sudo is not used when running as root") { return }

            # only sudo's own diagnostics ('sudo:' prefix) mean that elevation was
            # unavailable; the wrapped command's stderr is the manager's failure
            Reset-ServiceShim -Stdout "password-store.service loaded active running Password store" -Stderr "Failed to stop password-store.service" -ExitCode 1

            { Stop-Service -Name "password-store" -ErrorAction Stop } |
                Should -Throw -ErrorId "CouldNotStopService,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "throws a terminating error when the service manager enumeration fails" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "System has not been booted with systemd" -ExitCode 1
            $env:PS_SERVICE_SHIM_ENUM_FAILS = "1"

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "System.ComponentModel.Win32Exception,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "reports a clear error when sudo requires a password" -Skip:$script:isRoot {
            if (Test-SkipAsRoot -Because "sudo is not used when running as root") { return }

            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "sudo: a password is required" -ExitCode 1

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "SudoRequired,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "reports a clear error for a localized sudo authorization failure" -Skip:$script:isRoot {
            if (Test-SkipAsRoot -Because "sudo is not used when running as root") { return }

            # the 'sudo:' prefix is the program name and is stable across locales
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "sudo: un mot de passe est nécessaire" -ExitCode 1

            { Stop-Service -Name "sshd" -ErrorAction Stop } |
                Should -Throw -ErrorId "SudoRequired,Microsoft.PowerShell.Commands.StopServiceCommand"
        }

        It "reports SudoRequired when sudo is not available and the user is not root" -Skip:$script:isRoot {
            if (Test-SkipAsRoot -Because "sudo is not used when running as root") { return }

            $sudoPath = Join-Path $script:shimDir "sudo"
            Move-Item $sudoPath "$sudoPath.bak"
            # restrict PATH to the shim directory so that the host's real sudo is not
            # found either
            $savedPath = $env:PATH
            $env:PATH = $script:shimDir
            try {
                Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" -Stderr "Access denied" -ExitCode 1

                { Stop-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "SudoRequired,Microsoft.PowerShell.Commands.StopServiceCommand"
            } finally {
                Move-Item "$sudoPath.bak" $sudoPath
                $env:PATH = $savedPath
            }
        }

        It "Get-Service -DependentServices lists the services that directly depend on the service" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`nnetwork.service loaded active running Network" `
                -UnitFilesStdout "sshd.service enabled enabled" `
                -DependencyStdout "network.service sshd.socket`nsysinit.target"

            $dependents = Get-Service -Name "sshd" -DependentServices
            $dependents.Name | Should -Contain "network"
            $dependents.Name | Should -Not -Contain "sysinit"
        }

        It "Get-Service -RequiredServices lists the direct dependencies of the service" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`nnetwork.service loaded active running Network" `
                -UnitFilesStdout "sshd.service enabled enabled" `
                -DependencyStdout "`nnetwork.service basic.target"

            $required = Get-Service -Name "sshd" -RequiredServices
            $required.Name | Should -Be "network"
        }

        It "the RequiredServices property is available on service objects" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon`nnetwork.service loaded active running Network" `
                -DependencyStdout "network.service"

            # Windows exposes RequiredServices through type data; the Unix type provides
            # the same property directly
            (Get-Service -Name "sshd").RequiredServices.Name | Should -Contain "network"
        }

        It "dependency lookups skip units the service manager does not know" {
            Reset-ServiceShim -Stdout "sshd.service loaded active running OpenSSH server daemon" `
                -DependencyStdout "missing.service"

            $dependents = @(Get-Service -Name "sshd" -DependentServices)
            $dependents.Count | Should -Be 0
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
                Reset-ServiceShim -Stdout "PID`tStatus`tLabel`n-`t0`tcom.example.stopped`n123`t0`tcom.example.running"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "com.example.running" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "com.example.stopped" }).Status.ToString() | Should -Be "Stopped"

                (Get-InvocationLog)[0] | Should -Be "list"

                Start-Service -Name "com.example.running"
                Stop-Service -Name "com.example.running"
                Restart-Service -Name "com.example.running"

                # mutating operations use the launchctl domain of the current user
                # (system for root, gui/<uid> otherwise) and are sudo-wrapped when
                # not running as root.
                $log = Get-InvocationLog
                $ops = @($log | Where-Object { $_ -match "com\.example\.running" })
                if ($script:isRoot) {
                    $ops.Count | Should -Be 3
                    $ops[0] | Should -Be "kickstart system/com.example.running"
                    $ops[1] | Should -Be "kill SIGTERM system/com.example.running"
                    $ops[2] | Should -Be "kickstart -k system/com.example.running"
                } else {
                    # the sudo shim logs its own invocation, then the exec'd shim logs
                    # the payload, so the lines alternate
                    $ops.Count | Should -Be 6
                    $ops[0] | Should -Match '^-n .+launchctl kickstart gui/\d+/com\.example\.running$'
                    $ops[1] | Should -Match '^kickstart gui/\d+/com\.example\.running$'
                    $ops[2] | Should -Match '^-n .+launchctl kill SIGTERM gui/\d+/com\.example\.running$'
                    $ops[3] | Should -Match '^kill SIGTERM gui/\d+/com\.example\.running$'
                    $ops[4] | Should -Match '^-n .+launchctl kickstart -k gui/\d+/com\.example\.running$'
                    $ops[5] | Should -Match '^kickstart -k gui/\d+/com\.example\.running$'
                }

                # a loaded job without a running process is already stopped: stopping it
                # neither fails ('No such process') nor invokes the manager
                Stop-Service -Name "com.example.stopped" -ErrorAction Stop
                $log = Get-InvocationLog
                @($log | Where-Object { $_ -match "com\.example\.stopped" }).Count | Should -Be 0
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                $env:PATH = $script:testPath
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
                Reset-ServiceShim -Stdout " * sshd                                                                                    [ started ]`n * cron                                                                                     [ stopped ]`n * legacy                                                                                   [ started 00:12:34 (0) ]" `
                    -RcListStdout "sshd`ncron`nnetmount"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "sshd" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "cron" }).Status.ToString() | Should -Be "Stopped"

                # OpenRC appends uptime details to the state of supervised services
                ($services | Where-Object { $_.Name -eq "legacy" }).Status.ToString() | Should -Be "Running"

                # 'rc-status' omits installed services that belong to no runlevel; they
                # come from 'rc-service --list' and are reported as stopped
                ($services | Where-Object { $_.Name -eq "netmount" }).Status.ToString() | Should -Be "Stopped"

                $log = Get-InvocationLog
                $log[0] | Should -Be "--all"
                $log[1] | Should -Be "--list"

                # mutating operations take the service name before the action verb
                Stop-Service -Name "sshd"
                Start-Service -Name "netmount"
                Restart-Service -Name "sshd"

                $log = Get-InvocationLog
                $ops = @($log | Where-Object { $_ -match "sshd stop|netmount start|sshd restart" })
                if ($script:isRoot) {
                    $ops.Count | Should -Be 3
                    $ops[0] | Should -Be "sshd stop"
                    $ops[1] | Should -Be "netmount start"
                    $ops[2] | Should -Be "sshd restart"
                } else {
                    $ops.Count | Should -Be 6
                    $ops[0] | Should -Match '^-n .+rc-service sshd stop$'
                    $ops[1] | Should -Be "sshd stop"
                    $ops[2] | Should -Match '^-n .+rc-service netmount start$'
                    $ops[3] | Should -Be "netmount start"
                    $ops[4] | Should -Match '^-n .+rc-service sshd restart$'
                    $ops[5] | Should -Be "sshd restart"
                }
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                Move-Item "$launchctlShim.bak" $launchctlShim
                Move-Item "$serviceShim.bak" $serviceShim
                $env:PATH = $script:testPath
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
                Reset-ServiceShim -Stderr "[ + ]  sshd`n[ - ]  cron"

                $services = Get-Service
                ($services | Where-Object { $_.Name -eq "sshd" }).Status.ToString() | Should -Be "Running"
                ($services | Where-Object { $_.Name -eq "cron" }).Status.ToString() | Should -Be "Stopped"

                (Get-InvocationLog)[0] | Should -Be "--status-all"

                # mutating operations take the service name before the action verb
                Restart-Service -Name "sshd"
                Stop-Service -Name "sshd"

                $log = Get-InvocationLog
                $ops = @($log | Where-Object { $_ -match "sshd" })
                if ($script:isRoot) {
                    $ops.Count | Should -Be 2
                    $ops[0] | Should -Be "sshd restart"
                    $ops[1] | Should -Be "sshd stop"
                } else {
                    $ops.Count | Should -Be 4
                    $ops[0] | Should -Match '^-n .+service sshd restart$'
                    $ops[1] | Should -Be "sshd restart"
                    $ops[2] | Should -Match '^-n .+service sshd stop$'
                    $ops[3] | Should -Be "sshd stop"
                }
            } finally {
                Move-Item "$systemctlShim.bak" $systemctlShim
                Move-Item "$launchctlShim.bak" $launchctlShim
                Move-Item "$rcServiceShim.bak" $rcServiceShim
                $env:PATH = $script:testPath
            }
        }

        It "throws a terminating error when no service manager is found" {
            $savedPath = $env:PATH
            try {
                $env:PATH = $script:emptyDir

                { Get-Service -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.GetServiceCommand"

                # named invocations must also report the missing manager, not a missing service
                { Get-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.GetServiceCommand"

                # every service cmdlet translates the missing manager the same way
                { Start-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.StartServiceCommand"
                { Stop-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.StopServiceCommand"
                { Restart-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.RestartServiceCommand"
            } finally {
                $env:PATH = $savedPath
            }
        }

        It "skips non-executable manager commands during discovery" -Skip:$script:isRoot {
            if (Test-SkipAsRoot -Because "root can execute files regardless of the execute bit") { return }

            $dir = Join-Path $script:shimDir "nonexec"
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Copy-Item (Join-Path $script:shimDir "systemctl") (Join-Path $dir "systemctl")
            $null = & chmod -x (Join-Path $dir "systemctl")
            $savedPath = $env:PATH
            $env:PATH = $dir
            try {
                # a file without the execute bit is not a usable manager command; discovery
                # must skip it and report the missing manager instead of failing to launch
                { Get-Service -Name "sshd" -ErrorAction Stop } |
                    Should -Throw -ErrorId "NoServiceManagerFound,Microsoft.PowerShell.Commands.GetServiceCommand"
            } finally {
                $env:PATH = $savedPath
                Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "cancellation" {
        It "cancels a blocking service manager command when the pipeline is stopped" {
            $dir = Join-Path $script:shimDir "blocking"
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            $blockingBody = @'
#!/bin/bash
case "$1" in
    list-unit-files|show) exit 0 ;;
    list-units) printf 'blocking.service loaded active running Blocking\n'; exit 0 ;;
    stop) sleep 60 ;;
    *) exit 0 ;;
esac
'@
            $shimPath = Join-Path $dir "systemctl"
            Set-Content -Path $shimPath -Value ($blockingBody -replace "`r", "")
            $null = & chmod +x $shimPath
            $savedPath = $env:PATH
            # prepend the blocking directory so the shim's `sleep` and the sudo shim stay
            # available; a full restriction would fail the command before it blocks
            $env:PATH = "$dir$([System.IO.Path]::PathSeparator)$savedPath"
            $ps = $null
            try {
                $ps = [PowerShell]::Create()
                $null = $ps.AddScript('Stop-Service -Name blocking -WarningAction SilentlyContinue -ErrorAction SilentlyContinue')
                $async = $ps.BeginInvoke()
                # wait until the blocking stop command is in flight, then stop the pipeline;
                # request the stop asynchronously so the bounded wait below can detect a
                # missing cancellation instead of blocking on it
                Start-Sleep -Seconds 2
                $null = $ps.BeginStop($null, $null)
                $completed = $async.AsyncWaitHandle.WaitOne(15000)
                $completed | Should -BeTrue
                $ps.InvocationStateInfo.State | Should -BeIn @("Completed", "Stopped")
            } finally {
                if ($ps) {
                    $ps.Stop()
                    $ps.Dispose()
                }

                $env:PATH = $savedPath
                Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
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
