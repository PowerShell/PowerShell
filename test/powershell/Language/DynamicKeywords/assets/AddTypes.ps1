using module TypesDsl

Import-Module $PSScriptRoot\TypesDsl.psm1

TypeExtension System.Array
{
    Method Sum -ScriptMethod {
        $acc = $null
        foreach ($e in $this)
        {
            $acc += $e
        }
        $acc
    }

    Method All -CodeReference System.Array::TrueForAll
}

TypeExtension System.Collections.Hashtable
{
    Property TwiceCount -ScriptProperty { 2 * $this.Count }

    Property NumElements -Alias Count

    Property Greeting -NoteProperty Hello
}