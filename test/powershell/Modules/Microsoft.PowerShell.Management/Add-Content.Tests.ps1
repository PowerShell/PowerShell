# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Add-Content cmdlet tests" -Tags "CI" {

  BeforeAll {
    $file1 = "file1.txt"
    Setup -File "$file1"
  }

  Context "Add-Content should actually add content" {
    It "should Add-Content to TestDrive:\$file1" {
      $result = Add-Content -Path TestDrive:\$file1 -Value "ExpectedContent" -PassThru
      $result | Should -BeExactly "ExpectedContent"
    }

    It "should return expected string from TestDrive:\$file1" {
      $result = Get-Content -Path TestDrive:\$file1
      $result | Should -BeExactly "ExpectedContent"
    }

    It "should Add-Content to TestDrive:\dynamicfile.txt with dynamic parameters" -Pending:($IsLinux -Or $IsMacOS) {#https://github.com/PowerShell/PowerShell/issues/891
      $result = Add-Content -Path TestDrive:\dynamicfile.txt -Value "ExpectedContent" -PassThru
      $result | Should -BeExactly "ExpectedContent"
    }

    It "should return expected string from TestDrive:\dynamicfile.txt" -Pending:($IsLinux -Or $IsMacOS) {#https://github.com/PowerShell/PowerShell/issues/891
      $result = Get-Content -Path TestDrive:\dynamicfile.txt
      $result | Should -BeExactly "ExpectedContent"
    }

    It "should Add-Content to TestDrive:\$file1 even when -Value is `$null" {
      $AsItWas = Get-Content -Path TestDrive:\$file1
      { Add-Content -Path TestDrive:\$file1 -Value $null -ErrorAction Stop } | Should -Not -Throw
      Get-Content -Path TestDrive:\$file1 | Should -BeExactly $AsItWas
    }

    It "should throw 'ParameterArgumentValidationErrorNullNotAllowed' when -Path is `$null" {
      { Add-Content -Path $null -Value "ShouldNotWorkBecausePathIsNull" -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddContentCommand"
    }

    #[BugId(BugDatabase.WindowsOutOfBandReleases, 903880)]
    It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$()" {
      { Add-Content -Path $() -Value "ShouldNotWorkBecausePathIsInvalid" -ErrorAction Stop } | Should -Throw -ErrorId "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.AddContentCommand"
    }

    It "Should throw an error on a directory" {
      { Add-Content -Path . -Value "WriteContainerContentException" -ErrorAction Stop } | Should -Throw -ErrorId "WriteContainerContentException,Microsoft.PowerShell.Commands.AddContentCommand"
    }

    #[BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
    It "should throw 'NotSupportedException' when you add-content to an unsupported provider" -Skip:($IsLinux -Or $IsMacOS) {
      { Add-Content -Path HKLM:\\software\\microsoft -Value "ShouldNotWorkBecausePathIsUnsupported" -ErrorAction Stop } | Should -Throw -ErrorId "NotSupported,Microsoft.PowerShell.Commands.AddContentCommand"
    }

    #[BugId(BugDatabase.WindowsOutOfBandReleases, 9058182)]
    It "should be able to pass multiple [string]`$objects to Add-Content through the pipeline to output a dynamic Path file" -Pending:($IsLinux -Or $IsMacOS) {#https://github.com/PowerShell/PowerShell/issues/891
      "hello","world" | Add-Content -Path TestDrive:\dynamicfile2.txt
      $result = Get-Content -Path TestDrive:\dynamicfile2.txt
      $result.length | Should -Be 2
      $result[0]     | Should -BeExactly "hello"
      $result[1]     | Should -BeExactly "world"
    }

    It "Should not block reads while writing" {
      $logpath = Join-Path $testdrive "test.log"

      Set-Content $logpath -Value "hello"

      $f = [System.IO.FileStream]::new($logpath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)

      Add-Content $logpath -Value "world"

      $f.Close()

      $content = Get-Content $logpath
      $content | Should -HaveCount 2
      $content[0] | Should -BeExactly "hello"
      $content[1] | Should -BeExactly "world"
    }
  }
}
