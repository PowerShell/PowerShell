Describe "SSH Remoting Cmdlet Tests" -Tags "Feature" {

    Context "Enter-PSSession Cmdlet" {

        It "HostName parameter set should throw error for invalid key path" {

            try
            {
                Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile
                throw "Enter-PSSession did not throw expected PathNotFound exception."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Match "PathNotFound"
            }
        }
    }

    Context "New-PSSession Cmdlet" {

        It "HostName parameter set should throw error for invalid key path" {

            try
            {
                Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile
                throw "New-PSSession did not throw expected PathNotFound exception."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Match "PathNotFound"
            }
        }
    }

    Context "Invoke-Command Cmdlet" {

        It "Invoke-Command HostName parameter set should throw error for invalid key path" {

            try
            {
                Enter-PSSession -HostName localhost -UserName User -KeyFilePath NoKeyFile
                throw "Invoke-Command did not throw expected PathNotFound exception."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Match "PathNotFound"
            }
        }
    }
}
