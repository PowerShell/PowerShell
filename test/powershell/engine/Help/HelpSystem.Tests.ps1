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
        Update-Help -Module $ModuleName -Force -Verbose -ErrorAction Stop
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
