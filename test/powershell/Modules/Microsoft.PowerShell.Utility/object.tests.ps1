# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Object cmdlets" -Tags "CI" {
    Context "Group-Object" {
        It "AsHashtable returns a hashtable" {
            $result = Get-Process | Group-Object -Property ProcessName -AsHashTable
            $result["pwsh"].Count | Should -BeGreaterThan 0
        }

        It "AsString returns a string" {
           $processes = Get-Process | Group-Object -Property ProcessName -AsHashTable -AsString
           $result = $processes.Keys | ForEach-Object {$_.GetType()}
           $result[0].Name | Should -Be "String"
        }
    }

    Context "Tee-Object" {
        It "with literal path" {
            $path = "TestDrive:\[TeeObjectLiteralPathShouldWorkForSpecialFilename].txt"
            Write-Output "Test" | Tee-Object -LiteralPath $path | Tee-Object -Variable TeeObjectLiteralPathShouldWorkForSpecialFilename
            $TeeObjectLiteralPathShouldWorkForSpecialFilename | Should -Be (Get-Content -LiteralPath $path)
        }
    }
}

Describe "Object cmdlets" -Tags "CI" {
    Context "Measure-Object" {
        BeforeAll {
            ## Powershell language prefers , as an array separator without "".
            ## If a number has comma in them it considers it to be the 1000 separator like "1,000".
            ## In de-DE language the comma is used as decimal point, but powershell still uses it as a 1000 separator.
            ## In case the number has a comma, it is ignored. So, "99,1" becomes 991.
            ## To work around that behavior, we use ToString() on the expected answer and . for decimal in the input.

            $firstValue = "9995788.71"
            $expectedFirstValue = $null
            $null = [System.Management.Automation.LanguagePrimitives]::TryConvertTo($firstValue, [double], [cultureinfo]::InvariantCulture, [ref] $expectedFirstValue)
            $firstObject = New-Object psobject
            $firstObject | Add-Member -NotePropertyName Header -NotePropertyValue $firstValue

            $secondValue = "15847577.7"
            $expectedSecondValue = $null
            $null = [System.Management.Automation.LanguagePrimitives]::TryConvertTo($secondValue, [double], [cultureinfo]::InvariantCulture, [ref] $expectedSecondValue)
            $secondObject = New-Object psobject
            $secondObject | Add-Member -NotePropertyName Header -NotePropertyValue $secondValue

            $testCases = @(
                @{ data = @("abc","ABC","Def"); min = "abc"; max = "Def"},
                @{ data = @([datetime]::Today, [datetime]::Today.AddDays(-1)); min = ([datetime]::Today.AddDays(-1)).ToString() ; max = [datetime]::Today.ToString() }
                @{ data = @(1,2,3,"ABC"); min = 1; max = "ABC"},
                @{ data = @(4,2,3,"ABC",1); min = 1; max = "ABC"},
                @{ data = @(4,2,3,"ABC",1,"DEF"); min = 1; max = "DEF"},
                @{ data = @("111 Test","19"); min = "111 Test"; max = "19"},
                @{ data = @("19", "111 Test"); min = "111 Test"; max = "19"},
                @{ data = @("111 Test",19); min = "111 Test"; max = 19},
                @{ data = @(19, "111 Test"); min = "111 Test"; max = 19},
                @{ data = @(100,2,3, "A", 1); min = 1; max = "A"},
                @{ data = @(4,2,3, "ABC", 1, "DEF"); min = 1; max = "DEF"},
                @{ data = @("abc",[Datetime]::Today,"def"); min = [Datetime]::Today.ToString(); max = "def"}
            )
        }

        It "can compare string representation for minimum" {
            $minResult = $firstObject, $secondObject | Measure-Object Header -Minimum
            $minResult.Minimum.ToString() | Should -Be $expectedFirstValue.ToString()
        }

        It "can compare string representation for maximum" {
            $maxResult = $firstObject, $secondObject | Measure-Object Header -Maximum
            $maxResult.Maximum.ToString() | Should -Be $expectedSecondValue.ToString()
        }

        It 'correctly find minimum of (<data>)' -TestCases $testCases {
            param($data, $min, $max)

            $output = $data | Measure-Object -Minimum
            $output.Minimum.ToString() | Should -Be $min
        }

        It 'correctly find maximum of (<data>)' -TestCases $testCases {
            param($data, $min, $max)

            $output = $data | Measure-Object -Maximum
            $output.Maximum.ToString() | Should -Be $max
        }

        It 'returns a GenericMeasureInfoObject' {
            $gmi = 1,2,3 | Measure-Object -max -min
            $gmi | Should -BeOfType Microsoft.PowerShell.Commands.GenericMeasureInfo
        }

        It 'should return correct error for non-numeric input' {
            $gmi = "abc",[Datetime]::Now | Measure-Object -Sum -max -ErrorVariable err -ErrorAction silentlycontinue
            $err | ForEach-Object { $_.FullyQualifiedErrorId | Should -Be 'NonNumericInputObject,Microsoft.PowerShell.Commands.MeasureObjectCommand' }
        }

        It 'should have the correct count' {
            $gmi = "abc",[Datetime]::Now | Measure-Object -Sum -max -ErrorVariable err -ErrorAction silentlycontinue
            $gmi.Count | Should -Be 2
        }
    }
}
