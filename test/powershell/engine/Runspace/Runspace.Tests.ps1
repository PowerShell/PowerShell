Describe "Runspace operations which" -Tags "DRT" {
    # determine whether you're elevated and skip the remote test in that case
    BeforeAll { 
        $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
        $windowsPrincipal = new-object Security.Principal.WindowsPrincipal $windowsIdentity
        if ($windowsPrincipal.IsInRole("Administrators") -eq 1) 
        { 
            $skip = $false
            $skipReason = ""
        } 
        else 
        { 
            $skip = $true
            $skipReason = " (skipping because we're not an admin)"
        }
    }
    Context "Assign values to DefaultRunspace" {
        BeforeEach {
            $origRS = [runspace]::DefaultRunspace
        }
        # pester requires the original runspace to be available, so we assign
        # and then retrieve. We can't leave the DefaultRunspace
        # in a different state when we call our assertions because 
        # pester needs the environment set up in that runspace
        It "Null can be assigned to DefaultRunspace" {
            [Runspace]::DefaultRunspace = $null
            $newRs = [runspace]::DefaultRunspace
            [Runspace]::DefaultRunspace = $origRS
            $newRs | Should BeNullOrEmpty
        }
        It "A LocalRunspace can be assigned to DefaultRunspace" {
            $nrs = [runspacefactory]::CreateRunspace()
            $nrs.Open()
            [runspace]::DefaultRunspace = $nrs
            $newRS = [runspace]::DefaultRunspace
            [Runspace]::DefaultRunspace = $origRS
            $newRS.Name |Should Not Be $origRS.Name

        }
        It -skip:$skip "A RemoteRunspace fails to be assigned to DefaultRunspace.${skipReason}" {
            try
            {
                $s = New-PSSession localhost
                [runspace]::DefaultRunspace = $s.Runspace 
                throw "Execution OK"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should be ExceptionWhenSetting
                $_.Exception.InnerException.GetType().Name | 
                    Should Be InvalidOperationException
            }
            finally
            {
                if ( $s ) { $s | Remove-PSSession }
            }
        }
    }
    Context "Retrieve the RunspaceIsRemote property and validate" {
        It "The default runspace is not Remote" {
            [Runspace]::DefaultRunspace.RunspaceIsRemote | should be $false
        }
        It "A runspace created by runspacefactory without arguments is not Remote" {
            [RunspaceFactory]::CreateRunspace().RunspaceIsRemote | should be $false
        }
        It -skip:$skip "A Runspace created as part of new-pssession is Remote.${skipReason}" {
            try
            {
                $s = new-PsSession localhost
                $s.Runspace.RunspaceIsRemote | Should be $true
            }
            finally
            {
                if ( $s ) { $s | Remove-PsSession }
            }
        }
    }
}
