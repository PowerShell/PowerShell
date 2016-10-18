Describe "Adapter Tests" -tags "CI" {
    BeforeAll {
        $pso  = [System.Diagnostics.Process]::GetCurrentProcess()
        $processName = $pso.Name
        
        if(-not ('TestCodeMethodClass' -as "type"))
        {
            class TestCodeMethodClass {
                static [int] TestCodeMethod([PSObject] $target, [int] $i)
                {
                    return 1;
                }
            }
        }
        
        $psmemberset = new-object System.Management.Automation.PSMemberSet 'setname1'
        $psmemberset | Add-Member -MemberType NoteProperty -Name NoteName -Value 1
        $testmethod = [TestCodeMethodClass].GetMethod("TestCodeMethod")
        $psmemberset | Add-Member -MemberType CodeMethod -Name TestCodeMethod -Value $testmethod
        
        $document = new-object System.Xml.XmlDocument
        $document.LoadXml("<book ISBN='12345'><title>Pride And Prejudice</title><price>19.95</price></book>")
        $doc = $document.DocumentElement
    }
    It "can get a Dotnet parameterized property" {
        $col  = $pso.psobject.Properties.Match("*")
        $prop = $col.psobject.Members["Item"]
        $prop | Should Not BeNullOrEmpty
        $prop.IsGettable | Should be $true
        $prop.IsSettable | Should be $false
        $prop.TypeNameOfValue | Should be "System.Management.Automation.PSPropertyInfo"
        $prop.Invoke("ProcessName").Value | Should be $processName
    }
    
    It "can get a property" {
        $pso.psobject.Properties["ProcessName"] | should Not BeNullOrEmpty
    }
    
    It "Can access all properties" {
        $props = $pso.psobject.Properties.Match("*")
        $props | should Not BeNullOrEmpty
        $props["ProcessName"].Value |Should be $processName
    }
    
    It "Can invoke a method" {
        $method = $pso.psobject.Methods["ToString"]
        $method.Invoke()  | should be ($pso.ToString())
    }
    
    It "Access a Method via MemberSet adapter" {
        $prop = $psmemberset.psobject.Members["TestCodeMethod"]
        $prop.Invoke(2) | Should be 1
    }
    
    It "Access misc properties via MemberSet adapter" {
        $prop  = $psmemberset.psobject.Properties["NoteName"]
        $prop | Should Not BeNullOrEmpty
        $prop.IsGettable | Should be $true
        $prop.IsSettable | Should be $true
        $prop.TypeNameOfValue | Should be "System.Int32"
    }
    
    It "Access all the properties via XmlAdapter" {
        $col  = $doc.psobject.Properties.Match("*")
        $col.Count | Should Not Be 0
        $prop = $col["price"]
        $prop | Should Not BeNullOrEmpty
    }
    
    It "Access all the properties via XmlAdapter" {
        $prop  = $doc.psobject.Properties["price"]
        $prop.Value | Should Be "19.95"
        $prop.IsGettable | Should Not BeNullOrEmpty
        $prop.IsSettable | Should Not BeNullOrEmpty
        $prop.TypeNameOfValue | Should be "System.String"
    }
    
    It "Call to string on a XmlNode object" {
        $val  = $doc.ToString()
        $val | Should Be "book"
    }
}
