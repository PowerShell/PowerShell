# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe 'ConvertTo-Json' -tags "CI" {
    It 'Newtonsoft.Json.Linq.Jproperty should be converted to Json properly' {
        $EgJObject = New-Object -TypeName Newtonsoft.Json.Linq.JObject
        $EgJObject.Add("TestValue1", "123456")
        $EgJObject.Add("TestValue2", "78910")
        $EgJObject.Add("TestValue3", "99999")
        $dict = @{}
        $dict.Add('JObject', $EgJObject)
        $dict.Add('StrObject', 'This is a string Object')
        $properties = @{'DictObject' = $dict; 'RandomString' = 'A quick brown fox jumped over the lazy dog'}
        $object = New-Object -TypeName psobject -Property $properties
        $jsonFormat = ConvertTo-Json -InputObject $object
        $jsonFormat | Should Match '"TestValue1": 123456'
        $jsonFormat | Should Match '"TestValue2": 78910'
        $jsonFormat | Should Match '"TestValue3": 99999'
    }

	It "StopProcessing should succeed" {
        $tmpFile = Join-Path $TestDrive "test.txt"
        Set-Content -Path $tmpFile -Value "hello"
        $ps = [PowerShell]::Create()
        $null = $ps.AddCommand("Get-Content")
        $null = $ps.AddParameter("Path", $tmpFile)
        $null = $ps.AddCommand("ConvertTo-Json")
        $null = $ps.AddParameter("Depth", 10)
        $null = $ps.BeginInvoke()
        Start-Sleep -Milliseconds 100
        $null = $ps.Stop()
        $ps.InvocationStateInfo.State | should be "Stopped"
        $ps.Dispose()
    }
}
