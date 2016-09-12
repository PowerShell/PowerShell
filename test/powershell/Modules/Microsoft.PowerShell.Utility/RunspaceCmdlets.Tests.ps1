Describe "Get-Runspace cmdlet tests" -Tag "CI" {
    BeforeAll {
        # Clear out all unexpected runspaces
        # id 1 is the one we started with
        Get-Runspace |?{$_.id -ne 1} | %{$_.dispose()}
        $CurrentRunspace = $ExecutionContext.Host.Runspace
        $ExpectedInstanceId = $CurrentRunspace.InstanceId
        $ExpectedId = $currentRunspace.Id
    }
    It "Get-Runspace should return the current runspace" {
        $runspace = get-runspace
        $runspace.InstanceId | Should be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace InstanceId should return the correct runspace" {
        $runspace = get-runspace -instanceid $CurrentRunspace.InstanceId
        $runspace.InstanceId | Should be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace Id should return the correct runspace" {
        $runspace = get-runspace -id $CurrentRunspace.Id
        $runspace.InstanceId | Should be $ExpectedInstanceId
    }        
    Context "Multiple Runspaces" {
        BeforeAll {
            $runspaceCount = @(get-runspace).count
            $r1 = [runspacefactory]::CreateRunspace()
            $r1.Open()
            $r2 = [runspacefactory]::CreateRunspace()
        }
        AfterAll {
            $r1.Close()
            $r1.Dispose()
            $r2.Dispose()
        }
        It "Get-Runspace should return all runspaces" {
            $expectedCount = $runspaceCount + 2
            (get-runspace).Count | should be $expectedCount
        }
    }
}
