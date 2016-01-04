# Note that omicli must be in PATH and omiserver should be started with
#    --ignoreAuthentication option

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$mof = "$here/assets/sample.mof"
$file = "/tmp/linux.txt"

Describe "Start-DscConfiguration" {
    Import-Module Microsoft.PowerShell.Commands.Omi

    It "Should create file per sample MOF file" {
        $s = Start-DscConfiguration -ConfigurationMof $mof | 
                Where-Object {$_.NAME -eq "ReturnValue"}
        $s.VALUE    | Should Be "0"
        $file       | Should Exist
    }
}
