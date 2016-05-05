if ($IsWindows) {
  #check to see whether we're running as admin in Windows...
  $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
  $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $windowsIdentity
  if ($windowsPrincipal.IsInRole("Administrators") -eq $true) {
      $NonWinAdmin=$false
  } else {$NonWinAdmin=$true}
  Describe "Clear-EventLog cmdlet tests" -Tags:DRT {
    #DRT - CmdLet is NYI - change to -Skip:($NonWinAdmin) when implemented
    It "should be able to Clear-EventLog" -Pending:($True -Or $NonWinAdmin) {
      Remove-EventLog -LogName TestLog -ea Ignore
      {New-EventLog -LogName TestLog -Source TestSource -ea Stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop } | Should Not Throw
      {$result=Get-EventLog -LogName TestLog}                     | Should Not Throw
      ($result.Count)                                             | Should be 1
      {Clear-EventLog -LogName TestLog}                           | Should Not Throw
      $result=Get-EventLog -LogName TestLog -ea Ignore
      ($result.Count)                                             | Should be 0
      {Remove-EventLog -LogName TestLog -ea Stop}                 | Should Not Throw
    }
    It "should throw 'The Log name 'MissingTestLog' does not exist' when asked to clear a log that does not exist" -Pending:($True -Or $NonWinAdmin) {
      Remove-EventLog -LogName MissingTestLog -ea Ignore
      try {Clear-EventLog -LogName MissingTestLog -ea stop; Throw "Previous statement unexpectedly succeeded..."
      } catch {$_.FullyQualifiedErrorId      | Should Be "Microsoft.PowerShell.Commands.ClearEventLogCommand"}
    }
    It "should throw 'System.InvalidOperationException' when asked to clear a log that does not exist" -Pending:($True -Or $NonWinAdmin) {
      try {Clear-EventLog -LogName MissingTestLog -ea stop; Throw "Previous statement unexpectedly succeeded..."
      } catch {$_.FullyQualifiedErrorId      | Should Be "Microsoft.PowerShell.Commands.ClearEventLogCommand"}
    }
  }
}