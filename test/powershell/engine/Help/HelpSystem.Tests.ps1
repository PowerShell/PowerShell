#
# Validates Get-Help for cmdlets in Microsoft.PowerShell.Core.

function UpdateHelpFromLocalContentPath
{
    param ([string]$ModuleName, [string]$Tag = 'CI')

    if ($Tag -eq 'CI')
    {
        $helpContentPath = Join-Path $PSScriptRoot "assets"
        $helpFiles = @(Get-ChildItem "$helpContentPath\*" -ea SilentlyContinue)

        if ($helpFiles.Count -eq 0)
        {
            throw "Unable to find help content at '$helpContentPath'"
        }

        Update-Help -Module $ModuleName -SourcePath $helpContentPath -Force -ErrorAction Stop
    }
    else
    {
        Update-Help -Module $ModuleName -Force -ErrorAction Stop
    }
}

function RunTestCase
{
    param ([string]$tag = "CI")

    $moduleName = "Microsoft.PowerShell.Core"

    UpdateHelpFromLocalContentPath $moduleName $tag

    $cmdlets = get-command -module $moduleName

    $cmdletsToSkip = @(
        "Get-PSHostProcessInfo",
        "Out-Default",
        "Register-ArgumentCompleter",
        "New-PSRoleCapabilityFile",
        "Get-PSSessionCapability"
    )

    foreach ($cmdletName in $cmdlets)
    {
        if ($cmdletsToSkip -notcontains $cmdletName)
        {
            It "Validate -Description and -Examples sections in help content. Run 'Get-help -name $cmdletName'" {

                $help = get-help -name $cmdletName
                $help.Description | Out-String | Should Match $cmdletName
                $help.Examples | Out-String | Should Match $cmdletName
            }

            if ($tag -eq "CI")
            {
                # For a CI test run, we are only interested in validating one cmdlet to ensure that
                # get-help <cmdletName> works.
                break
            }
        }
    }
}

Describe "Validate that <pshome>/<culture>/default.help.txt is present" -Tags @('CI') {

    It "Get-Help returns information about the help system." {

        $help = Get-Help
        $help.Name | Should Be "default"
        $help.Category | Should Be "HelpFile"
        $help.Synopsis | Should Match "SHORT DESCRIPTION"
    }
}

Describe "Validate that get-help <cmdletName> works" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }
    RunTestCase -tag "CI"
}

Describe "Validate Get-Help for all cmdlets in 'Microsoft.PowerShell.Core'" -Tags @('Feature', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"
    }
    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    RunTestCase -tag "Feature"
}

Describe "Validate that Get-Help returns provider-specific help" -Tags @('CI', 'RequireAdminOnWindows') {
    BeforeAll {
        $SavedProgressPreference = $ProgressPreference
        $ProgressPreference = "SilentlyContinue"

        $namespaces = @{
            command = 'http://schemas.microsoft.com/maml/dev/command/2004/10'
            dev     = 'http://schemas.microsoft.com/maml/dev/2004/10'
            maml    = 'http://schemas.microsoft.com/maml/2004/10'
            msh     = 'http://msh'
        }

        # Currently these test cases are verified only on Windows, because
        # - WSMan:\ and Cert:\ providers are not yet supported on non-Windows platforms.
        $testCases = @(
            @{
                helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\System.Management.Automation.dll-help.xml"
                path        = "$PSHOME"
                helpContext = "[@id='FileSystem' or @ID='FileSystem']"
                verb        = 'Add'
                noun        = 'Content'
                pending     =  !$IsWindows
            }
        )

        if ($IsWindows)
        {
            $testCases += @(
                @{
                    helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\Microsoft.WSMan.Management.dll-help.xml"
                    path        = 'WSMan:\localhost\ClientCertificate'
                    helpContext = "[@id='ClientCertificate' or @ID='ClientCertificate']"
                    verb        = 'New'
                    noun        = 'Item'
                    pending     = $false
                }
                ,
                @{
                    helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\Microsoft.PowerShell.Security.dll-help.xml"
                    path        = 'Cert:\'
                    helpContext = $null  # CertificateProvider uses only verb and noun in XPath query
                    verb        = 'New'
                    noun        = 'Item'
                    pending     = $false
                }
            )
            UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.WSMan.Management' -Tag 'CI'
            UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.PowerShell.Security' -Tag 'CI'
        }

        UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.PowerShell.Core' -Tag 'CI'
    }

    AfterAll {
        $ProgressPreference = $SavedProgressPreference
    }

    foreach ($helptest in $testCases)
    {
        $helpFile = $helptest.helpFile
        $path = $helptest.path
        $helpContext = $helptest.helpContext
        $verb = $helptest.verb
        $noun = $helptest.noun
        $pending = $helptest.pending


        It -Pending:$pending "Shows contextual help when Get-Help is invoked for provider-specific path (Get-Help -Name $verb-$noun -Path $path)" {

            # Path should exist or else Get-Help will fallback to default help text
            $path | Should Exist

            $xpath = "/msh:helpItems/msh:providerHelp/msh:CmdletHelpPaths/msh:CmdletHelpPath$helpContext/command:command/command:details[command:verb='$verb' and command:noun='$noun']"
            $helpXmlNode = Select-Xml -Path $helpFile -XPath $xpath -Namespace $namespaces | Select-Object -ExpandProperty Node

            # Synopsis comes from command:command/command:details/maml:description
            $expected = Get-Help -Name "$verb-$noun" -Path $path | Select-Object -ExpandProperty Synopsis

            # System.Management.Automation.ProviderContext.GetProviderSpecificHelpInfo ignores extra whitespace, line breaks and
            # comments when loading help XML, but Select-Xml can not; use BeLikeExactly operator to omit trailing line breaks:
            $helpXmlNode.description.para -clike "$expected*" | Should Be $true
        }
    }
}

Describe "Validate about_help.txt under culture specific folder works" -Tags @('CI') {
    BeforeAll {
        $modulePath = "$pshome\Modules\Test"
        $null = New-Item -Path $modulePath\en-US -ItemType Directory -Force
        New-ModuleManifest -Path $modulePath\test.psd1 -RootModule test.psm1
        Set-Content -Path $modulePath\test.psm1 -Value "function foo{}"
        Set-Content -Path $modulePath\en-US\about_testhelp.help.txt -Value "Hello" -NoNewline
        ## This is needed for getting about topics. We use -Force, so we always update.
        Update-Help -Force
    }

    AfterAll {
        Remove-Item $modulePath -Recurse -Force
        # Remove all the help content.
        Get-ChildItem -Path $PSHOME -Include @('about_*.txt', "*help.xml") -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
    }

    It "Get-Help should return help text and not multiple HelpInfo objects when help is under `$pshome path" {

        $help = Get-Help about_testhelp
        $help.count | Should Be 1
        $help | Should BeExactly "Hello"
    }

    It "Get-Help for about_Variable should return only one help object" {

        $help = Get-Help about_Variables
        $help.count | Should Be 1
    }
}

Describe "Get-Help should find help info within help files" -Tags @('CI', 'RequireAdminOnWindows') {
    It "Get-Help should find help files under pshome" {
        $helpFile = "about_testCase.help.txt"
        $culture = (Get-Culture).Name
        $helpFolderPath = Join-Path $PSHOME $culture
        $helpFilePath = Join-Path $helpFolderPath $helpFile

        if (!(Test-Path $helpFolderPath))
        {
            $null = New-Item -ItemType Directory -Path $helpFolderPath -ErrorAction SilentlyContinue
        }

        try
        {
            $null = New-Item -ItemType File -Path $helpFilePath -Value "about_test" -ErrorAction SilentlyContinue
            $helpContent = Get-Help about_testCase
            $helpContent | Should Match "about_test"
        }
        finally
        {
            Remove-Item $helpFilePath -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "Get-Help should find pattern help files" -Tags "CI" {

    # There is a bug specific to Travis CI that suspends the test if "get-help" is used to search pattern string. This doesn't repro locally.
    # This occurs even if Unix system just returns "Directory.GetFiles(path, pattern);" as the windows' code does.
    # Since there's currently no way to get the vm from Travis CI and the test PASSES locally on both Ubuntu and MacOS, excluding pattern test under Unix system.

  BeforeAll {
    $helpFile1 = "about_testCase1.help.txt"
    $helpFile2 = "about_testCase.2.help.txt"
    $culture = (Get-Culture).Name
    $helpFolderPath = Join-Path $PSHOME $culture
    $helpFilePath1 = Join-Path $helpFolderPath $helpFile1
    $helpFilePath2 = Join-Path $helpFolderPath $helpFile2
    $null = New-Item -ItemType Directory -Path $helpFolderPath -ErrorAction SilentlyContinue -Force
    # Create at least one help file matches "about*" pattern
    $null = New-Item -ItemType File -Path $helpFilePath1 -Value "about_test1" -ErrorAction SilentlyContinue
    $null = New-Item -ItemType File -Path $helpFilePath2 -Value "about_test2" -ErrorAction SilentlyContinue
  }

  # Remove the test files
  AfterAll {
    Remove-Item $helpFilePath1 -Force -ErrorAction SilentlyContinue
    Remove-Item $helpFilePath2 -Force -ErrorAction SilentlyContinue
  }

  $testcases = @(
                  @{command = {Get-Help about_testCas?1}; testname = "test ? pattern"; result = "about_test1"}
                  @{command = {Get-Help about_testCase.?}; testname = "test ? pattern with dot"; result = "about_test2"}
                  @{command = {(Get-Help about_testCase*).Count}; testname = "test * pattern"; result = "2"}
                  @{command = {Get-Help about_testCas?.2*}; testname = "test ?, * pattern with dot"; result = "about_test2"}
               )

    It "Get-Help should find pattern help files - <testname>" -TestCases $testcases -Pending: (-not $IsWindows){
            param (
            $command,
            $result
        )
        $command.Invoke() | Should Be $result
    }
}

Describe "Get-Help should find pattern alias" -Tags "CI" {
    # Remove test alias
    AfterAll {
        Remove-Item alias:\testAlias1 -ErrorAction SilentlyContinue
    }

    It "Get-Help should find alias as command" {
       (Get-Help where).Name | Should BeExactly "Where-Object"
    }

    It "Get-Help should find alias with ? pattern" {
       $help = Get-Help wher?
       $help.Category | Should BeExactly "Alias"
       $help.Synopsis | Should BeExactly "Where-Object"
    }

    It "Get-Help should find alias with * pattern" {
       Set-Alias -Name testAlias1 -Value Where-Object
       $help = Get-Help testAlias1*
       $help.Category | Should BeExactly "Alias"
       $help.Synopsis | Should BeExactly "Where-Object"
    }
}

Describe "help function uses full view by default" -Tags "CI" {
    It "help should return full view without -Full switch" {
        $gpsHelp = (help Microsoft.PowerShell.Management\Get-Process)
        $gpsHelp | Where-Object {$_ -cmatch '^PARAMETERS'} | Should Not BeNullOrEmpty
    }

    It "help should return full view even with -Full switch" {
        $gpsHelp = (help Microsoft.PowerShell.Management\Get-Process -Full)
        $gpsHelp | Where-Object {$_ -cmatch '^PARAMETERS'} | Should Not BeNullOrEmpty
    }

    It "help should not append -Full when not using AllUsersView parameter set" {
        $gpsHelp = (help Microsoft.PowerShell.Management\Get-Process -Parameter Name)
        $gpsHelp | Where-Object {$_ -cmatch '^PARAMETERS'} | Should BeNullOrEmpty
    }
}
