# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

Describe "CliXml test" -Tags "CI" {

    BeforeAll {
        $testFilePath = Join-Path "testdrive:\" "testCliXml"
        $subFilePath = Join-Path $testFilePath ".test"

        if(Test-Path $testFilePath)
        {
            Remove-Item $testFilePath -Force -Recurse
        }

        # Create the test File and push the location into specified path
        New-Item -Path $testFilePath -ItemType Directory | Out-Null
        New-Item -Path $subFilePath -ItemType Directory | Out-Null
        Push-Location $testFilePath

        class TestData
        {
            [string] $testName
            [object] $inputObject
            [string] $expectedError
            [string] $testFile

            TestData($name, $file, $inputObj, $errorId)
            {
                $this.testName = $name
                $this.inputObject = $inputObj
                $this.expectedError = $errorId
                $this.testFile = $file
            }
        }
    }

    AfterAll {
        Pop-Location
    }

    Context "Export-CliXml" {
        BeforeAll {
            $gpsList = Get-Process pwsh
            $gps = $gpsList | Select-Object -First 1
            $filePath = Join-Path $subFilePath 'gps.xml'

            $testData = @()
            $testData += [TestData]::new("with path as Null", [NullString]::Value, $gps, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ExportClixmlCommand")
            $testData += [TestData]::new("with path as Empty string", "", $gps, "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ExportClixmlCommand")
            $testData += [TestData]::new("with path as non filesystem provider", "env:\", $gps, "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ExportClixmlCommand")
        }

        AfterEach {
            Remove-Item $filePath -Force -ErrorAction SilentlyContinue
        }

        $testData | ForEach-Object {

            It "$($_.testName)" {
                $test = $_
                { Export-Clixml -Depth 1 -LiteralPath $test.testFile -InputObject $test.inputObject -Force } | Should -Throw -ErrorId $test.expectedError
            }
        }

        It "can be created with literal path" {

            $filePath = Join-Path $subFilePath 'gps.xml'
            Export-Clixml -Depth 1 -LiteralPath $filePath -InputObject ($gpsList | Select-Object -First 1)

            $filePath | Should -Exist

            $fileContent = Get-Content $filePath
            $isExisted = $false

            foreach($item in $fileContent)
            {
                foreach($gpsItem in $gpsList)
                {
                    $checkId = $gpsItem.Id
                    if (($null -ne $(Select-String -InputObject $item -SimpleMatch $checkId)) -and ($null -ne $(Select-String -InputObject $item -SimpleMatch "Id")))
                    {
                        $isExisted = $true
                        break;
                    }
                }
            }

            $isExisted | Should -BeTrue
        }

        It "can be created with literal path using pipeline" {

            $filePath = Join-Path $subFilePath 'gps.xml'
            ($gpsList | Select-Object -First 1) | Export-Clixml -Depth 1 -LiteralPath $filePath

            $filePath | Should -Exist

            $fileContent = Get-Content $filePath
            $isExisted = $false

            foreach($item in $fileContent)
            {
                foreach($gpsItem in $gpsList)
                {
                    $checkId = $gpsItem.Id
                    if (($null -ne $(Select-String -InputObject $item -SimpleMatch $checkId)) -and ($null -ne $(Select-String -InputObject $item -SimpleMatch "Id")))
                    {
                        $isExisted = $true
                        break;
                    }
                }
            }

            $isExisted | Should -BeTrue
        }
    }

    Context "Import-CliXML" {
        BeforeAll {
            $gpsList = Get-Process pwsh
            $gps = $gpsList | Select-Object -First 1
            $filePath = Join-Path $subFilePath 'gps.xml'

            $testData = @()
            $testData += [TestData]::new("with path as Null", [NullString]::Value, $null, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ImportClixmlCommand")
            $testData += [TestData]::new("with path as Empty string", "", $null, "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ImportClixmlCommand")
            $testData += [TestData]::new("with path as non filesystem provider", "env:\", $null, "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ImportClixmlCommand")
        }

        $testData | ForEach-Object {

            It "$($_.testName)" {
                $test = $_

                { Import-Clixml -LiteralPath $test.testFile } | Should -Throw -ErrorId $test.expectedError
            }
        }

        It "can import from a literal path" {
            Export-Clixml -Depth 1 -LiteralPath $filePath -InputObject $gps
            $filePath | Should -Exist

            $fileContent = Get-Content $filePath
            $fileContent | Should -Not -Be $null

            $importedProcess = Import-Clixml $filePath
            $importedProcess.ProcessName | Should -Not -BeNullOrEmpty
            $gps.ProcessName | Should -Be $importedProcess.ProcessName
            $importedProcess.Id | Should -Not -BeNullOrEmpty
            $gps.Id | Should -Be $importedProcess.Id
        }

        It "can import from a literal path using pipeline" {
            $gps | Export-Clixml -Depth 1 -LiteralPath $filePath
            $filePath | Should -Exist

            $fileContent = Get-Content $filePath
            $fileContent | Should -Not -Be $null

            $importedProcess = Import-Clixml $filePath
            $importedProcess.ProcessName | Should -Not -BeNullOrEmpty
            $gps.ProcessName | Should -Be $importedProcess.ProcessName
            $importedProcess.Id | Should -Not -BeNullOrEmpty
            $gps.Id | Should -Be $importedProcess.Id
        }

        It "test follow-up for WinBlue: 161470 - Export-CliXml errors in WhatIf scenarios" {

            $testPath = "testdrive:\Bug161470NonExistPath.txt"
            Export-Clixml -Path $testPath -InputObject "string" -WhatIf
            $testPath | Should -Not -Exist
        }

        It "should import PSCredential" {
            $UserName = "Foo"
            $pass = ConvertTo-SecureString (New-RandomHexString) -AsPlainText -Force
            $cred =  [PSCredential]::new($UserName, $pass)
            $path = "$testdrive/cred.xml"
            $cred | Export-Clixml -Path $path
            $cred = Import-Clixml -Path $path
            $cred.UserName | Should -BeExactly "Foo"
            $cred.Password | Should -BeOfType System.Security.SecureString
        }
    }

    Context "ConvertTo-CliXml"{
        BeforeAll {
            $gpsList = Get-Process pwsh
            $gps = $gpsList | Select-Object -First 1
        }

        It "Create by passing as parameter" {
            $content = ConvertTo-CliXml -Depth 1 -InputObject ($gpsList | Select-Object -First 1)
            $isExisted = $false

            foreach($item in $content)
            {
                foreach($gpsItem in $gpsList)
                {
                    $checkId = $gpsItem.Id
                    if (($null -ne $(Select-String -InputObject $item -SimpleMatch $checkId)) -and ($null -ne $(Select-String -InputObject $item -SimpleMatch "Id")))
                    {
                        $isExisted = $true
                        break;
                    }
                }
            }

            $isExisted | Should -BeTrue
        }

        It "Create by passing as pipeline" {
            $content = ($gpsList | Select-Object -First 1) | ConvertTo-CliXml -Depth 1

            $isExisted = $false

            foreach($item in $content)
            {
                foreach($gpsItem in $gpsList)
                {
                    $checkId = $gpsItem.Id
                    if (($null -ne $(Select-String -InputObject $item -SimpleMatch $checkId)) -and ($null -ne $(Select-String -InputObject $item -SimpleMatch "Id")))
                    {
                        $isExisted = $true
                        break;
                    }
                }
            }

            $isExisted | Should -BeTrue
        }
    }

    Context "ConvertFrom-CliXml" {
        BeforeAll {
            $gpsList = Get-Process pwsh
            $gps = $gpsList | Select-Object -First 1
        }

        It "Create by passing as parameter" {
            $content = ConvertTo-CliXml -Depth 1 -InputObject $gps

            $content | Should -Not -Be $null

            $importedProcess = ConvertFrom-CliXml -InputObject $content
            $importedProcess.ProcessName | Should -Not -BeNullOrEmpty
            $gps.ProcessName | Should -Be $importedProcess.ProcessName
            $importedProcess.Id | Should -Not -BeNullOrEmpty
            $gps.Id | Should -Be $importedProcess.Id
        }

        It "Create by passing as pipeline" {
            $content = $gps | ConvertTo-CliXml -Depth 1

            $content | Should -Not -Be $null

            $importedProcess = $content | ConvertFrom-CliXml
            $importedProcess.ProcessName | Should -Not -BeNullOrEmpty
            $gps.ProcessName | Should -Be $importedProcess.ProcessName
            $importedProcess.Id | Should -Not -BeNullOrEmpty
            $gps.Id | Should -Be $importedProcess.Id
        }

        It "Should import PSCredential" {
            $UserName = "Foo"
            $pass = ConvertTo-SecureString (New-RandomHexString) -AsPlainText -Force
            $cred =  [PSCredential]::new($UserName, $pass)

            $content = $cred | ConvertTo-CliXml
            $cred2 = ConvertFrom-CliXml -InputObject $content
            $cred2.UserName | Should -BeExactly $cred.UserName
            $cred2.Password | Should -BeOfType System.Security.SecureString
            $cred2.GetNetworkCredential().Password | Should -BeExactly $cred.GetNetworkCredential().Password
        }
    }
}

##
## CIM deserialization security vulnerability
##
Describe "Deserializing corrupted Cim classes should not instantiate non-Cim types" -Tags "Feature","Slow" {

    BeforeAll {

        # Only run on Windows platform.
        # Ensure calc.exe is available for test.
        $shouldRunTest = $IsWindows -and ((Get-Command calc.exe -ErrorAction SilentlyContinue) -ne $null)
        $skipNotWindows = ! $shouldRunTest
        if ( $shouldRunTest )
        {
            (Get-Process -Name 'win32calc','calculator' 2> $null) | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }

    AfterAll {
        if ( $shouldRunTest )
        {
            (Get-Process -Name 'win32calc','calculator' 2> $null) | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verifies that importing the corrupted Cim class does not launch calc.exe" -Skip:$skipNotWindows {

        Import-Clixml -Path (Join-Path $PSScriptRoot "assets\CorruptedCim.clixml")

        # Wait up to 10 seconds for calc.exe to run
        $calcProc = $null
        $count = 0
        while (!$calcProc -and ($count++ -lt 20))
        {
            $calcProc = Get-Process -Name 'win32calc','calculator' 2> $null
            Start-Sleep -Milliseconds 500
        }

        $calcProc | Should -BeNullOrEmpty
    }
}
