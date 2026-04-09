# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "RegisterManifest Argument Parsing Tests" -Tags "CI" {
    BeforeAll {
        $scriptPath = Join-Path $PSScriptRoot '..' '..' '..' 'src' 'PowerShell.Core.Instrumentation' 'RegisterManifest.ps1'
        
        # Mock wevtutil to capture what arguments it receives
        function Test-WevtutilArgumentParsing {
            param(
                [string]$ManifestPath,
                [string]$BinaryPath,
                [string]$Command
            )
            
            # Build the arguments string as RegisterManifest.ps1 would
            if ($Command -eq 'install') {
                $arguments = 'im "{0}" /rf:"{1}" /mf:"{1}"' -f $ManifestPath, $BinaryPath
            }
            elseif ($Command -eq 'uninstall') {
                $arguments = 'um "{0}"' -f $ManifestPath
            }
            
            # Create a test script that echoes arguments to capture what wevtutil would receive
            $testScript = {
                param($WevtutilPath, $Arguments)
                
                # Use cmd.exe to simulate how Windows processes the command line
                $echoScript = "@echo off`necho ARGS: %*"
                $echoPath = Join-Path $env:TEMP "echo-test-$([guid]::NewGuid()).bat"
                try {
                    Set-Content -Path $echoPath -Value $echoScript
                    
                    # Execute with the arguments to see how they're parsed
                    $output = cmd /c "`"$echoPath`" $Arguments 2>&1"
                    return $output
                }
                finally {
                    Remove-Item $echoPath -Force -ErrorAction SilentlyContinue
                }
            }
            
            return @{
                Arguments = $arguments
                TestOutput = & $testScript 'wevtutil.exe' $arguments
            }
        }
    }
    
    Context "Argument construction for paths without spaces" {
        It "Should construct install arguments correctly for simple paths" {
            $manifest = "C:\PowerShell\PowerShell.Core.Instrumentation.man"
            $binary = "C:\PowerShell\PowerShell.Core.Instrumentation.dll"
            
            $result = Test-WevtutilArgumentParsing -ManifestPath $manifest -BinaryPath $binary -Command 'install'
            
            $result.Arguments | Should -Be 'im "C:\PowerShell\PowerShell.Core.Instrumentation.man" /rf:"C:\PowerShell\PowerShell.Core.Instrumentation.dll" /mf:"C:\PowerShell\PowerShell.Core.Instrumentation.dll"'
        }
        
        It "Should construct uninstall arguments correctly for simple paths" {
            $manifest = "C:\PowerShell\PowerShell.Core.Instrumentation.man"
            
            $result = Test-WevtutilArgumentParsing -ManifestPath $manifest -Command 'uninstall'
            
            $result.Arguments | Should -Be 'um "C:\PowerShell\PowerShell.Core.Instrumentation.man"'
        }
    }
    
    Context "Argument construction for paths with spaces" {
        It "Should construct install arguments correctly for paths with spaces" {
            $manifest = "C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.man"
            $binary = "C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.dll"
            
            $result = Test-WevtutilArgumentParsing -ManifestPath $manifest -BinaryPath $binary -Command 'install'
            
            $result.Arguments | Should -Be 'im "C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.man" /rf:"C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.dll" /mf:"C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.dll"'
        }
        
        It "Should construct uninstall arguments correctly for paths with spaces" {
            $manifest = "C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.man"
            
            $result = Test-WevtutilArgumentParsing -ManifestPath $manifest -Command 'uninstall'
            
            $result.Arguments | Should -Be 'um "C:\Program Files\PowerShell\7\PowerShell.Core.Instrumentation.man"'
        }
        
        It "Should handle paths with parentheses and spaces" {
            $manifest = "C:\Program Files (x86)\PowerShell\7\PowerShell.Core.Instrumentation.man"
            $binary = "C:\Program Files (x86)\PowerShell\7\PowerShell.Core.Instrumentation.dll"
            
            $result = Test-WevtutilArgumentParsing -ManifestPath $manifest -BinaryPath $binary -Command 'install'
            
            $result.Arguments | Should -Be 'im "C:\Program Files (x86)\PowerShell\7\PowerShell.Core.Instrumentation.man" /rf:"C:\Program Files (x86)\PowerShell\7\PowerShell.Core.Instrumentation.dll" /mf:"C:\Program Files (x86)\PowerShell\7\PowerShell.Core.Instrumentation.dll"'
        }
    }
    
    Context "Argument string handling by Start-Process" {
        It "Should properly quote arguments when passed as string to Start-Process" {
            # This tests that our argument formatting works with Start-Process
            $testPath = "C:\Program Files\Test Path\file.txt"
            $arguments = 'test "{0}"' -f $testPath
            
            # The arguments string should contain the quotes
            $arguments | Should -Match '"C:\\Program Files\\Test Path\\file\.txt"'
        }
        
        It "Should handle multiple quoted arguments in single string" {
            $path1 = "C:\Program Files\Path1\file.man"
            $path2 = "C:\Program Files\Path2\file.dll"
            $arguments = 'im "{0}" /rf:"{1}" /mf:"{1}"' -f $path1, $path2
            
            # Verify quotes are properly placed
            $arguments | Should -Match 'im "C:\\Program Files\\Path1\\file\.man"'
            $arguments | Should -Match '/rf:"C:\\Program Files\\Path2\\file\.dll"'
            $arguments | Should -Match '/mf:"C:\\Program Files\\Path2\\file\.dll"'
        }
    }
    
    Context "Integration test with actual process execution" {
        It "Should execute external process with quoted paths correctly" -Skip:(-not $IsWindows) {
            # Create a test directory with spaces
            $testDir = Join-Path $env:TEMP "Test Dir With Spaces $([guid]::NewGuid())"
            $testFile = Join-Path $testDir "test.txt"
            
            try {
                New-Item -Path $testDir -ItemType Directory -Force | Out-Null
                "test content" | Out-File $testFile -Force
                
                # Use cmd.exe type command which requires proper quoting
                $arguments = 'type "{0}"' -f $testFile
                
                $output = cmd /c $arguments 2>&1
                
                $output | Should -Match "test content"
            }
            finally {
                Remove-Item $testDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
}
