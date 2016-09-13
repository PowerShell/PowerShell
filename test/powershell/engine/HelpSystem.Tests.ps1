#
# Validates Get-Help for cmdlets in Microsoft.PowerShell.Core.

function UpdateHelpFromLocalContentPath
{
    param ([string]$ModuleName, [string]$Tag = 'CI')

    # Update-Help is not yet supported on non-Windows platforms;
    # currently it is using Windows Cabinet API (cabinet.dll) internally
    if ($IsWindows)
    {
        if ($Tag -eq 'CI')
        {
            $helpContentPath = Join-Path $PSScriptRoot "HelpContent"
            $helpFiles = @(Get-ChildItem "$helpContentPath\*" -ea SilentlyContinue)

            if ($helpFiles.Count -eq 0)
            {
                throw "Unable to find help content at '$helpContentPath'"
            }

            Update-Help -Module $ModuleName -SourcePath $helpContentPath -Force -ErrorAction Stop -Verbose
        }
        else
        {
            Update-Help -Module $ModuleName -Force -Verbose -ErrorAction Stop
        }
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
        "Register-ArgumentCompleter"
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

Describe "Validate that get-help <cmdletName> works" -Tags "CI" {

    RunTestCase -tag "CI"
}

Describe "Validate Get-Help for all cmdlets in 'Microsoft.PowerShell.Core'" -Tags "Feature" {

    RunTestCase -tag "Feature"
}

Describe "Validate that Get-Help returns provider-specific help" -Tags "CI" {
    BeforeAll {
        $namespaces = @{
            command = 'http://schemas.microsoft.com/maml/dev/command/2004/10'
            dev     = 'http://schemas.microsoft.com/maml/dev/2004/10'
            maml    = 'http://schemas.microsoft.com/maml/2004/10'
            msh     = 'http://msh'
        }

        # Currently these test cases are verified only on Windows, because
        # - WSMan:\ and Cert:\ providers are not yet supported on non-Windows platforms.
        # - Update-Help is not yet supported on non-Windows platforms; it is using Windows Cabinet API (cabinet.dll) internally.
        $testCases = @(
            @{
                helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\System.Management.Automation.dll-help.xml"
                path        = "$PSHOME"
                helpContext = "[@id='FileSystem' or @ID='FileSystem']"
                verb        = 'Add'
                noun        = 'Content'
            },
            @{
                helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\Microsoft.PowerShell.Security.dll-help.xml"
                path        = 'Cert:\'
                helpContext = $null  # CertificateProvider uses only verb and noun in XPath query
                verb        = 'New'
                noun        = 'Item'
            },
            @{
                helpFile    = "$PSHOME\$([Globalization.CultureInfo]::CurrentUICulture)\Microsoft.WSMan.Management.dll-help.xml"
                path        = 'WSMan:\localhost\ClientCertificate'
                helpContext = "[@id='ClientCertificate' or @ID='ClientCertificate']"
                verb        = 'New'
                noun        = 'Item'
            }
        )

        UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.PowerShell.Core' -Tag 'CI'
        UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.PowerShell.Security' -Tag 'CI'
        UpdateHelpFromLocalContentPath -ModuleName 'Microsoft.WSMan.Management' -Tag 'CI'
    }

    It -Skip:(-not $IsWindows) "shows contextual help when Get-Help is invoked for provider-specific path (Get-Help -Name <verb>-<noun> -Path <path>)" -TestCases $testCases {
        param($helpFile, $path, $helpContext, $verb, $noun)

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
