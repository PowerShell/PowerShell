# Note that omicli must be in PATH and omiserver should be started with 
#    --ignoreAuthentication option

Describe "Get-OmiInstance" {
    Import-Module Microsoft.PowerShell.Commands.Omi

    It "Should execute basic command correctly" {
        $obj = Get-OmiInstance -NameSpace root/omi -ClassName OMI_Identify

        $obj.Length      | Should Be 27
    }
}