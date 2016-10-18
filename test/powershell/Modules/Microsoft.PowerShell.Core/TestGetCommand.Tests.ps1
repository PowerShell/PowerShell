Describe "Get-Command Tests" -Tags "CI" {
    BeforeAll {       
        function TestGetCommand-DynamicParametersDCR 
        {
        [CmdletBinding()]
            param (
            [Parameter(Mandatory = $true, Position = 0)]
            [ValidateSet("ReturnNull", "ReturnThis", "Return1", "Return2","Return3", "ReturnDuplicateParameter", "ReturnAlias", "ReturnDuplicateAlias","ReturnObjectNoParameters", "ReturnGenericParameter", "ThrowException")]
            [string] $TestToRun,

            [Parameter()]
            [Type]$ParameterType
            )
          
            DynamicParam {
                if ( ! $testToRun ) {
                    $testToRun = "returnnull"
                }
                $dynamicParamDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                switch ( $testToRun )
                {
                    "returnnull" { 
                        $dynamicParamDictionary = $null
                        break
                     }
                     "return1" {
                        $attr = [System.Management.Automation.ParameterAttribute]::new()
                        $attr.Mandatory = $true
				        $dynamicParameter = [System.Management.Automation.RuntimeDefinedParameter]::new("OneString",[string],$attr)
                        $dynamicParamDictionary.Add("OneString",$dynamicParameter)
                        break 
                    }
                    "return2" {
                        $attr1 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr1.Mandatory = $true
                        $attr1.ParameterSetName = "__AllParameterSets"
                        $ac1 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac1.Add($attr1)
                        $p1 = [System.Management.Automation.RuntimeDefinedParameter]::new("OneString",[string],$ac1)
                        $dynamicParamDictionary.Add("OneString",$p1)


                        $attr2 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr2.Mandatory = $false
                        $attr2.ParameterSetName = "__AllParameterSets"
                        $VRattr = [System.Management.Automation.ValidateRangeAttribute]::new(5,9)

                        $ac2 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac2.Add($attr2)
                        $ac2.Add($VRattr)
                        $p2 = [System.Management.Automation.RuntimeDefinedParameter]::New("TwoInt",[int],$ac2)
                        $dynamicParamDictionary.Add("TwoInt",$p2)
                        break
                    }
                    "return3" {
                        $attr1 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr1.Mandatory = $true
                        $attr1.ParameterSetName = "__AllParameterSets"
                        $ac1 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac1.Add($attr1)
                        $p1 = [System.Management.Automation.RuntimeDefinedParameter]::new("OneString",[string],$ac1)
                        $dynamicParamDictionary.Add("OneString",$p1)


                        $attr2 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr2.Mandatory = $false
                        $attr2.ParameterSetName = "__AllParameterSets"
                        $VRattr = [System.Management.Automation.ValidateRangeAttribute]::new(5,9)

                        $ac2 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac2.Add($attr2)
                        $ac2.Add($VRattr)
                        $p2 = [System.Management.Automation.RuntimeDefinedParameter]::New("TwoInt",[int],$ac2)
                        $dynamicParamDictionary.Add("TwoInt",$p2)

                        $attr3 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr3.Mandatory = $false
                        $attr3.ParameterSetName = "__AllParameterSets"
                        $ac3 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac3.Add($attr3)
                        $p3 = [System.Management.Automation.RuntimeDefinedParameter]::new("ThreeBool",[bool],$ac3)
                        $dynamicParamDictionary.Add("ThreeBool",$p3)
                        break
                    }
                    "returnduplicateparameter" {
                        $attr1 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr1.Mandatory = $false
                        $attr1.ParameterSetName = "__AllParameterSets"
                        $ac1 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac1.Add($attr1)
                        $p1 = [System.Management.Automation.RuntimeDefinedParameter]::new("TestToRun",[int],$ac1)
                        $dynamicParamDictionary.Add("TestToRun",$p1)
                        break
                    }
                    "returngenericparameter" {
                        $attr1 = [System.Management.Automation.ParameterAttribute]::new()
                        $attr1.ParameterSetName = "__AllParameterSets"
                        $ac1 = [Collections.ObjectModel.Collection[Attribute]]::new()
                        $ac1.Add($attr1)
                        $p1 = [System.Management.Automation.RuntimeDefinedParameter]::new("TypedValue",$ParameterType,$ac1)                       
                        $dynamicParamDictionary.Add("TypedValue",$p1)              
                        break 
                    }
                    default { 
                        throw ([invalidoperationexception]::new("unable to determine which dynamic parameters to return!"))
                        break
                    }
                }
                return $dynamicParamDictionary
            }

            BEGIN {
                $ReturnNull = "ReturnNull"
                $ReturnThis = "ReturnThis"
                $ReturnAlias = "ReturnAlias"
                $ReturnDuplicateAlias = "ReturnDuplicateAlias"
                $Return1 = "Return1"
                $Return2 = "Return2"
                $Return3 = "Return3"
                $ReturnDuplicateParameter = "ReturnDuplicateParameter"
                $ReturnObjectNoParameters = "ReturnObjectNoParameters"
                $ReturnGenericParameter = "ReturnGenericParameter"
                $ThrowException = "ThrowException"
                return $dynamicParamDictionary
            }
        }       
        
        function GetDynamicParameter($cmdlet, $parameterName)
        {
            foreach ($paramSet in $cmdlet.ParameterSets)
            {
                foreach ($pinfo in $paramSet.Parameters)
                {
                    if ($pinfo.Name -eq $parameterName)
                    {
                        $foundParam = $pinfo
                        break
                    }
                }
                if($foundParam -ne $null)
                {
                    break
                }
            }
            return $foundParam
        }

        function VerifyDynamicParametersExist($cmdlet, $parameterNames)
        {
            foreach($paramName in $parameterNames)
            {
                $foundParam = GetDynamicParameter -cmdlet $cmdlet -parameterName $paramName
                $foundParam.Name | Should Be $paramName            
            } 
        }

        function VerifyParameterType($cmdlet, $parameterName, $ParameterType)
        {
            $foundParam = GetDynamicParameter -cmdlet $cmdlet -parameterName $parameterName
            $foundParam.ParameterType | Should Be $ParameterType                    
        }                              
    }

    It "Verify that get-command get-content includes the dynamic parameters when the cmdlet is checked against the file system provider implementation" {
        $fullPath = Join-Path $TestDrive -ChildPath "blah"
        New-Item -Path $fullPath -ItemType directory -Force
        $results = get-command get-content -path $fullPath
        $dynamicParameter = "Wait", "Encoding", "Delimiter"
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $dynamicParameter      
    }

    It "Verify that get-command get-content doesn't have any dynamic parameters for Function provider" {
        $results =get-command get-content -path function:
        $dynamicParameter = "Wait", "Encoding", "Delimiter"
        foreach ($dynamicPara in $dynamicParameter)
        {
            $results[0].ParameterSets.Parameters.Name -contains $dynamicPara | Should be $false
        }
    }
    
    It "Verify that the specified dynamic parameter exists in the CmdletInfo result returned" {
        $results = get-command testgetcommand-dynamicparametersdcr -testtorun return1       
        $dynamicParameter = "OneString"
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $dynamicParameter
        VerifyParameterType -cmdlet $results[0] -parameterName $dynamicParameter -ParameterType string
    } 
    
    It "Verify three dynamic parameters are created properly" {
        $results = get-command testgetcommand-dynamicparametersdcr -testtorun return3
        $dynamicParameter = "OneString", "TwoInt", "ThreeBool"
        
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $dynamicParameter
        VerifyParameterType -cmdlet $results[0] -parameterName "OneString" -parameterType string
        VerifyParameterType -cmdlet $results[0] -parameterName "TwoInt" -parameterType Int
        VerifyParameterType -cmdlet $results[0] -parameterName "ThreeBool" -parameterType bool      
    }  
    
    It "Verify dynamic parameter type is process" {
        $results = get-command testgetcommand-dynamicparametersdcr -args '-testtorun','returngenericparameter','-parametertype','System.Diagnostics.Process'      
        VerifyParameterType -cmdlet $results[0] -parameterName "TypedValue" -parameterType System.Diagnostics.Process
    }
    
    It "Verify a single cmdlet returned using verb and noun parameter set syntax works properly" {
        $paramName = "OneString"
        $results = get-command -verb testgetcommand -noun dynamicparametersdcr -testtorun Return1
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $paramName
        VerifyParameterType -cmdlet $results[0] -parameterName $paramName -parameterType string
    }
    
    It "Verify Single Cmdlet Using Verb&Noun ParameterSet" {
        $paramName = "Encoding"
        $results = get-command -verb get -noun content -Encoding Unicode
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $paramName
        VerifyParameterType -cmdlet $results[0] -parameterName $paramName -parameterType Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding
    }  
    
    It "Verify Single Cmdlet Using Verb&Noun ParameterSet With Usage" {
        $results =  get-command -verb get -noun content -Encoding Unicode -syntax
        $results.ToString() | Should Match "-Encoding"
        $results.ToString() | Should Match "-Wait"
        $results.ToString() | Should Match "-Delimiter"
    }
    

    It "Test Script Lookup Positive Script Info" {
        $tempFile = "mytempfile.ps1"
        $fullPath = Join-Path $TestDrive -ChildPath $tempFile
        "$a = dir" > $fullPath
        $results = get-command $fullPath
            
        $results.Name | Should Be $tempFile
        $results.Definition | Should Be $fullPath  
    }

    It "Two dynamic parameters are created properly" {
        $results = get-command testgetcommand-dynamicparametersdcr -testtorun return2
        $dynamicParameter = "OneString", "TwoInt"
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $dynamicParameter
        VerifyParameterType -cmdlet $results[0] -parameterName "OneString" -ParameterType string
        VerifyParameterType -cmdlet $results[0] -parameterName "TwoInt" -ParameterType int
    }

    It "Throw an Exception when set testtorun to 'returnduplicateparameter'" { 
        try
        {       
            Get-Command testgetcommand-dynamicparametersdcr -testtorun returnduplicateparameter -ErrorAction SilentlyContinue
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "GetCommandMetadataError,Microsoft.PowerShell.Commands.GetCommandCommand"
        }          
    }

    It "verify if get the proper dynamic parameter type skipped by issue #1430" -Pending {
        $results = Get-Command testgetcommand-dynamicparametersdcr -testtorun returngenericparameter -parametertype System.Diagnostics.Process
        VerifyParameterType -cmdlet $results[0] -parameterName "TypedValue" -parameterType System.Diagnostics.Process
    }

    It "It works with Single Cmdlet Using Verb&Noun ParameterSet" {
        $paramName = "Encoding"
        $results = Get-Command -verb get -noun content -encoding UTF8
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $paramName
        VerifyParameterType -cmdlet $results[0] -parameterName $paramName -ParameterType Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding
    }

    #unsupported parameter: -synop
    It "[Unsupported]It works with Single Cmdlet Using Verb&Noun ParameterSet With Synopsis" -Pending {
        $paramName = "Encoding"
        $results = get-command -verb get -noun content -encoding UTF8 -synop
        VerifyDynamicParametersExist -cmdlet $results[0] -parameterNames $paramName
        VerifyParameterType -cmdlet $results[0] -parameterName $paramName -ParameterType Microsoft.PowerShell.Commands.FileSystemCmdletProviderEncoding
    }            
}