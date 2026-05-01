#
# Copyright (c) Microsoft Corporation.
#
# THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
# IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
# FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
# AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
# LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
# OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
# THE SOFTWARE.
#

$resourceModuleRoot = Split-Path -Path (Split-Path -Path $PSScriptRoot -Parent) -Parent

# Import localization helper functions.
$helperName = 'PowerShellGet.LocalizationHelper'
$dscResourcesFolderFilePath = Join-Path -Path $resourceModuleRoot -ChildPath "Modules\$helperName\$helperName.psm1"
Import-Module -Name $dscResourcesFolderFilePath

$script:localizedData = Get-LocalizedData -ResourceName 'MSFT_PSModule' -ScriptRoot $PSScriptRoot

# Import resource helper functions.
$helperName = 'PowerShellGet.ResourceHelper'
$dscResourcesFolderFilePath = Join-Path -Path $resourceModuleRoot -ChildPath "Modules\$helperName\$helperName.psm1"
Import-Module -Name $dscResourcesFolderFilePath

<#
    .SYNOPSIS
        This DSC resource provides a mechanism to download PowerShell modules from the PowerShell
        Gallery and install it on your computer.

        Get-TargetResource returns the current state of the resource.

    .PARAMETER Name
        Specifies the name of the PowerShell module to be installed or uninstalled.

    .PARAMETER Repository
        Specifies the name of the module source repository where the module can be found.

    .PARAMETER RequiredVersion
        Provides the version of the module you want to install or uninstall.

    .PARAMETER MaximumVersion
        Provides the maximum version of the module you want to install or uninstall.

    .PARAMETER MinimumVersion
        Provides the minimum version of the module you want to install or uninstall.

    .PARAMETER Force
        Forces the installation of modules. If a module of the same name and version already exists on the computer,
        this parameter overwrites the existing module with one of the same name that was found by the command.

    .PARAMETER AllowClobber
        Allows the installation of modules regardless of if other existing module on the computer have cmdlets
        of the same name.

    .PARAMETER SkipPublisherCheck
        Allows the installation of modules that have not been catalog signed.
#>
function Get-TargetResource {
    <#
        These suppressions are added because this repository have other Visual Studio Code workspace
        settings than those in DscResource.Tests DSC test framework.
        Only those suppression that contradict this repository guideline is added here.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-ForEachStatement', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-FunctionBlockBraces', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-IfStatement', '')]
    [CmdletBinding()]
    [OutputType([System.Collections.Hashtable])]
    param
    (
        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [System.String]
        $Repository = 'PSGallery',

        [Parameter()]
        [System.String]
        $RequiredVersion,

        [Parameter()]
        [System.String]
        $MaximumVersion,

        [Parameter()]
        [System.String]
        $MinimumVersion,

        [Parameter()]
        [System.Boolean]
        $Force,

        [Parameter()]
        [System.Boolean]
        $AllowClobber,

        [Parameter()]
        [System.Boolean]
        $SkipPublisherCheck
    )

    $returnValue = @{
        Ensure             = 'Absent'
        Name               = $Name
        Repository         = $Repository
        Description        = $null
        Guid               = $null
        ModuleBase         = $null
        ModuleType         = $null
        Author             = $null
        InstalledVersion   = $null
        RequiredVersion    = $RequiredVersion
        MinimumVersion     = $MinimumVersion
        MaximumVersion     = $MaximumVersion
        Force              = $Force
        AllowClobber       = $AllowClobber
        SkipPublisherCheck = $SkipPublisherCheck
        InstallationPolicy = $null
    }

    Write-Verbose -Message ($localizedData.GetTargetResourceMessage -f $Name)

    $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
        -ArgumentNames ('Name', 'Repository', 'MinimumVersion', 'MaximumVersion', 'RequiredVersion')

    # Get the module with the right version and repository properties.
    $modules = Get-RightModule @extractedArguments -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

    # If the module is found, the count > 0
    if ($modules.Count -gt 0) {
        Write-Verbose -Message ($localizedData.ModuleFound -f $Name)

        # Find a module with the latest version and return its properties.
        $latestModule = $modules[0]

        foreach ($module in $modules) {
            if ($module.Version -gt $latestModule.Version) {
                $latestModule = $module
            }
        }

        # Check if the repository matches.
        $repositoryName = Get-ModuleRepositoryName -Module $latestModule -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

        if ($repositoryName) {
            $installationPolicy = Get-InstallationPolicy -RepositoryName $repositoryName -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }

        if ($installationPolicy) {
            $installationPolicyReturnValue = 'Trusted'
        }
        else {
            $installationPolicyReturnValue = 'Untrusted'
        }

        $returnValue.Ensure = 'Present'
        $returnValue.Repository = $repositoryName
        $returnValue.Description = $latestModule.Description
        $returnValue.Guid = $latestModule.Guid
        $returnValue.ModuleBase = $latestModule.ModuleBase
        $returnValue.ModuleType = $latestModule.ModuleType
        $returnValue.Author = $latestModule.Author
        $returnValue.InstalledVersion = $latestModule.Version
        $returnValue.InstallationPolicy = $installationPolicyReturnValue
    }
    else {
        Write-Verbose -Message ($localizedData.ModuleNotFound -f $Name)
    }

    return $returnValue
}

<#
    .SYNOPSIS
        This DSC resource provides a mechanism to download PowerShell modules from the PowerShell
        Gallery and install it on your computer.

        Test-TargetResource validates whether the resource is currently in the desired state.

    .PARAMETER Ensure
        Determines whether the module to be installed or uninstalled.

    .PARAMETER Name
        Specifies the name of the PowerShell module to be installed or uninstalled.

    .PARAMETER Repository
        Specifies the name of the module source repository where the module can be found.

    .PARAMETER InstallationPolicy
        Determines whether you trust the source repository where the module resides.

    .PARAMETER RequiredVersion
        Provides the version of the module you want to install or uninstall.

    .PARAMETER MaximumVersion
        Provides the maximum version of the module you want to install or uninstall.

    .PARAMETER MinimumVersion
        Provides the minimum version of the module you want to install or uninstall.

    .PARAMETER Force
        Forces the installation of modules. If a module of the same name and version already exists on the computer,
        this parameter overwrites the existing module with one of the same name that was found by the command.

    .PARAMETER AllowClobber
        Allows the installation of modules regardless of if other existing module on the computer have cmdlets
        of the same name.

    .PARAMETER SkipPublisherCheck
        Allows the installation of modules that have not been catalog signed.
#>
function Test-TargetResource {
    <#
        These suppressions are added because this repository have other Visual Studio Code workspace
        settings than those in DscResource.Tests DSC test framework.
        Only those suppression that contradict this repository guideline is added here.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-FunctionBlockBraces', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-IfStatement', '')]
    [CmdletBinding()]
    [OutputType([System.Boolean])]
    param
    (
        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [System.String]
        $Repository = 'PSGallery',

        [Parameter()]
        [ValidateSet('Trusted', 'Untrusted')]
        [System.String]
        $InstallationPolicy = 'Untrusted',

        [Parameter()]
        [System.String]
        $RequiredVersion,

        [Parameter()]
        [System.String]
        $MaximumVersion,

        [Parameter()]
        [System.String]
        $MinimumVersion,

        [Parameter()]
        [System.Boolean]
        $Force,

        [Parameter()]
        [System.Boolean]
        $AllowClobber,

        [Parameter()]
        [System.Boolean]
        $SkipPublisherCheck
    )

    Write-Verbose -Message ($localizedData.TestTargetResourceMessage -f $Name)

    $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
        -ArgumentNames ('Name', 'Repository', 'MinimumVersion', 'MaximumVersion', 'RequiredVersion')

    $status = Get-TargetResource @extractedArguments

    # The ensure returned from Get-TargetResource is not equal to the desired $Ensure.
    if ($status.Ensure -ieq $Ensure) {
        Write-Verbose -Message ($localizedData.InDesiredState -f $Name)
        return $true
    }
    else {
        Write-Verbose -Message ($localizedData.NotInDesiredState -f $Name)
        return $false
    }
}

<#
    .SYNOPSIS
        This DSC resource provides a mechanism to download PowerShell modules from the PowerShell
        Gallery and install it on your computer.

        Set-TargetResource sets the resource to the desired state. "Make it so".

    .PARAMETER Ensure
        Determines whether the module to be installed or uninstalled.

    .PARAMETER Name
        Specifies the name of the PowerShell module to be installed or uninstalled.

    .PARAMETER Repository
        Specifies the name of the module source repository where the module can be found.

    .PARAMETER InstallationPolicy
        Determines whether you trust the source repository where the module resides.

    .PARAMETER RequiredVersion
        Provides the version of the module you want to install or uninstall.

    .PARAMETER MaximumVersion
        Provides the maximum version of the module you want to install or uninstall.

    .PARAMETER MinimumVersion
        Provides the minimum version of the module you want to install or uninstall.

    .PARAMETER Force
        Forces the installation of modules. If a module of the same name and version already exists on the computer,
        this parameter overwrites the existing module with one of the same name that was found by the command.

    .PARAMETER AllowClobber
        Allows the installation of modules regardless of if other existing module on the computer have cmdlets
        of the same name.

    .PARAMETER SkipPublisherCheck
        Allows the installation of modules that have not been catalog signed.
#>
function Set-TargetResource {
    <#
        These suppressions are added because this repository have other Visual Studio Code workspace
        settings than those in DscResource.Tests DSC test framework.
        Only those suppression that contradict this repository guideline is added here.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-ForEachStatement', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-FunctionBlockBraces', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-IfStatement', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-TryStatement', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-CatchClause', '')]
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [ValidateSet('Present', 'Absent')]
        [System.String]
        $Ensure = 'Present',

        [Parameter(Mandatory = $true)]
        [System.String]
        $Name,

        [Parameter()]
        [System.String]
        $Repository = 'PSGallery',

        [Parameter()]
        [ValidateSet('Trusted', 'Untrusted')]
        [System.String]
        $InstallationPolicy = 'Untrusted',

        [Parameter()]
        [System.String]
        $RequiredVersion,

        [Parameter()]
        [System.String]
        $MaximumVersion,

        [Parameter()]
        [System.String]
        $MinimumVersion,

        [Parameter()]
        [System.Boolean]
        $Force,

        [Parameter()]
        [System.Boolean]
        $AllowClobber,

        [Parameter()]
        [System.Boolean]
        $SkipPublisherCheck
    )

    # Validate the repository argument
    if ($PSBoundParameters.ContainsKey('Repository')) {
        Test-ParameterValue -Value $Repository -Type 'PackageSource' -ProviderName 'PowerShellGet' -Verbose
    }

    if ($Ensure -ieq 'Present') {
        # Version check
        $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
            -ArgumentNames ('MinimumVersion', 'MaximumVersion', 'RequiredVersion')

        $null = Test-VersionParameter @extractedArguments

        try {
            $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
                -ArgumentNames ('Name', 'Repository', 'MinimumVersion', 'MaximumVersion', 'RequiredVersion')

            Write-Verbose -Message ($localizedData.StartFindModule -f $Name)

            $modules = Find-Module @extractedArguments -ErrorVariable ev
        }
        catch {
            $errorMessage = $script:localizedData.ModuleNotFoundInRepository -f $Name
            New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
        }

        $trusted = $null
        $moduleFound = $null

        foreach ($m in $modules) {
            # Check for the installation policy.
            $trusted = Get-InstallationPolicy -RepositoryName $m.Repository -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

            # Stop the loop if found a trusted repository.
            if ($trusted) {
                $moduleFound = $m
                break;
            }
        }

        try {
            # The repository is trusted, so we install it.
            if ($trusted) {
                Write-Verbose -Message ($localizedData.StartInstallModule -f $Name, $moduleFound.Version.toString(), $moduleFound.Repository)

                # Extract the installation options.
                $extractedSwitches = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters -ArgumentNames ('Force', 'AllowClobber', 'SkipPublisherCheck')

                $moduleFound | Install-Module @extractedSwitches 2>&1 | out-string | Write-Verbose
            }
            # The repository is untrusted but user's installation policy is trusted, so we install it with a warning.
            elseif ($InstallationPolicy -ieq 'Trusted') {
                Write-Warning -Message ($localizedData.InstallationPolicyWarning -f $Name, $modules[0].Repository, $InstallationPolicy)

                # Extract installation options (Force implied by InstallationPolicy).
                $extractedSwitches = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters -ArgumentNames ('AllowClobber', 'SkipPublisherCheck')

                # If all the repositories are untrusted, we choose the first one.
                $modules[0] | Install-Module @extractedSwitches -Force 2>&1 | out-string | Write-Verbose
            }
            # Both user and repository is untrusted
            else {
                $errorMessage = $script:localizedData.InstallationPolicyFailed -f $InstallationPolicy, 'Untrusted'
                New-InvalidOperationException -Message $errorMessage
            }

            Write-Verbose -Message ($localizedData.InstalledSuccess -f $Name)
        }
        catch {
            $errorMessage = $script:localizedData.FailToInstall -f $Name
            New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
        }
    }
    # Ensure=Absent
    else {

        $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
            -ArgumentNames ('Name', 'Repository', 'MinimumVersion', 'MaximumVersion', 'RequiredVersion')

        # Get the module with the right version and repository properties.
        $modules = Get-RightModule @extractedArguments

        if (-not $modules) {
            $errorMessage = $script:localizedData.ModuleWithRightPropertyNotFound -f $Name
            New-InvalidOperationException -Message $errorMessage
        }

        foreach ($module in $modules) {
            # Get the path where the module is installed.
            $path = $module.ModuleBase

            Write-Verbose -Message ($localizedData.StartUnInstallModule -f $Name)

            try {
                <#
                    There is no Uninstall-Module cmdlet for Windows PowerShell 4.0,
                    so we will remove the ModuleBase folder as an uninstall operation.
                #>
                Microsoft.PowerShell.Management\Remove-Item -Path $path -Force -Recurse

                Write-Verbose -Message ($localizedData.UnInstalledSuccess -f $module.Name)
            }
            catch {
                $errorMessage = $script:localizedData.FailToUninstall -f $module.Name
                New-InvalidOperationException -Message $errorMessage -ErrorRecord $_
            }
        } # foreach
    } # Ensure=Absent
}

<#
    .SYNOPSIS
        This is a helper function. It returns the modules that meet the specified versions and the repository requirements.

    .PARAMETER Name
        Specifies the name of the PowerShell module.

    .PARAMETER RequiredVersion
        Provides the version of the module you want to install or uninstall.

    .PARAMETER MaximumVersion
        Provides the maximum version of the module you want to install or uninstall.

    .PARAMETER MinimumVersion
        Provides the minimum version of the module you want to install or uninstall.

    .PARAMETER Repository
        Specifies the name of the module source repository where the module can be found.
#>
function Get-RightModule {
    <#
        These suppressions are added because this repository have other Visual Studio Code workspace
        settings than those in DscResource.Tests DSC test framework.
        Only those suppression that contradict this repository guideline is added here.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-ForEachStatement', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-FunctionBlockBraces', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-IfStatement', '')]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $Name,

        [Parameter()]
        [System.String]
        $RequiredVersion,

        [Parameter()]
        [System.String]
        $MinimumVersion,

        [Parameter()]
        [System.String]
        $MaximumVersion,

        [Parameter()]
        [System.String]
        $Repository
    )

    Write-Verbose -Message ($localizedData.StartGetModule -f $($Name))

    $modules = Microsoft.PowerShell.Core\Get-Module -Name $Name -ListAvailable -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

    if (-not $modules) {
        return $null
    }

    <#
        As Get-Module does not take RequiredVersion, MinimumVersion, MaximumVersion, or Repository,
        below we need to check whether the modules are containing the right version and repository
        location.
    #>

    $extractedArguments = New-SplatParameterHashTable -FunctionBoundParameters $PSBoundParameters `
        -ArgumentNames ('MaximumVersion', 'MinimumVersion', 'RequiredVersion')
    $returnVal = @()

    foreach ($m in $modules) {
        $versionMatch = $false
        $installedVersion = $m.Version

        # Case 1 - a user provides none of RequiredVersion, MinimumVersion, MaximumVersion
        if ($extractedArguments.Count -eq 0) {
            $versionMatch = $true
        }

        # Case 2 - a user provides RequiredVersion
        elseif ($extractedArguments.ContainsKey('RequiredVersion')) {
            # Check if it matches with the installed version
            $versionMatch = ($installedVersion -eq [System.Version] $RequiredVersion)
        }
        else {

            # Case 3 - a user provides MinimumVersion
            if ($extractedArguments.ContainsKey('MinimumVersion')) {
                $versionMatch = ($installedVersion -ge [System.Version] $extractedArguments['MinimumVersion'])
            }

            # Case 4 - a user provides MaximumVersion
            if ($extractedArguments.ContainsKey('MaximumVersion')) {
                $isLessThanMax = ($installedVersion -le [System.Version] $extractedArguments['MaximumVersion'])

                if ($extractedArguments.ContainsKey('MinimumVersion')) {
                    $versionMatch = $versionMatch -and $isLessThanMax
                }
                else {
                    $versionMatch = $isLessThanMax
                }
            }

            # Case 5 - Both MinimumVersion and MaximumVersion are provided. It's covered by the above.
            # Do not return $false yet to allow the foreach to continue
            if (-not $versionMatch) {
                Write-Verbose -Message ($localizedData.VersionMismatch -f $Name, $installedVersion)
                $versionMatch = $false
            }
        }

        # Case 6 - Version matches but need to check if the module is from the right repository.
        if ($versionMatch) {
            # A user does not provide Repository, we are good
            if (-not $PSBoundParameters.ContainsKey('Repository')) {
                Write-Verbose -Message ($localizedData.ModuleFound -f "$Name $installedVersion")
                $returnVal += $m
            }
            else {
                # Check if the Repository matches
                $sourceName = Get-ModuleRepositoryName -Module $m

                if ($Repository -ieq $sourceName) {
                    Write-Verbose -Message ($localizedData.ModuleFound -f "$Name $installedVersion")
                    $returnVal += $m
                }
                else {
                    Write-Verbose -Message ($localizedData.RepositoryMismatch -f $($Name), $($sourceName))
                }
            }
        }
    } # foreach

    return $returnVal
}

<#
    .SYNOPSIS
        This is a helper function that returns the module's repository name.

    .PARAMETER Module
        Specifies the name of the PowerShell module.
#>
function Get-ModuleRepositoryName {
    <#
        These suppressions are added because this repository have other Visual Studio Code workspace
        settings than those in DscResource.Tests DSC test framework.
        Only those suppression that contradict this repository guideline is added here.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-FunctionBlockBraces', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('DscResource.AnalyzerRules\Measure-IfStatement', '')]
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Object]
        $Module
    )

    <#
        RepositorySourceLocation property is supported in PS V5 only. To work with the earlier
        PowerShell version, we need to do a different way. PSGetModuleInfo.xml exists for any
        PowerShell modules downloaded through PSModule provider.
    #>
    $psGetModuleInfoFileName = 'PSGetModuleInfo.xml'
    $psGetModuleInfoPath = Microsoft.PowerShell.Management\Join-Path -Path $Module.ModuleBase -ChildPath $psGetModuleInfoFileName

    Write-Verbose -Message ($localizedData.FoundModulePath -f $psGetModuleInfoPath)

    if (Microsoft.PowerShell.Management\Test-path -Path $psGetModuleInfoPath) {
        $psGetModuleInfo = Microsoft.PowerShell.Utility\Import-Clixml -Path $psGetModuleInfoPath

        return $psGetModuleInfo.Repository
    }
}

# SIG # Begin signature block
# MIIjkgYJKoZIhvcNAQcCoIIjgzCCI38CAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCig4H1kVMWxA7U
# uX1JaRBJmDsHSZHmuBUTkDBwWHafCqCCDYEwggX/MIID56ADAgECAhMzAAABh3IX
# chVZQMcJAAAAAAGHMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTEwHhcNMjAwMzA0MTgzOTQ3WhcNMjEwMzAzMTgzOTQ3WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQDOt8kLc7P3T7MKIhouYHewMFmnq8Ayu7FOhZCQabVwBp2VS4WyB2Qe4TQBT8aB
# znANDEPjHKNdPT8Xz5cNali6XHefS8i/WXtF0vSsP8NEv6mBHuA2p1fw2wB/F0dH
# sJ3GfZ5c0sPJjklsiYqPw59xJ54kM91IOgiO2OUzjNAljPibjCWfH7UzQ1TPHc4d
# weils8GEIrbBRb7IWwiObL12jWT4Yh71NQgvJ9Fn6+UhD9x2uk3dLj84vwt1NuFQ
# itKJxIV0fVsRNR3abQVOLqpDugbr0SzNL6o8xzOHL5OXiGGwg6ekiXA1/2XXY7yV
# Fc39tledDtZjSjNbex1zzwSXAgMBAAGjggF+MIIBejAfBgNVHSUEGDAWBgorBgEE
# AYI3TAgBBggrBgEFBQcDAzAdBgNVHQ4EFgQUhov4ZyO96axkJdMjpzu2zVXOJcsw
# UAYDVR0RBEkwR6RFMEMxKTAnBgNVBAsTIE1pY3Jvc29mdCBPcGVyYXRpb25zIFB1
# ZXJ0byBSaWNvMRYwFAYDVQQFEw0yMzAwMTIrNDU4Mzg1MB8GA1UdIwQYMBaAFEhu
# ZOVQBdOCqhc3NyK1bajKdQKVMFQGA1UdHwRNMEswSaBHoEWGQ2h0dHA6Ly93d3cu
# bWljcm9zb2Z0LmNvbS9wa2lvcHMvY3JsL01pY0NvZFNpZ1BDQTIwMTFfMjAxMS0w
# Ny0wOC5jcmwwYQYIKwYBBQUHAQEEVTBTMFEGCCsGAQUFBzAChkVodHRwOi8vd3d3
# Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NlcnRzL01pY0NvZFNpZ1BDQTIwMTFfMjAx
# MS0wNy0wOC5jcnQwDAYDVR0TAQH/BAIwADANBgkqhkiG9w0BAQsFAAOCAgEAixmy
# S6E6vprWD9KFNIB9G5zyMuIjZAOuUJ1EK/Vlg6Fb3ZHXjjUwATKIcXbFuFC6Wr4K
# NrU4DY/sBVqmab5AC/je3bpUpjtxpEyqUqtPc30wEg/rO9vmKmqKoLPT37svc2NV
# BmGNl+85qO4fV/w7Cx7J0Bbqk19KcRNdjt6eKoTnTPHBHlVHQIHZpMxacbFOAkJr
# qAVkYZdz7ikNXTxV+GRb36tC4ByMNxE2DF7vFdvaiZP0CVZ5ByJ2gAhXMdK9+usx
# zVk913qKde1OAuWdv+rndqkAIm8fUlRnr4saSCg7cIbUwCCf116wUJ7EuJDg0vHe
# yhnCeHnBbyH3RZkHEi2ofmfgnFISJZDdMAeVZGVOh20Jp50XBzqokpPzeZ6zc1/g
# yILNyiVgE+RPkjnUQshd1f1PMgn3tns2Cz7bJiVUaqEO3n9qRFgy5JuLae6UweGf
# AeOo3dgLZxikKzYs3hDMaEtJq8IP71cX7QXe6lnMmXU/Hdfz2p897Zd+kU+vZvKI
# 3cwLfuVQgK2RZ2z+Kc3K3dRPz2rXycK5XCuRZmvGab/WbrZiC7wJQapgBodltMI5
# GMdFrBg9IeF7/rP4EqVQXeKtevTlZXjpuNhhjuR+2DMt/dWufjXpiW91bo3aH6Ea
# jOALXmoxgltCp1K7hrS6gmsvj94cLRf50QQ4U8Qwggd6MIIFYqADAgECAgphDpDS
# AAAAAAADMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJVUzETMBEGA1UECBMK
# V2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0
# IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9vdCBDZXJ0aWZpY2F0
# ZSBBdXRob3JpdHkgMjAxMTAeFw0xMTA3MDgyMDU5MDlaFw0yNjA3MDgyMTA5MDla
# MH4xCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdS
# ZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMT
# H01pY3Jvc29mdCBDb2RlIFNpZ25pbmcgUENBIDIwMTEwggIiMA0GCSqGSIb3DQEB
# AQUAA4ICDwAwggIKAoICAQCr8PpyEBwurdhuqoIQTTS68rZYIZ9CGypr6VpQqrgG
# OBoESbp/wwwe3TdrxhLYC/A4wpkGsMg51QEUMULTiQ15ZId+lGAkbK+eSZzpaF7S
# 35tTsgosw6/ZqSuuegmv15ZZymAaBelmdugyUiYSL+erCFDPs0S3XdjELgN1q2jz
# y23zOlyhFvRGuuA4ZKxuZDV4pqBjDy3TQJP4494HDdVceaVJKecNvqATd76UPe/7
# 4ytaEB9NViiienLgEjq3SV7Y7e1DkYPZe7J7hhvZPrGMXeiJT4Qa8qEvWeSQOy2u
# M1jFtz7+MtOzAz2xsq+SOH7SnYAs9U5WkSE1JcM5bmR/U7qcD60ZI4TL9LoDho33
# X/DQUr+MlIe8wCF0JV8YKLbMJyg4JZg5SjbPfLGSrhwjp6lm7GEfauEoSZ1fiOIl
# XdMhSz5SxLVXPyQD8NF6Wy/VI+NwXQ9RRnez+ADhvKwCgl/bwBWzvRvUVUvnOaEP
# 6SNJvBi4RHxF5MHDcnrgcuck379GmcXvwhxX24ON7E1JMKerjt/sW5+v/N2wZuLB
# l4F77dbtS+dJKacTKKanfWeA5opieF+yL4TXV5xcv3coKPHtbcMojyyPQDdPweGF
# RInECUzF1KVDL3SV9274eCBYLBNdYJWaPk8zhNqwiBfenk70lrC8RqBsmNLg1oiM
# CwIDAQABo4IB7TCCAekwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFEhuZOVQ
# BdOCqhc3NyK1bajKdQKVMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1Ud
# DwQEAwIBhjAPBgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFHItOgIxkEO5FAVO
# 4eqnxzHRI4k0MFoGA1UdHwRTMFEwT6BNoEuGSWh0dHA6Ly9jcmwubWljcm9zb2Z0
# LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcmwwXgYIKwYBBQUHAQEEUjBQME4GCCsGAQUFBzAChkJodHRwOi8vd3d3Lm1p
# Y3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY1Jvb0NlckF1dDIwMTFfMjAxMV8wM18y
# Mi5jcnQwgZ8GA1UdIASBlzCBlDCBkQYJKwYBBAGCNy4DMIGDMD8GCCsGAQUFBwIB
# FjNodHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2RvY3MvcHJpbWFyeWNw
# cy5odG0wQAYIKwYBBQUHAgIwNB4yIB0ATABlAGcAYQBsAF8AcABvAGwAaQBjAHkA
# XwBzAHQAYQB0AGUAbQBlAG4AdAAuIB0wDQYJKoZIhvcNAQELBQADggIBAGfyhqWY
# 4FR5Gi7T2HRnIpsLlhHhY5KZQpZ90nkMkMFlXy4sPvjDctFtg/6+P+gKyju/R6mj
# 82nbY78iNaWXXWWEkH2LRlBV2AySfNIaSxzzPEKLUtCw/WvjPgcuKZvmPRul1LUd
# d5Q54ulkyUQ9eHoj8xN9ppB0g430yyYCRirCihC7pKkFDJvtaPpoLpWgKj8qa1hJ
# Yx8JaW5amJbkg/TAj/NGK978O9C9Ne9uJa7lryft0N3zDq+ZKJeYTQ49C/IIidYf
# wzIY4vDFLc5bnrRJOQrGCsLGra7lstnbFYhRRVg4MnEnGn+x9Cf43iw6IGmYslmJ
# aG5vp7d0w0AFBqYBKig+gj8TTWYLwLNN9eGPfxxvFX1Fp3blQCplo8NdUmKGwx1j
# NpeG39rz+PIWoZon4c2ll9DuXWNB41sHnIc+BncG0QaxdR8UvmFhtfDcxhsEvt9B
# xw4o7t5lL+yX9qFcltgA1qFGvVnzl6UJS0gQmYAf0AApxbGbpT9Fdx41xtKiop96
# eiL6SJUfq/tHI4D1nvi/a7dLl+LrdXga7Oo3mXkYS//WsyNodeav+vyL6wuA6mk7
# r/ww7QRMjt/fdW1jkT3RnVZOT7+AVyKheBEyIXrvQQqxP/uozKRdwaGIm1dxVk5I
# RcBCyZt2WwqASGv9eZ/BvW1taslScxMNelDNMYIVZzCCFWMCAQEwgZUwfjELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEoMCYGA1UEAxMfTWljcm9z
# b2Z0IENvZGUgU2lnbmluZyBQQ0EgMjAxMQITMwAAAYdyF3IVWUDHCQAAAAABhzAN
# BglghkgBZQMEAgEFAKCBrjAZBgkqhkiG9w0BCQMxDAYKKwYBBAGCNwIBBDAcBgor
# BgEEAYI3AgELMQ4wDAYKKwYBBAGCNwIBFTAvBgkqhkiG9w0BCQQxIgQg2t+ikpO2
# 0WdDEf/JRKsEEOWFz2O6CEnKmWwPbrvPFuIwQgYKKwYBBAGCNwIBDDE0MDKgFIAS
# AE0AaQBjAHIAbwBzAG8AZgB0oRqAGGh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbTAN
# BgkqhkiG9w0BAQEFAASCAQBaj/JIwAbud/IMJN3SHVwenJwEjzrmM3cPznYlvkyE
# rnB6RiDYnohm+72wkCmVWobuualV9Xw8OGA9w7MvJjZuEnFP9TwDjlWinR4FpdVl
# YX49u4g50iMR2isaKTxMA2nA2LbyFpTLMqi6ynPLnv251xTdumj/XRLZjHdpr4Pt
# p6HQz45/jMWRtlh8x3piJPrgpINuJtmO91tnMQnykF++QgteDqKSboXOu2Wg0oCn
# Ao0eOifIujyUp2ez9Cqx2hzkMOqNa5jZD4JetiDTWBTkN8XHRbsfCVwyEQzxrpXn
# wAFT7jMzuI8o8xdCuCvuDokIzcnm+rKZJIy6Rwp6Cn11oYIS8TCCEu0GCisGAQQB
# gjcDAwExghLdMIIS2QYJKoZIhvcNAQcCoIISyjCCEsYCAQMxDzANBglghkgBZQME
# AgEFADCCAVUGCyqGSIb3DQEJEAEEoIIBRASCAUAwggE8AgEBBgorBgEEAYRZCgMB
# MDEwDQYJYIZIAWUDBAIBBQAEIL/3GmTbcOXlRJe8Pu+1/QVBo0Mk83o/PGK7iApB
# aAutAgZfYQoJ7SUYEzIwMjAwOTIyMjIxOTUxLjg4NlowBIACAfSggdSkgdEwgc4x
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKTAnBgNVBAsTIE1p
# Y3Jvc29mdCBPcGVyYXRpb25zIFB1ZXJ0byBSaWNvMSYwJAYDVQQLEx1UaGFsZXMg
# VFNTIEVTTjo4OTdBLUUzNTYtMTcwMTElMCMGA1UEAxMcTWljcm9zb2Z0IFRpbWUt
# U3RhbXAgU2VydmljZaCCDkQwggT1MIID3aADAgECAhMzAAABLCKvRZd1+RvuAAAA
# AAEsMA0GCSqGSIb3DQEBCwUAMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNo
# aW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29y
# cG9yYXRpb24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEw
# MB4XDTE5MTIxOTAxMTUwM1oXDTIxMDMxNzAxMTUwM1owgc4xCzAJBgNVBAYTAlVT
# MRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQK
# ExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKTAnBgNVBAsTIE1pY3Jvc29mdCBPcGVy
# YXRpb25zIFB1ZXJ0byBSaWNvMSYwJAYDVQQLEx1UaGFsZXMgVFNTIEVTTjo4OTdB
# LUUzNTYtMTcwMTElMCMGA1UEAxMcTWljcm9zb2Z0IFRpbWUtU3RhbXAgU2Vydmlj
# ZTCCASIwDQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAPK1zgSSq+MxAYo3qpCt
# QDxSMPPJy6mm/wfEJNjNUnYtLFBwl1BUS5trEk/t41ldxITKehs+ABxYqo4Qxsg3
# Gy1ugKiwHAnYiiekfC+ZhptNFgtnDZIn45zC0AlVr/6UfLtsLcHCh1XElLUHfEC0
# nBuQcM/SpYo9e3l1qY5NdMgDGxCsmCKdiZfYXIu+U0UYIBhdzmSHnB3fxZOBVcr5
# htFHEBBNt/rFJlm/A4yb8oBsp+Uf0p5QwmO/bCcdqB15JpylOhZmWs0sUfJKlK9E
# rAhBwGki2eIRFKsQBdkXS9PWpF1w2gIJRvSkDEaCf+lbGTPdSzHSbfREWOF9wY3i
# Yj8CAwEAAaOCARswggEXMB0GA1UdDgQWBBRRahZSGfrCQhCyIyGH9DkiaW7L0zAf
# BgNVHSMEGDAWgBTVYzpcijGQ80N7fEYbxTNoWoVtVTBWBgNVHR8ETzBNMEugSaBH
# hkVodHRwOi8vY3JsLm1pY3Jvc29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9NaWNU
# aW1TdGFQQ0FfMjAxMC0wNy0wMS5jcmwwWgYIKwYBBQUHAQEETjBMMEoGCCsGAQUF
# BzAChj5odHRwOi8vd3d3Lm1pY3Jvc29mdC5jb20vcGtpL2NlcnRzL01pY1RpbVN0
# YVBDQV8yMDEwLTA3LTAxLmNydDAMBgNVHRMBAf8EAjAAMBMGA1UdJQQMMAoGCCsG
# AQUFBwMIMA0GCSqGSIb3DQEBCwUAA4IBAQBPFxHIwi4vAH49w9Svmz6K3tM55RlW
# 5pPeULXdut2Rqy6Ys0+VpZsbuaEoxs6Z1C3hMbkiqZFxxyltxJpuHTyGTg61zfNI
# F5n6RsYF3s7IElDXNfZznF1/2iWc6uRPZK8rxxUJ/7emYXZCYwuUY0XjsCpP9pbR
# RKeJi6r5arSyI+NfKxvgoM21JNt1BcdlXuAecdd/k8UjxCscffanoK2n6LFw1PcZ
# lEO7NId7o+soM2C0QY5BYdghpn7uqopB6ixyFIIkDXFub+1E7GmAEwfU6VwEHL7y
# 9rNE8bd+JrQs+yAtkkHy9FmXg/PsGq1daVzX1So7CJ6nyphpuHSN3VfTMIIGcTCC
# BFmgAwIBAgIKYQmBKgAAAAAAAjANBgkqhkiG9w0BAQsFADCBiDELMAkGA1UEBhMC
# VVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNV
# BAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEyMDAGA1UEAxMpTWljcm9zb2Z0IFJv
# b3QgQ2VydGlmaWNhdGUgQXV0aG9yaXR5IDIwMTAwHhcNMTAwNzAxMjEzNjU1WhcN
# MjUwNzAxMjE0NjU1WjB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3Rv
# bjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0
# aW9uMSYwJAYDVQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EgMjAxMDCCASIw
# DQYJKoZIhvcNAQEBBQADggEPADCCAQoCggEBAKkdDbx3EYo6IOz8E5f1+n9plGt0
# VBDVpQoAgoX77XxoSyxfxcPlYcJ2tz5mK1vwFVMnBDEfQRsalR3OCROOfGEwWbEw
# RA/xYIiEVEMM1024OAizQt2TrNZzMFcmgqNFDdDq9UeBzb8kYDJYYEbyWEeGMoQe
# dGFnkV+BVLHPk0ySwcSmXdFhE24oxhr5hoC732H8RsEnHSRnEnIaIYqvS2SJUGKx
# Xf13Hz3wV3WsvYpCTUBR0Q+cBj5nf/VmwAOWRH7v0Ev9buWayrGo8noqCjHw2k4G
# kbaICDXoeByw6ZnNPOcvRLqn9NxkvaQBwSAJk3jN/LzAyURdXhacAQVPIk0CAwEA
# AaOCAeYwggHiMBAGCSsGAQQBgjcVAQQDAgEAMB0GA1UdDgQWBBTVYzpcijGQ80N7
# fEYbxTNoWoVtVTAZBgkrBgEEAYI3FAIEDB4KAFMAdQBiAEMAQTALBgNVHQ8EBAMC
# AYYwDwYDVR0TAQH/BAUwAwEB/zAfBgNVHSMEGDAWgBTV9lbLj+iiXGJo0T2UkFvX
# zpoYxDBWBgNVHR8ETzBNMEugSaBHhkVodHRwOi8vY3JsLm1pY3Jvc29mdC5jb20v
# cGtpL2NybC9wcm9kdWN0cy9NaWNSb29DZXJBdXRfMjAxMC0wNi0yMy5jcmwwWgYI
# KwYBBQUHAQEETjBMMEoGCCsGAQUFBzAChj5odHRwOi8vd3d3Lm1pY3Jvc29mdC5j
# b20vcGtpL2NlcnRzL01pY1Jvb0NlckF1dF8yMDEwLTA2LTIzLmNydDCBoAYDVR0g
# AQH/BIGVMIGSMIGPBgkrBgEEAYI3LgMwgYEwPQYIKwYBBQUHAgEWMWh0dHA6Ly93
# d3cubWljcm9zb2Z0LmNvbS9QS0kvZG9jcy9DUFMvZGVmYXVsdC5odG0wQAYIKwYB
# BQUHAgIwNB4yIB0ATABlAGcAYQBsAF8AUABvAGwAaQBjAHkAXwBTAHQAYQB0AGUA
# bQBlAG4AdAAuIB0wDQYJKoZIhvcNAQELBQADggIBAAfmiFEN4sbgmD+BcQM9naOh
# IW+z66bM9TG+zwXiqf76V20ZMLPCxWbJat/15/B4vceoniXj+bzta1RXCCtRgkQS
# +7lTjMz0YBKKdsxAQEGb3FwX/1z5Xhc1mCRWS3TvQhDIr79/xn/yN31aPxzymXlK
# kVIArzgPF/UveYFl2am1a+THzvbKegBvSzBEJCI8z+0DpZaPWSm8tv0E4XCfMkon
# /VWvL/625Y4zu2JfmttXQOnxzplmkIz/amJ/3cVKC5Em4jnsGUpxY517IW3DnKOi
# PPp/fZZqkHimbdLhnPkd/DjYlPTGpQqWhqS9nhquBEKDuLWAmyI4ILUl5WTs9/S/
# fmNZJQ96LjlXdqJxqgaKD4kWumGnEcua2A5HmoDF0M2n0O99g/DhO3EJ3110mCII
# YdqwUB5vvfHhAN/nMQekkzr3ZUd46PioSKv33nJ+YWtvd6mBy6cJrDm77MbL2IK0
# cs0d9LiFAR6A+xuJKlQ5slvayA1VmXqHczsI5pgt6o3gMy4SKfXAL1QnIffIrE7a
# KLixqduWsqdCosnPGUFN4Ib5KpqjEWYw07t0MkvfY3v1mYovG8chr1m1rtxEPJdQ
# cdeh0sVV42neV8HR3jDA/czmTfsNv11P6Z0eGTgvvM9YBS7vDaBQNdrvCScc1bN+
# NR4Iuto229Nfj950iEkSoYIC0jCCAjsCAQEwgfyhgdSkgdEwgc4xCzAJBgNVBAYT
# AlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYD
# VQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKTAnBgNVBAsTIE1pY3Jvc29mdCBP
# cGVyYXRpb25zIFB1ZXJ0byBSaWNvMSYwJAYDVQQLEx1UaGFsZXMgVFNTIEVTTjo4
# OTdBLUUzNTYtMTcwMTElMCMGA1UEAxMcTWljcm9zb2Z0IFRpbWUtU3RhbXAgU2Vy
# dmljZaIjCgEBMAcGBSsOAwIaAxUADE5OKSMoNx/mYxYWap1RTOohbJ2ggYMwgYCk
# fjB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMH
# UmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSYwJAYDVQQD
# Ex1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EgMjAxMDANBgkqhkiG9w0BAQUFAAIF
# AOMUwsMwIhgPMjAyMDA5MjIyMjM2NTFaGA8yMDIwMDkyMzIyMzY1MVowdzA9Bgor
# BgEEAYRZCgQBMS8wLTAKAgUA4xTCwwIBADAKAgEAAgIlfwIB/zAHAgEAAgIRzTAK
# AgUA4xYUQwIBADA2BgorBgEEAYRZCgQCMSgwJjAMBgorBgEEAYRZCgMCoAowCAIB
# AAIDB6EgoQowCAIBAAIDAYagMA0GCSqGSIb3DQEBBQUAA4GBAEmZCk5PsMA6oG40
# YulPUpxDtQA+JAuXyNrLI4wJcdmv34Bzmj9gk3gv6ZxQZxlvZjA1O1P3lcKGIYco
# 9UkRdAeP6Zzfim6U3UoiUz1AHKuSm6iLux7Z/hNtgEs2ZpYXxOK5JZ/9Pa2I5mg0
# VxsGdXBxl6TOZqbB6aGNG1ZKytRWMYIDDTCCAwkCAQEwgZMwfDELMAkGA1UEBhMC
# VVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNV
# BAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEmMCQGA1UEAxMdTWljcm9zb2Z0IFRp
# bWUtU3RhbXAgUENBIDIwMTACEzMAAAEsIq9Fl3X5G+4AAAAAASwwDQYJYIZIAWUD
# BAIBBQCgggFKMBoGCSqGSIb3DQEJAzENBgsqhkiG9w0BCRABBDAvBgkqhkiG9w0B
# CQQxIgQgioHn9SQtTXybUUTzgPmt61UPBGskDV9AErEC62Y8+H4wgfoGCyqGSIb3
# DQEJEAIvMYHqMIHnMIHkMIG9BCBbn/0uFFh42hTM5XOoKdXevBaiSxmYK9Ilcn9n
# u5ZH4TCBmDCBgKR+MHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9u
# MRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRp
# b24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwAhMzAAAB
# LCKvRZd1+RvuAAAAAAEsMCIEIJmkMFyuUW5yiXAemMCWHyk8T1EMMlB1cEiVD4/E
# +vhqMA0GCSqGSIb3DQEBCwUABIIBAOyFm0zpO+VogyMfTuCFeJjMyyDnAJRy/KhW
# jtq11pPg3e11HU0LuNa+ZpCrJXbSWlF+tLkU9eQXPpGPzO7ITGhxhnq5wyhfzA8a
# 9TRGlDMUNHp+6WhH6BZ86zUqMpcYx5zN0H9TaJTpC8JJ7HrNTW8zp032ocr6IXVr
# M6mOa0MUZsE366+OXRpCCm0fokT50uvXWbcRdngS9ZT5xmz134T1iS4Fds7ZOC5W
# m0VofKEuaTYbTc5aF0WN/OGMvowHRxlJhG7zhMX8ZuXGvbgzANq/o7tZu0HmGFTN
# rPs5smC3VEWVpNWgOTfrO3bfaWkAdsuBeLVXZUog+M4wmTDFSPs=
# SIG # End signature block
