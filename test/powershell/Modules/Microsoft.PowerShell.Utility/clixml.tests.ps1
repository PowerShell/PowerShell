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
            $gpsList = Get-Process powershell
            $gps = $gpsList | Select-Object -First 1 
            $filePath = Join-Path $subFilePath 'gps.xml'
                        
            $testData = @()
            $testData += [TestData]::new("with path as Null", [NullString]::Value, $gps, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ExportClixmlCommand")
            $testData += [TestData]::new("with path as Empty string", "", $gps, "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ExportClixmlCommand")
            $testData += [TestData]::new("with path as non filesystem provider", "cert:\", $gps, "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ExportClixmlCommand")
        }

        AfterEach {
            Remove-Item $filePath -Force -ErrorAction SilentlyContinue
        }

        $testData | % {

            It "$($_.testName)" {
                $test = $_
                    
                try
                {
                    Export-Clixml -LiteralPath $test.testFile -InputObject $test.inputObject -Force
                }
                catch
                {
                    $exportCliXmlError = $_
                }

                $exportCliXmlError.FullyQualifiedErrorId | Should Be $test.expectedError
            }
        }
                
        It "can be created with literal path" {

            $filePath = Join-Path $subFilePath 'gps.xml'
            Export-Clixml -LiteralPath $filePath -InputObject ($gpsList | Select-Object -First 1)

            $filePath | Should Exist
            
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
            
            $isExisted | Should Be $true       
        }

        It "can be created with literal path using pipeline" {

            
            $filePath = Join-Path $subFilePath 'gps.xml'
            ($gpsList | Select-Object -First 1) | Export-Clixml -LiteralPath $filePath

            $filePath | Should Exist
            
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
            
            $isExisted | Should Be $true       
        }
    }

    Context "Import-CliXML" {
        BeforeAll {
            $gpsList = Get-Process powershell
            $gps = $gpsList | Select-Object -First 1 
            $filePath = Join-Path $subFilePath 'gps.xml'
            
            $testData = @()
            $testData += [TestData]::new("with path as Null", [NullString]::Value, $null, "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ImportClixmlCommand")
            $testData += [TestData]::new("with path as Empty string", "", $null, "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.ImportClixmlCommand")
            $testData += [TestData]::new("with path as non filesystem provider", "cert:\", $null, "ReadWriteFileNotFileSystemProvider,Microsoft.PowerShell.Commands.ImportClixmlCommand")
        }

        $testData | % {

            It "$($_.testName)" {
                $test = $_
                    
                try
                {
                    Import-Clixml -LiteralPath $test.testFile
                }
                catch
                {
                    $importCliXmlError = $_
                }

                $importCliXmlError.FullyQualifiedErrorId | Should Be $test.expectedError
            }
        }

        It "can import from a literal path" {            
            Export-Clixml -LiteralPath $filePath -InputObject $gps
            $filePath | Should Exist

            $fileContent = Get-Content $filePath
            $fileContent | Should Not Be $null
            
            $importedProcess = Import-Clixml $filePath
            $gps.ProcessName | Should Be $importedProcess.ProcessName
            $gps.Id | Should Be $importedProcess.Id
        }

        It "can import from a literal path using pipeline" {            
            $gps | Export-Clixml -LiteralPath $filePath
            $filePath | Should Exist

            $fileContent = Get-Content $filePath
            $fileContent | Should Not Be $null
            
            $importedProcess = Import-Clixml $filePath
            $gps.ProcessName | Should Be $importedProcess.ProcessName
            $gps.Id | Should Be $importedProcess.Id
        }

        It "test follow-up for WinBlue: 161470 - Export-CliXml errors in WhatIf scenarios" {

            $testPath = "testdrive:\Bug161470NonExistPath.txt"
            Export-Clixml -Path $testPath -InputObject "string" -WhatIf
            $testPath | Should Not Exist
        }
    }
}
