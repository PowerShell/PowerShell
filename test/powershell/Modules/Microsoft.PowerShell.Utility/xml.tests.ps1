Describe "XML cmdlets" -Tags "Feature" {
    Context "Select-XML" {
        BeforeAll {
            $fileName = New-Item -Path 'TestDrive:\testSelectXml.xml'
	        Push-Location "$fileName\.."
	        "<Root>" | out-file -LiteralPath $fileName
	        "   <Node Attribute='blah' />" | out-file -LiteralPath $fileName -Append
	        "</Root>" | out-file -LiteralPath $fileName -Append

            $fileNameWithDots = $fileName.FullName.Replace("\", "\.\")

            $driveLetter = [string]($fileName.FullName)[0]
	        $fileNameAsNetworkPath = "\\localhost\$driveLetter`$" + $fileName.FullName.SubString(2)

            class TestData
            {
                [string] $testName 
                [hashtable] $parameters

                TestData($name, $parameters)
                {
                    $this.testName = $name
                    $this.parameters = $parameters
                }
            }

            $testcases = @()
            $testcases += [TestData]::new('literalpath with relative paths', @{LiteralPath = $fileName.Name; XPath = 'Root'})
            $testcases += [TestData]::new('literalpath with absolute paths', @{LiteralPath = $fileName.FullName; XPath = 'Root'})
            $testcases += [TestData]::new('literalpath with path with dots', @{LiteralPath = $fileNameWithDots; XPath = 'Root'})
            if ( ! $IsCoreCLR ) {
                $testcases += [TestData]::new('literalpath with network path', @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'})                                            
            }
            $testcases += [TestData]::new('path with relative paths', @{Path = $fileName.Name; XPath = 'Root'})
            $testcases += [TestData]::new('path with absolute paths', @{Path = $fileName.FullName; XPath = 'Root'})
            $testcases += [TestData]::new('path with path with dots', @{Path = $fileNameWithDots; XPath = 'Root'})
            if ( ! $IsCoreCLR ) {
                $testcases += [TestData]::new('path with network path', @{Path = $fileNameAsNetworkPath; XPath = 'Root'}) 
            }
        }

        AfterAll {
            Remove-Item -LiteralPath $fileName -Force -ErrorAction SilentlyContinue
            Pop-Location
        }
        
        $testcases | % {
            
            $params = $_.parameters

            It $_.testName {
                @(Select-XML @params).Count | Should Be 1
            }
        }

        It "literalpath with non filesystem path" {
            $__data = "abcdefg"
            Select-XML -literalPath variable:__data "Root" -ErrorVariable selectXmlError -ErrorAction SilentlyContinue
            $selectXmlError.FullyQualifiedErrorId | Should Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
        }       
        
        It "path with non filesystem path" {
            $__data = "abcdefg"
            Select-XML -Path variable:\__data "Root" -ErrorVariable selectXmlError -ErrorAction SilentlyContinue
            $selectXmlError.FullyQualifiedErrorId | Should Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
        } 
    }
}
