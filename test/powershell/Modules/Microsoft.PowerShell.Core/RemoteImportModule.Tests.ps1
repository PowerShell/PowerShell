# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Remote import-module tests" -Tags 'Feature','RequireAdminOnWindows' {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $modulePath = "$testdrive\Modules\TestImport"
        if (!$IsWindows) {
            $PSDefaultParameterValues["it:skip"] = $true
        } else {
            $pssession = New-RemoteSession
            Invoke-Command -Session $pssession -Scriptblock { $env:PSModulePath += ";${using:testdrive}" }
            # pending https://github.com/PowerShell/PowerShell/issues/4819
            # $cimsession = New-RemoteSession -CimSession
            $null = New-Item -ItemType Directory -Path $modulePath
            Set-Content -Path $modulePath\testimport.psm1 -Value "function test-hello { 'world' }"
            New-ModuleManifest -Path $modulePath\testimport.psd1 -ModuleVersion 1.2.3 -RootModule testimport.psm1 -FunctionsToExport "test-hello" `
                -HelpInfoUri "https://help" -Guid (New-Guid)
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
        if ($IsWindows) {
            $pssession | Remove-PSSession -ErrorAction SilentlyContinue
        }

        Remove-Module TestImport -Force -ErrorAction SilentlyContinue
    }

    BeforeEach {
        Remove-Module TestImport -Force -ErrorAction SilentlyContinue
    }

    It "Import-Module can be called as an API with '<parameter>' = '<value>'" -TestCases @(
        @{parameter = "Global"             ; value = $true},
        @{parameter = "Global"             ; value = $false},
        @{parameter = "Prefix"             ; value = "Hello"},
        @{parameter = "Name"               ; value = "foo","bar"},
        @{parameter = "FullyQualifiedName" ; value = @{ModuleName='foo';RequiredVersion='0.0'},@{ModuleName='bar';RequiredVersion='1.1'}},
        @{parameter = "Assembly"           ; script = { [System.AppDomain]::CurrentDomain.GetAssemblies() | Select-Object -First 2 }}
        @{parameter = "Function"           ; value = "foo","bar"},
        @{parameter = "Cmdlet"             ; value = "foo","bar"},
        @{parameter = "Variable"           ; value = "foo","bar"},
        @{parameter = "Alias"              ; value = "foo","bar"},
        @{parameter = "Force"              ; value = $true},
        @{parameter = "Force"              ; value = $false},
        @{parameter = "PassThru"           ; value = $true},
        @{parameter = "PassThru"           ; value = $false},
        @{parameter = "AsCustomObject"     ; value = $true},
        @{parameter = "AsCustomObject"     ; value = $false},
        @{parameter = "MinimumVersion"     ; value = "1.2.3"},
        @{parameter = "MaximumVersion"     ; value = "3.2.1"},
        @{parameter = "RequiredVersion"    ; value = "1.1.1"},
        @{parameter = "ArgumentList"       ; value = "hello","world"},
        @{parameter = "DisableNameChecking"; value = $true},
        @{parameter = "DisableNameChecking"; value = $false},
        @{parameter = "NoClobber"          ; value = $true},
        @{parameter = "NoClobber"          ; value = $false},
        @{parameter = "Scope"              ; value = "Local"},
        @{parameter = "Scope"              ; value = "Global"},
        @{parameter = "PSSession"          ; value = $pssession},
        # @{parameter = "CimSession"         ; value = $cimsession},
        @{parameter = "CimResourceUri"     ; value = "http://foo/"},
        @{parameter = "CimNamespace"       ; value = "foo"}
        ) {
        param($parameter, $value, $script)

        $importModuleCommand = [Microsoft.PowerShell.Commands.ImportModuleCommand]::new()
        if ($script -ne $null) {
            $value = & $script
        }
        $importModuleCommand.$parameter = $value
        if ($parameter -eq "FullyQualifiedName") {
            $importModuleCommand.FullyQualifiedName.Count | Should -BeExactly 2
            $importModuleCommand.FullyQualifiedName | Should -BeOfType Microsoft.PowerShell.Commands.ModuleSpecification
            $importModuleCommand.FullyQualifiedName[0].Name | Should -BeExactly "foo"
            $importModuleCommand.FullyQualifiedName[0].RequiredVersion | Should -Be "0.0"
            $importModuleCommand.FullyQualifiedName[1].Name | Should -BeExactly "bar"
            $importModuleCommand.FullyQualifiedName[1].RequiredVersion | Should -Be "1.1"
        } else {
            $importModuleCommand.$parameter | Should -BeExactly $value
        }
    }

    It "Import-Module can import over remote session: <test>" -TestCases @(
        @{ test = "pssession"              ; parameters = @{Name="TestImport";PSSession=$pssession}},
#        @{ test = "cimsession"             ; parameters = @{Name="TestImport";CimSession=$cimsession}},
        @{ test = "minimumversion"         ; parameters = @{Name="TestImport";PSSession=$pssession;MinimumVersion="1.0";Force=$true}},
        @{ test = "requiredversion"        ; parameters = @{Name="TestImport";PSSession=$pssession;RequiredVersion="1.2.3"}},
        @{ test = "maximumversion"          ; parameters = @{Name="TestImport";PSSession=$pssession;MaximumVersion="2.0"}},
        @{ test = "invalid minimumversion"  ; parameters = @{Name="TestImport";PSSession=$pssession;MinimumVersion="2.0"};
            errorid = "Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand,Microsoft.PowerShell.Commands.ImportModuleCommand"},
        @{ test = "invalid maximumversion" ; parameters = @{Name="TestImport";PSSession=$pssession;MaximumVersion="1.0"};
            errorid = "Modules_ModuleWithVersionNotFound,Microsoft.PowerShell.Commands.ImportModuleCommand,Microsoft.PowerShell.Commands.ImportModuleCommand"},
        @{ test = "fullyqualifiedname"     ; parameters = @{FullyQualifiedName=@{Modulename="TestImport"; RequiredVersion="1.2.3"};PSSession=$pssession}}
        ) {
        param ($test, $parameters, $errorid)

        Invoke-Command -Session $pssession -ScriptBlock { $env:PSModulePath += ";$(Split-Path $using:modulePath)"}
        Get-Module TestImport | Should -BeNullOrEmpty
        if ($errorid) {
            { Import-Module @parameters -ErrorAction Stop } | Should -Throw -ErrorId $errorid
        } else {
            Import-Module @parameters
            $module = Get-Module TestImport
            $module.Name | Should -BeExactly "TestImport"

            # generated proxy module always uses 1.0
            $module.Version | Should -BeExactly "1.0"

            test-hello | Should -BeExactly "world"
        }
    }
}
