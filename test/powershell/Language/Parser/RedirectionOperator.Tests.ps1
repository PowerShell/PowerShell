Describe "Redirection operator now supports encoding changes" {
    BeforeAll {
        $asciiString = "abc"

        if ( $IsWindows ) {
             $asciiCR = "`r`n"
        }
        else {
            $asciiCR = [string][char]10
        }


        # If out-file -encoding happens to have a default, be sure to 
        # save it away
        $SavedValue = $null
        $oldDefaultParameterValues = $psDefaultParameterValues
        $psDefaultParameterValues = @{}
    }
    AfterAll {
        # be sure to tidy up afterwards
        $psDefaultParameterValues = $oldDefaultParameterValues
    }
    BeforeEach {
        # start each test with a clean plate!
        $psdefaultParameterValues.Remove("out-file:encoding")
    }
    AfterEach {
        # end each test with a clean plate!
        $psdefaultParameterValues.Remove("out-file:encoding")
    }

    It "If encoding is unset, redirection should be Unicode" {
        $asciiString > TESTDRIVE:\file.txt
        $bytes = get-content -encoding byte TESTDRIVE:\file.txt
        # create the expected
        $BOM = [text.encoding]::unicode.GetPreamble()
        $TXT = [text.encoding]::unicode.GetBytes($asciiString)
        $CR  = [text.encoding]::unicode.GetBytes($asciiCR)
        $expectedBytes = .{ $BOM; $TXT; $CR }
        $bytes.Count | should be $expectedBytes.count
        for($i = 0; $i -lt $bytes.count; $i++) {
            $bytes[$i] | Should be $expectedBytes[$i]
        }
    }
    
    # $availableEncodings = "unknown","string","unicode","bigendianunicode","utf8","utf7", "utf32","ascii","default","oem"
    $availableEncodings = (get-command out-file).Parameters["Encoding"].Attributes.ValidValues

    foreach($encoding in $availableEncodings) {
        # some of the encodings accepted by out-file aren't real,
        # and out-file has its own translation, so we'll
        # not do that logic here, but simply ignore those encodings
        # as they eventually are translated to "real" encoding
        $enc = [system.text.encoding]::$encoding
        if ( $enc )
        {
            $msg = "Overriding encoding for out-file is respected for $encoding" 
            $BOM = $enc.GetPreamble()
            $TXT = $enc.GetBytes($asciiString)
            $CR  = $enc.GetBytes($asciiCR)
            $expectedBytes = .{ $BOM; $TXT; $CR }
            $psdefaultparameterValues["out-file:encoding"] = "$encoding"
            $asciiString > TESTDRIVE:/file.txt
            $observedBytes = Get-Content -encoding Byte TESTDRIVE:/file.txt
            # THE TEST
            It $msg {
                $observedBytes.Count | Should be $expectedBytes.Count
                for($i = 0;$i -lt $observedBytes.Count; $i++) {
                    $observedBytes[$i] | Should be $expectedBytes[$i]
                }
            }

        }
    }

}
