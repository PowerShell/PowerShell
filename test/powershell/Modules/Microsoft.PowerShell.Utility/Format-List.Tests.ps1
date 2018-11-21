# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Format-List" -Tags "CI" {
    $nl = [Environment]::NewLine
    BeforeEach {
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue
    }

    It "Should call format list without error" {
        { $in | Format-List } | Should -Not -BeNullOrEmpty
    }

    It "Should be able to call the alias" {
        { $in | fl } | Should -Not -BeNullOrEmpty
    }

    It "Should have the same output whether choosing alias or not" {
        $expected = $in | Format-List | Out-String
        $actual   = $in | fl          | Out-String

        $actual | Should -Be $expected
    }

    It "Should produce the expected output" {
        $expected = "${nl}testName : testValue${nl}${nl}${nl}"
        $in = New-Object PSObject
        Add-Member -InputObject $in -MemberType NoteProperty -Name testName -Value testValue

        $in | Format-List                  | Should -Not -BeNullOrEmpty
        $in | Format-List   | Out-String   | Should -Not -BeNullOrEmpty
        $in | Format-List   | Out-String   | Should -Be $expected
    }

    It "Should be able to call a property of the piped input" {
        # Tested on two input commands to verify functionality.
        Get-Command | Select-Object -First 1 | Format-List -Property Name | Should -Not -BeNullOrEmpty

        Get-Date | Format-List -Property DisplayName | Should -Not -BeNullOrEmpty
    }

    It "Should be able to display a list of props when separated by a comma" {

        (Get-Command | Select-Object -First 5 | Format-List -Property Name,Source | Out-String) -Split "${nl}" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should -Match "(Name)|(Source)" }
    }

    It "Should show the requested prop in every element" {
        # Testing each element of format-list, using a for-each loop since the Format-List is so opaque
        (Get-Command | Select-Object -First 5 | Format-List -Property Source | Out-String) -Split "${nl}" |
          Where-Object { $_.trim() -ne "" } |
          ForEach-Object { $_ | Should -Match "Source :" }
    }

    It "Should not show anything other than the requested props" {
        $output = Get-Command | Select-Object -First 5 | Format-List -Property Name | Out-String

        $output | Should -Not -Match "CommandType :"
        $output | Should -Not -Match "Source :"
        $output | Should -Not -Match "Module :"
    }

    It "Should be able to take input without piping objects to it" {
        $output = { Format-List -InputObject $in }

        $output | Should -Not -BeNullOrEmpty

    }
}

Describe "Format-List DRT basic functionality" -Tags "CI" {
    It "Format-List with array should work" {
        $al = (0..255)
        $info = @{}
        $info.array = $al
        $result = $info | Format-List | Out-String
        $result | Should -Match "Name  : array\s+Value : {0, 1, 2, 3`u{2026}}" # ellipsis
    }

	It "Format-List with No Objects for End-To-End should work"{
		$p = @{}
		$result = $p | Format-List -Force -Property "foo","bar" | Out-String
		$result.Trim() | Should -BeNullOrEmpty
	}

	It "Format-List with Null Objects for End-To-End should work"{
		$p = $null
		$result = $p | Format-List -Force -Property "foo","bar" | Out-String
		$result.Trim() | Should -BeNullOrEmpty
	}

	It "Format-List with single line string for End-To-End should work"{
		$p = "single line string"
		$result = $p | Format-List -Force -Property "foo","bar" | Out-String
		$result.Trim() | Should -BeNullOrEmpty
	}

	It "Format-List with multiple line string for End-To-End should work"{
		$p = "Line1\nLine2"
		$result = $p | Format-List -Force -Property "foo","bar" | Out-String
		$result.Trim() | Should -BeNullOrEmpty
	}

	It "Format-List with string sequence for End-To-End should work"{
		$p = "Line1","Line2"
		$result = $p | Format-List -Force -Property "foo","bar" | Out-String
		$result.Trim() | Should -BeNullOrEmpty
	}

    It "Format-List with complex object for End-To-End should work" {
        Add-Type -TypeDefinition "public enum MyDayOfWeek{Sun,Mon,Tue,Wed,Thu,Fri,Sat}"
        $eto = [MyDayOfWeek]::New()
        $info = @{}
        $info.intArray = 1,2,3,4
        $info.arrayList = "string1","string2"
        $info.enumerable = [MyDayOfWeek]$eto
        $info.enumerableTestObject = $eto
        $result = $info|Format-List|Out-String
        $result | Should -Match "Name  : enumerableTestObject"
        $result | Should -Match "Value : Sun"
        $result | Should -Match "Name  : arrayList"
        $result | Should -Match "Value : {string1, string2}"
        $result | Should -Match "Name  : enumerable"
        $result | Should -Match "Value : Sun"
        $result | Should -Match "Name  : intArray"
        $result | Should -Match "Value : {1, 2, 3, 4}"
    }

	It "Format-List with multiple same class object should work"{
		Add-Type -TypeDefinition "public class TestClass{public TestClass(string name,int length){Name = name;Length = length;}public string Name;public int Length;}"
		$testobjects = [TestClass]::New('name1',1),[TestClass]::New('name2',2),[TestClass]::New('name3',3)
		$result = $testobjects|Format-List|Out-String
		$result | Should -Match "Name   : name1"
		$result | Should -Match "Length : 1"
		$result | Should -Match "Name   : name2"
		$result | Should -Match "Length : 2"
		$result | Should -Match "Name   : name3"
		$result | Should -Match "Length : 3"
	}

	It "Format-List with multiple different class object should work"{
		Add-Type -TypeDefinition "public class TestClass{public TestClass(string name,int length){Name = name;Length = length;}public string Name;public int Length;}"
		Add-Type -TypeDefinition "public class TestClass2{public TestClass2(string name,string value,int length){Name = name;Value = value; Length = length;}public string Name;public string Value;public int Length;}"
		$testobjects = [TestClass]::New('name1',1),[TestClass2]::New('name2',"value2",2),[TestClass]::New('name3',3)
		$result = $testobjects|Format-List|Out-String
		$result | Should -Match "Name   : name1"
		$result | Should -Match "Length : 1"
		$result | Should -Match "Name   : name2"
		$result | Should -Match "Value  : value2"
		$result | Should -Match "Length : 2"
		$result | Should -Match "Name   : name3"
		$result | Should -Match "Length : 3"
    }

    It "Format-List with FileInfo should work" {
        $null = New-Item $testdrive\test.txt -ItemType File -Value "hello" -Force
        $result = Get-ChildItem -File $testdrive\test.txt | Format-List | Out-String
        $result | Should -Match "Name\s*:\s*test.txt"
        $result | Should -Match "Length\s*:\s*5"
    }
}
