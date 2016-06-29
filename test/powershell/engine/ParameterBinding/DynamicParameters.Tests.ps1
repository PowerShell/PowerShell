Describe "CommandInfo tests for dynamic parameters" -tags "P1", "RI" {

    BeforeAll {
		Push-Location .	
	
        function SimpleDynamicParamter1
        {
            $dynParamAttribute = [System.Management.Automation.ParameterAttribute]::new()
            $dynParamAttribute.Mandatory = $true 
            $attributeCollection = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
 
            $attributeCollection.Add($dynParamAttribute) 
            $dynParam = [System.Management.Automation.RuntimeDefinedParameter]::new('Param1', [string], $attributeCollection) 

            return $dynParam
        }

        function SimpleDynamicParamter2
        {
            $dynParamAttribute = [System.Management.Automation.ParameterAttribute]::new()
            $dynParamAttribute.Mandatory = $true 
            $attributeCollection = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
 
            $attributeCollection.Add($dynParamAttribute) 
            $dynParam = [System.Management.Automation.RuntimeDefinedParameter]::new('Param2', [string], $attributeCollection) 
            $dynParam.Value = 0
            
            return $dynParam
        }

        function simpledynamicparameter-discoverytest
        {
            [CmdletBinding()]            
            Param(
            )
            DynamicParam {
                $dynParam = SimpleDynamicParamter1            
                $paramDictionary =  [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                $paramDictionary.Add('Param1', $dynParam)
                return $paramDictionary           
            }
                    
            Begin
            {                
                Write-Output $dynParam
            }
        }

        function complexdynamicparameter-discoverytest
        {
            [CmdletBinding()]
            Param(
                [Parameter(Mandatory=$true, Position = 0)]
                [ValidateSet("SimpleDynamicParameter1", "SimpleDynamicParameter2")]
                [string] $WhichParams
            )

            DynamicParam {
                switch ($WhichParams)
                {
                    'SimpleDynamicParameter1' {
                            $dynParam = SimpleDynamicParamter1            
                            $paramDictionary =  [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                            $paramDictionary.Add('Param1', $dynParam)    
                            return $paramDictionary
                        }
                    'SimpleDynamicParameter2' {
                            $dynParam = SimpleDynamicParamter2
                            $paramDictionary =  [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                            $paramDictionary.Add('Param2', $dynParam)
                            return $paramDictionary
                        }
                }              
            }

            Begin
            {
                return $dynParam
            }
        }

        <# The intention of this function is to have a -Name dynamic parameter to conflict with Get-Command -Name. #>
        function conflictingdynamicparameter-discoverytest
        {
            [CmdletBinding()]            
            Param()

            DynamicParam {
                $dynParamAttribute = [System.Management.Automation.ParameterAttribute]::new()
                $dynParamAttribute.Mandatory = $true 
                $attributeCollection = [System.Collections.ObjectModel.Collection[System.Attribute]]::new()
 
                $attributeCollection.Add($dynParamAttribute) 
                $dynParam = [System.Management.Automation.RuntimeDefinedParameter]::new('Name', [string], $attributeCollection) 
                $paramDictionary =  [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                $paramDictionary.Add('Name', $dynParam)    
                return $paramDictionary
            }


            Begin
            {
                return $dynParam
            }
        }
    }

    AfterAll {
        Remove-Item function:SimpleDynamicParamter1 -Force -ErrorAction SilentlyContinue
        Remove-Item function:SimpleDynamicParamter2 -Force -ErrorAction SilentlyContinue
        Remove-Item function:simpledynamicparameter-discoverytest -Force -ErrorAction SilentlyContinue
        Remove-Item function:complexdynamicparameter-discoverytest -Force -ErrorAction SilentlyContinue    
        
        Pop-Location    
    }

    Context "GetDynamicParameters with Implicit named parameter" {
        
        $testData = @(        
            @{command = (Get-Command complexdynamicparameter-discoverytest -whichparams 'SimpleDynamicParameter1'); name = 'SimpleDynamicParameter1-Fullname'; paramName = 'Param1'}
            @{command = (Get-Command -Verb complexdynamicparameter -Noun discoverytest -whichparams 'SimpleDynamicParameter1'); name = 'SimpleDynamicParameter1-VerbNoun'; paramName = 'Param1' }
            @{command = (Get-Command complexdynamicparameter-discoverytest -whichparams 'SimpleDynamicParameter2'); name = 'SimpleDynamicParameter2-Fullname'; paramName = 'Param2'}
            @{command = (Get-Command -Verb complexdynamicparameter -Noun discoverytest -whichparams 'SimpleDynamicParameter2'); name = 'SimpleDynamicParameter2-VerbNoun'; paramName = 'Param2' }
        )
        
        It "GetDynamicParameters with implicit named parameter - <name>" -TestCases $testData {
            param($command, $paramName)            
        
            $command.GetType() | Should Be 'System.Management.Automation.FunctionInfo'

            $dynParam1 = $command.ResolveParameter('WhichParams') 
            $dynParam1.Name | Should Be 'WhichParams'
            #($dynParam1.Attributes | ? { $_.TypeId.Name -match 'ParameterAttribute' }).Mandatory | Should Be $true
            ($dynParam1.Attributes | ? { $_ -is 'System.Management.Automation.ParameterAttribute' }).Mandatory | Should Be $true
            $dynParam1.IsDynamic | Should be $false

            $dynParam1 = $command.ResolveParameter($paramName) 
            $dynParam1.Name | Should Be $paramName
            #($dynParam1.Attributes | ? { $_.TypeId.Name -match 'ParameterAttribute' }).Mandatory | Should Be $true
            ($dynParam1.Attributes | ? { $_ -is 'System.Management.Automation.ParameterAttribute' }).Mandatory | Should Be $true
            $dynParam1.IsDynamic | Should be $true
        }

        $testData = @(
            @{command = @(get-command *-discoverytest -param1 'foo'); name = 'argument' }
            @{command = @(get-command -noun discoverytest -param1 'foo'); name = 'noun' }
            @{command = @(get-command dir -path 'c:\foo'); name = 'non cmdlet positional' }
            @{command = @(get-command dir -ArgumentList '-path','c:\foo'); name = 'non cmdlet named' }
            @{command = @(set-location function:;get-command move-item); name = 'multiple' }
        )        

        It "GetDynamicParameters with multiple commands found - <name>" -TestCases $testData {
            param($command)            
            $command.Count | Should Be 1
        }
    }
    
    Context "GetDynamicParameters for provider specific parameters" {

        class ParameterData 
        {
            [string] $name
            [bool] $isMandatory
            [bool] $isDynamic

            ParameterData([string] $name, [bool] $isMandatory, [bool] $isDynamic)
            {
                $this.name = $name
                $this.isMandatory = $isMandatory
                $this.isDynamic = $isDynamic
            }
        }
        
        $parameterData = @(
            [ParameterData]::new('Path', $false, $false),
            [ParameterData]::new('Filter', $false, $false),
            [ParameterData]::new('Include', $false, $false),
            [ParameterData]::new('Exclude', $false, $false),
            [ParameterData]::new('Recurse', $false, $false),
            [ParameterData]::new('Depth', $false, $false),
            [ParameterData]::new('Force', $false, $false),
            [ParameterData]::new('Name', $false, $false),
            [ParameterData]::new('UseTransaction', $false, $false),
            [ParameterData]::new('CodeSigningCert', $false, $true),
            [ParameterData]::new('DocumentEncryptionCert', $false, $true),
            [ParameterData]::new('SSLServerAuthentication', $false, $true),
            [ParameterData]::new('DnsName', $false, $true),
            [ParameterData]::new('Eku', $false, $true),
            [ParameterData]::new('ExpiringInDays', $false, $true),
            [ParameterData]::new('LiteralPath', $true, $false)
        )
                
        $testData = @(
            @{command = (Get-Command Get-ChildItem cert: -recurse); name = 'Implicit'; params = $parameterData}
            @{command = (Get-Command -Verb Get -Noun ChildItem cert: -recurse);name = 'Implicit-VerbNoun'; params = $parameterData}                                           
            @{command = (Get-Command Get-ChildItem -path:cert:); name = 'Implicit-VerbNoun'; params = $parameterData}
            @{command = (Get-Command -Verb Get -Noun ChildItem -path:cert: );name = 'ImplicitCoupled-VerbNoun'; params = $parameterData}
            @{command = (Get-Command Get-ChildItem -ArgumentList '-path','cert:', '-recurse'); name = 'ArgsExplicit'; params = $parameterData}
            @{command = (Get-Command -Verb Get -Noun ChildItem -ArgumentList '-path','cert:', '-recurse');name = 'ArgsExplicit-VerbNoun'; params = $parameterData}                                           
        )       

        It "GetDynamicParameters for provider specific parameters <name>" -TestCases $testData {
            param($command, $params)

            $command.GetType() | Should Be 'System.Management.Automation.CmdletInfo'

            foreach($param in $params)
            {
                $dynParam = $command.ResolveParameter($param.Name)
                $dynParam.Name | Should Be $param.Name
                ($dynParam.Attributes | ? { $_.TypeId.Name -match 'ParameterAttribute' }).Mandatory | Should Be $param.IsMandatory
                $dynParam.IsDynamic | Should be $param.IsDynamic
            }
        }
    }

    Context "GetDynamicParameters error cases" {

        BeforeAll {

            function duplicatedynamicparameter-discoverytest
            {
                [CmdletBinding()]
                Param(
                    [Parameter(Mandatory=$true, Position = 0)]
                    [string] $Param1
                )

                DynamicParam {
                    $dynParam = SimpleDynamicParamter1            
                    $paramDictionary =  [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                    $paramDictionary.Add('Param1', $dynParam)
                    return $paramDictionary           
                }
                    
                Begin
                {                
                    Write-Output $dynParam
                }
            }
        }

        AfterAll {
            Remove-Item function:duplicatedynamicparameter-discoverytest -Force -ErrorAction SilentlyContinue
        }

        It "GetDynamicParameters conflicting parameter names fails with ParameterBindingException" {

            try
            {
                $null = get-command -Name conflictingdynamicparameter-discoverytest -Name "SimpleDynamicParameter1"                
                throw 'OK'
            }
            catch 
            {
                $_.Exception.GetType().Name | Should Be 'ParameterBindingException'
            }
        }

        It "GetDynamicParameters duplicate parameter names fails with MethodInvocationException" {

            try
            {
                $null = (get-command duplicatedynamicparameter-discoverytest).Get_Parameters()
                throw 'OK'
            }
            catch 
            {
                $_.Exception.GetType().Name | Should Be 'MethodInvocationException'
            }
        }
    }

    Context "GetDynamicParameters totalcount tests" {
        
        $testData = @(
            @{command = @(get-command -noun object -totalcount 1); name = 'foreach'; expectedName = 'ForEach-Object'; expectedModule = 'Microsoft.PowerShell.Core' }
            @{command = @(get-command *-object -totalcount 1); name = 'foreach-WildCard'; expectedName = 'ForEach-Object'; expectedModule = 'Microsoft.PowerShell.Core' }
            @{command = @(get-command -verb get -noun command -totalcount 1); name = 'GetCommandNounVerb'; expectedName = 'Get-Command'; expectedModule = 'Microsoft.PowerShell.Core' }
            @{command = @(get-command get-command -totalcount 1); name = 'GetCommandPostional'; expectedName = 'Get-Command'; expectedModule = 'Microsoft.PowerShell.Core' }
        )

        It "Totalcount tests <name>" -TestCases $testData {
            param($command, $expectedName, $expectedModule)

            $command.count | Should Be 1
            $command.Name | Should Be $expectedName
            $command.Source | Should Be $expectedModule
        }
    }
}