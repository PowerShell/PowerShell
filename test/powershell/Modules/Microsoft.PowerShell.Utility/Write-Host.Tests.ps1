Describe "Write-Host DRT Unit Tests" -Tags "Slow","Feature" {

    BeforeAll {
        $powershell = Join-Path -Path $PsHome -ChildPath "powershell"

        $testData = @(
            @{ Name = '-Separator';Command = "Write-Host a,b,c -Separator '+'"; returnCount = 1; returnValue = @("a+b+c") }
            @{ Name = '-NoNewline=true';Command = "Write-Host a,b -NoNewline:`$true;Write-Host a,b"; returnCount = 1; returnValue = @("a ba b") }
            @{ Name = '-NoNewline=false';Command = "Write-Host a1,b1;Write-Host a2,b2"; returnCount = 2; returnValue = @("a1 b1","a2 b2") }
        )
    }

    It "write-Host works with '<Name>' switch" -TestCases:$testData -Pending:$IsOSX {
        param($Command, $returnCount, $returnValue)

        [array]$result = & $powershell -noprofile $Command
        $result.Count | Should Be $returnCount
        $result[0] | Should Be $returnValue[0]
        $result[1] | Should Be $returnValue[1]
    }
}

Describe "Write-Host test wrong colors" -Tags "CI" {

    BeforeAll {
        $testWrongColor = @(
            @{ ForegroundColor = -1; BackgroundColor = 'Red' }
            @{ ForegroundColor = 16; BackgroundColor = 'Red' }
            @{ ForegroundColor = 'Red'; BackgroundColor = -1 }
            @{ ForegroundColor = 'Red'; BackgroundColor = 16 }
        )
    }

    It 'Should throw if color is invalid: ForegroundColor = <ForegroundColor>; BackgroundColor = <BackgroundColor>' -TestCases:$testWrongColor {
      param($ForegroundColor, $BackgroundColor)
      try
      {
          Write-Host "No output from Write-Host" -ForegroundColor $ForegroundColor -BackgroundColor $BackgroundColor
          throw "No Exception!"
      }
      catch { $_.FullyQualifiedErrorId | Should Be 'CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.WriteHostCommand' }
    }
}

Describe "Write-Host test with TestHostCS" -Tags "CI" {

    BeforeAll {
        if ( -not (get-module -ea SilentlyContinue TestHostCS )) {
            $hostmodule = Join-Path $PSScriptRoot "../../Common/TestHostCS.psm1"
            import-module $hostmodule
        }
        $th = New-TestHost
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs

        $testHostCSData = @(
            @{ Name = 'defaults';Command = "Write-Host a,b,c"; returnCount = 1; returnValue = @("White:Black:a b c:NewLine") }
            @{ Name = '-Separator';Command = "Write-Host a,b,c -Separator '+'"; returnCount = 1; returnValue = @("White:Black:a+b+c:NewLine") }
            @{ Name = '-Separator, colors and -NoNewLine';Command = "Write-Host a,b,c -Separator ',' -ForegroundColor Yellow -BackgroundColor DarkBlue -NoNewline"; returnCount = 1; returnValue = @("Yellow:DarkBlue:a,b,c:NoNewLine") }
            @{ Name = '-NoNewline:$true and colors';Command = "Write-Host a,b -NoNewline:`$true -ForegroundColor Red -BackgroundColor Green;Write-Host a,b"; returnCount = 2; returnValue = @("Red:Green:a b:NoNewLine", "White:Black:a b:NewLine") }
            @{ Name = '-NoNewline:$false and colors';Command = "Write-Host a,b -NoNewline:`$false -ForegroundColor Red -BackgroundColor Green;Write-Host a,b"; returnCount = 2; returnValue = @("Red:Green:a b:NewLine","White:Black:a b:NewLine") }
        )

    }

    AfterAll {
        $rs.Close()
        $rs.Dispose()
        $ps.Dispose()
    }

    AfterEach {
        $ps.Commands.Clear()
        $th.ui.Streams.Clear()
    }

    It "write-Host works with <Name>" -TestCases:$testHostCSData {
        param($Command, $returnCount, $returnValue)
        $ps.AddScript($Command).Invoke()
        $result = $th.ui.Streams.ConsoleOutput

        # I wonder that a console output is duplicated in Information Stream - Is it by design?
        Write-Host $th.ui.Streams.Information.Count
        Write-Host $th.ui.Streams.Information[0]
        Write-Host $th.ui.Streams.Information[1]

        $result.Count | Should Be $returnCount
        $result[0] | Should Be $returnValue[0]
        $result[1] | Should Be $returnValue[1]
    }
}
