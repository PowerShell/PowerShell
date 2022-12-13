# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# unit tests for telemetry
# these tests aren't going to check that telemetry is being sent
# only that we're not treating the telemetry.uuid file correctly

Describe "Telemetry for shell startup" -Tag CI {
    BeforeAll {
        # if the telemetry file exists, move it out of the way
        # the member is internal, but we can retrieve it via reflection
        $cacheDir = [System.Management.Automation.Platform].GetField("CacheDirectory","NonPublic,Static").GetValue($null)
        $uuidPath = Join-Path -Path $cacheDir -ChildPath telemetry.uuid
        $uuidFileExists = Test-Path -Path $uuidPath
        if ( $uuidFileExists ) {
            $originalBytes = Get-Content -AsByteStream -Path $uuidPath
            Rename-Item -Path $uuidPath -NewName "${uuidPath}.original"
        }

        $PWSH = (Get-Process -Id $PID).MainModule.FileName
        $telemetrySet = Test-Path -Path env:POWERSHELL_TELEMETRY_OPTOUT
        $SendingTelemetry = $env:POWERSHELL_TELEMETRY_OPTOUT
    }

    AfterAll {
        # check and reset the telemetry.uuid file
        if ( $uuidFileExists ) {
            if ( Test-Path -Path "${uuidPath}.original" ) {
                Rename-Item -NewName $uuidPath -Path "${uuidPath}.original" -Force
            }
            else {
                [System.IO.File]::WriteAllBytes($uuidPath, $originalBytes)
            }
        }
        if ( $telemetrySet ) {
            $env:POWERSHELL_TELEMETRY_OPTOUT = $SendingTelemetry
        }
    }

    AfterEach {
        if ( Test-Path -Path $uuidPath ) {
            Remove-Item -Path $uuidPath
        }
        if ( Test-Path -Path env:POWERSHELL_TELEMETRY_OPTOUT ) {
            Remove-Item env:POWERSHELL_TELEMETRY_OPTOUT
        }
    }

    It "Should not create a uuid file if telemetry is opted out" {
        $env:POWERSHELL_TELEMETRY_OPTOUT = 1
        & $PWSH -NoProfile -Command "exit"
        $uuidPath  | Should -Not -Exist
    }

    It "Should create a uuid file if telemetry is opted in" {
        $env:POWERSHELL_TELEMETRY_OPTOUT = "no"
        & $PWSH -NoProfile -Command "exit"
        $uuidPath  | Should -Exist
    }

    It "Should create a uuid file by default" {
        if ( Test-Path env:POWERSHELL_TELEMETRY_OPTOUT ) { Remove-Item -Path env:POWERSHELL_TELEMETRY_OPTOUT }
        & $PWSH -NoProfile -Command "exit"
        $uuidPath  | Should -Exist
    }

    It "Should create a property uuid file when telemetry is sent" {
        $env:POWERSHELL_TELEMETRY_OPTOUT = "no"
        & $PWSH -NoProfile -Command "exit"
        $uuidPath  | Should -Exist
        (Get-ChildItem -Path $uuidPath).Length | Should -Be 16
        [byte[]]$newBytes = Get-Content -AsByteStream -Path $uuidPath
        [System.Guid]::New($newBytes) | Should -BeOfType System.Guid
    }

    It "Should not create a telemetry file if one already exists and telemetry is opted in" {
        [byte[]]$bytes = [System.Guid]::NewGuid().ToByteArray()
        [System.IO.File]::WriteAllBytes($uuidPath, $bytes)
        & $PWSH -NoProfile -Command "exit"
        [byte[]]$newBytes = Get-Content -AsByteStream -Path $uuidPath
        Compare-Object -ReferenceObject $bytes -DifferenceObject $newBytes | Should -BeNullOrEmpty
    }

    It "Should create a new telemetry file if the current one is 00000000-0000-0000-0000-000000000000" {
        [byte[]]$zeroGuid = [System.Guid]::Empty.ToByteArray()
        [System.IO.File]::WriteAllBytes($uuidPath, $zeroGuid)
        & $PWSH -NoProfile -Command "exit"
        [byte[]]$newBytes = Get-Content -AsByteStream -Path $uuidPath
        # we could legitimately have zeros in the new guid, so we can't check for that
        # we're just making sure that there *is* a difference
        Compare-Object -ReferenceObject $zeroGuid -DifferenceObject $newBytes | Should -Not -BeNullOrEmpty
    }

    It "Should create a new telemetry file if the current one is smaller than 16 bytes" {
        $badBytes = [byte[]]::new(8);
        [System.IO.File]::WriteAllBytes($uuidPath, $badBytes)
        & $PWSH -NoProfile -Command "exit"
        [byte[]]$nb = Get-Content -AsByteStream -Path $uuidPath
        [System.Guid]::New($nb) | Should -BeOfType System.Guid
    }

    It "Should not create a new telemetry file if the current one has a valid guid and is larger than 16 bytes" {
        $g = [Guid]::newGuid()
        $tooManyBytes = $g.ToByteArray() * 2
        [System.IO.File]::WriteAllBytes($uuidPath, $tooManyBytes)
        [byte[]]$nb = Get-Content -Path $uuidPath -AsByteStream | Select-Object -First 16
        $ng = [System.Guid]::new($nb)
        $g | Should -Be $ng
    }

    It "Should properly set whether telemetry is sent based on when environment variable is not set" {
        $result = & $PWSH -NoProfile -Command '[Microsoft.PowerShell.Telemetry.ApplicationInsightsTelemetry]::CanSendTelemetry'
        $result | Should -Be "True"
    }

    $telemetryIsSetData = @(
        @{ name = "set to no"; Value = "no" ; expectedValue = "True" }
        @{ name = "set to 0"; Value = "0"; expectedValue = "True" }
        @{ name = "set to false"; Value = "false"; expectedValue = "True" }
        @{ name = "set to yes"; Value = "yes"; expectedValue = "False" }
        @{ name = "set to 1"; Value = "1"; expectedValue = "False" }
        @{ name = "set to true"; Value = "true"; expectedValue = "False" }
    )

    It "Should properly set whether telemetry is sent based on environment variable when <name>" -TestCases $telemetryIsSetData {
        param ( [string]$name, [string]$value, [string]$expectedValue )
        $env:POWERSHELL_TELEMETRY_OPTOUT = $value
        $result = & $PWSH -NoProfile -Command '[Microsoft.PowerShell.Telemetry.ApplicationInsightsTelemetry]::CanSendTelemetry'
        $result | Should -Be $expectedValue
    }

    It "Should send startup event" {
        $resultJson = & $PWSH -NoProfile -c {
            # this should ensure that the startup telemetry event is sent.
            $null = Get-Date | Out-String
            $telemetryType = [Microsoft.PowerShell.Telemetry.ApplicationInsightsTelemetry]
            $bindingFlags = [System.Reflection.BindingFlags]"NonPublic,Static"
            $observedValue = ${telemetryType}.GetMember("s_startupEventSent", $bindingFlags)[0].GetValue($null)
            $observedValue | Should -Be 1  -Because "Should have sent telemetry on console startup"
        }
    }
}
