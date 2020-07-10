# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# https://www.newtonsoft.com/json/help/html/ParsingLINQtoJSON.htm

Describe "Json.NET LINQ Parsing" -tags "CI" {
    BeforeEach {
	$jsonFile = Join-Path -Path (Join-Path $PSScriptRoot -ChildPath assets) -ChildPath TestJson.json
	$jsonData = (Get-Content $jsonFile | Out-String)
	$json = [Newtonsoft.Json.Linq.JObject]::Parse($jsonData)
    }

    It "Should return data via Item()" {
	[string]$json.Item("Name") | Should -BeExactly "Zaphod Beeblebrox"
    }

    It "Should return data via []" {
	[string]$json["Planet"] | Should -BeExactly "Betelgeuse"
    }

    It "Should return nested data via Item().Item()" {
	[int]$json.Item("Appendages").Item("Heads") | Should -Be 2
    }

    It "Should return nested data via [][]" {
	[int]$json["Appendages"]["Arms"] | Should -Be 3
    }

    It "Should return correct array count" {
	$json["Achievements"].Count | Should -Be 4
    }

    It "Should return array data via [n]" {
	[string]$json["Achievements"][3] | Should -BeExactly "One hoopy frood"
    }
}
