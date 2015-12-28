Describe "Test-ConvertTo-Xml" {
    $testObject    = Get-ChildItem
    $slash         = [System.IO.Path]::DirectorySeparatorChar
    $testDirectory = $Env:TEMP + $slash + "testDirectory"
    $testfile      = $testDirectory + $slash + "testfile.xml"
    $nl            = [Environment]::NewLine

    New-Item $testDirectory -ItemType Directory -Force

    It "Should create an xml Document" {
        $($testObject | ConvertTo-Xml).GetType().Name | Should Be XmlDocument
    }

    It "Should be able to save an object after being converted to an xml object" {
        { $xml = $testObject | ConvertTo-Xml
          $xml.Save($testfile) } | Should Not Throw

        Test-Path $testfile | Should Be $true
    }

    It "Should have a data type of XmlDocument and a BaseType of System.Xml.XmlNode" {
        $actual = $testObject | ConvertTo-Xml

    }

    It "Should be able to use the As switch without error" {
        { ConvertTo-Xml -InputObject $testObject -As String }   | Should Not Throw
        { ConvertTo-Xml -InputObject $testObject -As Document } | Should Not Throw
        { ConvertTo-Xml -InputObject $testObject -As Stream }   | Should Not Throw
    }

    It "Should be the same output between the As switch and just saving the file as an xml document" {
        # Create the test object, and do some formatting to get it in a testable format
        $asSwitch = ($testObject | ConvertTo-Xml -As String).Split([Environment]::NewLine,[System.StringSplitOptions]::RemoveEmptyEntries)

        ($testObject | ConvertTo-Xml).Save($testfile)

        # iterate through each line and compare the saved variable and the file contents
        for($line=0; $line -le $testfile.Length; $line++)
        {
            $currentLine = (Get-Content $testfile)[$line]

            $currentLine | Should Be $asSwitch[$line]
        }
    }
}
