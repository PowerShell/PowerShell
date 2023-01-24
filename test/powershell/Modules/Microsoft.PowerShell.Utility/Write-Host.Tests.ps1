# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Write-Host with default Console Host" -Tags "Slow","Feature" {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"

        $testData = @(
            @{ Name = '-Separator';       Command = "`$PSStyle.OutputRendering='Plaintext';Write-Host a,b,c -Separator '+'";                 returnCount = 1; returnValue = @("a+b+c") }
            @{ Name = '-NoNewline=true';  Command = "`$PSStyle.OutputRendering='Plaintext';Write-Host a,b -NoNewline:`$true;Write-Host a,b"; returnCount = 1; returnValue = @("a ba b") }
            @{ Name = '-NoNewline=false'; Command = "`$PSStyle.OutputRendering='Plaintext';Write-Host a1,b1;Write-Host a2,b2";               returnCount = 2; returnValue = @("a1 b1","a2 b2") }
        )
    }

    It "write-Host works with '<Name>' switch" -TestCases:$testData {
        param($Command, $returnCount, $returnValue)

        [array]$result = & $powershell -noprofile -c $Command

        $result.Count | Should -Be $returnCount
        foreach ($i in 0..($returnCount - 1))
        {
            $result[$i] | Should -Be $returnValue[$i]
        }
    }
}

Describe "Write-Host with wrong colors" -Tags "CI" {

    BeforeAll {
        $testWrongColor = @(
            @{ ForegroundColor = -1;    BackgroundColor = 'Red' }
            @{ ForegroundColor = 16;    BackgroundColor = 'Red' }
            @{ ForegroundColor = 'Red'; BackgroundColor = -1 }
            @{ ForegroundColor = 'Red'; BackgroundColor = 16 }
        )
    }

    It 'Should throw if color is invalid: ForegroundColor = <ForegroundColor>; BackgroundColor = <BackgroundColor>' -TestCases:$testWrongColor {
      param($ForegroundColor, $BackgroundColor)
      { Write-Host "No output from Write-Host" -ForegroundColor $ForegroundColor -BackgroundColor $BackgroundColor } | Should -Throw -ErrorId 'CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.WriteHostCommand'
    }
}

Describe "Write-Host with TestHostCS" -Tags "CI" {

    BeforeAll {
        $th = New-TestHost
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs

        $testHostCSData = @(
            @{ Name = 'defaults';                          Command = "Write-Host a,b,c";                                                                             returnCount = 1; returnValue = @("White:Black:a b c:NewLine"); returnInfo = @("a b c") }
            @{ Name = '-Object';                           Command = "Write-Host -Object a,b,c";                                                                     returnCount = 1; returnValue = @("White:Black:a b c:NewLine"); returnInfo = @("a b c") }
            @{ Name = '-Message';                          Command = "Write-Host -Message a,b,c";                                                                    returnCount = 1; returnValue = @("White:Black:a b c:NewLine"); returnInfo = @("a b c") }
            @{ Name = '-Msg';                              Command = "Write-Host -Msg a,b,c";                                                                        returnCount = 1; returnValue = @("White:Black:a b c:NewLine"); returnInfo = @("a b c") }
            @{ Name = '-Separator';                        Command = "Write-Host a,b,c -Separator '+'";                                                              returnCount = 1; returnValue = @("White:Black:a+b+c:NewLine"); returnInfo = @("a+b+c") }
            @{ Name = '-Separator, colors and -NoNewLine'; Command = "Write-Host a,b,c -Separator ',' -ForegroundColor Yellow -BackgroundColor DarkBlue -NoNewline"; returnCount = 1; returnValue = @("Yellow:DarkBlue:a,b,c:NoNewLine"); returnInfo = @("a,b,c") }
            @{ Name = '-NoNewline:$true and colors';       Command = "Write-Host a,b -NoNewline:`$true -ForegroundColor Red -BackgroundColor Green;Write-Host a,b";  returnCount = 2; returnValue = @("Red:Green:a b:NoNewLine", "White:Black:a b:NewLine"); returnInfo = @("a b", "a b") }
            @{ Name = '-NoNewline:$false and colors';      Command = "Write-Host a,b -NoNewline:`$false -ForegroundColor Red -BackgroundColor Green;Write-Host a,b"; returnCount = 2; returnValue = @("Red:Green:a b:NewLine","White:Black:a b:NewLine"); returnInfo = @("a b", "a b") }
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

    It "Write-Host works with <Name>" -TestCases:$testHostCSData {
        param($Command, $returnCount, $returnValue, $returnInfo)
        $ps.AddScript($Command).Invoke()

        $result = $th.ui.Streams.ConsoleOutput

        $result.Count | Should -Be $returnCount
        (Compare-Object $result $returnValue -SyncWindow 0).length | Should -Be 0

        $result = $th.ui.Streams.Information

        $result.Count | Should -Be $returnCount
        (Compare-Object $result $returnInfo -SyncWindow 0).length | Should -Be 0
    }
}
