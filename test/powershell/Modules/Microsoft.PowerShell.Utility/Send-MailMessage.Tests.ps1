# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

function Test-SmtpServer
{
    try
    {
        $tc = New-Object -TypeName System.Net.Sockets.TcpClient -ArgumentList "localhost", 25
        $rv = $tc.Connected
        $tc.Close()
        return $rv
    }
    catch
    {
        return $false
    }
}
function Get-Mail
{
    Param(
        [parameter(Mandatory=$true)]
        [String]
        $mailBox
    )

    $state = "init"
    $mail = Get-Content $mailBox
    $rv = @{}
    foreach ($line in $mail)
    {
        switch ($state)
        {
            "init"
            {
                if ($line.Length -gt 0)
                {
                    $state = "headers"
                }
            }
            "headers"
            {
                if ($line.StartsWith("From: "))
                {
                    $rv.From = $line.Substring(6)
                }
                elseif ($line.StartsWith("To: "))
                {
                    if ($null -eq $rv.To)
                    {
                        $rv.To = @()
                    }

                    $rv.To += $line.Substring(4)
                }
                elseif ($line.StartsWith("Subject: "))
                {
                    $rv.Subject = $line.Substring(9);
                }
                elseif ($line.Length -eq 0)
                {
                    $state = "body"
                }
            }
            "body"
            {
                if ($line.Length -eq 0)
                {
                    $state = "done"
                    continue
                }

                if ($null -eq $rv.Body)
                {
                    $rv.Body = @()
                }

                $rv.Body += $line
            }
        }
    }

    return $rv
}

Describe "Send-MailMessage" -Tags CI {
    BeforeAll {
        $user = [Environment]::UserName
        $domain = [Environment]::MachineName
        $address = "$user@$domain"
        $mailBox = "/var/mail/$user"

        if (-not $IsLinux)
        {
            $ItArgs = @{ Skip = $true }
            $testCases = @{ Name = "(skipped: not Linux)" }
            return
        }

        if (-not (Test-SmtpServer))
        {
            $ItArgs = @{ Pending = $true }
            $testCases = @{ Name = "(pending: no mail server detected)" }
            return
        }

        $inPassword = Select-String "^${user}:" /etc/passwd -ErrorAction SilentlyContinue
        if (-not $inPassword)
        {
            $ItArgs = @{ Pending = $true }
            $testCases = @{ Name = "(pending: user not in /etc/passwd)" }
            return
        }

        # Save content of mail box before running tests
        $mailBoxContent = Get-Content -Path $mailBox -ErrorAction SilentlyContinue

        $ItArgs = @{}
        $testCases = @(
            @{
                Name = "with minimal set"
                InputObject = @{
                    To = $address
                    From = $address
                    Subject = "Subject $(Get-Date)"
                    Body = "Body $(Get-Date)"
                    SmtpServer = "127.0.0.1"
                }
            }
        )
    }

    BeforeEach {
        # Clear mail box before each test
        "" | Set-Content -Path $mailBox -Force -ErrorAction SilentlyContinue
    }

    AfterAll {
        # Restore content of mail box after running tests
        $mailBoxContent | Set-Content -Path $mailBox -Force -ErrorAction SilentlyContinue
    }

    It "Can send mail message using named parameters <Name>" -TestCases $testCases @ItArgs {
        param($InputObject)

        Send-MailMessage @InputObject -ErrorAction SilentlyContinue
        Test-Path -Path $mailBox | Should -BeTrue
        $mail = Get-Mail $mailBox
        $mail.From | Should -BeExactly $InputObject.From
        $mail.To.Count | Should -BeExactly $InputObject.To.Count
        $mail.To | Should -BeExactly $InputObject.To
        $mail.Subject | Should -BeExactly  $InputObject.Subject
        $mail.Body.Count | Should -BeExactly $InputObject.Body.Count
        $mail.Body | Should -BeExactly  $InputObject.Body
    }

    It "Can send mail message using pipline named parameters <Name>" -TestCases $testCases @ItArgs {
        param($InputObject)

        [PsCustomObject]$InputObject | Send-MailMessage -ErrorAction SilentlyContinue
        Test-Path -Path $mailBox | Should -BeTrue
        $mail = Get-Mail $mailBox
        $mail.From | Should -BeExactly $InputObject.From
        $mail.To.Count | Should -BeExactly $InputObject.To.Count
        $mail.To | Should -BeExactly $InputObject.To
        $mail.Subject | Should -BeExactly  $InputObject.Subject
        $mail.Body.Count | Should -BeExactly $InputObject.Body.Count
        $mail.Body | Should -BeExactly  $InputObject.Body
    }
}
