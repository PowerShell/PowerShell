# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "InitialSessionState capacity" -Tags CI {
    BeforeAll {
        $iss = [initialsessionstate]::CreateDefault()

        for ($i = 0; $i -lt 5000; $i++)
        {
            $ssfe = [System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new("f$i", "'fn f$i'")
            $iss.Commands.Add($ssfe)

            $ssve = [System.Management.Automation.Runspaces.SessionStateVariableEntry]::new("v$i", "var v$i", $null)
            $iss.Variables.Add($ssve)

            $ssae = [System.Management.Automation.Runspaces.SessionStateAliasEntry]::new("a$i", "f$i")
            $iss.Commands.Add($ssae)
        }

        $ps = [PowerShell]::Create($iss)
    }

    AfterAll {
        $ps.Dispose()
    }

    BeforeEach {
        $ps.Commands.Clear()
    }

    It "function capacity in initial session state should not be limited" {
        $ps.AddCommand('f4999').Invoke() | Should -Be "fn f4999"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }

    It "alias capacity in initial session state should not be limited" {
        $ps.AddCommand('a4999').Invoke() | Should -Be "fn f4999"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }

    It "variable capacity in initial session state should not be limited" {
        $ps.AddScript('$v4999').Invoke() | Should -Be "var v4999"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }

    It "function capacity should not be limited after runspace is opened" {
        $ps.AddScript('function f5000 { "in f5000" } f5000').Invoke() | Should -Be "in f5000"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }

    It "variable capacity should not be limited after runspace is opened" {
        $ps.AddScript('$v5000 = "var v5000"; $v5000').Invoke() | Should -Be "var v5000"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }

    It "alias capacity should not be limited after runspace is opened" {
        $ps.AddScript('New-Alias -Name a5000 -Value f1; a5000').Invoke() | Should -Be "fn f1"
        $ps.Streams.Error | Should -BeNullOrEmpty
    }
}

##
## A reused InitialSessionState created from a TypeTable should not have duplicate types.
##
Describe "TypeTable duplicate types in reused runspace InitialSessionState TypeTable" -Tags 'Feature' {

    Context "No duplicate types test" {

        BeforeAll {

            $typeTable = [System.Management.Automation.Runspaces.TypeTable]::new([string[]](Join-Path $PSScriptRoot "assets/TestTypeFile.ps1xml"))
            [initialsessionstate] $iss = [initialsessionstate]::Create()
            $iss.Types.Add($typeTable)
            [runspace] $rs1 = [runspacefactory]::CreateRunspace($iss)

            # Process TypeTable types from ISS
            $rs1.Open()

            # Get processed ISS from runspace.
            $issReused = $rs1.InitialSessionState.Clone()
            $issReused.ThrowOnRunspaceOpenError = $true

            # Create new runspace with reused ISS.
            $rs2 = [runspacefactory]::CreateRunspace($issReused)
        }

        AfterAll {

            if ($null -ne $rs1) { $rs1.Dispose() }
            if ($null -ne $rs2) { $rs2.Dispose() }
        }

        It "Verifies that a reused InitialSessionState object created from a TypeTable object does not have duplicate types" {

            { $rs2.Open() } | Should -Not -Throw
        }
    }

    Context "Cannot use shared TypeTable in ISS test" {

        BeforeAll {

            # Create default ISS and add shared TypeTable.
            $typeTable = [System.Management.Automation.Runspaces.TypeTable]::new([string[]](Join-Path $PSScriptRoot "assets/TestTypeFile.ps1xml"))
            [initialsessionstate] $iss = [initialsessionstate]::CreateDefault2()
            $iss.Types.Add($typeTable)
            $iss.ThrowOnRunspaceOpenError = $true
            [runspace] $rs = [runspacefactory]::CreateRunspace($iss)
        }

        AfterAll {

            if ($null -ne $rs) { $rs.Dispose() }
        }

        It "Verifies that shared TypeTable is not allowed in ISS" {

            # Process TypeTable types from ISS.
            $e = { $rs.Open() } | Should -Throw -ErrorId "RuntimeException" -PassThru
	    $e.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "ErrorsUpdatingTypes"
        }
    }
}
