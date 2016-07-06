if ($IsWindows -and !$IsCore) {
    #check to see whether we're running as admin in Windows...
    $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $windowsIdentity
    if ($windowsPrincipal.IsInRole("Administrators") -eq $true) {
        $NonWinAdmin=$false
    } else {$NonWinAdmin=$true}
  Describe "New-EventLog cmdlet tests" -Tags DRT {
    BeforeEach {
      Remove-EventLog -LogName TestLog -ea Ignore
    }
    It "should be able to create a New-EventLog with a -Source paramter" -Skip:($True -Or $NonWinAdmin) {
      {New-EventLog -LogName TestLog -Source TestSource -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
    }
    It "should be able to create a New-EventLog with a -ComputerName paramter" -Skip:($True -Or $NonWinAdmin) {
      {New-EventLog -LogName TestLog -Source TestSource -ComputerName $env:COMPUTERNAME -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 1
    }
    It "should be able to create a New-EventLog with a -CategoryResourceFile paramter" -Skip:($True -Or $NonWinAdmin) {
      {New-EventLog -LogName TestLog -Source TestSource -CategoryResourceFile "CategoryMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 2 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 2
    }
    It "should be able to create a New-EventLog with a -MessageResourceFile paramter" -Skip:($True -Or $NonWinAdmin) {
      {New-EventLog -LogName TestLog -Source TestSource -MessageResourceFile "ResourceMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 3 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 3
    }
    It "should be able to create a New-EventLog with a -ParameterResourceFile paramter" -Skip:($True -Or $NonWinAdmin) {
      {New-EventLog -LogName TestLog -Source TestSource -ParameterResourceFile "ParameterMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 4 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 4
    }
  } 
}
