Describe "Get-EventLog cmdlet tests" -Tags @('CI', 'RequireAdminOnWindows') {

    BeforeAll {
        $defaultParamValues = $PSdefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = !$IsWindows -or $IsCoreCLR
    }

    AfterAll {
        $global:PSDefaultParameterValues = $defaultParamValues
    }

    #CmdLets are not yet implemented, so these cases are -Pending:($True) for now...
    It "should return an array of eventlogs objects when called with -AsString parameter" -Pending:($True) {
      {$result=Get-EventLog -AsString -ea stop}    | Should Not Throw
      $result.GetType().BaseType.Name              | Should Be "Array"
      $result -eq "Application"                    | Should Be "Application" 
      $result.Count -ge 3                          | Should Be $true
    }
    It "should return a list of eventlog objects when called with -List parameter" -Pending:($True) {
      {$result=Get-EventLog -List -ea stop}        | Should Not Throw
      $result.GetType().BaseType.Name              | Should Be "array"
      {$logs=$result|Select -ExpandProperty Log}  | Should Not Throw
      $logs -eq "System"                           | Should Be "System" 
      $logs.Count -ge 3                            | Should Be $true
    }
    It "should be able to Get-EventLog -LogName Application -Newest 100" -Pending:($True) {
      {$result=get-eventlog -LogName Application -Newest 100 -ea stop} | Should Not Throw
      $result                                      | Should Not BeNullOrEmpty
      $result.Length -le 100                       | Should Be $true
      $result[0].GetType().Name                    | Should Be "EventLogEntry"
    }
    It "should throw 'AmbiguousParameterSetException' when called with both -LogName and -List parameters" -Pending:($True) {
      try {Get-EventLog -LogName System -List -ea stop; Throw "Previous statement unexpectedly succeeded..."
      } catch {echo $_.FullyQualifiedErrorId       | Should Be "AmbiguousParameterSet,Microsoft.PowerShell.Commands.GetEventLogCommand"}
    }
    It "should be able to Get-EventLog -LogName * with multiple matches" -Pending:($True) {
      {$result=get-eventlog -LogName *  -ea stop}  | Should Not Throw
      $result                                      | Should Not BeNullOrEmpty
      $result -eq "Security"                       | Should Be "Security" 
      $result.Count -ge 3                          | Should Be $true
    }
    It "should throw 'InvalidOperationException' when asked to get a log that does not exist" -Pending:($True) {
      try {Get-EventLog  -LogName MissingTestLog -ea stop; Throw "Previous statement unexpectedly succeeded..."
      } catch {echo $_.FullyQualifiedErrorId      | Should Be "System.InvalidOperationException,Microsoft.PowerShell.Commands.GetEventLogCommand"}
    }
}
