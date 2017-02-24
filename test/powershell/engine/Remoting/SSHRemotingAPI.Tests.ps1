
Describe "SSH Remoting API Tests" -Tags "Feature" {

    Context "SSHConnectionInfo Class Tests" {

        It "SSHConnectionInfo constructor should throw null argument exception for null HostName parameter" {
            {
                [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    [System.Management.Automation.Internal.AutomationNull]::Value,
                    [System.Management.Automation.Internal.AutomationNull]::Value)
            } | ShouldBeErrorId "PSArgumentNullException"
        }

        It "SSHConnectionInfo should throw file not found exception for invalid key file path" {

            $exc = {
                $sshConnectionInfo = [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    "localhost",
                    "NoValidKeyFilePath")

                $rs = [runspacefactory]::CreateRunspace($sshConnectionInfo)
                $rs.Open()
            } | ShouldBeErrorId "PSRemotingDataStructureException"
            $expectedFileNotFoundException = $exc.Exception.InnerException.InnerException
            $expectedFileNotFoundException | Should Not BeNullOrEmpty
            $expectedFileNotFoundException | Should BeOfType System.IO.FileNotFoundException
        }
    }
}
