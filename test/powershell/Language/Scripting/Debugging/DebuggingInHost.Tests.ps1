##
## Copyright (c) Microsoft Corporation. All rights reserved.
##
## Debugging in Host tests
##

Describe "Tests Debugger GetCallStack() on runspaces when attached to a WinRM host process" -Tags "CI" {

    It -skip "Disabled test because it is fragile and does not consistently succeed on test VMs" { }
    return

    try
    {
        # Create PSSession
        $wc = [System.Management.Automation.Runspaces.WSManConnectionInfo]::new()
        $rs = [runspacefactory]::CreateRunspace($host, $wc)
        $rs.Open()

        # Get WinRM host process id
        [powershell] $ps = [powershell]::Create()
        $ps.Runspace = $rs
        $result = $ps.AddScript('$pid').Invoke()
        It "Verifies the WinRM host process Id was found" {
            $result | Should Not Be $null
            ($result.Count -eq 1) | Should Be $true
        }
        [int]$winRMHostProcId = $result[0]

        # Run script to stop at breakpoint
        $ps.Commands.Clear()
        $ps.AddScript('"Hello"; Wait-Debugger; "Goodbye"').BeginInvoke()

        # Attach to process.
        Enter-PSHostProcess -Id $winRMHostProcId

        # Get local remote runspace to attached process
        $hostRS = Get-Runspace -Name PSAttachRunspace
        It "Verifies that the attached-to host runspace was found" {
            $hostRS | Should Not Be $null
            ($hostRS.RunspaceStateInfo.State -eq 'Opened') | Should Be $true
        }

        # Wait for host runspace to become available.
        $count = 0
        while (($hostRS.RunspaceAvailability -ne 'Available') -and ($count++ -lt 60))
        {
            sleep -Milliseconds 500
        }
        It "Verifies that the attached-to host runspace is available" {
            ($hostRS.RunspaceAvailability -eq 'Available') | Should Be $true
        }

        # Get call stack from default runspace.
        $script = @'
            $rs = Get-Runspace -Id 1
            if ($null -eq $rs) { throw 'Runspace not found' }
            return $rs.Debugger.GetCallStack()
'@
        [powershell]$psHost = [powershell]::Create()
        $psHost.Runspace = $hostRS
        $psHost.AddScript($script)
        $stack = $psHost.Invoke()

        # Detach from process
        Exit-PSHostProcess

        It "Verifies a call stack was returned from the attached-to host." {
            $stack | Should Not Be $null
            ($stack.Count -gt 0) | Should Be $true
        }
    }
    finally
    {
        # Clean up
        if ($host.IsRunspacePushed) { $host.PopRunspace() }

        if ($null -ne $psHost) { $psHost.Dispose() }
        if ($null -ne $hostRS) { $hostRS.Dispose() }
        if ($null -ne $ps) { $ps.Dispose() }
        if ($null -ne $rs) { $rs.Dispose() }
    }
}
