Describe "XML cmdlets" -Tags 'Feature' {
    Context "Select-XML" {
        BeforeAll {
            $filenamepath = Setup -F testSelectXml.xml -pass -content "<Root><Node Attribute='blah' /></Root>"
            $filename = get-item $filenamepath
            $fileNameWithDots = $fileName.FullName.Replace([io.path]::DirectorySeparatorChar, ".")

            $testcases =
                @{ Name = "literalpath with relative paths"; params = @{LiteralPath = $fileName.Name; XPath = 'Root'}},
                @{ Name = 'literalpath with absolute paths'; params = @{LiteralPath = $fileName.FullName; XPath = 'Root'}},
                @{ Name = 'path with relative paths';        params = @{Path = $fileName.Name; XPath = 'Root'}},
                @{ Name = 'path with absolute paths';        params = @{Path = $fileName.FullName; XPath = 'Root'}}

            if ( $IsWindows )
            {
                $testcases += @{ Name = 'literalpath with path with dots'; params =  @{LiteralPath = $fileNameWithDots; XPath = 'Root'}}
                $testcases += @{ Name = 'path with path with dots';        params =  @{Path = $fileNameWithDots; XPath = 'Root'}}
                $testcases += @{ Name = 'literalpath with network path';   params =  @{LiteralPath = $fileNameAsNetworkPath; XPath = 'Root'}}
                $testcases += @{ Name = 'path with network path';          params =  @{Path = $fileNameAsNetworkPath; XPath = 'Root'}}
            }
            push-location TESTDRIVE:
        }

        AfterAll {
            Pop-Location
        }

        It "<Name>" -testcase $testcases {
            param ( $name, $params )
            @(Select-XML @params).Count | should be 1
        }

        It -skip:(!$IsWindows) "literalpath with non filesystem path" {
            Select-XML -literalPath cert:\currentuser\my "Root" -ErrorVariable selectXmlError -ErrorAction SilentlyContinue
            $selectXmlError.FullyQualifiedErrorId | Should Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
        }

        It -skip:(!$IsWindows) "path with non filesystem path" {
            Select-XML -Path cert:\currentuser\my "Root" -ErrorVariable selectXmlError -ErrorAction SilentlyContinue
            $selectXmlError.FullyQualifiedErrorId | Should Be 'ProcessingFile,Microsoft.PowerShell.Commands.SelectXmlCommand'
        }
    }
}
