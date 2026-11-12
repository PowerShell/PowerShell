# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

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

    It 'Deserializes a CIM instance whether or not its CIM class can be rehydrated' {
        # Serialized on Windows with class metadata (__ClassMetadata/MiXml). Where the native MI library is
        # available the object is rehydrated into a CimInstance; where it is not (e.g. macOS) the deserialized
        # property bag must be returned instead of failing the whole deserialization.
        $xml = Get-Content -Raw -Path (Join-Path $PSScriptRoot 'assets' 'CimInstance-Win32_TimeZone.clixml')

        $result = [System.Management.Automation.PSSerializer]::Deserialize($xml)

        $result.Bias | Should -Be 180
        $result.Caption | Should -Not -BeNullOrEmpty
        $result.PSTypeNames -match 'Win32_TimeZone' | Should -Not -BeNullOrEmpty
    }

    It 'Keeps the CIM metadata of a deserialized CIM instance hidden but serializable' {
        # The serialization metadata must not show up as properties of the object, but serializing the object
        # again must still carry it so that a receiver able to rehydrate CIM instances gets a CimInstance.
        $xml = Get-Content -Raw -Path (Join-Path $PSScriptRoot 'assets' 'CimInstance-Win32_TimeZone.clixml')
        $result = [System.Management.Automation.PSSerializer]::Deserialize($xml)

        ($result | Select-Object -Property *).PSObject.Properties.Name | Should -Not -Contain '__ClassMetadata'
        ($result | Format-List -Property * | Out-String) | Should -Not -Match '__ClassMetadata'

        $roundTrip = [System.Management.Automation.PSSerializer]::Serialize($result, 1)
        $roundTrip | Should -Match '__ClassMetadata'
        ([System.Management.Automation.PSSerializer]::Deserialize($roundTrip)).Bias | Should -Be 180
    }
}
