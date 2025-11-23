# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Format-List" -Tags "CI" {
    BeforeAll {
        $nl = [Environment]::NewLine

        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

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
        $expected = "${nl}testName : testValue${nl}${nl}"
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
    BeforeAll {
        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

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

    It "Format-List should work with double byte wide chars" {
        $obj = [pscustomobject]@{
            "哇" = "62";
            "dbda" = "KM";
            "消息" = "千"
        }

        $expected = @"

哇   : 62
dbda : KM
消息 : 千


"@
        $expected = $expected -replace "`r`n", "`n"

        $actual = $obj | Format-List | Out-String
        $actual = $actual -replace "`r`n", "`n"
        $actual | Should -BeExactly $expected
    }

    It 'Float, double, and decimal should not be truncated to number of decimals from current culture' {
        $o = [PSCustomObject]@{
            double = [double]1234.56789
            float = [float]9876.543
            decimal = [decimal]4567.123456789
        }

        $expected = @"

double  : 1234.56789
float   : 9876.543
decimal : 4567.123456789


"@

        $actual = $o | Format-List | Out-String
        ($actual.Replace("`r`n", "`n")) | Should -BeExactly ($expected.Replace("`r`n", "`n"))
    }
}

Describe 'Format-List color tests' -Tag 'CI' {
    BeforeAll {
        $originalRendering = $PSStyle.OutputRendering
        $PSStyle.OutputRendering = 'Ansi'
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ForceFormatListFixedLabelWidth', $true)
    }

    AfterAll {
        $PSStyle.OutputRendering = $originalRendering
        [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook('ForceFormatListFixedLabelWidth', $false)
    }

    It 'Property names should use FormatAccent' {
        $out = ([pscustomobject]@{Short=1;LongLabelName=2} | fl | out-string).Split([Environment]::NewLine, [System.StringSplitOptions]::RemoveEmptyEntries)
        $out.Count | Should -Be 2
        $out[0] | Should -BeExactly "$($PSStyle.Formatting.FormatAccent)Short      : $($PSStyle.Reset)1" -Because ($out[0] | Format-Hex)
        $out[1] | Should -BeExactly "$($PSStyle.Formatting.FormatAccent)LongLabelN : $($PSStyle.Reset)2"
    }

    It 'VT decorations in a property value should not be leaked in list view' {
        $expected = @"
`e[32;1ma : `e[0mHello
`e[32;1mb : `e[0m`e[36mworld`e[0m
"@
        ## Format-List should append the 'reset' escape sequence to the value of 'b' property.
        $obj = [pscustomobject]@{ a = "Hello"; b = $PSStyle.Foreground.Cyan + "world"; }
        $obj | Format-List | Out-File "$TestDrive/outfile.txt"

        $output = Get-Content "$TestDrive/outfile.txt" -Raw
        $output.Trim().Replace("`r", "") | Should -BeExactly $expected.Replace("`r", "")
    }

    Context 'ExcludeProperty parameter' {
        It 'Should exclude specified properties' {
            $obj = [pscustomobject]@{ Name = 'Test'; Age = 30; City = 'Seattle' }
            $result = $obj | Format-List -ExcludeProperty Age | Out-String
            $result | Should -Match 'Name'
            $result | Should -Match 'City'
            $result | Should -Not -Match 'Age'
        }

        It 'Should work with wildcard patterns' {
            $obj = [pscustomobject]@{ Prop1 = 1; Prop2 = 2; Other = 3 }
            $result = $obj | Format-List -ExcludeProperty Prop* | Out-String
            $result | Should -Match 'Other'
            $result | Should -Not -Match 'Prop1'
            $result | Should -Not -Match 'Prop2'
        }

        It 'Should work without Property parameter (implies -Property *)' {
            $obj = [pscustomobject]@{ A = 1; B = 2; C = 3 }
            $result = $obj | Format-List -ExcludeProperty B | Out-String
            $result | Should -Match 'A'
            $result | Should -Match 'C'
            $result | Should -Not -Match 'B'
        }

        It 'Should work with Property parameter' {
            $obj = [pscustomobject]@{ Name = 'Test'; Age = 30; City = 'Seattle'; Country = 'USA' }
            $result = $obj | Format-List -Property Name, Age, City, Country -ExcludeProperty Age | Out-String
            $result | Should -Match 'Name'
            $result | Should -Match 'City'
            $result | Should -Match 'Country'
            $result | Should -Not -Match 'Age'
        }

        It 'Should handle multiple excluded properties' {
            $obj = [pscustomobject]@{ A = 1; B = 2; C = 3; D = 4 }
            $result = $obj | Format-List -ExcludeProperty B, D | Out-String
            $result | Should -Match 'A'
            $result | Should -Match 'C'
            $result | Should -Not -Match 'B'
            $result | Should -Not -Match 'D'
        }
    }
}
