Describe "ConvertFrom-StringData DRT Unit Tests" -Tags "CI" {
    It "Should able to throw error when convert invalid line" {
        $str =@"
#comments here
abc
#comments here
def=content of def
"@
        try
        {
            ConvertFrom-StringData $str
            Throw "we expect 'InvalidOperation' exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidOperation,Microsoft.PowerShell.Commands.ConvertFromStringDataCommand"
        }
    }
}

Describe "ConvertFrom-StringData" -Tags "CI" {
    $sampleData = @'
foo  = 0
bar  = 1
bazz = 2
'@

    It "Should not throw when called with just the stringdata switch" {
	{ ConvertFrom-StringData -StringData 'a=b' } | Should Not Throw
    }

    It "Should return a hashtable" {
	$result = ConvertFrom-StringData -StringData 'a=b'
    $result | Should BeOfType Hashtable
    }

    It "Should throw if not in x=y format" {
	{ ConvertFrom-StringData -StringData 'ab' }  | Should Throw
	{ ConvertFrom-StringData -StringData 'a,b' } | Should Throw
	{ ConvertFrom-StringData -StringData 'a b' } | Should Throw
	{ ConvertFrom-StringData -StringData 'a\tb' } | Should Throw
	{ ConvertFrom-StringData -StringData 'a:b' } | Should Throw
    }

    It "Should return the data on the left side in the key" {
	$actualValue = ConvertFrom-StringData -StringData 'a=b'

	$actualValue.Keys | Should Be "a"
    }

    It "Should return the data on the right side in the value" {
	$actualValue = ConvertFrom-StringData -StringData 'a=b'

	$actualValue.Values | Should Be "b"
    }

    It "Should return a keycollection for the keys" {
        $(ConvertFrom-StringData -StringData 'a=b').Keys.PSObject.TypeNames[0] | Should Be "System.Collections.Hashtable+KeyCollection"
    }

    It "Should return a valuecollection for the values" {
	$(ConvertFrom-StringData -StringData 'a=b').Values.PSObject.TypeNames[0] | Should Be "System.Collections.Hashtable+ValueCollection"
    }

    It "Should work for multiple lines" {
	{ ConvertFrom-StringData -StringData $sampleData } | Should Not Throw

	$(ConvertFrom-StringData -StringData $sampleData).Keys   | Should Be "foo", "bar", "bazz"

	$(ConvertFrom-StringData -StringData $sampleData).Values | Should Be "0","1","2"
    }
}
