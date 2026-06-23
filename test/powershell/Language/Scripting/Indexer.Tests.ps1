# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Tests for indexers' -Tags "CI" {
    It 'Indexer in dictionary' {

        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello"] | Should -BeExactly "There"
    }

    It 'Accessing a Indexed property of a dictionary that does not exist should return $null' {
        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello There"] | Should -BeNullOrEmpty
        }

    It 'CimClass implements an indexer' -Skip:(-not $IsWindows)  {

        $service = Get-CimClass -ClassName Win32_Service

        $service.CimClassProperties["DisplayName"].Name | Should -BeExactly 'DisplayName'
    }

    It 'Accessing a Indexed property of a CimClass that does not exist should return $null' -Skip:(-not $IsWindows) {

        $service = Get-CimClass -ClassName Win32_Service
        $service.CimClassProperties["Hello There"] | Should -BeNullOrEmpty
    }

    It 'ITuple implementations can be indexed' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[0] | Should -Be 10
        $tuple[1] | Should -BeExactly 'Hello'
    }

    It 'ITuple objects can be spliced' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[0..1] | Should -Be @(10, 'Hello')
    }

    It 'Index of -1 should return the last item for ITuple objects' {
        $tuple = [Tuple]::Create(10, 'Hello')
        $tuple[-1] | Should -BeExactly 'Hello'
    }

    It 'ITuple can be assigned to multiple variables with exact element count' {
        $tuple = [Tuple]::Create(1, 2)
        $a, $b = $tuple
        $a | Should -Be 1
        $b | Should -Be 2
    }

    It 'Single element Tuple can be assigned to multiple variables' {
        $tuple = [Tuple]::Create(1)
        $a, $b = $tuple
        $a | Should -Be 1
        $b | Should -BeNullOrEmpty
    }

    It 'Tuple with 7 elements can be assigned to multiple variables' {
        $tuple = [Tuple]::Create(1, 2, 3, 4, 5, 6, 7)
        $a, $b, $c, $d, $e, $f, $g = $tuple
        $a | Should -Be 1
        $b | Should -Be 2
        $c | Should -Be 3
        $d | Should -Be 4
        $e | Should -Be 5
        $f | Should -Be 6
        $g | Should -Be 7
    }

    It 'Tuple with 8 elements can be assigned to multiple variables' {
        $tuple = [Tuple]::Create(1, 2, 3, 4, 5, 6, 7, 8)
        $a, $b, $c, $d, $e, $f, $g, $h = $tuple
        $a | Should -Be 1
        $b | Should -Be 2
        $c | Should -Be 3
        $d | Should -Be 4
        $e | Should -Be 5
        $f | Should -Be 6
        $g | Should -Be 7
        $h | Should -Be 8
    }

    It 'ValueTuple can be assigned to multiple variables' {
        $vtuple = [ValueTuple]::Create("x", "y", "z")
        $a, $b, $c = $vtuple
        $a | Should -BeExactly "x"
        $b | Should -BeExactly "y"
        $c | Should -BeExactly "z"
    }

    It 'ITuple with more elements than variables assigns remaining to last variable' {
        $tuple = [Tuple]::Create(10, 20, 30)
        $first, $rest = $tuple
        $first | Should -Be 10
        $rest | Should -Be @(20, 30)
    }

    It 'ITuple with fewer elements than variables assigns null to extras' {
        $tuple = [Tuple]::Create(100, 200)
        $x, $y, $z = $tuple
        $x | Should -Be 100
        $y | Should -Be 200
        $z | Should -BeNullOrEmpty
    }

    It 'Math.DivRem result can be assigned to multiple variables' {
        $quotient, $remainder = [Math]::DivRem(17, 5)
        $quotient | Should -Be 3
        $remainder | Should -Be 2
    }

    It 'DictionaryEntry can be assigned to multiple variables' {
        $de = [System.Collections.DictionaryEntry]::new("testKey", "testValue")
        $key, $value = $de
        $key | Should -BeExactly "testKey"
        $value | Should -BeExactly "testValue"
    }

    It 'KeyValuePair can be assigned to multiple variables' {
        $kvp = [System.Collections.Generic.KeyValuePair[string, int]]::new("count", 42)
        $k, $v = $kvp
        $k | Should -BeExactly "count"
        $v | Should -Be 42
    }

    It 'DictionaryEntry with more variables than elements assigns null to extras' {
        $de = [System.Collections.DictionaryEntry]::new("key", "value")
        $a, $b, $c = $de
        $a | Should -BeExactly "key"
        $b | Should -BeExactly "value"
        $c | Should -BeNullOrEmpty
    }

    It 'Hashtable enumeration produces deconstructable DictionaryEntry' {
        $ht = @{ name = "test" }
        foreach ($entry in $ht.GetEnumerator()) {
            $k, $v = $entry
            $k | Should -BeExactly "name"
            $v | Should -BeExactly "test"
        }
    }

    It 'Deconstruct method should only be called once during array assignment' {
        Add-Type -TypeDefinition @'
        public class DeconstructCounter
        {
            public static int CallCount = 0;
            public string A { get; set; }
            public string B { get; set; }
            public string C { get; set; }

            public DeconstructCounter(string a, string b, string c)
            {
                A = a;
                B = b;
                C = c;
            }

            public void Deconstruct(out string a, out string b, out string c)
            {
                CallCount++;
                a = A;
                b = B;
                c = C;
            }
        }
'@
        [DeconstructCounter]::CallCount = 0
        $obj = [DeconstructCounter]::new("x", "y", "z")

        # This assignment should call Deconstruct only once
        # even though there are more deconstructed values (3) than variables (2)
        $first, $rest = $obj

        $first | Should -BeExactly "x"
        $rest | Should -Be @("y", "z")
        [DeconstructCounter]::CallCount | Should -Be 1 -Because "Deconstruct should only be called once"
    }
}
