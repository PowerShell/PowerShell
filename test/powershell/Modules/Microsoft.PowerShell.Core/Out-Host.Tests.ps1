
if ( ! (get-module -ea silentlycontinue TestHostCS ))
{
    # this is sensitive to the location of this test and the common directory"
    $pestertestroot = resolve-path "$psscriptroot/../.."
    $common = join-path $pestertestroot Common
    $hostmodule = join-path $common TestHostCS.psm1
    import-module $hostmodule
}

Describe "Out-Host Tests" -tag CI {
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
        $result = $ps.AddScript("Out-Host -inputobject '$stringToWrite'").Invoke()
        $th.UI.Streams.ConsoleOutput.Count | should be 1
        $th.UI.Streams.ConsoleOutput[0] | should be $stringToWrite
    }
}
