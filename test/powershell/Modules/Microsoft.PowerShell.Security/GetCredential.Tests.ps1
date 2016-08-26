if ( ! (get-module -ea silentlycontinue TestHostCS ))
{
    $root = git rev-parse --show-toplevel
    $pestertestroot = join-path $root test/powershell
    $common = join-path $pestertestroot Common
    $hostmodule = join-path $common TestHostCS.psm1
    import-module $hostmodule
}
Describe "Get-Credential Test" -tag "CI" {
    BeforeAll {
        $th = New-TestHost
        $th.UI.StringForSecureString = "This is a test"
        $rs = [runspacefactory]::Createrunspace($th)
        $rs.open()
        $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $ps.Commands.Clear()
    }
    AfterAll {
        $rs.Close()
        $rs.Dispose()
        $ps.Dispose()
    }
    It "Get-Credential produces as credential object" {
        $cred = $ps.AddScript("Get-Credential -User Joe -Message Foo").Invoke() | Select-Object -First 1
        $cred.gettype().FullName | Should Be "System.Management.Automation.PSCredential"
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should be "Joe"
        $netcred.Password | Should be "this is a test"
    }
}
