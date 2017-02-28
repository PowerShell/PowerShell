using module TypesDsl

Import-Module $PSScriptRoot\TypesDsl.psm1

TypeExtension System.Array
{
    Method -Name Sum -ScriptMethod {
        $acc = $null
        foreach ($e in $this)
        {
            $acc += $e
        }
        $acc
    }

    Method -Name All -CodeReference System.Array::TrueForAll
}

TypeExtension System.Collections.Hashtable
{
    Property -Name TwiceCount -ScriptProperty { 2 * $this.Count }

    Property -Name NumElements -Alias Count

    Property -Name Greeting -NoteProperty "Hello"
}