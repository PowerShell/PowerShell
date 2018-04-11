# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Remove-Item" -Tags "CI" {
    $testpath = $TestDrive
    $testfile = "testfile.txt"
    $testfileSpName = "[testfile].txt"
    $testfileSp = "``[testfile``].txt"
    $testfilepath = Join-Path -Path $testpath -ChildPath $testfile
    $testfilepathSp = Join-Path -Path $testpath -ChildPath $testfileSp
    Context "File removal Tests" {
	BeforeEach {
	    New-Item -Name $testfile -Path $testpath -ItemType "file" -Value "lorem ipsum" -Force

	    Test-Path $testfilepath | Should -BeTrue

	}

	It "Should be able to be called on a regular file without error using the Path switch" {
	    { Remove-Item -Path $testfilepath } | Should -Not -Throw

	    Test-Path $testfilepath | Should -BeFalse
	}

	It "Should be able to be called on a file without the Path switch" {
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

	It "Should be able to remove all files matching a regular expression with the include switch" {
	    # Create multiple files with specific string
	    New-Item -Name file1.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
	    New-Item -Name file2.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
	    New-Item -Name file3.txt -Path $testpath -ItemType "file" -Value "lorem ipsum"
	    # Create a single file that does not match that string - already done in BeforeEach

	    # Delete the specific string
	    Remove-Item (Join-Path -Path $testpath -ChildPath "*") -Include file*.txt
	    # validate that the string under test was deleted, and the nonmatching strings still exist
	    Test-path (Join-Path -Path $testpath -ChildPath file1.txt) | Should -BeFalse
	    Test-path (Join-Path -Path $testpath -ChildPath file2.txt) | Should -BeFalse
	    Test-path (Join-Path -Path $testpath -ChildPath file3.txt) | Should -BeFalse
	    Test-Path $testfilepath  | Should -BeTrue

	    # Delete the non-matching strings
	    Remove-Item $testfilepath

	    Test-Path $testfilepath  | Should -BeFalse
	}

	It "Should be able to not remove any files matching a regular expression with the exclude switch" {
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

        It "Should be able to remove file when path contains special char" {
            New-Item -Path $testpath -Name $testfileSpName -ItemType File -Force
            $testfilepathSp | Should -Exist

            Remove-Item -Path $testfilepathSp
            $testfilepathSp | Should -Not -Exist
        }
    }

    Context "Directory Removal Tests" {
	$testdirectory = Join-Path -Path $testpath -ChildPath testdir
	$testsubdirectory = Join-Path -Path $testdirectory -ChildPath subd
	BeforeEach {
	    New-Item -Name "testdir" -Path $testpath -ItemType "directory" -Force

	    Test-Path $testdirectory | Should -BeTrue
	}

	It "Should be able to remove a directory" {
	    { Remove-Item $testdirectory } | Should -Not -Throw

	    Test-Path $testdirectory | Should -BeFalse
	}

	It "Should be able to recursively delete subfolders" {
	    New-Item -Name "subd" -Path $testdirectory -ItemType "directory"
	    New-Item -Name $testfile -Path $testsubdirectory -ItemType "file" -Value "lorem ipsum"

	    $complexDirectory = Join-Path -Path $testsubdirectory -ChildPath $testfile
	    test-path $complexDirectory | Should -BeTrue

	    { Remove-Item $testdirectory -Recurse} | Should -Not -Throw

	    Test-Path $testdirectory | Should -BeFalse
	}
    }
}
