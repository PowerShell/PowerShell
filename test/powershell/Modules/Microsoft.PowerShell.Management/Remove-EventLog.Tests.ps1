# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "New-EventLog cmdlet tests" -Tags @('CI', 'RequireAdminOnWindows') {

    BeforeAll {
        $defaultParamValues = $PSDefaultParameterValues.Clone()
        $IsNotSkipped = ($IsWindows -and !$IsCoreCLR)
        $PSDefaultParameterValues["it:skip"] = !$IsNotSkipped
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    BeforeEach {
        if ($IsNotSkipped) {
            Remove-EventLog -LogName TestLog -ErrorAction Ignore
            {New-EventLog -LogName TestLog -Source TestSource -ErrorAction Stop}                              | Should -Not -Throw
            {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 1 -ErrorAction Stop} | Should -Not -Throw
        }
    }
    #CmdLet is NYI - change to -Skip:($NonWinAdmin) when implemented
    It "should be able to Remove-EventLog -LogName <string> -ComputerName <string>" -Pending:($true) {
      { Remove-EventLog -LogName TestLog -ComputerName $env:COMPUTERNAME -ErrorAction Stop }              | Should -Not -Throw
      { Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 1 -ErrorAction Stop } | Should -Throw -ErrorId "Microsoft.PowerShell.Commands.WriteEventLogCommand"
      { Get-EventLog -LogName TestLog -ErrorAction Stop } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.GetEventLogCommand"
    }
    #CmdLet is NYI - change to -Skip:($NonWinAdmin) when implemented
    It "should be able to Remove-EventLog -Source <string> -ComputerName <string>"  -Pending:($true) {
      {Remove-EventLog -Source TestSource -ComputerName $env:COMPUTERNAME -ErrorAction Stop} | Should -Not -Throw
      { Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 1 -ErrorAction Stop } | Should -Throw -ErrorId "Microsoft.PowerShell.Commands.WriteEventLogCommand"
      { Get-EventLog -LogName TestLog -ErrorAction Stop; } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.GetEventLogCommand"
    }
}
