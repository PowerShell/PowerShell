# Pester Module for testing GetPowerShell API fixes
Describe "Validation for GetPowerShell API Changes" -Tags "DRT" {
    BeforeAll {
        If ( $IsWindows ) {
            $base = "C:\"
        }
        else {
            $base = "/"
        }
    }

    it "001 - Baseline1 - constant pipeline executes correctly" {
        $sb = { Write-Output 'Hello World' }
        $result = $sb.GetPowerShell().Invoke() 
        $result.Count | should be 1
        $result[0] | should be 'Hello World'
    }

    it "002 - Baseline2 - constant pipeline executes correctly" {
        $sb = [scriptblock]::Create("get-item $base")
        $result = $sb.GetPowerShell().Invoke() | select-object -First 1
        $result.Name | should be "$base"
        $result.GetType().FullName | Should Be "System.IO.DirectoryInfo"
    }

    it "003 - pipeline with a single argument to format-table" {
        $sb = [scriptblock]::Create("Get-Item $base|format-table Name")
        $result = $sb.GetPowerShell().Invoke() 
        $resultLines = $result | out-string -stream | %{$_.trim()} | select-object -First 1 -Skip 1
        $resultLines | should be "Name"
    }

    it "004 - pipeline with two unadorned string arguments to format-table" {
        $sb = [scriptblock]::Create("Get-Item $base|format-table Length,Name")
        $result = $sb.GetPowerShell().Invoke() 
        $resultLine = $result | out-string -stream | %{$_.trim()} | select-object -First 1 -Skip 1
        $resultLine | should Match "Length *Name"
    }

    it "005 - pipeline with one quoted string as an argument to format-table" {
        $sb = [scriptblock]::Create(" Get-Item $base|format-table Length,'name'")
        $result = $sb.GetPowerShell().Invoke() 
        $resultLine = $result | out-string -stream | %{$_.trim()} | select-object -First 1 -Skip 1
        $resultLine | should Match "Length *Name"
    }

    it "006 - Constant variables are Returned" {
        $sb = { write-output $True }
        $result = $sb.GetPowerShell().Invoke()
        $result | Should Be "True"
    }

    it "007 - ParamBlock parameters are honored by GetPowerShell" {
        $expectedResult = "Param1: P1, param2: P2"
        $sb = {
            param ($a, $b)
            Write-Output "Param1: $a, param2: $b"
            }
        $result = $sb.GetPowerShell(@('P1','P2')).Invoke()|select-object -First 1
        $result | Should Be $expectedResult
    }

    it "008 - GetPowerShell can convert a hashtable to a psobject" {
        $sb = {new-object psobject -prop @{ c = 1; d = "astring"}}
        $result = $sb.GetPowerShell().Invoke() | select-object -First 1
        $result.GetType().FullName | Should Be "System.Management.Automation.PSCustomObject"
        $result.c | should be 1
        $result.d | should be "astring"
    }

    it "009a - GetPowerShell can convert a hashtable to a psobject with a parameter" {
        $sb = {param ($a) new-object psobject -prop @{c = 1; d = $a}}
        $result = $sb.GetPowerShell("astring").Invoke() | select-object -First 1
        $result.GetType().FullName | Should Be "System.Management.Automation.PSCustomObject"
        $result.c | should be 1
        $result.d | should be "astring"
    }

    it "009b - GetPowerShell can convert a hashtable with a parameter and value is an expandable string" {
        $sb = {param ($a) new-object psobject -prop @{c = 1; d = "$a"}}
        $result = $sb.GetPowerShell("astring").Invoke() | select-object -First 1
        $result.GetType().FullName | Should Be "System.Management.Automation.PSCustomObject"
        $result.c | should be 1
        $result.d | should be "astring"
    }

    it "009c - GetPowerShell can not convert a hashtable when the value is to be executed" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = {new-object psobject -prop @{c = 1; d = get-date}}
        try
        {
            $result = $sb.GetPowerShell("astring").Invoke() 
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "010 - return passed arguments as unadorned variable" {
        $Value = "PassedValue"
        $sb = { param ( $p) write-output $p }
        $result = $sb.GetPowerShell($Value).Invoke() | select-object -First 1
        $result | should Be $Value
    }

    it "011 - return passed arguments as expandable string variable" {
        $Value = "PassedValue"
        $sb = {param ($p) write-output "START $p END"}
        $result = $sb.GetPowerShell($Value).Invoke() | select-object -First 1
        $result | should Be "START $Value END"
    }

    # this test is from our current library of tests
    it "012 - Simple parameters are honored by GetPowerShell" {
        $expectedResult = "Param1: P1, param2: P2"
        function TestFunction ($a, $b)
        {
            Write-Output "Param1: $a, param2: $b"
        }
        $scriptBlock = (Get-Command TestFunction).ScriptBlock
        $result = $ScriptBlock.GetPowerShell(@('P1','P2')).Invoke()|select-object -First 1
        $result | Should Be $expectedResult
    }

    it "013 - Supports more than one passed parameter" {
        $Value = "PassedValue1","PassedValue2"
        $ExpectedString = "a=PassedValue1 b=PassedValue2"
        $sb = {param ( $a, $b) write-output "a=$a b=$b"}
        $result = $sb.GetPowerShell($Value).Invoke() | select-object -First 1
        $result | should be $ExpectedString
    }

    it "014 - pipeline with two arguments, one being passed to format-table" {
        $sb = [scriptblock]::Create("param (`$p) Get-Item $base|format-table Length,`$p")
        $result = $sb.GetPowerShell("Name").Invoke() 
        $resultLine = $result | out-string -stream | %{$_.trim()} | select-object -First 1 -Skip 1
        $resultLine | should Match "Length *Name"
    }

    it "015 - Exception when variable is not a parameter" {
        $Value = "PassedValue"
        # use ErrorID instead of FQDN of exception
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = {param ($p) write-output "$p $b"}
        try
        {
            $result = $sb.GetPowerShell($Value).Invoke() | select-object -First 1
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "016 - Error Message is merged to Output" {
        $sb = { write-error 'Error Message' 2>&1 }
        $result = $sb.GetPowerShell().Invoke() | select-object -First 1
        $result | Should Be "Error Message"
    }

    it "017 - Using statement actually uses those variables" {
        $sb = { write-Output @using:r }
        $q = ,"Hello"
        $parameters = new-object -type "System.Collections.Generic.Dictionary[string,object]"
        $parameters["r"] = $q
        $result = $sb.GetPowerShell($parameters).Invoke() | select-object -First 1
        $result | Should Be "Hello"
    }

    it "018 - Using statement returns null value and not the local variable" {
        $sb = { write-Output @using:pid }
        $q = ,"Hello"
        $parameters = new-object -type "System.Collections.Generic.Dictionary[string,object]"
        $parameters["r"] = $q
        $result = $sb.GetPowerShell($Value).Invoke() | select-object -First 1
        $result | Should BeNullOrEmpty
    }

    it "019 - Array is passed back" {
        $sb = { write-output 1,2,3,4,5 -NoEnumerate }
        $result = $sb.GetPowerShell().Invoke()[0]
        $result.GetType().FullName | Should be "System.Object[]"
        $result.Count | Should be 5
        $result[0] | Should be 1
        $result[4] | Should be 5
    }

    it "020 - Array of Arrays is passed back" {
        $sb = { write-output @(1,2,3),@(4,5,6),@(7,8,9) -NoEnumerate }
        $result = $sb.GetPowerShell().Invoke()[0]
        $result.GetType().FullName | Should be "System.Object[]"
        $result.Count | should be 3
        $result[0][0] | Should Be 1
        $result[1][0] | should be 4
        $result[2][0] | should be 7
        $result[2][2] | should be 9
    }

    it "021 - Disallow execution via static method invocation" {
        $sb = { write-output ([console]::WriteLine("CodeExecuted")) }
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "022 - Disallow execution in expandable strings" {
        $sb = { write-output "$(get-date)" }
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "023 - Disallow accessing types" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = { write-output ([type]) }
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "024 - Disallow accessing type static properties which are marked as literal" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = { write-output ([int32]::MaxValue) }
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "025 - Disallow accessing type static methods" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = { write-output ([math]::pow(2,2)) }
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "026 - Disallow accessing type instance properties" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $sb = { param($a) write-output "$a".length }
        try
        {
            $result = $sb.GetPowerShell().Invoke()
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "027 - Allow type declaration of parameters" {
        $sb = { param([int]$a) write-output $a}
        $result = $sb.GetPowerShell("1").Invoke() | select-object -first 1
        $result | Should be 1
        $result.GetType().FullName | Should be System.Int32
    }

    it "028 - Allow access to array index" {
        $sb = { param($a) write-output $a[0]}
        $result = $sb.GetPowerShell(@("a","b")).Invoke() | select-object -first 1
        $result | Should be "a"
    }

    it "029 - Allow access to args array via index" {
        $sb = { write-output $args[0]}
        $result = $sb.GetPowerShell(@("a","b")).Invoke() | select-object -first 1
        $result | Should be "a"
    }

    it "030 - Allow access to args array via index as parameter" {
        $sb = { param ($a, $b ) write-output $a[$b]}
        $result = $sb.GetPowerShell(@("a","b"),0).Invoke() | select-object -first 1
        $result | Should be "a"
    }

    it "031 - Unary operator gives the right answer for numbers" {
        $sb = { write-output ( -(+(-1)))}
        $result = $sb.GetPowerShell().Invoke() | select-object -first 1
        $result | Should be 1
    }

    it "032 - Unary operator gives the right answer for bools" {
        $sb = { write-output ( !(!(! $false)))}
        $result = $sb.GetPowerShell().Invoke() | select-object -first 1
        $result | Should be $true
    }

    it "033 - Conversions actually convert" {
        $sb = { write-output ([int]"2")}
        $result = $sb.GetPowerShell().Invoke() | select-object -first 1
        $result | Should be 2
        $result.GetType().FullName | should be "System.Int32"
    }

    it "034 - Conversions actually convert when passed in" {
        $sb = { param ([bool]$a) write-output (! $a)}
        $result = $sb.GetPowerShell($false).Invoke() | select-object -first 1
        $result | Should be $true
    }

    it "035 - Array slices support negative numbers" {
        $sb = { write-output ("abcd"[-1])}
        $result = $sb.GetPowerShell().Invoke() | select-object -first 1
        $result | Should be "d"
    }

    it "036 - Array slices support negative numbers" {
        $sb = { write-output ("abcd"[-1,0])}
        $result = $sb.GetPowerShell().Invoke() 
        $result[0] | Should be "d"
        $result[1] | Should be "a"
    }

    it "037a - an array as a singleton is actually an array" {
        $sb = { write-output (,1) -NoEnumerate }
        $result = $sb.GetPowerShell().Invoke()[0]
        $result.GetType().FullName | Should be "System.Object[]"
        $result.Count | should be 1
    }

    it "037b - an array as a singleton w/ @() is actually an array" {
        $sb = { write-output @(,1) -NoEnumerate }
        $result = $sb.GetPowerShell().Invoke()[0]
        $result.GetType().FullName | Should be "System.Object[]"
        $result.Count | should be 1
    }

    it "038 - Conversions actually convert when passed in" {
        $sb = { param ([bool]$a) write-output (! $a)}
        $result = $sb.GetPowerShell($false).Invoke() | select-object -first 1
        $result | Should be $true
    }

    it "039 - Complex ExpandableStrings behave right" {
        $sb = { param ( $p1 ) echo "abc $p1 $true $(1) $p1 $(2,3) $(""$p1"") def" }
        $OrigOFS = $OFS; $OFS = $null
        $result = $sb.GetPowerShell("ZZ").Invoke() | select-object -First 1
        $OFS = $OrigOFS
        $result | Should be "abc ZZ True 1 ZZ 2 3 ZZ def"
    }

    it "040 - Complex ExpandableStrings with multiple statements behave right" {
        $sb = { param ( $p1 ) echo "abc $p1 $true $(1) $p1 $(2,3;4,5) $(""$p1"") def" }
        $OrigOFS = $OFS; $OFS = $null
        $result = $sb.GetPowerShell("ZZ").Invoke() | select-object -First 1
        $OFS = $OrigOFS
        $result | Should be "abc ZZ True 1 ZZ 2 3 4 5 ZZ def"
    }

    it "041 - Complex ExpandableStrings with multiple statements behave right (2)" {
        $sb = { echo "$true $(1) : $(1;2) : $(1,2) : $(1,2;3,4;5,6)" }
        $OrigOFS = $OFS; $OFS = $null
        $result = $sb.GetPowerShell("ZZ").Invoke() | select-object -First 1
        $OFS = $OrigOFS
        $result | Should be "True 1 : 1 2 : 1 2 : 1 2 3 4 5 6"
    }

    it "042 - Expandable strings respect `$OFS" {
        $sb = { echo "$(1;2)" }
        $OrigOFS = $OFS; $OFS = "::::"
        $result = $sb.GetPowerShell("ZZ").Invoke() | select-object -First 1
        $OFS = $OrigOFS
        $result | Should be "1::::2"
    }

    it "043 - Too many nodes in an AST is an error" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $OrigOFS = $OFS; $OFS = $null
        $sb = [scriptblock]::Create((1..5000|%{"echo @("}{"$_,"}{"5001)"}))
        $OFS = $OrigOFS
        try
        {
            $result = $sb.GetPowerShell().Invoke() | select-object -First 1
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

    it "044 - Too many keys in an hashtable is an error" {
        $ExpectedErrorId = "ScriptBlockToPowerShellNotSupportedException"
        $OrigOFS = $OFS; $OFS = $null
        $sb = [scriptblock]::Create((1..500|%{"echo @{"}{"$_ = $_;"}{"501 = 501}"}))
        $OFS = $OrigOFS
        try
        {
            $result = $sb.GetPowerShell().Invoke() | select-object -First 1
            throw "CodeExecuted"
        }
        catch
        {
            $_.FullyQualifiedErrorId | should be $ExpectedErrorId
        }
    }

}
