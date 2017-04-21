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
        $localHost.IPV4Address | should be '127.0.0.1'
        $localhost.IPV6Address | should be '::1'
    }
    
    It "The count parameter counting to <count>" -TestCases $countCases {
        param($count)
        (Test-Connection -ComputerName localhost -Count $count).Count | should be $count
    }
        
    It "The quiet parameter on a <message> computer" -TestCases $quietTests {
        param($computername, $result)
        Test-Connection -ComputerName $computername -Count 1 -Quiet | should be $result
    }
}

Describe  "Test-Connection Slow Tests" -Tags "Slow" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows )
        {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }
    It "Uses the timeout parameter" {
        (Measure-Command {
                Test-Connection -Computer _bad_computer_name_ -Timeout 10000 -ea silentlycontinue
            }).TotalSeconds | should BeGreaterThan 10.0
    }
}
