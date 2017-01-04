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
            Remove-EventLog -LogName TestLog -ea Ignore
        }
    }

    It "should be able to create a New-EventLog with a -Source parameter" -Skip:($True) {
      {New-EventLog -LogName TestLog -Source TestSource -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
    }
    It "should be able to create a New-EventLog with a -ComputerName parameter" -Skip:($True) {
      {New-EventLog -LogName TestLog -Source TestSource -ComputerName $env:COMPUTERNAME -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 1
    }
    It "should be able to create a New-EventLog with a -CategoryResourceFile parameter" -Skip:($True) {
      {New-EventLog -LogName TestLog -Source TestSource -CategoryResourceFile "CategoryMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 2 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 2
    }
    It "should be able to create a New-EventLog with a -MessageResourceFile parameter" -Skip:($True) {
      {New-EventLog -LogName TestLog -Source TestSource -MessageResourceFile "ResourceMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 3 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 3
    }
    It "should be able to create a New-EventLog with a -ParameterResourceFile parameter" -Skip:($True) {
      {New-EventLog -LogName TestLog -Source TestSource -ParameterResourceFile "ParameterMessageFile" -ea stop} | Should Not Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 4 -ea stop} | Should Not Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should be 1
      $result.EventID | Should be 4
    }
}
