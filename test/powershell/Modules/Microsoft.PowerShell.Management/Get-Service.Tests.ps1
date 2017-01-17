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
    @{ data = $null; value = 'null' },
    @{ data = [String]::Empty; value = 'empty string' }

  Context 'Check null or empty value to the -Name parameter' {
    It 'Should throw if <value> is passed to -Name parameter' -TestCases $testCases {
      param($data)
      try {
        $null = Get-Service -Name $data -ErrorAction Stop
        throw 'Expected error on previous command'
      }
      catch {
        $_.FullyQualifiedErrorId | Should Be 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
      }
    }
  }
  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if <value> is passed through pipeline to -Name parameter' -TestCases $testCases {
      param($data)
      try {
        $null = Get-Service -Name $data -ErrorAction Stop
        throw 'Expected error on previous command'
      }
      catch {
        $_.FullyQualifiedErrorId | Should Be 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
      }
    }
  }
}
