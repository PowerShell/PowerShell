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
        }
    }

    It "should be able to create a New-EventLog with a -Source parameter" -Skip:($true) {
      {New-EventLog -LogName TestLog -Source TestSource -ErrorAction Stop} | Should -Not -Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 1 -ErrorAction Stop} | Should -Not -Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should -Be 1
    }
    It "should be able to create a New-EventLog with a -ComputerName parameter" -Skip:($true) {
      {New-EventLog -LogName TestLog -Source TestSource -ComputerName $env:COMPUTERNAME -ErrorAction Stop} | Should -Not -Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 1 -ErrorAction Stop} | Should -Not -Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should -Be 1
      $result.EventID | Should -Be 1
    }
    It "should be able to create a New-EventLog with a -CategoryResourceFile parameter" -Skip:($true) {
      {New-EventLog -LogName TestLog -Source TestSource -CategoryResourceFile "CategoryMessageFile" -ErrorAction Stop} | Should -Not -Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 2 -ErrorAction Stop} | Should -Not -Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should -Be 1
      $result.EventID | Should -Be 2
    }
    It "should be able to create a New-EventLog with a -MessageResourceFile parameter" -Skip:($true) {
      {New-EventLog -LogName TestLog -Source TestSource -MessageResourceFile "ResourceMessageFile" -ErrorAction Stop} | Should -Not -Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 3 -ErrorAction Stop} | Should -Not -Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should -Be 1
      $result.EventID | Should -Be 3
    }
    It "should be able to create a New-EventLog with a -ParameterResourceFile parameter" -Skip:($true) {
      {New-EventLog -LogName TestLog -Source TestSource -ParameterResourceFile "ParameterMessageFile" -ErrorAction Stop} | Should -Not -Throw
      {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventId 4 -ErrorAction Stop} | Should -Not -Throw
      $result=Get-EventLog -LogName TestLog
      $result.Count   | Should -Be 1
      $result.EventID | Should -Be 4
    }
}
