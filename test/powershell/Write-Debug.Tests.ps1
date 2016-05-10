Describe "Write-Debug DRT Unit Tests" -Tags DRT{     
    function unittest-writedebugline
    {
        $DebugPreference = 'continue'
        Write-Debug "this is a test" 
    }
    
    function unittest-writedebug
    {
        [cmdletbinding()]
        Param (
            [String]$Value
        )

        $PSBoundParameters.GetEnumerator() | ForEach {
            Write-Verbose $_
        }
        Write-Verbose "DebugPreference: $DebugPreference"

        If ($PSBoundParameters['Debug']) {
        $DebugPreference = 'Continue'
        }

        Write-Debug $Value
    }

    It 'Write-Debug Test1: $DebugPreference is set' {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("`$o = 'this is a test';`$DebugPreference = 'continue';Write-Debug `$o").Invoke()
        $ps.Streams.Debug | Should Be 'this is a test'
    }

    It "Write-Debug Test2: Calling a cmdleting binding and passing -Debug " {      
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("Write-Debug foo -debug").Invoke()
        $ps.Streams.Debug | Should Be 'foo'
    }
    
    It "Write-Debug Test3: Calling a regular function" {              
        unittest-writedebugline 5>&1 | Should Be 'this is a test'
    }

    It "Write-Debug Test4: Redirecting the debug stream" {
        unittest-writedebug -Value "foo" -Debug 5>&1 | Should Be 'foo'
    }
}
