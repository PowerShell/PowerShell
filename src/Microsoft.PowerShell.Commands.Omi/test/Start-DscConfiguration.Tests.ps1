# Note that omicli must be in PATH and omiserver should be started with
#    --ignoreAuthentication option

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$mof = "$here/assets/sample.mof"
$mofMeta = "$here/assets/sampleMeta.mof"
$file = "/tmp/linux.txt"

Describe "DscConfiguration" {
    Import-Module Microsoft.PowerShell.Commands.Omi

    It "Should create file per sample MOF file" {
        $s = Start-DscConfiguration -ConfigurationMof $mof | 
                Where-Object {$_.NAME -eq "ReturnValue"}
        $s.VALUE    | Should Be "0"
        $file       | Should Exist
    }

    It "Should get DSC configuration successfully" {
        $s = Get-DscConfiguration | Where-Object {$_.NAME -eq "ReturnValue"}
        $s.VALUE    | Should Be "0"
    }

    It "Should set Meta MOF file properly" {
        $s = Set-DscLocalConfigurationManager -ConfigurationMof $mofMeta | 
                Where-Object {$_.NAME -eq "ReturnValue"}
        $s.VALUE    | Should Be "0"
    }

    It "Should get DSC local configuration successfully" {
        $s = Get-DscLocalConfigurationManager | Where-Object {$_.NAME -eq "ReturnValue"}
        $s.VALUE    | Should Be "0"
    }

}
