Describe "Write-Host DRT Unit Tests" -Tags DRT{ 
    $testData = @(
        @{ Name = 'NoNewline';Command = "Write-Host a,b -Separator ',' -ForegroundColor Yellow -BackgroundColor DarkBlue -NoNewline"; returnValue = "a,b" }
        @{ Name = 'Separator';Command = "Write-Host a,b,c -Separator '+'"; returnValue = "a+b+c" }
    )
   
    It "write-Host works with '<Name>' switch" -TestCases $testData -Pending:$IsOSX { 
        param($Command, $returnValue) 

        If($IsLinux)
        {
            $content = powershell  -noprofile -command $Command
        }
        
        If ($IsWindows)
        {
            $content = powershell.exe -noprofile -command $Command
        }

        $content | Should Be $returnValue
    }
}