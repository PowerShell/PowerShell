Describe -tags 'Innerloop', 'DRT' "bug613651" {
#      Bug 613651: ParameterizedProperty 'Item' mask an XML Document 'Item' tag
#
# The bug would produce an error when get-member was called (The XML adapter would try
# to add the "Item" parameterized property of the XmlElement to a dictionary that
# already contains the "Item" adapted property)
#
	It "The number of properties is incorrect" {
        $x = [xml] "<root><item>1</item><item>2</item></root>"
        $count = @(get-member -i $x.root -membertype property | measure).Count
		$count | Should Be 1
	}
}
Describe -tags 'Innerloop', 'DRT' "bug914412" {
#      Bug:914412: XML Adapter should support #cdata-section,#text sections
    BeforeAll {
        $cdata = 'some cdata content'
        $text  = 'some text'
        $xmlContent = "<a><![CDATA[$cdata]]>$text</a>"
        $xml = [xml]$xmlContent
        $xmlCdata = $xml.a."#cdata-section"
        $xmlText  = $xml.a."#text"

    }
    Context "Getting values from Xml" {
        It "[xml] type accelerator gives proper type" {
            $xml.GetType() | Should Be ([System.Xml.XmlDocument])
        }

        It "cdata has correct value" {
            $XmlCdata | should Be $cdata 
        }
        It "text has correct value" {
            $xmlText | should Be $text 
        }
    }

    Context "Setting new values for Xml Elements" {
        BeforeAll {
            $newCdata = 'new cdata content'
            $newText  = 'new text'
            $xml.a."#cdata-section" = $newCdata
            $xml.a."#text" = $newText
            $newXmlCdata = $xml.a."#cdata-section"
            $newXmlText  = $xml.a."#text"
        }
        It "text of element has correct value" {
            $newXmlText | Should be $newText
        }

        It "Expected: $newCdata Actual: $xmlCdata" {
            $newXmlCdata | should be $newCdata 
        }
    }
}
Describe -tags 'Innerloop', 'P1' "win8_481571" {
#    <summary>Win8: 481571 Enforcing "PreserveWhitespace" breaks XML pretty printing</summary>
	It "[xml] preserves comments" {
        $content = "<?xml version=`"1.0`" encoding=`"utf-8`"?>`r`n<a>   <b>   <!-- comment --> </b> </a>"
        $xml = [xml]$content
        $actual = $xml.a.b.("#comment")
		$actual | Should Be " comment "
	}
}
