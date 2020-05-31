# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Runspace cmdlet tests" -Tag "CI" {
    BeforeAll {
        $CurrentRunspace = $ExecutionContext.Host.Runspace
        $ExpectedInstanceId = $CurrentRunspace.InstanceId
        $ExpectedId = $currentRunspace.Id
    }
    It "Get-Runspace should return the current runspace" {
        $runspace = Get-Runspace |Sort-Object -Property id | Select-Object -First 1
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace InstanceId should return the correct runspace" {
        $runspace = Get-Runspace -InstanceId $CurrentRunspace.InstanceId
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace name should return the correct runspace" {
        $runspace = Get-Runspace -Name $currentRunspace.Name
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    It "Get-Runspace with runspace Id should return the correct runspace" {
        $runspace = Get-Runspace -Id $CurrentRunspace.Id
        $runspace.InstanceId | Should -Be $ExpectedInstanceId
    }
    Context "Multiple Runspaces" {
        BeforeAll {
            $runspaceCount = @(Get-Runspace).count
            $r1 = [runspacefactory]::CreateRunspace()
            $r1.Open()
            $r2 = [runspacefactory]::CreateRunspace()
        }
        AfterAll {
            $r1.Dispose()
            $r2.Dispose()
        }
        It "Get-Runspace should return the new runspaces" {
            $result = Get-Runspace
            # if the ids don't match, we'll get null passed to should
            $result.id | Where-Object {$_ -eq $r1.id } | Should -Be $r1.id
            $result.id | Where-Object {$_ -eq $r2.id } | Should -Be $r2.id
        }
    }
}
