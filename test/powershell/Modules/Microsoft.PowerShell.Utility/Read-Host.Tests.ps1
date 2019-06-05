# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
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
        $result | Should -Be $th.UI.ReadLineData
    }

    It "Read-Host sets the prompt correctly" {
        $result = $ps.AddScript("Read-Host -prompt myprompt").Invoke()
        $prompt = $th.ui.streams.prompt[0]
        $prompt | Should -Not -BeNullOrEmpty
        $prompt.split(":")[-1] | Should -Be myprompt
    }

    It "Read-Host returns a secure string when using -AsSecureString parameter" {
        $result = $ps.AddScript("Read-Host -AsSecureString").Invoke() | select-object -first 1
        $result | Should -BeOfType SecureString
        [pscredential]::New("foo",$result).GetNetworkCredential().Password | Should -BeExactly TEST
    }

    It "Read-Host doesn't enter command prompt mode" {
        $result = "!1" | pwsh -NoProfile -c "Read-host -Prompt 'foo'"
        if ($IsWindows) {
            # Windows write to console directly so can't capture prompt in stdout
            $expected = @('!1','!1')
        }
        else {
            $expected = @('foo: !1','!1')
        }
        $result | should -BeExactly $expected
    }
}
