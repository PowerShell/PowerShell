# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Get-Service cmdlet tests" -Tags "CI" {
  # Service cmdlet is currently working on windows only
  # So skip the tests on non-Windows
  BeforeAll {
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    if ( -not $IsWindows ) {
        $PSDefaultParameterValues["it:skip"] = $true
    }
  }
  # Restore the defaults
  AfterAll {
      $global:PSDefaultParameterValues = $originalDefaultParameterValues
  }

  $testCases =
    @{ data = $null          ; value = 'null' },
    @{ data = [String]::Empty; value = 'empty string' }

  Context 'Check null or empty value to the -Name parameter' {
    It 'Should throw if <value> is passed to -Name parameter' -TestCases $testCases {
      param($data)
      { $null = Get-Service -Name $data -ErrorAction Stop } |
        Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
    }
  }

  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if <value> is passed through pipeline to -Name parameter' -TestCases $testCases {
      param($data)
      { $null = Get-Service -Name $data -ErrorAction Stop } |
        Should -Throw -ErrorId 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
    }
  }

  It "GetServiceCommand can be used as API for '<parameter>' with '<value>'" -TestCases @(
    @{ parameter = "DisplayName" ; value = "foo" },
    @{ parameter = "Include"     ; value = "foo","bar" },
    @{ parameter = "Exclude"     ; value = "bar","foo" },
    @{ parameter = "InputObject" ; script = { Get-Service | Select-Object -First 1 } },
    @{ parameter = "Name"        ; value = "foo","bar" }
  ) {
    param($parameter, $value, $script)
    if ($script -ne $null) {
      $value = & $script
    }

    $getservicecmd = [Microsoft.PowerShell.Commands.GetServiceCommand]::new()
    $getservicecmd.$parameter = $value
    $getservicecmd.$parameter | Should -BeExactly $value
  }

  It "Get-Service filtering works for '<script>'" -TestCases @(
    @{ script = { Get-Service -DisplayName Net* }               ; expected = { Get-Service | Where-Object { $_.DisplayName -like 'Net*' } } },
    @{ script = { Get-Service -Include Net* -Exclude *logon }   ; expected = { Get-Service | Where-Object { $_.Name -match '^net.*?(?<!logon)$' } } }
    @{ script = { Get-Service -Name Net* | Get-Service }        ; expected = { Get-Service -Name Net* } },
    @{ script = { Get-Service -Name "$(New-Guid)*" }            ; expected = $null },
    @{ script = { Get-Service -DisplayName "$(New-Guid)*" }     ; expected = $null },
    @{ script = { Get-Service -DependentServices -Name winmgmt }; expected = { (Get-Service -Name winmgmt).DependentServices } },
    @{ script = { Get-Service -RequiredServices -Name winmgmt } ; expected = { (Get-Service -Name winmgmt).RequiredServices } }
  ) {
    param($script, $expected)
    $services = & $script
    if ($expected -ne $null) {
      $servicesCheck = & $expected
    }
    if ($servicesCheck -ne $null) {
      Compare-Object $services $servicesCheck | Out-String | Should -BeNullOrEmpty
    } else {
      $services | Should -BeNullOrEmpty
    }
  }

  It "Get-Service fails for non-existing service using '<script>'" -TestCases @(
    @{ script  = { Get-Service -Name (New-Guid) -ErrorAction Stop}       ;
       ErrorId = "NoServiceFoundForGivenName,Microsoft.PowerShell.Commands.GetServiceCommand" },
    @{ script  = { Get-Service -DisplayName (New-Guid) -ErrorAction Stop};
       ErrorId = "NoServiceFoundForGivenDisplayName,Microsoft.PowerShell.Commands.GetServiceCommand" }
  ) {
    param($script,$errorid)
    { & $script } | Should -Throw -ErrorId $errorid
  }
}
