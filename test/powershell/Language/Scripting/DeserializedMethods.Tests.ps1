# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "DeserializedMethods" -Tags "CI" {
    It "Deserialized objects shouldn't ever have any methods (unless they are primitive known types)" {
        $a = [collections.arraylist]::new()
        $null = $a.Add(1)
        $null = $a.Add(2)
        $null = $a.Add(3)

        # using linkedlist that implements IEnumerable,
        # but doesn't implement IList or IList<T>
        $x = [collections.generic.linkedlist[int]]::new()
        $null = $x.Add(123)
        $null = $x.Add(456)

        $s = [System.Management.Automation.PSSerializer]::Serialize($x)
        $d = [System.Management.Automation.PSSerializer]::Deserialize($s)

        $d | Get-Member -MemberType *Method* Add | Should -BeNullOrEmpty
    }
}
