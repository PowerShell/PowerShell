# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "CliXml test" -Tags "CI" {

    BeforeAll {
        $testFilePath = Join-Path "testdrive:\" "testCliXml"
        $subFilePath = Join-Path $testFilePath ".test"

        if(test-path $testFilePath)
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

            TestData($name, $file, $inputObj, $error)
            {
                $this.testName = $name
                $this.inputObject = $inputObj
                $this.expectedError = $error
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

        It "should fail to import PSCredential on non-Windows" -Skip:$IsWindows {
            $cliXml = @"
                <Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
                <Obj RefId="0">
                <TN RefId="0">
                    <T>System.Management.Automation.PSCredential</T>
                    <T>System.Object</T>
                </TN>
                <ToString>System.Management.Automation.PSCredential</ToString>
                <Props>
                    <S N="UserName">foo</S>
                    <!-- this is just an empty password, but needs to be a valid hash -->
                    <SS N="Password">01000000d08c9ddf0115d1118c7a00c04fc297eb01000000977756228672474da9bfbe7b6acc02cb0000000002000000000003660000c0000000100000002529194617877bbbc952c6aacb6535f00000000004800000a000000010000000703ca974777a8335d5e2f8b3e592ff4208000000635d9fcda035734e140000001b6940e3222117a1014c67377e8786549f3dca29</SS>
                </Props>
                </Obj>
                </Objs>
"@
            $path = "$testdrive/cred.xml"
            Set-Content -Path $path -Value $cliXml
            { Import-Clixml -Path $path } | Should -Throw -ErrorId "System.PlatformNotSupportedException,Microsoft.PowerShell.Commands.ImportClixmlCommand"
        }

        It "should import PSCredential" -Skip:(!$IsWindows) {
            $UserName = "Foo"
            $pass = ConvertTo-SecureString "bar" -AsPlainText -Force
            $cred =  [PSCredential]::new($UserName, $pass)
            $path = "$testdrive/cred.xml"
            $cred | Export-Clixml -Path $path
            $cred = Import-Clixml -Path $path
            $cred.UserName | Should -BeExactly "Foo"
            $cred.Password | Should -BeOfType "System.Security.SecureString"
        }
    }
}

##
## CIM deserialization security vulnerability
##
Describe "Deserializing corrupted Cim classes should not instantiate non-Cim types" -Tags "Feature","Slow" {

    BeforeAll {

        # Only run on Windows platform.
        # Ensure calc.exe is avaiable for test.
        $shouldRunTest = $IsWindows -and ((Get-Command calc.exe -ErrorAction SilentlyContinue) -ne $null)
        $skipNotWindows = ! $shouldRunTest
        if ( $shouldRunTest )
        {
            (Get-Process -Name 'win32calc','calculator' 2>$null) | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }

    AfterAll {
        if ( $shouldRunTest )
        {
            (Get-Process -Name 'win32calc','calculator' 2>$null) | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }

    It "Verifies that importing the corrupted Cim class does not launch calc.exe" -skip:$skipNotWindows {

        Import-Clixml -Path (Join-Path $PSScriptRoot "assets\CorruptedCim.clixml")

        # Wait up to 10 seconds for calc.exe to run
        $calcProc = $null
        $count = 0
        while (!$calcProc -and ($count++ -lt 20))
        {
            $calcProc = Get-Process -Name 'win32calc','calculator' 2>$null
            Start-Sleep -Milliseconds 500
        }

        $calcProc | Should -BeNullOrEmpty
    }
}
