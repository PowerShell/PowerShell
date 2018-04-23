# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Clear-EventLog cmdlet tests" -Tags @('CI', 'RequireAdminOnWindows') {

    BeforeAll {
        $defaultParamValues = $PSdefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = !$IsWindows -or $IsCoreCLR
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    It "should be able to Clear-EventLog" -Pending:($True) {
      Remove-EventLog -LogName TestLog -ErrorAction Ignore
      { New-EventLog -LogName TestLog -Source TestSource -ErrorAction Stop } | Should -Not -Throw
      { Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ErrorAction Stop } | Should -Not -Throw
      { Get-EventLog -LogName TestLog }                           | Should -Not -Throw
      $result = Get-EventLog -LogName TestLog
      $result.Count                                               | Should -Be 1
      { Clear-EventLog -LogName TestLog }                         | Should -Not -Throw
      $result = Get-EventLog -LogName TestLog -ErrorAction Ignore
      $result.Count                                               | Should -Be 0
      { Remove-EventLog -LogName TestLog -ErrorAction Stop }      | Should -Not -Throw
    }

    It "should throw 'System.InvalidOperationException' when asked to clear a log that does not exist" -Pending:($True) {
      { Clear-EventLog -LogName MissingTestLog -ErrorAction Stop } | Should -Throw -ExceptionType "System.InvalidOperationException"
    }

    It "should throw 'Microsoft.PowerShell.Commands.ClearEventLogCommand' ErrorId when asked to clear a log that does not exist" -Pending:($True) {
      { Clear-EventLog -LogName MissingTestLog -ErrorAction Stop } | Should -Throw -ErrorId "Microsoft.PowerShell.Commands.ClearEventLogCommand"
    }
}
