Describe "SSH Remoting API Tests" -Tags "Feature" {

    Context "SSHConnectionInfo Class Tests" {

        It "SSHConnectionInfo constructor should throw null argument exception for null HostName parameter" {

            try
            {
                [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    [System.Management.Automation.Internal.AutomationNull]::Value,
                    [System.Management.Automation.Internal.AutomationNull]::Value)

                throw "No Exception!"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "PSArgumentNullException"
            }
        }

        It "SSHConnectionInfo should throw file not found exception for invalid key file path" {

            try
            {
                $sshConnectionInfo = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    "localhost",
                    "NoValidKeyFilePath")

                $rs = [runspacefactory]::CreateRunspace($sshConnectionInfo)
                $rs.Open()

                throw "No Exception!"
            }
            catch
            {
                $_.Exception.InnerException.InnerException | Should BeOfType System.IO.FileNotFoundException
            }
        }
    }
}
