# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Out-Host Tests" -Tag CI {
    BeforeAll {
        $th = New-TestHost
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $ps.Commands.Clear()
    }
    AfterEach {
        $ps.Commands.Clear()
    }
    AfterAll {
        $rs.Close()
        $rs.Dispose()
        $ps.Dispose()
    }
    It "Out-Host writes to host output" {
        $stringToWrite = "thing to write"
        $stringExpected = "::$($stringToWrite):NewLine"
        $result = $ps.AddScript("Out-Host -inputobject '$stringToWrite'").Invoke()
        $th.UI.Streams.ConsoleOutput.Count | Should -Be 1
        $th.UI.Streams.ConsoleOutput[0] | Should -BeExactly $stringExpected
    }
}
