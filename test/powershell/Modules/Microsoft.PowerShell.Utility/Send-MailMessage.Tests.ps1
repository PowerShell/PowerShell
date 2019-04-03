# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Send-MailMessage" -Tags CI, RequireSudoOnUnix {
    BeforeAll {
        Register-PackageSource -Name nuget.org -Location https://api.nuget.org/v3/index.json -ProviderName NuGet -ErrorAction SilentlyContinue

        $nugetPackage = "netDumbster"
        Install-Package -Name $nugetPackage -ProviderName NuGet -Scope CurrentUser -Force -Source 'nuget.org'

        $dll = "$(Split-Path (Get-Package $nugetPackage).Source)\lib\netstandard2.0\netDumbster.dll"
        Add-Type -Path $dll

        $server = [netDumbster.smtp.SimpleSmtpServer]::Start(25)

        function Read-Mail
        {
            param()

            if($server)
            {
                return $server.ReceivedEmail[0]
            }
            return $null
        }
    }

    AfterEach {
        if($server)
        {
            $server.ClearReceivedEmail()
        }
    }

    AfterAll {
        if($server)
        {
            $server.Stop()
        }
    }

    $testCases = @(
        @{
            Name = "with mandatory parameters"
            InputObject = @{
                From = "user01@example.com"
                To = "user02@example.com"
                Subject = "Subject $(Get-Date)"
                Body = "Body $(Get-Date)"
                SmtpServer = "127.0.0.1"
            }
        }
        @{
            Name = "with ReplyTo"
            InputObject = @{
                From = "user01@example.com"
                To = "user02@example.com"
                ReplyTo = "noreply@example.com"
                Subject = "Subject $(Get-Date)"
                Body = "Body $(Get-Date)"
                SmtpServer = "127.0.0.1"
            }
        }
        @{
            Name = "with No Subject"
            InputObject = @{
                From = "user01@example.com"
                To = "user02@example.com"
                ReplyTo = "noreply@example.com"
                Body = "Body $(Get-Date)"
                SmtpServer = "127.0.0.1"
            }
        }
    )

    It "Can send mail message using named parameters <Name>" -TestCases $testCases {
        param($InputObject)

        $server | Should -Not -Be $null

        $powershell = [PowerShell]::Create()

        $null = $powershell.AddCommand("Send-MailMessage").AddParameters($InputObject).AddParameter("ErrorAction","SilentlyContinue")

        $powershell.Invoke()

        $warnings = $powershell.Streams.Warning

        $warnings.count | Should -BeGreaterThan 0
        $warnings[0].ToString() | Should  -BeLike  "The command 'Send-MailMessage' is obsolete. *"

        $mail = Read-Mail

        $mail.FromAddress | Should -BeExactly $InputObject.From
        $mail.ToAddresses | Should -BeExactly $InputObject.To

        $mail.Headers["From"] | Should -BeExactly $InputObject.From
        $mail.Headers["To"] | Should -BeExactly $InputObject.To
        $mail.Headers["Reply-To"] | Should -BeExactly $InputObject.ReplyTo
        If ($InputObject.Subject -ne $null) {
            $mail.Headers["Subject"] | Should -BeExactly $InputObject.Subject
        }

        $mail.MessageParts.Count | Should -BeExactly 1
        $mail.MessageParts[0].BodyData | Should -BeExactly $InputObject.Body
    }

    It "Can send mail message using pipline named parameters <Name>" -TestCases $testCases -Pending {
        param($InputObject)

        Set-TestInconclusive "As of right now the Send-MailMessage cmdlet does not support piping named parameters (see issue 7591)"

        $server | Should -Not -Be $null

        [PsCustomObject]$InputObject | Send-MailMessage -ErrorAction SilentlyContinue

        $mail = Read-Mail

        $mail.FromAddress | Should -BeExactly $InputObject.From
        $mail.ToAddresses | Should -BeExactly $InputObject.To

        $mail.Headers["From"] | Should -BeExactly $InputObject.From
        $mail.Headers["To"] | Should -BeExactly $InputObject.To
        $mail.Headers["Reply-To"] | Should -BeExactly $InputObject.ReplyTo
        If ($InputObject.Subject -ne $null) {
            $mail.Headers["Subject"] | Should -BeExactly $InputObject.Subject
        }

        $mail.MessageParts.Count | Should -BeExactly 1
        $mail.MessageParts[0].BodyData | Should -BeExactly $InputObject.Body
    }
}
