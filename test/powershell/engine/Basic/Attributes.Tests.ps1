# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Attribute tests" -Tags "CI" {
    BeforeEach {
        Remove-Item $testdrive/test.ps1 -Force -ErrorAction SilentlyContinue
    }

    It "<attribute> attribute returns argument error if empty" -TestCases @(
        @{Attribute="HelpMessage"},
        @{Attribute="HelpMessageBaseName"},
        @{Attribute="HelpMessageResourceId"}
    ) {
        param($attribute)

        $script = @"
[CmdletBinding()]
Param (
[Parameter($attribute="")]
[String]`$Parameter1
)
Write-Output "Hello"
"@
        New-Item -Path $testdrive/test.ps1 -Value $script -ItemType File
        { & $testdrive/test.ps1 } | Should -Throw -ErrorId "Argument"
    }
}

describe "ArgumentTransformAttribute Tests" {

    it "Can transform a parameter value using a ScriptBlock" {
        function TestFunc {
            param(
            [ArgumentTransform({
                if ($_ -is [datetime]) {
                    $_.ToFileTimeUTC()
                } elseif ($_ -as [datetime]) {
                    ($_ -as [datetime]).ToFileTimeUTC()
                } else {
                    $_
                }
            })]
            [long]
            $TimeStamp
            )

            process {
                $TimeStamp
            }
        }


        # Grabs the appropriate property from an actual date
        $date = (Get-Date)
        TestFunc -Timestamp $date | Should -Be $date.ToFileTimeUtc()
        $notADate = "2023-12-25T08:00:00.0000000Z"

        # Coerces to date, then grabs the appropriate property.
        TestFunc -Timestamp $notADate | Should -Be ([datetime]$notADate).ToFileTimeUtc()

        # Get the command metadata
        $testFunc = Get-Command TestFunc
        # Find the attribute
        $attr = $testFunc.Parameters["TimeStamp"].Attributes |
            Where-Object { $_ -is [ArgumentTransform]}
        # disable it.
        $attr.Disabled = $true

        # These will now both output a binding error
        { TestFunc -Timestamp (Get-Date) } | Should -Throw

        { TestFunc -Timestamp ("2023-12-25T08:00:00.0000000Z") } | Should -Throw

        # re-enable the attribute
        $attr.Disabled = $false

        # ensure it still works.
        TestFunc -Timestamp $date  | Should -Be $date.ToFileTimeUtc()
    }
}

