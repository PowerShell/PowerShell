# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Unblock-File" -Tags "CI" {

    Context "Windows" {
        BeforeAll {
            function Test-UnblockFile {
                { Get-Content -Path $testfilepath -Stream Zone.Identifier -ErrorAction Stop | Out-Null } |
                    Should -Throw -ErrorId "GetContentReaderFileNotFoundError,Microsoft.PowerShell.Commands.GetContentCommand"
            }
                        if ( ! $IsWindows )
            {
                $origDefaults = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues['it:skip'] = $true

            } else {
                $testfilepath = Join-Path -Path $TestDrive -ChildPath testunblockfile.ttt
            }
        }

        AfterAll {
            if ( ! $IsWindows ){
                $global:PSDefaultParameterValues = $origDefaults
            }
        }

        BeforeEach {
            if ( $IsWindows ){
                Set-Content -Value "[ZoneTransfer]`r`nZoneId=4" -Path $testfilepath -Stream Zone.Identifier
            }
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
            Set-Content -Path $TestFile -value 'test'
            $ZoneIdentifier = {
                [ZoneTransfer]
                ZoneId=3
            }
            Set-Content -Path $TestFile -Value $ZoneIdentifier -Stream 'Zone.Identifier'
            Set-ItemProperty -Path $TestFile -Name IsReadOnly -Value $True

            $TestFileCreated = Get-ChildItem $TestFile
            $TestFileCreated.IsReadOnly | Should -BeTrue

            { Unblock-File -LiteralPath $TestFile -ErrorAction Stop } | Should -Throw -ErrorId "RemoveItemUnableToAccessFile,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }
    }

    Context "macOS" {
        BeforeAll {
            function Test-UnblockFile {
                $result = (xattr $testfilepath | Select-String 'com.apple.com.quarantine')
                $result | Should -BeNullOrEmpty
            }

            if ( ! $IsMacOS )
            {
                $origDefaults = $PSDefaultParameterValues.Clone()
                $PSDefaultParameterValues['it:skip'] = $true

            } else {
                $testfilepath = Join-Path -Path $TestDrive -ChildPath testunblockfile.ttt
                New-Item -Path $testfilepath -ItemType File
            }
        }

        AfterAll {
            if ( ! $IsMacOS ){
                $global:PSDefaultParameterValues = $origDefaults
            }
        }

        BeforeEach {
            if ( $IsMacOS ){
                xattr -w com.apple.quarantine '0081;5dd5c373;Microsoft Edge;1A9A933D-619A-4036-BAF3-17A7966A1BA8' $testfilepath
            }
        }

        It "With '-Path': no file exist" {
            { Unblock-File -Path nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        It "With '-LiteralPath': no file exist" {
            { Unblock-File -LiteralPath nofileexist.ttt -ErrorAction Stop } | Should -Throw -ErrorId "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }

        # the error code in not unique
        # cannot suppress the error if it's already unblocked.
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
            Set-Content -Path $TestFile -value 'test'
            xattr -w com.apple.quarantine '0081;5dd5c373;Microsoft Edge;1A9A933D-619A-4036-BAF3-17A7966A1BA8' $TestFile
            Set-ItemProperty -Path $TestFile -Name IsReadOnly -Value $True

            $TestFileCreated = Get-ChildItem $TestFile
            $TestFileCreated.IsReadOnly | Should -BeTrue

            { Unblock-File -LiteralPath $TestFile -ErrorAction Stop } | Should -Throw -ErrorId "UnblockError,Microsoft.PowerShell.Commands.UnblockFileCommand"
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
                New-Item -Path $testfilepath -ItemType File
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
