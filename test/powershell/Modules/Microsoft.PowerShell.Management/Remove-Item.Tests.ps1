# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Remove-Item" -Tags "CI" {
    BeforeAll {
        $testpath = $TestDrive
        $testfile = "testfile.txt"
        $testfilepath = Join-Path -Path $testpath -ChildPath $testfile
    }

    Context "File removal Tests" {
        BeforeEach {
            New-Item -Name $testfile -Path $testpath -ItemType "file" -Value "lorem ipsum" -Force
            Test-Path $testfilepath | Should -BeTrue
        }

        It "Should be able to be called on a regular file without error using the Path parameter" {
            { Remove-Item -Path $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to be called on a file without the Path parameter" {
            { Remove-Item $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to call the rm alias" {
            { rm $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to call the del alias" {
            { del $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to call the erase alias" {
            { erase $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to call the ri alias" {
            { ri $testfilepath } | Should -Not -Throw

            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should not be able to remove a read-only document without using the force switch" {
            # Set to read only
            Set-ItemProperty -Path $testfilepath -Name IsReadOnly -Value $true

            # attempt to remove the file
            { Remove-Item $testfilepath -ErrorAction SilentlyContinue } | Should -Not -Throw

            # validate
            Test-Path $testfilepath | Should -BeTrue

            # remove using the -force switch on the readonly object
            Remove-Item  $testfilepath -Force

            # Validate
            Test-Path $testfilepath | Should -BeFalse
        }

        It "Should be able to remove all files matching a regular expression with the include parameter" {
            # Create multiple files with specific string
            New-Item -Name file1.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file2.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file3.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            # Create a single file that does not match that string - already done in BeforeEach

            # Delete the specific string
            Remove-Item (Join-Path -Path $testpath -ChildPath "*") -Include file*.txt
            # validate that the string under test was deleted, and the nonmatching strings still exist
            Test-Path (Join-Path -Path $testpath -ChildPath file1.txt) | Should -BeFalse
            Test-Path (Join-Path -Path $testpath -ChildPath file2.txt) | Should -BeFalse
            Test-Path (Join-Path -Path $testpath -ChildPath file3.txt) | Should -BeFalse
            Test-Path $testfilepath  | Should -BeTrue

            # Delete the non-matching strings
            Remove-Item $testfilepath

            Test-Path $testfilepath  | Should -BeFalse
        }

        It "Should be able to not remove any files matching a regular expression with the exclude parameter" {
            # Create multiple files with specific string
            New-Item -Name file1.wav -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file2.wav -Path $testpath -ItemType "file" -Value "lorem ipsum"

            # Create a single file that does not match that string
            New-Item -Name file1.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"

            # Delete the specific string
            Remove-Item (Join-Path -Path $testpath -ChildPath "file*") -Exclude *.wav -Include *.txt

            # validate that the string under test was deleted, and the nonmatching strings still exist
            Test-Path (Join-Path -Path $testpath -ChildPath file1.wav) | Should -BeTrue
            Test-Path (Join-Path -Path $testpath -ChildPath file2.wav) | Should -BeTrue
            Test-Path (Join-Path -Path $testpath -ChildPath file1.txt) | Should -BeFalse

            # Delete the non-matching strings
            Remove-Item (Join-Path -Path $testpath -ChildPath file1.wav)
            Remove-Item (Join-Path -Path $testpath -ChildPath file2.wav)

            Test-Path (Join-Path -Path $testpath -ChildPath file1.wav) | Should -BeFalse
            Test-Path (Join-Path -Path $testpath -ChildPath file2.wav) | Should -BeFalse
        }
    }

    Context "Directory Removal Tests" {
        BeforeAll {
            $testdirectory = Join-Path -Path $testpath -ChildPath testdir
            $testsubdirectory = Join-Path -Path $testdirectory -ChildPath subd
        }

        BeforeEach {
            New-Item -Name "testdir" -Path $testpath -ItemType "directory" -Force

            Test-Path $testdirectory | Should -BeTrue
        }

        It "Should be able to remove a directory" {
            { Remove-Item $testdirectory -ErrorAction Stop } | Should -Not -Throw

            Test-Path $testdirectory | Should -BeFalse
        }

        It "Should be able to recursively delete subfolders" {
            New-Item -Name "subd" -Path $testdirectory -ItemType "directory"
            New-Item -Name $testfile -Path $testsubdirectory -ItemType "file" -Value "lorem ipsum"

            $complexDirectory = Join-Path -Path $testsubdirectory -ChildPath $testfile
            Test-Path $complexDirectory | Should -BeTrue

            { Remove-Item $testdirectory -Recurse -ErrorAction Stop } | Should -Not -Throw

            Test-Path $testdirectory | Should -BeFalse
        }

        It "Should be able to recursively delete a directory with a trailing backslash" {
            New-Item -Name "subd" -Path $testdirectory -ItemType "directory"
            New-Item -Name $testfile -Path $testsubdirectory -ItemType "file" -Value "lorem ipsum"

            $complexDirectory = Join-Path -Path $testsubdirectory -ChildPath $testfile
            Test-Path $complexDirectory | Should -BeTrue

            $testdirectoryWithBackSlash = Join-Path -Path $testdirectory -ChildPath ([IO.Path]::DirectorySeparatorChar)
            Test-Path $testdirectoryWithBackSlash | Should -BeTrue

            { Remove-Item $testdirectoryWithBackSlash -Recurse -ErrorAction Stop } | Should -Not -Throw

            Test-Path $testdirectoryWithBackSlash | Should -BeFalse
            Test-Path $testdirectory | Should -BeFalse
        }
    }

    Context "Alternate Data Streams should be supported on Windows" {
        BeforeAll {
            if (!$IsWindows) {
                return
            }
            $fileName = "ADStest.txt"
            $streamName = "teststream"
            $dirName = "ADStestdir"
            $fileContent =" This is file content."
            $streamContent = "datastream content here"
            $streamfile = Join-Path -Path $testpath -ChildPath $fileName
            $streamdir = Join-Path -Path $testpath -ChildPath $dirName

            $null = New-Item -Path $streamfile -ItemType "File" -force
            Add-Content -Path $streamfile -Value $fileContent
            Add-Content -Path $streamfile -Stream $streamName -Value $streamContent
            $null = New-Item -Path $streamdir -ItemType "Directory" -Force
            Add-Content -Path $streamdir -Stream $streamName -Value $streamContent
        }

        It "Should completely remove a datastream from a file" -Skip:(!$IsWindows) {
            Get-Item -Path $streamfile -Stream $streamName | Should -Not -BeNullOrEmpty
            Remove-Item -Path $streamfile -Stream $streamName
            Get-Item -Path $streamfile -Stream $streamName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }

        It "Should completely remove a datastream from a directory" -Skip:(!$IsWindows) {
            Get-Item -Path $streamdir -Stream $streamName | Should -Not -BeNullOrEmpty
            Remove-Item -Path $streamdir -Stream $streamName
            Get-Item -Path $streamdir -Stream $streamname -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }
}
