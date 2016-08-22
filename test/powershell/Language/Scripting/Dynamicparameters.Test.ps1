Describe "Dynamic parameter support in script cmdlets." -Tags "CI" {
    
    function foo-bar
    {
      [CmdletBinding()]
      param($path)

      dynamicparam {
        if ($PSBoundParameters["path"] -contains "abc") {
          $attributes = new-object System.Management.Automation.ParameterAttribute
          $attributes.ParameterSetName = 'pset1'
          $attributes.Mandatory = $false

          $attributeCollection = new-object -Type System.Collections.ObjectModel.Collection``1[System.Attribute]
          $attributeCollection.Add($attributes)
            
          $dynParam1 = new-object System.Management.Automation.RuntimeDefinedParameter("dp1", [Int32], $attributeCollection)

          $paramDictionary = new-object System.Management.Automation.RuntimeDefinedParameterDictionary
          $paramDictionary.Add("dp1", $dynParam1)
            
          return $paramDictionary
        }
    
        $paramDictionary = $null
        return $null
      }

      begin {
        if ($paramDictionary -ne $null) {
          if ($paramDictionary.dp1.Value -ne $null) {
            $paramDictionary.dp1.Value
          }
          else {
            "dynamic parameters not passed"
          }
        }
        else {
          "no dynamic parameters"
        }
      }
      process {}
      end {}
    }

    It "The dynamic parameter is enabled and bound" {
        (foo-bar -path abc -dp1 42) | Should Be 42
    }

    Context "When the dynamic parameter is not available, and raises an error when specified" {
        $failed = $true
        try {
            foo-bar -path def -dp1 42
            $failed = $false
        } catch {
            It '$_.FullyQualifiedErrorId' { $_.FullyQualifiedErrorId | Should Be "NamedParameterNotFound,foo-bar" }
            It 'An error should be raised' { $failed | Should Be $true }
        }
    }

    It "No dynamic parameter shouldn't cause an errr " {        
        (foo-bar -path def ) | Should Be 'no dynamic parameters'
    }

    It "Not specifying dynamic parameter shouldn't cause an error" {
        (foo-bar -path abc) | Should Be 'dynamic parameters not passed'
    }
}
