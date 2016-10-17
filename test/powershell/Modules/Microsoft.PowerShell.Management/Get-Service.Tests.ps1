# Service cmdlet is currently working on windows builds only
if ($IsWindows)
{
  Describe "Get-Service cmdlet tests" -Tags "CI" {
    It "should throw if null is passed to -Name parameter" {
      {Get-Service -Name $null} | Should Throw
    }
    It "should throw if empty string is passed to -Name parameter" {
      {Get-Service -Name [String]::Empty} | Should Throw
    }
    It "should throw if null is passed through pipeline to -Name parameter" {
      {$null | Get-Service} | Should Throw
    }
    It "should throw if empty string is passed through pipeline to -Name parameter" {
      {[String]::Empty | Get-Service} | Should Throw
    }
  } 
}
