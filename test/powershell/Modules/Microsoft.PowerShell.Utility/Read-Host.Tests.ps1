# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Read-Host Test" -Tag "CI" {
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
        $th.UI.Streams.Clear()
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
        $result | Should -BeExactly 'this is a prompt response'
    }

    It "Read-Host returns a secure string when using -AsSecureString parameter" {
        $result = $ps.AddScript("Read-Host -AsSecureString").Invoke() | Select-Object -First 1
        $result | Should -BeOfType SecureString
        $result | ConvertFrom-SecureString -AsPlainText | Should -BeExactly 'TEST'
    }

    It "Read-Host returns a string when using -MaskInput parameter" {
        $result = $ps.AddScript("Read-Host -MaskInput").Invoke()
        $result | Should -BeExactly 'TEST'
    }

    It "Read-Host returns a string when using -MaskInput parameter used with -Prompt" {
        $result = $ps.AddScript("Read-Host -MaskInput -Prompt Test").Invoke()
        $prompt = $th.ui.streams.prompt[0]
        $prompt | Should -Not -BeNullOrEmpty
        $prompt.split(":")[-1] | Should -BeExactly 'Test'
        $result | Should -BeExactly 'this is a prompt response'
    }

    Context "-DefaultValue" {
        AfterEach {
            $th.UI.promptResponse = 'this is a prompt response'
            $th.UI.ReadLineData = 'This is readline data'
        }

        It "Read-Host -DefaultValue shows the default in the prompt" {
            $null = $ps.AddScript("Read-Host -Prompt myprompt -DefaultValue blue").Invoke()
            # The test host records "caption:message:name"; caption and message are empty here.
            $th.ui.streams.prompt[0] | Should -BeExactly '::myprompt (default: blue)'
        }

        It "Read-Host -DefaultValue returns the default when the prompt answer is empty" {
            $th.UI.promptResponse = ''
            $result = $ps.AddScript("Read-Host -Prompt myprompt -DefaultValue blue").Invoke()
            $result | Should -BeExactly 'blue'
        }

        It "Read-Host -DefaultValue returns what the user typed when it is not empty" {
            $result = $ps.AddScript("Read-Host -Prompt myprompt -DefaultValue blue").Invoke()
            $result | Should -BeExactly 'this is a prompt response'
        }

        It "Read-Host -DefaultValue without -Prompt returns the default when the line is empty" {
            $th.UI.ReadLineData = ''
            $result = $ps.AddScript("Read-Host -DefaultValue blue").Invoke()
            $result | Should -BeExactly 'blue'
        }

        It "Read-Host -DefaultValue cannot be used with -AsSecureString" {
            $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ReadHostCommand"
            { Read-Host -AsSecureString -DefaultValue blue } | Should -Throw -ErrorId $errorId
        }
    }

    It "Read-Host throws an error when both -AsSecureString parameter and -MaskInput parameter are used" {
        # Contrary to the rest of the tests this does not need to be invoked through a runspace since it is going to throw an error.
        $errorId = "AmbiguousParameterSet,Microsoft.PowerShell.Commands.ReadHostCommand"
        {Read-Host -MaskInput -AsSecureString} | Should -Throw -ErrorId $errorId
    }

    It "Read-Host doesn't enter command prompt mode" {
        $result = "!1" | & "$PSHOME/pwsh" -NoProfile -c "Read-host -Prompt 'foo'"
        if ($IsWindows) {
            # Windows write to console directly so can't capture prompt in stdout
            $expected = @('!1','!1')
        }
        else {
            $expected = @('foo: !1','!1')
        }
        $result | Should -BeExactly $expected
    }
}
