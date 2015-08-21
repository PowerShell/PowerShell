Describe "Test-Remove-Item" {
    $testpath = "/tmp/"
    $testfile = "testfile.txt"
    $testfilepath = $testpath + $testfile
    Context "File removal Tests" {
        BeforeEach {
            # redundant code in case the AfterEach fails
            if (Test-Path $testfilepath)
            {
                Remove-Item $testfilepath -Force
            }

            New-Item -Name $testfile -Path $testpath -ItemType "file" -Value "lorem ipsum"
        }

        It "Should be able to be called on a regular file without error using the Path switch" {
            { Remove-Item -Path $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to be called on a file without the Path switch" {
            { Remove-Item $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to call the rm alias" {
            { rm $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to call the del alias" {
            { del $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to call the erase alias" {
            { erase $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to call the ri alias" {
            { ri $testfilepath } | Should Not Throw

            Test-Path $testfilepath | Should Be $false
        }

        It "Should not be able to remove a read-only document without using the force switch" {
            # Set to read only
            Set-ItemProperty -Path $testfilepath -Name IsReadOnly -Value $true

            # attempt to remove the file
            { Remove-Item $testfilepath -ErrorAction SilentlyContinue } | Should Not Throw

            # validate
            Test-Path $testfilepath | Should Be $true

            # set to not be read only
            Set-ItemProperty -Path $testfilepath -Name IsReadOnly -Value $false

            # remove
            Remove-Item  $testfilepath -Force

            # Validate 
            Test-Path $testfilepath | Should Be $false
        }

        It "Should be able to remove all files matching a regular expression with the include switch" {
            # Create multiple files with specific string
            New-Item -Name file1.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file2.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file3.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            # Create a single file that does not match that string - already done in BeforeEach

            # Delete the specific string
            Remove-Item /tmp/* -Include file*.txt
            # validate that the string under test was deleted, and the nonmatching strings still exist
            Test-path /tmp/file1.txt | Should Be $false
            Test-path /tmp/file2.txt | Should Be $false
            Test-path /tmp/file3.txt | Should Be $false
            Test-Path $testfilepath   | Should Be $true

            # Delete the non-matching strings
            Remove-Item $testfilepath
        }

        It "Should be able to not remove any files matching a regular expression with the exclude switch" {
            # Create multiple files with specific string
            New-Item -Name file1.wav -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file2.wav -Path $testpath -ItemType "file" -Value "lorem ipsum"

            # Create a single file that does not match that string
            New-Item -Name file1.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
            New-Item -Name file2.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"

            # Delete the specific string
            Remove-Item /tmp/file* -Exclude *.wav -Include *.txt

            # validate that the string under test was deleted, and the nonmatching strings still exist
            Test-Path /tmp/file1.wav | Should Be $true
            Test-Path /tmp/file2.wav | Should Be $true
            Test-Path /tmp/file1.txt | Should Be $false
            Test-path /tmp/file2.txt | Should Be $false

            # Delete the non-matching strings
            Remove-Item /tmp/file1.wav
            Remove-Item /tmp/file2.wav

            Test-Path /tmp/file1.wav | Should Be $false
            Test-Path /tmp/file2.wav | Should Be $false
        }
    }

    Context "Directory Removal Tests" {
        $testdirectory = "/tmp/testdir"
        $testsubdirectory = $testdirectory + "/subd"
        BeforeEach{
            if (Test-Path $testdirectory)
            {
                Remove-Item $testdirectory -Force
            }

            test-path $testdirectory | Should Be $false

            New-Item -Name "testdir" -Path "/tmp/" -ItemType "directory"

            test-path $testdirectory | Should Be $true
        }

        It "Should be able to remove a directory" {
            { Remove-Item $testdirectory } | Should Not Throw

            Test-Path $testdirectory | Should Be $false
        }

        It "Should be able to recursively delete subfolders" {
            New-Item -Name "subd" -Path $testdirectory -ItemType "directory"
            New-Item -Name $testfile -Path $testsubdirectory -ItemType "file" -Value "lorem ipsum"

            $complexDirectory = $testsubdirectory + "/" + $testfile
            test-path $complexDirectory | Should Be $true

            { Remove-Item $testdirectory -Recurse} | Should Not Throw

            Test-Path $testdirectory | Should Be $false
        }
    }
}
