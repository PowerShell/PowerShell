Describe "InvokeOnRunspace method argument error handling" -tags "CI" {

    $command = [System.Management.Automation.PSCommand]::new()
    $localRunspace = $host.Runspace

    $ex = $null
    try
    {
        [System.Management.Automation.HostUtilities]::InvokeOnRunspace($null, $localRunspace)
    }
    catch [System.Management.Automation.PSArgumentNullException]
    {
        $ex = $_
    }

    It "Null argument exception should be thrown for null PSCommand argument" {
        $ex | Should Not Be $null
    }

    $ex = $null
    try
    {
        [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $null)
    }
    catch [System.Management.Automation.PSArgumentNullException]
    {
        $ex = $_
    }

    It "Null argument exception should be thrown for null Runspace argument" {
        $ex | Should Not Be $null
    }
}

Describe "InvokeOnRunspace method as nested command" -tags "Feature" {

    $command = [System.Management.Automation.PSCommand]::new()
    $command.AddScript('"Hello!"')
    $currentRunspace = $host.Runspace

    $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $currentRunspace)

    It "Method should successfully invoke command as nested on busy runspace" {
        $results[0] | Should Be "Hello!"
    }
}

Describe "InvokeOnRunspace method on remote runspace" -tags "Feature" {
    
    BeforeAll {
        $wc = [System.Management.Automation.Runspaces.WSManConnectionInfo]::new()
        $remoteRunspace = [runspacefactory]::CreateRunspace($host, $wc)
        $remoteRunspace.Open()
    }

    AfterAll {
        $remoteRunspace.Dispose();
    }

    $command = [System.Management.Automation.PSCommand]::new()
    $command.AddScript('"Hello!"')

    $results = [System.Management.Automation.HostUtilities]::InvokeOnRunspace($command, $remoteRunspace)

    It "Method should successfully invoke command on remote runspace" {
        $results[0] | Should Be "Hello!"
    }
}
