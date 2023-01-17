# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Native commands will properly call a JSON adapter" -Tag "CI" {
    BeforeAll {
        # no useful overlap in commands, we have use specific commands for each platform
        $winCmd = "whoami"
        $winArguments = "/USER","FO","CSV"

        $nixCmd = "hostname"
        $nixArguments = "-f"

        # start with a clean slate
        if (test-path "function:${cmd}-json") {
            Remove-Item -Force "function:${cmd}-json"
        }

        $platformArguments = $IsWindows ? $winArguments : $nixArguments
        $cmd = $IsWindows ? $winCmd : $nixCmd

        # adapters
        $psAdapterName = "hostname-json.ps1"
        $winAdapterName = "whoami-json.bat"
        $nixAdapterName = "hostname-json.sh"
        $nativeAdapterName = $IsWindows ? $winAdapterName : $nixAdapterName

        # path to the adapter
        $psAdapterPath = Join-Path -Path $PSScriptRoot -ChildPath assets -AdditionalChildPath $psAdapterName
        $winAdapterPath = Join-Path -Path $PSScriptRoot -ChildPath assets -AdditionalChildPath $winAdapterName
        $nixAdapterPath = Join-Path -Path $PSScriptRoot -ChildPath assets -AdditionalChildPath $nixAdapterName
        $nativeAdapterPath = $IsWindows ? $winAdapterPath : $nixAdapterPath

        # the scriptblock we use for the powershell adapter
        $adapterSB = [scriptblock]::Create((Get-Content -Raw $psAdapterPath))

        # All the adapters should return the same data, except with regard to the passed arguments
        # so the expected results will differ based on platform
        $expectedResults = & $cmd ${platformArguments} | & $adapterSB $cmd ${platformArguments}
    }

    BeforeEach {
        $originalPath = $env:PATH
        ${env:PATH} = "${TESTDRIVE}" + [io.path]::PathSeparator + "${env:PATH}"
    }

    AfterEach {
        ${env:PATH} = $originalPath
    }

    Context "A JSON adapter exists" {

        It "will call a function adapter if it exists" {
            # create the adapter function
            Set-Content -Path "function:${cmd}-json" -Value $adapterSB

            # collect the results
            $observedResult = & $cmd ${platformArguments}
            $observedResult | Should -Be $expectedResults
        }

        It "A powershell script will be called if it is in the Path" {
            # copy the adapter script to the path
            Copy-Item -Path $psAdapterPath -Destination ${TESTDRIVE} -Force

            # collect the results
            $observedResult = & $cmd ${platformArguments}
            $observedResult | Should -Be $expectedResults
        }

        It "A native script will be called if it is in the path" {
            # copy the adapter script to the path
            Copy-Item -Path $nativeAdapterPath -Destination ${TESTDRIVE} -Force

            # collect the results
            $observedResult = & $cmd ${platformArguments}
            $observedResult | Should -Be $expectedResults
        }
    }

    Context "JSON Adapter History" {
        BeforeEach {
            $setting = [system.management.automation.psinvocationsettings]::New()
            $setting.AddToHistory = $true
            $ps = [PowerShell]::Create("NewRunspace")
        }

        AfterEach {
            If (Test-Path "function:${cmd}-json") {
                Remove-Item -Force "function:${cmd}-json"
            }
            $nativeAdapterTestPath = Join-Path ${TESTDRIVE} $nativeAdapterName
            if (Test-Path $nativeAdapterTestPath) {
                Remove-Item -Force $nativeAdapterTestPath
            }
            $ps.Dispose()
        }

        It "History has the new property with the correct value for a function adapter" {
            # create the adapter function
            Set-Content -Path "function:${cmd}-json" -Value $adapterSB
            $ps.AddScript("$cmd ${platformArguments}").Invoke($null, $setting)
            $ps.Commands.Clear()
            $history = $ps.AddCommand("Get-History").AddParameter("Count", 1).Invoke()
            $history.ConstructedPipeline | Should -Match "| ${cmd}-json$"
        }

        It "History has the new property with the correct value for powershell script adapter" {
            Copy-Item $psAdapterPath $TESTDRIVE -Force
            $ps.AddScript("$cmd ${platformArguments}").Invoke($null, $setting)
            $ps.Commands.Clear()
            $history = $ps.AddCommand("Get-History").AddParameter("Count", 1).Invoke()
            $history.ConstructedPipeline | Should -Match " | ${psAdapterName}$"
        }

        It "History has the new property with the correct value for native script adapter '$nativeAdapterName'" {
            Copy-Item $nativeAdapterPath $TESTDRIVE -Force
            $ps.AddScript("$cmd ${platformArguments}").Invoke($null, $setting)
            $ps.Commands.Clear()
            $history = $ps.AddCommand("Get-History").AddParameter("Count", 1).Invoke()
            $history.ConstructedPipeline | Should -Match " | ${nativeAdapterName}$"
        }
    }

}
