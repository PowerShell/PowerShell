# This is a Pester test suite to validate the Format-Hex cmdlet in the Microsoft.PowerShell.Utility module.
#
# Copyright (c) Microsoft Corporation, 2015
#

<#
    Purpose:
        Verify that Format-Hex display the Hexa decimal value for the input data.
                
    Action:
        Run Format-Fex.
               
    Expected Result: 
        Hexa decimal equivalent of the input data is displayed. 
#>

Describe "FormatHex" -tags "CI" {
    BeforeAll {
        Setup -d FormatHexDataDir
        $inputText1 = 'Hello World'
        $inputText2 = 'This is a bit more text'
        $inputFile1 = setup -f "FormatHexDataDir/SourceFile-1.txt" -content $inputText1 -pass
        $inputFile2 = setup -f "FormatHexDataDir/SourceFile-2.txt" -content $inputText2 -pass
    }
    
    # This test is to validate to pipeline support in Format-Hex cmdlet.  
    It "ValidatePipelineSupport" {

        # InputObject Parameter set should get invoked and 
        # the input data should be treated as string.
        $result = $inputText1 | Format-Hex
        $result | Should Not Be $null
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true
    }

    # This test is to validate to pipeline support in Format-Hex cmdlet.  
    It "ValidateByteArrayInputSupport" {

        # InputObject Parameter set should get invoked and 
        # the input data should be treated as byte[].
        $inputBytes = [System.Text.Encoding]::ASCII.GetBytes($inputText1)

        $result =  Format-Hex -InputObject $inputBytes
        $result | Should Not Be $null
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true   
    }

    # This test is to validate to input given through Path parameter set in Format-Hex cmdlet.
    It "ValidatePathParameterSet" {

        $result =  Format-Hex -Path $inputFile1
        $result | Should Not Be $null
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true  
    }

    # This test is to validate to Path parameter set is considered as default in Format-Hex cmdlet.
    It "ValidatePathAsDefaultParameterSet" {

        $result =  Format-Hex $inputFile1
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true  
    }

    # This test is to validate to input given through LiteralPath parameter set in Format-Hex cmdlet.
    It "ValidateLiteralPathParameterSet" {
        
        $result =  Format-Hex -LiteralPath $inputFile1
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true
    }

    # This test is to validate to input given through pipeline. The input being piped from results of Get-hildItem
    It "ValidateFileInfoPipelineInput" {
        
        $result = Get-ChildItem $inputFile1 | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true
    }

    # This test is to validate Encoding formats functionality of Format-Hex cmdlet.
    It "ValidateEncodingFormats" {
        
        $result =  Format-Hex -InputObject $inputText1 -Encoding ASCII
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        ($actualResult -match $inputText1) | Should Be $true
    }

    # This test is to validate that integers can be piped to the format-hex
    It "ValidateIntegerInput" {
        
        $result = 1,2,3,4 | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        # whitespace sensitive
        $actualResult | should be "00000000   01 02 03 04                                      ....            "
    }

    # This test is to validate that integers can be piped to the format-hex
    # and properly represented as characters in the string
    It "ValidateIntegerInputThatPresentAsCharacters" {
        
        $result = 65..68 | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        # whitespace sensitive
        $actualResult | should be "00000000   41 42 43 44                                      ABCD            "
    }

    # This test is to validate that integers can be piped to the format-hex
    It "ValidateIntegerRawInput" {
        
        $result = 1,2,3,4 | Format-Hex -Raw
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        # whitespace sensitive
        $actualResult | should be "00000000   01 00 00 00 02 00 00 00 03 00 00 00 04 00 00 00  ................"
    }

    # handle int64
    It "ValidateInteger64" {
        
        $result = [int64]::MaxValue | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString()
        # whitespace sensitive
        $actualResult | Should Match "00000000   FF FF FF FF FF FF FF 7F                          ."
    }

    # handle bytes, int16, int32, and int64
    It "Validate combined and reduced number formatting" {
        $b = 65 # fits in a byte
        $i16 = 32767 # fits in an int16
        $i32 = 2147483647 # an int32
        $i64 = 9223372036854775807 # an int64
        $result = $b,$i16,$i32,$i64 | format-Hex
        $result.GetType().Name |  should be 'ByteCollection'
        $actualResult = $result.ToString()
        $actualResult | should Match "00000000   41 FF 7F FF FF FF 7F FF FF FF FF FF FF FF 7F     A"
    }

    # handle bytes, int16, int32, and int64
    It "Validate combined and with raw number formatting" {
        $b = 65 # fits in a byte
        $i16 = 32767 # fits in an int16
        $i32 = 2147483647 # an int32
        $i64 = 9223372036854775807 # an int64
        # this will cause 2 lines to be emitted
        $result = $b,$i16,$i32,$i64 | format-Hex -Raw
        $result[0].GetType().Name |  should be 'ByteCollection'
        $result[1].GetType().Name |  should be 'ByteCollection'
        $result0 = $result[0].ToString()
        $result0 | should match "00000000   41 00 00 00 FF 7F 00 00 FF FF FF 7F FF FF FF FF  A"
        $result1 = $result[1].ToString()
        $result1 | should match "00000010   FF FF FF 7F                                      .."
    }

    # This test is to validate that streamed text does not have buffer underrun problems
    It "ValidateEachBufferHasCorrectContentForStreamingText" {
        $result = "a" * 30 | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be 'ByteCollection'
        $actualResult = $result.ToString() -split "`r`n"
        $actualResult.Count | should be 2
        $actualResult[0].ToString() | Should be "00000000   61 61 61 61 61 61 61 61 61 61 61 61 61 61 61 61  aaaaaaaaaaaaaaaa"
        $actualResult[1].ToString() | Should be "00000010   61 61 61 61 61 61 61 61 61 61 61 61 61 61        aaaaaaaaaaaaaa  "
    }

    # This test is to validate that files do not have buffer underrun problems
    It "ValidateEachBufferHasCorrectContentForFiles" {
        $result = Format-Hex -path $InputFile2
        $result | Should Not BeNullOrEmpty
        $result.Count | should be 2
        $result[0].ToString() | Should be "00000000   54 68 69 73 20 69 73 20 61 20 62 69 74 20 6D 6F  This is a bit mo"
        if ( $IsCoreCLR ) {
            $result[1].ToString() | Should be "00000010   72 65 20 74 65 78 74                             re text         "
        }
    }

    # This test ensures that if we stream bytes from a file, the output is correct
    It "ValidateStreamOfBytesFromFileHasProperOutput" {
        $result = Get-Content $InputFile1 -Encoding Byte | Format-Hex
        $result | Should Not BeNullOrEmpty
        $result.GetType().Name | Should Be "ByteCollection"
        if ( $IsCoreCLR ) {
            $result.ToString() | Should be    "00000000   48 65 6C 6C 6F 20 57 6F 72 6C 64                 Hello World     "
        }
    }

    # This test is to validate the alias for Format-Hex cmdlet.
    It "ValidateCmdletAlias" {
        
        try
        {
            $result = Get-Command fhx -ErrorAction Stop
            $result | Should Not BeNullOrEmpty
            $result.CommandType.ToString() | Should Be "Alias"
        }
        catch
        {
            $_ | Should BeNullOrEmpty
        }
    }
}
