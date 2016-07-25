Describe "History cmdlet test cases" -Tags "CI" {
	
	It "Tests Invoke-History on a cmdlet that generates output on all streams" {
        $streamSpammer = '
        function StreamSpammer
        {
            [CmdletBinding()]
            param()
            
            Write-Debug "Debug"
            Write-Error "Error"
            Write-Information "Information"
            Write-Progress "Progress"
            Write-Verbose "Verbose"
            Write-Warning "Warning"
            "Output"
        }

        $informationPreference = "Continue"
        $debugPreference = "Continue"
        $verbosePreference = "Continue"
        '

        $invocationSettings = New-Object System.Management.Automation.PSInvocationSettings
        $invocationSettings.AddToHistory = $true
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript($streamSpammer).Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("StreamSpammer");
        $null = $ps.Invoke($null, $invocationSettings)
        $ps.Commands.Clear()
        $null = $ps.AddScript("Invoke-History -id 1")
        $result = $ps.Invoke($null, $invocationSettings)
        $outputCount = $(
            $ps.Streams.Error;
            $ps.Streams.Progress;
            $ps.Streams.Verbose;
            $ps.Streams.Debug;
            $ps.Streams.Warning;
            $ps.Streams.Information).Count
        $ps.Dispose()
        
        ## Twice per stream - once for the original invocatgion, and once for the re-invocation
        $outputCount | Should be 12
    }   

	It "Tests Invoke-History on a private command" {
        
        $invocationSettings = New-Object System.Management.Automation.PSInvocationSettings
        $invocationSettings.AddToHistory = $true
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("(Get-Command Get-Process).Visibility = 'Private'").Invoke()
        $ps.Commands.Clear()
        $null = $ps.AddScript("Get-Process -id $pid")
        $null = $ps.Invoke($null, $invocationSettings)
        $ps.Commands.Clear()
        $null = $ps.AddScript("Invoke-History -id 1")
        $result = $ps.Invoke($null, $invocationSettings)
        $errorResult = $ps.Streams.Error[0].FullyQualifiedErrorId
        $ps.Dispose()
        
        $errorResult | Should be CommandNotFoundException
    }
}
