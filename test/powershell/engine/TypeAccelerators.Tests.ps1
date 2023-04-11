# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for TypeAccelerators' -Tag 'CI' {
    AfterEach {
        $null = [TypeAccelerators]::Remove('DummyType')
    }

    It 'Creates accelerator' {
        [TypeAccelerators]::Add('DummyType', [int])
        [DummyType].FullName | Should -Be System.Int32
    }

    It 'Creates accelerator for generic type' {
        [TypeAccelerators]::Add('DummyType', [System.Collections.Generic.List`1])
        [DummyType].FullName | Should -Be 'System.Collections.Generic.List`1'

        $list = [DummyType[int]]::new()
        $list.Add('1')
        $list[0] | Should -BeOfType ([int])
        $list[0] | Should -Be 1
    }

    It 'Removes accelerators' {
        # Run twice to check that it doesn't throw
        [TypeAccelerators]::Remove('DummyType') | Should -BeTrue
        [TypeAccelerators]::Remove('DummyType') | Should -BeTrue
    }

    It 'Gets accelerators' {
        [TypeAccelerators]::Add('DummyType', [int])
        $actual = [TypeAccelerators]::Get
        $actual.ContainsKey('DummyType') | Should -BeTrue
    }
}
