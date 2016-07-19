
using namespace System.Management.Automation

Describe "function:help tests" -Tags @("P1") {
    It "parameters match Get-Help" {
        $helpFn = Get-Command help -CommandType Function
        $getHelpCmdlet = Get-Command Get-Help -CommandType Cmdlet

        $helpFn | Should Not Be $null
        $GetHelpCmdlet | Should Not Be $null

        $helpMD = [CommandMetadata]::new($helpFn)
        $gethelpMD = [CommandMetadata]::new($getHelpCmdlet)

        $helpParams = [ProxyCommand]::GetParamBlock($helpMD)
        $gethelpParams = [ProxyCommand]::GetParamBlock($gethelpMD)

        $helpParams | Should Be $gethelpParams

        $helpAttr = [ProxyCommand]::GetCmdletBindingAttribute($helpMD)
        $gethelpAttr = [ProxyCommand]::GetCmdletBindingAttribute($gethelpMD)

        $helpAttr | Should Be $gethelpAttr
    }
}
