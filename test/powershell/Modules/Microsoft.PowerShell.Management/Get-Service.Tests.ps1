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
    @{ data = [string]::Empty; value = 'empty string' }

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

Describe 'Get-Service Admin tests' -Tag CI,RequireAdminOnWindows {
  BeforeAll {
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    if ( -not $IsWindows ) {
        $PSDefaultParameterValues["it:skip"] = $true
    }
  }
  AfterAll {
      $global:PSDefaultParameterValues = $originalDefaultParameterValues
  }

  BeforeEach {
    $serviceParams = @{
      Name = "PowerShellTest-$([Guid]::NewGuid().Guid)"
      BinaryPathName = "$env:SystemRoot\System32\cmd.exe"
      StartupType = 'Manual'
    }
    $service = New-Service @serviceParams
  }
  AfterEach {
    $service | Remove-Service
  }

  It "Ignores invalid description MUI entry" {
    $serviceRegPath = "HKLM:\SYSTEM\CurrentControlSet\Services\$($service.Name)"
    Set-ItemProperty -LiteralPath $serviceRegPath -Name Description -Value '@Fake.dll,-0'
    $actual = Get-Service -Name $service.Name -ErrorAction Stop

    $actual.Name | Should -Be $service.Name
    $actual.Status | Should -Be Stopped
    $actual.Description | Should -BeNullOrEmpty
    $actual.UserName | Should -Be LocalSystem
    $actual.StartupType | Should -Be Manual
  }

  It "Ignores no SERVICE_QUERY_CONFIG access" {
    $sddl = ((sc.exe sdshow $service.Name) -join "").Trim()
    $sd = ConvertFrom-SddlString -Sddl $sddl
    $sd.RawDescriptor.DiscretionaryAcl.AddAccess(
      [System.Security.AccessControl.AccessControlType]::Deny,
      [System.Security.Principal.WindowsIdentity]::GetCurrent().User,
      0x1,  # SERVICE_QUERY_CONFIG
      [System.Security.AccessControl.InheritanceFlags]::None,
      [System.Security.AccessControl.PropagationFlags]::None)
    $newSddl = $sd.RawDescriptor.GetSddlForm([System.Security.AccessControl.AccessControlSections]::All)
    $null = sc.exe sdset $service.Name $newSddl

    $actual = Get-Service -Name $service.Name -ErrorAction Stop

    $actual.Name | Should -Be $service.Name
    $actual.Status | Should -Be Stopped
    $actual.Description | Should -BeNullOrEmpty
    $actual.UserName | Should -BeNullOrEmpty
    $actual.StartupType | Should -Be InvalidValue
  }
}
