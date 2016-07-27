$guid = [Guid]::NewGuid().ToString().Replace("-","")

Describe "Add-Type" -Tags "CI" {
    It "Should not throw given a simple class definition" -pending:($IsCoreCLR) {
        { Add-Type -TypeDefinition "public static class foo { }" } | Should Not Throw
    }

    It "Can use System.Management.Automation.CmdletAttribute" -pending:($IsCoreCLR) {
        $code = @"
[System.Management.Automation.Cmdlet("Get", "Thing", ConfirmImpact = System.Management.Automation.ConfirmImpact.High, SupportsPaging = true)]
public class AttributeTest$guid {}
"@
        Add-Type -TypeDefinition $code -PassThru | Should Not Be $null
    }

    It "Can load TPA assembly System.Runtime.Serialization.Primitives.dll" -pending:($IsCoreCLR) {
        Add-Type -AssemblyName 'System.Runtime.Serialization.Primitives' -PassThru | Should Not Be $null
    }
}
