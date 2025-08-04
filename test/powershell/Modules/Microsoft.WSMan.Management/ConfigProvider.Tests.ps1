# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '')]
param()

Describe "WSMan Config Provider" -Tag Feature,RequireAdminOnWindows {
    BeforeAll {
        #skip all tests on non-windows platform
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues["it:skip"] = !$IsWindows

        if ($IsWindows) {
            $badCredentialError = 1326
            $pluginXml = [xml](winrm g winrm/config/plugin?name=microsoft.powershell -format:xml)
            $pluginPath = "WSMan:\localhost\Plugin\microsoft.powershell"
            $testPluginXml = [xml]($pluginXml.OuterXml)
            $testPluginXml.PlugInConfiguration.Name = "TestPlugin"
            $testPluginXml.PlugInConfiguration.RemoveAttribute("xml:lang")
            $testPluginXml.PlugInConfiguration.Resources.Resource.ResourceUri = "http://schemas.microsoft.com/powershell/TestPlugin"
            $testPluginXml.PlugInConfiguration.Resources.Resource.Security.Uri = "http://schemas.microsoft.com/powershell/TestPlugin"
            Set-Content $TestDrive\plugin.xml -Value $testPluginXml.OuterXml
            $null = winrm c winrm/config/plugin?Name=TestPlugin -file:$TestDrive\plugin.xml
            $testPluginPath = "WSMan:\localhost\Plugin\TestPlugin"
            $testUser = (Get-Random)
            $testPass = "Secret123!"
            $null = net user $testUser $testPass /add
        }
    }

    AfterAll {
        $Global:PSDefaultParameterValues = $originalDefaultParameterValues
        if ($IsWindows) {
            $null = winrm d winrm/config/plugin?Name=TestPlugin
            $null = net user $testUser /DELETE
        }
    }

    Function Test-Plugin($plugin, $expectedMissingProperties, $expectedMissingAttributes) {
        $plugin.PSPath | Should -Exist
        $testPluginXml = [xml](winrm g winrm/config/plugin?name=$($plugin.Name) -format:xml)
        $pluginProperties = Get-ChildItem $plugin.PSPath
        $xmlElementCount = ($testPluginXml.PluginConfiguration | Get-Member -Type Properties).Count + $expectedMissingProperties.Count - $expectedMissingAttributes.Count
        $pluginProperties.Count | Should -BeExactly $xmlElementCount
        foreach ($pluginProperty in $pluginProperties) {
            if ($pluginProperty.Type -eq "System.String") {
                $pluginProperty.Value | Should -Be $testPluginXml.PluginConfiguration.$($pluginProperty.Name)
                (Get-Item "$($plugin.PSPath)\$($pluginProperty.Name)").Value | Should -Be $testPluginXml.PluginConfiguration.$($pluginProperty.Name)
            }
        }
    }

    AfterEach {
        if (-not (Test-Path wsman:))
        {
            $null = New-PSDrive -Name WSMan -PSProvider WSMan -Root "" -Scope Global
        }
    }

    Context "Misc tests" {
        It "Can remove and add wsman drive" {
            $wsmanDrive = Get-PSDrive -Name WSMan
            Remove-PSDrive -Name wsman
            { Get-PSDrive -Name wsman -ErrorAction Stop } | Should -Throw -ErrorId "GetLocationNoMatchingDrive,Microsoft.PowerShell.Commands.GetPSDriveCommand"
            $wsmanDrive2 = $wsmanDrive | New-PSDrive -PSProvider WSMan
            $wsmanDrive2 | Should -BeOfType System.Management.Automation.PSDriveInfo
            $wsmanDrive2.Name | Should -BeExactly "WSMan"
            $wsmanDrive2.Provider.Name | Should -BeExactly "WSMan"
        }

        It "WSMan Config Provider starts WinRM if it is stopped" {
            try {
                Stop-Service WinRM
                Get-ChildItem wsman:\localhost -Force | Should -Not -BeNullOrEmpty
                (Get-Service WinRM).Status | Should -Be 'Running'
            }
            finally {
                if ((Get-Service WinRM).Status -eq 'stopped') {
                    Start-Service WinRM
                }
            }
        }

        It "Container check works" {
            Test-Path wsman:\foo -PathType container | Should -BeFalse
        }
    }

    Context "Get-Item tests" {

        It "Plugin has correct properties" {
            $plugin = Get-Item $pluginPath
            Test-Plugin -Plugin $plugin
        }

        It "Plugin InitializationParameters are correct" {
            $initializationParameters = Get-ChildItem $pluginPath\InitializationParameters
            $initializationParameters.Count | Should -Be (,$pluginXml.PluginConfiguration.InitializationParameters.Param).Count
            foreach ($initializationParameter in $initializationParameters) {
                $initializationParameter.Value | Should -Be ($pluginXml.PluginConfiguration.InitializationParameters | Where-Object { $_.Param.Name -eq $initializationParameter.Name }).Param.Value
                (Get-Item $pluginPath\InitializationParameters\$($initializationParameter.Name)).Value | Should -Be ($pluginXml.PluginConfiguration.InitializationParameters | Where-Object { $_.Param.Name -eq $initializationParameter.Name }).Param.Value
            }
        }

        It "Plugin Quotas are correct" {
            $quotas = Get-ChildItem $pluginPath\Quotas
            $quotas.Count | Should -Be ($pluginXml.PluginConfiguration.Quotas | Get-Member -Type Properties).Count
            foreach ($quota in $quotas) {
                $quota.Value | Should -Be $pluginXml.PluginConfiguration.Quotas.$($quota.Name)
            }
        }

        It "Plugin Resources are correct" {
            $resources = Get-ChildItem $pluginPath\Resources
            $resources.Count | Should -Be (,$pluginXml.PluginConfiguration.Resources.Resource).Count
        }

        It "Plugin Security Resource is correct" {
            (,$pluginXml.PluginConfiguration.Resources.Resource).Count | Should -Be 1 # by default only Security resource should be there
            $resource = Get-ChildItem "$pluginPath\Resources" | Select-Object -First 1
            $resourceUri = Get-Item "$($resource.PSPath)\ResourceUri"
            $resourceUri.Value | Should -Be "http://schemas.microsoft.com/powershell/microsoft.powershell"
            $securityContainer = Get-ChildItem "$pluginPath\Resources\$($resource.Name)\Security" | Select-Object -First 1
            $securityProperties = Get-ChildItem $securityContainer.PSPath
            $skippedAttributes = @("ParentResourceUri","xmlns") # these are added by the WSMan Config Provider but not in the original xml

            # ParentResourceUri is added by the Config Provider, xmlns existing seems to be dependent on WinRM which is inconsistent
            $securityProperties.Count | Should -BeGreaterThan (($pluginXml.PluginConfiguration.Resources.Resource.Security | Get-Member -Type Properties).Count)
            foreach ($securityProperty in $securityProperties) {
                if ($skippedAttributes -notcontains $securityProperty.Name) {
                    $securityProperty.Value | Should -Be ($pluginXml.PluginConfiguration.Resources.Resource.Security.$($securityProperty.Name))
                    (Get-Item "$($securityContainer.PSPath)\$($securityProperty.Name)").Value | Should -Be ($pluginXml.PluginConfiguration.Resources.Resource.Security.$($securityProperty.Name))
                }
            }
        }
    }

    Context "Set-Item tests" {
        It "Set-Item should fail for `$null value" {
            { Set-Item WSMan:\localhost\Client\TrustedHosts $null } | Should -Throw -ErrorId "System.ArgumentException,Microsoft.PowerShell.Commands.SetItemCommand"
        }

        It "Set-Item should fail for <path>" -TestCases @(
            @{path="WSMan:\"},
            @{path="WSMan:\localhost"}
        ) {
            param ($path)
            { Set-Item $path "foo" } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand"
        }

        It "Set-Item -WhatIf should work" {
            $trustedHostsPath = "WSMan:\localhost\Client\TrustedHosts"
            $trustedHosts = Get-Item $trustedHostsPath
            Set-Item $trustedHostsPath "hello" -WhatIf
            (Get-Item $trustedHostsPath).Value | Should -Be $trustedHosts.Value
        }

        It "Set-Item on TrustedHosts should succeed" {
            try {
                $trustedHostsPath = "WSMan:\localhost\Client\TrustedHosts\"
                $trustedHosts = Get-Item $trustedHostsPath
                Set-Item -Path $trustedHostsPath -Value "hello" -Force
                (Get-Item $trustedHostsPath).Value | Should -Be "hello"
            }
            finally {
                Set-Item $trustedHostsPath $trustedHosts.Value -Force
            }
        }

        It "Set-Item on plugin RunAsUser should fail for invalid creds" {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $password = ConvertTo-SecureString "My voice is my passport, verify me" -AsPlainText -Force
            $creds = [pscredential]::new((Get-Random),$password)
            $exception = { Set-Item $testPluginPath\RunAsUser $creds } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand" -PassThru
            $exception.Exception.Message | Should -Match ".*$badCredentialError.*"
        }

        It "Set-Item and Clear-Item on plugin RunAsUser should succeed for valid creds" {
            $password = ConvertTo-SecureString $testPass -AsPlainText -Force
            $creds = [pscredential]::new($testUser,$password)
            Set-Item $testPluginPath\RunAsUser $creds -WarningAction SilentlyContinue
            (Get-Item $testPluginPath\RunAsUser).Value | Should -Be $testUser
            (Get-Item $testPluginPath\RunAsPassword).Value | Should -Be "System.Security.SecureString"
            Clear-Item $testPluginPath\RunAsUser -WarningAction SilentlyContinue
            (Get-Item $testPluginPath\RunAsUser).Value | Should -BeNullOrEmpty
            (Get-Item $testPluginPath\RunAsPassword).Value | Should -BeNullOrEmpty
        }

        It "Set-Item on plugin RunAsUser should fail for invalid password" {
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            $password = ConvertTo-SecureString "My voice is my passport, verify me" -AsPlainText -Force
            $creds = [pscredential]::new($testUser,$password)
            $exception = { Set-Item $testPluginPath\RunAsUser $creds } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand" -PassThru
            $exception.Exception.Message | Should -Match ".*$badCredentialError.*"
        }

        It "Set-Item on password without user on plugin should fail for <password>" -TestCases @(
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            @{password=(ConvertTo-SecureString "My voice is my passport, verify me" -AsPlainText -Force)},
            #[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification="Demo/doc/test secret.")]
            @{password="hello"}
        ) {
            param($password)
            Clear-Item $testPluginPath\RunAsUser -WarningAction SilentlyContinue
            { Set-Item $testPluginPath\RunAsPassword $password } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand"
        }

        It "Set-Item on plugin XmlRenderingType property should succeed for '<type>'" -TestCases @(
            @{type="XmlReader"},
            @{type="text"},
            @{type="xMLrEADER"},
            @{type="TEXT"}
        ) {
            param ($type)
            Set-Item $testPluginPath\XmlRenderingType $type -WarningAction SilentlyContinue
            (Get-Item $testPluginPath\XmlRenderingType).Value | Should -Be $type
        }

        It "Set-Item on non-existent property should fail" {
            { Set-Item $testPluginPath\foo "bar" } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand"
        }

        It "Set-Item on plugin Resource '<property>' property with '<value>' should succeed" -TestCases @(
            @{property="SupportsOptions"; value=$false; expected="False"},
            @{property="sUPPORTSoPTIONS"; value=$true; expected="True"}
        ) {
            param($property, $value, $expected)
            $resource = Get-ChildItem "$testPluginPath\Resources" | Select-Object -First 1
            Set-Item "$($resource.PSPath)\$property" $value -WarningAction SilentlyContinue
            (Get-Item "$($resource.PSPath)\$property").Value | Should -BeExactly $expected
        }

        It "Set-Item on plugin Security Resource '<property>' property with '<value>' should succeed" -TestCases @(
            # truncated version of the default SDDL
            @{property="Sddl"; value="O:NSG:BAD:P(A;;GA;;;BA)"},
            # default SDDL set by WinRM
            @{property="SDDL"; value="O:NSG:BAD:P(A;;GA;;;BA)(A;;GA;;;IU)(A;;GA;;;RM)S:P(AU;FA;GA;;;WD)(AU;SA;GXGW;;;WD)"}
        ) {
            param($property, $value, $expected)
            $resource = Get-ChildItem "$testPluginPath\Resources" | Select-Object -First 1
            $security = Get-ChildItem "$($resource.PSPath)\Security" | Select-Object -First 1
            Set-Item "$($security.PSPath)\$property" $value -WarningAction SilentlyContinue -Force
            (Get-Item "$($security.PSPath)\$property").Value | Should -Be $value
        }

        It "Set-Item on plugin Security Resource '<property>' property with invalid '<value>' should fail" -TestCases @(
            @{property="Sddl"; value="foo"},
            @{property="sDDL"; value="D:P(A;;GA;;;BA)"} # truncated version of default SDDL with owner removed
        ) {
            param($property, $value, $expected)
            $resource = Get-ChildItem "$testPluginPath\Resources" | Select-Object -First 1
            $security = Get-ChildItem "$($resource.PSPath)\Security" | Select-Object -First 1
            { Set-Item "$($security.PSPath)\$property" $value -Force } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand"
        }

        It "Set-Item on plugin InitializationParameters '<property>' property with '<value>' should succeed" -TestCases @(
            @{property="PSVersion"; value="6.0.0"},
            @{property="psvERSION"; value="5.1"}
        ) {
            param($property, $value)
            Set-Item "$testPluginPath\InitializationParameters\$property" $value -WarningAction SilentlyContinue
            (Get-Item "$testPluginPath\InitializationParameters\$property").Value | Should -Be $value
        }

        It "Set-Item on plugin Quotas '<property>' property with '<value>' should succeed" -TestCases @(
            @{property="IdleTimeoutms"; value=61000},
            @{property="iDLEtIMEOUTMS"; value=7200000},
            @{property="MaxShells"; value=1},
            @{property="mAXsHELLS"; value=2147483647}
            ) {
            param($property, $value)
            Set-Item "$testPluginPath\Quotas\$property" $value -WarningAction SilentlyContinue
            (Get-Item "$testPluginPath\Quotas\$property").Value | Should -Be $value
        }

        It "Set-Item on plugin Quotas out of range on '<property>' property with '<value>' should fail" -TestCases @(
            @{property="IdleTimeoutms"; value=59999},
            @{property="MaxShells"; value=0}
        ) {
            param($property, $value)
            { Set-Item "$testPluginPath\Quotas\$property" $value -WarningAction SilentlyContinue } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.SetItemCommand"
        }
    }

    Context "Clear-Item tests" {
        It "Clear-Item on <property> should fail" -TestCases @(
            @{property="Filename"},
            @{property="Quotas\IdleTimeoutms"},
            @{property="InitializationParameters\PSVersion"}
        ) {
            { Clear-Item "$testPluginPath\$property" -WarningAction SilentlyContinue } | Should -Throw -ErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.ClearItemCommand"
        }
    }

    Context "New-Item and Remove-Item tests" {
        It "New-Item and Remove-Item at root should succeed" -TestCases @(
            @{name=$env:computername;expected=$env:computername},
            @{name="${env:computername}\";expected=$env:computername}
        ) {
            param($name, $expected)
            try {
                $connections = Get-ChildItem wsman:\
                $newItem = New-Item WSMan:\$name
                "WSMan:\$name" | Should -Exist
                (Get-ChildItem wsman:\).Count | Should -Be ($connections.Count + 1)
                # not a .Net type so can't use BeOfType
                $newItem.PSObject.TypeNames[0] | Should -Be "Microsoft.WSMan.Management.WSManConfigContainerElement#ComputerLevel"
                $newItem.Name | Should -Be $expected
                Remove-Item WSMan:\$name -Recurse -Force
                "WSMan:\$name" | Should -Not -Exist
            }
            finally {
                Remove-Item WSMan:\$name -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for a listener" {
            try {
                $newListener = New-Item -Path WSMan:\localhost\Listener\ -Address IP:127.0.0.1 -port 6666 -Transport HTTP -Enabled $false -HostName foo -URLPrefix bar -Force
                $listenerName = $newListener.Name
                $listenerName | Should -Not -BeNullOrEmpty
                $properties = Get-ChildItem "WSMan:\localhost\Listener\$listenerName"
                $listenerXml = [xml](winrm g winrm/config/listener?Address=IP:127.0.0.1+Transport=HTTP -format:xml)
                $expectedMissingAttributes = "cfg","xsi","lang"
                $properties.Count | Should -Be (($listenerXml.Listener | Get-Member -Type Properties).Count - $expectedMissingAttributes.Count)
                foreach ($property in $properties) {
                    if (-not $property.Name.StartsWith("ListeningOn")) { # this property is represented differently
                        $property.Value | Should -Be $listenerXml.Listener.$($property.Name)
                    }
                }
                Remove-Item -Path "WSMan:\localhost\Listener\$listenerName" -Recurse -Force
                $newListener.PSPath | Should -Not -Exist
            }
            finally {
                Remove-Item -Path "WSMan:\localhost\Listener\$listenerName" -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for a plugin using parameters" {
            try {
                $password = ConvertTo-SecureString $testPass -AsPlainText -Force
                $creds = [pscredential]::new($testUser, $password)
                $plugin = New-Item -Plugin TestPlugin2 -UseSharedProcess -AutoRestart `
                    -ProcessIdleTimeoutSec 120 -FileName "${env:windir}\system32\pwrshplugin.dll" `
                    -SDKVersion 2 -Resource foo -Capability shell -XMLRenderingType text -Path WSMan:\localhost\plugin `
                    -RunAsCredential $creds
                $expectedMissingProperties = @("InitializationParameters")
                Test-Plugin -Plugin $plugin -expectedMissingProperties $expectedMissingProperties
                Remove-Item WSMan:\localhost\Plugin\TestPlugin2\ -Recurse -Force
                "WSMan:\localhost\Plugin\TestPlugin2" | Should -Not -Exist
            }
            finally {
                Remove-Item WSMan:\localhost\Plugin\TestPlugin2\ -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for a plugin using a file" {
            $fileXml = @"
<PlugInConfiguration Name="TestPlugin2" Filename="${env:windir}\system32\pwrshplugin.dll" SDKVersion="2" XmlRenderingType="text" Enabled="false"
 Architecture="64" UseSharedProcess="true" ProcessIdleTimeoutSec="0" RunAsUser="" RunAsPassword="" AutoRestart="false" RunAsVirtualAccount="false"
 RunAsVirtualAccountGroups="" OutputBufferingMode="Block" xmlns="http://schemas.microsoft.com/wbem/wsman/1/config/PluginConfiguration">
    <InitializationParameters>
        <Param Name="PSVersion" Value="5.1"></Param>
    </InitializationParameters>
    <Resources>
        <Resource ResourceUri="http://schemas.microsoft.com/powershell/TestPlugin2" SupportsOptions="true" ExactMatch="true">
            <Security Uri="http://schemas.microsoft.com/powershell/TestPlugin2"
                Sddl="O:NSG:BAD:P(A;;GA;;;BA)(A;;GA;;;IU)(A;;GA;;;RM)S:P(AU;FA;GA;;;WD)(AU;SA;GXGW;;;WD)" ExactMatch="False"></Security>
            <Capability Type="Shell"></Capability>
        </Resource>
    </Resources>
    <Quotas MaxIdleTimeoutms="2147483646" MaxConcurrentUsers="{0}" IdleTimeoutms="6200000" MaxProcessesPerShell="2147483646"
        MaxMemoryPerShellMB="2147483646" MaxConcurrentCommandsPerShell="2147483646" MaxShells="2147483646" MaxShellsPerUser="2147483646"></Quotas>
</PlugInConfiguration>
"@
            $osInfo = [System.Environment]::OSVersion.Version
            $isSrv2k12R2 = $osInfo.Major -eq 6 -and $osInfo.Minor -eq 3

            # On Windows Server 2012R2 MaxConcurrentUsers is limited to 100.
            $maxConcurrentUsers = if ($isSrv2k12R2) { '50' } else { '2147483646' }

            $fileXml = $fileXml -f $maxConcurrentUsers

            Set-Content -Path $testdrive\plugin.xml -Value $fileXml
            try {
                $plugin = New-Item -Path WSMan:\localhost\Plugin -File $testdrive\plugin.xml -Name TestPlugin2
                Test-Plugin -Plugin $plugin
                Remove-Item WSMan:\localhost\Plugin\TestPlugin2\ -Recurse -Force
                "WSMan:\localhost\Plugin\TestPlugin2\" | Should -Not -Exist
            }
            finally {
                Remove-Item "WSMan:\localhost\Plugin\TestPlugin2\" -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for a plugin resource" {
            try {
                $resource = New-Item -Path WSMan:\localhost\Plugin\TestPlugin\Resources\ `
                    -ResourceUri http://foo -Capability shell
                $resource.PSPath | Should -Exist
                $properties = Get-ChildItem $resource.PSPath
                ($properties | Where-Object { $_.Name -eq "ResourceUri" }).Value | Should -Be "http://foo/"
                ($properties | Where-Object { $_.Name -eq "Capability" })[0].Value | Should -Be "shell"
                Remove-Item $resource.PSPath -Recurse -Force
                $resource.PSPath | Should -Not -Exist
            }
            finally {
                Remove-Item $resource.PSPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for an initialization parameter" {
            try {
                $parameter = New-Item -Path WSMan:\localhost\Plugin\TestPlugin\InitializationParameters `
                    -ParamName foo -ParamValue bar
                $parameterObj = Get-Item $parameter.PSPath
                $parameterObj.Name | Should -Be "foo"
                $parameterObj.Value | Should -Be "bar"
                Remove-Item $parameter.PSPath -Force
                $parameter.PSPath | Should -Not -Exist
            }
            finally {
                Remove-Item $parameter.PSPath -Force -ErrorAction SilentlyContinue
            }
        }

        It "New-Item and Remove-Item for a security resource" {
            try {
                $sddl = "O:NSG:BAD:P(A;;GA;;;BA)"
                $resource = Get-ChildItem -Path WSMan:\localhost\Plugin\TestPlugin\Resources\ | Select-Object -First 1
                # remove existing security resource since the folder name is just a hash of the resource uri
                Get-ChildItem "$($resource.PSPath)\Security" | Remove-Item -Recurse -Force
                $security = New-Item "$($resource.PSPath)\Security" -SDDL $sddl -Force
                $security.PSPath | Should -Exist
                $securityObj = Get-Item $security.PSPath
                (Get-ChildItem $securityObj.PSPath | Where-Object { $_.Name -eq 'sddl' }).Value | Should -Be $sddl
                Remove-Item $security.PSPath -Recurse -Force
                $security.PSPath | Should -Not -Exist
            }
            finally {
                Remove-Item $security.PSPath -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Get-Help tests" {
        It "Get-Help while in WSMan: drive works" {
            try {
                Push-Location WSMan:\localhost
                Get-Help New-Item | Should -Not -BeNullOrEmpty
            }
            finally {
                Pop-Location
            }
        }
    }

    Context 'ItemSeparator properties' {
        It 'WSMan provider has ItemSeparator properties' {

            (Get-PSProvider WSMan).ItemSeparator | Should -Be '\'
            (Get-PSProvider WSMan).AltItemSeparator | Should -Be '/'
        }

        It 'ItemSeparator properties is read-only in WSMan provider' {
            { (Get-PSProvider WSMan).ItemSeparator = $null } | Should -Throw
            { (Get-PSProvider WSMan).AltItemSeparator = $null } | Should -Throw
        }
    }
}
