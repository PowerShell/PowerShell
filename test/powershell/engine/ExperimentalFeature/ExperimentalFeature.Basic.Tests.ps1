# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Experimental Feature Basic Tests - Feature-Disabled" -tags "CI" {

    BeforeAll {
        $skipTest = $EnabledExperimentalFeatures.Contains('ExpTest.FeatureOne')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'ExpTest.FeatureOne' to be disabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            $TestModule = Join-Path $PSScriptRoot "assets" "ExpTest"
            $AssemblyPath = Join-Path $TestModule "ExpTest.dll"
            if (-not (Test-Path $AssemblyPath)) {
                $SourcePath = Join-Path $TestModule "ExpTest.cs"
                Add-Type -Path $SourcePath -OutputType Library -OutputAssembly $AssemblyPath
            }
            $moduleInfo = Import-Module $TestModule -PassThru
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        } else {
            Remove-Module -ModuleInfo $moduleInfo -Force -ErrorAction SilentlyContinue
        }
    }

    It "No experimental features is enabled" {
        $EnabledExperimentalFeatures.Count | Should -Be 0
    }

    It "Replace existing command <Name> - version one should be shown" -TestCases @(
        @{ Name = "Invoke-AzureFunction"; CommandType = "Function" }
        @{ Name = "Invoke-AzureFunctionCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        $command.Source | Should -BeExactly $moduleInfo.Name
        & $Name -Token "Token" -Command "Command" | Should -BeExactly "Invoke-AzureFunction Version ONE"

        if ($CommandType -eq "Function") {
            $expectedErrorId = "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
            { Get-Command "Invoke-AzureFunctionV2" -ErrorAction Stop } | Should -Throw -ErrorId $expectedErrorId
            { & $moduleInfo { Get-Command "Invoke-AzureFunctionV2" -ErrorAction Stop } } | Should -Throw -ErrorId $expectedErrorId
        }
    }

    It "Experimental parameter set - '<Name>' should NOT have '-SwitchOne' and '-SwitchTwo'" -TestCases @(
        @{ Name = "Get-GreetingMessage"; CommandType = "Function" }
        @{ Name = "Get-GreetingMessageCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-Name'
        $command.Parameters.Count | Should -Be 12
        & $Name -Name Joe | Should -BeExactly "Hello World Joe."
    }

    It "Experimental parameter set - '<Name>' should NOT have 'WebSocket' parameter set" -TestCases @(
        @{ Name = "Invoke-MyCommand"; CommandType = "Function" }
        @{ Name = "Invoke-MyCommandCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType

        ## 11 common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.Parameters.Count | Should -Be 18
        $command.ParameterSets.Count | Should -Be 2

        $command.Parameters["UserName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["UserName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true

        $command.Parameters["ComputerName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ComputerName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true

        $command.Parameters["ConfigurationName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true

        $command.Parameters["VMName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["VMName"].ParameterSets.ContainsKey("VMSet") | Should -Be $true

        $command.Parameters["Port"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Port"].ParameterSets.ContainsKey("VMSet") | Should -Be $true

        $command.Parameters["ThrottleLimit"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ThrottleLimit"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        $command.Parameters["Command"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Command"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        ## 11 common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[0].Name | Should -BeExactly "ComputerSet"
        $command.ParameterSets[0].Parameters.Count | Should -Be 16

        ## 11 common parameters + '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[1].Name | Should -BeExactly "VMSet"
        $command.ParameterSets[1].Parameters.Count | Should -Be 15

        & $Name -UserName "user" -ComputerName "localhost" -ConfigurationName "config" | Should -BeExactly "Invoke-MyCommand with ComputerSet"
        & $Name -VMName "VM" -Port "80" | Should -BeExactly "Invoke-MyCommand with VMSet"
    }

    It "Experimental parameter set - '<Name>' should have '-SessionName' only" -TestCases @(
        @{ Name = "Test-MyRemoting"; CommandType = "Function" }
        @{ Name = "Test-MyRemotingCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-SessionName'
        $command.Parameters.Count | Should -Be 12
        $command.Parameters["SessionName"].ParameterType.FullName | Should -BeExactly "System.String"
        $command.Parameters.ContainsKey("ComputerName") | Should -Be $false
    }

    It "Use 'Experimental' attribute directly on parameters - '<Name>'" -TestCases @(
        @{ Name = "Save-MyFile"; CommandType = "Function" }
        @{ Name = "Save-MyFileCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-ByUrl', '-ByRadio', '-FileName', '-Configuration'
        $command.Parameters.Count | Should -Be 15
        $command.ParameterSets.Count | Should -Be 2
        
        $command.Parameters["ByUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByUrl"].ParameterSets.ContainsKey("UrlSet") | Should -Be $true

        $command.Parameters["ByRadio"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByRadio"].ParameterSets.ContainsKey("RadioSet") | Should -Be $true

        $command.Parameters["Configuration"].ParameterSets.Count | Should -Be 2
        $command.Parameters["Configuration"].ParameterSets.ContainsKey("UrlSet") | Should -Be $true
        $command.Parameters["Configuration"].ParameterSets.ContainsKey("RadioSet") | Should -Be $true

        $command.Parameters["FileName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["FileName"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        $command.Parameters.ContainsKey("Destination") | Should -Be $false
    }

    It "Dynamic parameters - <CommandType>-<Name>" -TestCases @(
        @{ Name = "Test-MyDynamicParamOne"; CommandType = "Function" }
        @{ Name = "Test-MyDynamicParamOneCSharp"; CommandType = "Cmdlet" }
        @{ Name = "Test-MyDynamicParamTwo"; CommandType = "Function" }
        @{ Name = "Test-MyDynamicParamTwoCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-Name' (dynamic parameters are not triggered)
        $command.Parameters.Count | Should -Be 12
        $command.Parameters["Name"] | Should -Not -BeNullOrEmpty

        $command = Get-Command $Name -ArgumentList "Joe"
        ## 11 common parameters + '-Name' and '-ConfigName' (dynamic parameters are triggered)
        $command.Parameters.Count | Should -Be 13
        $command.Parameters["ConfigName"].Attributes.Count | Should -Be 2
        $command.Parameters["ConfigName"].Attributes[0] | Should -BeOfType [parameter]
        $command.Parameters["ConfigName"].Attributes[1] | Should -BeOfType [ValidateNotNullOrEmpty]

        $command.Parameters.ContainsKey("ConfigFile") | Should -Be $false
    }
}

Describe "Experimental Feature Basic Tests - Feature-Enabled" -Tag "CI" {

    BeforeAll {
        $skipTest = -not $EnabledExperimentalFeatures.Contains('ExpTest.FeatureOne')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'ExpTest.FeatureOne' to be enabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            $TestModule = Join-Path $PSScriptRoot "assets" "ExpTest"
            $AssemblyPath = Join-Path $TestModule "ExpTest.dll"
            if (-not (Test-Path $AssemblyPath)) {
                $SourcePath = Join-Path $TestModule "ExpTest.cs"
                Add-Type -Path $SourcePath -OutputType Library -OutputAssembly $AssemblyPath
            }
            $moduleInfo = Import-Module $TestModule -PassThru
        }
    }

    AfterAll {
        if ($skipTest) {
            $global:PSDefaultParameterValues = $originalDefaultParameterValues
        } else {
            Remove-Module -ModuleInfo $moduleInfo -Force -ErrorAction SilentlyContinue
        }
    }

    It "Experimental feature 'ExpTest.FeatureOne' should be enabled" {
        $EnabledExperimentalFeatures.Count | Should -Be 1
        $EnabledExperimentalFeatures -contains "ExpTest.FeatureOne" | Should -Be $true
    }

    It "Replace existing command <Name> - version two should be shown" -TestCases @(
        @{ Name = "Invoke-AzureFunction"; CommandType = "Alias" }
        @{ Name = "Invoke-AzureFunctionCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        $command.Source | Should -BeExactly $moduleInfo.Name
        & $Name -Token "Token" -Command "Command" | Should -BeExactly "Invoke-AzureFunction Version TWO"

        if ($CommandType -eq "Alias") {
            $command.Definition | Should -Be "Invoke-AzureFunctionV2"
            $expectedErrorId = "CommandNotFoundException,Microsoft.PowerShell.Commands.GetCommandCommand"
            { Get-Command "Invoke-AzureFunction" -CommandType Function -ErrorAction Stop } | Should -Throw -ErrorId $expectedErrorId
            { & $moduleInfo { Get-Command "Invoke-AzureFunction" -CommandType Function -ErrorAction Stop } } | Should -Throw -ErrorId $expectedErrorId
        }
    }

    It "Experimental parameter set - '<Name>' should have '-SwitchOne' and '-SwitchTwo'" -TestCases @(
        @{ Name = "Get-GreetingMessage"; CommandType = "Function" }
        @{ Name = "Get-GreetingMessageCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-Name' + '-SwitchOne' + '-SwitchTwo'
        $command.Parameters.Count | Should -Be 14
        $command.ParameterSets.Count | Should -Be 3

        & $Name -Name Joe | Should -BeExactly "Hello World Joe."
        & $Name -Name Joe -SwitchOne | Should -BeExactly "Hello World Joe.-SwitchOne is on."
        & $Name -Name Joe -SwitchTwo | Should -BeExactly "Hello World Joe.-SwitchTwo is on."
    }

    It "Experimental parameter set - '<Name>' should have 'WebSocket' parameter set" -TestCases @(
        @{ Name = "Invoke-MyCommand"; CommandType = "Function" }
        @{ Name = "Invoke-MyCommandCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType

        ## 11 common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-VMName', '-Port',
        ## '-Token', '-WebSocketUrl', '-ThrottleLimit' and '-Command'
        $command.Parameters.Count | Should -Be 20
        $command.ParameterSets.Count | Should -Be 3

        $command.Parameters["UserName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["UserName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true

        $command.Parameters["ComputerName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ComputerName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true

        $command.Parameters["VMName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["VMName"].ParameterSets.ContainsKey("VMSet") | Should -Be $true

        $command.Parameters["Token"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Token"].ParameterSets.ContainsKey("WebSocketSet") | Should -Be $true

        $command.Parameters["WebSocketUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["WebSocketUrl"].ParameterSets.ContainsKey("WebSocketSet") | Should -Be $true

        $command.Parameters["ConfigurationName"].ParameterSets.Count | Should -Be 2
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("ComputerSet") | Should -Be $true
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("WebSocketSet") | Should -Be $true

        $command.Parameters["Port"].ParameterSets.Count | Should -Be 2
        $command.Parameters["Port"].ParameterSets.ContainsKey("VMSet") | Should -Be $true
        $command.Parameters["Port"].ParameterSets.ContainsKey("WebSocketSet") | Should -Be $true

        $command.Parameters["ThrottleLimit"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ThrottleLimit"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        $command.Parameters["Command"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Command"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        ## 11 common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[0].Name | Should -BeExactly "ComputerSet"
        $command.ParameterSets[0].Parameters.Count | Should -Be 16

        ## 11 common parameters + '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[1].Name | Should -BeExactly "VMSet"
        $command.ParameterSets[1].Parameters.Count | Should -Be 15

        ## 11 common parameters + '-Token', '-WebSocketUrl', '-ConfigurationName', '-Port', '-ThrottleLimit', '-Command'
        $command.ParameterSets[2].Name | Should -BeExactly "WebSocketSet"
        $command.ParameterSets[2].Parameters.Count | Should -Be 17

        & $Name -UserName "user" -ComputerName "localhost" | Should -BeExactly "Invoke-MyCommand with ComputerSet"
        & $Name -UserName "user" -ComputerName "localhost" -ConfigurationName "config" | Should -BeExactly "Invoke-MyCommand with ComputerSet"

        & $Name -VMName "VM" | Should -BeExactly "Invoke-MyCommand with VMSet"
        & $Name -VMName "VM" -Port "80" | Should -BeExactly "Invoke-MyCommand with VMSet"

        & $Name -Token "token" -WebSocketUrl 'url' | Should -BeExactly "Invoke-MyCommand with WebSocketSet"
        & $Name -Token "token" -WebSocketUrl 'url' -ConfigurationName 'config' -Port 80 | Should -BeExactly "Invoke-MyCommand with WebSocketSet"
    }

    It "Experimental parameter set - '<Name>' should have '-ComputerName' only" -TestCases @(
        @{ Name = "Test-MyRemoting"; CommandType = "Function" }
        @{ Name = "Test-MyRemotingCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-ComputerName'
        $command.Parameters.Count | Should -Be 12
        $command.Parameters["ComputerName"].ParameterType.FullName | Should -BeExactly "System.String"
        $command.Parameters.ContainsKey("SessionName") | Should -Be $false
    }

    It "Use 'Experimental' attribute directly on parameters - '<Name>'" -TestCases @(
        @{ Name = "Save-MyFile"; CommandType = "Function" }
        @{ Name = "Save-MyFileCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-ByUrl', '-ByRadio', '-FileName', '-Destination'
        $command.Parameters.Count | Should -Be 15
        $command.ParameterSets.Count | Should -Be 2
        
        $command.Parameters["ByUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByUrl"].ParameterSets.ContainsKey("UrlSet") | Should -Be $true

        $command.Parameters["ByRadio"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByRadio"].ParameterSets.ContainsKey("RadioSet") | Should -Be $true

        $command.Parameters["Destination"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Destination"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        $command.Parameters["FileName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["FileName"].ParameterSets.ContainsKey("__AllParameterSets") | Should -Be $true

        $command.Parameters.ContainsKey("Configuration") | Should -Be $false
    }

    It "Dynamic parameters - <CommandType>-<Name>" -TestCases @(
        @{ Name = "Test-MyDynamicParamOne"; CommandType = "Function" }
        @{ Name = "Test-MyDynamicParamOneCSharp"; CommandType = "Cmdlet" }
        @{ Name = "Test-MyDynamicParamTwo"; CommandType = "Function" }
        @{ Name = "Test-MyDynamicParamTwoCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)

        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## 11 common parameters + '-Name' (dynamic parameters are not triggered)
        $command.Parameters.Count | Should -Be 12
        $command.Parameters["Name"] | Should -Not -BeNullOrEmpty

        $command = Get-Command $Name -ArgumentList "Joe"
        ## 11 common parameters + '-Name' and '-ConfigFile' (dynamic parameters are triggered)
        $command.Parameters.Count | Should -Be 13
        $command.Parameters["ConfigFile"].Attributes.Count | Should -Be 2
        $command.Parameters["ConfigFile"].Attributes[0] | Should -BeOfType [parameter]
        $command.Parameters["ConfigFile"].Attributes[1] | Should -BeOfType [ValidateNotNullOrEmpty]

        $command.Parameters.ContainsKey("ConfigName") | Should -Be $false
    }
}
