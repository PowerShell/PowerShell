# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

Describe "Get-Credential Test" -Tag "CI" {
    BeforeAll {
        $th = New-TestHost
        $th.UI.StringForSecureString = "This is a test"
        $th.UI.UserNameForCredential = "John"
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
    AfterEach {
        $ps.Commands.Clear()
    }
    It "Get-Credential with message, produces a credential object" {
        $cred = $ps.AddScript("Get-Credential -UserName Joe -Message Foo").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "this is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:Foo"
    }
    It "Get-Credential with title, produces a credential object" {
        $cred = $ps.AddScript("Get-Credential -UserName Joe -Title CustomTitle").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "this is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:CustomTitle:[^:]+"
    }
    It "Get-Credential with only username, produces a credential object" {
        $cred = $ps.AddScript("Get-Credential -UserName Joe").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "this is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential with title and message, produces a credential object" {
        $cred = $ps.AddScript("Get-Credential -UserName Joe -Message Foo -Title CustomTitle").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "this is a test"
        $th.ui.Streams.Prompt[-1] | Should -Be "Credential:CustomTitle:Foo"
    }
    It "Get-Credential without parameters" {
        $cred = $ps.AddScript("Get-Credential").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "John"
        $netcred.Password | Should -Be "This is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential `$null" {
        $cred = $ps.AddScript("Get-Credential `$null").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "John"
        $netcred.Password | Should -Be "This is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential -Credential `$null" {
        $cred = $ps.AddScript("Get-Credential -Credential `$null").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "John"
        $netcred.Password | Should -Be "This is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential Joe" {
        $cred = $ps.AddScript("Get-Credential Joe").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "This is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential -Credential Joe" {
        $cred = $ps.AddScript("Get-Credential Joe").Invoke() | Select-Object -First 1
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "Joe"
        $netcred.Password | Should -Be "This is a test"
        $th.ui.Streams.Prompt[-1] | Should -Match "Credential:[^:]+:[^:]+"
    }
    It "Get-Credential `$credential" {
        #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
        $password = ConvertTo-SecureString -String "CredTest" -AsPlainText -Force
        $credential = [pscredential]::new("John", $password)

        $cred = Get-Credential $credential
        $cred | Should -BeOfType System.Management.Automation.PSCredential
        $netcred = $cred.GetNetworkCredential()
        $netcred.UserName | Should -Be "John"
        $netcred.Password | Should -Be "CredTest"
    }
}
