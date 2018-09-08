# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Basic Send-MailMessage tests" -Tags CI {
    BeforeAll {
        function test-smtpserver
        {
            $rv = $false

            try
            {
                $tc = New-Object -TypeName System.Net.Sockets.TcpClient -ArgumentList "localhost", 25
                $rv = $tc.Connected
                $tc.Close()
            }
            catch
            {
                $rv = false
            }

            return $rv
        }

        function read-mail
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

        $PesterArgs = @{Name = ""}
        $alreadyHasMail = $true

        if (-not $IsLinux)
        {
            $PesterArgs["Skip"] = $true
            $PesterArgs["Name"] += " (skipped: not Linux)"
            return
        }

        $domain = [Environment]::MachineName
        if (-not (test-smtpserver))
        {
            $PesterArgs["Pending"] = $true
            $PesterArgs["Name"] += " (pending: no mail server detected)"
            return
        }

        $user = [Environment]::UserName
        $inPassword = Select-String "^${user}:" /etc/passwd -ErrorAction SilentlyContinue
        if (-not $inPassword)
        {
            $PesterArgs["Pending"] = $true
            $PesterArgs["Name"] += " (pending: user not in /etc/passwd)"
            return
        }

        $address = "$user@$domain"
        $mailStore = "/var/mail"
        $mailBox = Join-Path $mailStore $user
        $mailBoxFile = Get-Item $mailBox -ErrorAction SilentlyContinue
        if ($null -ne $mailBoxFile -and $mailBoxFile.Length -gt 2)
        {
            $PesterArgs["Pending"] = $true
            $PesterArgs["Name"] += " (pending: mailbox not empty)"
            return
        }
        $alreadyHasMail = $false
    }

    AfterEach {
       if (-not $alreadyHasMail)
       {
           Set-Content -Value "" -Path $mailBox -Force -ErrorAction SilentlyContinue
       }
    }

    $ItArgs = $PesterArgs.Clone()
    $ItArgs['Name'] = "Can send mail message from user to self " + $ItArgs['Name']

    It @ItArgs {
        $body = "Greetings from me."
        $subject = "Test message"
        Send-MailMessage -To $address -From $address -Subject $subject -Body $body -SmtpServer 127.0.0.1
        Test-Path -Path $mailBox | Should -BeTrue
        $mail = read-mail $mailBox
        $mail.From | Should -BeExactly $address
        $mail.To.Count | Should -BeExactly 1
        $mail.To[0] | Should -BeExactly $address
        $mail.Subject | Should -BeExactly $subject
        $mail.Body.Count | Should -BeExactly 1
        $mail.Body[0] | Should -BeExactly $body
    }

    $ItArgs = $PesterArgs.Clone()
    $ItArgs['Name'] = "Can send mail message from user to self using pipeline " + $ItArgs['Name']

    It @ItArgs {
        $body = "Greetings from me again."
        $subject = "Second test message"
        $object = [PSCustomObject]@{To = $address; From = $address; Subject = $subject; Body = $body; SmtpServer = '127.0.0.1'}
        $object | Send-MailMessage
        Test-Path -Path $mailBox | Should -BeTrue
        $mail = read-mail $mailBox
        $mail.From | Should -BeExactly $address
        $mail.To.Count | Should -BeExactly 1
        $mail.To[0] | Should -BeExactly $address
        $mail.Subject | Should -BeExactly $subject
        $mail.Body.Count | Should -BeExactly 1
        $mail.Body[0] | Should -BeExactly $body
    }
}
