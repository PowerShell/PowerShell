$here = Split-Path -Parent $MyInvocation.MyCommand.Path

Describe "Measure-Command" {

    Context "Validate return types for Measure-Command" {

        It "Should return TimeSpan as the return type" {
            (Measure-Command { Get-Date }).GetType() | Should Be timespan
        }
    }

    Context "Validate that it is executing commands correctly" {

        It "Should return TimeSpan after executing a script" {
            (Measure-Command { echo hi }).GetType() | Should Be timespan
        }

        It "Should return TimeSpan after executing a cmdlet" {
            $pesterscript = "$here/assets/echoscript.ps1"
            $testfile = "$here/assets/echoscript.ps1"
            $testcommand = "echo pestertestscript"
            $testcommand | Add-Content -Path $testfile

            (Measure-Command { $pesterscript }).GetType() | Should Be timespan
            Remove-Item $testfile
        }
    }
}
