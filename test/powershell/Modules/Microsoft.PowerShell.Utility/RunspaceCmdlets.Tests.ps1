# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Runspace cmdlet tests" -Tag "CI" {
    BeforeAll {
        $CurrentRunspace = $ExecutionContext.Host.Runspace
        $ExpectedInstanceId = $CurrentRunspace.InstanceId
        $ExpectedId = $currentRunspace.Id
    }
    It "Get-Runspace should return the current runspace" {
        $runspace = get-runspace |Sort-Object -property id | Select-Object -first 1
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace InstanceId should return the correct runspace" {
        $runspace = get-runspace -instanceid $CurrentRunspace.InstanceId
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace name should return the correct runspace" {
        $runspace = get-runspace -name $currentRunspace.Name
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace Id should return the correct runspace" {
        $runspace = get-runspace -id $CurrentRunspace.Id
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    Context "Multiple Runspaces" {
        BeforeAll {
            $runspaceCount = @(get-runspace).count
            $r1 = [runspacefactory]::CreateRunspace()
            $r1.Open()
            $r2 = [runspacefactory]::CreateRunspace()
        }
        AfterAll {
            $r1.Dispose()
            $r2.Dispose()
        }
        It "Get-Runspace should return the new runspaces" {
            $result = get-runspace
            # if the ids don't match, we'll get null passed to should
            $result.id | Where-Object {$_ -eq $r1.id } | Should -Be $r1.id
            $result.id | Where-Object {$_ -eq $r2.id } | Should -Be $r2.id
        }
    }
}
