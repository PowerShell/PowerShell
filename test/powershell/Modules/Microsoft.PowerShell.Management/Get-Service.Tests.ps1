
Describe "Get-Service cmdlet tests" -Tags "CI" {

  BeforeAll {
    $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
    if ( -not $IsWindows ) {
        $PSDefaultParameterValues["it:skip"] = $true
    }

    $testCases =
      @{ data = $null; value = 'null' },
      @{ data = [String]::Empty; value = 'empty string' }
  }

  AfterAll {
      $global:PSDefaultParameterValues = $originalDefaultParameterValues
  }

  Context 'Check null or empty value to the -Name parameter' {
    It 'Should throw if <value> is passed to -Name parameter' -TestCases $testCases {
      param($data)
      { $null = Get-Service -Name $data -ErrorAction Stop } | ShouldBeErrorId 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
    }
  }
  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if <value> is passed through pipeline to -Name parameter' -TestCases $testCases {
      param($data)
      { $null = Get-Service -Name $data -ErrorAction Stop } | ShouldBeErrorId 'ParameterArgumentValidationError,Microsoft.Powershell.Commands.GetServiceCommand'
    }
  }
}
