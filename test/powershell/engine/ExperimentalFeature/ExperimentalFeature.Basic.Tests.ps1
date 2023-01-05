# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Experimental Feature Basic Tests - Feature-Disabled" -tags "CI" {

    BeforeAll {
        $skipTest = $EnabledExperimentalFeatures.Contains('ExpTest.FeatureOne')

        if ($skipTest) {
            Write-Verbose "Test Suite Skipped. The test suite requires the experimental feature 'ExpTest.FeatureOne' to be disabled." -Verbose
            $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            ## Common parameters are defined in the type 'CommonParameters' as public properties.
            $CommonParameterCount = [System.Management.Automation.Internal.CommonParameters].GetProperties().Length
            $TestModule = Join-Path $PSScriptRoot "assets" "ExpTest"
            $AssemblyPath = Join-Path $TestModule "ExpTest.dll"
            if (-not (Test-Path $AssemblyPath)) {
                ## When using $SourcePath directly, 'Add-Type' fails in Windows CI runs with an 'access denied' error.
                ## It turns out Pester doesn't handle an exception like this from 'BeforeAll'. It causes the Pester to
                ## be somehow corrupted, and results in random failures in other tests.
                ## To work around this issue, we copy the source file to 'TestDrive' before calling 'Add-Type'.
                $SourcePath = Join-Path $TestModule "ExpTest.cs"
                $SourcePath = (Copy-Item $SourcePath TestDrive:\ -PassThru).FullName
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
        ## Common parameters + '-Name'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 1)
        & $Name -Name Joe | Should -BeExactly "Hello World Joe."
    }

    It "Experimental parameter set - '<Name>' should NOT have 'WebSocket' parameter set" -TestCases @(
        @{ Name = "Invoke-MyCommand"; CommandType = "Function" }
        @{ Name = "Invoke-MyCommandCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType

        ## Common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 7)
        $command.ParameterSets.Count | Should -Be 2

        $command.Parameters["UserName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["UserName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue

        $command.Parameters["ComputerName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ComputerName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue

        $command.Parameters["ConfigurationName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue

        $command.Parameters["VMName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["VMName"].ParameterSets.ContainsKey("VMSet") | Should -BeTrue

        $command.Parameters["Port"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Port"].ParameterSets.ContainsKey("VMSet") | Should -BeTrue

        $command.Parameters["ThrottleLimit"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ThrottleLimit"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        $command.Parameters["Command"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Command"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        ## Common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[0].Name | Should -BeExactly "ComputerSet"
        $command.ParameterSets[0].Parameters.Count | Should -Be ($CommonParameterCount + 5)

        ## Common parameters + '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[1].Name | Should -BeExactly "VMSet"
        $command.ParameterSets[1].Parameters.Count | Should -Be ($CommonParameterCount + 4)

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
        ## Common parameters + '-SessionName'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 1)
        $command.Parameters["SessionName"].ParameterType.FullName | Should -BeExactly "System.String"
        $command.Parameters.ContainsKey("ComputerName") | Should -BeFalse
    }

    It "Use 'Experimental' attribute directly on parameters - '<Name>'" -TestCases @(
        @{ Name = "Save-MyFile"; CommandType = "Function" }
        @{ Name = "Save-MyFileCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## Common parameters + '-ByUrl', '-ByRadio', '-FileName', '-Configuration'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 4)
        $command.ParameterSets.Count | Should -Be 2

        $command.Parameters["ByUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByUrl"].ParameterSets.ContainsKey("UrlSet") | Should -BeTrue

        $command.Parameters["ByRadio"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByRadio"].ParameterSets.ContainsKey("RadioSet") | Should -BeTrue

        $command.Parameters["Configuration"].ParameterSets.Count | Should -Be 2
        $command.Parameters["Configuration"].ParameterSets.ContainsKey("UrlSet") | Should -BeTrue
        $command.Parameters["Configuration"].ParameterSets.ContainsKey("RadioSet") | Should -BeTrue

        $command.Parameters["FileName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["FileName"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        $command.Parameters.ContainsKey("Destination") | Should -BeFalse
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
        ## Common parameters + '-Name' (dynamic parameters are not triggered)
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 1)
        $command.Parameters["Name"] | Should -Not -BeNullOrEmpty

        $command = Get-Command $Name -ArgumentList "Joe"
        ## Common parameters + '-Name' and '-ConfigName' (dynamic parameters are triggered)
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 2)
        $command.Parameters["ConfigName"].Attributes.Count | Should -Be 2
        $command.Parameters["ConfigName"].Attributes[0] | Should -BeOfType parameter
        $command.Parameters["ConfigName"].Attributes[1] | Should -BeOfType ValidateNotNullOrEmpty

        $command.Parameters.ContainsKey("ConfigFile") | Should -BeFalse
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
            ## Common parameters are defined in the type 'CommonParameters' as public properties.
            $CommonParameterCount = [System.Management.Automation.Internal.CommonParameters].GetProperties().Length
            $TestModule = Join-Path $PSScriptRoot "assets" "ExpTest"
            $AssemblyPath = Join-Path $TestModule "ExpTest.dll"
            if (-not (Test-Path $AssemblyPath)) {
                $SourcePath = Join-Path $TestModule "ExpTest.cs"
                $SourcePath = (Copy-Item $SourcePath TestDrive:\ -PassThru).FullName
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
        $EnabledExperimentalFeatures -contains "ExpTest.FeatureOne" | Should -BeTrue
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
        ## Common parameters + '-Name' + '-SwitchOne' + '-SwitchTwo'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 3) -Because ($command.Parameters.Keys -join ", ")
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

        ## Common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-VMName', '-Port',
        ## '-Token', '-WebSocketUrl', '-ThrottleLimit' and '-Command'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 9) -Because ($command.Parameters.Keys -join ", ")
        $command.ParameterSets.Count | Should -Be 3

        $command.Parameters["UserName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["UserName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue

        $command.Parameters["ComputerName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ComputerName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue

        $command.Parameters["VMName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["VMName"].ParameterSets.ContainsKey("VMSet") | Should -BeTrue

        $command.Parameters["Token"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Token"].ParameterSets.ContainsKey("WebSocketSet") | Should -BeTrue

        $command.Parameters["WebSocketUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["WebSocketUrl"].ParameterSets.ContainsKey("WebSocketSet") | Should -BeTrue

        $command.Parameters["ConfigurationName"].ParameterSets.Count | Should -Be 2
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("ComputerSet") | Should -BeTrue
        $command.Parameters["ConfigurationName"].ParameterSets.ContainsKey("WebSocketSet") | Should -BeTrue

        $command.Parameters["Port"].ParameterSets.Count | Should -Be 2
        $command.Parameters["Port"].ParameterSets.ContainsKey("VMSet") | Should -BeTrue
        $command.Parameters["Port"].ParameterSets.ContainsKey("WebSocketSet") | Should -BeTrue

        $command.Parameters["ThrottleLimit"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ThrottleLimit"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        $command.Parameters["Command"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Command"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        ## Common parameters + '-UserName', '-ComputerName', '-ConfigurationName', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[0].Name | Should -BeExactly "ComputerSet"
        $command.ParameterSets[0].Parameters.Count | Should -Be ($CommonParameterCount + 5)

        ## Common parameters + '-VMName', '-Port', '-ThrottleLimit' and '-Command'
        $command.ParameterSets[1].Name | Should -BeExactly "VMSet"
        $command.ParameterSets[1].Parameters.Count | Should -Be ($CommonParameterCount + 4)

        ## Common parameters + '-Token', '-WebSocketUrl', '-ConfigurationName', '-Port', '-ThrottleLimit', '-Command'
        $command.ParameterSets[2].Name | Should -BeExactly "WebSocketSet"
        $command.ParameterSets[2].Parameters.Count | Should -Be ($CommonParameterCount + 6)

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
        ## Common parameters + '-ComputerName'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 1) -Because ($command.Parameters.Keys -join ", ")
        $command.Parameters["ComputerName"].ParameterType.FullName | Should -BeExactly "System.String"
        $command.Parameters.ContainsKey("SessionName") | Should -BeFalse
    }

    It "Use 'Experimental' attribute directly on parameters - '<Name>'" -TestCases @(
        @{ Name = "Save-MyFile"; CommandType = "Function" }
        @{ Name = "Save-MyFileCSharp"; CommandType = "Cmdlet" }
    ) {
        param($Name, $CommandType)
        $command = Get-Command $Name
        $command.CommandType | Should -Be $CommandType
        ## Common parameters + '-ByUrl', '-ByRadio', '-FileName', '-Destination'
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 4) -Because ($command.Parameters.Keys -join ", ")
        $command.ParameterSets.Count | Should -Be 2

        $command.Parameters["ByUrl"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByUrl"].ParameterSets.ContainsKey("UrlSet") | Should -BeTrue

        $command.Parameters["ByRadio"].ParameterSets.Count | Should -Be 1
        $command.Parameters["ByRadio"].ParameterSets.ContainsKey("RadioSet") | Should -BeTrue

        $command.Parameters["Destination"].ParameterSets.Count | Should -Be 1
        $command.Parameters["Destination"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        $command.Parameters["FileName"].ParameterSets.Count | Should -Be 1
        $command.Parameters["FileName"].ParameterSets.ContainsKey("__AllParameterSets") | Should -BeTrue

        $command.Parameters.ContainsKey("Configuration") | Should -BeFalse
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
        ## Common parameters + '-Name' (dynamic parameters are not triggered)
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 1) -Because ($command.Parameters.Keys -join ", ")
        $command.Parameters["Name"] | Should -Not -BeNullOrEmpty

        $command = Get-Command $Name -ArgumentList "Joe"
        ## Common parameters + '-Name' and '-ConfigFile' (dynamic parameters are triggered)
        $command.Parameters.Count | Should -Be ($CommonParameterCount + 2)
        $command.Parameters["ConfigFile"].Attributes.Count | Should -Be 2
        $command.Parameters["ConfigFile"].Attributes[0] | Should -BeOfType parameter
        $command.Parameters["ConfigFile"].Attributes[1] | Should -BeOfType ValidateNotNullOrEmpty

        $command.Parameters.ContainsKey("ConfigName") | Should -BeFalse
    }
}

Describe "Expected errors" -Tag "CI" {
    It "'[Experimental()]' should fail to construct the attribute" {
        { [Experimental()]param() } | Should -Throw -ErrorId "MethodCountCouldNotFindBest"
    }

    It "Argument validation for constructors of 'ExperimentalAttribute' - <TestName>" -TestCases @(
        @{ TestName = "Name is empty string"; FeatureName = "";                  FeatureAction = "None"; ErrorId = "PSArgumentNullException" }
        @{ TestName = "Name is null";         FeatureName = [NullString]::Value; FeatureAction = "None"; ErrorId = "PSArgumentNullException" }
        @{ TestName = "Action is None";       FeatureName = "feature";           FeatureAction = "None"; ErrorId = "PSArgumentException" }
        @{ TestName = "Action is Show";       FeatureName = "feature";           FeatureAction = "Show"; ErrorId = $null }
        @{ TestName = "Action is Hide";       FeatureName = "feature";           FeatureAction = "Hide"; ErrorId = $null }
    ) {
        param($FeatureName, $FeatureAction, $ErrorId)

        if ($ErrorId -ne $null) {
            { [Experimental]::new($FeatureName, $FeatureAction) } | Should -Throw -ErrorId $ErrorId
        } else {
            { [Experimental]::new($FeatureName, $FeatureAction) } | Should -Not -Throw
        }
    }

    It "Argument validation for constructors of 'ParameterAttribute' - <TestName>" -TestCases @(
        @{ TestName = "Name is empty string"; FeatureName = "";                  FeatureAction = "None"; ErrorId = "PSArgumentNullException" }
        @{ TestName = "Name is null";         FeatureName = [NullString]::Value; FeatureAction = "None"; ErrorId = "PSArgumentNullException" }
        @{ TestName = "Action is None";       FeatureName = "feature";           FeatureAction = "None"; ErrorId = "PSArgumentException" }
        @{ TestName = "Action is Show";       FeatureName = "feature";           FeatureAction = "Show"; ErrorId = $null }
        @{ TestName = "Action is Hide";       FeatureName = "feature";           FeatureAction = "Hide"; ErrorId = $null }
    ) {
        param($FeatureName, $FeatureAction, $ErrorId)

        if ($ErrorId -ne $null) {
            { [Parameter]::new($FeatureName, $FeatureAction) } | Should -Throw -ErrorId $ErrorId
        } else {
            { [Parameter]::new($FeatureName, $FeatureAction) } | Should -Not -Throw
        }
    }

    It "Feature name check" {
        $psd1Content = @'
@{
ModuleVersion = '0.0.1'
CompatiblePSEditions = @('Core')
GUID = 'ce31259c-1804-4016-bc29-083bd2599e19'
PrivateData = @{
    PSData = @{
        ExperimentalFeatures = @(
            @{ Name = '.Feature1'; Description = "Test feature number 1." }
            @{ Name = 'Feature2.'; Description = "Test feature number 2." }
            @{ Name = 'Feature3'; Description = "Test feature number 3." }
            @{ Name = 'Module.Feature4'; Description = "Test feature number 4." }
            @{ Name = 'InvalidFeatureName.Feature5'; Description = "Test feature number 5." }
        )
    }
}
}
'@
        $moduleFile = Join-Path $TestDrive InvalidFeatureName.psd1
        Set-Content -Path $moduleFile -Value $psd1Content -Encoding Ascii

        Import-Module $moduleFile -ErrorVariable featureNameError -ErrorAction SilentlyContinue
        $featureNameError | Should -Not -BeNullOrEmpty
        $featureNameError[0].FullyQualifiedErrorId | Should -Be "Modules_InvalidExperimentalFeatureName,Microsoft.PowerShell.Commands.ImportModuleCommand"
        $featureNameError[0].Exception.Message.Contains(".Feature1") | Should -BeTrue
        $featureNameError[0].Exception.Message.Contains("Feature2.") | Should -BeTrue
        $featureNameError[0].Exception.Message.Contains("Feature3") | Should -BeTrue
        $featureNameError[0].Exception.Message.Contains("Module.Feature4") | Should -BeTrue
        $featureNameError[0].Exception.Message.Contains("InvalidFeatureName.Feature5") | Should -BeFalse
    }
}
