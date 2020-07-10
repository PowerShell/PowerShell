# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Unblock-File" -Tags "CI" {

    Context "Windows and macOS" {
        BeforeAll {

            if ($IsWindows) {
                function Test-UnblockFile {
                    { Get-Content -Path $testfilepath -Stream Zone.Identifier -ErrorAction Stop | Out-Null } |
                    Should -Throw -ErrorId "GetContentReaderFileNotFoundError,Microsoft.PowerShell.Commands.GetContentCommand"
                }

                function Block-File {
                    param($path)
                    Set-Content -Value "[ZoneTransfer]`r`nZoneId=4" -Path $path -Stream Zone.Identifier
                }
            }
            else {
                function Test-UnblockFile {
                    $result = (xattr $testfilepath | Select-String 'com.apple.com.quarantine')
                    $result | Should -BeNullOrEmpty
                }

                function Block-File {
                    param($path)
                    Set-Content -Path $path -Value 'test'
                    xattr -w com.apple.quarantine '0081;5dd5c373;Microsoft Edge;1A9A933D-619A-4036-BAF3-17A7966A1BA8' $path

                }
            }

            if ( $IsLinux )
            {
                $origDefaults = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues['it:skip'] = $true

            } else {
                $testfilepath = Join-Path -Path $TestDrive -ChildPath testunblockfile.ttt
            }
        }

        AfterAll {
            if ( $IsLinux ){
                $global:PSDefaultParameterValues = $origDefaults
            }
        }

        BeforeEach {
            Block-File -Path $testfilepath
        }

        It "With '-Path': no file exist" {
            { Unblock-File -Path nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-LiteralPath': no file exist" {
            { Unblock-File -LiteralPath nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-Path': file exist" {
            Unblock-File -Path $testfilepath
            Test-UnblockFile

            # If a file is not blocked we silently return without an error.
            { Unblock-File -Path $testfilepath -ErrorAction Stop } | Should -Not -Throw
        }

        It "With '-LiteralPath': file exist" {
            Unblock-File -LiteralPath $testfilepath
            Test-UnblockFile
        }

        It "Write an error if a file is read only" {
            $TestFile = Join-Path $TestDrive "testfileunlock.ps1"
            Block-File -Path $TestFile
            Set-ItemProperty -Path $TestFile -Name IsReadOnly -Value $true

            $TestFileCreated = Get-ChildItem $TestFile
            $TestFileCreated.IsReadOnly | Should -BeTrue
            if ($IsWindows) {
                $expectedError = "RemoveItemUnableToAccessFile,Microsoft.PowerShell.Commands.UnblockFileCommand"
            } else {
                $expectedError = "UnblockError,Microsoft.PowerShell.Commands.UnblockFileCommand"
            }

            { Unblock-File -LiteralPath $TestFile -ErrorAction Stop } | Should -Throw -ErrorId $expectedError
        }
    }

    Context "Linux" {
        BeforeAll {
            if ( ! $IsLinux )
            {
                $origDefaults = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues['it:skip'] = $true

            } else {
                $testfilepath = Join-Path -Path $TestDrive -ChildPath testunblockfile.ttt
                $null = New-Item -Path $testfilepath -ItemType File
            }
        }

        AfterAll {
            if ( ! $IsLinux ){
                $global:PSDefaultParameterValues = $origDefaults
            }
        }


        It "With '-Path': no file exist" {
            { Unblock-File -Path nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-LiteralPath': no file exist" {
            { Unblock-File -LiteralPath nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-LiteralPath': file exist" {
            { Unblock-File -LiteralPath $testfilepath -ErrorAction Stop } | Should -Throw -ErrorId "LinuxNotSupported,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-Path': file exist" {
            { Unblock-File -Path $testfilepath -ErrorAction Stop } | Should -Throw -ErrorId "LinuxNotSupported,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }
    }
}
