function Test-SuspiciousScriptBlock
{
    param(
        [ScriptBlock] $ScriptBlock,
        $Timeout = 30
    )
    
    $mi = [ScriptBlock].GetMethod("CheckSuspiciousContent", [System.Reflection.BindingFlags] "InvokeMethod,Static,NonPublic")
    $results = $mi.Invoke($null, $ScriptBlock.Ast)

    if($results) { $true }
    else { $false }
}

Describe "Tests for suspicious script blocks being logged automatically" -Tags "OuterLoop", "P1", "RI" {

    It "Detects a bad type being used directly" {
        Test-SuspiciousScriptBlock { [System.Runtime.InteropServices.Marshal]::PtrToStringAuto } | Should be $true
    }
    
    It "Detects using a variable to access a static member" {

        Test-SuspiciousScriptBlock { $mshall::NumParamBytes } | Should be $true
    }

    It "Allows non-static access to a variable" {
        Test-SuspiciousScriptBlock -Timeout 3 { $mshall.NumParamBytes } | Should be $false
    }
    
    It "Detects casts to [Type]" {
        Test-SuspiciousScriptBlock { $mshall = [Type] ("System.Runtime.InteropService" + "s.Mars" + "hal") } | Should be $true
    }
    
    It "Allows casting to other object types" {
        Test-SuspiciousScriptBlock -Timeout 3 { $mshall = [String] ("System.Runtime.InteropService" + "s.Mars" + "hal") } | Should be $false
    }
    
    It "Detects access to [Type] members" {
        Test-SuspiciousScriptBlock { $mshall = [Type]::Equals("System.Runtime.InteropService" + "s.Mars" + "hal") } | Should be $true    
    }

    It "Allows access to [Object] members" {
        Test-SuspiciousScriptBlock -Timeout 3 { $mshall = [Object]::Equals("System.Runtime.InteropService" + "s.Mars" + "hal") } | Should be $false
    }

    It "Detects type casting using -as" {
        Test-SuspiciousScriptBlock { $mshall = ("System.Runtime.InteropService" + "s.Mars" + "hal") -as [Type] } | Should be $true    
    }

    It "Detects basic bad method invocation" {
        Test-SuspiciousScriptBlock { $foo.WriteProcessMemory() } | Should be $true
    }
    
    It "Detects indirect method invocation" {
        Test-SuspiciousScriptBlock {
            $foo = New-Object System.Object
            $method = "Write" + "ProcessMemory"
            $foo.$method()
        } | Should be $true    
    }
    
    It "Detects PSObject method indirection" {
        Test-SuspiciousScriptBlock {
            $foo = New-Object System.Object
            $method = "Write" + "ProcessMemory"

            $foo.psobject.methods[$method].Invoke()
        } | Should be $true
    }    
    
    It "Detects often-malicious methods" {
        Test-SuspiciousScriptBlock {
            $foo = New-Object System.Object
            [Object].GetMethods()[0].Invoke()
        } | Should be $true
    }

    It "Detects often-malicious methods" {
        Test-SuspiciousScriptBlock {
            $method = "Write" + "ProcessMemory"
            [Object].InvokeMember($method, $null)
        } | Should be $true
    }

    It "Detects AppDomain enumeration" {
        Test-SuspiciousScriptBlock {
            [AppDomain]::CurrentDomain.GetAssemblies()[0].GetTypes()[0].InvokeMember()
        } | Should be $true
    }

    It "Detects AppDomain enumeration" {
        Test-SuspiciousScriptBlock {
            [object].Assembly.GetTypes()[0].InvokeMember()
        } | Should be $true
    }
}