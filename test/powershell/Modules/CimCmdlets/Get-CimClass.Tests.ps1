Describe 'Get-CimClass' {
    # Get-CimClass works only on windows
    It 'can get CIM_Error CIM class' -Skip:(-not $IsWindows) {
        Get-CimClass -ClassName CIM_Error | Should Not Be $null
    }
}            
