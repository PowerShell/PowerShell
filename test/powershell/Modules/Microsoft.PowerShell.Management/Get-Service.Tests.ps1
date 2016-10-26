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
      try {Get-Service -Name $data }
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}
    }
  }
  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if <value> is passed through pipeline to -Name parameter' -TestCases $testCases {
      param($data)
      try {$data | Get-Service -ErrorAction Stop}
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}
    }
  }
}
