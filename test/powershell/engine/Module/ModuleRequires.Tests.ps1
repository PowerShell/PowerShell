Describe "Test '#requires' module directive" -tags "CI" {

    Context "'#requires -OS'" {
        BeforeAll {
            function shouldLoad ($OS) {
                switch ($OS)
                {
                    "Windows"   { return [System.Management.Automation.Platform]::isWindows }
                    "Linux"     { return [System.Management.Automation.Platform]::isLinux }
                    "OSX"       { return [System.Management.Automation.Platform]::isOSX }
                    "Unix"      { return [System.Management.Automation.Platform]::isLinux -or [System.Management.Automation.Platform]::isOSX }
                    "Inbox"     { return [System.Management.Automation.Platform]::IsIoT   -or [System.Management.Automation.Platform]::IsNanoServer }
                    "IoT"       { return [System.Management.Automation.Platform]::IsIoT }
                    "Nano"      { return [System.Management.Automation.Platform]::IsNanoServer }
                    default     { throw "Wrong OS argument value." }
                }
            }
        }

        It "Directive '<moduleCode>' works well" -TestCases (
            @{ moduleCode = "#requires -OS Windows"; shouldModuleLoad = shouldLoad("Windows")   },
            @{ moduleCode = "#requires -OS Linux";   shouldModuleLoad = shouldLoad("Linux")     },
            @{ moduleCode = "#requires -OS OSX";     shouldModuleLoad = shouldLoad("OSX")       },
            @{ moduleCode = "#requires -OS Unix";    shouldModuleLoad = shouldLoad("Unix")      },
            @{ moduleCode = "#requires -OS Inbox";   shouldModuleLoad = shouldLoad("Inbox")     },
            @{ moduleCode = "#requires -OS IoT";     shouldModuleLoad = shouldLoad("IoT")       },
            @{ moduleCode = "#requires -OS Nano";    shouldModuleLoad = shouldLoad("Nano")      }
        ) {
            param ($moduleCode, $shouldModuleLoad)

            $testModulePath = Join-Path $TestDrive "moduleTest-$(New-GUID).psm1"
            Set-Content -Path $testModulePath -Value $moduleCode -Force

            if ($shouldModuleLoad) {
                { Import-Module $testModulePath -ErrorAction Stop -Force} | Should Not Throw
            } else {
                { Import-Module $testModulePath -ErrorAction Stop } | ShouldBeErrorId "ScriptRequiresUnmatchedOS,Microsoft.PowerShell.Commands.ImportModuleCommand"
            }
        }
    }
}
