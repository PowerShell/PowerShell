Describe "SSH Remoting API Tests" -Tags "Feature" {

    Context "SSHConnectionInfo Class Tests" {

        AfterEach {
            if ($rs -ne $null) {
                $rs.Dispose()
            }
        }

        It "SSHConnectionInfo constructor should throw null argument exception for null HostName parameter" {

            { [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                "UserName",
                [System.Management.Automation.Internal.AutomationNull]::Value,
                [System.Management.Automation.Internal.AutomationNull]::Value,
                0) } | ShouldBeErrorId "PSArgumentNullException"
        }

        It "SSHConnectionInfo should throw file not found exception for invalid key file path" {

            try
            {
                $sshConnectionInfo = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    "localhost",
                    "NoValidKeyFilePath",
                    22)

                $rs = [runspacefactory]::CreateRunspace($sshConnectionInfo)
                $rs.Open()
                
                throw "No Exception!"
            }
            catch
            {
                $_.Exception.InnerException.InnerException | Should BeOfType "System.IO.FileNotFoundException"
            }
        }

        It "SSHConnectionInfo should throw argument exception for invalid port (non 16bit uint)" {
            try 
            {
                $sshConnectionInfo = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    "localhost",
                    "ValidKeyFilePath",
                    99999)

                $rs = [runspacefactory]::CreateRunspace($sshConnectionInfo)
                $rs.Open()
                
                throw "No Exception!"
            }
            catch
            {
                $expectedArgumentException = $_.Exception
                if ($_.Exception.InnerException -ne $null)
                {
                    $expectedArgumentException = $_.Exception.InnerException
                }

                $expectedArgumentException | Should BeOfType "System.ArgumentException"
            }
        }
    }
}
