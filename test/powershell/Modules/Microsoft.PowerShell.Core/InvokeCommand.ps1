##
##  Invoke-Command remoting tests
##  Copyright (c) Microsoft Corporation, 2016
##

Describe "Invoke-Command on down level PSv2 endpoint" -Tags 'Innerloop','P1' {

    # Only run these tests if .NET 2.0 and PS 2.0 is installed on the machine
    if (! (test-path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v2.0.50727') -or
        ! (test-path 'HKLM:\SOFTWARE\Microsoft\PowerShell\1\PowerShellEngine')
        )
    {
        It -SKip "PS 2.0 not installed.  Skipping test."
        return
    }

    $configName = "InvokeCommandDLEPTest"

    try
    {
        $null = Register-PSSessionConfiguration -Name $configName -PSVersion 2.0 -Force
        $s = New-PSSession -ComputerName . -ConfigurationName $configName

        # Script commands with EndOfStatements should run
        $results = Invoke-Command $s { Write-Output "Output 1"; Write-Output "Output 2" }
        It "Verifies that script with mutiple statements run in a remote PSv2 session" {
            $results.Count | Should Be 2
            $results[0] | Should Be "Output 1"
            $results[1] | Should Be "Output 2"
        }

        # Script commands running in NoLanguage session
        Invoke-Command $s { $ExecutionContext.SessionState.LanguageMode = "NoLanguage" }

        # Expected to succeed since client should send PowerShell command and not script
        $results = Invoke-Command $s { param([string] $toOut) Write-Output $toOut } -ArgumentList "Output 1"
        It "Verifies that commands still run in NoLanguage mode session" {
            $results | Should Be "Output 1"
        }

        # Expected to fail since client can only send script, which won't run in NoLanguage mode
        $err = $null
        $null = Invoke-Command $s { param([string] $out1, [string] $out2) Write-Output $out1; Write-Output $out2 } `
            -ArgumentList "Output 1","Output 2" -ErrorAction SilentlyContinue -ErrorVariable err
        It "Verifies that Invoke-Command with multiple statements fails on PSv2 endpoint" {
            $err | Should Not Be $null
            $err.FullyQualifiedErrorId | Should Match "ScriptsNotAllowed"
        }
    }
    finally
    {
        if ($s -ne $null) { Remove-PSSession $s -ErrorAction SilentlyContinue }
        Unregister-PSSessionConfiguration -Name $configName -Force -ErrorAction SilentlyContinue
    }
}
