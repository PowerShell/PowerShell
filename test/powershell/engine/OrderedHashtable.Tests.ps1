# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'Tests for OrderedHashtable' -Tag 'CI' {

    It 'Can create an empty OrderedHashtable' {
        $oh =[System.Management.Automation.OrderedHashtable]::new()
        $oh.Count | Should -Be 0
        $oh.GetType().Name | Should -BeExactly 'OrderedHashtable'
    }

    It 'Can create an empty OrderedHashtable with a capacity' {
        $oh = [System.Management.Automation.OrderedHashtable]::new(10)
        $oh.Count | Should -Be 0
        $oh.GetType().Name | Should -BeExactly 'OrderedHashtable'
    }

    It 'Can create an OrderedHashtable with an initial dictionary' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.Count | Should -Be 2
        $oh['a'] | Should -Be $h['a']
        $oh['b'] | Should -Be $h['b']
    }

    It 'Can use the Add() method' {
        $oh = [System.Management.Automation.OrderedHashtable]::new()
        $oh.Add('a', 1)
        $oh.Add('b', 2)
        $oh.Count | Should -Be 2
        $oh['a'] | Should -Be 1
        $oh['b'] | Should -Be 2
    }

    It 'Can use the Clear() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.Count | Should -Be 2
        $oh.Clear()
        $oh.Count | Should -Be 0
    }

    It 'Can use the Clone() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh2 = $oh.Clone()
        $oh2.Count | Should -Be 2
        $oh2['a'] | Should -Be $h['a']
        $oh2['b'] | Should -Be $h['b']
    }

    It 'Can use the Contains() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.Contains('a') | Should -BeTrue
        $oh.Contains('b') | Should -BeTrue
        $oh.Contains('c') | Should -BeFalse
    }

    It 'Can use the ContainsKey() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.ContainsKey('a') | Should -BeTrue
        $oh.ContainsKey('b') | Should -BeTrue
        $oh.ContainsKey('c') | Should -BeFalse
    }

    It 'Can use the ContainsValue() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.ContainsValue(1) | Should -BeTrue
        $oh.ContainsValue(2) | Should -BeTrue
        $oh.ContainsValue(3) | Should -BeFalse
    }

    It 'Can use the CopyTo() method' {
        $oh = [System.Management.Automation.OrderedHashtable]::new()
        $oh.Add('a', 1)
        $oh.Add('b', 2)
        $oh.Add('c', 3)
        $a = (4, 5, 6, 7)
        $oh.CopyTo($a, 1)
        $a[0] | Should -Be 4
        # OrderedDictionary.CopyTo() doesn't guarantee to preserve order
        # so we can't easily test the values
        $a[1].GetType().Name | Should -BeExactly 'DictionaryEntry'
        $a[2].GetType().Name | Should -BeExactly 'DictionaryEntry'
        $a[3].GetType().Name | Should -BeExactly 'DictionaryEntry'
    }

    It 'Can use the Equals() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh2 = $oh.Clone()
        $oh3 = $oh
        $oh.Equals($oh2) | Should -BeFalse
        $oh.Equals($oh3) | Should -BeTrue
    }

    It 'Can use the GetEnumerator() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.GetEnumerator().GetType().Name | Should -BeExactly 'OrderedDictionaryEnumerator'
    }

    It 'Can use the GetHashCode() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.GetHashCode() | Should -BeGreaterThan 0
        $oh2 = $oh.Clone()
        $oh.GetHashCode() | Should -Not -Be $oh2.GetHashCode()
        $oh3 = $oh
        $oh.GetHashCode() | Should -Be $oh3.GetHashCode()
    }

    It 'Can use Remove() method' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.Remove('a')
        $oh.Count | Should -Be 1
        $oh.Contains('a') | Should -BeFalse
    }

    It 'Can use Item property' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh['a'] | Should -Be 1
        $oh['b'] | Should -Be 2
    }

    It 'Can use IsFixedSize property' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.IsFixedSize | Should -BeFalse
    }

    It 'Can use IsReadOnly property' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.IsReadOnly | Should -BeFalse
    }

    It 'Can use IsSynchronized property' {
        $h = @{ a = 1; b = 2 }
        $oh = [System.Management.Automation.OrderedHashtable]::new($h)
        $oh.IsSynchronized | Should -BeFalse
    }

    It 'Can use Keys property' {
        $oh = [System.Management.Automation.OrderedHashtable]::new()
        $oh['a'] = 1
        $oh['b'] = 2
        $oh.Keys | Should -Be ('a', 'b')
    }

    It 'Can use Values property' {
        $oh = [System.Management.Automation.OrderedHashtable]::new()
        $oh['a'] = 1
        $oh['b'] = 2
        $oh.Values | Should -Be (1, 2)
    }
}
