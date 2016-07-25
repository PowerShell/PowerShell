Describe "Write-Host DRT Unit Tests" -Tags "CI" {

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
    }


    $testData = @(
        @{ Name = 'NoNewline';Command = "Write-Host a,b -Separator ',' -ForegroundColor Yellow -BackgroundColor DarkBlue -NoNewline"; returnValue = "a,b" }
        @{ Name = 'Separator';Command = "Write-Host a,b,c -Separator '+'"; returnValue = "a+b+c" }
    )

    It "write-Host works with '<Name>' switch" -TestCases $testData -Pending:$IsOSX {
        param($Command, $returnValue)

        & $powershell -noprofile $Command | Should Be $returnValue
    }
}
