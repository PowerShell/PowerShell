using module TypesDsl

Import-Module $PSScriptRoot\TypesDsl.psm1

class MyPSClass
{
    static [System.Object] GetFirst([psobject]$obj)
    {
        [array]$arr = $obj -as [array]
        if ($arr.Length -gt 0)
        {
            return $arr[0]
        }
        return $null
    }

    static [System.Object] ValueOfX([psobject]$obj)
    {
        [hashtable]$hashtbl = $obj -as [hashtable]

        if ($hashtbl.ContainsKey("x"))
        {
            return $hashtbl.x
        }
        return $null
    }
}

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

    Method First -CodeReference [MyPSClass]::GetFirst
}

TypeExtension System.Collections.Hashtable
{
    Property TwiceCount -ScriptProperty { 2 * $this.Count }

    Property NumElements -Alias Count

    Property Greeting -NoteProperty "Hello"

    Property TheValueOfX -CodeReference [MyPSClass]::ValueOfX
}