Describe "Test-Connection" -Tags "CI" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows )
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
        else
        {
            $countCases = @(
                @{count = 2}
                @{count = 3}
            )

            $quietTests = @(
                @{computerName = 'localhost'; message = 'online' ; result = $true}
                @{computerName = '_fake_computer_namex'; message = 'offline' ; result = $false}
            )
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }
    It "Gets the right IP Address" {
        $localHost = Test-Connection -ComputerName localhost -Count 1
        $localHost.IPV4Address | Should Be '127.0.0.1'
        $localhost.IPV6Address | Should Be '::1'
    }
    
    It "The count parameter counting to <count>" -TestCases $countCases {
        param($count)
        (Test-Connection -ComputerName localhost -Count $count).Count | Should Be $count
    }
        
    It "The quiet parameter on a <message> computer" -TestCases $quietTests {
        param($computername, $result)
        Test-Connection -ComputerName $computername -Count 1 -Quiet | Should Be $result
    }
}
