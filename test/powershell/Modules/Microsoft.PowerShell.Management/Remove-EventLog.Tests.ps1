
Describe "New-EventLog cmdlet tests" -Tags @('CI', 'RequireAdminOnWindows') {

    BeforeAll {
        $defaultParamValues = $PSdefaultParameterValues.Clone()
        $IsNotSkipped = ($IsWindows -and !$IsCoreCLR)
        $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    BeforeEach {
        if ($IsNotSkipped) {
            Remove-EventLog -LogName TestLog -ErrorAction Ignore -Force
            {New-EventLog -LogName TestLog -Source TestSource -ErrorAction Stop}                              | Should Not Throw
            {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ErrorAction Stop} | Should Not Throw
        }
    }
    #CmdLet is NYI - change to -Skip:($NonWinAdmin) when implemented
    It "should be able to Remove-EventLog -LogName <string> -ComputerName <string>" -Pending:($True) {
      {Remove-EventLog -LogName TestLog -ComputerName $env:COMPUTERNAME -ErrorAction Stop -Force}              | Should Not Throw
      { Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ErrorAction Stop } | ShouldBeErrorId "Microsoft.PowerShell.Commands.WriteEventLogCommand"
      { Get-EventLog -LogName TestLog -ErrorAction Stop } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.GetEventLogCommand"
    }
    #CmdLet is NYI - change to -Skip:($NonWinAdmin) when implemented
    It "should be able to Remove-EventLog -Source <string> -ComputerName <string>"  -Pending:($True) {
      {Remove-EventLog -Source TestSource -ComputerName $env:COMPUTERNAME -ErrorAction Stop -Force} | Should Not Throw
      { Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ErrorAction Stop } | ShouldBeErrorId "Microsoft.PowerShell.Commands.WriteEventLogCommand"
      { Get-EventLog -LogName TestLog -ErrorAction Stop } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.GetEventLogCommand"
    }
}
