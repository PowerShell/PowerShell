# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Serialization Tests" -tags "CI" {
    BeforeAll {
        $testfileName="SerializationTest.txt"
        $testPath = Join-Path $TestDrive $testfileName
        $testfile = New-Item $testPath -Force
        function SerializeAndDeserialize([PSObject]$inputObject)
        {
            $xmlSerializer = New-Object System.Xml.Serialization.XmlSerializer($inputObject.GetType())
            $stream = [System.IO.StreamWriter]$testPath
            $xmlWriter = [System.Xml.XmlWriter]::Create($stream)
            $xmlSerializer.Serialize($xmlWriter,$inputObject)
            $stream.Close()

            $stream = [System.IO.StreamReader]$testPath
            $xmlReader = [System.Xml.XmlReader]::Create($stream)
            $outputObject = $xmlSerializer.Deserialize($xmlReader)
            $stream.Close()
            $xmlReader.Dispose()
            return $outputObject;
        }

        enum MyColorFlag
        {
            RED
            BLUE
        }
    }
    AfterAll {
            Remove-Item $testPath -Force -ErrorAction SilentlyContinue
    }

    It 'Test DateTimeUtc serialize and deserialize work as expected.' {
        $inputObject = [System.DateTime]::UtcNow;
        SerializeAndDeserialize($inputObject) | Should -Be $inputObject
    }

    It 'Test DateTime stamps serialize and deserialize work as expected.' {
        $objs = [System.DateTime]::MaxValue, [System.DateTime]::MinValue, [System.DateTime]::Today, (New-Object System.DateTime), (New-Object System.DateTime 123456789)
        foreach($inputObject in $objs)
        {
           SerializeAndDeserialize($inputObject) | Should -Be $inputObject
        }
    }

    #pending because of "System.Uri cannot be serialized because it does not have a parameterless constructor."
    It 'Test system Uri objects serialize and deserialize work as expected.' -Pending:$true {
        $uristrings = "http://www.microsoft.com","http://www.microsoft.com:8000","http://www.microsoft.com/index.html","http://www.microsoft.com/default.asp","http://www.microsoft.com/Hello%20World.htm"
        foreach($uristring in $uristrings)
        {
           $inputObject = New-Object System.Uri $uristring
           SerializeAndDeserialize($inputObject) | Should -Be $inputObject
        }
    }

    It 'Test a byte array serialize and deserialize work as expected.' {
        $objs1 = [byte]0, [byte]1, [byte]2, [byte]3, [byte]255
        $objs2 = @()
        $objs3 = [byte]128
        $objs4 = [System.Byte[]]::new(256)
        for($i=0; $i -lt 256; $i++)
        {
           $objs4[$i] = [byte]$i
        }
        $objsArray = New-Object System.Collections.ArrayList
        $objsArray.Add($objs1)
        $objsArray.Add($objs2)
        $objsArray.Add($objs3)
        $objsArray.Add($objs4)

        foreach($inputObject in $objsArray )
        {
           $outputs = SerializeAndDeserialize($inputObject);
           for($i=0;$i -lt $inputObject.Length;$i++)
           {
               $outputs[$i] | Should -Be $inputObject[$i]
           }
        }
    }

    It 'Test Enum serialize and deserialize work as expected.' {
        $inputObject = [MyColorFlag]::RED
        SerializeAndDeserialize($inputObject).ToString() | Should -Be $inputObject.ToString()
    }

    It 'Test SecureString serialize and deserialize work as expected.' {
        #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
        $inputObject = ConvertTo-SecureString -String "PowerShellRocks!" -AsPlainText -Force
        SerializeAndDeserialize($inputObject).Length | Should -Be $inputObject.Length

    }

    It 'Test ScriptProperty object serialize and deserialize work as expected.' {
        $versionObject = New-Object PSObject
        $versionObject | Add-Member -MemberType NoteProperty -Name TestNote -Value "TestNote"
        $versionObject | Add-Member -MemberType ScriptProperty -Name TestScriptProperty -Value { ($this.TestNote) }

        SerializeAndDeserialize($versionObject).TestScriptProperty | Should -Be $versionObject.TestScriptProperty
    }
}

