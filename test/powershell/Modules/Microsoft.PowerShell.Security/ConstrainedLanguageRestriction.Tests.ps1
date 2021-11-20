# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

##
## ----------
## Test Note:
## ----------
## Since these tests change session and system state (constrained language and system lockdown)
## they will all use try/finally blocks instead of Pester AfterEach/AfterAll to ensure session
## and system state is restored.
## Pester AfterEach, AfterAll is not reliable when the session is constrained language or locked down.
##

Import-Module HelpersSecurity

try
{
    $defaultParamValues = $PSDefaultParameterValues.Clone()
    $PSDefaultParameterValues["it:Skip"] = !$IsWindows

    Describe "Help built-in function should not expose nested module private functions when run on locked down systems" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $restorePSModulePath = $env:PSModulePath
            $env:PSModulePath += ";$TestDrive"

            $trustedModuleName1 = "TrustedModule$(Get-Random -Max 999)_System32"
            $trustedModulePath1 = Join-Path $TestDrive $trustedModuleName1
            New-Item -ItemType Directory $trustedModulePath1
            $trustedModuleFilePath1 = Join-Path $trustedModulePath1 ($trustedModuleName1 + ".psm1")
            $trustedModuleManifestPath1 = Join-Path $trustedModulePath1 ($trustedModuleName1 + ".psd1")

            $trustedModuleName2 = "TrustedModule$(Get-Random -Max 999)_System32"
            $trustedModulePath2 = Join-Path $TestDrive $trustedModuleName2
            New-Item -ItemType Directory $trustedModulePath2
            $trustedModuleFilePath2 = Join-Path $trustedModulePath2 ($trustedModuleName2 + ".psm1")

            $trustedModuleScript1 = @'
            function PublicFn1
            {
                NestedFn1
                PrivateFn1
            }
            function PrivateFn1
            {
                "PrivateFn1"
            }
'@
            $trustedModuleScript1 | Out-File -FilePath $trustedModuleFilePath1
            '@{{ FunctionsToExport = "PublicFn1"; ModuleVersion = "1.0"; RootModule = "{0}"; NestedModules = "{1}" }}' -f @($trustedModuleFilePath1,$trustedModuleName2) | Out-File -FilePath $trustedModuleManifestPath1

            $trustedModuleScript2 = @'
            function NestedFn1
            {
                "NestedFn1"
                "Language mode is $($ExecutionContext.SessionState.LanguageMode)"
            }
'@
            $trustedModuleScript2 | Out-File -FilePath $trustedModuleFilePath2
        }

        AfterAll {

            $env:PSModulePath = $restorePSModulePath
            if ($trustedModuleName1 -ne $null) { Remove-Module -Name $trustedModuleName1 -Force -ErrorAction Ignore }
            if ($trustedModuleName2 -ne $null) { Remove-Module -Name $trustedModuleName2 -Force -ErrorAction Ignore }
        }

        It "Verifies that private functions in trusted nested modules are not globally accessible after running the help function" {

            $isCommandAccessible = "False"
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $command = @"
                Import-Module -Name $trustedModuleName1 -Force -ErrorAction Stop;
"@
                $command += @'
                $null = help -Name NestedFn1 -Category Function 2> $null;
                $result = Get-Command NestedFn1 2> $null;
                return ($result -ne $null)
'@
                $isCommandAccessible = pwsh.exe -noprofile -nologo -c $command
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            # Verify that nested function NestedFn1 was not accessible
            $isCommandAccessible | Should -BeExactly "False"
        }
    }

    Describe "NoLanguage runspace pool session should remain in NoLanguage mode when created on a system-locked down machine" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $configFileName = "RestrictedSessionConfig.pssc"
            $configFilePath = Join-Path $TestDrive $configFileName
            '@{ SchemaVersion = "2.0.0.0"; SessionType = "RestrictedRemoteServer"}' > $configFilePath

            $scriptModuleName = "ImportTrustedModuleForTest_System32"
            $moduleFilePath = Join-Path $TestDrive ($scriptModuleName + ".psm1")
            $template = @'
            function TestRestrictedSession
            {{
                $iss = [initialsessionstate]::CreateFromSessionConfigurationFile("{0}")
                $rsp = [runspacefactory]::CreateRunspacePool($iss)
                $rsp.Open()
                $ps = [powershell]::Create()
                $ps.RunspacePool = $rsp
                $null = $ps.AddScript("Hello")

                try
                {{
                    $ps.Invoke()
                }}
                finally
                {{
                    $ps.Dispose()
                    $rsp.Dispose()
                }}
            }}

            Export-ModuleMember -Function TestRestrictedSession
'@
            $template -f $configFilePath > $moduleFilePath
        }

        AfterAll {
            Remove-Module $scriptModuleName -Force -ErrorAction SilentlyContinue
        }

        It "Verifies that a NoLanguage runspace pool throws the expected 'script not allowed' error" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $mod = Import-Module -Name $moduleFilePath -Force -PassThru

                # Running module function TestRestrictedSession should throw a 'script not allowed' error
                # because it runs in a 'no language' session.
                try
                {
                    & "$scriptModuleName\TestRestrictedSession"
                    throw "No Exception!"
                }
                catch
                {
                    $expectedError = $_
                }
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly "ScriptsNotAllowed"
        }
    }

    Describe "Built-ins work within constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {
            $TestCasesBuiltIn = @(
                @{testName = "Verify built-in function"; scriptblock = { Get-Verb } }
                @{testName = "Verify built-in error variable"; scriptblock = { Write-Error SomeError -ErrorVariable ErrorOutput -ErrorAction SilentlyContinue; $ErrorOutput} }
            )
        }

        It "<testName>" -TestCases $TestCasesBuiltIn {

            param ($scriptblock)

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $result = (& $scriptblock)
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $result.Count | Should -BeGreaterThan 0
        }
    }

    Describe "Background jobs" -Tags 'Feature','RequireAdminOnWindows' {

        Context "Background jobs in system lock down mode" {

            It "Verifies that background jobs in system lockdown mode run in constrained language" {

                try
                {
                    Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                    $job = Start-Job -ScriptBlock { [object]::Equals("A", "B") } | Wait-Job
                    $expectedErrorId = $job.ChildJobs[0].Error.FullyQualifiedErrorId
                    $job | Remove-Job
                }
                finally
                {
                    Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                }

                $expectedErrorId | Should -BeExactly "MethodInvocationNotSupportedInConstrainedLanguage"
            }
        }

        Context "Background jobs within inconsistent mode" {

            It "Verifies that background job is denied when mode is inconsistent" {

                try
                {
                    $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                    Start-Job { [object]::Equals("A", "B") }
                    throw "No Exception!"
                }
                catch
                {
                    $expectedError = $_
                }
                finally
                {
                    Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
                }

                $expectedError.FullyQualifiedErrorId | Should -BeExactly "CannotStartJobInconsistentLanguageMode,Microsoft.PowerShell.Commands.StartJobCommand"
            }
        }
    }

    Describe "Add-Type in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies Add-Type fails in constrained language mode" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Add-Type -TypeDefinition 'public class ConstrainedLanguageTest { public static string Hello = "HelloConstrained"; }'
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "CannotDefineNewType,Microsoft.PowerShell.Commands.AddTypeCommand"
        }

        It "Verifies Add-Type works back in full language mode again" {
            Add-Type -TypeDefinition 'public class AfterFullLanguageTest { public static string Hello = "HelloAfter"; }'
            [AfterFullLanguageTest]::Hello | Should -BeExactly "HelloAfter"
        }
    }

    Describe "New-Object in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        Context "New-Object with dotNet types" {

            It "Verifies New-Object works in constrained language of allowed string type" {

                try
                {
                    $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                    $resultString = New-Object System.String "Hello"
                }
                finally
                {
                    Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
                }

                $resultString | Should -Be "Hello"
            }

            It "Verifies New-Object throws error in constrained language for disallowed IntPtr type" {

                try
                {
                    $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                    New-Object System.IntPtr 1234
                    throw "No Exception!"
                }
                catch
                {
                    $expectedError = $_
                }
                finally
                {
                    Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
                }

                $expectedError.FullyQualifiedErrorId | Should -BeExactly "CannotCreateTypeConstrainedLanguage,Microsoft.PowerShell.Commands.NewObjectCommand"
            }

            It "Verifies New-Object works for IntPtr type back in full language mode again" {

                New-Object System.IntPtr 1234 | Should -Be 1234
            }
        }

        Context "New-Object with COM types" {

            It "Verifies New-Object with COM types is disallowed in system lock down" {

                try
                {
                    $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                    Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                    New-Object -Com ADODB.Parameter
                    throw "No Exception!"
                }
                catch
                {
                    $expectedError = $_
                }
                finally
                {
                    Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                }

                $expectedError.FullyQualifiedErrorId | Should -BeExactly "CannotCreateComTypeConstrainedLanguage,Microsoft.PowerShell.Commands.NewObjectCommand"
            }

            It "Verifies New-Object with COM types works back in full language mode again" {

                $result = New-Object -ComObject ADODB.Parameter
                $result.Direction | Should -Be 1
            }
        }
    }

    Describe "New-Item command on function drive in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies New-Item directory on function drive is not allowed in constrained language mode" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                $null = New-Item -Path function:\SomeEvilFunction -ItemType Directory -Value SomeBadScriptBlock -ErrorAction Stop
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "NotSupported,Microsoft.PowerShell.Commands.NewItemCommand"
        }
    }

    Describe "Engine events in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies engine event in constrained language mode, its action runs as constrained" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $job = Register-EngineEvent LockdownEvent -Action { [object]::Equals("A", "B") }
                $null = New-Event LockdownEvent
                Wait-Job $job
                Unregister-Event LockdownEvent
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $job.Error.FullyQualifiedErrorId | Should -Match "MethodInvocationNotSupportedInConstrainedLanguage"
        }
    }

    Describe "Module scope scripts in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that while in constrained language mode script run in a module scope also runs constrained" {
            Import-Module PSDiagnostics
            $module = Get-Module PSDiagnostics

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & $module { [object]::Equals("A", "B") }
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
                Remove-Module PSDiagnostics -Force -ErrorAction SilentlyContinue
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "CantInvokeCallOperatorAcrossLanguageBoundaries"
        }
    }

    Describe "Switch -file in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that switch -file will not work in constrained language without provider" {

            [initialsessionstate] $iss = [initialsessionstate]::Create()
            $iss.LanguageMode = "ConstrainedLanguage"
            [runspace] $rs = [runspacefactory]::CreateRunspace($iss)
            $rs.Open()
            $pl = $rs.CreatePipeline("switch -file $testDrive/foo.txt { 'A' { 'B' } }")

            $e = { $pl.Invoke() } | Should -Throw -ErrorId "DriveNotFoundException"

            $rs.Dispose()
        }
    }

    Describe "Get content syntax in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that the get content syntax returns null value in constrained language without provider" {

            $iss = [initialsessionstate]::Create()
            $iss.LanguageMode = "ConstrainedLanguage"
            $rs = [runspacefactory]::CreateRunspace($iss)
            $rs.Open()
            $pl = $rs.CreatePipeline('${' + "$testDrive/foo.txt}")

            $result = $pl.Invoke()
            $rs.Dispose()

            $result[0] | Should -BeNullOrEmpty
        }
    }

    Describe "Stream redirection in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that stream redirection doesn't work in constrained language mode without provider" {

            $iss = [initialsessionstate]::CreateDefault2()
            $iss.Providers.Clear()
            $iss.LanguageMode = "ConstrainedLanguage"
            $rs = [runspacefactory]::CreateRunspace($iss)
            $rs.Open()
            $pl = $rs.CreatePipeline('"Hello" > c:\temp\foo.txt')

            $e = { $pl.Invoke() } | Should -Throw -ErrorId "DriveNotFoundException"

            $rs.Dispose()
        }
    }

    Describe "Invoke-Expression in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            function VulnerableFunctionFromFullLanguage { Invoke-Expression $args[0] }

            $TestCasesIEX = @(
                @{testName = "Verifies direct Invoke-Expression does not bypass constrained language mode";
                  scriptblock = { Invoke-Expression '[object]::Equals("A", "B")' } }
                @{testName = "Verifies indirect Invoke-Expression does not bypass constrained language mode";
                  scriptblock = { VulnerableFunctionFromFullLanguage '[object]::Equals("A", "B")' } }
            )
        }

        It "<testName>" -TestCases $TestCasesIEX {

            param ($scriptblock)

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & $scriptblock
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "MethodInvocationNotSupportedInConstrainedLanguage,Microsoft.PowerShell.Commands.InvokeExpressionCommand"
        }
    }

    Describe "Dynamic method invocation in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies dynamic method invocation does not bypass constrained language mode" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & {
                    $type = [IO.Path]
                    $method = "GetRandomFileName"
                    $type::$method()
                }
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "MethodInvocationNotSupportedInConstrainedLanguage"
        }

        It "Verifies dynamic methods invocation does not bypass constrained language mode" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & {
                    $type = [IO.Path]
                    $methods = "GetRandomFileName","GetTempPath"
                    $type::($methods[0])()
                }
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "MethodInvocationNotSupportedInConstrainedLanguage"
        }
    }

    Describe "Conversion in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that PowerShell cannot convert disallowed IntPtr type" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                $result = 1234 -as [IntPtr]
                $null -eq $result | Should -BeTrue
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }
        }
    }

    Describe "Variable AllScope in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies Set-Variable cannot create AllScope in constrained language" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Set-Variable -Name SetVariableAllScopeNotSupported -Value bar -Option AllScope
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "NotSupported,Microsoft.PowerShell.Commands.SetVariableCommand"
        }

        It "Verifies New-Variable cannot create AllScope in constrained language" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                New-Variable -Name NewVarialbeAllScopeNotSupported -Value bar -Option AllScope
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "NotSupported,Microsoft.PowerShell.Commands.NewVariableCommand"
        }
    }

    Describe "Data section additional commands in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        function InvokeDataSectionConstrained
        {
            try
            {
                Invoke-Expression 'data foo -SupportedCommand Add-Type { Add-Type }'
                throw "No Exception!"
            }
            catch
            {
                return $_
            }
        }

        It "Verifies data section Add-Type additional command is disallowed in constrained language" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                $exception1 = InvokeDataSectionConstrained
                # Repeat to make sure the first time properly restored the language mode to constrained.
                $exception2 = InvokeDataSectionConstrained
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $exception1.FullyQualifiedErrorId | Should -Match "DataSectionAllowedCommandDisallowed"
            $exception2.FullyQualifiedErrorId | Should -Match "DataSectionAllowedCommandDisallowed"
        }

        It "Verifies data section with no-constant expression Add-Type additional command is disallowed in constrained language" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                $addedCommand = "Add-Type"
                Invoke-Expression 'data foo -SupportedCommand $addedCommand { Add-Type }'
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "DataSectionAllowedCommandDisallowed,Microsoft.PowerShell.Commands.InvokeExpressionCommand"
        }
    }

    Describe "Add-Type in no language mode on locked down system" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies Add-Type fails in no language mode when in system lock down" {

            # Create No-Language session, that allows Add-Type cmdlet
            $entry = [System.Management.Automation.Runspaces.SessionStateCmdletEntry]::new('Add-Type', [Microsoft.PowerShell.Commands.AddTypeCommand], $null)
            $iss = [initialsessionstate]::CreateRestricted([System.Management.Automation.SessionCapabilities]::Language)
            $iss.Commands.Add($entry)
            $rs = [runspacefactory]::CreateRunspace($iss)
            $rs.Open()

            # Try to use Add-Type in No-Language session
            $ps = [powershell]::Create($rs)
            $ps.AddCommand('Add-Type').AddParameter('TypeDefinition', 'public class C1 { }')
            $expectedError = $null
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ps.Invoke()
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
                $rs.Dispose()
                $ps.Dispose()
            }

            $expectedError.Exception.InnerException.ErrorRecord.FullyQualifiedErrorId | Should -BeExactly 'CannotDefineNewType,Microsoft.PowerShell.Commands.AddTypeCommand'
        }
    }

    Describe "Import-LocalizedData additional commands in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies Import-LocalizedData disallows Add-Type in constrained language" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & {
                    $localizedDataFileName = Join-Path $TestDrive ImportLocalizedDataAdditionalCommandsNotSupported.psd1
                    $null = New-Item -ItemType File -Path $localizedDataFileName -Force
                    Import-LocalizedData -SupportedCommand Add-Type -BaseDirectory $TestDrive -FileName ImportLocalizedDataAdditionalCommandsNotSupported
                }
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "CannotDefineSupportedCommand,Microsoft.PowerShell.Commands.ImportLocalizedData"
        }
    }

    Describe "Where and Foreach operators should not allow unapproved types in constrained language" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $script1 = @'
                $data = @(
                    @{
                        Node = "first"
                        Value1 = 1
                        Value2 = 2
                        first = $true
                    }
                    @{
                        Node = "second"
                        Value1 = 3
                        Value2 = 4
                        Second = $true
                    }
                    @{
                        Node = "third"
                        Value1 = 5
                        Value2 = 6
                        third = $true
                    }
                )

                $result = $data.where{$_.Node -eq "second"}
                Write-Output $result

                # Execute method in scriptblock of where operator, should throw in ConstrainedLanguage mode.
                $data.where{[system.io.path]::GetRandomFileName() -eq "Hello"}
'@

            $script2 = @'
                $data = @(
                    @{
                        Node = "first"
                        Value1 = 1
                        Value2 = 2
                        first = $true
                    }
                    @{
                        Node = "second"
                        Value1 = 3
                        Value2 = 4
                        Second = $true
                    }
                    @{
                        Node = "third"
                        Value1 = 5
                        Value2 = 6
                        third = $true
                    }
                )

                $result = $data.ForEach('value1')
                Write-Output $result

                # Execute method in scriptblock of ForEach operator, should throw in ConstrainedLanguage mode.
                $data.ForEach{[system.io.path]::GetRandomFileName().Length}
'@

            $script3 = @'
            # Method call should throw error.
            (Get-Process powershell*).ForEach('GetHashCode')
'@

            $script4 = @'
            # Where method call should throw error.
            (get-process powershell).where{$_.GetType().FullName -match "process"}
'@

            $TestCasesForeach = @(
                @{testName = "Verify where statement with invalid method call in constrained language is disallowed"; script = $script1 }
                @{testName = "Verify foreach statement with invalid method call in constrained language is disallowed"; script = $script2 }
                @{testName = "Verify foreach statement with embedded method call in constrained language is disallowed"; script = $script3 }
                @{testName = "Verify where statement with embedded method call in constrained language is disallowed"; script = $script4 }
            )
        }

        It "<testName>" -TestCases $TestCasesForeach {

            param (
                [string] $script
            )

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                & {
                    # Scriptblock must be created inside constrained language.
                    $sb = [scriptblock]::Create($script)
                    & sb
                }
                throw "No Exception!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "MethodInvocationNotSupportedInConstrainedLanguage"
        }
    }

    Describe "ThreadJob Constrained Language Tests" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $sb = { $ExecutionContext.SessionState.LanguageMode }
        }

        It "ThreadJob script must run in ConstrainedLanguage mode with system lock down" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = Start-ThreadJob -ScriptBlock { $ExecutionContext.SessionState.LanguageMode } | Wait-Job | Receive-Job
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $results | Should -BeExactly "ConstrainedLanguage"
        }

        It "ThreadJob script block using variable must run in ConstrainedLanguage mode with system lock down" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = Start-ThreadJob -ScriptBlock { & $using:sb } | Wait-Job | Receive-Job
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $results | Should -BeExactly "ConstrainedLanguage"
        }

        It "ThreadJob script block argument variable must run in ConstrainedLanguage mode with system lock down" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = Start-ThreadJob -ScriptBlock { param ($sb) & $sb } -ArgumentList $sb | Wait-Job | Receive-Job
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $results | Should -BeExactly "ConstrainedLanguage"
        }

        It "ThreadJob script block piped variable must run in ConstrainedLanguage mode with system lock down" {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = $sb | Start-ThreadJob -ScriptBlock { $input | ForEach-Object { & $_ } } | Wait-Job | Receive-Job
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $results | Should -BeExactly "ConstrainedLanguage"
        }
    }

    Describe "ForEach-Object -Parallel Constrained Language Tests" -Tags 'Feature','RequireAdminOnWindows' {

        It 'Foreach-Object -Parallel must run in ConstrainedLanguage mode under system lock down' {

            try
            {
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode

                $results = 1..1 | ForEach-Object -Parallel { $ExecutionContext.SessionState.LanguageMode }
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $results | Should -BeExactly "ConstrainedLanguage"
        }
    }

    Describe "Dot sourced script block functions from trusted script files should not run FullLanguage in ConstrainedLanguage context" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $scriptFileName = "TrustedScriptBlockTest_System32"
            $scriptFilePath = Join-Path $TestDrive ($scriptFileName + ".ps1")
            @'
            function TrustedFn {
                Write-Output $ExecutionContext.SessionState.LanguageMode
            }
'@ | Out-File -FilePath $scriptFilePath

            $scriptModuleName = "UntrustedModuleScriptBlockTest"
            $scriptModulePath = Join-Path $TestDrive ($scriptModuleName + ".psm1")
            @'
            function RunScriptBlock {{
                $sb = (Get-Command -Name {0}).ScriptBlock

                # ScriptBlock trusted function, TrustedFn, is dot sourced into current scope
                1 | ForEach-Object $sb
                TrustedFn
            }}
'@ -f $scriptFilePath | Out-File -FilePath $scriptModulePath
        }

        AfterAll {
            Remove-Module $scriptModuleName -Force -ErrorAction SilentlyContinue
        }

        It "Verifies a scriptblock from a trusted script file does not run as trusted" {
            if (Test-IsWindowsArm64) {
                Set-ItResult -Pending -Because "https://github.com/PowerShell/PowerShell/issues/20169"
                return
            }

            $result = $null

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                # Wait for the lockdown mode to take effect
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                # Import untrusted module
                Import-Module -Name $scriptModulePath -Force

                # Run module function that dot sources TrustedFn and runs it in module scope
                $result = RunScriptBlock
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            # Ensure scriptblock TrustedFn function ran as untrusted
            $result | Should -BeExactly "ConstrainedLanguage"
        }
    }

    Describe "Dot sourcing trusted script in ConstrainedLanguage context is allowed when importing modules" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $importModuleName = "ToImportTrustedModuleTest_System32"
            $importModulePath = Join-Path $TestDrive ($importModuleName + ".psm1")
            $modScript = @'
            function ImportModuleFn { "ImportModuleFn: $($ExecutionContext.SessionState.LanguageMode)" }
            Export-ModuleMember -Function "ImportModuleFn"
'@ | Out-File -FilePath $importModulePath

            $scriptModuleName = "ImportTrustedModuleTest_System32"
            $scriptModulePath = Join-Path $TestDrive ($scriptModuleName + ".psm1")
            @'
            Import-Module -Name {0} -Force
            function ModuleFn {{ "ModuleFn: $($ExecutionContext.SessionState.LanguageMode)" }}
            Export-ModuleMember -Function "ModuleFn","ImportModuleFn"
'@ -f $importModulePath | Out-File -FilePath $scriptModulePath
        }

        AfterAll {
            Remove-Module $importModuleName -Force -ErrorAction SilentlyContinue
            Remove-Module $scriptModuleName -Force -ErrorAction SilentlyContinue
        }

        It "Verifies that trusted module functions run in FullLanguage" {

            $result1 = $null
            $result2 = $null

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Import-Module -Name $scriptModulePath -Force


                $result1 = ModuleFn
                $result2 = ImportModuleFn
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $result1 | Should -BeExactly "ModuleFn: FullLanguage"
            $result2 | Should -BeExactly "ImportModuleFn: FullLanguage"
        }
    }

    Describe "PowerShell classes are not allowed in constrained language mode" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $randomClassName = "class_$(Get-Random -Max 9999)"

            $script = "class ${randomClassName} { static [string] GetLanguageMode() { return (Get-Variable -ValueOnly -Name ExecutionContext).SessionState.LanguageMode } }"

            $modulePathName = "modulePath_$(Get-Random -Max 9999)"
            $modulePath = Join-Path $testdrive $modulePathName
            New-Item -Path $modulePath -ItemType Directory -Force

            $untrustedScriptFile = Join-Path $modulePath "T1ScriptClass.ps1"
            $script | Out-File -FilePath $untrustedScriptFile

            $untrustedScriptModule = Join-Path $modulePath "T1ScriptClass.psm1"
            $script | Out-File -FilePath $untrustedScriptModule

            $trustedScriptFile = Join-Path $modulePath "T1ScriptClass_System32.ps1"
            $script | Out-File -FilePath $trustedScriptFile

            $trustedScriptModule = Join-Path $modulePath "T1ScriptClass_System32.psm1"
            $script | Out-File -FilePath $trustedScriptModule
        }

        AfterAll {

            Remove-Module -Name T1ScriptClass_System32 -Force -ErrorAction Ignore
            Remove-Module -Name T1ScriptClass -Force -ErrorAction Ignore
        }

        It "Verifies that classes cannot be created in script running under constrained language" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Invoke-Expression -Command $script 2>$null -ErrorAction Stop
                throw "No Error!"
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly "ClassesNotAllowedInConstrainedLanguage,Microsoft.PowerShell.Commands.InvokeExpressionCommand"
        }

        It "Verifies that classes cannot be created in script files running under constrained language" {

            try {
                $ps = [powershell]::Create("NewRunspace")
                $ps.Runspace.LanguageMode = "ConstrainedLanguage"
                $result = $ps.AddScript($untrustedScriptFile).Invoke()
                $ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly  "ClassesNotAllowedInConstrainedLanguage" -Because "Invoke-Command should fail in constrained language"
            }
            catch {
                $_ | Should -BeNullOrEmpty -Because "exception '$_' unexpected."
            }
            finally {
                $ps.Dispose()
            }
        }

        It "Verifies that classes cannot be created in untrusted script modules running under constrained language" {
            try {
                $ps = [powershell]::Create("NewRunspace")
                $ps.Runspace.LanguageMode = "ConstrainedLanguage"
                # importing the module whilst in constrained language makes it untrusted, even without lockdown mode
                $ps.AddCommand("Import-Module").AddParameter("Name", $untrustedScriptModule).Invoke()
                $ps.Streams.Error[0].FullyQualifiedErrorId | Should -BeExactly  "ClassesNotAllowedInConstrainedLanguage" -Because "Import-Module should fail in constrained language"
            }
            catch {
                $_ | Should -BeNullOrEmpty -Because "exception '$_' unexpected."
            }
            finally {
                $ps.Dispose()
            }
        }

        It "Verifies that classes can be created in trusted script files running under constrained language" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                { & ($trustedScriptFile) } | Should -Not -Throw
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode -RevertLockdownMode
            }
        }

        It "Verifies that classes can be created in trusted script modules running under constrained language" {

            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                { Import-Module -Name $trustedScriptModule -ErrorAction Stop } | Should -Not -Throw
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -EnableFullLanguageMode -RevertLockdownMode
            }
        }
    }

    Describe "Invoke-History should not run command lines in FullLanguage mode when system is locked down" -Tags 'Feature','RequireAdminOnWindows' {

        BeforeAll {

            $LanguageModeHistoryFilePath = Join-Path $TestDrive "LanguageModeHistory.XML"

            # $ExecutionContext.SessionState.LanguageMode command line history item clixml
            @'
            <Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
                <Obj RefId="0">
                <TN RefId="0">
                    <T>Microsoft.PowerShell.Commands.HistoryInfo</T>
                    <T>System.Object</T>
                </TN>
                <ToString>$ExecutionContext.SessionState.LanguageMode</ToString>
                <Props>
                    <I64 N="Id">123</I64>
                    <S N="CommandLine">$ExecutionContext.SessionState.LanguageMode</S>
                    <Obj N="ExecutionStatus" RefId="1">
                    <TN RefId="1">
                        <T>System.Management.Automation.Runspaces.PipelineState</T>
                        <T>System.Enum</T>
                        <T>System.ValueType</T>
                        <T>System.Object</T>
                    </TN>
                    <ToString>Completed</ToString>
                    <I32>4</I32>
                    </Obj>
                    <DT N="StartExecutionTime">2018-07-26T14:36:33.923608-07:00</DT>
                    <DT N="EndExecutionTime">2018-07-26T14:36:33.9266018-07:00</DT>
                </Props>
                </Obj>
            </Objs>
'@ | Out-File -FilePath $LanguageModeHistoryFilePath

            $historyItem = Import-Clixml -Path $LanguageModeHistoryFilePath
        }

        It "Verifies that Invoke-History runs command lines in ConstrainedLanguage" {

            $result = $null
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                # Add "$ExecutionContext.SessionState.LanguageMode" command line to history
                $historyItem | Add-History

                # Retrieve history item command and invoke
                $retrievedItem = Get-History -Count 1
                $result = $retrievedItem | Invoke-History
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $result | Should -BeExactly "ConstrainedLanguage"
        }
    }

    Describe "Enter-PSHostProcess cmdlet should be disabled on locked down systems" -Tags 'Feature','RequireAdminOnWindows' {

        It "Verifies that Enter-PSHostProcess is disabled with lock down policy" {

            $expectedError = $null
            try
            {
                Invoke-LanguageModeTestingSupportCmdlet -SetLockdownMode
                $ExecutionContext.SessionState.LanguageMode = "ConstrainedLanguage"

                Enter-PSHostProcess -Id 5555 -ErrorAction Stop
            }
            catch
            {
                $expectedError = $_
            }
            finally
            {
                Invoke-LanguageModeTestingSupportCmdlet -RevertLockdownMode -EnableFullLanguageMode
            }

            $expectedError.FullyQualifiedErrorId | Should -BeExactly 'EnterPSHostProcessCmdletDisabled,Microsoft.PowerShell.Commands.EnterPSHostProcessCommand'
        }
    }

    # End Describe blocks
}
finally
{
    if ($defaultParamValues -ne $null)
    {
        $Global:PSDefaultParameterValues = $defaultParamValues
    }
}
