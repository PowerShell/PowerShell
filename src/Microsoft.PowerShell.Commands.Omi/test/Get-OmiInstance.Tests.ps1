# Note that omicli must be in PATH and omiserver should be started with
#    --ignoreAuthentication option

Describe "Get-OmiInstance" {
    Import-Module Microsoft.PowerShell.Commands.Omi

    It "Should execute basic command correctly" {
        $instance = Get-OmiInstance -NameSpace root/omi -ClassName OMI_Identify

        # This test is a workaround
        $instance.Value.Contains("OMI") | Should Be $true

        # TODO: test these when available
        #$instance.ProductName | Should Be "OMI"
        #$instance.ProductVendor | Should Be "Microsoft"
        #$instance.OperatingSystem | Should Be "LINUX"
    }
}
