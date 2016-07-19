using namespace System.Management.Automation

Describe "Default Parameters" -tags P1 {

    <#
    Purpose:
        Validate the API functionality of the DefaultParameterDictionary type.
        
    Action:
        Call the public APIs of the type with valid and invalid parameter values. Then verify if the result is expected.
        
    Expected Result: 
        All result should be the same as expected. No error should be thrown out.
    #>
    Context "API functionality tests" {
        
        It '001 - Successful assignment' {
            [DefaultParameterDictionary]$PSDefaultParameterValues = @{ "Command:Parameter" = "Value" }
            $PSDefaultParameterValues.GetType().FullName | Should Be "System.Management.Automation.DefaultParameterDictionary"
            $PSDefaultParameterValues.Count | Should Be 1
            # Case insensitive
            $PSDefaultParameterValues["COmmanD:pARAmeter"] | Should Be "Value"
        }


        It '002 - Assignment with invalid string key format' {
            { [DefaultParameterDictionary]$PSDefaultParameterValues = @{ "Command:Parameter" = "Value"; "InvalidKey" = "Value" } } | Should Throw InvalidKey
            { [DefaultParameterDictionary]$PSDefaultParameterValues = @{ "Command:Parameter" = "Value"; ([IO.FileInfo]"InvalidKey") = "Value" } } | Should Throw InvalidKey
        }

        It '003 - Contains(object key)' {
            { $PSDefaultParameterValues.Contains($null) } | Should Throw key
            $PSDefaultParameterValues.Contains("NotExisting") | Should Be $false
            $PSDefaultParameterValues.Contains([datetime]) | Should Be $false
        }

        It '004 - Add - invalid keys' {
            [DefaultParameterDictionary]$PSDefaultParameterValues = @{}
            { $PSDefaultParameterValues.Add($null, "Value") } | Should Throw key
            { $PSDefaultParameterValues.Add("InvalidKey", "Value") } | Should Throw "InvalidKey"
            { $PSDefaultParameterValues.Add([IO.FileInfo]"InvalidKey", "Value") } | Should Throw "System.IO.FileInfo"

            # Add the same effective key twice, second time should throw
            $PSDefaultParameterValues.Add("Command2:Parameter2", "Value") 
            { $PSDefaultParameterValues.Add("     Command2:Parameter2", "Value") } | Should Throw "Command2:Parameter2"
        }

        It '005 - Add - valid keys' {
            [DefaultParameterDictionary]$PSDefaultParameterValues = @{}
            $PSDefaultParameterValues.Add("     Command:Parameter", "Value")
            $PSDefaultParameterValues.Add("Command2:Parameter2", "Value2")
            $PSDefaultParameterValues.Count | Should Be 2
            $PSDefaultParameterValues.Contains(" CommAnd:ParaMeter ") | Should Be $true
            $PSDefaultParameterValues.Contains("CommAnd2:ParaMeter2") | Should Be $true
            $PSDefaultParameterValues[" CommAnd:ParaMeter "] | Should Be "Value"
            $PSDefaultParameterValues["CommAnd2:ParaMeter2"] | Should Be "Value2"
        }

        It '006 - indexer' {
            [DefaultParameterDictionary]$PSDefaultParameterValues = @{}
            $PSDefaultParameterValues.Add("Command2:Parameter2", "Value2")

            $PSDefaultParameterValues["NonExisting"] | Should BeNullOrEmpty
            $PSDefaultParameterValues[[datetime]] | Should BeNullOrEmpty
            $PSDefaultParameterValues["CommAnd2:ParaMeter2  "] | Should Be "Value2"

            $PSDefaultParameterValues["  CommAnd3:ParaMeter3  "] = "Value3"
            $PSDefaultParameterValues[" CommAnd:ParaMeter  "] = "Value1"
            $PSDefaultParameterValues.Count | Should Be 3
            $PSDefaultParameterValues["Command:Parameter"] | Should Be "Value1"
            $PSDefaultParameterValues["  Command3:Parameter3"] | Should Be "Value3"

            { $PSDefaultParameterValues[[IO.FileInfo]"InvalidKey"] = "Value" } | Should Throw System.IO.FileInfo
            $PSDefaultParameterValues.Count | Should Be 3
        }

        It '007 - Remove' {
            [DefaultParameterDictionary]$PSDefaultParameterValues = @{}
            $PSDefaultParameterValues.Add("     Command:Parameter", "Value")
            $PSDefaultParameterValues.Add("Command2:Parameter2", "Value2")
            $PSDefaultParameterValues["  CommAnd3:ParaMeter3  "] = "Value3"

            { $PSDefaultParameterValues.Remove($null) } | Should Throw "key"

            $PSDefaultParameterValues.Remove('NonExisting')
            $PSDefaultParameterValues.Count | Should Be 3
            $PSDefaultParameterValues.Remove([datetime])
            $PSDefaultParameterValues.Count | Should Be 3


            $PSDefaultParameterValues.Contains("Command3:Parameter3") | Should Be $true
            $PSDefaultParameterValues["Command3:Parameter3"] | Should Be "Value3"
            $PSDefaultParameterValues.Remove("Command3:Parameter3   ")
            $PSDefaultParameterValues.Contains("Command3:Parameter3") | Should Be $false
            $PSDefaultParameterValues["Command3:Parameter3"] | Should BeNullOrEmpty
            $PSDefaultParameterValues.Count | Should Be 2
        }

        It '008 - Clear' {
            $PSDefaultParameterValues.Clear()
            $PSDefaultParameterValues.Count | Should Be 0
        }
    }


    <#
    Purpose:
        Tests default parameter infrastructure using a $PSDefaultParameterValues variable of the type "System.Management.Automation.DefaultParameterDictionary".
        
    Action:
        Use differnt key/value pairs for $PSDefaultParameterValues, and then run corresponding cmdlet to verify if the result is expected.
        
    Expected Result: 
        All result should be the same as expected. No error should be thrown out.
    #>
    Context "Parameter binding" {
        ## Note that, the following tests are *almost* the same with the tests in utscript\Engine\TestCmdletParameter.ps1. 
        ## The only difference is that we are using $PSDefaultParameterValues of the type 'System.Management.Automation.DefaultParameterDictionary' 
        ## in the following test cases, while in TestCmdletParameter.ps1, we are using a local Hashtable variable -- $PSDefaultParameterValues.

        [DefaultParameterDictionary]$PSDefaultParameterValues = @{ "Disabled" = $true }

        BeforeEach {
            # Turn off the default parameter binding
            $PSDefaultParameterValues["Disabled"] = $false
        }

        AfterEach {
            # Turn off the default parameter binding
            $PSDefaultParameterValues["Disabled"] = $true
        }

        ##
        ## Helper function - copies default parameters from $defParams into $PSDefaultParameterValues,
        ## runs script $New, and compares results to $expectedArray, making sure they are the same. 
        ##
        function test
        {
            param(
                [hashtable]$defParams,
                [scriptblock]$New,
                [object[]]$expectedArray
            )

            $PSDefaultParameterValues.Clear()
            foreach ($pair in $defParams.GetEnumerator())
            {
                $PSDefaultParameterValues[$pair.Key] = $pair.Value
            }
    
            $error.Clear()

            It "Test - { $($New.ToString().Trim()) }" {
                $actualArray = @(& $New)

                $error.Count | Should Be 0
                $actualArray.Count | Should Be $expectedArray.Count

                for ($i = 0; $i -lt $actualArray.Count; $i++)
                {
                    $actualArray[$i].ToString() | Should Be $expectedArray[$i].ToString()
                }
            }
        }

        # Set up some script blocks and get the expected results
        $gpsSb = { Get-Process } 
        $gpsExpected= @(Get-Process -Id $pid)

        $gmSb = { $host | Get-Member }
        $gmExpected = @($host | Get-Member -force)

        $galSb = { Get-Alias icm,gi,gc }
        $galExpected = @(Get-Alias icm,gi,gc -Exclude gi,gc)

        #################################################################################
        #
        # Test $PSDefaultParameterValues parsing and if the default binding works fine
        #
        #################################################################################
        # Test if default binding works as expected with case-insensitive comparison
        # for the cmdlet name and the parameter name

        test @{ "get-prOCEss:iD" = $pid } $gpsSb $gpsExpected

        #-------------------------------------------------------------------------------
        # Subtests: key format with no wildcards
        #-------------------------------------------------------------------------------
        # "cmdletName":"parameterName" with no whitespaces
        test @{ "`"get-prOCEss`":`"iD`"" = $pid } $gpsSb $gpsExpected

        # "cmdletName":"parameterName" with withspaces
        test @{ "`"  get-prOCEss  `"  :  `"  iD  `" " = $pid } $gpsSb $gpsExpected

        # "cmdletName":parameterName with no whitespaces
        test @{ "`"get-prOCEss`":iD" = $pid } $gpsSb $gpsExpected

        # "cmdletName":parameterName with whitespaces
        test @{ "  `"  get-prOCEss  `"  :  iD  " = $pid } $gpsSb $gpsExpected

        # cmdletName:"parameterName" with no whitespaces
        test @{ "get-prOCEss:`"iD`"" = $pid } $gpsSb $gpsExpected

        # cmdletName:"parameterName" with whitespaces
        test @{ "  get-prOCEss  :  `"  iD  `" " = $pid } $gpsSb $gpsExpected

        # 'cmdletName':'parameterName' with no whitespaces
        test @{ "'get-prOCEss':'iD'" = $pid } $gpsSb $gpsExpected

        # 'cmdletName':'parameterName' with whitespaces
        test @{ "  '  get-prOCEss  '  :  ' iD ' " = $pid } $gpsSb $gpsExpected

        # 'cmdletName':parameterName with no whitespaces
        test @{ "'get-prOCEss':iD" = $pid } $gpsSb $gpsExpected

        # 'cmdletName':parameterName with whitespaces
        test @{ " '  get-prOCEss  '  :  iD  " = $pid } $gpsSb $gpsExpected

        # cmdletName:'parameterName' with no whitespaces
        test @{ "get-prOCEss:'iD'" = $pid } $gpsSb $gpsExpected

        # cmdletName:'parameterName' with whitespaces
        test @{ "  get-prOCEss  :  '  iD  ' " = $pid } $gpsSb $gpsExpected

        # cmdletName:parameterName with no whitespaces
        test @{ "get-prOCEss:iD" = $pid } $gpsSb $gpsExpected

        # cmdletName:parameterName with whitespaces
        test @{ "  get-prOCEss  :  iD  " = $pid } $gpsSb $gpsExpected

        #################################################################################
        # Test if the default binding works as expected with the format "*"."*"
        test @{ "`"get-member`":`"force`"" = $true } $gmSb $gmExpected


        #################################################################################
        # Test if default binding parses the key correctly (any number of whitespaces 
        # before or after the colon)
        test @{ "get-prOCEss   :  `t iD" = $pid } $gpsSb $gpsExpected

        test @{ "`"get-prOCEss `"  :  `t `" iD`"" = $pid } $gpsSb $gpsExpected

        #################################################################################
        # Test the key format "*"."*" and wildcards in both the cmdlet name and the
        # parameter name
        test @{ "`"get-*`":`"fo*e`"" = $true } $gmSb $gmExpected

        test @{ "`"get-* `"  :  `t `" fo*e`"" = $pid } $gmSb $gmExpected

        #################################################################################    
        # Test if the match pattern of the wildcard is case insensitive
        test @{ "`"GEt-*`":`"fO*E`"" = $true } $gmSb $gmExpected

        #--------------------------------------------------------------------------------
        # Subtests: key format with wildcards
        #--------------------------------------------------------------------------------
        # "cmdletName":"parameterName" with no whitespaces
        test @{ "`"GEt-*`":`"fO*E`"" = $true } $gmSb $gmExpected

        # "cmdletName":"parameterName" with whitespaces
        test @{ " `"  GEt-* `"  : `" fO*E `" " = $true } $gmSb $gmExpected

        # "cmdletName":parameterName with no whitespaces
        test @{ "`"GEt-*`":fO*E" = $true } $gmSb $gmExpected

        # "cmdletName":parameterName with whitespaces
        test @{ " `" GEt-*  `"  :  fO*E  " = $true } $gmSb $gmExpected

        # cmdletName:"parameterName" with no whitespaces
        test @{ "GEt-*:`"fO*E`"" = $true } $gmSb $gmExpected

        # cmdletName:"parameterName" with whitespaces
        test @{ " GEt-*  :  `" fO*E `"  " = $true } $gmSb $gmExpected

        # 'cmdletName':'parameterName' with no whitespaces
        test @{ "'GEt-*':'fO*E'" = $true } $gmSb $gmExpected

        # 'cmdletName':'parameterName' with whitespaces
        test @{ " ' GEt-* ' : ' fO*E ' " = $true } $gmSb $gmExpected

        # 'cmdletName':parameterName with no whitespaces
        test @{ "'GEt-*':fO*E" = $true } $gmSb $gmExpected

        # 'cmdletName':parameterName with whitespaces
        test @{ " ' GEt-* ' : fO*E " = $true } $gmSb $gmExpected

        # cmdletName:'parameterName' with no whitespaces
        test @{ "GEt-*:'fO*E'" = $true } $gmSb $gmExpected

        # cmdletName:'parameterName' with whitespaces
        test @{ " GEt-* : ' fO*E ' " = $true } $gmSb $gmExpected

        # cmdletName:parameterName with no whitespaces
        test @{ "GEt-*:fO*E" = $true } $gmSb $gmExpected

        # cmdletName:parameterName with whitespaces
        test @{ " GEt-* :  fO*E  " = $true } $gmSb $gmExpected

        #################################################################################
        # Test if default binding works as expected with array value

        test @{ "get-alias:exclude" = @("gi", "gc") } $galSb $galExpected

        test @{ "  get-alias :  exclude" = @("gi", "gc") } $galSb $galExpected

        test @{ "`"get-alias `" :  `" exclude `" " = @("gi", "gc") } $galSb $galExpected

        test @{ "'get-alias ' :  ' exclude ' " = @("gi", "gc") } $galSb $galExpected

        test @{ "`"get-alias `" :   exclude  " = @("gi", "gc") } $galSb $galExpected

        test @{ " get-alias  :  `" exclude `" " = @("gi", "gc") } $galSb $galExpected

        test @{ "'get-alias ' :   exclude  " = @("gi", "gc") } $galSb $galExpected

        test @{ " get-alias :  ' exclude ' " = @("gi", "gc") } $galSb $galExpected

        #################################################################################
        # Test if default binding works as expected with string value
        $getDateSb = { Get-Date -Date "2016/02/14" }
        $getDateExpected = @(Get-Date -UFormat "%Y / %m / %d / %A" -Date "2016/02/14")
        test @{ "get-date:uformat" = "%Y / %m / %d / %A" } $getDateSb $getDateExpected

        #################################################################################
        # Test if default binding works as expected with ScriptBlock value
        test @{ "invoke-command:scriptblock" = {{hostname}} } { Invoke-Command } @(Invoke-Command {hostname})

        ##################################################################################
        # Test if default binding works as expected with the wildcards in the "*"."*" key format
        test @{ "get-random:mini*" = 10; "`"get-random`":`"maxi*`"" = 11 } { Get-Random } @(Get-Random -Maximum 11 -Minimum 10)
    }

    Context "Module isolation" {
        BeforeAll {
            New-Module -Name "$testDrive - dynamic module" {
                function PSDefaultParamTest {
                    $PSDefaultParameterValues["gps:id"] = $pid
                    gps
                }
            } | Import-Module
        }

        AfterAll {
            Remove-Module "$testDrive - dynamic module"
        }

        #################################################################################
        #
        # Test $PSDefaultParameterValues should be isolated in the module boundary
        #
        #################################################################################

        It "Set PSDefaultParameterValues in module, doesn't change outside module" {
            $result = PSDefaultParamTest
            $expected = Get-Process -Id $pid

            $result.ToString() | Should Be $expected.ToString()
            $PSDefaultParameterValues.Count | Should Be 0
        }
    }
}
