$guid = [Guid]::NewGuid().ToString().Replace("-","")

Describe "Add-Type" -Tags "CI" {
    It "Should not throw given a simple class definition" {
        { Add-Type -TypeDefinition "public static class foo { }" } | Should Not Throw
    }

    It "Can use System.Management.Automation.CmdletAttribute" {
        $code = @"
[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact = System.Management.Automation.ConfirmImpact.High, SupportsPaging = true)]
public class AttributeTest$guid {}
"@
        Add-Type -TypeDefinition $code -PassThru | Should Not Be $null
    }

    It "Can load TPA assembly System.Runtime.Serialization.Primitives.dll" {
        Add-Type -AssemblyName 'System.Runtime.Serialization.Primitives' -PassThru | Should Not Be $null
    }

    It "Can use Console class" {
        # For more context, see https://github.com/PowerShell/PowerShell/issues/1616
        Add-Type -ReferencedAssemblies System.Console -TypeDefinition @"
using System;
public static class ConsoleUser$guid {
    public static void Test() {
        Console.BackgroundColor = Console.BackgroundColor;
    }
}
"@ -PassThru | Should Not Be $null
    }

}
