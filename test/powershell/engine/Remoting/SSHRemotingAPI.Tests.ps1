Describe "SSH Remoting API Tests" -Tags "Feature" {

    Context "SSHConnectionInfo Class Tests" {

        It "SSHConnectionInfo constructor should throw null argument exception for null UserName parameter" {

            try
            {
                [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    [System.Management.Automation.Internal.AutomationNull]::Value,
                    "localhost",
                    [System.Management.Automation.Internal.AutomationNull]::Value)

                throw "SSHConnectionInfo constructor did not throw expected PSArgumentNullException exception"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Match "PSArgumentNullException"
            }
        }

        It "SSHConnectionInfo constructor should throw null argument exception for null HostName parameter" {

            try
            {
                [System.Management.Automation.Runspaces.SSHConnectionInfo]::new(
                    "UserName",
                    [System.Management.Automation.Internal.AutomationNull]::Value,
                    [System.Management.Automation.Internal.AutomationNull]::Value)

                throw "SSHConnectionInfo constructor did not throw expected PSArgumentNullException exception"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Match "PSArgumentNullException"
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

                throw "SSHConnectionInfo did not throw expected FileNotFoundException exception"
            }
            catch
            {
                $expectedFileNotFoundExecption = $null
                if (($_.Exception -ne $null) -and ($_.Exception.InnerException -ne $null))
                {
                    $expectedFileNotFoundExecption = $_.Exception.InnerException.InnerException
                }

                ($expectedFileNotFoundExecption.GetType().FullName) | Should Be "System.IO.FileNotFoundException"
            }
        }
    }
}
