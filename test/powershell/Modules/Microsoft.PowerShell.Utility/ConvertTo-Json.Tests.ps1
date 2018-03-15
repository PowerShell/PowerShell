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
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript({
            $obj = [PSCustomObject]@{P1 = ''; P2 = ''; P3 = ''; P4 = ''; P5 = ''; P6 = ''}
            $obj.P1 = $obj.P2 = $obj.P3 = $obj.P4 = $obj.P5 = $obj.P6 = $obj
            1..100 | Foreach-Object { $obj } | ConvertTo-Json -Depth 10
            # the conversion is expected to take some time, this throw is in case it doesn't
            throw "Should not have thrown exception"
        })
        $null = $ps.BeginInvoke()
        # wait to ensure invocation has reached ConvertTo-Json
        Start-Sleep -Milliseconds 100
        $null = $ps.BeginStop($null, $null)
        # wait to ensure Stopped is set within ConvertTo-Json
        Start-Sleep -Milliseconds 100
        $ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
        $ps.Dispose()
    }
}
