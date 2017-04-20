Describe "Test-Connection" -Tags "CI" {
    if ( -not($IsWindows)) {
         $PSDefaultParameterValues["it:skip"] = $true
    }
    Context "Querying localhost" {
        it "Gets the right IP Address" {
            $localHost = Test-Connection -ComputerName localhost -Count 1
            $localHost.IPV4Address | should be '127.0.0.1'
            $localhost.IPV6Address | should be '::1'
        }

        $countCases = @(
            @{count = 2}
            @{count = 3}
        )
        it "Respects the count parameter counting to <count>" -TestCases $countCases {
            param($count)
            (Test-Connection -ComputerName localhost -Count $count).Count | should be $count
        }
        
        $quietTests = @(
            @{computerName = 'localhost'; message = 'good' ; result = $true}
            @{computerName = '_fake_computer_namex'; message = 'bad' ; result = $false}
        )
        it "Respects the quiet parameter on a <message> computer" -TestCases $quietTests {
            param($computername,$result)
            Test-Connection -ComputerName $computername -Count 1 -Quiet | should be $result
        }

        it "Respects the timeout parameter" {
            $start = [datetime]::now
            Test-Connection -Computer _bad_computer_name_ -Timeout 10000 -ea silentlycontinue
            $end = [datetime]::now
            ($end - $start).TotalSeconds | should BeGreaterThan 10.0
        }
    }
}
