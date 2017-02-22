$hostmodule = Join-Path $PSScriptRoot "../../Common/TestHostCS.psm1"
import-module $hostmodule -ErrorAction SilentlyContinue

Describe "Read-Host Test" -tag "CI" {
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
    It "Read-Host returns expected string" {
        $result = $ps.AddCommand("Read-Host").Invoke()
        $result | Should Be $th.UI.ReadLineData
    }
    It "Read-Host sets the prompt correctly" {
        $result = $ps.AddScript("Read-Host -prompt myprompt").Invoke()
        $prompt = $th.ui.streams.prompt[0]
        $prompt | should Not BeNullOrEmpty
        $prompt.split(":")[-1] | should be myprompt
    }
    It "Read-Host returns a secure string when using -AsSecureString parameter" {
        $result = $ps.AddScript("Read-Host -AsSecureString").Invoke() | select-object -first 1
        $result | Should BeOfType SecureString
        [pscredential]::New("foo",$result).GetNEtworkCredential().Password | should BeExactly TEST

    }
}
