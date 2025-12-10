# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Set-PSDebug" -Tags "CI" {
    Context "Tracing can be used" {
        AfterEach {
            Set-PSDebug -Off
        }

        It "Should be able to go through the tracing options" {
            { Set-PSDebug -Trace 0 } | Should -Not -Throw
            { Set-PSDebug -Trace 1 } | Should -Not -Throw
            { Set-PSDebug -Trace 2 } | Should -Not -Throw
        }

        It "Should be able to set strict" {
            { Set-PSDebug -Strict } | Should -Not -Throw
        }
        
        It "Should skip magic extents created by pwsh" {
            class ClassWithDefaultCtor {
                MyMethod() { }
            }
            
            { 
                Set-PSDebug -Trace 1
                [ClassWithDefaultCtor]::new()
            } | Should -Not -Throw
        }

        It "Should trace all lines of a multiline command" {
            $tempScript = Join-Path $TestDrive "multiline-trace.ps1"
            $scriptContent = "Set-PSDebug -Trace 1`nWrite-Output `"foo ```nbar`""
            Set-Content -Path $tempScript -Value $scriptContent -NoNewline

            # Run in a separate process to capture trace output
            $pinfo = [System.Diagnostics.ProcessStartInfo]::new()
            $pinfo.FileName = (Get-Process -Id $PID).Path
            $pinfo.Arguments = "-NoProfile -File `"$tempScript`""
            $pinfo.RedirectStandardOutput = $true
            $pinfo.UseShellExecute = $false

            $process = [System.Diagnostics.Process]::new()
            $process.StartInfo = $pinfo
            $process.Start() | Should -BeTrue
            $output = $process.StandardOutput.ReadToEnd()
            $process.WaitForExit()

            # The debug trace for multiline commands should include all lines with DEBUG: prefix
            $output | Should -Match 'DEBUG:.*Write-Output' -Because "debug output should contain the command with DEBUG: prefix"
            $output | Should -Match 'DEBUG:.*bar"' -Because "debug output should contain the continuation line with DEBUG: prefix"
        }

        It "Should trace all lines of a multiline command with -Trace 2" {
            $tempScript = Join-Path $TestDrive "multiline-trace2.ps1"
            $scriptContent = "Set-PSDebug -Trace 2`nWrite-Output `"foo ```nbar`""
            Set-Content -Path $tempScript -Value $scriptContent -NoNewline

            # Run in a separate process to capture trace output
            $pinfo = [System.Diagnostics.ProcessStartInfo]::new()
            $pinfo.FileName = (Get-Process -Id $PID).Path
            $pinfo.Arguments = "-NoProfile -File `"$tempScript`""
            $pinfo.RedirectStandardOutput = $true
            $pinfo.UseShellExecute = $false

            $process = [System.Diagnostics.Process]::new()
            $process.StartInfo = $pinfo
            $process.Start() | Should -BeTrue
            $output = $process.StandardOutput.ReadToEnd()
            $process.WaitForExit()

            # The debug trace for multiline commands should include all lines with DEBUG: prefix
            $output | Should -Match 'DEBUG:.*Write-Output' -Because "debug output should contain the command with DEBUG: prefix"
            $output | Should -Match 'DEBUG:.*bar"' -Because "debug output should contain the continuation line with DEBUG: prefix"
        }
    }
}
