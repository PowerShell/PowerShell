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
  Context 'Check null or empty value to the -Name parameter' {
    It 'Should throw if null is passed to -Name parameter' {
      try {Get-Service -Name $null }
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}
    }
    It 'Should throw if empty string is passed to -Name parameter' {
      try {Get-Service -Name [String]::Empty }
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}
    }
  }
  Context 'Check null or empty value to the -Name parameter via pipeline' {
    It 'Should throw if null is passed through pipeline to -Name parameter' {
      try {$null | Get-Service -ErrorAction Stop}
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}
    }
    It 'Should throw if empty string is passed through pipeline to -Name parameter' {
      try {[String]::Empty | Get-Service -ErrorAction Stop}
      catch [System.Management.Automation.ParameterBindingException] {$_.Exception.ErrorId | Should Be 'ParameterArgumentValidationError'}

    }
  }
}
