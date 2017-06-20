$guid = [Guid]::NewGuid().ToString().Replace("-","")

Describe "Add-Type" -Tags "CI" {
    It "Public 'Language' enumeration contains all members" {
        [Enum]::GetNames("Microsoft.PowerShell.Commands.Language") -join "," | Should Be "CSharp,CSharpVersion7,CSharpVersion6,CSharpVersion5,CSharpVersion4,CSharpVersion3,CSharpVersion2,CSharpVersion1,VisualBasic,JScript"
    }

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
}
