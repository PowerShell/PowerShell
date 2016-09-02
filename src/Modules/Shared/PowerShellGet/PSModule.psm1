
#########################################################################################
#
# Copyright (c) Microsoft Corporation. All rights reserved.
#
# PowerShellGet Module
#
#########################################################################################

Microsoft.PowerShell.Core\Set-StrictMode -Version Latest

#region script variables

# Check if this is nano server. [System.Runtime.Loader.AssemblyLoadContext] is only available on NanoServer
$script:isNanoServer = $null -ne ('System.Runtime.Loader.AssemblyLoadContext' -as [Type])

function IsInbox { $PSHOME.EndsWith('\WindowsPowerShell\v1.0', [System.StringComparison]::OrdinalIgnoreCase) }
function IsWindows { $PSVariable = Get-Variable -Name IsWindows -ErrorAction Ignore; return (-not $PSVariable -or $PSVariable.Value) }
function IsLinux { $PSVariable = Get-Variable -Name IsLinux -ErrorAction Ignore; return ($PSVariable -and $PSVariable.Value) }
function IsOSX { $PSVariable = Get-Variable -Name IsOSX -ErrorAction Ignore; return ($PSVariable -and $PSVariable.Value) }
function IsCoreCLR { $PSVariable = Get-Variable -Name IsCoreCLR -ErrorAction Ignore; return ($PSVariable -and $PSVariable.Value) }

if(IsInbox)
{
    $script:ProgramFilesPSPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramFiles -ChildPath "WindowsPowerShell"
}
else
{
    $script:ProgramFilesPSPath = $PSHome
}

if(IsInbox)
{
    try
    {
        $script:MyDocumentsFolderPath = [Environment]::GetFolderPath("MyDocuments")
    }
    catch
    {
        $script:MyDocumentsFolderPath = $null
    }

    $script:MyDocumentsPSPath = if($script:MyDocumentsFolderPath)
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsFolderPath -ChildPath "WindowsPowerShell"
                                } 
                                else
                                {
                                    Microsoft.PowerShell.Management\Join-Path -Path $env:USERPROFILE -ChildPath "Documents\WindowsPowerShell"
                                }
}
elseif(IsWindows)
{
    $script:MyDocumentsPSPath = Microsoft.PowerShell.Management\Join-Path -Path $HOME -ChildPath 'Documents\PowerShell'
}
else
{
    $script:MyDocumentsPSPath = Microsoft.PowerShell.Management\Join-Path -Path $HOME -ChildPath ".local/share/powershell"
}

$script:ProgramFilesModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath "Modules"
$script:MyDocumentsModulesPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath "Modules"

$script:ProgramFilesScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesPSPath -ChildPath "Scripts"

$script:MyDocumentsScriptsPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsPSPath -ChildPath "Scripts"

$script:TempPath = if(IsWindows){ ([System.IO.DirectoryInfo]$env:TEMP).FullName } else { '/tmp' }
$script:PSGetItemInfoFileName = "PSGetModuleInfo.xml"

if(IsWindows)
{
    $script:PSGetProgramDataPath = Microsoft.PowerShell.Management\Join-Path -Path $env:ProgramData -ChildPath 'Microsoft\Windows\PowerShell\PowerShellGet\'
    $script:PSGetAppLocalPath = Microsoft.PowerShell.Management\Join-Path -Path $env:LOCALAPPDATA -ChildPath 'Microsoft\Windows\PowerShell\PowerShellGet\'
}
else
{
    $script:PSGetProgramDataPath = "$HOME/.config/powershell/powershellget" #TODO: Get $env:ProgramData equivalent
    $script:PSGetAppLocalPath = "$HOME/.config/powershell/powershellget"
}

$script:PSGetModuleSourcesFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:PSGetAppLocalPath -ChildPath "PSRepositories.xml"
$script:PSGetModuleSources = $null
$script:PSGetInstalledModules = $null
$script:PSGetSettingsFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:PSGetAppLocalPath -ChildPath "PowerShellGetSettings.xml"
$script:PSGetSettings = $null

$script:MyDocumentsInstalledScriptInfosPath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsScriptsPath -ChildPath 'InstalledScriptInfos'
$script:ProgramFilesInstalledScriptInfosPath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesScriptsPath -ChildPath 'InstalledScriptInfos'

$script:InstalledScriptInfoFileName = 'InstalledScriptInfo.xml'
$script:PSGetInstalledScripts = $null

# Public PSGallery module source name and location
$Script:PSGalleryModuleSource="PSGallery"
$Script:PSGallerySourceUri  = 'https://go.microsoft.com/fwlink/?LinkID=397631&clcid=0x409'
$Script:PSGalleryPublishUri = 'https://go.microsoft.com/fwlink/?LinkID=397527&clcid=0x409'
$Script:PSGalleryScriptSourceUri = 'https://go.microsoft.com/fwlink/?LinkID=622995&clcid=0x409'

# PSGallery V3 Source
$Script:PSGalleryV3SourceUri = 'https://go.microsoft.com/fwlink/?LinkId=528403&clcid=0x409'

$Script:PSGalleryV2ApiAvailable = $true
$Script:PSGalleryV3ApiAvailable = $false
$Script:PSGalleryApiChecked = $false

$Script:ResponseUri = "ResponseUri"
$Script:StatusCode = "StatusCode"
$Script:Exception = "Exception"

$script:PSModuleProviderName = 'PowerShellGet'
$script:PackageManagementProviderParam  = "PackageManagementProvider"
$script:PublishLocation = "PublishLocation"
$script:ScriptSourceLocation = 'ScriptSourceLocation'
$script:ScriptPublishLocation = 'ScriptPublishLocation'
$script:Proxy = 'Proxy'
$script:ProxyCredential = 'ProxyCredential'
$script:Credential = 'Credential'
$script:VSTSAuthenticatedFeedsDocUrl = 'https://go.microsoft.com/fwlink/?LinkID=698608'

$script:NuGetProviderName = "NuGet"
$script:NuGetProviderVersion  = [Version]'2.8.5.201'

$script:SupportsPSModulesFeatureName="supports-powershell-modules"
$script:FastPackRefHastable = @{}
$script:NuGetBinaryProgramDataPath=if(IsWindows) {"$env:ProgramFiles\PackageManagement\ProviderAssemblies"}
$script:NuGetBinaryLocalAppDataPath=if(IsWindows) {"$env:LOCALAPPDATA\PackageManagement\ProviderAssemblies"}
# go fwlink for 'https://nuget.org/nuget.exe'
$script:NuGetClientSourceURL = 'https://go.microsoft.com/fwlink/?LinkID=690216&clcid=0x409'
$script:NuGetExeName = 'NuGet.exe'
$script:NuGetExePath = $null
$script:NuGetProvider = $null
# PowerShellGetFormatVersion will be incremented when we change the .nupkg format structure. 
# PowerShellGetFormatVersion is in the form of Major.Minor.  
# Minor is incremented for the backward compatible format change.
# Major is incremented for the breaking change.
$script:CurrentPSGetFormatVersion = "1.0"
$script:PSGetFormatVersion = "PowerShellGetFormatVersion"
$script:SupportedPSGetFormatVersionMajors = @("1")
$script:ModuleReferences = 'Module References'
$script:AllVersions = "AllVersions"
$script:Filter      = "Filter"
$script:IncludeValidSet = @('DscResource','Cmdlet','Function','Workflow','RoleCapability')
$script:DscResource = "PSDscResource"
$script:Command     = "PSCommand"
$script:Cmdlet      = "PSCmdlet"
$script:Function    = "PSFunction"
$script:Workflow    = "PSWorkflow"
$script:RoleCapability = 'PSRoleCapability'
$script:Includes    = "PSIncludes"
$script:Tag         = "Tag"
$script:NotSpecified= '_NotSpecified_'
$script:PSGetModuleName = 'PowerShellGet'
$script:FindByCanonicalId = 'FindByCanonicalId'
$script:InstalledLocation = 'InstalledLocation'
$script:PSArtifactType = 'Type'
$script:PSArtifactTypeModule = 'Module'
$script:PSArtifactTypeScript = 'Script'
$script:All = 'All'

$script:Name = 'Name'
$script:Version = 'Version'
$script:Guid = 'Guid'
$script:Path = 'Path'
$script:ScriptBase = 'ScriptBase'
$script:Description = 'Description'
$script:Author = 'Author'
$script:CompanyName = 'CompanyName'
$script:Copyright = 'Copyright'
$script:Tags = 'Tags'
$script:LicenseUri = 'LicenseUri'
$script:ProjectUri = 'ProjectUri'
$script:IconUri = 'IconUri'
$script:RequiredModules = 'RequiredModules'
$script:ExternalModuleDependencies = 'ExternalModuleDependencies'
$script:ReleaseNotes = 'ReleaseNotes'
$script:RequiredScripts = 'RequiredScripts'
$script:ExternalScriptDependencies = 'ExternalScriptDependencies'
$script:DefinedCommands  = 'DefinedCommands'
$script:DefinedFunctions = 'DefinedFunctions'
$script:DefinedWorkflows = 'DefinedWorkflows'
$script:TextInfo = (Get-Culture).TextInfo

$script:PSScriptInfoProperties = @($script:Name
                                   $script:Version,
                                   $script:Guid,
                                   $script:Path,
                                   $script:ScriptBase,
                                   $script:Description,
                                   $script:Author,
                                   $script:CompanyName,
                                   $script:Copyright,
                                   $script:Tags,
                                   $script:ReleaseNotes,
                                   $script:RequiredModules,
                                   $script:ExternalModuleDependencies,
                                   $script:RequiredScripts,
                                   $script:ExternalScriptDependencies,
                                   $script:LicenseUri,
                                   $script:ProjectUri,
                                   $script:IconUri,
                                   $script:DefinedCommands,
                                   $script:DefinedFunctions,
                                   $script:DefinedWorkflows
                                   )

$script:SystemEnvironmentKey = 'HKLM:\System\CurrentControlSet\Control\Session Manager\Environment'
$script:UserEnvironmentKey = 'HKCU:\Environment'
$script:SystemEnvironmentVariableMaximumLength = 1024
$script:UserEnvironmentVariableMaximumLength = 255
$script:EnvironmentVariableTarget = @{ Process = 0; User = 1; Machine = 2 }

# Wildcard pattern matching configuration.
$script:wildcardOptions = [System.Management.Automation.WildcardOptions]::CultureInvariant -bor `
                          [System.Management.Automation.WildcardOptions]::IgnoreCase

$script:DynamicOptionTypeMap = @{
                                    0 = [string];       # String
                                    1 = [string[]];     # StringArray
                                    2 = [int];          # Int
                                    3 = [switch];       # Switch
                                    4 = [string];       # Folder
                                    5 = [string];       # File
                                    6 = [string];       # Path
                                    7 = [Uri];          # Uri
                                    8 = [SecureString]; #SecureString
                                }
#endregion script variables

#region Module message resolvers
$script:PackageManagementMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                return (PackageManagementMessageResolver -MsgId $i, -Message $Message)			
                                            }		

$script:PackageManagementSaveModuleMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallModulewhatIfMessage
                                                $QuerySaveUntrustedPackage = $LocalizedData.QuerySaveUntrustedPackage

                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Save-Module" }
                                                    'QueryInstallUntrustedPackage' {return $QuerySaveUntrustedPackage}
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        $Message = $Message -creplace "Install", "Download"
                                                        $Message = $Message -creplace "install", "download"
                                                        return (PackageManagementMessageResolver -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }

$script:PackageManagementInstallModuleMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallModulewhatIfMessage

                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Install-Module" }
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolver -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }		

$script:PackageManagementUnInstallModuleMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallModulewhatIfMessage
                                                switch ($i)
                                                {
                                                    'ActionUninstallPackage' { return "Uninstall-Module" }              
                                                    'TargetPackageVersion' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolver -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }		

$script:PackageManagementUpdateModuleMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = ($LocalizedData.UpdateModulewhatIfMessage -replace "__OLDVERSION__",$($psgetItemInfo.Version))                                                
                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Update-Module" }              
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolver -MsgId $i, -Message $Message)
                                                     }
                                                }                                     
                                            }
                                            
function PackageManagementMessageResolver($MsgID, $Message) {    
              	$NoMatchFound = $LocalizedData.NoMatchFound
              	$SourceNotFound = $LocalizedData.SourceNotFound              
                $ModuleIsNotTrusted = $LocalizedData.ModuleIsNotTrusted
                $RepositoryIsNotTrusted = $LocalizedData.RepositoryIsNotTrusted
                $QueryInstallUntrustedPackage = $LocalizedData.QueryInstallUntrustedPackage

                switch ($MsgID)
                {
                   'NoMatchFound' { return $NoMatchFound }
                   'SourceNotFound' { return $SourceNotFound }
                   'CaptionPackageNotTrusted' { return $ModuleIsNotTrusted }
                   'CaptionSourceNotTrusted' { return $RepositoryIsNotTrusted }
                   'QueryInstallUntrustedPackage' {return $QueryInstallUntrustedPackage}
                    Default {
                        if($Message)
                        {
                            $tempMessage = $Message     -creplace "PackageSource", "PSRepository"
                            $tempMessage = $tempMessage -creplace "packagesource", "psrepository"
                            $tempMessage = $tempMessage -creplace "Package", "Module"
                            $tempMessage = $tempMessage -creplace "package", "module"
                            $tempMessage = $tempMessage -creplace "Sources", "Repositories"
                            $tempMessage = $tempMessage -creplace "sources", "repositories"
                            $tempMessage = $tempMessage -creplace "Source", "Repository"
                            $tempMessage = $tempMessage -creplace "source", "repository"

                            return $tempMessage
                        }
                    }
                }    
}                                    		

#endregion Module message resolvers

#region Script message resolvers
$script:PackageManagementMessageResolverScriptBlockForScriptCmdlets =  {
                                                param($i, $Message)
                                                return (PackageManagementMessageResolverForScripts -MsgId $i, -Message $Message)			
                                            }		

$script:PackageManagementSaveScriptMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallScriptwhatIfMessage
                                                $QuerySaveUntrustedPackage = $LocalizedData.QuerySaveUntrustedScriptPackage

                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Save-Script" }
                                                    'QueryInstallUntrustedPackage' {return $QuerySaveUntrustedPackage}
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        $Message = $Message -creplace "Install", "Download"
                                                        $Message = $Message -creplace "install", "download"
                                                        return (PackageManagementMessageResolverForScripts -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }

$script:PackageManagementInstallScriptMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallScriptwhatIfMessage

                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Install-Script" }
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolverForScripts -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }		

$script:PackageManagementUnInstallScriptMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = $LocalizedData.InstallScriptwhatIfMessage
                                                switch ($i)
                                                {
                                                    'ActionUninstallPackage' { return "Uninstall-Script" }              
                                                    'TargetPackageVersion' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolverForScripts -MsgId $i, -Message $Message)
                                                     }
                                                }                                                
                                            }		

$script:PackageManagementUpdateScriptMessageResolverScriptBlock =  {
                                                param($i, $Message)
                                                $PackageTarget = ($LocalizedData.UpdateScriptwhatIfMessage -replace "__OLDVERSION__",$($psgetItemInfo.Version))                                                
                                                switch ($i)
                                                {
                                                    'ActionInstallPackage' { return "Update-Script" }              
                                                    'TargetPackage' { return $PackageTarget }
                                                     Default {
                                                        return (PackageManagementMessageResolverForScripts -MsgId $i, -Message $Message)
                                                     }
                                                }                                     
                                            }
                                            
function PackageManagementMessageResolverForScripts($MsgID, $Message) {    
              	$NoMatchFound = $LocalizedData.NoMatchFoundForScriptName
              	$SourceNotFound = $LocalizedData.SourceNotFound              
                $ScriptIsNotTrusted = $LocalizedData.ScriptIsNotTrusted
                $RepositoryIsNotTrusted = $LocalizedData.RepositoryIsNotTrusted
                $QueryInstallUntrustedPackage = $LocalizedData.QueryInstallUntrustedScriptPackage

                switch ($MsgID)
                {
                   'NoMatchFound' { return $NoMatchFound }
                   'SourceNotFound' { return $SourceNotFound }
                   'CaptionPackageNotTrusted' { return $ScriptIsNotTrusted }
                   'CaptionSourceNotTrusted' { return $RepositoryIsNotTrusted }
                   'QueryInstallUntrustedPackage' {return $QueryInstallUntrustedPackage}
                    Default {
                        if($Message)
                        {
                            $tempMessage = $Message     -creplace "PackageSource", "PSRepository"
                            $tempMessage = $tempMessage -creplace "packagesource", "psrepository"
                            $tempMessage = $tempMessage -creplace "Package", "Script"
                            $tempMessage = $tempMessage -creplace "package", "script"
                            $tempMessage = $tempMessage -creplace "Sources", "Repositories"
                            $tempMessage = $tempMessage -creplace "sources", "repositories"
                            $tempMessage = $tempMessage -creplace "Source", "Repository"
                            $tempMessage = $tempMessage -creplace "source", "repository"

                            return $tempMessage
                        }
                    }
                }    
}                                    		

#endregion Script message resolvers

Microsoft.PowerShell.Utility\Import-LocalizedData  LocalizedData -filename PSGet.Resource.psd1

#region Add .Net type for Telemetry APIs and WebProxy

# This code is required to add a .Net type and call the Telemetry APIs 
# This is required since PowerShell does not support generation of .Net Anonymous types
#
$requiredAssembly = @( "system.management.automation, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35" )

$source = @" 
using System; 
using System.Net;
using System.Management.Automation;
using Microsoft.Win32.SafeHandles;
using System.Security.Cryptography;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell.Commands.PowerShellGet 
{ 
    public static class Telemetry  
    { 
        public static void TraceMessageArtifactsNotFound(string[] artifactsNotFound, string operationName) 
        { 
            Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI.TraceMessage(operationName, new { ArtifactsNotFound = artifactsNotFound });
        }         
        
        public static void TraceMessageNonPSGalleryRegistration(string sourceLocationType, string sourceLocationHash, string installationPolicy, string packageManagementProvider, string publishLocationHash, string scriptSourceLocationHash, string scriptPublishLocationHash, string operationName) 
        { 
            Microsoft.PowerShell.Telemetry.Internal.TelemetryAPI.TraceMessage(operationName, new { SourceLocationType = sourceLocationType, SourceLocationHash = sourceLocationHash, InstallationPolicy = installationPolicy, PackageManagementProvider = packageManagementProvider, PublishLocationHash = publishLocationHash, ScriptSourceLocationHash = scriptSourceLocationHash, ScriptPublishLocationHash = scriptPublishLocationHash });
        }         
        
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct CERT_CHAIN_POLICY_PARA {
        public CERT_CHAIN_POLICY_PARA(int size) {
            cbSize = (uint) size;
            dwFlags = 0;
            pvExtraPolicyPara = IntPtr.Zero;
        }
        public uint   cbSize;
        public uint   dwFlags;
        public IntPtr pvExtraPolicyPara; 
    }

    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public struct CERT_CHAIN_POLICY_STATUS {
        public CERT_CHAIN_POLICY_STATUS(int size) {
            cbSize = (uint) size;
            dwError = 0;
            lChainIndex = IntPtr.Zero;
            lElementIndex = IntPtr.Zero;
            pvExtraPolicyStatus = IntPtr.Zero;
        }
        public uint   cbSize;
        public uint   dwError;
        public IntPtr lChainIndex;
        public IntPtr lElementIndex;
        public IntPtr pvExtraPolicyStatus; 
    }

    public class Win32Helpers
    {
        [DllImport("Crypt32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        public extern static 
        bool CertVerifyCertificateChainPolicy(
            [In]     IntPtr                       pszPolicyOID,
            [In]     SafeX509ChainHandle pChainContext,
            [In]     ref CERT_CHAIN_POLICY_PARA   pPolicyPara,
            [In,Out] ref CERT_CHAIN_POLICY_STATUS pPolicyStatus);

        [DllImport("Crypt32.dll", CharSet=CharSet.Auto, SetLastError=true)]
        public static extern
        SafeX509ChainHandle CertDuplicateCertificateChain(
            [In]     IntPtr pChainContext);

        public static bool IsMicrosoftCertificate([In] SafeX509ChainHandle pChainContext)
        {
            //-------------------------------------------------------------------------
            //  CERT_CHAIN_POLICY_MICROSOFT_ROOT  
            //  
            //  Checks if the last element of the first simple chain contains a  
            //  Microsoft root public key. If it doesn't contain a Microsoft root  
            //  public key, dwError is set to CERT_E_UNTRUSTEDROOT.  
            //  
            //  pPolicyPara is optional. However,  
            //  MICROSOFT_ROOT_CERT_CHAIN_POLICY_ENABLE_TEST_ROOT_FLAG can be set in  
            //  the dwFlags in pPolicyPara to also check for the Microsoft Test Roots.  
            //  
            //  MICROSOFT_ROOT_CERT_CHAIN_POLICY_CHECK_APPLICATION_ROOT_FLAG can be set  
            //  in the dwFlags in pPolicyPara to check for the Microsoft root for  
            //  application signing instead of the Microsoft product root. This flag  
            //  explicitly checks for the application root only and cannot be combined  
            //  with the test root flag.    
            //  
            //  MICROSOFT_ROOT_CERT_CHAIN_POLICY_DISABLE_FLIGHT_ROOT_FLAG can be set  
            //  in the dwFlags in pPolicyPara to always disable the Flight root.  
            //  
            //  pvExtraPolicyPara and pvExtraPolicyStatus aren't used and must be set  
            //  to NULL.  
            //--------------------------------------------------------------------------  
            const uint MICROSOFT_ROOT_CERT_CHAIN_POLICY_ENABLE_TEST_ROOT_FLAG       = 0x00010000;
            //const uint MICROSOFT_ROOT_CERT_CHAIN_POLICY_CHECK_APPLICATION_ROOT_FLAG = 0x00020000;
            //const uint MICROSOFT_ROOT_CERT_CHAIN_POLICY_DISABLE_FLIGHT_ROOT_FLAG    = 0x00040000;

            CERT_CHAIN_POLICY_PARA PolicyPara = new CERT_CHAIN_POLICY_PARA(Marshal.SizeOf(typeof(CERT_CHAIN_POLICY_PARA)));
            CERT_CHAIN_POLICY_STATUS PolicyStatus = new CERT_CHAIN_POLICY_STATUS(Marshal.SizeOf(typeof(CERT_CHAIN_POLICY_STATUS)));
            int CERT_CHAIN_POLICY_MICROSOFT_ROOT = 7;
            
            PolicyPara.dwFlags = (uint) MICROSOFT_ROOT_CERT_CHAIN_POLICY_ENABLE_TEST_ROOT_FLAG;
            
            if(!CertVerifyCertificateChainPolicy(new IntPtr(CERT_CHAIN_POLICY_MICROSOFT_ROOT),
                                                 pChainContext,
                                                 ref PolicyPara,
                                                 ref PolicyStatus))
            {
                return false;
            }

            return (PolicyStatus.dwError == 0);
        }
    }
} 
"@ 

# Telemetry is turned off by default.
$script:TelemetryEnabled = $false

try
{
    # If the telemetry namespace/methods are not found flow goes to the catch block where telemetry is disabled
    $telemetryMethods = ([Microsoft.PowerShell.Commands.PowerShellGet.Telemetry] | Get-Member -Static).Name

    if ($telemetryMethods.Contains("TraceMessageArtifactsNotFound") -and $telemetryMethods.Contains("TraceMessageNonPSGalleryRegistration"))
    {
        # Turn ON Telemetry if the infrastructure is present on the machine
        $script:TelemetryEnabled = $true
    }
}
catch
{
    # Ignore the error and try adding the type below
}

if(-not $script:TelemetryEnabled -and (IsWindows))
{
    try
    {
        Add-Type -ReferencedAssemblies $requiredAssembly -TypeDefinition $source -Language CSharp -ErrorAction SilentlyContinue
    
        # If the telemetry namespace/methods are not found flow goes to the catch block where telemetry is disabled
        $telemetryMethods = ([Microsoft.PowerShell.Commands.PowerShellGet.Telemetry] | Get-Member -Static).Name

        if ($telemetryMethods.Contains("TraceMessageArtifactsNotFound") -and $telemetryMethods.Contains("TraceMessageNonPSGalleryRegistration"))
        {
            # Turn ON Telemetry if the infrastructure is present on the machine
            $script:TelemetryEnabled = $true
        }
    }
    catch
    {
        # Disable Telemetry if there are any issues finding/loading the Telemetry infrastructure
        $script:TelemetryEnabled = $false
    }
}

$RequiredAssembliesForInternalWebProxy = @( "$([System.Net.IWebProxy].AssemblyQualifiedName)".Substring('System.Net.IWebProxy'.Length+1).Trim(), 
                                            "$([System.Uri].AssemblyQualifiedName)".Substring('System.Uri'.Length+1).Trim() )

$SourceForInternalWebProxy = @" 
using System; 
using System.Net;

namespace Microsoft.PowerShell.Commands.PowerShellGet 
{ 
    /// <summary>
    /// Used by Ping-Endpoint function to supply webproxy to HttpClient
    /// We cannot use System.Net.WebProxy because this is not available on CoreClr
    /// </summary>
    public class InternalWebProxy : IWebProxy
    {
        Uri _proxyUri;
        ICredentials _credentials;

        public InternalWebProxy(Uri uri, ICredentials credentials)
        {
            Credentials = credentials;
            _proxyUri = uri;
        }

        /// <summary>
        /// Credentials used by WebProxy
        /// </summary>
        public ICredentials Credentials
        {
            get
            {
                return _credentials;
            }
            set
            {
                _credentials = value;
            }
        }

        public Uri GetProxy(Uri destination)
        {
            return _proxyUri;
        }

        public bool IsBypassed(Uri host)
        {
            return false;
        }
    }
} 
"@ 

if(-not ('Microsoft.PowerShell.Commands.PowerShellGet.InternalWebProxy' -as [Type]))
{
    Add-Type -ReferencedAssemblies $RequiredAssembliesForInternalWebProxy `
             -TypeDefinition $SourceForInternalWebProxy `
             -Language CSharp `
             -ErrorAction SilentlyContinue
}

#endregion

#region *-Module cmdlets
function Publish-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(SupportsShouldProcess=$true,
                   PositionalBinding=$false,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkID=398575',
                   DefaultParameterSetName="ModuleNameParameterSet")]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ParameterSetName="ModuleNameParameterSet",
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name,

        [Parameter(Mandatory=$true, 
                   ParameterSetName="ModulePathParameterSet",
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(ParameterSetName="ModuleNameParameterSet")]
        [ValidateNotNullOrEmpty()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $NuGetApiKey,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Repository = $Script:PSGalleryModuleSource,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()] 
        [ValidateSet("1.0")]
        [Version]
        $FormatVersion,

        [Parameter()]
        [string[]]
        $ReleaseNotes,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Tags,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $LicenseUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $IconUri,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ProjectUri,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        if($script:isNanoServer -or (IsCoreCLR)) {
            $message = $LocalizedData.PublishPSArtifactUnsupportedOnNano -f "Module"
            ThrowError -ExceptionName "System.InvalidOperationException" `
                        -ExceptionMessage $message `
                        -ErrorId "PublishModuleIsNotSupportedOnNanoServer" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $PSCmdlet `
                        -ErrorCategory InvalidOperation
        }

        Get-PSGalleryApiAvailability -Repository $Repository
        
        if($LicenseUri -and -not (Test-WebUri -uri $LicenseUri))
        {
            $message = $LocalizedData.InvalidWebUri -f ($LicenseUri, "LicenseUri")
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InvalidWebUri" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $LicenseUri
        }

        if($IconUri -and -not (Test-WebUri -uri $IconUri))
        {
            $message = $LocalizedData.InvalidWebUri -f ($IconUri, "IconUri")
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InvalidWebUri" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $IconUri
        }

        if($ProjectUri -and -not (Test-WebUri -uri $ProjectUri))
        {
            $message = $LocalizedData.InvalidWebUri -f ($ProjectUri, "ProjectUri")
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InvalidWebUri" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $ProjectUri
        }
       
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -BootstrapNuGetExe -Force:$Force
    }

    Process
    {
        if($Repository -eq $Script:PSGalleryModuleSource)
        {
            $moduleSource = Get-PSRepository -Name $Repository -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
            if(-not $moduleSource) 
            {
                $message = $LocalizedData.PSGalleryNotFound -f ($Repository)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId 'PSGalleryNotFound' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Repository
                return
            }            
        }
        else
        {
            $ev = $null
            $moduleSource = Get-PSRepository -Name $Repository -ErrorVariable ev
            if($ev) { return }
        }
        
        $DestinationLocation = $moduleSource.PublishLocation
                
        if(-not $DestinationLocation -or
           (-not (Microsoft.PowerShell.Management\Test-Path $DestinationLocation) -and 
           -not (Test-WebUri -uri $DestinationLocation)))

        {
            $message = $LocalizedData.PSGalleryPublishLocationIsMissing -f ($Repository, $Repository)
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "PSGalleryPublishLocationIsMissing" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $Repository
        }
        
        $message = $LocalizedData.PublishLocation -f ($DestinationLocation)
        Write-Verbose -Message $message

        if(-not $NuGetApiKey.Trim())
        {
            if(Microsoft.PowerShell.Management\Test-Path -Path $DestinationLocation)
            {
                $NuGetApiKey = "$(Get-Random)"
            }
            else
            {
                $message = $LocalizedData.NuGetApiKeyIsRequiredForNuGetBasedGalleryService -f ($Repository, $DestinationLocation)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "NuGetApiKeyIsRequiredForNuGetBasedGalleryService" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
            }
        }

        $providerName = Get-ProviderName -PSCustomObject $moduleSource
        if($providerName -ne $script:NuGetProviderName)
        {
            $message = $LocalizedData.PublishModuleSupportsOnlyNuGetBasedPublishLocations -f ($moduleSource.PublishLocation, $Repository, $Repository)
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "PublishModuleSupportsOnlyNuGetBasedPublishLocations" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $Repository
        }

        $moduleName = $null

        if($Name)
        {
            $module = Microsoft.PowerShell.Core\Get-Module -ListAvailable -Name $Name -Verbose:$false | 
                          Microsoft.PowerShell.Core\Where-Object {-not $RequiredVersion -or ($RequiredVersion -eq $_.Version)} 

            if(-not $module)
            {
                if($RequiredVersion)
                {
                    $message = $LocalizedData.ModuleWithRequiredVersionNotAvailableLocally -f ($Name, $RequiredVersion)
                }
                else
                {
                    $message = $LocalizedData.ModuleNotAvailableLocally -f ($Name)
                }

                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "ModuleNotAvailableLocallyToPublish" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Name

            }
            elseif($module.GetType().ToString() -ne "System.Management.Automation.PSModuleInfo")
            {
                $message = $LocalizedData.AmbiguousModuleName -f ($Name)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "AmbiguousModuleNameToPublish" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Name
            }

            $moduleName = $module.Name
            $Path = $module.ModuleBase
        }
        else
        {
            $resolvedPath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if(-not $resolvedPath -or 
               -not (Microsoft.PowerShell.Management\Test-Path -Path $resolvedPath -PathType Container))
            {
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage ($LocalizedData.PathIsNotADirectory -f ($Path)) `
                           -ErrorId "PathIsNotADirectory" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Path
                return
            }

            $moduleName = Microsoft.PowerShell.Management\Split-Path -Path $resolvedPath -Leaf
            $modulePathWithVersion = $false
        
            # if the Leaf of the $resolvedPath is a version, use its parent folder name as the module name
            $ModuleVersion = New-Object System.Version
            if([System.Version]::TryParse($moduleName, ([ref]$ModuleVersion)))
            {
                $moduleName = Microsoft.PowerShell.Management\Split-Path -Path (Microsoft.PowerShell.Management\Split-Path $resolvedPath -Parent) -Leaf
                $modulePathWithVersion = $true
            }

            $manifestPath = Join-Path -Path $resolvedPath -ChildPath "$moduleName.psd1"
            $module = $null

            if(Microsoft.PowerShell.Management\Test-Path -Path $manifestPath -PathType Leaf)
            {            
                $ev = $null            
                $module = Microsoft.PowerShell.Core\Test-ModuleManifest -Path $manifestPath `
                                                                        -ErrorVariable ev `
                                                                        -Verbose:$VerbosePreference
                if($ev)
                {
                    # Above Test-ModuleManifest cmdlet should write an errors to the Errors stream and Console.
                    return
                }
            }
            elseif(-not $modulePathWithVersion -and ($PSVersionTable.PSVersion -ge '5.0.0'))
            {
                $module = Microsoft.PowerShell.Core\Get-Module -Name $resolvedPath -ListAvailable -ErrorAction SilentlyContinue -Verbose:$false
            }

            if(-not $module)
            {
                $message = $LocalizedData.InvalidModulePathToPublish -f ($Path)

                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId 'InvalidModulePathToPublish' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Path
            }
            elseif($module.GetType().ToString() -ne "System.Management.Automation.PSModuleInfo")
            {
                $message = $LocalizedData.AmbiguousModulePath -f ($Path)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId 'AmbiguousModulePathToPublish' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Path
            }

            if($module -and (-not $module.Path.EndsWith('.psd1', [System.StringComparison]::OrdinalIgnoreCase)))
            {
                $message = $LocalizedData.InvalidModuleToPublish -f ($module.Name)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidModuleToPublish" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation `
                           -ExceptionObject $module.Name
            }

            $moduleName = $module.Name
            $Path = $module.ModuleBase
        }

        $message = $LocalizedData.PublishModuleLocation -f ($moduleName, $Path)
        Write-Verbose -Message $message

        #If users are providing tags using -Tags while running PS 5.0, will show warning messages
        if($Tags)
        {
            $message = $LocalizedData.TagsShouldBeIncludedInManifestFile -f ($moduleName, $Path)
            Write-Warning $message 
        }

        if($ReleaseNotes)
        {
            $message = $LocalizedData.ReleaseNotesShouldBeIncludedInManifestFile -f ($moduleName, $Path)
            Write-Warning $message 
        }

        if($LicenseUri)
        {
            $message = $LocalizedData.LicenseUriShouldBeIncludedInManifestFile -f ($moduleName, $Path)
            Write-Warning $message
        }

        if($IconUri)
        {
            $message = $LocalizedData.IconUriShouldBeIncludedInManifestFile -f ($moduleName, $Path)
            Write-Warning $message
        }

        if($ProjectUri)
        {
            $message = $LocalizedData.ProjectUriShouldBeIncludedInManifestFile -f ($moduleName, $Path)
            Write-Warning $message
        }


        # Copy the source module to temp location to publish
        $tempModulePath = Microsoft.PowerShell.Management\Join-Path -Path $script:TempPath `
                              -ChildPath "$(Microsoft.PowerShell.Utility\Get-Random)\$moduleName"

        if(-not $FormatVersion)
        {
            $tempModulePathForFormatVersion = $tempModulePath
        }
        elseif ($FormatVersion -eq "1.0")
        {
            $tempModulePathForFormatVersion = Microsoft.PowerShell.Management\Join-Path $tempModulePath "Content\Deployment\$script:ModuleReferences\$moduleName"
        }

        $null = Microsoft.PowerShell.Management\New-Item -Path $tempModulePathForFormatVersion -ItemType Directory -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        Microsoft.PowerShell.Management\Copy-Item -Path "$Path\*" -Destination $tempModulePathForFormatVersion -Force -Recurse -Confirm:$false -WhatIf:$false

        try
        {
            $manifestPath = Microsoft.PowerShell.Management\Join-Path $tempModulePathForFormatVersion "$moduleName.psd1"
        
            if(-not (Microsoft.PowerShell.Management\Test-Path $manifestPath))
            {
                $message = $LocalizedData.InvalidModuleToPublish -f ($moduleName)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidModuleToPublish" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation `
                           -ExceptionObject $moduleName
            }

            $ev = $null
            $moduleInfo = Microsoft.PowerShell.Core\Test-ModuleManifest -Path $manifestPath `
                                                                        -ErrorVariable ev `
                                                                        -Verbose:$VerbosePreference
            if($ev)
            {
                # Above Test-ModuleManifest cmdlet should write an errors to the Errors stream and Console.
                return
            }

            if(-not $moduleInfo -or 
               -not $moduleInfo.Author -or 
               -not $moduleInfo.Description)
            {
                $message = $LocalizedData.MissingRequiredManifestKeys -f ($moduleName)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "MissingRequiredModuleManifestKeys" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation `
                           -ExceptionObject $moduleName
            }

            $FindParameters = @{
                Name = $moduleName
                Repository = $Repository
                Tag = 'PSScript'
                Verbose = $VerbosePreference
                ErrorAction = 'SilentlyContinue'
                WarningAction = 'SilentlyContinue'
                Debug = $DebugPreference
            }

            if($Credential)
            {
                $FindParameters[$script:Credential] = $Credential
            }

            # Check if the specified module name is already used for a script on the specified repository
            # Use Find-Script to check if that name is already used as scriptname
            $scriptPSGetItemInfo = Find-Script @FindParameters | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $moduleName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore
            if($scriptPSGetItemInfo)
            {
                $message = $LocalizedData.SpecifiedNameIsAlearyUsed -f ($moduleName, $Repository, 'Find-Script')
                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "SpecifiedNameIsAlearyUsed" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation `
                           -ExceptionObject $moduleName
            }

            $null = $FindParameters.Remove('Tag')
            $currentPSGetItemInfo = Find-Module @FindParameters | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $moduleInfo.Name} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if($currentPSGetItemInfo)
            {
                if($currentPSGetItemInfo.Version -eq $moduleInfo.Version)
                {
                    $message = $LocalizedData.ModuleVersionIsAlreadyAvailableInTheGallery -f ($moduleInfo.Name, $moduleInfo.Version, $currentPSGetItemInfo.Version, $currentPSGetItemInfo.RepositorySourceLocation)
                    ThrowError -ExceptionName 'System.InvalidOperationException' `
                               -ExceptionMessage $message `
                               -ErrorId 'ModuleVersionIsAlreadyAvailableInTheGallery' `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation
                }
                elseif(-not $Force -and ($currentPSGetItemInfo.Version -gt $moduleInfo.Version))
                {
                    $message = $LocalizedData.ModuleVersionShouldBeGreaterThanGalleryVersion -f ($moduleInfo.Name, $moduleInfo.Version, $currentPSGetItemInfo.Version, $currentPSGetItemInfo.RepositorySourceLocation)
                    ThrowError -ExceptionName "System.InvalidOperationException" `
                               -ExceptionMessage $message `
                               -ErrorId "ModuleVersionShouldBeGreaterThanGalleryVersion" `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation
                }
            }

            $shouldProcessMessage = $LocalizedData.PublishModulewhatIfMessage -f ($moduleInfo.Version, $moduleInfo.Name)
            if($Force -or $PSCmdlet.ShouldProcess($shouldProcessMessage, "Publish-Module"))
            {
                Publish-PSArtifactUtility -PSModuleInfo $moduleInfo `
                                          -ManifestPath $manifestPath `
                                          -NugetApiKey $NuGetApiKey `
                                          -Destination $DestinationLocation `
                                          -Repository $Repository `
                                          -NugetPackageRoot $tempModulePath `
                                          -FormatVersion $FormatVersion `
                                          -ReleaseNotes $($ReleaseNotes -join "`r`n") `
                                          -Tags $Tags `
                                          -LicenseUri $LicenseUri `
                                          -IconUri $IconUri `
                                          -ProjectUri $ProjectUri `
                                          -Verbose:$VerbosePreference `
                                          -WarningAction $WarningPreference `
                                          -ErrorAction $ErrorActionPreference `
                                          -Debug:$DebugPreference
            }
        }
        finally
        {
            Microsoft.PowerShell.Management\Remove-Item $tempModulePath -Force -Recurse -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }
    }
}

function Find-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkID=398574')]
    [outputtype("PSCustomObject[]")]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [switch]
        $AllVersions,

        [Parameter()]
        [switch]
        $IncludeDependencies,

        [Parameter()]
        [ValidateNotNull()]
        [string]
        $Filter,
        
        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Tag,

        [Parameter()]
        [ValidateNotNull()]
        [ValidateSet('DscResource','Cmdlet','Function','RoleCapability')]
        [string[]]
        $Includes,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $DscResource,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $RoleCapability,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Command,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
        
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential
    }

    Process
    {
        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MinimumVersion $MinimumVersion `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion `
                                                       -AllVersions:$AllVersions

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule
                
        if($PSBoundParameters.ContainsKey("Repository"))
        {
            $PSBoundParameters["Source"] = $Repository
            $null = $PSBoundParameters.Remove("Repository")
            
            $ev = $null
            $null = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
            if($ev) { return }
        }
        
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock

        $modulesFoundInPSGallery = @()

        # No Telemetry must be performed if PSGallery is not in the supplied list of Repositories
        $isRepositoryNullOrPSGallerySpecified = $false
        if ($Repository -and ($Repository -Contains $Script:PSGalleryModuleSource)) 
        {
            $isRepositoryNullOrPSGallerySpecified = $true
        }
        elseif(-not $Repository)
        {
            $psgalleryRepo = Get-PSRepository -Name $Script:PSGalleryModuleSource `
                                              -ErrorAction SilentlyContinue `
                                              -WarningAction SilentlyContinue
            if($psgalleryRepo)
            {
                $isRepositoryNullOrPSGallerySpecified = $true
            }
        }
		
		PackageManagement\Find-Package @PSBoundParameters | Microsoft.PowerShell.Core\ForEach-Object {

            $psgetItemInfo = New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeModule 
                                                        
            $psgetItemInfo

            if ($psgetItemInfo -and 
                $isRepositoryNullOrPSGallerySpecified -and 
                $script:TelemetryEnabled -and 
                ($psgetItemInfo.Repository -eq $Script:PSGalleryModuleSource))
            { 
                $modulesFoundInPSGallery += $psgetItemInfo.Name 
            }
        }

        # Perform Telemetry if Repository is not supplied or Repository contains PSGallery
        # We are only interested in finding modules not in PSGallery
        if ($isRepositoryNullOrPSGallerySpecified)
        {
            Log-ArtifactNotFoundInPSGallery -SearchedName $Name -FoundName $modulesFoundInPSGallery -operationName 'PSGET_FIND_MODULE'
        }
    }
}

function Save-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameAndPathParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=531351',
                   SupportsShouldProcess=$true)]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputOjectAndPathParameterSet')]
        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputOjectAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter(Mandatory=$true, ParameterSetName='NameAndPathParameterSet')]
        [Parameter(Mandatory=$true, ParameterSetName='InputOjectAndPathParameterSet')]
        [string]
        $Path,

        [Parameter(Mandatory=$true, ParameterSetName='NameAndLiteralPathParameterSet')]
        [Parameter(Mandatory=$true, ParameterSetName='InputOjectAndLiteralPathParameterSet')]
        [string]
        $LiteralPath,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,
        
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
                
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        # Module names already tried in the current pipeline for InputObject parameterset
        $moduleNamesInPipeline = @()
    }

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementSaveModuleMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule
        
        # When -Force is specified, Path will be created if not available.
        if(-not $Force)
        {
            if($Path)
            {
                $destinationPath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

                if(-not $destinationPath -or -not (Microsoft.PowerShell.Management\Test-path $destinationPath))
                {
                    $errorMessage = ($LocalizedData.PathNotFound -f $Path)
                    ThrowError  -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $errorMessage `
                                -ErrorId "PathNotFound" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $Path `
                                -ErrorCategory InvalidArgument
                }

                $PSBoundParameters['Path'] = $destinationPath
            }
            else
            {
                $destinationPath = Resolve-PathHelper -Path $LiteralPath -IsLiteralPath -CallerPSCmdlet $PSCmdlet | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

                if(-not $destinationPath -or -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $destinationPath))
                {
                    $errorMessage = ($LocalizedData.PathNotFound -f $LiteralPath)
                    ThrowError  -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $errorMessage `
                                -ErrorId "PathNotFound" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $LiteralPath `
                                -ErrorCategory InvalidArgument
                }

                $PSBoundParameters['LiteralPath'] = $destinationPath
            }
        }

        if($Name)
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            if($PSBoundParameters.ContainsKey("Repository"))
            {
                $PSBoundParameters["Source"] = $Repository
                $null = $PSBoundParameters.Remove("Repository")

                $ev = $null
                $null = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
                if($ev) { return }
            }

            $null = PackageManagement\Save-Package @PSBoundParameters
        }
        elseif($InputObject)
        {
            $null = $PSBoundParameters.Remove("InputObject")

            foreach($inputValue in $InputObject)
            {
                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetCommandInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetCommandInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo"))

                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }
                
                if( ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -or
                    ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetCommandInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetCommandInfo") -or
                    ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo"))
                {
                    $psgetModuleInfo = $inputValue.PSGetModuleInfo
                }
                else
                {
                    $psgetModuleInfo = $inputValue                    
                }

                # Skip the module name if it is already tried in the current pipeline
                if($moduleNamesInPipeline -contains $psgetModuleInfo.Name)
                {
                    continue
                }

                $moduleNamesInPipeline += $psgetModuleInfo.Name

                if ($psgetModuleInfo.PowerShellGetFormatVersion -and 
                    ($script:SupportedPSGetFormatVersionMajors -notcontains $psgetModuleInfo.PowerShellGetFormatVersion.Major))
                {
                    $message = $LocalizedData.NotSupportedPowerShellGetFormatVersion -f ($psgetModuleInfo.Name, $psgetModuleInfo.PowerShellGetFormatVersion, $psgetModuleInfo.Name)
                    Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                    continue
                }

                $PSBoundParameters["Name"] = $psgetModuleInfo.Name
                $PSBoundParameters["RequiredVersion"] = $psgetModuleInfo.Version
                $PSBoundParameters['Source'] = $psgetModuleInfo.Repository
                $PSBoundParameters["PackageManagementProvider"] = (Get-ProviderName -PSCustomObject $psgetModuleInfo)
                
                $null = PackageManagement\Save-Package @PSBoundParameters
            }
        }
    }
}

function Install-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkID=398573',
                   SupportsShouldProcess=$true)]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputObject')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()] 
        [ValidateSet("CurrentUser","AllUsers")]
        [string]
        $Scope = "AllUsers",

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [switch]
        $AllowClobber,

        [Parameter()]
        [switch]
        $SkipPublisherCheck,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
        
        if(-not (Test-RunningAsElevated) -and ($Scope -ne "CurrentUser"))
        {
            # Throw an error when Install-Module is used as a non-admin user and '-Scope CurrentUser' is not specified
            $message = $LocalizedData.InstallModuleNeedsCurrentUserScopeParameterForNonAdminUser -f @($script:programFilesModulesPath, $script:MyDocumentsModulesPath)

            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InstallModuleNeedsCurrentUserScopeParameterForNonAdminUser" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument
        }

        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        # Module names already tried in the current pipeline for InputObject parameterset
        $moduleNamesInPipeline = @()
        $YesToAll = $false
        $NoToAll = $false
        $SourceSGrantedTrust = @()
        $SourcesDeniedTrust = @()
    }

    Process
    {
        $RepositoryIsNotTrusted = $LocalizedData.RepositoryIsNotTrusted
        $QueryInstallUntrustedPackage = $LocalizedData.QueryInstallUntrustedPackage
        $PackageTarget = $LocalizedData.InstallModulewhatIfMessage
        	
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementInstallModuleMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule
        $PSBoundParameters['Scope'] = $Scope

        if($PSCmdlet.ParameterSetName -eq "NameParameterSet")
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            if($PSBoundParameters.ContainsKey("Repository"))
            {
                $PSBoundParameters["Source"] = $Repository
                $null = $PSBoundParameters.Remove("Repository")

                $ev = $null
                $null = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
                if($ev) { return }
            }

            $null = PackageManagement\Install-Package @PSBoundParameters
        }
        elseif($PSCmdlet.ParameterSetName -eq "InputObject")
        {
            $null = $PSBoundParameters.Remove("InputObject")

            foreach($inputValue in $InputObject)
            {
                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetCommandInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetCommandInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo"))
                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }
                
                if( ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetDscResourceInfo") -or
                    ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetCommandInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetCommandInfo") -or                    
                    ($inputValue.PSTypeNames -contains "Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo") -or
                    ($inputValue.PSTypeNames -contains "Deserialized.Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo"))
                {
                    $psgetModuleInfo = $inputValue.PSGetModuleInfo
                }
                else
                {
                    $psgetModuleInfo = $inputValue                    
                }

                # Skip the module name if it is already tried in the current pipeline
                if($moduleNamesInPipeline -contains $psgetModuleInfo.Name)
                {
                    continue
                }

                $moduleNamesInPipeline += $psgetModuleInfo.Name

                if ($psgetModuleInfo.PowerShellGetFormatVersion -and 
                    ($script:SupportedPSGetFormatVersionMajors -notcontains $psgetModuleInfo.PowerShellGetFormatVersion.Major))
                {
                    $message = $LocalizedData.NotSupportedPowerShellGetFormatVersion -f ($psgetModuleInfo.Name, $psgetModuleInfo.PowerShellGetFormatVersion, $psgetModuleInfo.Name)
                    Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                    continue
                }

                $PSBoundParameters["Name"] = $psgetModuleInfo.Name
                $PSBoundParameters["RequiredVersion"] = $psgetModuleInfo.Version
                $PSBoundParameters['Source'] = $psgetModuleInfo.Repository
                $PSBoundParameters["PackageManagementProvider"] = (Get-ProviderName -PSCustomObject $psgetModuleInfo)

                #Check if module is already installed
                $InstalledModuleInfo = Test-ModuleInstalled -Name $psgetModuleInfo.Name -RequiredVersion  $psgetModuleInfo.Version                 
                if(-not $Force -and $InstalledModuleInfo -ne $null)
                {
                    $message = $LocalizedData.ModuleAlreadyInstalledVerbose -f ($InstalledModuleInfo.Version, $InstalledModuleInfo.Name, $InstalledModuleInfo.ModuleBase)
                    Write-Verbose -Message $message
                }
                else
                {
                    $source =  $psgetModuleInfo.Repository
                    $installationPolicy = (Get-PSRepository -Name $source).InstallationPolicy                
                    $ShouldProcessMessage = $PackageTarget -f ($psgetModuleInfo.Name, $psgetModuleInfo.Version)
                
                    if($psCmdlet.ShouldProcess($ShouldProcessMessage))
                    {
                        if($installationPolicy.Equals("Untrusted", [StringComparison]::OrdinalIgnoreCase))
                        {
    	                    if(-not($YesToAll -or $NoToAll -or $SourceSGrantedTrust.Contains($source) -or $sourcesDeniedTrust.Contains($source) -or $Force))   
                            {
	                            $message = $QueryInstallUntrustedPackage -f ($psgetModuleInfo.Name, $psgetModuleInfo.RepositorySourceLocation)
                                if($PSVersionTable.PSVersion -ge '5.0.0')
                                {
                                     $sourceTrusted = $psCmdlet.ShouldContinue("$message", "$RepositoryIsNotTrusted",$true, [ref]$YesToAll, [ref]$NoToAll)
                                }
                                else
                                {
                                    $sourceTrusted = $psCmdlet.ShouldContinue("$message", "$RepositoryIsNotTrusted", [ref]$YesToAll, [ref]$NoToAll)
                                }                               

                                if($sourceTrusted)
                                {
                                    $SourceSGrantedTrust+=$source
                                }
                                else
                                {
                                    $SourcesDeniedTrust+=$source
                                }
                            }
                        }
                        
                        if($installationPolicy.Equals("trusted", [StringComparison]::OrdinalIgnoreCase) -or $SourceSGrantedTrust.Contains($source) -or $YesToAll -or $Force)
                        {
                            $PSBoundParameters["Force"] = $true                        
	                        $null = PackageManagement\Install-Package @PSBoundParameters
                        }                                  
                    }
                }
            }
        }
    }
}

function Update-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkID=398576')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true, 
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [Switch]
        $Force
    )

    Begin
    {
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        # Module names already tried in the current pipeline
        $moduleNamesInPipeline = @()
    }

    Process
    {
        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        $GetPackageParameters = @{}
        $GetPackageParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule
        $GetPackageParameters["Provider"] = $script:PSModuleProviderName
        $GetPackageParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock
        $GetPackageParameters['ErrorAction'] = 'SilentlyContinue'
        $GetPackageParameters['WarningAction'] = 'SilentlyContinue'

        $PSGetItemInfos = @()

        if($Name)
        {
            foreach($moduleName in $Name)
            {
                $GetPackageParameters['Name'] = $moduleName
                $installedPackages = PackageManagement\Get-Package @GetPackageParameters
                
                if(-not $installedPackages -and -not (Test-WildcardPattern -Name $moduleName))
                {
                    $availableModules = Get-Module -ListAvailable $moduleName -Verbose:$false | Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore

                    if(-not $availableModules)
                    {                    
                        $message = $LocalizedData.ModuleNotInstalledOnThisMachine -f ($moduleName)
                        Write-Error -Message $message -ErrorId 'ModuleNotInstalledOnThisMachine' -Category InvalidOperation -TargetObject $moduleName
                    }
                    else
                    {
                        $message = $LocalizedData.ModuleNotInstalledUsingPowerShellGet -f ($moduleName)
                        Write-Error -Message $message -ErrorId 'ModuleNotInstalledUsingInstallModuleCmdlet' -Category InvalidOperation -TargetObject $moduleName
                    }

                    continue
                }

                $installedPackages |
                    Microsoft.PowerShell.Core\ForEach-Object {New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeModule} | 
                        Microsoft.PowerShell.Core\ForEach-Object {                    
                            if(-not (Test-RunningAsElevated) -and $_.InstalledLocation.StartsWith($script:programFilesModulesPath, [System.StringComparison]::OrdinalIgnoreCase))
                            {                            
                                if(-not (Test-WildcardPattern -Name $moduleName))
                                {
                                    $message = $LocalizedData.AdminPrivilegesRequiredForUpdate -f ($_.Name, $_.InstalledLocation)
                                    Write-Error -Message $message -ErrorId "AdminPrivilegesAreRequiredForUpdate" -Category InvalidOperation -TargetObject $moduleName
                                }
                                continue
                            }

                            $PSGetItemInfos += $_
                        }
            }
        }
        else
        {

            $PSGetItemInfos = PackageManagement\Get-Package @GetPackageParameters |
                                Microsoft.PowerShell.Core\ForEach-Object {New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeModule} | 
                                    Microsoft.PowerShell.Core\Where-Object {
                                        (Test-RunningAsElevated) -or 
                                        $_.InstalledLocation.StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase)
                                    }
        }


        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementUpdateModuleMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule

        foreach($psgetItemInfo in $PSGetItemInfos)
        {
            # Skip the module name if it is already tried in the current pipeline
            if($moduleNamesInPipeline -contains $psgetItemInfo.Name)
            {
                continue
            }

            $moduleNamesInPipeline += $psgetItemInfo.Name

            $message = $LocalizedData.CheckingForModuleUpdate -f ($psgetItemInfo.Name)
            Write-Verbose -Message $message

            $providerName = Get-ProviderName -PSCustomObject $psgetItemInfo
            if(-not $providerName)
            {
                $providerName = $script:NuGetProviderName
            }

            $PSBoundParameters["Name"] = $psgetItemInfo.Name
            $PSBoundParameters['Source'] = $psgetItemInfo.Repository

            Get-PSGalleryApiAvailability -Repository (Get-SourceName -Location $psgetItemInfo.RepositorySourceLocation)

            $PSBoundParameters["PackageManagementProvider"] = $providerName 
            $PSBoundParameters["InstallUpdate"] = $true

            if($psgetItemInfo.InstalledLocation.ToString().StartsWith($script:MyDocumentsModulesPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $PSBoundParameters["Scope"] = "CurrentUser"
            }
            else
            {
                $PSBoundParameters['Scope'] = 'AllUsers'
            }

            $sid = PackageManagement\Install-Package @PSBoundParameters
        }
    }
}

function Uninstall-Module
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameParameterSet',
                   SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=526864')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   Mandatory=$true, 
                   Position=0,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputObject')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ParameterSetName='NameParameterSet')]
        [switch]
        $AllVersions,

        [Parameter()]
        [Switch]
        $Force
    )

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementUnInstallModuleMessageResolverScriptBlock 
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule

        if($PSCmdlet.ParameterSetName -eq "InputObject")
        {
            $null = $PSBoundParameters.Remove("InputObject")
        
            foreach($inputValue in $InputObject)
            {
                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo"))
                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }

                $PSBoundParameters["Name"] = $inputValue.Name
                $PSBoundParameters["RequiredVersion"] = $inputValue.Version

                $null = PackageManagement\Uninstall-Package @PSBoundParameters
            }
        }
        else
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion `
                                                           -AllVersions:$AllVersions

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            $null = PackageManagement\Uninstall-Package @PSBoundParameters
        }
    }
}

function Get-InstalledModule
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkId=526863')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true, 
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter()]
        [switch]
        $AllVersions
    )

    Process
    {
        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MinimumVersion $MinimumVersion `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion `
                                                       -AllVersions:$AllVersions

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeModule

        PackageManagement\Get-Package @PSBoundParameters | Microsoft.PowerShell.Core\ForEach-Object {New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeModule}  
    }
}

#endregion *-Module cmdlets

#region Find-DscResource cmdlet

function Find-DscResource
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri = 'https://go.microsoft.com/fwlink/?LinkId=517196')]
    [outputtype('PSCustomObject[]')]
    Param
    (
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleName,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [switch]
        $AllVersions,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Tag,

        [Parameter()]
        [ValidateNotNull()]
        [string]
        $Filter,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository
    )


    Process
    {
        $PSBoundParameters['Includes'] = 'DscResource'
        
        if($PSBoundParameters.ContainsKey('Name'))
        {
            $PSBoundParameters['DscResource'] = $Name
            $null = $PSBoundParameters.Remove('Name')
        }

        if($PSBoundParameters.ContainsKey('ModuleName'))
        {
            $PSBoundParameters['Name'] = $ModuleName
            $null = $PSBoundParameters.Remove('ModuleName')
        }        

        PowerShellGet\Find-Module @PSBoundParameters | 
        Microsoft.PowerShell.Core\ForEach-Object {
            $psgetModuleInfo = $_
            $psgetModuleInfo.Includes.DscResource | Microsoft.PowerShell.Core\ForEach-Object {
                if($Name -and ($Name -notcontains $_))
                {
                    return
                }

                $psgetDscResourceInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                        Name            = $_
                        Version         = $psgetModuleInfo.Version
                        ModuleName      = $psgetModuleInfo.Name
                        Repository      = $psgetModuleInfo.Repository
                        PSGetModuleInfo = $psgetModuleInfo
                })

                $psgetDscResourceInfo.PSTypeNames.Insert(0, 'Microsoft.PowerShell.Commands.PSGetDscResourceInfo')
                $psgetDscResourceInfo
            }   
        } 
    }
}

#endregion Find-DscResource cmdlet

#region Find-Command cmdlet

function Find-Command
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri = 'https://go.microsoft.com/fwlink/?LinkId=733636')]
    [outputtype('PSCustomObject[]')]
    Param
    (
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleName,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [switch]
        $AllVersions,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Tag,

        [Parameter()]
        [ValidateNotNull()]
        [string]
        $Filter,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository
    )


    Process
    {
        if($PSBoundParameters.ContainsKey('Name'))
        {
            $PSBoundParameters['Command'] = $Name
            $null = $PSBoundParameters.Remove('Name')
        }
        else
        {
            $PSBoundParameters['Includes'] = @('Cmdlet','Function')
        }

        if($PSBoundParameters.ContainsKey('ModuleName'))
        {
            $PSBoundParameters['Name'] = $ModuleName
            $null = $PSBoundParameters.Remove('ModuleName')
        }        

        PowerShellGet\Find-Module @PSBoundParameters | 
        Microsoft.PowerShell.Core\ForEach-Object {
            $psgetModuleInfo = $_
            $psgetModuleInfo.Includes.Command | Microsoft.PowerShell.Core\ForEach-Object {
                if(($_ -eq "*") -or ($Name -and ($Name -notcontains $_)))
                {
                    return
                }

                $psgetCommandInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                        Name            = $_
                        Version         = $psgetModuleInfo.Version
                        ModuleName      = $psgetModuleInfo.Name
                        Repository      = $psgetModuleInfo.Repository
                        PSGetModuleInfo = $psgetModuleInfo
                })

                $psgetCommandInfo.PSTypeNames.Insert(0, 'Microsoft.PowerShell.Commands.PSGetCommandInfo')
                $psgetCommandInfo
            }
        }
    }
}

#endregion Find-Command cmdlet

#region Find-RoleCapability cmdlet

function Find-RoleCapability
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri = 'https://go.microsoft.com/fwlink/?LinkId=718029')]
    [outputtype('PSCustomObject[]')]
    Param
    (
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleName,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,
        
        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,

        [Parameter()]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [switch]
        $AllVersions,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Tag,

        [Parameter()]
        [ValidateNotNull()]
        [string]
        $Filter,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository
    )


    Process
    {
        $PSBoundParameters['Includes'] = 'RoleCapability'
        
        if($PSBoundParameters.ContainsKey('Name'))
        {
            $PSBoundParameters['RoleCapability'] = $Name
            $null = $PSBoundParameters.Remove('Name')
        }

        if($PSBoundParameters.ContainsKey('ModuleName'))
        {
            $PSBoundParameters['Name'] = $ModuleName
            $null = $PSBoundParameters.Remove('ModuleName')
        }        

        PowerShellGet\Find-Module @PSBoundParameters | 
            Microsoft.PowerShell.Core\ForEach-Object {
                $psgetModuleInfo = $_
                $psgetModuleInfo.Includes.RoleCapability | Microsoft.PowerShell.Core\ForEach-Object {
                    if($Name -and ($Name -notcontains $_))
                    {
                        return
                    }

                    $psgetRoleCapabilityInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                            Name            = $_
                            Version         = $psgetModuleInfo.Version
                            ModuleName      = $psgetModuleInfo.Name
                            Repository      = $psgetModuleInfo.Repository
                            PSGetModuleInfo = $psgetModuleInfo
                    })

                    $psgetRoleCapabilityInfo.PSTypeNames.Insert(0, 'Microsoft.PowerShell.Commands.PSGetRoleCapabilityInfo')
                    $psgetRoleCapabilityInfo
                }   
            } 
    }
}

#endregion Find-RoleCapability cmdlet

#region *-Script cmdlets
function Publish-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(SupportsShouldProcess=$true,
                   PositionalBinding=$false,
                   DefaultParameterSetName='PathParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619788')]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='PathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='LiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $LiteralPath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $NuGetApiKey,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Repository = $Script:PSGalleryModuleSource,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        if($script:isNanoServer -or (IsCoreCLR)) {
            $message = $LocalizedData.PublishPSArtifactUnsupportedOnNano -f "Script"
            ThrowError -ExceptionName "System.InvalidOperationException" `
                        -ExceptionMessage $message `
                        -ErrorId "PublishScriptIsNotSupportedOnNanoServer" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $PSCmdlet `
                        -ErrorCategory InvalidOperation
        }

        Get-PSGalleryApiAvailability -Repository $Repository        

        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -BootstrapNuGetExe -Force:$Force
    }

    Process
    {
        $scriptFilePath = $null
        if($Path)
        {
            $scriptFilePath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | 
                                  Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore
            
            if(-not $scriptFilePath -or 
               -not (Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $Path)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $Path `
                            -ErrorCategory InvalidArgument
            }
        }
        else
        {
            $scriptFilePath = Resolve-PathHelper -Path $LiteralPath -IsLiteralPath -CallerPSCmdlet $PSCmdlet | 
                                  Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if(-not $scriptFilePath -or 
               -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $LiteralPath)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $LiteralPath `
                            -ErrorCategory InvalidArgument
            }
        }

        if(-not $scriptFilePath.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase))
        {
            $errorMessage = ($LocalizedData.InvalidScriptFilePath -f $scriptFilePath)
            ThrowError  -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "InvalidScriptFilePath" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $scriptFilePath `
                        -ErrorCategory InvalidArgument
            return
        }

        if($Repository -eq $Script:PSGalleryModuleSource)
        {
            $repo = Get-PSRepository -Name $Repository -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
            if(-not $repo) 
            {
                $message = $LocalizedData.PSGalleryNotFound -f ($Repository)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId 'PSGalleryNotFound' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Repository
                return
            }            
        }
        else
        {
            $ev = $null
            $repo = Get-PSRepository -Name $Repository -ErrorVariable ev
            if($ev) { return }
        }

        $DestinationLocation = $null

        if(Get-Member -InputObject $repo -Name $script:ScriptPublishLocation)
        {
            $DestinationLocation = $repo.ScriptPublishLocation
        }
        
        if(-not $DestinationLocation -or
           (-not (Microsoft.PowerShell.Management\Test-Path -Path $DestinationLocation) -and 
           -not (Test-WebUri -uri $DestinationLocation)))

        {
            $message = $LocalizedData.PSRepositoryScriptPublishLocationIsMissing -f ($Repository, $Repository)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "PSRepositoryScriptPublishLocationIsMissing" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Repository
        }

        $message = $LocalizedData.PublishLocation -f ($DestinationLocation)
        Write-Verbose -Message $message

        if(-not $NuGetApiKey.Trim())
        {
            if(Microsoft.PowerShell.Management\Test-Path -Path $DestinationLocation)
            {
                $NuGetApiKey = "$(Get-Random)"
            }
            else
            {
                $message = $LocalizedData.NuGetApiKeyIsRequiredForNuGetBasedGalleryService -f ($Repository, $DestinationLocation)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "NuGetApiKeyIsRequiredForNuGetBasedGalleryService" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
            }
        }

        $providerName = Get-ProviderName -PSCustomObject $repo
        if($providerName -ne $script:NuGetProviderName)
        {
            $message = $LocalizedData.PublishScriptSupportsOnlyNuGetBasedPublishLocations -f ($DestinationLocation, $Repository, $Repository)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "PublishScriptSupportsOnlyNuGetBasedPublishLocations" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Repository
        }

        if($Path)
        {
            $PSScriptInfo = Test-ScriptFileInfo -Path $scriptFilePath
        }
        else
        {
            $PSScriptInfo = Test-ScriptFileInfo -LiteralPath $scriptFilePath
        }
       
        if(-not $PSScriptInfo)
        {
            # Test-ScriptFileInfo throws the actual error
            return
        }

        $scriptName = $PSScriptInfo.Name

        # Copy the source script file to temp location to publish
        $tempScriptPath = Microsoft.PowerShell.Management\Join-Path -Path $script:TempPath `
                              -ChildPath "$(Microsoft.PowerShell.Utility\Get-Random)\$scriptName"

        $null = Microsoft.PowerShell.Management\New-Item -Path $tempScriptPath -ItemType Directory -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        if($Path)
        {
            Microsoft.PowerShell.Management\Copy-Item -Path $scriptFilePath -Destination $tempScriptPath -Force -Recurse -Confirm:$false -WhatIf:$false
        }
        else
        {
            Microsoft.PowerShell.Management\Copy-Item -LiteralPath $scriptFilePath -Destination $tempScriptPath -Force -Recurse -Confirm:$false -WhatIf:$false
        }

        try
        {
            $FindParameters = @{
                Name = $scriptName
                Repository = $Repository
                Tag = 'PSModule'
                Verbose = $VerbosePreference
                ErrorAction = 'SilentlyContinue'
                WarningAction = 'SilentlyContinue'
                Debug = $DebugPreference
            }

            if($Credential)
            {
                $FindParameters[$script:Credential] = $Credential
            }

            # Check if the specified script name is already used for a module on the specified repository
            # Use Find-Module to check if that name is already used as module name
            $modulePSGetItemInfo = Find-Module @FindParameters | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $scriptName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore
            if($modulePSGetItemInfo)
            {
                $message = $LocalizedData.SpecifiedNameIsAlearyUsed -f ($scriptName, $Repository, 'Find-Module')
                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "SpecifiedNameIsAlearyUsed" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation `
                           -ExceptionObject $scriptName
            }

            $null = $FindParameters.Remove('Tag')

            $currentPSGetItemInfo = $null
            $currentPSGetItemInfo = Find-Script @FindParameters | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $scriptName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if($currentPSGetItemInfo)
            {
                if($currentPSGetItemInfo.Version -eq $PSScriptInfo.Version)
                {
                    $message = $LocalizedData.ScriptVersionIsAlreadyAvailableInTheGallery -f ($scriptName,
                                                                                              $PSScriptInfo.Version,
                                                                                              $currentPSGetItemInfo.Version,
                                                                                              $currentPSGetItemInfo.RepositorySourceLocation)
                    ThrowError -ExceptionName "System.InvalidOperationException" `
                               -ExceptionMessage $message `
                               -ErrorId 'ScriptVersionIsAlreadyAvailableInTheGallery' `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation
                }
                elseif(-not $Force -and ($currentPSGetItemInfo.Version -gt $PSScriptInfo.Version))
                {
                    $message = $LocalizedData.ScriptVersionShouldBeGreaterThanGalleryVersion -f ($scriptName,
                                                                                                 $PSScriptInfo.Version,
                                                                                                 $currentPSGetItemInfo.Version,
                                                                                                 $currentPSGetItemInfo.RepositorySourceLocation)
                    ThrowError -ExceptionName "System.InvalidOperationException" `
                               -ExceptionMessage $message `
                               -ErrorId "ScriptVersionShouldBeGreaterThanGalleryVersion" `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation
                }
            }

            $shouldProcessMessage = $LocalizedData.PublishScriptwhatIfMessage -f ($PSScriptInfo.Version, $scriptName)
            if($Force -or $PSCmdlet.ShouldProcess($shouldProcessMessage, "Publish-Script"))
            {
                Publish-PSArtifactUtility -PSScriptInfo $PSScriptInfo `
                                          -NugetApiKey $NuGetApiKey `
                                          -Destination $DestinationLocation `
                                          -Repository $Repository `
                                          -NugetPackageRoot $tempScriptPath `
                                          -Verbose:$VerbosePreference `
                                          -WarningAction $WarningPreference `
                                          -ErrorAction $ErrorActionPreference `
                                          -Debug:$DebugPreference
            }
        }
        finally
        {
            Microsoft.PowerShell.Management\Remove-Item $tempScriptPath -Force -Recurse -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }
    }
}

function Find-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkId=619785')]
    [outputtype("PSCustomObject[]")]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [switch]
        $AllVersions,

        [Parameter()]
        [switch]
        $IncludeDependencies,

        [Parameter()]
        [ValidateNotNull()]
        [string]
        $Filter,
        
        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Tag,

        [Parameter()]
        [ValidateNotNull()]
        [ValidateSet('Function','Workflow')]
        [string[]]
        $Includes,

        [Parameter()]
        [ValidateNotNull()]
        [string[]]
        $Command,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
        
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential
    }

    Process
    {
        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MinimumVersion $MinimumVersion `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion `
                                                       -AllVersions:$AllVersions

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        $PSBoundParameters['Provider'] = $script:PSModuleProviderName
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript
                
        if($PSBoundParameters.ContainsKey("Repository"))
        {
            $PSBoundParameters["Source"] = $Repository
            $null = $PSBoundParameters.Remove("Repository")

            $ev = $null
            $repositories = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
            if($ev) { return }

            $RepositoriesWithoutScriptSourceLocation = $false
            foreach($repo in $repositories)
            {
                if(-not $repo.ScriptSourceLocation)
                {
                    $message = $LocalizedData.ScriptSourceLocationIsMissing -f ($repo.Name)
                    Write-Error -Message $message `
                                -ErrorId 'ScriptSourceLocationIsMissing' `
                                -Category InvalidArgument `
                                -TargetObject $repo.Name `
                                -Exception 'System.ArgumentException'

                    $RepositoriesWithoutScriptSourceLocation = $true
                }
            }

            if($RepositoriesWithoutScriptSourceLocation)
            {
                return
            }
        }
        
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlockForScriptCmdlets

        $scriptsFoundInPSGallery = @()

        # No Telemetry must be performed if PSGallery is not in the supplied list of Repositories
        $isRepositoryNullOrPSGallerySpecified = $false
        if ($Repository -and ($Repository -Contains $Script:PSGalleryModuleSource))
        {
            $isRepositoryNullOrPSGallerySpecified = $true
        }
        elseif(-not $Repository)
        {
            $psgalleryRepo = Get-PSRepository -Name $Script:PSGalleryModuleSource `
                                              -ErrorAction SilentlyContinue `
                                              -WarningAction SilentlyContinue
            # And check for IsDefault?
            if($psgalleryRepo)
            {
                $isRepositoryNullOrPSGallerySpecified = $true
            }
        }

        PackageManagement\Find-Package @PSBoundParameters | Microsoft.PowerShell.Core\ForEach-Object {
                $psgetItemInfo = New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeScript 
                                                        
                $psgetItemInfo

                if ($psgetItemInfo -and 
                    $isRepositoryNullOrPSGallerySpecified -and 
                    $script:TelemetryEnabled -and 
                    ($psgetItemInfo.Repository -eq $Script:PSGalleryModuleSource))
                { 
                    $scriptsFoundInPSGallery += $psgetItemInfo.Name 
                }
            }

        # Perform Telemetry if Repository is not supplied or Repository contains PSGallery
        # We are only interested in finding artifacts not in PSGallery
        if ($isRepositoryNullOrPSGallerySpecified)
        {
            Log-ArtifactNotFoundInPSGallery -SearchedName $Name -FoundName $scriptsFoundInPSGallery -operationName PSGET_FIND_SCRIPT
        }
    }
}

function Save-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameAndPathParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619786',
                   SupportsShouldProcess=$true)]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputOjectAndPathParameterSet')]
        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputOjectAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter(Mandatory=$true,
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndPathParameterSet')]

        [Parameter(Mandatory=$true,
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='InputOjectAndPathParameterSet')]
        [string]
        $Path,

        [Parameter(Mandatory=$true,
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameAndLiteralPathParameterSet')]

        [Parameter(Mandatory=$true,
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='InputOjectAndLiteralPathParameterSet')]
        [string]
        $LiteralPath,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,
        
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
                
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        # Script names already tried in the current pipeline for InputObject parameterset
        $scriptNamesInPipeline = @()
    }

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementSaveScriptMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript

        # When -Force is specified, Path will be created if not available.
        if(-not $Force)
        {
            if($Path)
            {
                $destinationPath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | 
                                       Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

                if(-not $destinationPath -or -not (Microsoft.PowerShell.Management\Test-path $destinationPath))
                {
                    $errorMessage = ($LocalizedData.PathNotFound -f $Path)
                    ThrowError  -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $errorMessage `
                                -ErrorId "PathNotFound" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $Path `
                                -ErrorCategory InvalidArgument
                }

                $PSBoundParameters['Path'] = $destinationPath
            }
            else
            {
                $destinationPath = Resolve-PathHelper -Path $LiteralPath -IsLiteralPath -CallerPSCmdlet $PSCmdlet | 
                                       Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

                if(-not $destinationPath -or -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $destinationPath))
                {
                    $errorMessage = ($LocalizedData.PathNotFound -f $LiteralPath)
                    ThrowError  -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $errorMessage `
                                -ErrorId "PathNotFound" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $LiteralPath `
                                -ErrorCategory InvalidArgument
                }

                $PSBoundParameters['LiteralPath'] = $destinationPath
            }
        }

        if($Name)
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            if($PSBoundParameters.ContainsKey("Repository"))
            {
                $PSBoundParameters["Source"] = $Repository
                $null = $PSBoundParameters.Remove("Repository")

                $ev = $null
                $repositories = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
                if($ev) { return }

                $RepositoriesWithoutScriptSourceLocation = $false
                foreach($repo in $repositories)
                {
                    if(-not $repo.ScriptSourceLocation)
                    {
                        $message = $LocalizedData.ScriptSourceLocationIsMissing -f ($repo.Name)
                        Write-Error -Message $message `
                                    -ErrorId 'ScriptSourceLocationIsMissing' `
                                    -Category InvalidArgument `
                                    -TargetObject $repo.Name `
                                    -Exception 'System.ArgumentException'

                        $RepositoriesWithoutScriptSourceLocation = $true
                    }
                }

                if($RepositoriesWithoutScriptSourceLocation)
                {
                    return
                }
            }

            $null = PackageManagement\Save-Package @PSBoundParameters
        }
        elseif($InputObject)
        {
            $null = $PSBoundParameters.Remove("InputObject")

            foreach($inputValue in $InputObject)
            {
                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo"))
                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }
                
                $psRepositoryItemInfo = $inputValue

                # Skip the script name if it is already tried in the current pipeline
                if($scriptNamesInPipeline -contains $psRepositoryItemInfo.Name)
                {
                    continue
                }

                $scriptNamesInPipeline += $psRepositoryItemInfo.Name

                if ($psRepositoryItemInfo.PowerShellGetFormatVersion -and 
                    ($script:SupportedPSGetFormatVersionMajors -notcontains $psRepositoryItemInfo.PowerShellGetFormatVersion.Major))
                {
                    $message = $LocalizedData.NotSupportedPowerShellGetFormatVersionScripts -f ($psRepositoryItemInfo.Name, $psRepositoryItemInfo.PowerShellGetFormatVersion, $psRepositoryItemInfo.Name)
                    Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                    continue
                }

                $PSBoundParameters["Name"] = $psRepositoryItemInfo.Name
                $PSBoundParameters["RequiredVersion"] = $psRepositoryItemInfo.Version
                $PSBoundParameters['Source'] = $psRepositoryItemInfo.Repository
                $PSBoundParameters["PackageManagementProvider"] = (Get-ProviderName -PSCustomObject $psRepositoryItemInfo)

                $null = PackageManagement\Save-Package @PSBoundParameters
            }
        }
    }
}

function Install-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619784',
                   SupportsShouldProcess=$true)]
    Param
    (
        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name,

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputObject')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,
        
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Repository,

        [Parameter()]
        [ValidateSet("CurrentUser","AllUsers")]
        [string]
        $Scope = 'AllUsers',

        [Parameter()]
        [Switch]
        $NoPathUpdate,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,
        
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [switch]
        $Force
    )

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Repository
        
        if(-not (Test-RunningAsElevated) -and ($Scope -ne "CurrentUser"))
        {
            # Throw an error when Install-Script is used as a non-admin user and '-Scope CurrentUser' is not specified
            $AdminPreviligeErrorMessage = $LocalizedData.InstallScriptNeedsCurrentUserScopeParameterForNonAdminUser -f @($script:ProgramFilesScriptsPath, $script:MyDocumentsScriptsPath)
            $AdminPreviligeErrorId = 'InstallScriptNeedsCurrentUserScopeParameterForNonAdminUser'

            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $AdminPreviligeErrorMessage `
                        -ErrorId $AdminPreviligeErrorId `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument
        }

        # Check and add the scope path to PATH environment variable
        if($Scope -eq 'AllUsers')
        {
            $scopePath = $script:ProgramFilesScriptsPath
        }
        else
        {
            $scopePath = $script:MyDocumentsScriptsPath
        }

        ValidateAndSet-PATHVariableIfUserAccepts -Scope $Scope `
                                                 -ScopePath $scopePath `
                                                 -NoPathUpdate:$NoPathUpdate `
                                                 -Force:$Force
        
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential
        
        # Script names already tried in the current pipeline for InputObject parameterset
        $scriptNamesInPipeline = @()

        $YesToAll = $false
        $NoToAll = $false
        $SourceSGrantedTrust = @()
        $SourcesDeniedTrust = @()
    }

    Process
    {
        $RepositoryIsNotTrusted = $LocalizedData.RepositoryIsNotTrusted
        $QueryInstallUntrustedPackage = $LocalizedData.QueryInstallUntrustedScriptPackage
        $PackageTarget = $LocalizedData.InstallScriptwhatIfMessage
        	
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementInstallScriptMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript
        $PSBoundParameters['Scope'] = $Scope

        if($PSCmdlet.ParameterSetName -eq "NameParameterSet")
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            if($PSBoundParameters.ContainsKey("Repository"))
            {
                $PSBoundParameters["Source"] = $Repository
                $null = $PSBoundParameters.Remove("Repository")

                $ev = $null
                $repositories = Get-PSRepository -Name $Repository -ErrorVariable ev -verbose:$false
                if($ev) { return }

                $RepositoriesWithoutScriptSourceLocation = $false
                foreach($repo in $repositories)
                {
                    if(-not $repo.ScriptSourceLocation)
                    {
                        $message = $LocalizedData.ScriptSourceLocationIsMissing -f ($repo.Name)
                        Write-Error -Message $message `
                                    -ErrorId 'ScriptSourceLocationIsMissing' `
                                    -Category InvalidArgument `
                                    -TargetObject $repo.Name `
                                    -Exception 'System.ArgumentException'

                        $RepositoriesWithoutScriptSourceLocation = $true
                    }
                }

                if($RepositoriesWithoutScriptSourceLocation)
                {
                    return
                }
            }

            if(-not $Force)
            {
                foreach($scriptName in $Name)
                {
                    # Throw an error if there is a command with the same name and -force is not specified.
                    $cmd = Microsoft.PowerShell.Core\Get-Command -Name $scriptName `
                                                                 -ErrorAction SilentlyContinue `
                                                                 -WarningAction SilentlyContinue
                    if($cmd)
                    {
                        # Check if this script was already installed, may be with -Force
                        $InstalledScriptInfo = Test-ScriptInstalled -Name $scriptName `
                                                                    -ErrorAction SilentlyContinue `
                                                                    -WarningAction SilentlyContinue
                        if(-not $InstalledScriptInfo)
                        {
                            $message = $LocalizedData.CommandAlreadyAvailable -f ($scriptName)
                            Write-Error -Message $message -ErrorId CommandAlreadyAvailableWitScriptName -Category InvalidOperation

                            # return if only single name is specified
                            if($scriptName -eq $Name)
                            {
                                return
                            }
                        }
                    }
                }
            }

            $null = PackageManagement\Install-Package @PSBoundParameters
        }
        elseif($PSCmdlet.ParameterSetName -eq "InputObject")
        {
            $null = $PSBoundParameters.Remove("InputObject")

            foreach($inputValue in $InputObject)
            {

                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo"))
                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }

                $psRepositoryItemInfo = $inputValue

                # Skip the script name if it is already tried in the current pipeline
                if($scriptNamesInPipeline -contains $psRepositoryItemInfo.Name)
                {
                    continue
                }

                $scriptNamesInPipeline += $psRepositoryItemInfo.Name

                if ($psRepositoryItemInfo.PowerShellGetFormatVersion -and 
                    ($script:SupportedPSGetFormatVersionMajors -notcontains $psRepositoryItemInfo.PowerShellGetFormatVersion.Major))
                {
                    $message = $LocalizedData.NotSupportedPowerShellGetFormatVersionScripts -f ($psRepositoryItemInfo.Name, $psRepositoryItemInfo.PowerShellGetFormatVersion, $psRepositoryItemInfo.Name)
                    Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                    continue
                }

                $PSBoundParameters["Name"] = $psRepositoryItemInfo.Name
                $PSBoundParameters["RequiredVersion"] = $psRepositoryItemInfo.Version
                $PSBoundParameters['Source'] = $psRepositoryItemInfo.Repository
                $PSBoundParameters["PackageManagementProvider"] = (Get-ProviderName -PSCustomObject $psRepositoryItemInfo)
                
                $InstalledScriptInfo = Test-ScriptInstalled -Name $psRepositoryItemInfo.Name                 
                if(-not $Force -and $InstalledScriptInfo)
                {
                    $message = $LocalizedData.ScriptAlreadyInstalledVerbose -f ($InstalledScriptInfo.Version, $InstalledScriptInfo.Name, $InstalledScriptInfo.ScriptBase)
                    Write-Verbose -Message $message
                }
                else
                {
                    # Throw an error if there is a command with the same name and -force is not specified.
                    if(-not $Force)
                    {
                        $cmd = Microsoft.PowerShell.Core\Get-Command -Name $psRepositoryItemInfo.Name `
                                                                     -ErrorAction SilentlyContinue `
                                                                     -WarningAction SilentlyContinue
                        if($cmd)
                        {
                            $message = $LocalizedData.CommandAlreadyAvailable -f ($psRepositoryItemInfo.Name)
                            Write-Error -Message $message -ErrorId CommandAlreadyAvailableWitScriptName -Category InvalidOperation
                                                       
                            continue
                        }
                    }

                    $source =  $psRepositoryItemInfo.Repository
                    $installationPolicy = (Get-PSRepository -Name $source).InstallationPolicy                
                    $ShouldProcessMessage = $PackageTarget -f ($psRepositoryItemInfo.Name, $psRepositoryItemInfo.Version)
                
                    if($psCmdlet.ShouldProcess($ShouldProcessMessage))
                    {
                        if($installationPolicy.Equals("Untrusted", [StringComparison]::OrdinalIgnoreCase))
                        {
                            if(-not($YesToAll -or $NoToAll -or $SourceSGrantedTrust.Contains($source) -or $sourcesDeniedTrust.Contains($source) -or $Force))
                            {
                                $message = $QueryInstallUntrustedPackage -f ($psRepositoryItemInfo.Name, $psRepositoryItemInfo.RepositorySourceLocation)
                            
                                if($PSVersionTable.PSVersion -ge '5.0.0')
                                {
                                    $sourceTrusted = $psCmdlet.ShouldContinue("$message", "$RepositoryIsNotTrusted",$true, [ref]$YesToAll, [ref]$NoToAll)
                                }
                                else
                                {
                                    $sourceTrusted = $psCmdlet.ShouldContinue("$message", "$RepositoryIsNotTrusted", [ref]$YesToAll, [ref]$NoToAll)
                                }

                                if($sourceTrusted)
                                {
                                    $SourcesGrantedTrust+=$source
                                }
                                else
                                {
                                    $SourcesDeniedTrust+=$source
                                }
                            }
                         }
                     }
                     if($installationPolicy.Equals("trusted", [StringComparison]::OrdinalIgnoreCase) -or $SourcesGrantedTrust.Contains($source) -or $YesToAll -or $Force)
                     {
                        $PSBoundParameters["Force"] = $true                        
                        $null = PackageManagement\Install-Package @PSBoundParameters                        
                     }                                  
                }                   
            }
        }
    }
}

function Update-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619787')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true, 
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [Switch]
        $Force
    )

    Begin
    {
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        # Script names already tried in the current pipeline
        $scriptNamesInPipeline = @()
    }

    Process
    {
        $scriptFilePathsToUpdate = @()

        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        if($Name)
        {
            foreach($scriptName in $Name)
            {
                $availableScriptPaths = Get-AvailableScriptFilePath -Name $scriptName -Verbose:$false
        
                if(-not $availableScriptPaths -and -not (Test-WildcardPattern -Name $scriptName))
                {                    
                    $message = $LocalizedData.ScriptNotInstalledOnThisMachine -f ($scriptName, $script:MyDocumentsScriptsPath, $script:ProgramFilesScriptsPath)
                    Write-Error -Message $message -ErrorId "ScriptNotInstalledOnThisMachine" -Category InvalidOperation -TargetObject $scriptName
                    continue
                }

                foreach($scriptFilePath in $availableScriptPaths)
                {
                    $installedScriptFilePath = Get-InstalledScriptFilePath -Name ([System.IO.Path]::GetFileNameWithoutExtension($scriptFilePath)) | 
                                                   Microsoft.PowerShell.Core\Where-Object {$_ -eq $scriptFilePath }

                    # Check if this script got installed with PowerShellGet and user has required permissions
                    if ($installedScriptFilePath)
                    {
                        if(-not (Test-RunningAsElevated) -and $installedScriptFilePath.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
                        {                            
                            if(-not (Test-WildcardPattern -Name $scriptName))
                            {
                                $message = $LocalizedData.AdminPrivilegesRequiredForScriptUpdate -f ($scriptName, $installedScriptFilePath)
                                Write-Error -Message $message -ErrorId "AdminPrivilegesAreRequiredForUpdate" -Category InvalidOperation -TargetObject $scriptName
                            }
                            continue
                        }

                        $scriptFilePathsToUpdate += $installedScriptFilePath
                    }
                    else
                    {
                        if(-not (Test-WildcardPattern -Name $scriptName))
                        {
                            $message = $LocalizedData.ScriptNotInstalledUsingPowerShellGet -f ($scriptName)
                            Write-Error -Message $message -ErrorId "ScriptNotInstalledUsingPowerShellGet" -Category InvalidOperation -TargetObject $scriptName
                        }
                        continue
                    }
                }
            }
        }
        else
        {
            $isRunningAsElevated = Test-RunningAsElevated
            $installedScriptFilePaths = Get-InstalledScriptFilePath

            if($isRunningAsElevated)
            {
                $scriptFilePathsToUpdate = $installedScriptFilePaths
            }
            else
            {
                # Update the scripts installed under 
                $scriptFilePathsToUpdate = $installedScriptFilePaths | Microsoft.PowerShell.Core\Where-Object {
                                                $_.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase)}
            }
        }

        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementUpdateScriptMessageResolverScriptBlock
        $PSBoundParameters["InstallUpdate"] = $true

        foreach($scriptFilePath in $scriptFilePathsToUpdate)
        {
            $scriptName = [System.IO.Path]::GetFileNameWithoutExtension($scriptFilePath)

            $installedScriptInfoFilePath = $null
            $installedScriptInfoFileName = "$($scriptName)_$script:InstalledScriptInfoFileName"

            if($scriptFilePath.ToString().StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $PSBoundParameters["Scope"] = "CurrentUser"
                $installedScriptInfoFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsInstalledScriptInfosPath `
                                                                                         -ChildPath $installedScriptInfoFileName
            }
            elseif($scriptFilePath.ToString().StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $PSBoundParameters["Scope"] = "AllUsers"
                $installedScriptInfoFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesInstalledScriptInfosPath `
                                                                                         -ChildPath $installedScriptInfoFileName

            }

            $psgetItemInfo = $null
            if($installedScriptInfoFilePath -and (Microsoft.PowerShell.Management\Test-Path -Path $installedScriptInfoFilePath -PathType Leaf))
            {
                $psgetItemInfo = DeSerialize-PSObject -Path $installedScriptInfoFilePath
            }
            
            # Skip the script name if it is already tried in the current pipeline
            if(-not $psgetItemInfo -or ($scriptNamesInPipeline -contains $psgetItemInfo.Name))
            {
                continue
            }


            $scriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $psgetItemInfo.InstalledLocation `
                                                                        -ChildPath "$($psgetItemInfo.Name).ps1"

            # Remove the InstalledScriptInfo.xml file if the actual script file was manually uninstalled by the user
            if(-not (Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf))
            {
                Microsoft.PowerShell.Management\Remove-Item -Path $installedScriptInfoFilePath -Force -ErrorAction SilentlyContinue

                continue
            }

            $scriptNamesInPipeline += $psgetItemInfo.Name

            $message = $LocalizedData.CheckingForScriptUpdate -f ($psgetItemInfo.Name)
            Write-Verbose -Message $message

            $providerName = Get-ProviderName -PSCustomObject $psgetItemInfo
            if(-not $providerName)
            {
                $providerName = $script:NuGetProviderName
            }

            $PSBoundParameters["PackageManagementProvider"] = $providerName 
            $PSBoundParameters["Name"] = $psgetItemInfo.Name
            $PSBoundParameters['Source'] = $psgetItemInfo.Repository

            Get-PSGalleryApiAvailability -Repository (Get-SourceName -Location $psgetItemInfo.RepositorySourceLocation)

            $sid = PackageManagement\Install-Package @PSBoundParameters
        }
    }
}

function Uninstall-Script
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameParameterSet',
                   SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619789')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   Mandatory=$true, 
                   Position=0,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(Mandatory=$true, 
                   ValueFromPipeline=$true,
                   ValueFromPipelineByPropertyName=$true,
                   Position=0,
                   ParameterSetName='InputObject')]
        [ValidateNotNull()]
        [PSCustomObject[]]
        $InputObject,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion,

        [Parameter()]
        [Switch]
        $Force
    )

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementUnInstallScriptMessageResolverScriptBlock
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript

        if($PSCmdlet.ParameterSetName -eq "InputObject")
        {
            $null = $PSBoundParameters.Remove("InputObject")
        
            foreach($inputValue in $InputObject)
            {
                if (($inputValue.PSTypeNames -notcontains "Microsoft.PowerShell.Commands.PSRepositoryItemInfo") -and
                    ($inputValue.PSTypeNames -notcontains "Deserialized.Microsoft.PowerShell.Commands.PSRepositoryItemInfo"))
                {
                    ThrowError -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $LocalizedData.InvalidInputObjectValue `
                                -ErrorId "InvalidInputObjectValue" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ErrorCategory InvalidArgument `
                                -ExceptionObject $inputValue
                }

                $PSBoundParameters["Name"] = $inputValue.Name
                $PSBoundParameters["RequiredVersion"] = $inputValue.Version

                $null = PackageManagement\Uninstall-Package @PSBoundParameters
            }
        }
        else
        {
            $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                           -Name $Name `
                                                           -TestWildcardsInName `
                                                           -MinimumVersion $MinimumVersion `
                                                           -MaximumVersion $MaximumVersion `
                                                           -RequiredVersion $RequiredVersion

            if(-not $ValidationResult)
            {
                # Validate-VersionParameters throws the error. 
                # returning to avoid further execution when different values are specified for -ErrorAction parameter
                return
            }

            $null = PackageManagement\Uninstall-Package @PSBoundParameters
        }
    }
}

function Get-InstalledScript
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkId=619790')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true, 
                   Position=0)]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $Name, 

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MinimumVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $RequiredVersion,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Version]
        $MaximumVersion
    )

    Process
    {
        $ValidationResult = Validate-VersionParameters -CallerPSCmdlet $PSCmdlet `
                                                       -Name $Name `
                                                       -MinimumVersion $MinimumVersion `
                                                       -MaximumVersion $MaximumVersion `
                                                       -RequiredVersion $RequiredVersion

        if(-not $ValidationResult)
        {
            # Validate-VersionParameters throws the error. 
            # returning to avoid further execution when different values are specified for -ErrorAction parameter
            return
        }

        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlockForScriptCmdlets
        $PSBoundParameters[$script:PSArtifactType] = $script:PSArtifactTypeScript

        PackageManagement\Get-Package @PSBoundParameters | Microsoft.PowerShell.Core\ForEach-Object {New-PSGetItemInfo -SoftwareIdentity $_ -Type $script:PSArtifactTypeScript}
    }
}

#endregion *-Script cmdlets

#region *-PSRepository cmdlets

function Register-PSRepository
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(DefaultParameterSetName='NameParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkID=517129')]
    Param 
    (
        [Parameter(Mandatory=$true,
                   Position=0, 
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name,

        [Parameter(Mandatory=$true,
                   Position=1, 
                   ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $SourceLocation,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $PublishLocation,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ScriptSourceLocation,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ScriptPublishLocation,

        [Parameter(ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='NameParameterSet')]
        [PSCredential]
        $Credential,

        [Parameter(Mandatory=$true,
                   ParameterSetName='PSGalleryParameterSet')]
        [Switch]
        $Default,

        [Parameter()]
        [ValidateSet('Trusted','Untrusted')]
        [string]
        $InstallationPolicy = 'Untrusted',

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter(ParameterSetName='NameParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $PackageManagementProvider        
    )

    DynamicParam
    {
        if (Get-Variable -Name SourceLocation -ErrorAction SilentlyContinue)
        {
            Set-Variable -Name selctedProviderName -value $null -Scope 1

            if(Get-Variable -Name PackageManagementProvider -ErrorAction SilentlyContinue)
            {
                $selctedProviderName = $PackageManagementProvider
                $null = Get-DynamicParameters -Location $SourceLocation -PackageManagementProvider ([REF]$selctedProviderName)
            }
            else
            {
                $dynamicParameters = Get-DynamicParameters -Location $SourceLocation -PackageManagementProvider ([REF]$selctedProviderName)
                Set-Variable -Name PackageManagementProvider -Value $selctedProviderName -Scope 1
                $null = $dynamicParameters
            }
        }
    }

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Name
        
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        if($PackageManagementProvider)
        {
            $providers = PackageManagement\Get-PackageProvider | Where-Object { $_.Name -ne $script:PSModuleProviderName -and $_.Features.ContainsKey($script:SupportsPSModulesFeatureName) }

            if (-not $providers -or $providers.Name -notcontains $PackageManagementProvider)
            {
                $possibleProviderNames = $script:NuGetProviderName

                if($providers)
                { 
                    $possibleProviderNames = ($providers.Name -join ',')
                }

                $message = $LocalizedData.InvalidPackageManagementProviderValue -f ($PackageManagementProvider, $possibleProviderNames, $script:NuGetProviderName)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidPackageManagementProviderValue" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $PackageManagementProvider
                return
            }
        }
    }

    Process
    {
        if($PSCmdlet.ParameterSetName -eq 'PSGalleryParameterSet')
        {
            if(-not $Default)
            {
                return
            }

            $PSBoundParameters['Name'] = $Script:PSGalleryModuleSource
            $null = $PSBoundParameters.Remove('Default')
        }
        else
        {
            if($Name -eq $Script:PSGalleryModuleSource)
            {
                $message = $LocalizedData.UseDefaultParameterSetOnRegisterPSRepository
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId 'UseDefaultParameterSetOnRegisterPSRepository' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $Name
                return   
            }

            # Ping and resolve the specified location
            $SourceLocation = Resolve-Location -Location (Get-LocationString -LocationUri $SourceLocation) `
                                               -LocationParameterName 'SourceLocation' `
                                               -Credential $Credential `
                                               -Proxy $Proxy `
                                               -ProxyCredential $ProxyCredential `
                                               -CallerPSCmdlet $PSCmdlet
            if(-not $SourceLocation)
            {
                # Above Resolve-Location function throws an error when it is not able to resolve a location
                return
            }

            $providerName = $null

            if($PackageManagementProvider)
            {            
                $providerName = $PackageManagementProvider
            }
            elseif($selctedProviderName)
            {
                $providerName = $selctedProviderName
            }
            else
            {
                $providerName = Get-PackageManagementProviderName -Location $SourceLocation
            }

            if($providerName)
            {
                $PSBoundParameters[$script:PackageManagementProviderParam] = $providerName
            }

            if($PublishLocation)
            {
                $PSBoundParameters[$script:PublishLocation] = Get-LocationString -LocationUri $PublishLocation
            }

            if($ScriptPublishLocation)
            {
                $PSBoundParameters[$script:ScriptPublishLocation] = Get-LocationString -LocationUri $ScriptPublishLocation
            }

            if($ScriptSourceLocation)
            {
                $PSBoundParameters[$script:ScriptSourceLocation] = Get-LocationString -LocationUri $ScriptSourceLocation
            }

            $PSBoundParameters["Location"] = Get-LocationString -LocationUri $SourceLocation
            $null = $PSBoundParameters.Remove("SourceLocation")
        }

        if($InstallationPolicy -eq "Trusted")
        {
            $PSBoundParameters['Trusted'] = $true
        }
        $null = $PSBoundParameters.Remove("InstallationPolicy")

        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock

        $null = PackageManagement\Register-PackageSource @PSBoundParameters
    }
}

function Set-PSRepository
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(PositionalBinding=$false,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkID=517128')]
    Param
    (
        [Parameter(Mandatory=$true, Position=0)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name,
        
        [Parameter(Position=1)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $SourceLocation,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $PublishLocation,        

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ScriptSourceLocation,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ScriptPublishLocation,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $Credential,

        [Parameter()]
        [ValidateSet('Trusted','Untrusted')]
        [string]
        $InstallationPolicy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Proxy,

        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [PSCredential]
        $ProxyCredential,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $PackageManagementProvider
    )

    DynamicParam
    {
        if (Get-Variable -Name Name -ErrorAction SilentlyContinue)
        {
            $moduleSource = Get-PSRepository -Name $Name -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

            if($moduleSource)
            {
                $providerName = (Get-ProviderName -PSCustomObject $moduleSource)
            
                $loc = $moduleSource.SourceLocation
            
                if(Get-Variable -Name SourceLocation -ErrorAction SilentlyContinue)
                {
                    $loc = $SourceLocation
                }

                if(Get-Variable -Name PackageManagementProvider -ErrorAction SilentlyContinue)
                {
                    $providerName = $PackageManagementProvider
                }

                $null = Get-DynamicParameters -Location $loc -PackageManagementProvider ([REF]$providerName)
            }
        }
    }

    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Name
        
        Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -Proxy $Proxy -ProxyCredential $ProxyCredential

        if($PackageManagementProvider)
        {
            $providers = PackageManagement\Get-PackageProvider | Where-Object { $_.Name -ne $script:PSModuleProviderName -and $_.Features.ContainsKey($script:SupportsPSModulesFeatureName) }

            if (-not $providers -or $providers.Name -notcontains $PackageManagementProvider)
            {
                $possibleProviderNames = $script:NuGetProviderName

                if($providers)
                { 
                    $possibleProviderNames = ($providers.Name -join ',')
                }

                $message = $LocalizedData.InvalidPackageManagementProviderValue -f ($PackageManagementProvider, $possibleProviderNames, $script:NuGetProviderName)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidPackageManagementProviderValue" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $PackageManagementProvider
                return
            }
        }
    }

    Process
    {
        # Ping and resolve the specified location
        if($SourceLocation)
        {
            # Ping and resolve the specified location
            $SourceLocation = Resolve-Location -Location (Get-LocationString -LocationUri $SourceLocation) `
                                               -LocationParameterName 'SourceLocation' `
                                               -Credential $Credential `
                                               -Proxy $Proxy `
                                               -ProxyCredential $ProxyCredential `
                                               -CallerPSCmdlet $PSCmdlet
            if(-not $SourceLocation)
            {
                # Above Resolve-Location function throws an error when it is not able to resolve a location
                return
            }
        }

        $ModuleSource = Get-PSRepository -Name $Name -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

        if(-not $ModuleSource)
        {
            $message = $LocalizedData.RepositoryNotFound -f ($Name)

            ThrowError -ExceptionName "System.InvalidOperationException" `
                       -ExceptionMessage $message `
                       -ErrorId "RepositoryNotFound" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidOperation `
                       -ExceptionObject $Name
        }

        if (-not $PackageManagementProvider)
        {
            $PackageManagementProvider = (Get-ProviderName -PSCustomObject $ModuleSource)
        }

        $Trusted = $ModuleSource.Trusted
        if($InstallationPolicy)
        {
            if($InstallationPolicy -eq "Trusted")
            {
                $Trusted = $true
            }
            else
            {
                $Trusted = $false
            }

            $null = $PSBoundParameters.Remove("InstallationPolicy")
        }

        if($PublishLocation)
        {
            $PSBoundParameters[$script:PublishLocation] = Get-LocationString -LocationUri $PublishLocation
        }

        if($ScriptPublishLocation)
        {
            $PSBoundParameters[$script:ScriptPublishLocation] = Get-LocationString -LocationUri $ScriptPublishLocation
        }

        if($ScriptSourceLocation)
        {
            $PSBoundParameters[$script:ScriptSourceLocation] = Get-LocationString -LocationUri $ScriptSourceLocation
        }

        if($SourceLocation)
        {
            $PSBoundParameters["NewLocation"] = Get-LocationString -LocationUri $SourceLocation

            $null = $PSBoundParameters.Remove("SourceLocation")
        }

        $PSBoundParameters[$script:PackageManagementProviderParam] = $PackageManagementProvider
        $PSBoundParameters.Add("Trusted", $Trusted)        
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock

        $null = PackageManagement\Set-PackageSource @PSBoundParameters
    }
}

function Unregister-PSRepository
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkID=517130')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true,
                   Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name
    )
    
    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Name
    }

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock

        $null = $PSBoundParameters.Remove("Name")

        foreach ($moduleSourceName in $Name)
        {
            # Check if $moduleSourceName contains any wildcards
            if(Test-WildcardPattern $moduleSourceName)
            {
                $message = $LocalizedData.RepositoryNameContainsWildCards -f ($moduleSourceName)
                Write-Error -Message $message -ErrorId "RepositoryNameContainsWildCards" -Category InvalidOperation
                continue
            }

            $PSBoundParameters["Source"] = $moduleSourceName

            $null = PackageManagement\Unregister-PackageSource @PSBoundParameters
        }
    }
}

function Get-PSRepository
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(HelpUri='https://go.microsoft.com/fwlink/?LinkID=517127')]
    Param
    (
        [Parameter(ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Name
    )
    
    Begin
    {
        Get-PSGalleryApiAvailability -Repository $Name
    }

    Process
    {
        $PSBoundParameters["Provider"] = $script:PSModuleProviderName
        $PSBoundParameters["MessageResolver"] = $script:PackageManagementMessageResolverScriptBlock

        if($Name)
        {
            foreach($sourceName in $Name)
            {
                $PSBoundParameters["Name"] = $sourceName
                
                $packageSources = PackageManagement\Get-PackageSource @PSBoundParameters

                $packageSources | Microsoft.PowerShell.Core\ForEach-Object { New-ModuleSourceFromPackageSource -PackageSource $_ }
            }
        }
        else
        {
            $packageSources = PackageManagement\Get-PackageSource @PSBoundParameters

            $packageSources | Microsoft.PowerShell.Core\ForEach-Object { New-ModuleSourceFromPackageSource -PackageSource $_ }
        }
    }
}

#endregion *-PSRepository cmdlets

#region *-ScriptFileInfo cmdlets

# Below is the sample PSScriptInfo in a script file.
<#PSScriptInfo

.VERSION 1.0

.GUID 544238e3-1751-4065-9227-be105ff11636

.AUTHOR manikb

.COMPANYNAME Microsoft Corporation

.COPYRIGHT (c) 2015 Microsoft Corporation. All rights reserved.

.TAGS Tag1 Tag2 Tag3

.LICENSEURI https://contoso.com/License

.PROJECTURI https://contoso.com/

.ICONURI https://contoso.com/Icon

.EXTERNALMODULEDEPENDENCIES ExternalModule1

.REQUIREDSCRIPTS Start-WFContosoServer,Stop-ContosoServerScript

.EXTERNALSCRIPTDEPENDENCIES Stop-ContosoServerScript

.RELEASENOTES
contoso script now supports following features
Feature 1
Feature 2
Feature 3
Feature 4
Feature 5

#>

<# #Requires -Module statements #>

<# 

.DESCRIPTION 
 Description goes here. 

#> 


#
function Test-ScriptFileInfo
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(PositionalBinding=$false,
                   DefaultParameterSetName='PathParameterSet',
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619791')]
    Param
    (
        [Parameter(Mandatory=$true,
                   Position=0,
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='PathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(Mandatory=$true, 
                   ValueFromPipelineByPropertyName=$true,
                   ParameterSetName='LiteralPathParameterSet')]
        [ValidateNotNullOrEmpty()]
        [string]
        $LiteralPath
    )

    Process
    {
        $scriptFilePath = $null
        if($Path)
        {
            $scriptFilePath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore
            
            if(-not $scriptFilePath -or -not (Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $Path)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $Path `
                            -ErrorCategory InvalidArgument
                return
            }
        }
        else
        {
            $scriptFilePath = Resolve-PathHelper -Path $LiteralPath -IsLiteralPath -CallerPSCmdlet $PSCmdlet | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if(-not $scriptFilePath -or -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $LiteralPath)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $LiteralPath `
                            -ErrorCategory InvalidArgument
                return
            }
        }

        if(-not $scriptFilePath.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase))
        {
            $errorMessage = ($LocalizedData.InvalidScriptFilePath -f $scriptFilePath)
            ThrowError  -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "InvalidScriptFilePath" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $scriptFilePath `
                        -ErrorCategory InvalidArgument
            return
        }

        $PSScriptInfo = New-PSScriptInfoObject -Path $scriptFilePath

        [System.Management.Automation.Language.Token[]]$tokens = $null;
        [System.Management.Automation.Language.ParseError[]]$errors = $null;
        $ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptFilePath, ([ref]$tokens), ([ref]$errors))
        

        $notSupportedOnNanoErrorIds = @('WorkflowNotSupportedInPowerShellCore',
                                        'ConfigurationNotSupportedInPowerShellCore')
        $errosAfterSkippingOneCoreErrors = $errors | Microsoft.PowerShell.Core\Where-Object { $notSupportedOnNanoErrorIds -notcontains $_.ErrorId}

        if($errosAfterSkippingOneCoreErrors)
        {
            $errorMessage = ($LocalizedData.ScriptParseError -f $scriptFilePath)
            ThrowError  -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "ScriptParseError" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $errosAfterSkippingOneCoreErrors `
                        -ErrorCategory InvalidArgument
            return
        }

        if($ast)
        {
            # Get the block/group comment beginning with <#PSScriptInfo
            $CommentTokens = $tokens | Microsoft.PowerShell.Core\Where-Object {$_.Kind -eq 'Comment'}

            $psscriptInfoComments = $CommentTokens | 
                                        Microsoft.PowerShell.Core\Where-Object { $_.Extent.Text -match "<#PSScriptInfo" } | 
                                            Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if(-not $psscriptInfoComments)
            {
                $errorMessage = ($LocalizedData.MissingPSScriptInfo -f $scriptFilePath)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "MissingPSScriptInfo" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $scriptFilePath `
                            -ErrorCategory InvalidArgument
                return
            }

            # $psscriptInfoComments.Text will have the multiline PSScriptInfo comment, 
            # split them into multiple lines to parse for the PSScriptInfo metadata properties.
            $commentLines = $psscriptInfoComments.Text -split "`r`n"

            $KeyName = $null
            $Value = ""

            # PSScriptInfo comment will be in following format:
                <#PSScriptInfo

                .VERSION 1.0

                .GUID 544238e3-1751-4065-9227-be105ff11636

                .AUTHOR manikb

                .COMPANYNAME Microsoft Corporation

                .COPYRIGHT (c) 2015 Microsoft Corporation. All rights reserved.

                .TAGS Tag1 Tag2 Tag3

                .LICENSEURI https://contoso.com/License

                .PROJECTURI https://contoso.com/

                .ICONURI https://contoso.com/Icon

                .EXTERNALMODULEDEPENDENCIES ExternalModule1

                .REQUIREDSCRIPTS Start-WFContosoServer,Stop-ContosoServerScript

                .EXTERNALSCRIPTDEPENDENCIES Stop-ContosoServerScript

                .RELEASENOTES
                contoso script now supports following features
                Feature 1
                Feature 2
                Feature 3
                Feature 4
                Feature 5

                #>
            # If comment line count is not more than two, it doesn't have the any metadata property
            # First line is <#PSScriptInfo
            # Last line #>
            #
            if($commentLines.Count -gt 2)
            {
                for($i = 1; $i -lt ($commentLines.count - 1); $i++)
                {
                    $line = $commentLines[$i]

                    if(-not $line)
                    {
                        continue
                    }

                    # A line is starting with . conveys a new metadata property
                    # __NEWLINE__ is used for replacing the value lines while adding the value to $PSScriptInfo object
                    #
                    if($line.trim().StartsWith('.'))
                    {
                        $parts = $line.trim() -split '[.\s+]',3 | Microsoft.PowerShell.Core\Where-Object {$_}

                        if($KeyName -and $Value)
                        {
                            if($keyName -eq $script:ReleaseNotes)
                            {
                                $Value = $Value.Trim() -split '__NEWLINE__' 
                            }
                            elseif($keyName -eq $script:DESCRIPTION)
                            {
                                $Value = $Value -split '__NEWLINE__'
                                $Value = ($Value -join "`r`n").Trim()
                            }
                            else
                            {
                                $Value = $Value -split '__NEWLINE__'  | Microsoft.PowerShell.Core\Where-Object { $_ }

                                if($Value -and $Value.GetType().ToString() -eq "System.String")
                                {
                                    $Value = $Value.Trim()
                                }
                            }

                            ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                             -PropertyName $KeyName `
                                                             -PropertyValue $Value `
                                                             -CallerPSCmdlet $PSCmdlet
                        }

                        $KeyName = $null
                        $Value = ""

                        if($parts.GetType().ToString() -eq "System.String")
                        {
                            $KeyName = $parts
                        } 
                        else
                        {
                            $KeyName = $parts[0]; 
                            $Value = $parts[1]
                        }
                    }                    
                    else
                    {
                        if($Value)
                        {
                            # __NEWLINE__ is used for replacing the value lines while adding the value to $PSScriptInfo object
                            $Value += '__NEWLINE__'
                        }

                        $Value += $line
                    }
                }

                if($KeyName -and $Value)
                {
                    if($keyName -eq $script:ReleaseNotes)
                    {
                        $Value = $Value.Trim() -split '__NEWLINE__' 
                    }
                    elseif($keyName -eq $script:DESCRIPTION)
                    {
                        $Value = $Value -split '__NEWLINE__'
                        $Value = ($Value -join "`r`n").Trim()
                    }
                    else
                    {
                        $Value = $Value -split '__NEWLINE__'  | Microsoft.PowerShell.Core\Where-Object { $_ }

                        if($Value -and $Value.GetType().ToString() -eq "System.String")
                        {
                            $Value = $Value.Trim()
                        }
                    }

                    ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                     -PropertyName $KeyName `
                                                     -PropertyValue $Value `
                                                     -CallerPSCmdlet $PSCmdlet

                    $KeyName = $null
                    $Value = ""
                }
            }

            $helpContent = $ast.GetHelpContent()
            if($helpContent -and $helpContent.Description)
            {
                ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                 -PropertyName $script:DESCRIPTION `
                                                 -PropertyValue $helpContent.Description.Trim() `
                                                 -CallerPSCmdlet $PSCmdlet

            }

            # Handle RequiredModules
            if((Microsoft.PowerShell.Utility\Get-Member -InputObject $ast -Name 'ScriptRequirements') -and 
               $ast.ScriptRequirements -and
               (Microsoft.PowerShell.Utility\Get-Member -InputObject $ast.ScriptRequirements -Name 'RequiredModules') -and
               $ast.ScriptRequirements.RequiredModules)
            {
                ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                 -PropertyName $script:RequiredModules `
                                                 -PropertyValue $ast.ScriptRequirements.RequiredModules `
                                                 -CallerPSCmdlet $PSCmdlet
            }

            # Get all defined functions and populate DefinedCommands, DefinedFunctions and DefinedWorkflows
            $allCommands = $ast.FindAll({param($i) return ($i.GetType().Name -eq 'FunctionDefinitionAst')}, $true)

            if($allCommands)
            {
                $allCommandNames = $allCommands | ForEach-Object {$_.Name} | Select-Object -Unique -ErrorAction Ignore
                ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                 -PropertyName $script:DefinedCommands `
                                                 -PropertyValue $allCommandNames `
                                                 -CallerPSCmdlet $PSCmdlet            

                $allFunctionNames = $allCommands | Where-Object {-not $_.IsWorkflow}  | ForEach-Object {$_.Name} | Select-Object -Unique -ErrorAction Ignore
                ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                 -PropertyName $script:DefinedFunctions `
                                                 -PropertyValue $allFunctionNames `
                                                 -CallerPSCmdlet $PSCmdlet


                $allWorkflowNames = $allCommands | Where-Object {$_.IsWorkflow} | ForEach-Object {$_.Name} | Select-Object -Unique -ErrorAction Ignore
                ValidateAndAdd-PSScriptInfoEntry -PSScriptInfo $PSScriptInfo `
                                                 -PropertyName $script:DefinedWorkflows `
                                                 -PropertyValue $allWorkflowNames `
                                                 -CallerPSCmdlet $PSCmdlet
            }
        }

        # Ensure that the script file has the required metadata properties. 
        if(-not $PSScriptInfo.Version -or -not $PSScriptInfo.Guid -or -not $PSScriptInfo.Author -or -not $PSScriptInfo.Description)
        {
            $errorMessage = ($LocalizedData.MissingRequiredPSScriptInfoProperties -f $scriptFilePath)
            ThrowError  -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "MissingRequiredPSScriptInfoProperties" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $Path `
                        -ErrorCategory InvalidArgument
            return
        }

        $PSScriptInfo = Get-OrderedPSScriptInfoObject -PSScriptInfo $PSScriptInfo

        return $PSScriptInfo
    }
}

function New-ScriptFileInfo
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(PositionalBinding=$false,
                   SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619792')]
    Param
    (
        [Parameter(Mandatory=$false,
                   Position=0,
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $Version,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Author,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Description,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Guid]
        $Guid,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $CompanyName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Copyright,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Object[]]
        $RequiredModules,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $ExternalModuleDependencies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $RequiredScripts,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $ExternalScriptDependencies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Tags,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ProjectUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $LicenseUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $IconUri,

        [Parameter()]
        [string[]]
        $ReleaseNotes,
                
        [Parameter()]
        [switch]
        $PassThru,

        [Parameter()]
        [switch]
        $Force
    )

    Process
    {
        if($Path)
        {
            if(-not $Path.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $errorMessage = ($LocalizedData.InvalidScriptFilePath -f $Path)
                ThrowError  -ExceptionName 'System.ArgumentException' `
                            -ExceptionMessage $errorMessage `
                            -ErrorId 'InvalidScriptFilePath' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $Path `
                            -ErrorCategory InvalidArgument
                return
            }

            if(-not $Force -and (Microsoft.PowerShell.Management\Test-Path -Path $Path))
            {
                $errorMessage = ($LocalizedData.ScriptFileExist -f $Path)
                ThrowError  -ExceptionName 'System.ArgumentException' `
                            -ExceptionMessage $errorMessage `
                            -ErrorId 'ScriptFileExist' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $Path `
                            -ErrorCategory InvalidArgument
                return
            }
        }
        elseif(-not $PassThru)
        {
            ThrowError  -ExceptionName 'System.ArgumentException' `
                        -ExceptionMessage $LocalizedData.MissingTheRequiredPathOrPassThruParameter `
                        -ErrorId 'MissingTheRequiredPathOrPassThruParameter' `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument
            return
        }

        if(-not $Version)
        {
            $Version = [Version]'1.0'
        }

        if(-not $Author)
        {
            if(IsWindows)
            {
                $Author = (Get-EnvironmentVariable -Name 'USERNAME' -Target $script:EnvironmentVariableTarget.Process -ErrorAction SilentlyContinue)
            }
            else
            {
                $Author = $env:USER
            }
        }

        if(-not $Guid)
        {
            $Guid = [System.Guid]::NewGuid()
        }

        $params = @{
            Version = $Version
            Author = $Author
            Guid = $Guid
            CompanyName = $CompanyName
            Copyright = $Copyright
            ExternalModuleDependencies = $ExternalModuleDependencies
            RequiredScripts = $RequiredScripts
            ExternalScriptDependencies = $ExternalScriptDependencies
            Tags = $Tags
            ProjectUri = $ProjectUri
            LicenseUri = $LicenseUri
            IconUri = $IconUri
            ReleaseNotes = $ReleaseNotes
        }

        if(-not (Validate-ScriptFileInfoParameters -parameters $params))
        {
            return
        }

        if("$Description" -match '<#' -or "$Description" -match '#>') 
        {
            $message = $LocalizedData.InvalidParameterValue -f ($Description, 'Description')
            Write-Error -Message $message -ErrorId 'InvalidParameterValue' -Category InvalidArgument

            return
        }

        $PSScriptInfoString = Get-PSScriptInfoString @params
                                                     
        $requiresStrings = Get-RequiresString -RequiredModules $RequiredModules

        $ScriptCommentHelpInfoString = Get-ScriptCommentHelpInfoString -Description $Description

        $ScriptMetadataString = $PSScriptInfoString
        $ScriptMetadataString += "`r`n"

        if("$requiresStrings".Trim())
        {
            $ScriptMetadataString += "`r`n"
            $ScriptMetadataString += $requiresStrings -join "`r`n"
            $ScriptMetadataString += "`r`n"
        }

        $ScriptMetadataString += "`r`n"
        $ScriptMetadataString += $ScriptCommentHelpInfoString        
        $ScriptMetadataString += "Param()`r`n`r`n"

        $tempScriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:TempPath -ChildPath "$(Get-Random).ps1"
        
        try
        {
            Microsoft.PowerShell.Management\Set-Content -Value $ScriptMetadataString -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false

            $scriptInfo = Test-ScriptFileInfo -Path $tempScriptFilePath

            if(-not $scriptInfo)
            {
                # Above Test-ScriptFileInfo cmdlet writes the errors
                return
            }

    	    if($Path -and ($Force -or $PSCmdlet.ShouldProcess($Path, ($LocalizedData.NewScriptFileInfowhatIfMessage -f $Path) )))
    	    {
                Microsoft.PowerShell.Management\Copy-Item -Path $tempScriptFilePath -Destination $Path -Force -WhatIf:$false -Confirm:$false
            }

            if($PassThru)
            {
                Write-Output -InputObject $ScriptMetadataString
            }
        }
        finally
        {
            Microsoft.PowerShell.Management\Remove-Item -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }
    }
}

function Update-ScriptFileInfo
{
    <#
    .ExternalHelp PSGet.psm1-help.xml
    #>
    [CmdletBinding(PositionalBinding=$false,
                   DefaultParameterSetName='PathParameterSet',
                   SupportsShouldProcess=$true,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619793')]
    Param
    (
        [Parameter(Mandatory=$true,
                   Position=0,
                   ParameterSetName='PathParameterSet',
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(Mandatory=$true,
                   Position=0,
                   ParameterSetName='LiteralPathParameterSet',
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $LiteralPath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $Version,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Author,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Guid]
        $Guid,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Description,

        [Parameter()] 
        [ValidateNotNullOrEmpty()]
        [String]
        $CompanyName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Copyright,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Object[]]
        $RequiredModules,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $ExternalModuleDependencies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $RequiredScripts,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $ExternalScriptDependencies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Tags,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ProjectUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $LicenseUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $IconUri,

        [Parameter()]
        [string[]]
        $ReleaseNotes,
                
        [Parameter()]
        [switch]
        $PassThru,

        [Parameter()]
        [switch]
        $Force
    )

    Process
    {
        $scriptFilePath = $null
        if($Path)
        {
            $scriptFilePath = Resolve-PathHelper -Path $Path -CallerPSCmdlet $PSCmdlet | 
                                  Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore
            
            if(-not $scriptFilePath -or 
               -not (Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $Path)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $Path `
                            -ErrorCategory InvalidArgument
            }
        }
        else
        {
            $scriptFilePath = Resolve-PathHelper -Path $LiteralPath -IsLiteralPath -CallerPSCmdlet $PSCmdlet | 
                                  Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if(-not $scriptFilePath -or 
               -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $scriptFilePath -PathType Leaf))
            {
                $errorMessage = ($LocalizedData.PathNotFound -f $LiteralPath)
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $errorMessage `
                            -ErrorId "PathNotFound" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ExceptionObject $LiteralPath `
                            -ErrorCategory InvalidArgument
            }
        }

        if(-not $scriptFilePath.EndsWith('.ps1', [System.StringComparison]::OrdinalIgnoreCase))
        {
            $errorMessage = ($LocalizedData.InvalidScriptFilePath -f $scriptFilePath)
            ThrowError  -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "InvalidScriptFilePath" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ExceptionObject $scriptFilePath `
                        -ErrorCategory InvalidArgument
            return
        }
        
        $psscriptInfo = $null
        try
        {
            $psscriptInfo = Test-ScriptFileInfo -LiteralPath $scriptFilePath
        }
        catch
        {
            if(-not $Force)
            {
                throw $_
                return
            }
        }

        if(-not $psscriptInfo)
        {
            if(-not $Description)
            {                
                ThrowError  -ExceptionName "System.ArgumentException" `
                            -ExceptionMessage $LocalizedData.DescriptionParameterIsMissingForAddingTheScriptFileInfo `
                            -ErrorId 'DescriptionParameterIsMissingForAddingTheScriptFileInfo' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidArgument
                return
            }

            if(-not $Version)
            {
                $Version = [Version]'1.0'
            }

            if(-not $Author)
            {
                if(IsWindows)
                {
                    $Author = (Get-EnvironmentVariable -Name 'USERNAME' -Target $script:EnvironmentVariableTarget.Process -ErrorAction SilentlyContinue)
                }
                else
                {
                    $Author = $env:USER
                }
            }

            if(-not $Guid)
            {
                $Guid = [System.Guid]::NewGuid()
            }
        }
        else
        {     
            # Use existing values if any of the parameters are not specified during Update-ScriptFileInfo
            if(-not $Version -and $psscriptInfo.Version)
            {
                $Version = $psscriptInfo.Version
            }

            if(-not $Guid -and $psscriptInfo.Guid)
            {
                $Guid = $psscriptInfo.Guid
            }

            if(-not $Author -and $psscriptInfo.Author)
            {
                $Author = $psscriptInfo.Author
            }

            if(-not $CompanyName -and $psscriptInfo.CompanyName)
            {
                $CompanyName = $psscriptInfo.CompanyName
            }

            if(-not $Copyright -and $psscriptInfo.Copyright)
            {
                $Copyright = $psscriptInfo.Copyright
            }

            if(-not $RequiredModules -and $psscriptInfo.RequiredModules)
            {
                $RequiredModules = $psscriptInfo.RequiredModules
            }

            if(-not $ExternalModuleDependencies -and $psscriptInfo.ExternalModuleDependencies)
            {
                $ExternalModuleDependencies = $psscriptInfo.ExternalModuleDependencies
            }

            if(-not $RequiredScripts -and $psscriptInfo.RequiredScripts)
            {
                $RequiredScripts = $psscriptInfo.RequiredScripts
            }

            if(-not $ExternalScriptDependencies -and $psscriptInfo.ExternalScriptDependencies)
            {
                $ExternalScriptDependencies = $psscriptInfo.ExternalScriptDependencies
            }

            if(-not $Tags -and $psscriptInfo.Tags)
            {
                $Tags = $psscriptInfo.Tags
            }

            if(-not $ProjectUri -and $psscriptInfo.ProjectUri)
            {
                $ProjectUri = $psscriptInfo.ProjectUri
            }

            if(-not $LicenseUri -and $psscriptInfo.LicenseUri)
            {
                $LicenseUri = $psscriptInfo.LicenseUri
            }

            if(-not $IconUri -and $psscriptInfo.IconUri)
            {
                $IconUri = $psscriptInfo.IconUri
            }

            if(-not $ReleaseNotes -and $psscriptInfo.ReleaseNotes)
            {
                $ReleaseNotes = $psscriptInfo.ReleaseNotes
            }
        }

        $params = @{
            Version = $Version
            Author = $Author
            Guid = $Guid
            CompanyName = $CompanyName
            Copyright = $Copyright
            ExternalModuleDependencies = $ExternalModuleDependencies
            RequiredScripts = $RequiredScripts
            ExternalScriptDependencies = $ExternalScriptDependencies
            Tags = $Tags
            ProjectUri = $ProjectUri
            LicenseUri = $LicenseUri
            IconUri = $IconUri
            ReleaseNotes = $ReleaseNotes
        }

        if(-not (Validate-ScriptFileInfoParameters -parameters $params))
        {
            return
        }

        if("$Description" -match '<#' -or "$Description" -match '#>') 
        {
            $message = $LocalizedData.InvalidParameterValue -f ($Description, 'Description')
            Write-Error -Message $message -ErrorId 'InvalidParameterValue' -Category InvalidArgument

            return
        }

        $PSScriptInfoString = Get-PSScriptInfoString @params
        
        $requiresStrings = ""                                             
        $requiresStrings = Get-RequiresString -RequiredModules $RequiredModules
        
        $DescriptionValue = if($Description) {$Description} else {$psscriptInfo.Description}
        $ScriptCommentHelpInfoString = Get-ScriptCommentHelpInfoString -Description $DescriptionValue

        $ScriptMetadataString = $PSScriptInfoString
        $ScriptMetadataString += "`r`n"

        if("$requiresStrings".Trim())
        {
            $ScriptMetadataString += "`r`n"
            $ScriptMetadataString += $requiresStrings -join "`r`n"
            $ScriptMetadataString += "`r`n"
        }

        $ScriptMetadataString += "`r`n"
        $ScriptMetadataString += $ScriptCommentHelpInfoString
        $ScriptMetadataString += "`r`nParam()`r`n`r`n"
        if(-not $ScriptMetadataString)
        {
            return
        }
        
        $tempScriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:TempPath -ChildPath "$(Get-Random).ps1"
        
        try
        {
            # First create a new script file with new script metadata to ensure that updated values are valid.
            Microsoft.PowerShell.Management\Set-Content -Value $ScriptMetadataString -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false

            $scriptInfo = Test-ScriptFileInfo -Path $tempScriptFilePath

            if(-not $scriptInfo)
            {
                # Above Test-ScriptFileInfo cmdlet writes the error
                return
            }

            $scriptFileContents = Microsoft.PowerShell.Management\Get-Content -LiteralPath $scriptFilePath

            # If -Force is specified and script file doesnt have a valid PSScriptInfo 
            # Prepend the PSScriptInfo and Check if the Test-ScriptFileInfo returns a valid script info without any errors
            if($Force -and -not $psscriptInfo)
            {
                # Add the script file contents to the temp file with script metadata
                Microsoft.PowerShell.Management\Set-Content -LiteralPath $tempScriptFilePath `
                                                            -Value $ScriptMetadataString,$scriptFileContents `
                                                            -Force `
                                                            -WhatIf:$false `
                                                            -Confirm:$false

                $tempScriptInfo = $null
                try
                {
                    $tempScriptInfo = Test-ScriptFileInfo -LiteralPath $tempScriptFilePath
                }
                catch
                {
                    $errorMessage = ($LocalizedData.UnableToAddPSScriptInfo -f $scriptFilePath)
                    ThrowError  -ExceptionName 'System.InvalidOperationException' `
                                -ExceptionMessage $errorMessage `
                                -ErrorId 'UnableToAddPSScriptInfo' `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $scriptFilePath `
                                -ErrorCategory InvalidOperation
                    return
                }
            }
            else
            {
                [System.Management.Automation.Language.Token[]]$tokens = $null;
                [System.Management.Automation.Language.ParseError[]]$errors = $null;
                $ast = [System.Management.Automation.Language.Parser]::ParseFile($scriptFilePath, ([ref]$tokens), ([ref]$errors))

                # Update PSScriptInfo and #Requires
                $CommentTokens = $tokens | Microsoft.PowerShell.Core\Where-Object {$_.Kind -eq 'Comment'}

                $psscriptInfoComments = $CommentTokens | 
                                            Microsoft.PowerShell.Core\Where-Object { $_.Extent.Text -match "<#PSScriptInfo" } | 
                                                Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

                if(-not $psscriptInfoComments)
                {
                    $errorMessage = ($LocalizedData.MissingPSScriptInfo -f $scriptFilePath)
                    ThrowError  -ExceptionName "System.ArgumentException" `
                                -ExceptionMessage $errorMessage `
                                -ErrorId "MissingPSScriptInfo" `
                                -CallerPSCmdlet $PSCmdlet `
                                -ExceptionObject $scriptFilePath `
                                -ErrorCategory InvalidArgument
                    return
                }

                # Ensure that metadata is replaced at the correct location and should not corrupt the existing script file.

                # Remove the lines between below lines and add the new PSScriptInfo and new #Requires statements
                # ($psscriptInfoComments.Extent.StartLineNumber - 1)
                # ($psscriptInfoComments.Extent.EndLineNumber - 1)
                $tempContents = @()
                $IsNewPScriptInfoAdded = $false

                for($i = 0; $i -lt $scriptFileContents.Count; $i++)
                {
                   $line = $scriptFileContents[$i]
                   if(($i -ge ($psscriptInfoComments.Extent.StartLineNumber - 1)) -and
                      ($i -le ($psscriptInfoComments.Extent.EndLineNumber - 1)))
                   {
                       if(-not $IsNewPScriptInfoAdded)
                       {
                           $PSScriptInfoString = $PSScriptInfoString.TrimStart()
                           $requiresStrings = $requiresStrings.TrimEnd()

                           $tempContents += "$PSScriptInfoString `r`n`r`n$($requiresStrings -join "`r`n")"
                           $IsNewPScriptInfoAdded = $true
                       }
                   }
                   elseif($line -notmatch "\s*#Requires\s+-Module")
                   {
                       # Add the existing lines if they are not part of PSScriptInfo comment or not containing #Requires -Module statements.
                       $tempContents += $line
                   }
                }

                Microsoft.PowerShell.Management\Set-Content -Value $tempContents -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false

                $scriptInfo = Test-ScriptFileInfo -Path $tempScriptFilePath

                if(-not $scriptInfo)
                {
                    # Above Test-ScriptFileInfo cmdlet writes the error
                    return
                }
            
                # Now update the Description value if a new is specified.
                if($Description)
                {
                    $tempContents = @()
                    $IsDescriptionAdded = $false
                
                    $IsDescriptionBeginFound = $false
                    $scriptFileContents = Microsoft.PowerShell.Management\Get-Content -Path $tempScriptFilePath

                    for($i = 0; $i -lt $scriptFileContents.Count; $i++)
                    {
                       $line = $scriptFileContents[$i]

                       if(-not $IsDescriptionAdded)
                       {
                            if(-not $IsDescriptionBeginFound)
                            {
                                if($line.Trim().StartsWith(".DESCRIPTION", [System.StringComparison]::OrdinalIgnoreCase))
                                {
                                   $IsDescriptionBeginFound = $true
                                }
                                else
                                {
                                    $tempContents += $line
                                }
                            }
                            else
                            {
                                # Description begin has found
                                # Skip the old description lines until description end is found

                                if($line.Trim().StartsWith("#>", [System.StringComparison]::OrdinalIgnoreCase) -or 
                                   $line.Trim().StartsWith(".", [System.StringComparison]::OrdinalIgnoreCase))
                                {
                                   $tempContents += ".DESCRIPTION `r`n$($Description -join "`r`n")`r`n"
                                   $IsDescriptionAdded = $true
                                   $tempContents += $line
                                }      
                            }
                       }
                       else
                       {
                           $tempContents += $line
                       }
                    }

                    Microsoft.PowerShell.Management\Set-Content -Value $tempContents -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false

                    $scriptInfo = Test-ScriptFileInfo -Path $tempScriptFilePath

                    if(-not $scriptInfo)
                    {
                        # Above Test-ScriptFileInfo cmdlet writes the error
                        return
                    }
                }
            }

            if($Force -or $PSCmdlet.ShouldProcess($scriptFilePath, ($LocalizedData.UpdateScriptFileInfowhatIfMessage -f $Path) ))
    	    {
                Microsoft.PowerShell.Management\Copy-Item -Path $tempScriptFilePath -Destination $scriptFilePath -Force -WhatIf:$false -Confirm:$false

                if($PassThru)
                {
                    $ScriptMetadataString
                }
            }
        }
        finally
        {
            Microsoft.PowerShell.Management\Remove-Item -Path $tempScriptFilePath -Force -WhatIf:$false -Confirm:$false -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
        }
    }
}

function Get-RequiresString
{
    [CmdletBinding()]
    Param
    (
        [Parameter()]
        [Object[]]
        $RequiredModules
    )

    Process
    {
        if($RequiredModules)
        {
            $RequiredModuleStrings = @()

            foreach($requiredModuleObject in $RequiredModules)
            {
                if($requiredModuleObject.GetType().ToString() -eq 'System.Collections.Hashtable')
                {
                    if(($requiredModuleObject.Keys.Count -eq 1) -and 
                        (Microsoft.PowerShell.Utility\Get-Member -InputObject $requiredModuleObject -Name 'ModuleName'))
                    {
                        $RequiredModuleStrings += $requiredModuleObject['ModuleName'].ToString()
                    }
                    else
                    {
                        $moduleSpec = New-Object Microsoft.PowerShell.Commands.ModuleSpecification -ArgumentList $requiredModuleObject
                        if (-not (Microsoft.PowerShell.Utility\Get-Variable -Name moduleSpec -ErrorAction SilentlyContinue))
                        {
                            return
                        }

                        $keyvalueStrings = $requiredModuleObject.Keys | Microsoft.PowerShell.Core\ForEach-Object {"$_ = '$( $requiredModuleObject[$_])'"}
                        $RequiredModuleStrings += "@{$($keyvalueStrings -join '; ')}"
                    }
                }
                elseif(($PSVersionTable.PSVersion -eq '3.0.0') -and
                       ($requiredModuleObject.GetType().ToString() -eq 'Microsoft.PowerShell.Commands.ModuleSpecification'))
                {
                    # ModuleSpecification.ToString() is not implemented on PowerShell 3.0.
                                    
                    $optionalString = " "
    
                    if($requiredModuleObject.Version)
                    {
                        $optionalString += "ModuleVersion = '$($requiredModuleObject.Version.ToString())'; "
                    }

                    if($requiredModuleObject.Guid)
                    {
                        $optionalString += "Guid = '$($requiredModuleObject.Guid.ToString())'; "
                    }
    
                    if($optionalString.Trim())
                    {
                        $moduleSpecString = "@{ ModuleName = '$($requiredModuleObject.Name.ToString())';$optionalString}"
                    }
                    else
                    {
                        $moduleSpecString = $requiredModuleObject.Name.ToString()
                    }

                    $RequiredModuleStrings += $moduleSpecString
                }
                else
                {
                    $RequiredModuleStrings += $requiredModuleObject.ToString()
                }
            }

            $hashRequiresStrings = $RequiredModuleStrings | 
                                       Microsoft.PowerShell.Core\ForEach-Object { "#Requires -Module $_" }
        
            return $hashRequiresStrings
        }
        else
        {
            return ""
        }
    }
}

function Get-PSScriptInfoString
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Version]
        $Version,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Guid]
        $Guid,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Author,

        [Parameter()] 
        [String]
        $CompanyName,

        [Parameter()]
        [string]
        $Copyright,

        [Parameter()]
        [String[]]
        $ExternalModuleDependencies,

        [Parameter()]
        [string[]]
        $RequiredScripts,

        [Parameter()]
        [String[]]
        $ExternalScriptDependencies,

        [Parameter()]
        [string[]]
        $Tags,

        [Parameter()]
        [Uri]
        $ProjectUri,

        [Parameter()]
        [Uri]
        $LicenseUri,

        [Parameter()]
        [Uri]
        $IconUri,

        [Parameter()]
        [string[]]
        $ReleaseNotes
    )

    Process
    {
        $PSScriptInfoString = @"

<#PSScriptInfo

.VERSION $Version

.GUID $Guid

.AUTHOR $Author

.COMPANYNAME $CompanyName

.COPYRIGHT $Copyright

.TAGS $Tags

.LICENSEURI $LicenseUri

.PROJECTURI $ProjectUri

.ICONURI $IconUri

.EXTERNALMODULEDEPENDENCIES $($ExternalModuleDependencies -join ',')

.REQUIREDSCRIPTS $($RequiredScripts -join ',')

.EXTERNALSCRIPTDEPENDENCIES $($ExternalScriptDependencies -join ',')

.RELEASENOTES
$($ReleaseNotes -join "`r`n")

#>
"@
        return $PSScriptInfoString
    }
}

function Validate-ScriptFileInfoParameters
{    
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]
        $Parameters
    )

    $hasErrors = $false

    $Parameters.Keys | ForEach-Object { 
                                     
                                    $parameterName = $_

                                    $parameterValue = $($Parameters[$parameterName])

                                    if("$parameterValue" -match '<#' -or "$parameterValue" -match '#>') 
                                    {
                                        $message = $LocalizedData.InvalidParameterValue -f ($parameterValue, $parameterName)
                                        Write-Error -Message $message -ErrorId 'InvalidParameterValue' -Category InvalidArgument

                                        $hasErrors = $true
                                    }
                                }

    return (-not $hasErrors)
}

function Get-ScriptCommentHelpInfoString
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Description,

        [Parameter()]
        [string]
        $Synopsis,

        [Parameter()]
        [string[]]
        $Example,

        [Parameter()]
        [string[]]
        $Inputs,

        [Parameter()]
        [string[]]
        $Outputs,

        [Parameter()]
        [string[]]
        $Notes,

        [Parameter()]
        [string[]]
        $Link,

        [Parameter()]
        [string]
        $Component,

        [Parameter()]
        [string]
        $Role,

        [Parameter()]
        [string]
        $Functionality
    )

    Process
    {
        $ScriptCommentHelpInfoString = "<# `r`n`r`n.DESCRIPTION `r`n $Description `r`n`r`n"

        if("$Synopsis".Trim())
        {
            $ScriptCommentHelpInfoString += ".SYNOPSIS `r`n$Synopsis `r`n`r`n"
        }

        if("$Example".Trim())
        {
            $Example | ForEach-Object {
                           if($_)
                           {
                               $ScriptCommentHelpInfoString += ".EXAMPLE `r`n$_ `r`n`r`n"
                           }
                       } 
        }

        if("$Inputs".Trim())
        {
            $Inputs |  ForEach-Object {
                           if($_)
                           {
                               $ScriptCommentHelpInfoString += ".INPUTS `r`n$_ `r`n`r`n"
                           }
                       } 
        }

        if("$Outputs".Trim())
        {
            $Outputs |  ForEach-Object {
                           if($_)
                           {
                               $ScriptCommentHelpInfoString += ".OUTPUTS `r`n$_ `r`n`r`n"
                           }
                       } 
        }

        if("$Notes".Trim())
        {
            $ScriptCommentHelpInfoString += ".NOTES `r`n$($Notes -join "`r`n") `r`n`r`n"
        }

        if("$Link".Trim())
        {
            $Link |  ForEach-Object {
                         if($_)
                         {
                              $ScriptCommentHelpInfoString += ".LINK `r`n$_ `r`n`r`n"
                         }
                     } 
        }

        if("$Component".Trim())
        {
            $ScriptCommentHelpInfoString += ".COMPONENT `r`n$($Component -join "`r`n") `r`n`r`n"
        }

        if("$Role".Trim())
        {
            $ScriptCommentHelpInfoString += ".ROLE `r`n$($Role -join "`r`n") `r`n`r`n"
        }

        if("$Functionality".Trim())
        {
            $ScriptCommentHelpInfoString += ".FUNCTIONALITY `r`n$($Functionality -join "`r`n") `r`n`r`n"
        }

        $ScriptCommentHelpInfoString += "#> `r`n"

        return $ScriptCommentHelpInfoString
    }
}

#endregion *-ScriptFileInfo cmdlets

#region Utility functions

function Get-ManifestHashTable
{
    param 
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )

    $Lines = $null

    try
    {
        $Lines = Get-Content -Path $Path -Force
    }
    catch
    {
        if($CallerPSCmdlet)
        {
            $CallerPSCmdlet.ThrowTerminatingError($_.Exception.ErrorRecord)
        }
    }

    if(-not $Lines)
    {
        return
    }
    
    $scriptBlock = [ScriptBlock]::Create( $Lines -join "`n" )

    $allowedVariables = [System.Collections.Generic.List[String]] @('PSEdition', 'PSScriptRoot')
    $allowedCommands = [System.Collections.Generic.List[String]] @()
    $allowEnvironmentVariables = $false

    try
    {
        $scriptBlock.CheckRestrictedLanguage($allowedCommands, $allowedVariables, $allowEnvironmentVariables)
    }
    catch
    {
        if($CallerPSCmdlet)
        {
            $CallerPSCmdlet.ThrowTerminatingError($_.Exception.ErrorRecord)
        }

        return
    }

    return $scriptBlock.InvokeReturnAsIs()
}

function Get-ParametersHashtable
{
    param(
        $Proxy,
        $ProxyCredential
    )

    $ParametersHashtable = @{}
    if($Proxy)
    {
        $ParametersHashtable[$script:Proxy] = $Proxy
    }

    if($ProxyCredential)
    {
        $ParametersHashtable[$script:ProxyCredential] = $ProxyCredential
    }

    return $ParametersHashtable
}

function ToUpper
{
    param([string]$str)
    return $script:TextInfo.ToUpper($str)
}

function Resolve-PathHelper
{
    param 
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $path,

        [Parameter()]
        [switch]
        $isLiteralPath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $callerPSCmdlet
    )
    
    $resolvedPaths =@()

    foreach($currentPath in $path)
    {
        try
        {
            if($isLiteralPath)
            {
                $currentResolvedPaths = Microsoft.PowerShell.Management\Resolve-Path -LiteralPath $currentPath -ErrorAction Stop
            }
            else
            {
                $currentResolvedPaths = Microsoft.PowerShell.Management\Resolve-Path -Path $currentPath -ErrorAction Stop
            }
        }
        catch
        {
            $errorMessage = ($LocalizedData.PathNotFound -f $currentPath)
            ThrowError  -ExceptionName "System.InvalidOperationException" `
                        -ExceptionMessage $errorMessage `
                        -ErrorId "PathNotFound" `
                        -CallerPSCmdlet $callerPSCmdlet `
                        -ErrorCategory InvalidOperation
        }

        foreach($currentResolvedPath in $currentResolvedPaths)
        {
            $resolvedPaths += $currentResolvedPath.ProviderPath
        }
    }

    $resolvedPaths
}

function Check-PSGalleryApiAvailability
{
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $PSGalleryV2ApiUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $PSGalleryV3ApiUri
    )   

    # check internet availability first
    $connected = $false
    $microsoftDomain = 'www.microsoft.com'
    if(Get-Command Microsoft.PowerShell.Management\Test-Connection -ErrorAction SilentlyContinue)
    {        
        $connected = Microsoft.PowerShell.Management\Test-Connection -ComputerName $microsoftDomain -Count 1 -Quiet
    }
    elseif(Get-Command NetTCPIP\Test-Connection -ErrorAction Ignore)
    {
        $connected = NetTCPIP\Test-NetConnection -ComputerName $microsoftDomain -InformationLevel Quiet
    }
    else
    {
        $connected = [System.Net.NetworkInformation.NetworkInterface]::GetIsNetworkAvailable()
    }

    if ( -not $connected)
    {
        return
    }

    $statusCode_v2 = $null
    $resolvedUri_v2 = $null
    $statusCode_v3 = $null
    $resolvedUri_v3 = $null

    # ping V2
    $res_v2 = Ping-Endpoint -Endpoint $PSGalleryV2ApiUri 
    if ($res_v2.ContainsKey($Script:ResponseUri))
    {
        $resolvedUri_v2 = $res_v2[$Script:ResponseUri]
    }
    if ($res_v2.ContainsKey($Script:StatusCode))
    {
        $statusCode_v2 = $res_v2[$Script:StatusCode]
    } 
    

    # ping V3
    $res_v3 = Ping-Endpoint -Endpoint $PSGalleryV3ApiUri
    if ($res_v3.ContainsKey($Script:ResponseUri))
    {
        $resolvedUri_v3 = $res_v3[$Script:ResponseUri]
    }
    if ($res_v3.ContainsKey($Script:StatusCode))
    {
        $statusCode_v3 = $res_v3[$Script:StatusCode]
    } 
    

    $Script:PSGalleryV2ApiAvailable = (($statusCode_v2 -eq 200) -and ($resolvedUri_v2))
    $Script:PSGalleryV3ApiAvailable = (($statusCode_v3 -eq 200) -and ($resolvedUri_v3))
    $Script:PSGalleryApiChecked = $true
}

function Get-PSGalleryApiAvailability
{
    param
    (
        [Parameter()]
        [string[]]
        $Repository
    )

    # skip if repository is null or not PSGallery
    if ( -not $Repository)
    {
        return
    }

    if ($Repository -notcontains $Script:PSGalleryModuleSource )
    {
        return
    }

    # run check only once 
    if( -not $Script:PSGalleryApiChecked)
    {
        $null = Check-PSGalleryApiAvailability -PSGalleryV2ApiUri $Script:PSGallerySourceUri -PSGalleryV3ApiUri $Script:PSGalleryV3SourceUri
    }

    if ( -not $Script:PSGalleryV2ApiAvailable )
    {
        if ($Script:PSGalleryV3ApiAvailable)
        {
            ThrowError -ExceptionName "System.InvalidOperationException" `
                       -ExceptionMessage $LocalizedData.PSGalleryApiV2Discontinued `
                       -ErrorId "PSGalleryApiV2Discontinued" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidOperation
        }
        else 
        {
            # both APIs are down, throw error
            ThrowError -ExceptionName "System.InvalidOperationException" `
                       -ExceptionMessage $LocalizedData.PowerShellGalleryUnavailable `
                       -ErrorId "PowerShellGalleryUnavailable" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidOperation
        }

    }
    else 
    {
        if ($Script:PSGalleryV3ApiAvailable)
        {
            Write-Warning -Message $LocalizedData.PSGalleryApiV2Deprecated
            return
        }
    }

    # if V2 is available and V3 is not available, do nothing  
}

function HttpClientApisAvailable
{
    $HttpClientApisAvailable = $false
    try 
    {
        [System.Net.Http.HttpClient]
        $HttpClientApisAvailable = $true
    } 
    catch 
    {
    }
    return $HttpClientApisAvailable
}

function Ping-Endpoint
{
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Endpoint,

        [Parameter()]
        $Credential,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential,

        [Parameter()]
        [switch]
        $AllowAutoRedirect = $true
    )   
        
    $results = @{}

    $WebProxy = $null
    if($Proxy -and (IsWindows))
    {
        $ProxyNetworkCredential = $null
        if($ProxyCredential)
        {
            $ProxyNetworkCredential = $ProxyCredential.GetNetworkCredential()
        }

        $WebProxy = New-Object Microsoft.PowerShell.Commands.PowerShellGet.InternalWebProxy -ArgumentList $Proxy,$ProxyNetworkCredential
    }

    if(HttpClientApisAvailable)
    {
        $response = $null
        try
        {
            $handler = New-Object System.Net.Http.HttpClientHandler
            
            if($Credential)
            {
                $handler.Credentials = $Credential.GetNetworkCredential()
            }
            else
            {
                $handler.UseDefaultCredentials = $true
            }

            if($WebProxy)
            {
                $handler.Proxy = $WebProxy
            }

            $httpClient = New-Object System.Net.Http.HttpClient -ArgumentList $handler
            $response = $httpclient.GetAsync($endpoint)  
        }
        catch
        {            
        } 

        if ($response -ne $null -and $response.result -ne $null)
        {        
            $results.Add($Script:ResponseUri,$response.Result.RequestMessage.RequestUri.AbsoluteUri.ToString())
            $results.Add($Script:StatusCode,$response.result.StatusCode.value__)            
        }
    }
    else
    {
        $iss = [System.Management.Automation.Runspaces.InitialSessionState]::Create()
        $iss.types.clear()
        $iss.formats.clear()
        $iss.LanguageMode = "FullLanguage"

        $WebRequestcmd =  @'
            param($Credential, $WebProxy)  

            try
            {{
                $request = [System.Net.WebRequest]::Create("{0}")
                $request.Method = 'GET'
                $request.Timeout = 30000
                if($Credential)
                {{
                    $request.Credentials = $Credential.GetNetworkCredential()
                }}
                else
                {{
                    $request.Credentials = [System.Net.CredentialCache]::DefaultNetworkCredentials
                }}

                $request.AllowAutoRedirect = ${1}
                
                if($WebProxy)
                {{
                    $request.Proxy = $WebProxy
                }}

                $response = [System.Net.HttpWebResponse]$request.GetResponse()             
                if($response.StatusCode.value__ -eq 302)
                {{
                    $response.Headers["Location"].ToString()
                }}
                else
                {{
                    $response
                }}                
                $response.Close()
            }}
            catch [System.Net.WebException]
            {{
                "Error:System.Net.WebException"
            }} 
'@ -f $EndPoint, $AllowAutoRedirect

        $ps = [powershell]::Create($iss).AddScript($WebRequestcmd)

        if($WebProxy)
        {
            $null = $ps.AddParameter('WebProxy', $WebProxy)
        }

        if($Credential)
        {
            $null = $ps.AddParameter('Credential', $Credential)
        }

        $response = $ps.Invoke()
        $ps.dispose()
        if ($response -ne "Error:System.Net.WebException")
        {            
            if($AllowAutoRedirect)
            {
                $results.Add($Script:ResponseUri,$response.ResponseUri.ToString())                
                $results.Add($Script:StatusCode,$response.StatusCode.value__)
            }
            else
            {
                $results.Add($Script:ResponseUri,[String]$response)                 
            }
        }
    }    
    return $results
}

function Validate-VersionParameters
{
    Param(
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet,

        [Parameter()]
        [String[]]
        $Name,

        [Parameter()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [Version]
        $MaximumVersion,

        [Parameter()]
        [Switch]
        $AllVersions,

        [Parameter()]
        [Switch]
        $TestWildcardsInName
    )

    if($TestWildcardsInName -and $Name -and (Test-WildcardPattern -Name "$Name"))
    {
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage ($LocalizedData.NameShouldNotContainWildcardCharacters -f "$($Name -join ',')") `
                   -ErrorId 'NameShouldNotContainWildcardCharacters' `
                   -CallerPSCmdlet $CallerPSCmdlet `
                   -ErrorCategory InvalidArgument `
                   -ExceptionObject $Name
    }
    elseif($AllVersions -and ($RequiredVersion -or $MinimumVersion -or $MaximumVersion))
    {
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $LocalizedData.AllVersionsCannotBeUsedWithOtherVersionParameters `
                   -ErrorId 'AllVersionsCannotBeUsedWithOtherVersionParameters' `
                   -CallerPSCmdlet $CallerPSCmdlet `
                   -ErrorCategory InvalidArgument
    }
    elseif($RequiredVersion -and ($MinimumVersion -or $MaximumVersion))
    {
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $LocalizedData.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether `
                   -ErrorId "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether" `
                   -CallerPSCmdlet $CallerPSCmdlet `
                   -ErrorCategory InvalidArgument
    }
    elseif($MinimumVersion -and $MaximumVersion -and ($MinimumVersion -gt $MaximumVersion))
    {
        $Message = $LocalizedData.MinimumVersionIsGreaterThanMaximumVersion -f ($MinimumVersion, $MaximumVersion)
        ThrowError -ExceptionName "System.ArgumentException" `
                    -ExceptionMessage $Message `
                    -ErrorId "MinimumVersionIsGreaterThanMaximumVersion" `
                    -CallerPSCmdlet $CallerPSCmdlet `
                    -ErrorCategory InvalidArgument
    }
    elseif($AllVersions -or $RequiredVersion -or $MinimumVersion -or $MaximumVersion)
    {
        if(-not $Name -or $Name.Count -ne 1 -or (Test-WildcardPattern -Name $Name[0]))
        {
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $LocalizedData.VersionParametersAreAllowedOnlyWithSingleName `
                       -ErrorId "VersionParametersAreAllowedOnlyWithSingleName" `
                       -CallerPSCmdlet $CallerPSCmdlet `
                       -ErrorCategory InvalidArgument
        }
    }

    return $true
}

function ValidateAndSet-PATHVariableIfUserAccepts
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $Scope,

        [Parameter(Mandatory=$true)]
        [string]
        $ScopePath,

        [Parameter()]
        [Switch]
        $NoPathUpdate,

        [Parameter()]
        [Switch]
        $Force,

        [Parameter()]
        $Request
    )

    if(-not (IsWindows))
    {
        return
    }

    Set-PSGetSettingsVariable

    # Check and add the scope path to PATH environment variable if USER accepts the prompt.
    if($Scope -eq 'AllUsers')
    {
        $envVariableTarget = $script:EnvironmentVariableTarget.Machine
        $scriptPATHPromptQuery=$LocalizedData.ScriptPATHPromptQuery -f $ScopePath
        $scopeSpecificKey = 'AllUsersScope_AllowPATHChangeForScripts'
    }
    else
    {
        $envVariableTarget = $script:EnvironmentVariableTarget.User
        $scriptPATHPromptQuery=$LocalizedData.ScriptPATHPromptQuery -f $ScopePath
        $scopeSpecificKey = 'CurrentUserScope_AllowPATHChangeForScripts'
    }

    $AlreadyPromptedForScope = $script:PSGetSettings.Contains($scopeSpecificKey)
    Write-Debug "Already prompted for the current scope:$AlreadyPromptedForScope"

    if(-not $AlreadyPromptedForScope)
    {
        # Read the file contents once again to ensure that it was not set in another PowerShell Session
        Set-PSGetSettingsVariable -Force
        
        $AlreadyPromptedForScope = $script:PSGetSettings.Contains($scopeSpecificKey)
        Write-Debug "After reading contents of PowerShellGetSettings.xml file, the Already prompted for the current scope:$AlreadyPromptedForScope"

        if($AlreadyPromptedForScope)
        {
            return
        }

        $userResponse = $false

        if(-not $NoPathUpdate)
        {
            $scopePathEndingWithBackSlash = "$scopePath\"

            # Check and add the $scopePath to $env:Path value
            if( (($env:PATH -split ';') -notcontains $scopePath) -and
                (($env:PATH -split ';') -notcontains $scopePathEndingWithBackSlash))
            {
                if($Force)
                {
                    $userResponse = $true
                }
                else
                {
                    $scriptPATHPromptCaption = $LocalizedData.ScriptPATHPromptCaption

                    if($Request)
                    {
                        $userResponse = $Request.ShouldContinue($scriptPATHPromptQuery, $scriptPATHPromptCaption)
                    }
                    else
                    {
                        $userResponse = $PSCmdlet.ShouldContinue($scriptPATHPromptQuery, $scriptPATHPromptCaption)
                    }
                }

                if($userResponse)
                {
                    $currentPATHValue = Get-EnvironmentVariable -Name 'PATH' -Target $envVariableTarget

                    if((($currentPATHValue -split ';') -notcontains $scopePath) -and
                       (($currentPATHValue -split ';') -notcontains $scopePathEndingWithBackSlash))
                    {
                        # To ensure that the installed script is immediately usable, 
                        # we need to add the scope path to the PATH enviroment variable.
                        Set-EnvironmentVariable -Name 'PATH' `
                                                -Value "$currentPATHValue;$scopePath" `
                                                -Target $envVariableTarget

                        Write-Verbose ($LocalizedData.AddedScopePathToPATHVariable -f ($scopePath,$Scope))
                    }

                    # Process specific PATH
                    # Check and add the $scopePath to $env:Path value of current process
                    # so that installed scripts can be used in the current process.
                    $target = $script:EnvironmentVariableTarget.Process
                    $currentPATHValue = Get-EnvironmentVariable -Name 'PATH' -Target $target

                    if((($currentPATHValue -split ';') -notcontains $scopePath) -and
                       (($currentPATHValue -split ';') -notcontains $scopePathEndingWithBackSlash))
                    {
                        # To ensure that the installed script is immediately usable, 
                        # we need to add the scope path to the PATH enviroment variable.
                        Set-EnvironmentVariable -Name 'PATH' `
                                                -Value "$currentPATHValue;$scopePath" `
                                                -Target $target

                        Write-Verbose ($LocalizedData.AddedScopePathToProcessSpecificPATHVariable -f ($scopePath,$Scope))
                    }
                }
            }
        }

        # Add user's response to the PowerShellGet.settings file
        $script:PSGetSettings[$scopeSpecificKey] = $userResponse

        Save-PSGetSettings
    }
}

function Save-PSGetSettings
{
    if($script:PSGetSettings)
    {
        if(-not (Microsoft.PowerShell.Management\Test-Path -Path $script:PSGetAppLocalPath))
        {
            $null = Microsoft.PowerShell.Management\New-Item -Path $script:PSGetAppLocalPath `
                                                             -ItemType Directory `
                                                             -Force `
                                                             -ErrorAction SilentlyContinue `
                                                             -WarningAction SilentlyContinue `
                                                             -Confirm:$false `
                                                             -WhatIf:$false
        }

        Microsoft.PowerShell.Utility\Out-File -FilePath $script:PSGetSettingsFilePath -Force `
            -InputObject ([System.Management.Automation.PSSerializer]::Serialize($script:PSGetSettings))
        
        Write-Debug "In Save-PSGetSettings, persisted the $script:PSGetSettingsFilePath file"
   }
}

function Set-PSGetSettingsVariable
{
    [CmdletBinding()]
    param([switch]$Force)

    if(-not $script:PSGetSettings -or $Force)
    {
        if(Microsoft.PowerShell.Management\Test-Path -Path $script:PSGetSettingsFilePath)
        {
            $script:PSGetSettings = DeSerialize-PSObject -Path $script:PSGetSettingsFilePath
        }
        else
        {
            $script:PSGetSettings = [ordered]@{}
        }
    }   
}

function Set-PSGalleryRepository
{
    [CmdletBinding()]
    param(
        [Parameter()]
        [switch]
        $Trusted,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential
    )

    $psgalleryLocation = Resolve-Location -Location $Script:PSGallerySourceUri `
                                          -LocationParameterName 'SourceLocation' `
                                          -Proxy $Proxy `
                                          -ProxyCredential $ProxyCredential `
                                          -ErrorAction SilentlyContinue `
                                          -WarningAction SilentlyContinue

    $scriptSourceLocation = Resolve-Location -Location $Script:PSGalleryScriptSourceUri `
                                             -LocationParameterName 'ScriptSourceLocation' `
                                             -Proxy $Proxy `
                                             -ProxyCredential $ProxyCredential `
                                             -ErrorAction SilentlyContinue `
                                             -WarningAction SilentlyContinue
    if($psgalleryLocation)
    {
        $result = Ping-Endpoint -Endpoint $Script:PSGalleryPublishUri -AllowAutoRedirect:$false -Proxy $Proxy -ProxyCredential $ProxyCredential
        if ($result.ContainsKey($Script:ResponseUri) -and $result[$Script:ResponseUri])
        {   
                $script:PSGalleryPublishUri = $result[$Script:ResponseUri]                    
        }

        $repository = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                Name = $Script:PSGalleryModuleSource
                SourceLocation =  $psgalleryLocation
                PublishLocation = $Script:PSGalleryPublishUri
                ScriptSourceLocation = $scriptSourceLocation
                ScriptPublishLocation = $Script:PSGalleryPublishUri
                Trusted=$Trusted
                Registered=$true
                InstallationPolicy = if($Trusted) {'Trusted'} else {'Untrusted'}
                PackageManagementProvider=$script:NuGetProviderName
                ProviderOptions = @{}
            })

        $repository.PSTypeNames.Insert(0, "Microsoft.PowerShell.Commands.PSRepository")
        $script:PSGetModuleSources[$Script:PSGalleryModuleSource] = $repository
        
        Save-ModuleSources

        return $repository
    }
}

function Set-ModuleSourcesVariable
{
    [CmdletBinding()]
    param(
        [switch]
        $Force,

        $Proxy,

        $ProxyCredential
    )

    if(-not $script:PSGetModuleSources -or $Force)
    {
        $isPersistRequired = $false
        if(Microsoft.PowerShell.Management\Test-Path $script:PSGetModuleSourcesFilePath)
        {
            $script:PSGetModuleSources = DeSerialize-PSObject -Path $script:PSGetModuleSourcesFilePath
        }
        else
        {
            $script:PSGetModuleSources = [ordered]@{}

            if(-not $script:PSGetModuleSources.Contains($Script:PSGalleryModuleSource))
            {
                $null = Set-PSGalleryRepository -Proxy $Proxy -ProxyCredential $ProxyCredential
            }
        }

        # Already registered repositories may not have the ScriptSourceLocation property, try to populate it from the existing SourceLocation
        # Also populate the PublishLocation and ScriptPublishLocation from the SourceLocation if PublishLocation is empty/null.
        # 
        $script:PSGetModuleSources.Keys | Microsoft.PowerShell.Core\ForEach-Object { 
                                              $moduleSource = $script:PSGetModuleSources[$_]

                                              if(-not (Get-Member -InputObject $moduleSource -Name $script:ScriptSourceLocation))
                                              {
                                                  $scriptSourceLocation = Get-ScriptSourceLocation -Location $moduleSource.SourceLocation -Proxy $Proxy -ProxyCredential $ProxyCredential

                                                  Microsoft.PowerShell.Utility\Add-Member -InputObject $script:PSGetModuleSources[$_] `
                                                                                          -MemberType NoteProperty `
                                                                                          -Name $script:ScriptSourceLocation `
                                                                                          -Value $scriptSourceLocation

                                                  if(Get-Member -InputObject $moduleSource -Name $script:PublishLocation)
                                                  {
                                                      if(-not $moduleSource.PublishLocation)
                                                      {
                                                          $script:PSGetModuleSources[$_].PublishLocation = Get-PublishLocation -Location $moduleSource.SourceLocation
                                                      }

                                                      Microsoft.PowerShell.Utility\Add-Member -InputObject $script:PSGetModuleSources[$_] `
                                                                                              -MemberType NoteProperty `
                                                                                              -Name $script:ScriptPublishLocation `
                                                                                              -Value $moduleSource.PublishLocation
                                                  }

                                                  $isPersistRequired = $true
                                              }
                                          }
        
        if($isPersistRequired)
        {
            Save-ModuleSources
        }
    }   
}

function Get-PackageManagementProviderName
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Location
    )

    $PackageManagementProviderName = $null
    $loc = Get-LocationString -LocationUri $Location

    $providers = PackageManagement\Get-PackageProvider | Where-Object { $_.Features.ContainsKey($script:SupportsPSModulesFeatureName) }

    foreach($provider in $providers)
    {
        # Skip the PowerShellGet provider
        if($provider.ProviderName -eq $script:PSModuleProviderName)
        {
            continue
        }

        $packageSource = Get-PackageSource -Location $loc -Provider $provider.ProviderName  -ErrorAction SilentlyContinue 
                    
        if($packageSource)
        {
            $PackageManagementProviderName = $provider.ProviderName
            break
        }
    }

    return $PackageManagementProviderName
}

function Get-ProviderName
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]
        $PSCustomObject
    )

    $providerName = $script:NuGetProviderName

    if((Get-Member -InputObject $PSCustomObject -Name PackageManagementProvider))
    {
        $providerName = $PSCustomObject.PackageManagementProvider
    }

    return $providerName
}

function Get-DynamicParameters
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $Location,

        [Parameter(Mandatory=$true)]
        [REF]
        $PackageManagementProvider
    )

    $paramDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary
    $dynamicOptions = $null

    $loc = Get-LocationString -LocationUri $Location

    if(-not $loc)
    {
        return $paramDictionary
    }

    # Ping and resolve the specified location
    $loc = Resolve-Location -Location $loc `
                            -LocationParameterName 'Location' `
                            -ErrorAction SilentlyContinue `
                            -WarningAction SilentlyContinue
    if(-not $loc)
    {
        return $paramDictionary
    }

    $providers = PackageManagement\Get-PackageProvider | Where-Object { $_.Features.ContainsKey($script:SupportsPSModulesFeatureName) }
            
    if ($PackageManagementProvider.Value)
    {
        # Skip the PowerShellGet provider
        if($PackageManagementProvider.Value -ne $script:PSModuleProviderName)
        {
            $SelectedProvider = $providers | Where-Object {$_.ProviderName -eq $PackageManagementProvider.Value}

            if($SelectedProvider)
            {
                $res = Get-PackageSource -Location $loc -Provider $PackageManagementProvider.Value -ErrorAction SilentlyContinue 
            
                if($res)
                {
                    $dynamicOptions = $SelectedProvider.DynamicOptions
                }
            }
        }
    }
    else
    {
        $PackageManagementProvider.Value = Get-PackageManagementProviderName -Location $Location
        if($PackageManagementProvider.Value)
        {
            $provider = $providers | Where-Object {$_.ProviderName -eq $PackageManagementProvider.Value}
            $dynamicOptions = $provider.DynamicOptions
        }
    }

    foreach ($option in $dynamicOptions)
    {
        # Skip the Destination parameter
        if( $option.IsRequired -and 
            ($option.Name -eq "Destination") )
        {
            continue
        }

        $paramAttribute = New-Object System.Management.Automation.ParameterAttribute
        $paramAttribute.Mandatory = $option.IsRequired

        $message = $LocalizedData.DynamicParameterHelpMessage -f ($option.Name, $PackageManagementProvider.Value, $loc, $option.Name)
        $paramAttribute.HelpMessage = $message

        $attributeCollection = new-object System.Collections.ObjectModel.Collection[System.Attribute]
        $attributeCollection.Add($paramAttribute)

        $ageParam = New-Object System.Management.Automation.RuntimeDefinedParameter($option.Name,
                                                                                    $script:DynamicOptionTypeMap[$option.Type.value__],
                                                                                    $attributeCollection)
        $paramDictionary.Add($option.Name, $ageParam)
    }

    return $paramDictionary
}

function New-PSGetItemInfo
{
    param
    (
        [Parameter(Mandatory=$true)]
        $SoftwareIdentity,

        [Parameter()]
        $PackageManagementProviderName,

        [Parameter()]
        [string]
        $SourceLocation,

        [Parameter(Mandatory=$true)]
        [string]
        $Type,

        [Parameter()]
        [string]
        $InstalledLocation,

        [Parameter()]
        [System.DateTime]
        $InstalledDate,

        [Parameter()]
        [System.DateTime]
        $UpdatedDate
    )

    foreach($swid in $SoftwareIdentity)
    {

        if($SourceLocation)
        {
            $sourceName = (Get-SourceName -Location $SourceLocation)
        }
        else
        {
            # First get the source name from the Metadata
            # if not exists, get the source name from $swid.Source
            # otherwise default to $swid.Source  
            $sourceName = (Get-First $swid.Metadata["SourceName"])

            if(-not $sourceName)
            {
                $sourceName = (Get-SourceName -Location $swid.Source)
            }

            if(-not $sourceName)
            {
                $sourceName = $swid.Source
            }

            $SourceLocation = Get-SourceLocation -SourceName $sourceName
        }

        $published = (Get-First $swid.Metadata["published"])
        $PublishedDate = New-Object System.DateTime

        $InstalledDateString = (Get-First $swid.Metadata['installeddate'])
        if(-not $InstalledDate -and $InstalledDateString)
        {
            $InstalledDate = New-Object System.DateTime
            if(-not ([System.DateTime]::TryParse($InstalledDateString, ([ref]$InstalledDate))))
            {
                $InstalledDate = $null
            }
        }

        $UpdatedDateString = (Get-First $swid.Metadata['updateddate'])
        if(-not $UpdatedDate -and $UpdatedDateString)
        {
            $UpdatedDate = New-Object System.DateTime
            if(-not ([System.DateTime]::TryParse($UpdatedDateString, ([ref]$UpdatedDate))))
            {
                $UpdatedDate = $null
            }
        }

        $tags = (Get-First $swid.Metadata["tags"]) -split " "
        $userTags = @()
		
        $exportedDscResources = @()
        $exportedRoleCapabilities = @()
        $exportedCmdlets = @()
        $exportedFunctions = @()
        $exportedWorkflows = @()
        $exportedCommands = @()
		
        $exportedRoleCapabilities += (Get-First $swid.Metadata['RoleCapabilities']) -split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
        $exportedDscResources += (Get-First $swid.Metadata["DscResources"]) -split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
        $exportedCmdlets += (Get-First $swid.Metadata["Cmdlets"]) -split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
        $exportedFunctions += (Get-First $swid.Metadata["Functions"]) -split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
        $exportedWorkflows += (Get-First $swid.Metadata["Workflows"]) -split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
        $exportedCommands += $exportedCmdlets + $exportedFunctions + $exportedWorkflows
        $PSGetFormatVersion = $null

        ForEach($tag in $tags)
        {
            if(-not $tag.Trim())
            {
                continue
            }

            $parts = $tag -split "_",2
            if($parts.Count -ne 2)
            {
                $userTags += $tag
                continue
            }

            Switch($parts[0])
            {
                $script:Command            { $exportedCommands += $parts[1]; break }
                $script:DscResource        { $exportedDscResources += $parts[1]; break }
                $script:Cmdlet             { $exportedCmdlets += $parts[1]; break }
                $script:Function           { $exportedFunctions += $parts[1]; break }
                $script:Workflow           { $exportedWorkflows += $parts[1]; break }
                $script:RoleCapability     { $exportedRoleCapabilities += $parts[1]; break }
                $script:PSGetFormatVersion { $PSGetFormatVersion = $parts[1]; break }
                $script:Includes           { break }
                Default                    { $userTags += $tag; break }
            }
        }

        $ArtifactDependencies = @()
        Foreach ($dependencyString in $swid.Dependencies)
        {
            [Uri]$packageId = $null
            if([Uri]::TryCreate($dependencyString, [System.UriKind]::Absolute, ([ref]$packageId)))
            {
                $segments = $packageId.Segments
                $Version = $null
                $DependencyName = $null
                if ($segments)   
                {
                    $DependencyName = [Uri]::UnescapeDataString($segments[0].Trim('/', '\'))
                    $Version = if($segments.Count -gt 1){[Uri]::UnescapeDataString($segments[1])}
                }

                $dep = [ordered]@{
                            Name=$DependencyName
                        }

                if($Version)
                {
                    # Required/exact version is represented in NuGet as "[2.0]"
                    if ($Version -match "\[+[0-9.]+\]")
                    {
                        $dep["RequiredVersion"] = $Version.Trim('[', ']')
                    }
                    elseif ($Version -match "\[+[0-9., ]+\]")
                    {
                        # Minimum and Maximum version range is represented in NuGet as "[1.0, 2.0]"
                        $versionRange = $Version.Trim('[', ']') -split ',' | Microsoft.PowerShell.Core\Where-Object {$_}
                        if($versionRange -and $versionRange.count -eq 2)
                        {
                            $dep["MinimumVersion"] = $versionRange[0].Trim()
                            $dep["MaximumVersion"] = $versionRange[1].Trim()
                        }
                    }
                    elseif ($Version -match "\(+[0-9., ]+\]")
                    {
                        # Maximum version is represented in NuGet as "(, 2.0]"
                        $maximumVersion = $Version.Trim('(', ']') -split ',' | Microsoft.PowerShell.Core\Where-Object {$_}

                        if($maximumVersion)
                        {
                            $dep["MaximumVersion"] = $maximumVersion.Trim()
                        }
                    }
                    else
                    {
                        $dep['MinimumVersion'] = $Version
                    }
                }
                
                $dep["CanonicalId"]=$dependencyString

                $ArtifactDependencies += $dep
            }
        }
		
        $additionalMetadata =  New-Object -TypeName  System.Collections.Hashtable
        foreach ( $key in $swid.Metadata.Keys.LocalName)
        {
            if (!$additionalMetadata.ContainsKey($key))
            {
                $additionalMetadata.Add($key, (Get-First $swid.Metadata[$key]) )
            }
        }

        if($additionalMetadata.ContainsKey('ItemType'))
        {
            $Type = $additionalMetadata['ItemType']
        }
        elseif($userTags -contains 'PSModule')
        {
            $Type = $script:PSArtifactTypeModule
        }
        elseif($userTags -contains 'PSScript')
        {
            $Type = $script:PSArtifactTypeScript
        }

        $PSGetItemInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                Name = $swid.Name
                Version = [Version]$swid.Version
                Type = $Type    
                Description = (Get-First $swid.Metadata["description"])
                Author = (Get-EntityName -SoftwareIdentity $swid -Role "author")
                CompanyName = (Get-EntityName -SoftwareIdentity $swid -Role "owner")
                Copyright = (Get-First $swid.Metadata["copyright"])
                PublishedDate = if([System.DateTime]::TryParse($published, ([ref]$PublishedDate))){$PublishedDate};
                InstalledDate = $InstalledDate;
                UpdatedDate = $UpdatedDate;
                LicenseUri = (Get-UrlFromSwid -SoftwareIdentity $swid -UrlName "license")
                ProjectUri = (Get-UrlFromSwid -SoftwareIdentity $swid -UrlName "project")
                IconUri = (Get-UrlFromSwid -SoftwareIdentity $swid -UrlName "icon")
                Tags = $userTags

                Includes = @{
                                DscResource = $exportedDscResources
                                Command     = $exportedCommands
                                Cmdlet      = $exportedCmdlets
                                Function    = $exportedFunctions
                                Workflow    = $exportedWorkflows
                                RoleCapability = $exportedRoleCapabilities
                            }

                PowerShellGetFormatVersion=[Version]$PSGetFormatVersion

                ReleaseNotes = (Get-First $swid.Metadata["releaseNotes"])

                Dependencies = $ArtifactDependencies

                RepositorySourceLocation = $SourceLocation
                Repository = $sourceName
                PackageManagementProvider = if($PackageManagementProviderName) { $PackageManagementProviderName } else { (Get-First $swid.Metadata["PackageManagementProvider"]) }
				
				AdditionalMetadata = $additionalMetadata
            })

        if(-not $InstalledLocation)
        {
            $InstalledLocation = (Get-First $swid.Metadata[$script:InstalledLocation])
        }

        if($InstalledLocation)
        {
            Microsoft.PowerShell.Utility\Add-Member -InputObject $PSGetItemInfo -MemberType NoteProperty -Name $script:InstalledLocation -Value $InstalledLocation
        }

        $PSGetItemInfo.PSTypeNames.Insert(0, "Microsoft.PowerShell.Commands.PSRepositoryItemInfo")
        $PSGetItemInfo
    }
}

function Get-SourceName
{
    [CmdletBinding()]
    [OutputType("string")]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Location
    )

    Set-ModuleSourcesVariable

    foreach($psModuleSource in $script:PSGetModuleSources.Values)
    {
        if(($psModuleSource.Name -eq $Location) -or
           ($psModuleSource.SourceLocation -eq $Location) -or
           ((Get-Member -InputObject $psModuleSource -Name $script:ScriptSourceLocation) -and
           ($psModuleSource.ScriptSourceLocation -eq $Location)))
        {
            return $psModuleSource.Name
        }
    }
}

function Get-SourceLocation
{
    [CmdletBinding()]
    [OutputType("string")]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $SourceName
    )

    Set-ModuleSourcesVariable

    if($script:PSGetModuleSources.Contains($SourceName))
    {
        return $script:PSGetModuleSources[$SourceName].SourceLocation
    }
    else
    {
        return $SourceName
    }
}

function Get-UrlFromSwid
{
    param
    (
        [Parameter(Mandatory=$true)]
        $SoftwareIdentity,

        [Parameter(Mandatory=$true)]
        $UrlName
    )
    
    foreach($link in $SoftwareIdentity.Links)
    {
        if( $link.Relationship -eq $UrlName)
        {
            return $link.HRef
        }
    }

    return $null
}

function Get-EntityName
{
    param
    (
        [Parameter(Mandatory=$true)]
        $SoftwareIdentity,

        [Parameter(Mandatory=$true)]
        $Role
    )

    foreach( $entity in $SoftwareIdentity.Entities )
    {
        if( $entity.Role -eq $Role)
        {
            $entity.Name
        }
    }
}

function Install-NuGetClientBinaries
{
    [CmdletBinding()]
    param
    (
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet,

        [parameter()]
        [switch]
        $BootstrapNuGetExe,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential,

        [parameter()]
        [switch]
        $Force
    )

    if(-not (IsWindows) -or
       ($script:NuGetProvider -and 
        (-not $BootstrapNuGetExe -or 
        ($script:NuGetExePath -and (Microsoft.PowerShell.Management\Test-Path -Path $script:NuGetExePath)))))
    {
        return
    }

    $bootstrapNuGetProvider = (-not $script:NuGetProvider)

    if($bootstrapNuGetProvider)
    {
        # Bootstrap the NuGet provider only if it is not available.
        # By default PackageManagement loads the latest version of the NuGet provider.
        $nugetProvider = PackageManagement\Get-PackageProvider -ErrorAction SilentlyContinue -WarningAction SilentlyContinue |
                            Microsoft.PowerShell.Core\Where-Object { 
                                                                     $_.Name -eq $script:NuGetProviderName -and 
                                                                     $_.Version -ge $script:NuGetProviderVersion
                                                                   }
        if($nugetProvider)
        {
            $script:NuGetProvider = $nugetProvider
        
            $bootstrapNuGetProvider = $false
        }
        else
        {
            # User might have installed it in an another console or in the same process, check available NuGet providers and import the required provider.
            $availableNugetProviders = PackageManagement\Get-PackageProvider -Name $script:NuGetProviderName `
                                                                             -ListAvailable `
                                                                             -ErrorAction SilentlyContinue `
                                                                             -WarningAction SilentlyContinue |
                                            Microsoft.PowerShell.Core\Where-Object { 
                                                                                       $_.Name -eq $script:NuGetProviderName -and 
                                                                                       $_.Version -ge $script:NuGetProviderVersion
                                                                                   }
            if($availableNugetProviders)
            {
                # Force import ensures that nuget provider with minimum version got loaded.
                $null = PackageManagement\Import-PackageProvider -Name $script:NuGetProviderName `
                                                                 -MinimumVersion $script:NuGetProviderVersion `
                                                                 -Force

                $nugetProvider = PackageManagement\Get-PackageProvider -ErrorAction SilentlyContinue -WarningAction SilentlyContinue |
                                    Microsoft.PowerShell.Core\Where-Object { 
                                                                             $_.Name -eq $script:NuGetProviderName -and 
                                                                             $_.Version -ge $script:NuGetProviderVersion
                                                                           }
                if($nugetProvider)
                {
                    $script:NuGetProvider = $nugetProvider
        
                    $bootstrapNuGetProvider = $false
                }
            }
        }
    }

    if($BootstrapNuGetExe -and 
       (-not $script:NuGetExePath -or 
        -not (Microsoft.PowerShell.Management\Test-Path -Path $script:NuGetExePath)))
    {
        $programDataExePath = Microsoft.PowerShell.Management\Join-Path -Path $script:PSGetProgramDataPath -ChildPath $script:NuGetExeName
        $applocalDataExePath = Microsoft.PowerShell.Management\Join-Path -Path $script:PSGetAppLocalPath -ChildPath $script:NuGetExeName        

        # Check if NuGet.exe is available under one of the predefined PowerShellGet locations under ProgramData or LocalAppData
        if(Microsoft.PowerShell.Management\Test-Path -Path $programDataExePath)
        {
            $script:NuGetExePath = $programDataExePath
            $BootstrapNuGetExe = $false
        }
        elseif(Microsoft.PowerShell.Management\Test-Path -Path $applocalDataExePath)
        {
            $script:NuGetExePath = $applocalDataExePath
            $BootstrapNuGetExe = $false
        }
        else
        {
            # Using Get-Command cmdlet, get the location of NuGet.exe if it is available under $env:PATH.
            # NuGet.exe does not work if it is under $env:WINDIR, so skip it from the Get-Command results.
            $nugetCmd = Microsoft.PowerShell.Core\Get-Command -Name $script:NuGetExeName `
                                                              -ErrorAction SilentlyContinue `
                                                              -WarningAction SilentlyContinue | 
                            Microsoft.PowerShell.Core\Where-Object { 
                                $_.Path -and 
                                ((Microsoft.PowerShell.Management\Split-Path -Path $_.Path -Leaf) -eq $script:NuGetExeName) -and
                                (-not $_.Path.StartsWith($env:windir, [System.StringComparison]::OrdinalIgnoreCase)) 
                            } | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

            if($nugetCmd -and $nugetCmd.Path)
            {
                $script:NuGetExePath = $nugetCmd.Path
                $BootstrapNuGetExe = $false
            }
        }
    }
    else
    {
        # No need to bootstrap the NuGet.exe when $BootstrapNuGetExe is false or NuGet.exe path is already assigned.
        $BootstrapNuGetExe = $false
    }
    
    # On Nano server we don't need NuGet.exe
    if(-not $bootstrapNuGetProvider -and ($script:isNanoServer -or (IsCoreCLR) -or -not $BootstrapNuGetExe))
    {
        return
    }

    # We should prompt only once for bootstrapping the NuGet provider and/or NuGet.exe
    
    # Should continue message for bootstrapping only NuGet provider
    $shouldContinueQueryMessage = $LocalizedData.InstallNuGetProviderShouldContinueQuery -f @($script:NuGetProviderVersion,$script:NuGetBinaryProgramDataPath,$script:NuGetBinaryLocalAppDataPath)
    $shouldContinueCaption = $LocalizedData.InstallNuGetProviderShouldContinueCaption

    # Should continue message for bootstrapping both NuGet provider and NuGet.exe
    if($bootstrapNuGetProvider -and $BootstrapNuGetExe)
    {
        $shouldContinueQueryMessage = $LocalizedData.InstallNuGetBinariesShouldContinueQuery2 -f @($script:NuGetProviderVersion,$script:NuGetBinaryProgramDataPath,$script:NuGetBinaryLocalAppDataPath, $script:PSGetProgramDataPath, $script:PSGetAppLocalPath)
        $shouldContinueCaption = $LocalizedData.InstallNuGetBinariesShouldContinueCaption2
    }
    elseif($BootstrapNuGetExe)
    {
        # Should continue message for bootstrapping only NuGet.exe
        $shouldContinueQueryMessage = $LocalizedData.InstallNuGetExeShouldContinueQuery -f ($script:PSGetProgramDataPath, $script:PSGetAppLocalPath)
        $shouldContinueCaption = $LocalizedData.InstallNuGetExeShouldContinueCaption
    }

    $AdditionalParams = Get-ParametersHashtable -Proxy $Proxy -ProxyCredential $ProxyCredential

    if($Force -or $psCmdlet.ShouldContinue($shouldContinueQueryMessage, $shouldContinueCaption))
    {
        if($bootstrapNuGetProvider)
        {
            Write-Verbose -Message $LocalizedData.DownloadingNugetProvider

            $scope = 'CurrentUser'
            if(Test-RunningAsElevated)
            {
                $scope = 'AllUsers'
            }

            # Bootstrap the NuGet provider
            $null = PackageManagement\Install-PackageProvider -Name $script:NuGetProviderName `
                                                              -MinimumVersion $script:NuGetProviderVersion `
                                                              -Scope $scope `
                                                              -Force @AdditionalParams

            # Force import ensures that nuget provider with minimum version got loaded.
            $null = PackageManagement\Import-PackageProvider -Name $script:NuGetProviderName `
                                                             -MinimumVersion $script:NuGetProviderVersion `
                                                             -Force

            $nugetProvider = PackageManagement\Get-PackageProvider -Name $script:NuGetProviderName

            if ($nugetProvider)
            {
                $script:NuGetProvider = $nugetProvider
            }
        }

        if($BootstrapNuGetExe -and -not $script:isNanoServer -and -not (IsCoreCLR))
        {
            Write-Verbose -Message $LocalizedData.DownloadingNugetExe

            $nugetExeBasePath = $script:PSGetAppLocalPath

            # if the current process is running with elevated privileges, 
            # install NuGet.exe to $script:PSGetProgramDataPath
            if(Test-RunningAsElevated)
            {
                $nugetExeBasePath = $script:PSGetProgramDataPath
            }

            if(-not (Microsoft.PowerShell.Management\Test-Path -Path $nugetExeBasePath))
            {
                $null = Microsoft.PowerShell.Management\New-Item -Path $nugetExeBasePath `
                                                                 -ItemType Directory -Force `
                                                                 -ErrorAction SilentlyContinue `
                                                                 -WarningAction SilentlyContinue `
                                                                 -Confirm:$false -WhatIf:$false
            }

            $nugetExeFilePath = Microsoft.PowerShell.Management\Join-Path -Path $nugetExeBasePath -ChildPath $script:NuGetExeName

            # Download the NuGet.exe from http://nuget.org/NuGet.exe
            $null = Microsoft.PowerShell.Utility\Invoke-WebRequest -Uri $script:NuGetClientSourceURL `
                                                                   -OutFile $nugetExeFilePath `
                                                                   @AdditionalParams

            if (Microsoft.PowerShell.Management\Test-Path -Path $nugetExeFilePath)
            {
                $script:NuGetExePath = $nugetExeFilePath
            }
        }
    }

    $message = $null
    $errorId = $null
    $failedToBootstrapNuGetProvider = $false
    $failedToBootstrapNuGetExe = $false

    if($bootstrapNuGetProvider -and -not $script:NuGetProvider)
    {
        $failedToBootstrapNuGetProvider = $true

        $message = $LocalizedData.CouldNotInstallNuGetProvider -f @($script:NuGetProviderVersion)
        $errorId = 'CouldNotInstallNuGetProvider'
    }

    if($BootstrapNuGetExe -and 
       (-not $script:NuGetExePath -or 
        -not (Microsoft.PowerShell.Management\Test-Path -Path $script:NuGetExePath)))
    {
        $failedToBootstrapNuGetExe = $true

        $message = $LocalizedData.CouldNotInstallNuGetExe -f @($script:NuGetProviderVersion)
        $errorId = 'CouldNotInstallNuGetExe'
    }

    # Change the error id and message if both NuGet provider and NuGet.exe are not installed.
    if($failedToBootstrapNuGetProvider -and $failedToBootstrapNuGetExe)
    {
        $message = $LocalizedData.CouldNotInstallNuGetBinaries2 -f @($script:NuGetProviderVersion)
        $errorId = 'CouldNotInstallNuGetBinaries'
    }

    # Throw the error message if one of the above conditions are met
    if($message -and $errorId)
    {
        ThrowError -ExceptionName "System.InvalidOperationException" `
                    -ExceptionMessage $message `
                    -ErrorId $errorId `
                    -CallerPSCmdlet $CallerPSCmdlet `
                    -ErrorCategory InvalidOperation
    }
}

# Check if current user is running with elevated privileges
function Test-RunningAsElevated
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param()

    if(IsWindows)
    {
        $wid=[System.Security.Principal.WindowsIdentity]::GetCurrent()
        $prp=new-object System.Security.Principal.WindowsPrincipal($wid)
        $adm=[System.Security.Principal.WindowsBuiltInRole]::Administrator
        return $prp.IsInRole($adm)
    }
    elseif((IsLinux) -or (IsOSX))
    {
        # Permission models on *nix can be very complex, to the point that you could never possibly guess without simply trying what you need to try;
        # This is totally different from Windows where you can know what you can or cannot do with/without admin rights.
        return $true
    }

    return $false
}

function Get-EscapedString
{
    [CmdletBinding()]
    [OutputType([String])]
    Param
    (
        [Parameter()]
        [string]
        $ElementValue
    )

    return [System.Security.SecurityElement]::Escape($ElementValue)
}

function ValidateAndGet-ScriptDependencies
{
    param(
        [Parameter(Mandatory=$true)]
        [string]
        $Repository,

        [Parameter(Mandatory=$true)]
        [PSCustomObject]
        $DependentScriptInfo,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )

    $DependenciesDetails = @()

    # Validate dependent modules
    $RequiredModuleSpecification = $DependentScriptInfo.RequiredModules
    if($RequiredModuleSpecification)
    {
        ForEach($moduleSpecification in $RequiredModuleSpecification)
        {
            $ModuleName = $moduleSpecification.Name

            $FindModuleArguments = @{
                                        Repository = $Repository
                                        Verbose = $VerbosePreference
                                        ErrorAction = 'SilentlyContinue'
                                        WarningAction = 'SilentlyContinue'
                                        Debug = $DebugPreference
                                    }

            if($DependentScriptInfo.ExternalModuleDependencies -contains $ModuleName)
            {
                Write-Verbose -Message ($LocalizedData.SkippedModuleDependency -f $ModuleName)

                continue
            }

            $FindModuleArguments['Name'] = $ModuleName
            $ReqModuleInfo = @{}
            $ReqModuleInfo['Name'] = $ModuleName

            if($moduleSpecification.Version)
            {
                $FindModuleArguments['MinimumVersion'] = $moduleSpecification.Version
                $ReqModuleInfo['MinimumVersion'] = $moduleSpecification.Version
            }
            elseif((Get-Member -InputObject $moduleSpecification -Name RequiredVersion) -and $moduleSpecification.RequiredVersion)
            {            
                $FindModuleArguments['RequiredVersion'] = $moduleSpecification.RequiredVersion
                $ReqModuleInfo['RequiredVersion'] = $moduleSpecification.RequiredVersion
            }

            if((Get-Member -InputObject $moduleSpecification -Name MaximumVersion) -and $moduleSpecification.MaximumVersion)
            {
                # * can be specified in the MaximumVersion of a ModuleSpecification to convey that maximum possible value of that version part.
                # like 1.0.0.* --> 1.0.0.99999999
                # replace * with 99999999, PowerShell core takes care validating the * to be the last character in the version string.
                $maximumVersion = $moduleSpecification.MaximumVersion -replace '\*','99999999'
                $FindModuleArguments['MaximumVersion'] = $maximumVersion
                $ReqModuleInfo['MaximumVersion'] = $maximumVersion
            }

            $psgetItemInfo = Find-Module @FindModuleArguments  | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $ModuleName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if(-not $psgetItemInfo)
            {
                $message = $LocalizedData.UnableToResolveScriptDependency -f ('module', $ModuleName, $DependentScriptInfo.Name, $Repository, 'ExternalModuleDependencies')
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "UnableToResolveScriptDependency" `
                            -CallerPSCmdlet $CallerPSCmdlet `
                            -ErrorCategory InvalidOperation
            }

            $DependenciesDetails += $ReqModuleInfo
        }
    }

    # Validate dependent scrips
    $RequiredScripts = $DependentScriptInfo.RequiredScripts
    if($RequiredScripts)
    {
        ForEach($requiredScript in $RequiredScripts)
        {
            $FindScriptArguments = @{
                                        Repository = $Repository
                                        Verbose = $VerbosePreference
                                        ErrorAction = 'SilentlyContinue'
                                        WarningAction = 'SilentlyContinue'
                                        Debug = $DebugPreference
                                    }

            if($DependentScriptInfo.ExternalScriptDependencies -contains $requiredScript)
            {
                Write-Verbose -Message ($LocalizedData.SkippedScriptDependency -f $requiredScript)

                continue
            }

            $FindScriptArguments['Name'] = $requiredScript
            $ReqScriptInfo = @{}
            $ReqScriptInfo['Name'] = $requiredScript

            $psgetItemInfo = Find-Script @FindScriptArguments  | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $requiredScript} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if(-not $psgetItemInfo)
            {
                $message = $LocalizedData.UnableToResolveScriptDependency -f ('script', $requiredScript, $DependentScriptInfo.Name, $Repository, 'ExternalScriptDependencies')
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "UnableToResolveScriptDependency" `
                            -CallerPSCmdlet $CallerPSCmdlet `
                            -ErrorCategory InvalidOperation
            }

            $DependenciesDetails += $ReqScriptInfo
        }
    }

    return $DependenciesDetails
}

function ValidateAndGet-RequiredModuleDetails
{
    param(
        [Parameter()]
        $ModuleManifestRequiredModules,

        [Parameter()]
        [PSModuleInfo[]]
        $RequiredPSModuleInfos,

        [Parameter(Mandatory=$true)]
        [string]
        $Repository,

        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $DependentModuleInfo,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )

    $RequiredModuleDetails = @()

    if(-not $RequiredPSModuleInfos)
    {
        return $RequiredModuleDetails
    }

    if($ModuleManifestRequiredModules)
    {
        ForEach($RequiredModule in $ModuleManifestRequiredModules)
        {
            $ModuleName = $null
            $VersionString = $null

            $ReqModuleInfo = @{}

            $FindModuleArguments = @{
                                        Repository = $Repository
                                        Verbose = $VerbosePreference
                                        ErrorAction = 'SilentlyContinue'
                                        WarningAction = 'SilentlyContinue'
                                        Debug = $DebugPreference
                                    }

            # ModuleSpecification case
            if($RequiredModule.GetType().ToString() -eq 'System.Collections.Hashtable')
            {
                $ModuleName = $RequiredModule.ModuleName

                # Version format in NuSpec:
                # "[2.0]" --> (== 2.0) Required Version
                # "2.0" --> (>= 2.0) Minimum Version
                if($RequiredModule.Keys -Contains "RequiredVersion")
                {
                    $FindModuleArguments['RequiredVersion'] = $RequiredModule.RequiredVersion
                    $ReqModuleInfo['RequiredVersion'] = $RequiredModule.RequiredVersion
                }
                elseif($RequiredModule.Keys -Contains "ModuleVersion")
                {
                    $FindModuleArguments['MinimumVersion'] = $RequiredModule.ModuleVersion
                    $ReqModuleInfo['MinimumVersion'] = $RequiredModule.ModuleVersion
                }

                if($RequiredModule.Keys -Contains 'MaximumVersion' -and $RequiredModule.MaximumVersion)
                {
                    # * can be specified in the MaximumVersion of a ModuleSpecification to convey that maximum possible value of that version part.
                    # like 1.0.0.* --> 1.0.0.99999999
                    # replace * with 99999999, PowerShell core takes care validating the * to be the last character in the version string.
                    $maximumVersion = $RequiredModule.MaximumVersion -replace '\*','99999999'

                    $FindModuleArguments['MaximumVersion'] = $maximumVersion
                    $ReqModuleInfo['MaximumVersion'] = $maximumVersion
                }
            }
            else
            {
                # Just module name was specified
                $ModuleName = $RequiredModule.ToString()
            }
            
            if((Get-ExternalModuleDependencies -PSModuleInfo $DependentModuleInfo) -contains $ModuleName)
            {
                Write-Verbose -Message ($LocalizedData.SkippedModuleDependency -f $ModuleName)

                continue
            }      

            # Skip this module name if it's name is not in $RequiredPSModuleInfos.
            # This is required when a ModuleName is part of the NestedModules list of the actual module.
            # $ModuleName is packaged as part of the actual module When $RequiredPSModuleInfos doesn't contain it's name.
            if($RequiredPSModuleInfos.Name -notcontains $ModuleName)
            {
                continue
            }

            $ReqModuleInfo['Name'] = $ModuleName

            # Add the dependency only if the module is available on the gallery
            # Otherwise Module installation will fail as all required modules need to be available on 
            # the same Repository
            $FindModuleArguments['Name'] = $ModuleName

            $psgetItemInfo = Find-Module @FindModuleArguments  | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $ModuleName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if(-not $psgetItemInfo)
            {
                $message = $LocalizedData.UnableToResolveModuleDependency -f ($ModuleName, $DependentModuleInfo.Name, $Repository, $ModuleName, $Repository, $ModuleName, $ModuleName)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "UnableToResolveModuleDependency" `
                            -CallerPSCmdlet $CallerPSCmdlet `
                            -ErrorCategory InvalidOperation
            }

            $RequiredModuleDetails += $ReqModuleInfo
        }
    }
    else
    {
        # If Import-LocalizedData cmdlet was failed to read the .psd1 contents 
        # use provided $RequiredPSModuleInfos (PSModuleInfo.RequiredModules or PSModuleInfo.NestedModules of the actual dependent module)

        $FindModuleArguments = @{
                                    Repository = $Repository
                                    Verbose = $VerbosePreference
                                    ErrorAction = 'SilentlyContinue'
                                    WarningAction = 'SilentlyContinue'
                                    Debug = $DebugPreference
                                }

        ForEach($RequiredModuleInfo in $RequiredPSModuleInfos)
        {
            $ModuleName = $requiredModuleInfo.Name

            if((Get-ExternalModuleDependencies -PSModuleInfo $DependentModuleInfo) -contains $ModuleName)
            {
                Write-Verbose -Message ($LocalizedData.SkippedModuleDependency -f $ModuleName)

                continue
            }

            $FindModuleArguments['Name'] = $ModuleName
            $FindModuleArguments['MinimumVersion'] = $requiredModuleInfo.Version

            $psgetItemInfo = Find-Module @FindModuleArguments  | 
                                        Microsoft.PowerShell.Core\Where-Object {$_.Name -eq $ModuleName} | 
                                            Microsoft.PowerShell.Utility\Select-Object -Last 1 -ErrorAction Ignore

            if(-not $psgetItemInfo)
            {
                $message = $LocalizedData.UnableToResolveModuleDependency -f ($ModuleName, $DependentModuleInfo.Name, $Repository, $ModuleName, $Repository, $ModuleName, $ModuleName)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "UnableToResolveModuleDependency" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation
            }

            $RequiredModuleDetails += @{
                                            Name=$_.Name
                                            MinimumVersion=$_.Version
                                       }
        }
    }

    return $RequiredModuleDetails
}

function Get-ExternalModuleDependencies
{
    Param (
        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $PSModuleInfo
    )

    if($PSModuleInfo.PrivateData -and 
       ($PSModuleInfo.PrivateData.GetType().ToString() -eq "System.Collections.Hashtable") -and 
       $PSModuleInfo.PrivateData["PSData"] -and
       ($PSModuleInfo.PrivateData["PSData"].GetType().ToString() -eq "System.Collections.Hashtable") -and
       $PSModuleInfo.PrivateData.PSData['ExternalModuleDependencies'] -and
       ($PSModuleInfo.PrivateData.PSData['ExternalModuleDependencies'].GetType().ToString() -eq "System.Object[]")
    )
    {
        return $PSModuleInfo.PrivateData.PSData.ExternalModuleDependencies        
    }
}

function Get-ModuleDependencies
{
    Param (
        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $PSModuleInfo,

        [Parameter(Mandatory=$true)]
        [string]
        $Repository,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )

    $DependentModuleDetails = @()

    if($PSModuleInfo.RequiredModules -or $PSModuleInfo.NestedModules)
    {
        # PSModuleInfo.RequiredModules doesn't provide the RequiredVersion info from the ModuleSpecification
        # Reading the contents of module manifest file
        # to get the RequiredVersion details.
        $ModuleManifestHashTable = Get-ManifestHashTable -Path $PSModuleInfo.Path

        if($PSModuleInfo.RequiredModules)
        {
            $ModuleManifestRequiredModules = $null

            if($ModuleManifestHashTable)
            {
                $ModuleManifestRequiredModules = $ModuleManifestHashTable.RequiredModules
            }
           

            $DependentModuleDetails += ValidateAndGet-RequiredModuleDetails -ModuleManifestRequiredModules $ModuleManifestRequiredModules `
                                                                            -RequiredPSModuleInfos $PSModuleInfo.RequiredModules `
                                                                            -Repository $Repository `
                                                                            -DependentModuleInfo $PSModuleInfo `
                                                                            -CallerPSCmdlet $CallerPSCmdlet `
                                                                            -Verbose:$VerbosePreference `
                                                                            -Debug:$DebugPreference 
        }

        if($PSModuleInfo.NestedModules)
        {
            $ModuleManifestRequiredModules = $null

            if($ModuleManifestHashTable)
            {
                $ModuleManifestRequiredModules = $ModuleManifestHashTable.NestedModules
            }
           
            # A nested module is be considered as a dependency 
            # 1) whose module base is not under the specified module base OR 
            # 2) whose module base is under the specified module base and it's path doesn't exists
            #
            $RequiredPSModuleInfos = $PSModuleInfo.NestedModules | Microsoft.PowerShell.Core\Where-Object {
                        -not $_.ModuleBase.StartsWith($PSModuleInfo.ModuleBase, [System.StringComparison]::OrdinalIgnoreCase) -or
                        -not $_.Path -or 
                        -not (Microsoft.PowerShell.Management\Test-Path -LiteralPath $_.Path)
                    }

            $DependentModuleDetails += ValidateAndGet-RequiredModuleDetails -ModuleManifestRequiredModules $ModuleManifestRequiredModules `
                                                                            -RequiredPSModuleInfos $RequiredPSModuleInfos `
                                                                            -Repository $Repository `
                                                                            -DependentModuleInfo $PSModuleInfo `
                                                                            -CallerPSCmdlet $CallerPSCmdlet `
                                                                            -Verbose:$VerbosePreference `
                                                                            -Debug:$DebugPreference 
        }
    }

    return $DependentModuleDetails
}

function Publish-PSArtifactUtility
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true, ParameterSetName='PublishModule')]
        [ValidateNotNullOrEmpty()]
        [PSModuleInfo]
        $PSModuleInfo,

        [Parameter(Mandatory=$true, ParameterSetName='PublishScript')]
        [ValidateNotNullOrEmpty()]
        [PSCustomObject]
        $PSScriptInfo,

        [Parameter(Mandatory=$true, ParameterSetName='PublishModule')]
        [ValidateNotNullOrEmpty()]
        [string]
        $ManifestPath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Destination,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Repository,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $NugetApiKey,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $NugetPackageRoot,

        [Parameter(ParameterSetName='PublishModule')] 
        [Version]
        $FormatVersion,

        [Parameter(ParameterSetName='PublishModule')]
        [string]
        $ReleaseNotes,

        [Parameter(ParameterSetName='PublishModule')]
        [string[]]
        $Tags,
        
        [Parameter(ParameterSetName='PublishModule')]
        [Uri]
        $LicenseUri,

        [Parameter(ParameterSetName='PublishModule')]
        [Uri]
        $IconUri,
        
        [Parameter(ParameterSetName='PublishModule')]
        [Uri]
        $ProjectUri
    )

    Install-NuGetClientBinaries -CallerPSCmdlet $PSCmdlet -BootstrapNuGetExe

    $PSArtifactType = $script:PSArtifactTypeModule
    $Name = $null
    $Description = $null
    $Version = $null
    $Author = $null
    $CompanyName = $null
    $Copyright = $null

    if($PSModuleInfo)
    {
        $Name = $PSModuleInfo.Name
        $Description = $PSModuleInfo.Description
        $Version = $PSModuleInfo.Version
        $Author = $PSModuleInfo.Author
        $CompanyName = $PSModuleInfo.CompanyName
        $Copyright = $PSModuleInfo.Copyright

        if($PSModuleInfo.PrivateData -and 
           ($PSModuleInfo.PrivateData.GetType().ToString() -eq "System.Collections.Hashtable") -and 
           $PSModuleInfo.PrivateData["PSData"] -and
           ($PSModuleInfo.PrivateData["PSData"].GetType().ToString() -eq "System.Collections.Hashtable")
           )
        {
            if( -not $Tags -and $PSModuleInfo.PrivateData.PSData["Tags"])
            { 
                $Tags = $PSModuleInfo.PrivateData.PSData.Tags
            }

            if( -not $ReleaseNotes -and $PSModuleInfo.PrivateData.PSData["ReleaseNotes"])
            { 
                $ReleaseNotes = $PSModuleInfo.PrivateData.PSData.ReleaseNotes
            }

            if( -not $LicenseUri -and $PSModuleInfo.PrivateData.PSData["LicenseUri"])
            { 
                $LicenseUri = $PSModuleInfo.PrivateData.PSData.LicenseUri
            }

            if( -not $IconUri -and $PSModuleInfo.PrivateData.PSData["IconUri"])
            { 
                $IconUri = $PSModuleInfo.PrivateData.PSData.IconUri
            }

            if( -not $ProjectUri -and $PSModuleInfo.PrivateData.PSData["ProjectUri"])
            { 
                $ProjectUri = $PSModuleInfo.PrivateData.PSData.ProjectUri
            }
        }
    }
    else
    {
        $PSArtifactType = $script:PSArtifactTypeScript

        $Name = $PSScriptInfo.Name
        $Description = $PSScriptInfo.Description
        $Version = $PSScriptInfo.Version        
        $Author = $PSScriptInfo.Author
        $CompanyName = $PSScriptInfo.CompanyName
        $Copyright = $PSScriptInfo.Copyright

        if($PSScriptInfo.'Tags')
        { 
            $Tags = $PSScriptInfo.Tags
        }

        if($PSScriptInfo.'ReleaseNotes')
        { 
            $ReleaseNotes = $PSScriptInfo.ReleaseNotes
        }

        if($PSScriptInfo.'LicenseUri')
        { 
            $LicenseUri = $PSScriptInfo.LicenseUri
        }

        if($PSScriptInfo.'IconUri')
        { 
            $IconUri = $PSScriptInfo.IconUri
        }

        if($PSScriptInfo.'ProjectUri')
        { 
            $ProjectUri = $PSScriptInfo.ProjectUri
        }
    }


    # Add PSModule and PSGet format version tags
    if(-not $Tags)
    {
        $Tags = @()
    }
    
    if($FormatVersion)
    {
        $Tags += "$($script:PSGetFormatVersion)_$FormatVersion"
    }

    $DependentModuleDetails = @()

    if($PSScriptInfo)
    {        
        $Tags += "PSScript"

        if($PSScriptInfo.DefinedCommands)
        {
            if($PSScriptInfo.DefinedFunctions)
            {
                $Tags += "$($script:Includes)_Function"
                $Tags += $PSScriptInfo.DefinedFunctions | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Function)_$_" }
            }

            if($PSScriptInfo.DefinedWorkflows)
            {
                $Tags += "$($script:Includes)_Workflow"
                $Tags += $PSScriptInfo.DefinedWorkflows | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Workflow)_$_" }
            }

            $Tags += $PSScriptInfo.DefinedCommands | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Command)_$_" }
        }

        # Populate the dependencies elements from RequiredModules and RequiredScripts
        # 
        $DependentModuleDetails += ValidateAndGet-ScriptDependencies -Repository $Repository `
                                                                     -DependentScriptInfo $PSScriptInfo `
                                                                     -CallerPSCmdlet $PSCmdlet `
                                                                     -Verbose:$VerbosePreference `
                                                                     -Debug:$DebugPreference
    }
    else
    {
        $Tags += "PSModule"

        $ModuleManifestHashTable = Get-ManifestHashTable -Path $ManifestPath

        if($PSModuleInfo.ExportedCommands.Count)
        {
            if($PSModuleInfo.ExportedCmdlets.Count)
            {
                $Tags += "$($script:Includes)_Cmdlet"
                $Tags += $PSModuleInfo.ExportedCmdlets.Keys | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Cmdlet)_$_" }

                #if CmdletsToExport field in manifest file is "*", we suggest the user to include all those cmdlets for best practice
                if($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey('CmdletsToExport') -and ($ModuleManifestHashTable.CmdletsToExport -eq "*"))
                {
                    $WarningMessage = $LocalizedData.ShouldIncludeCmdletsToExport -f ($ManifestPath)
                    Write-Warning -Message $WarningMessage
                }
            }

            if($PSModuleInfo.ExportedFunctions.Count)
            {
                $Tags += "$($script:Includes)_Function"
                $Tags += $PSModuleInfo.ExportedFunctions.Keys | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Function)_$_" }

                if($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey('FunctionsToExport') -and ($ModuleManifestHashTable.FunctionsToExport -eq "*"))
                {
                    $WarningMessage = $LocalizedData.ShouldIncludeFunctionsToExport -f ($ManifestPath)
                    Write-Warning -Message $WarningMessage
                }
            }

            $Tags += $PSModuleInfo.ExportedCommands.Keys | Microsoft.PowerShell.Core\ForEach-Object { "$($script:Command)_$_" }
        }

        $dscResourceNames = Get-ExportedDscResources -PSModuleInfo $PSModuleInfo 
        if($dscResourceNames)
        {
            $Tags += "$($script:Includes)_DscResource"

            $Tags += $dscResourceNames | Microsoft.PowerShell.Core\ForEach-Object { "$($script:DscResource)_$_" }

            #If DscResourcesToExport is commented out or "*" is used, we will write-warning
            if($ModuleManifestHashTable -and 
                ($ModuleManifestHashTable.ContainsKey("DscResourcesToExport") -and 
                $ModuleManifestHashTable.DscResourcesToExport -eq "*") -or 
                -not $ModuleManifestHashTable.ContainsKey("DscResourcesToExport"))
            {
                $WarningMessage = $LocalizedData.ShouldIncludeDscResourcesToExport -f ($ManifestPath)
                Write-Warning -Message $WarningMessage
            }
        }

        $RoleCapabilityNames = Get-AvailableRoleCapabilityName -PSModuleInfo $PSModuleInfo
        if($RoleCapabilityNames)
        {
            $Tags += "$($script:Includes)_RoleCapability"

            $Tags += $RoleCapabilityNames | Microsoft.PowerShell.Core\ForEach-Object { "$($script:RoleCapability)_$_" }
        }

        # Populate the module dependencies elements from RequiredModules and 
        # NestedModules properties of the current PSModuleInfo
        $DependentModuleDetails = Get-ModuleDependencies -PSModuleInfo $PSModuleInfo `
                                                         -Repository $Repository `
                                                         -CallerPSCmdlet $PSCmdlet `
                                                         -Verbose:$VerbosePreference `
                                                         -Debug:$DebugPreference 
    }
    
    $dependencies = @()
    ForEach($Dependency in $DependentModuleDetails)
    {    
        $ModuleName = $Dependency.Name
        $VersionString = $null

        # Version format in NuSpec:
        # "[2.0]" --> (== 2.0) Required Version
        # "2.0" --> (>= 2.0) Minimum Version
        # 
        # When only MaximumVersion is specified in the ModuleSpecification
        # (,1.0]  = x <= 1.0
        #
        # When both Minimum and Maximum versions are specified in the ModuleSpecification
        # [1.0,2.0] = 1.0 <= x <= 2.0

        if($Dependency.Keys -Contains "RequiredVersion")
        {
            $VersionString = "[$($Dependency.RequiredVersion)]"
        }
        elseif($Dependency.Keys -Contains 'MinimumVersion' -and $Dependency.Keys -Contains 'MaximumVersion')
        {
            $VersionString = "[$($Dependency.MinimumVersion),$($Dependency.MaximumVersion)]"
        }
        elseif($Dependency.Keys -Contains 'MaximumVersion')
        {
            $VersionString = "(,$($Dependency.MaximumVersion)]"
        }
        elseif($Dependency.Keys -Contains 'MinimumVersion')
        {
            $VersionString = "$($Dependency.MinimumVersion)"
        }

        $dependencies += "<dependency id='$($ModuleName)' version='$($VersionString)' />"
    }
    
    # Populate the nuspec elements
    $nuspec = @"
<?xml version="1.0"?>
<package >
    <metadata>
        <id>$(Get-EscapedString -ElementValue "$Name")</id>
        <version>$($Version)</version>
        <authors>$(Get-EscapedString -ElementValue "$Author")</authors>
        <owners>$(Get-EscapedString -ElementValue "$CompanyName")</owners>
        <description>$(Get-EscapedString -ElementValue "$Description")</description>
        <releaseNotes>$(Get-EscapedString -ElementValue "$ReleaseNotes")</releaseNotes>
        <copyright>$(Get-EscapedString -ElementValue "$Copyright")</copyright>
        <tags>$(if($Tags){ Get-EscapedString -ElementValue ($Tags -join ' ')})</tags>
        $(if($LicenseUri){
        "<licenseUrl>$(Get-EscapedString -ElementValue "$LicenseUri")</licenseUrl>
        <requireLicenseAcceptance>true</requireLicenseAcceptance>"
        })
        $(if($ProjectUri){
        "<projectUrl>$(Get-EscapedString -ElementValue "$ProjectUri")</projectUrl>"
        })
        $(if($IconUri){
        "<iconUrl>$(Get-EscapedString -ElementValue "$IconUri")</iconUrl>"
        })
        <dependencies>
            $dependencies
        </dependencies>
    </metadata>
</package>
"@

    $NupkgPath = "$NugetPackageRoot\$Name.$($Version.ToString()).nupkg"
    $NuspecPath = "$NugetPackageRoot\$Name.nuspec"
    $tempErrorFile = $null
    $tempOutputFile = $null

    try
    {        
        # Remove existing nuspec and nupkg files
        Microsoft.PowerShell.Management\Remove-Item $NupkgPath  -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        Microsoft.PowerShell.Management\Remove-Item $NuspecPath -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
            
        Microsoft.PowerShell.Management\Set-Content -Value $nuspec -Path $NuspecPath -Force -Confirm:$false -WhatIf:$false

        # Create .nupkg file
        $output = & $script:NuGetExePath pack $NuspecPath -OutputDirectory $NugetPackageRoot
        if($LASTEXITCODE)
        {
            if($PSArtifactType -eq $script:PSArtifactTypeModule)
            {
                $message = $LocalizedData.FailedToCreateCompressedModule -f ($output)
                $errorId = "FailedToCreateCompressedModule"
            }
            else
            {
                $message = $LocalizedData.FailedToCreateCompressedScript -f ($output)
                $errorId = "FailedToCreateCompressedScript"
            }

            Write-Error -Message $message -ErrorId $errorId -Category InvalidOperation
            return
        }

        # Publish the .nupkg to gallery
        $tempErrorFile = Microsoft.PowerShell.Management\Join-Path -Path $nugetPackageRoot -ChildPath "TempPublishError.txt"
        $tempOutputFile = Microsoft.PowerShell.Management\Join-Path -Path $nugetPackageRoot -ChildPath "TempPublishOutput.txt"
        
        Microsoft.PowerShell.Management\Start-Process -FilePath "$script:NuGetExePath" `
                                                      -ArgumentList @('push', "`"$NupkgPath`"", '-source', "`"$($Destination.TrimEnd('\'))`"", '-NonInteractive', '-ApiKey', "`"$NugetApiKey`"") `
                                                      -RedirectStandardError $tempErrorFile `
                                                      -RedirectStandardOutput $tempOutputFile `
                                                      -NoNewWindow `
                                                      -Wait

        $errorMsg = Microsoft.PowerShell.Management\Get-Content -Path $tempErrorFile -Raw

        if($errorMsg)
        {
            if(($NugetApiKey -eq 'VSTS') -and 
               ($errorMsg -match 'Cannot prompt for input in non-interactive mode.') )
            {
                $errorMsg = $LocalizedData.RegisterVSTSFeedAsNuGetPackageSource -f ($Destination, $script:VSTSAuthenticatedFeedsDocUrl)
            }

            if($PSArtifactType -eq $script:PSArtifactTypeModule)
            {
                $message = $LocalizedData.FailedToPublish -f ($Name,$errorMsg)
                $errorId = "FailedToPublishTheModule"
            }
            else
            {
                $message = $LocalizedData.FailedToPublishScript -f ($Name,$errorMsg)
                $errorId = "FailedToPublishTheScript"
            }

            Write-Error -Message $message -ErrorId $errorId -Category InvalidOperation
        }
        else
        {
            if($PSArtifactType -eq $script:PSArtifactTypeModule)
            {
                $message = $LocalizedData.PublishedSuccessfully -f ($Name, $Destination, $Name)
            }
            else
            {
                $message = $LocalizedData.PublishedScriptSuccessfully -f ($Name, $Destination, $Name)
            }

            Write-Verbose -Message $message
        }
    }
    finally
    {
        if($NupkgPath -and (Test-Path -Path $NupkgPath -PathType Leaf))
        {
            Microsoft.PowerShell.Management\Remove-Item $NupkgPath  -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }

        if($NuspecPath -and (Test-Path -Path $NuspecPath -PathType Leaf))
        {
            Microsoft.PowerShell.Management\Remove-Item $NuspecPath -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }
        
        if($tempErrorFile -and (Test-Path -Path $tempErrorFile -PathType Leaf))
        {
            Microsoft.PowerShell.Management\Remove-Item $tempErrorFile -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }
        
        if($tempOutputFile -and (Test-Path -Path $tempOutputFile -PathType Leaf))
        {
            Microsoft.PowerShell.Management\Remove-Item $tempOutputFile -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false        
        }
    }
}

function ValidateAndAdd-PSScriptInfoEntry
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]
        $PSScriptInfo,
        
        [Parameter(Mandatory=$true)]
        [string]
        $PropertyName,

        [Parameter()]
        $PropertyValue,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )
    
    $Value = $PropertyValue
    $KeyName = $PropertyName

    # return if $KeyName value is not null in $PSScriptInfo
    if(-not $value -or -not $KeyName -or (Get-Member -InputObject $PSScriptInfo -Name $KeyName) -and $PSScriptInfo."$KeyName")
    {
        return
    }

    switch($PropertyName)
    {
        # Validate the property value and also use proper key name as users can specify the property name in any case.
        $script:Version {
                            $KeyName = $script:Version

                            [Version]$Version = $null

                            if([System.Version]::TryParse($Value, ([ref]$Version)))
                            {
                                $Value = $Version                            
                            }
                            else
                            {
                                $message = $LocalizedData.InvalidVersion -f ($Value)
                                ThrowError -ExceptionName "System.ArgumentException" `
                                            -ExceptionMessage $message `
                                            -ErrorId "InvalidVersion" `
                                            -CallerPSCmdlet $CallerPSCmdlet `
                                            -ErrorCategory InvalidArgument `
                                            -ExceptionObject $Value
                                return
                            }
                            break
                        }

        $script:Author  { $KeyName = $script:Author }

        $script:Guid  {
                        $KeyName = $script:Guid

                        [Guid]$guid = [System.Guid]::Empty
                        if([System.Guid]::TryParse($Value, ([ref]$guid)))
                        {
                            $Value = $guid                            
                        }
                        else
                        {
                            $message = $LocalizedData.InvalidGuid -f ($Value)
                            ThrowError -ExceptionName 'System.ArgumentException' `
                                       -ExceptionMessage $message `
                                       -ErrorId 'InvalidGuid' `
                                       -CallerPSCmdlet $CallerPSCmdlet `
                                       -ErrorCategory InvalidArgument `
                                       -ExceptionObject $Value
                            return
                        }

                        break
                     }

        $script:Description { $KeyName = $script:Description }

        $script:CompanyName { $KeyName = $script:CompanyName }

        $script:Copyright { $KeyName = $script:Copyright }

        $script:Tags {
                        $KeyName = $script:Tags
                        $Value = $Value -split '[,\s+]' | Microsoft.PowerShell.Core\Where-Object {$_}
                        break
                     }

        $script:LicenseUri {
                                $KeyName = $script:LicenseUri
                                if(-not (Test-WebUri -Uri $Value))
                                {
                                    $message = $LocalizedData.InvalidWebUri -f ($LicenseUri, "LicenseUri")
                                    ThrowError -ExceptionName "System.ArgumentException" `
                                                -ExceptionMessage $message `
                                                -ErrorId "InvalidWebUri" `
                                                -CallerPSCmdlet $CallerPSCmdlet `
                                                -ErrorCategory InvalidArgument `
                                                -ExceptionObject $Value
                                    return
                                }

                                $Value = [Uri]$Value
                           }

        $script:ProjectUri {
                                $KeyName = $script:ProjectUri
                                if(-not (Test-WebUri -Uri $Value))
                                {
                                    $message = $LocalizedData.InvalidWebUri -f ($ProjectUri, "ProjectUri")
                                    ThrowError -ExceptionName "System.ArgumentException" `
                                                -ExceptionMessage $message `
                                                -ErrorId "InvalidWebUri" `
                                                -CallerPSCmdlet $CallerPSCmdlet `
                                                -ErrorCategory InvalidArgument `
                                                -ExceptionObject $Value
                                    return
                                }

                                $Value = [Uri]$Value
                           }

        $script:IconUri {
                            $KeyName = $script:IconUri
                            if(-not (Test-WebUri -Uri $Value))
                            {
                                $message = $LocalizedData.InvalidWebUri -f ($IconUri, "IconUri")
                                ThrowError -ExceptionName "System.ArgumentException" `
                                            -ExceptionMessage $message `
                                            -ErrorId "InvalidWebUri" `
                                            -CallerPSCmdlet $CallerPSCmdlet `
                                            -ErrorCategory InvalidArgument `
                                            -ExceptionObject $Value
                                return
                            }

                            $Value = [Uri]$Value
                        }

        $script:ExternalModuleDependencies {
                                               $KeyName = $script:ExternalModuleDependencies
                                               $Value = $Value -split '[,\s+]' | Microsoft.PowerShell.Core\Where-Object {$_}
                                           }

        $script:ReleaseNotes { $KeyName = $script:ReleaseNotes }

        $script:RequiredModules { $KeyName = $script:RequiredModules }

        $script:RequiredScripts { 
                                    $KeyName = $script:RequiredScripts
                                    $Value = $Value -split '[,\s+]' | Microsoft.PowerShell.Core\Where-Object {$_}
                                }

        $script:ExternalScriptDependencies { 
                                               $KeyName = $script:ExternalScriptDependencies
                                               $Value = $Value -split '[,\s+]' | Microsoft.PowerShell.Core\Where-Object {$_}
                                           }

        $script:DefinedCommands  { $KeyName = $script:DefinedCommands }

        $script:DefinedFunctions { $KeyName = $script:DefinedFunctions }

        $script:DefinedWorkflows { $KeyName = $script:DefinedWorkflows }
    }

    Microsoft.PowerShell.Utility\Add-Member -InputObject $PSScriptInfo `
                                            -MemberType NoteProperty `
                                            -Name $KeyName `
                                            -Value $Value `
                                            -Force
}

function Get-ExportedDscResources
{
    [CmdletBinding(PositionalBinding=$false)]
    Param 
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [PSModuleInfo]
        $PSModuleInfo
    )

    $dscResources = @()

    if(Get-Command -Name Get-DscResource -Module PSDesiredStateConfiguration -ErrorAction SilentlyContinue)
    {
        $OldPSModulePath = $env:PSModulePath

        try
        {
            $env:PSModulePath = Join-Path -Path $PSHOME -ChildPath "Modules"
            $env:PSModulePath = "$env:PSModulePath;$(Split-Path -Path $PSModuleInfo.ModuleBase -Parent)"

            $dscResources = PSDesiredStateConfiguration\Get-DscResource -ErrorAction SilentlyContinue -WarningAction SilentlyContinue | 
                                Microsoft.PowerShell.Core\ForEach-Object {
                                    if($_.Module -and ($_.Module.Name -eq $PSModuleInfo.Name))
                                    {
                                        $_.Name
                                    }
                                }
        }
        finally
        {
            $env:PSModulePath = $OldPSModulePath
        }
    }
    else
    {
        $dscResourcesDir = Microsoft.PowerShell.Management\Join-Path -Path $PSModuleInfo.ModuleBase -ChildPath "DscResources"
        if(Microsoft.PowerShell.Management\Test-Path $dscResourcesDir)
        {
            $dscResources = Microsoft.PowerShell.Management\Get-ChildItem -Path $dscResourcesDir -Directory -Name
        }
    }

    return $dscResources
}

function Get-AvailableRoleCapabilityName
{
    [CmdletBinding(PositionalBinding=$false)]
    Param 
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [PSModuleInfo]
        $PSModuleInfo
    )

    $RoleCapabilityNames = @()

    $RoleCapabilitiesDir = Microsoft.PowerShell.Management\Join-Path -Path $PSModuleInfo.ModuleBase -ChildPath 'RoleCapabilities'
    if(Microsoft.PowerShell.Management\Test-Path -Path $RoleCapabilitiesDir -PathType Container)
    {
        $RoleCapabilityNames = Microsoft.PowerShell.Management\Get-ChildItem -Path $RoleCapabilitiesDir `
                                  -Name -Filter *.psrc |
                                      ForEach-Object {[System.IO.Path]::GetFileNameWithoutExtension($_)}
    }

    return $RoleCapabilityNames
}

function Get-LocationString
{
    [CmdletBinding(PositionalBinding=$false)]
    Param 
    (
        [Parameter()]
        [Uri]
        $LocationUri
    )

    $LocationString = $null

    if($LocationUri)
    {
        if($LocationUri.Scheme -eq 'file')
        {
            $LocationString = $LocationUri.OriginalString
        }
        elseif($LocationUri.AbsoluteUri)
        {
            $LocationString = $LocationUri.AbsoluteUri
        }
        else
        {
            $LocationString = $LocationUri.ToString()
        }
    }

    return $LocationString
}

#endregion Utility functions

#region PowerShellGet Provider APIs Implementation
function Get-PackageProviderName
{ 
    return $script:PSModuleProviderName
}

function Get-Feature
{
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Get-Feature'))
    Write-Output -InputObject (New-Feature $script:SupportsPSModulesFeatureName )
}

function Initialize-Provider
{
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Initialize-Provider'))
}

function Get-DynamicOptions
{
    param
    (
        [Microsoft.PackageManagement.MetaProvider.PowerShell.OptionCategory] 
        $category
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Get-DynamicOptions'))

    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:PackageManagementProviderParam -ExpectedType String -IsRequired $false)

    switch($category)
    {
        Package {
                    Write-Output -InputObject (New-DynamicOption -Category $category `
                                                                 -Name $script:PSArtifactType `
                                                                 -ExpectedType String `
                                                                 -IsRequired $false `
                                                                 -PermittedValues @($script:PSArtifactTypeModule,$script:PSArtifactTypeScript, $script:All))
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:Filter -ExpectedType String -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:Tag -ExpectedType StringArray -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name Includes -ExpectedType StringArray -IsRequired $false -PermittedValues $script:IncludeValidSet)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name DscResource -ExpectedType StringArray -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name RoleCapability -ExpectedType StringArray -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name Command -ExpectedType StringArray -IsRequired $false)
                }

        Source  {
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:PublishLocation -ExpectedType String -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:ScriptSourceLocation -ExpectedType String -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name $script:ScriptPublishLocation -ExpectedType String -IsRequired $false)
                }

        Install 
                {
                    Write-Output -InputObject (New-DynamicOption -Category $category `
                                                                 -Name $script:PSArtifactType `
                                                                 -ExpectedType String `
                                                                 -IsRequired $false `
                                                                 -PermittedValues @($script:PSArtifactTypeModule,$script:PSArtifactTypeScript, $script:All))
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name "Scope" -ExpectedType String -IsRequired $false -PermittedValues @("CurrentUser","AllUsers"))
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name 'AllowClobber' -ExpectedType Switch -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name 'SkipPublisherCheck' -ExpectedType Switch -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name "InstallUpdate" -ExpectedType Switch -IsRequired $false)
                    Write-Output -InputObject (New-DynamicOption -Category $category -Name 'NoPathUpdate' -ExpectedType Switch -IsRequired $false)
                }
    }
}

function Add-PackageSource
{
    [CmdletBinding()]
    param
    (
        [string]
        $Name,
         
        [string]
        $Location,

        [bool]
        $Trusted
    )
     
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Add-PackageSource'))

    if(-not $Name)
    {
        return
    }
    
    $Credential = $request.Credential

    $IsNewModuleSource = $false
    $Options = $request.Options

    foreach( $o in $Options.Keys )
    {
        Write-Debug ( "OPTION: {0} => {1}" -f ($o, $Options[$o]) )
    }

    $Proxy = $null
    if($Options.ContainsKey($script:Proxy))
    {
        $Proxy = $Options[$script:Proxy]

        if(-not (Test-WebUri -Uri $Proxy))
        {
            $message = $LocalizedData.InvalidWebUri -f ($Proxy, $script:Proxy)
            ThrowError -ExceptionName 'System.ArgumentException' `
                        -ExceptionMessage $message `
                        -ErrorId 'InvalidWebUri' `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $Proxy
        }
    }

    $ProxyCredential = $null
    if($Options.ContainsKey($script:ProxyCredential))
    {
        $ProxyCredential = $Options[$script:ProxyCredential]
    }

    Set-ModuleSourcesVariable -Force -Proxy $Proxy -ProxyCredential $ProxyCredential

    if($Options.ContainsKey('IsNewModuleSource'))
    {
        $IsNewModuleSource = $Options['IsNewModuleSource']

        if($IsNewModuleSource.GetType().ToString() -eq 'System.String')
        {
            if($IsNewModuleSource -eq 'false')
            {
                $IsNewModuleSource = $false
            }
            elseif($IsNewModuleSource -eq 'true')
            {
                $IsNewModuleSource = $true
            }
        }
    }

    $IsUpdatePackageSource = $false
    if($Options.ContainsKey('IsUpdatePackageSource'))
    {
        $IsUpdatePackageSource = $Options['IsUpdatePackageSource']

        if($IsUpdatePackageSource.GetType().ToString() -eq 'System.String')
        {
            if($IsUpdatePackageSource -eq 'false')
            {
                $IsUpdatePackageSource = $false
            }
            elseif($IsUpdatePackageSource -eq 'true')
            {
                $IsUpdatePackageSource = $true
            }
        }
    }

    $PublishLocation = $null
    if($Options.ContainsKey($script:PublishLocation))
    {
        if($Name -eq $Script:PSGalleryModuleSource)
        {
            $message = $LocalizedData.ParameterIsNotAllowedWithPSGallery -f ('PublishLocation')
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId 'ParameterIsNotAllowedWithPSGallery' `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $PublishLocation
        }

        $PublishLocation = $Options[$script:PublishLocation]

        if(-not (Microsoft.PowerShell.Management\Test-Path $PublishLocation) -and
           -not (Test-WebUri -uri $PublishLocation))
        {
            $PublishLocationUri = [Uri]$PublishLocation
            if($PublishLocationUri.Scheme -eq 'file')
            {
                $message = $LocalizedData.PathNotFound -f ($PublishLocation)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "PathNotFound" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $PublishLocation
            }
            else
            {
                $message = $LocalizedData.InvalidWebUri -f ($PublishLocation, "PublishLocation")
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidWebUri" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $PublishLocation
            }            
        }
    }

    $ScriptSourceLocation = $null
    if($Options.ContainsKey($script:ScriptSourceLocation))
    {
        if($Name -eq $Script:PSGalleryModuleSource)
        {
            $message = $LocalizedData.ParameterIsNotAllowedWithPSGallery -f ('ScriptSourceLocation')
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId 'ParameterIsNotAllowedWithPSGallery' `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $ScriptSourceLocation
        }

        $ScriptSourceLocation = $Options[$script:ScriptSourceLocation]

        if(-not (Microsoft.PowerShell.Management\Test-Path $ScriptSourceLocation) -and
           -not (Test-WebUri -uri $ScriptSourceLocation))
        {
            $ScriptSourceLocationUri = [Uri]$ScriptSourceLocation
            if($ScriptSourceLocationUri.Scheme -eq 'file')
            {
                $message = $LocalizedData.PathNotFound -f ($ScriptSourceLocation)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "PathNotFound" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $ScriptSourceLocation
            }
            else
            {
                $message = $LocalizedData.InvalidWebUri -f ($ScriptSourceLocation, "ScriptSourceLocation")
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidWebUri" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $ScriptSourceLocation
            }            
        }
    }

    $ScriptPublishLocation = $null
    if($Options.ContainsKey($script:ScriptPublishLocation))
    {
        if($Name -eq $Script:PSGalleryModuleSource)
        {
            $message = $LocalizedData.ParameterIsNotAllowedWithPSGallery -f ('ScriptPublishLocation')
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId 'ParameterIsNotAllowedWithPSGallery' `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $ScriptPublishLocation
        }

        $ScriptPublishLocation = $Options[$script:ScriptPublishLocation]

        if(-not (Microsoft.PowerShell.Management\Test-Path $ScriptPublishLocation) -and
           -not (Test-WebUri -uri $ScriptPublishLocation))
        {
            $ScriptPublishLocationUri = [Uri]$ScriptPublishLocation
            if($ScriptPublishLocationUri.Scheme -eq 'file')
            {
                $message = $LocalizedData.PathNotFound -f ($ScriptPublishLocation)
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "PathNotFound" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $ScriptPublishLocation
            }
            else
            {
                $message = $LocalizedData.InvalidWebUri -f ($ScriptPublishLocation, "ScriptPublishLocation")
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "InvalidWebUri" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument `
                           -ExceptionObject $ScriptPublishLocation
            }            
        }
    }

    $currentSourceObject = $null

    # Check if Name is already registered
    if($script:PSGetModuleSources.Contains($Name))
    {
        $currentSourceObject = $script:PSGetModuleSources[$Name]
    }

    # Location is not allowed for PSGallery source
    # However OneGet passes Location value during Set-PackageSource cmdlet, 
    # that's why ensuring that Location value is same as the current SourceLocation
    #
    if(($Name -eq $Script:PSGalleryModuleSource) -and 
       $Location -and
       ((-not $IsUpdatePackageSource) -or ($currentSourceObject -and $currentSourceObject.SourceLocation -ne $Location)))
    {
        $message = $LocalizedData.ParameterIsNotAllowedWithPSGallery -f ('Location, NewLocation or SourceLocation')
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $message `
                   -ErrorId 'ParameterIsNotAllowedWithPSGallery' `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument `
                   -ExceptionObject $Location
    }

    if($Name -eq $Script:PSGalleryModuleSource)
    {
        # Add or update the PSGallery repository
        $repository = Set-PSGalleryRepository -Trusted:$Trusted

        if($repository)
        {
            # return the package source object.
            Write-Output -InputObject (New-PackageSourceFromModuleSource -ModuleSource $repository)
        }

        return
    }

    if($Location)
    {
        # Ping and resolve the specified location
        $Location = Resolve-Location -Location $Location `
                                     -LocationParameterName 'Location' `
                                     -Credential $Credential `
                                     -Proxy $Proxy `
                                     -ProxyCredential $ProxyCredential `
                                     -CallerPSCmdlet $PSCmdlet
    }

    if(-not $Location)
    {
        # Above Resolve-Location function throws an error when it is not able to resolve a location
        return
    }

    if(-not (Microsoft.PowerShell.Management\Test-Path -Path $Location) -and
       -not (Test-WebUri -uri $Location) )
    {
        $LocationUri = [Uri]$Location
        if($LocationUri.Scheme -eq 'file')
        {
            $message = $LocalizedData.PathNotFound -f ($Location)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "PathNotFound" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
        else
        {
            $message = $LocalizedData.InvalidWebUri -f ($Location, "Location")
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "InvalidWebUri" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
    }

    if(Test-WildcardPattern $Name)
    {
        $message = $LocalizedData.RepositoryNameContainsWildCards -f ($Name)
        ThrowError -ExceptionName "System.ArgumentException" `
                    -ExceptionMessage $message `
                    -ErrorId "RepositoryNameContainsWildCards" `
                    -CallerPSCmdlet $PSCmdlet `
                    -ErrorCategory InvalidArgument `
                    -ExceptionObject $Name
    }

    $LocationString = Get-ValidModuleLocation -LocationString $Location -ParameterName "Location" -Proxy $Proxy -ProxyCredential $ProxyCredential -Credential $Credential

    # Check if Location is already registered with another Name
    $existingSourceName = Get-SourceName -Location $LocationString

    if($existingSourceName -and 
       ($Name -ne $existingSourceName) -and
       -not $IsNewModuleSource)
    {
        $message = $LocalizedData.RepositoryAlreadyRegistered -f ($existingSourceName, $Location, $Name)
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $message `
                   -ErrorId "RepositoryAlreadyRegistered" `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
    }
    
    if(-not $PublishLocation -and $currentSourceObject -and $currentSourceObject.PublishLocation)
    {
        $PublishLocation = $currentSourceObject.PublishLocation
    }

    if((-not $ScriptPublishLocation) -and 
       $currentSourceObject -and 
       (Get-Member -InputObject $currentSourceObject -Name $script:ScriptPublishLocation) -and 
       $currentSourceObject.ScriptPublishLocation)
    {
        $ScriptPublishLocation = $currentSourceObject.ScriptPublishLocation
    }

    if((-not $ScriptSourceLocation) -and 
       $currentSourceObject -and 
       (Get-Member -InputObject $currentSourceObject -Name $script:ScriptSourceLocation) -and 
       $currentSourceObject.ScriptSourceLocation)
    {
        $ScriptSourceLocation = $currentSourceObject.ScriptSourceLocation
    }

    $IsProviderSpecified = $false;
    if ($Options.ContainsKey($script:PackageManagementProviderParam))
    {
        $SpecifiedProviderName = $Options[$script:PackageManagementProviderParam] 

        $IsProviderSpecified = $true

        Write-Verbose ($LocalizedData.SpecifiedProviderName -f $SpecifiedProviderName)
        if ($SpecifiedProviderName -eq $script:PSModuleProviderName)
        {
            $message = $LocalizedData.InvalidPackageManagementProviderValue -f ($SpecifiedProviderName, $script:NuGetProviderName, $script:NuGetProviderName)
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InvalidPackageManagementProviderValue" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $SpecifiedProviderName
            return
        }
    }
    else
    {
        $SpecifiedProviderName = $script:NuGetProviderName
        Write-Verbose ($LocalizedData.ProviderNameNotSpecified -f $SpecifiedProviderName)
    }

    $packageSource = $null
        
    $selProviders = $request.SelectProvider($SpecifiedProviderName)

    if(-not $selProviders -and $IsProviderSpecified)
    {
        $message = $LocalizedData.SpecifiedProviderNotAvailable -f $SpecifiedProviderName
        ThrowError -ExceptionName "System.InvalidOperationException" `
                    -ExceptionMessage $message `
                    -ErrorId "SpecifiedProviderNotAvailable" `
                    -CallerPSCmdlet $PSCmdlet `
                    -ErrorCategory InvalidOperation `
                    -ExceptionObject $SpecifiedProviderName
    }

    # Try with user specified provider or NuGet provider
    foreach($SelectedProvider in $selProviders)
    {
        if($request.IsCanceled)
        {
            return
        }

        if($SelectedProvider -and $SelectedProvider.Features.ContainsKey($script:SupportsPSModulesFeatureName))
        {
            $NewRequest = $request.CloneRequest( $null, @($LocationString), $request.Credential )
            $packageSource = $SelectedProvider.ResolvePackageSources( $NewRequest )
        }
        else
        {
            $message = $LocalizedData.SpecifiedProviderDoesnotSupportPSModules -f $SelectedProvider.ProviderName
            ThrowError -ExceptionName "System.InvalidOperationException" `
                        -ExceptionMessage $message `
                        -ErrorId "SpecifiedProviderDoesnotSupportPSModules" `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidOperation `
                        -ExceptionObject $SelectedProvider.ProviderName
        }

        if($packageSource)
        {
            break
        }
    }

    # Poll other package provider when NuGet provider doesn't resolves the specified location
    if(-not $packageSource -and -not $IsProviderSpecified)
    {
        Write-Verbose ($LocalizedData.PollingPackageManagementProvidersForLocation -f $LocationString)

        $moduleProviders = $request.SelectProvidersWithFeature($script:SupportsPSModulesFeatureName)
        
        foreach($provider in $moduleProviders)
        {
            if($request.IsCanceled)
            {
                return
            }

            # Skip already tried $SpecifiedProviderName and PowerShellGet provider
            if($provider.ProviderName -eq $SpecifiedProviderName -or 
               $provider.ProviderName -eq $script:PSModuleProviderName)
            {
                continue
            }

            Write-Verbose ($LocalizedData.PollingSingleProviderForLocation -f ($LocationString, $provider.ProviderName))
            $NewRequest = $request.CloneRequest( @{}, @($LocationString), $request.Credential )
            $packageSource = $provider.ResolvePackageSources($NewRequest) 

            if($packageSource)
            {
                Write-Verbose ($LocalizedData.FoundProviderForLocation -f ($provider.ProviderName, $Location))
                $SelectedProvider = $provider
                break
            }
        }
    }

    if(-not $packageSource)
    {
        $message = $LocalizedData.SpecifiedLocationCannotBeRegistered -f $Location
        ThrowError -ExceptionName "System.InvalidOperationException" `
                    -ExceptionMessage $message `
                    -ErrorId "SpecifiedLocationCannotBeRegistered" `
                    -CallerPSCmdlet $PSCmdlet `
                    -ErrorCategory InvalidOperation `
                    -ExceptionObject $Location
    }

    $ProviderOptions = @{}

    $SelectedProvider.DynamicOptions | Microsoft.PowerShell.Core\ForEach-Object { 
                                            if($options.ContainsKey($_.Name) ) 
                                            { 
                                                $ProviderOptions[$_.Name] = $options[$_.Name]
                                            }
                                       }

    # Keep the existing provider options if not specified in Set-PSRepository
    if($currentSourceObject)
    {
        $currentSourceObject.ProviderOptions.GetEnumerator() | Microsoft.PowerShell.Core\ForEach-Object {
                                                                   if (-not $ProviderOptions.ContainsKey($_.Key) )
                                                                   {
                                                                       $ProviderOptions[$_.Key] = $_.Value
                                                                   }
                                                               }
    }
    
    if(-not $PublishLocation)
    {
        $PublishLocation = Get-PublishLocation -Location $LocationString
    }
    
    # Use the PublishLocation for the scripts when ScriptPublishLocation is not specified by the user
    if(-not $ScriptPublishLocation)
    {
        $ScriptPublishLocation = $PublishLocation

        # ScriptPublishLocation and PublishLocation should be equal in case of SMB Share or Local directory paths
        if($Options.ContainsKey($script:ScriptPublishLocation) -and
           (Microsoft.PowerShell.Management\Test-Path -Path $ScriptPublishLocation))
        {
            if($ScriptPublishLocation -ne $PublishLocation)
            {
                $message = $LocalizedData.PublishLocationPathsForModulesAndScriptsShouldBeEqual -f ($LocationString, $ScriptSourceLocation)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "PublishLocationPathsForModulesAndScriptsShouldBeEqual" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation `
                            -ExceptionObject $Location
            }
        }
    }

    if(-not $ScriptSourceLocation)
    {
        $ScriptSourceLocation = Get-ScriptSourceLocation -Location $LocationString -Proxy $Proxy -ProxyCredential $ProxyCredential -Credential $Credential
    }
    elseif($Options.ContainsKey($script:ScriptSourceLocation))
    {
        # ScriptSourceLocation and SourceLocation cannot be same for they are URLs
        # Both should be equal in case of SMB Share or Local directory paths
        if(Microsoft.PowerShell.Management\Test-Path -Path $ScriptSourceLocation)
        {
            if($ScriptSourceLocation -ne $LocationString)
            {
                $message = $LocalizedData.SourceLocationPathsForModulesAndScriptsShouldBeEqual -f ($LocationString, $ScriptSourceLocation)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "SourceLocationPathsForModulesAndScriptsShouldBeEqual" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation `
                            -ExceptionObject $Location
            }
        }
        else
        {
            if($ScriptSourceLocation -eq $LocationString -and
               -not ($LocationString.EndsWith('/nuget/v2', [System.StringComparison]::OrdinalIgnoreCase)) -and
               -not ($LocationString.EndsWith('/nuget/v2/', [System.StringComparison]::OrdinalIgnoreCase)) -and
               -not ($LocationString.EndsWith('/nuget', [System.StringComparison]::OrdinalIgnoreCase)) -and
               -not ($LocationString.EndsWith('/nuget/', [System.StringComparison]::OrdinalIgnoreCase)) -and
               -not ($LocationString.EndsWith('index.json', [System.StringComparison]::OrdinalIgnoreCase)) -and
               -not ($LocationString.EndsWith('index.json/', [System.StringComparison]::OrdinalIgnoreCase))
              )
            {
                $message = $LocalizedData.SourceLocationUrisForModulesAndScriptsShouldBeDifferent -f ($LocationString, $ScriptSourceLocation)
                ThrowError -ExceptionName "System.InvalidOperationException" `
                            -ExceptionMessage $message `
                            -ErrorId "SourceLocationUrisForModulesAndScriptsShouldBeDifferent" `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation `
                            -ExceptionObject $Location
            }
        }
    }    

    # no error so we can safely remove the source
    if($script:PSGetModuleSources.Contains($Name))
    {
        $null = $script:PSGetModuleSources.Remove($Name)
    }

    # Add new module source
    $moduleSource = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
            Name = $Name
            SourceLocation = $LocationString            
            PublishLocation = $PublishLocation
            ScriptSourceLocation = $ScriptSourceLocation
            ScriptPublishLocation = $ScriptPublishLocation
            Trusted=$Trusted
            Registered= (-not $IsNewModuleSource)
            InstallationPolicy = if($Trusted) {'Trusted'} else {'Untrusted'}
            PackageManagementProvider = $SelectedProvider.ProviderName
            ProviderOptions = $ProviderOptions
        })

    #region telemetry - Capture non-PSGallery registrations as telemetry events
    if ($script:TelemetryEnabled)
    {   
                
        Log-NonPSGalleryRegistration -sourceLocation $moduleSource.SourceLocation `
                                     -installationPolicy $moduleSource.InstallationPolicy `
                                     -packageManagementProvider $moduleSource.PackageManagementProvider `
                                     -publishLocation $moduleSource.PublishLocation `
                                     -scriptSourceLocation $moduleSource.ScriptSourceLocation `
                                     -scriptPublishLocation $moduleSource.ScriptPublishLocation `
                                     -operationName PSGET_NONPSGALLERY_REGISTRATION `
                                     -ErrorAction SilentlyContinue `
                                     -WarningAction SilentlyContinue                                   
                
    }
    #endregion 

    $moduleSource.PSTypeNames.Insert(0, "Microsoft.PowerShell.Commands.PSRepository")

    # Persist the repositories only when Register-PSRepository cmdlet is used
    if(-not $IsNewModuleSource)
    {
        $script:PSGetModuleSources.Add($Name, $moduleSource)            

        $message = $LocalizedData.RepositoryRegistered -f ($Name, $LocationString)
        Write-Verbose $message

        # Persist the module sources
        Save-ModuleSources
    }

    # return the package source object.
    Write-Output -InputObject (New-PackageSourceFromModuleSource -ModuleSource $moduleSource)
}

function Resolve-PackageSource
{ 
    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Resolve-PackageSource'))

    Set-ModuleSourcesVariable

    $SourceName = $request.PackageSources

    if(-not $SourceName)
    {
        $SourceName = "*"
    }

    foreach($moduleSourceName in $SourceName)
    {
        if($request.IsCanceled)
        {
            return
        }

        $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $moduleSourceName,$script:wildcardOptions
        $moduleSourceFound = $false

        $script:PSGetModuleSources.GetEnumerator() | 
            Microsoft.PowerShell.Core\Where-Object {$wildcardPattern.IsMatch($_.Key)} | 
                Microsoft.PowerShell.Core\ForEach-Object {

                    $moduleSource = $script:PSGetModuleSources[$_.Key]

                    $packageSource = New-PackageSourceFromModuleSource -ModuleSource $moduleSource

                    Write-Output -InputObject $packageSource

                    $moduleSourceFound = $true
                }

        if(-not $moduleSourceFound)
        {
            $sourceName  = Get-SourceName -Location $moduleSourceName

            if($sourceName)
            {
                $moduleSource = $script:PSGetModuleSources[$sourceName]

                $packageSource = New-PackageSourceFromModuleSource -ModuleSource $moduleSource

                Write-Output -InputObject $packageSource
            }
            elseif( -not (Test-WildcardPattern $moduleSourceName))
            {
                $message = $LocalizedData.RepositoryNotFound -f ($moduleSourceName)

                Write-Error -Message $message -ErrorId "RepositoryNotFound" -Category InvalidOperation -TargetObject $moduleSourceName
            }
        }
    }
}

function Remove-PackageSource
{ 
    param
    (
        [string]
        $Name
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Remove-PackageSource'))

    Set-ModuleSourcesVariable -Force

    $ModuleSourcesToBeRemoved = @()

    foreach ($moduleSourceName in $Name)
    {
        if($request.IsCanceled)
        {
            return
        }

        # Check if $Name contains any wildcards
        if(Test-WildcardPattern $moduleSourceName)
        {
            $message = $LocalizedData.RepositoryNameContainsWildCards -f ($moduleSourceName)
            Write-Error -Message $message -ErrorId "RepositoryNameContainsWildCards" -Category InvalidOperation -TargetObject $moduleSourceName
            continue
        }

        # Check if the specified module source name is in the registered module sources
        if(-not $script:PSGetModuleSources.Contains($moduleSourceName))
        {
            $message = $LocalizedData.RepositoryNotFound -f ($moduleSourceName)
            Write-Error -Message $message -ErrorId "RepositoryNotFound" -Category InvalidOperation -TargetObject $moduleSourceName
            continue
        }

        $ModuleSourcesToBeRemoved += $moduleSourceName
        $message = $LocalizedData.RepositoryUnregistered -f ($moduleSourceName)
        Write-Verbose $message
    }

    # Remove the module source
    $ModuleSourcesToBeRemoved | Microsoft.PowerShell.Core\ForEach-Object { $null = $script:PSGetModuleSources.Remove($_) }

    # Persist the module sources
    Save-ModuleSources
}

function Find-Package
{ 
    [CmdletBinding()]
    param
    (
        [string[]]
        $names,

        [string]
        $requiredVersion,

        [string]
        $minimumVersion,

        [string]
        $maximumVersion
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Find-Package'))

    Set-ModuleSourcesVariable

    if($RequiredVersion -and $MinimumVersion)
    {
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $LocalizedData.VersionRangeAndRequiredVersionCannotBeSpecifiedTogether `
                   -ErrorId "VersionRangeAndRequiredVersionCannotBeSpecifiedTogether" `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
    }

    if($RequiredVersion -or $MinimumVersion)
    {
        if(-not $names -or $names.Count -ne 1 -or (Test-WildcardPattern -Name $names[0]))
        {
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $LocalizedData.VersionParametersAreAllowedOnlyWithSingleName `
                       -ErrorId "VersionParametersAreAllowedOnlyWithSingleName" `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
        }
    }

    $options = $request.Options

    foreach( $o in $options.Keys )
    {
        Write-Debug ( "OPTION: {0} => {1}" -f ($o, $options[$o]) )
    }
	
	# When using -Name, we don't send PSGet-specific properties to the server - we will filter it ourselves
	$postFilter = New-Object -TypeName  System.Collections.Hashtable
	if($options.ContainsKey("Name"))
	{
		if($options.ContainsKey("Includes"))
		{	
			$postFilter["Includes"] = $options["Includes"]
			$null = $options.Remove("Includes")
		}
		
		if($options.ContainsKey("DscResource"))
		{	
			$postFilter["DscResource"] = $options["DscResource"]
			$null = $options.Remove("DscResource")
		}
		
		if($options.ContainsKey('RoleCapability'))
		{	
			$postFilter['RoleCapability'] = $options['RoleCapability']
			$null = $options.Remove('RoleCapability')
		}
		
		if($options.ContainsKey("Command"))
		{	
			$postFilter["Command"] = $options["Command"]
			$null = $options.Remove("Command")
		}
	}

    $LocationOGPHashtable = [ordered]@{}
    if($options -and $options.ContainsKey('Source'))
    {
        $SourceNames = $($options['Source'])

        Write-Verbose ($LocalizedData.SpecifiedSourceName -f ($SourceNames))

        foreach($sourceName in $SourceNames)
        {
            if($script:PSGetModuleSources.Contains($sourceName))
            {
                $ModuleSource = $script:PSGetModuleSources[$sourceName]
                $LocationOGPHashtable[$ModuleSource.SourceLocation] = (Get-ProviderName -PSCustomObject $ModuleSource)
            }
            else
            {
                $sourceByLocation = Get-SourceName -Location $sourceName

                if ($sourceByLocation)
                {
                    $ModuleSource = $script:PSGetModuleSources[$sourceByLocation]
                    $LocationOGPHashtable[$ModuleSource.SourceLocation] = (Get-ProviderName -PSCustomObject $ModuleSource)
                }
                else
                {
                    $message = $LocalizedData.RepositoryNotFound -f ($sourceName)
                    Write-Error -Message $message `
                                -ErrorId 'RepositoryNotFound' `
                                -Category InvalidArgument `
                                -TargetObject $sourceName
                }
            }
        }
    }
    elseif($options -and 
           $options.ContainsKey($script:PackageManagementProviderParam) -and 
           $options.ContainsKey('Location'))
    {
        $Location = $options['Location']
        $PackageManagementProvider = $options['PackageManagementProvider']

        Write-Verbose ($LocalizedData.SpecifiedLocationAndOGP -f ($Location, $PackageManagementProvider))

        $LocationOGPHashtable[$Location] = $PackageManagementProvider
    }
    else
    {
        Write-Verbose $LocalizedData.NoSourceNameIsSpecified

        $script:PSGetModuleSources.Values | Microsoft.PowerShell.Core\ForEach-Object { $LocationOGPHashtable[$_.SourceLocation] = (Get-ProviderName -PSCustomObject $_) }
    }

    $artifactTypes = $script:PSArtifactTypeModule
    if($options.ContainsKey($script:PSArtifactType))
    {
        $artifactTypes = $options[$script:PSArtifactType]
    }

    if($artifactTypes -eq $script:All)
    {
        $artifactTypes = @($script:PSArtifactTypeModule,$script:PSArtifactTypeScript)
    }

    $providerOptions = @{}

    if($options.ContainsKey($script:AllVersions))
    {
        $providerOptions[$script:AllVersions] = $options[$script:AllVersions]
    }

    if($options.ContainsKey($script:Filter))
    {
        $Filter = $options[$script:Filter]
        $providerOptions['Contains'] = $Filter
    }

    if($options.ContainsKey($script:Tag))
    {
        $userSpecifiedTags = $options[$script:Tag] | Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore
    }
    else
    {
        $userSpecifiedTags = @($script:NotSpecified)
    }

    $specifiedDscResources = @()
    if($options.ContainsKey('DscResource'))
    {
        $specifiedDscResources = $options['DscResource'] | 
                                    Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore | 
                                        Microsoft.PowerShell.Core\ForEach-Object {"$($script:DscResource)_$_"}
    }

    $specifiedRoleCapabilities = @()
    if($options.ContainsKey('RoleCapability'))
    {
        $specifiedRoleCapabilities = $options['RoleCapability'] | 
                                        Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore | 
                                            Microsoft.PowerShell.Core\ForEach-Object {"$($script:RoleCapability)_$_"}
    }

    $specifiedCommands = @()
    if($options.ContainsKey('Command'))
    {
        $specifiedCommands = $options['Command'] | 
                                Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore |
                                    Microsoft.PowerShell.Core\ForEach-Object {"$($script:Command)_$_"}
    }

    $specifiedIncludes = @()
    if($options.ContainsKey('Includes'))
    {
        $includes = $options['Includes'] | 
                        Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore | 
                            Microsoft.PowerShell.Core\ForEach-Object {"$($script:Includes)_$_"}
        
        # Add PSIncludes_DscResource to $specifiedIncludes iff -DscResource names are not specified
        # Add PSIncludes_RoleCapability to $specifiedIncludes iff -RoleCapability names are not specified
        # Add PSIncludes_Cmdlet or PSIncludes_Function to $specifiedIncludes iff -Command names are not specified
        # otherwise $script:NotSpecified will be added to $specifiedIncludes
        if($includes)
        {   
            if(-not $specifiedDscResources -and ($includes -contains "$($script:Includes)_DscResource") )
            {
               $specifiedIncludes += "$($script:Includes)_DscResource"
            }

            if(-not $specifiedRoleCapabilities -and ($includes -contains "$($script:Includes)_RoleCapability") )
            {
               $specifiedIncludes += "$($script:Includes)_RoleCapability"
            }

            if(-not $specifiedCommands)
            {
               if($includes -contains "$($script:Includes)_Cmdlet")
               {
                   $specifiedIncludes += "$($script:Includes)_Cmdlet"
               }

               if($includes -contains "$($script:Includes)_Function")
               {
                   $specifiedIncludes += "$($script:Includes)_Function"
               }

               if($includes -contains "$($script:Includes)_Workflow")
               {
                   $specifiedIncludes += "$($script:Includes)_Workflow"
               }
            }
        }
    }

    if(-not $specifiedDscResources)
    {
        $specifiedDscResources += $script:NotSpecified
    }

    if(-not $specifiedRoleCapabilities)
    {
        $specifiedRoleCapabilities += $script:NotSpecified
    }

    if(-not $specifiedCommands)
    {
        $specifiedCommands += $script:NotSpecified
    }

    if(-not $specifiedIncludes)
    {
        $specifiedIncludes += $script:NotSpecified
    }
    
    $providerSearchTags = @{}

    foreach($tag in $userSpecifiedTags)
    {
        foreach($include in $specifiedIncludes)
        {
            foreach($command in $specifiedCommands)
            {
                foreach($resource in $specifiedDscResources)
                {
                    foreach($roleCapability in $specifiedRoleCapabilities)
                    {
                        $providerTags = @()
                        if($resource -ne $script:NotSpecified)
                        {
                            $providerTags += $resource
                        }

                        if($roleCapability -ne $script:NotSpecified)
                        {
                            $providerTags += $roleCapability
                        }

                        if($command -ne $script:NotSpecified)
                        {
                            $providerTags += $command
                        }
                    
                        if($include -ne $script:NotSpecified)
                        {
                            $providerTags += $include
                        }

                        if($tag -ne $script:NotSpecified)
                        {
                            $providerTags += $tag
                        }

                        if($providerTags)
                        {
                            $providerSearchTags["$tag $resource $roleCapability $command $include"] = $providerTags
                        }
                    }
                }
            }
        }
    }

    $InstallationPolicy = "Untrusted"
    if($options.ContainsKey('InstallationPolicy'))
    {
        $InstallationPolicy = $options['InstallationPolicy']
    }

    $streamedResults = @()

    foreach($artifactType in $artifactTypes)
    {
        foreach($kvPair in $LocationOGPHashtable.GetEnumerator())
        {
            if($request.IsCanceled)
            {
                return
            }

            $Location = $kvPair.Key
            if($artifactType -eq $script:PSArtifactTypeScript)
            {
                $sourceName = Get-SourceName -Location $Location

                if($SourceName)
                {
                    $ModuleSource = $script:PSGetModuleSources[$SourceName]

                    # Skip source if no ScriptSourceLocation is available.
                    if(-not $ModuleSource.ScriptSourceLocation)
                    {
                        if($options.ContainsKey('Source'))
                        {
                            $message = $LocalizedData.ScriptSourceLocationIsMissing -f ($ModuleSource.Name)
                            Write-Error -Message $message `
                                        -ErrorId 'ScriptSourceLocationIsMissing' `
                                        -Category InvalidArgument `
                                        -TargetObject $ModuleSource.Name
                        }

                        continue
                    }

                    $Location = $ModuleSource.ScriptSourceLocation
                }
            }

            $ProviderName = $kvPair.Value

            Write-Verbose ($LocalizedData.GettingPackageManagementProviderObject -f ($ProviderName))

	        $provider = $request.SelectProvider($ProviderName)

            if(-not $provider)
            {
                Write-Error -Message ($LocalizedData.PackageManagementProviderIsNotAvailable -f $ProviderName)

                Continue
            }

            Write-Verbose ($LocalizedData.SpecifiedLocationAndOGP -f ($Location, $provider.ProviderName))	

            if($providerSearchTags.Values.Count)
            {
                $tagList = $providerSearchTags.Values
            }
            else
            {
                $tagList = @($script:NotSpecified)
            }

            $namesParameterEmpty = ($names.Count -eq 1) -and ($names[0] -eq '')
        
            foreach($providerTag in $tagList)
            {
                if($request.IsCanceled)
                {
                    return
                }

                $FilterOnTag = @()

                if($providerTag -ne $script:NotSpecified)
                {
                    $FilterOnTag = $providerTag
                }

                if(Microsoft.PowerShell.Management\Test-Path -Path $Location)
                {
                    if($artifactType -eq $script:PSArtifactTypeScript)
                    {
                        $FilterOnTag += 'PSScript'
                    }
                    elseif($artifactType -eq $script:PSArtifactTypeModule)
                    {
                        $FilterOnTag += 'PSModule'
                    }
                }

                if($FilterOnTag)
                {
                    $providerOptions["FilterOnTag"] = $FilterOnTag
                }
                elseif($providerOptions.ContainsKey('FilterOnTag'))
                {
                    $null = $providerOptions.Remove('FilterOnTag')
                }

                if($request.Options.ContainsKey($script:FindByCanonicalId))
                {
                    $providerOptions[$script:FindByCanonicalId] = $request.Options[$script:FindByCanonicalId]
                }

                $providerOptions["Headers"] = 'PSGalleryClientVersion=1.1'
				
                $NewRequest = $request.CloneRequest( $providerOptions, @($Location), $request.Credential )

                $pkgs = $provider.FindPackages($names, 
                                               $requiredVersion, 
                                               $minimumVersion, 
                                               $maximumVersion,
                                               $NewRequest )

                foreach($pkg in  $pkgs)
                {
                    if($request.IsCanceled)
                    {
                        return
                    }

                    # $pkg.Name has to match any of the supplied names, using PowerShell wildcards
                    if ($namesParameterEmpty -or ($names | % { if ($pkg.Name -like $_){return $true; break} } -End {return $false}))
                    {
						$includePackage = $true
						
						# If -Name was provided, we need to post-filter
						# Filtering has AND semantics between different parameters and OR within a parameter (each parameter is potentially an array)
						if($options.ContainsKey("Name") -and $postFilter.Count -gt 0)
						{
							if ($pkg.Metadata["DscResources"].Count -gt 0)
							{
								$pkgDscResources = $pkg.Metadata["DscResources"] -Split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
							}
							else
							{
								$pkgDscResources = $pkg.Metadata["tags"] -Split " " `
									| Microsoft.PowerShell.Core\Where-Object { $_.Trim() } `
									| Microsoft.PowerShell.Core\Where-Object { $_.StartsWith($script:DscResource, [System.StringComparison]::OrdinalIgnoreCase) } `
									| Microsoft.PowerShell.Core\ForEach-Object { $_.Substring($script:DscResource.Length + 1) }
							}

							if ($pkg.Metadata['RoleCapabilities'].Count -gt 0)
							{
								$pkgRoleCapabilities = $pkg.Metadata['RoleCapabilities'] -Split ' ' | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
							}
							else
							{
								$pkgRoleCapabilities = $pkg.Metadata["tags"] -Split ' ' `
									| Microsoft.PowerShell.Core\Where-Object { $_.Trim() } `
									| Microsoft.PowerShell.Core\Where-Object { $_.StartsWith($script:RoleCapability, [System.StringComparison]::OrdinalIgnoreCase) } `
									| Microsoft.PowerShell.Core\ForEach-Object { $_.Substring($script:RoleCapability.Length + 1) }
							}
							
							if ($pkg.Metadata["Functions"].Count -gt 0)
							{
								$pkgFunctions = $pkg.Metadata["Functions"] -Split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
							}
							else
							{
								$pkgFunctions = $pkg.Metadata["tags"] -Split " " `
									| Microsoft.PowerShell.Core\Where-Object { $_.Trim() } `
									| Microsoft.PowerShell.Core\Where-Object { $_.StartsWith($script:Function, [System.StringComparison]::OrdinalIgnoreCase) } `
									| Microsoft.PowerShell.Core\ForEach-Object { $_.Substring($script:Function.Length + 1) }
							}
							
							if ($pkg.Metadata["Cmdlets"].Count -gt 0)
							{
								$pkgCmdlets = $pkg.Metadata["Cmdlets"] -Split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
							}
							else
							{
								$pkgCmdlets = $pkg.Metadata["tags"] -Split " " `
									| Microsoft.PowerShell.Core\Where-Object { $_.Trim() } `
									| Microsoft.PowerShell.Core\Where-Object { $_.StartsWith($script:Cmdlet, [System.StringComparison]::OrdinalIgnoreCase) } `
									| Microsoft.PowerShell.Core\ForEach-Object { $_.Substring($script:Cmdlet.Length + 1) }
							}
							
							if ($pkg.Metadata["Workflows"].Count -gt 0)
							{
								$pkgWorkflows = $pkg.Metadata["Workflows"] -Split " " | Microsoft.PowerShell.Core\Where-Object { $_.Trim() }
							}
							else
							{
								$pkgWorkflows = $pkg.Metadata["tags"] -Split " " `
									| Microsoft.PowerShell.Core\Where-Object { $_.Trim() } `
									| Microsoft.PowerShell.Core\Where-Object { $_.StartsWith($script:Workflow, [System.StringComparison]::OrdinalIgnoreCase) } `
									| Microsoft.PowerShell.Core\ForEach-Object { $_.Substring($script:Workflow.Length + 1) }
							}
						
							foreach ($key in $postFilter.Keys)
							{
								switch ($key)
								{
									"DscResource" {
										$values = $postFilter[$key]
										
										$includePackage = $false
										
										foreach ($value in $values)
										{
											$wildcardPattern = New-Object System.Management.Automation.WildcardPattern $value,$script:wildcardOptions
											
											$pkgDscResources | Microsoft.PowerShell.Core\ForEach-Object {
												if ($wildcardPattern.IsMatch($_))
												{
													$includePackage = $true
													break
												}
											}
										}
										
										if (-not $includePackage)
										{
											break
										}
									}

									'RoleCapability' {
										$values = $postFilter[$key]
										
										$includePackage = $false
										
										foreach ($value in $values)
										{
											$wildcardPattern = New-Object System.Management.Automation.WildcardPattern $value,$script:wildcardOptions
											
											$pkgRoleCapabilities | Microsoft.PowerShell.Core\ForEach-Object {
												if ($wildcardPattern.IsMatch($_))
												{
													$includePackage = $true
													break
												}
											}
										}
										
										if (-not $includePackage)
										{
											break
										}
									}
									
									"Command" {
										$values = $postFilter[$key]
										
										$includePackage = $false
										
										foreach ($value in $values)
										{
											$wildcardPattern = New-Object System.Management.Automation.WildcardPattern $value,$script:wildcardOptions
											
											$pkgFunctions | Microsoft.PowerShell.Core\ForEach-Object {
												if ($wildcardPattern.IsMatch($_))
												{
													$includePackage = $true
													break
												}
											}
					
											$pkgCmdlets | Microsoft.PowerShell.Core\ForEach-Object {
												if ($wildcardPattern.IsMatch($_))
												{
													$includePackage = $true
													break
												}
											}
											
											$pkgWorkflows | Microsoft.PowerShell.Core\ForEach-Object {
												if ($wildcardPattern.IsMatch($_))
												{
													$includePackage = $true
													break
												}
											}
										}
				
										if (-not $includePackage)
										{
											break
										}
									}
									
									"Includes" {
										$values = $postFilter[$key]
										
										$includePackage = $false
										
										foreach ($value in $values)
										{
											switch ($value)
											{
												"Cmdlet" { if ($pkgCmdlets ) { $includePackage = $true } }
												"Function" { if ($pkgFunctions ) { $includePackage = $true } }
												"DscResource" { if ($pkgDscResources ) { $includePackage = $true } }
												"RoleCapability" { if ($pkgRoleCapabilities ) { $includePackage = $true } }
												"Workflow" { if ($pkgWorkflows ) { $includePackage = $true } }
											}
										}
										
										if (-not $includePackage)
										{
											break
										}
									}
								}
							}
						}
						
						if ($includePackage)
						{
							$fastPackageReference = New-FastPackageReference -ProviderName $provider.ProviderName `
																			-PackageName $pkg.Name `
																			-Version $pkg.Version `
																			-Source $Location `
																			-ArtifactType $artifactType
	
							if($streamedResults -notcontains $fastPackageReference)
							{
								$streamedResults += $fastPackageReference
	
								$FromTrustedSource = $false
	
								$ModuleSourceName = Get-SourceName -Location $Location
	
								if($ModuleSourceName)
								{
									$FromTrustedSource = $script:PSGetModuleSources[$ModuleSourceName].Trusted
								}
								elseif($InstallationPolicy -eq "Trusted")
								{
									$FromTrustedSource = $true
								}
	
								$sid = New-SoftwareIdentityFromPackage -Package $pkg `
																	-PackageManagementProviderName $provider.ProviderName `
																	-SourceLocation $Location `
																	-IsFromTrustedSource:$FromTrustedSource `
																	-Type $artifactType `
																	-request $request
				
								$script:FastPackRefHastable[$fastPackageReference] = $pkg
	
								Write-Output -InputObject $sid
							}
						}
                    }
                }
            }
        }
    }
}

function Download-Package
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FastPackageReference,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Location
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Download-Package'))

    Install-PackageUtility -FastPackageReference $FastPackageReference -Request $Request -Location $Location
}

function Install-Package
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FastPackageReference
    )

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Install-Package'))

    Install-PackageUtility -FastPackageReference $FastPackageReference -Request $Request
}

function Install-PackageUtility
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FastPackageReference,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Location,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $request
    )

    Set-ModuleSourcesVariable

    Write-Debug ($LocalizedData.ProviderApiDebugMessage -f ('Install-PackageUtility'))

    Write-Debug ($LocalizedData.FastPackageReference -f $fastPackageReference)     
    
    $Force = $false
    $SkipPublisherCheck = $false
    $AllowClobber = $false
    $Debug = $false
    $MinimumVersion = $null
    $RequiredVersion = $null
    $IsSavePackage = $false
    $Scope = $null
    $NoPathUpdate = $false

    # take the fastPackageReference and get the package object again.
    $parts = $fastPackageReference -Split '[|]'

    if( $parts.Length -eq 5 )
    {
        $providerName = $parts[0]
        $packageName = $parts[1]
        $version = $parts[2]
        $sourceLocation= $parts[3]
        $artfactType = $parts[4]

        # The default destination location for Modules and Scripts is ProgramFiles path
        $scriptDestination = $script:ProgramFilesScriptsPath
        $moduleDestination = $script:programFilesModulesPath
        $Scope = 'AllUsers'

        if($artfactType -eq $script:PSArtifactTypeScript)
        {
            $AdminPreviligeErrorMessage = $LocalizedData.InstallScriptNeedsCurrentUserScopeParameterForNonAdminUser -f @($script:ProgramFilesScriptsPath, $script:MyDocumentsScriptsPath)
            $AdminPreviligeErrorId = 'InstallScriptNeedsCurrentUserScopeParameterForNonAdminUser'
        }
        else
        {
            $AdminPreviligeErrorMessage = $LocalizedData.InstallModuleNeedsCurrentUserScopeParameterForNonAdminUser -f @($script:programFilesModulesPath, $script:MyDocumentsModulesPath)
            $AdminPreviligeErrorId = 'InstallModuleNeedsCurrentUserScopeParameterForNonAdminUser'
        }

        $installUpdate = $false

        $options = $request.Options

        if($options)
        {
            foreach( $o in $options.Keys )
            {
                Write-Debug ("OPTION: {0} => {1}" -f ($o, $request.Options[$o]) )
            }

            if($options.ContainsKey('Scope'))
            {
                $Scope = $options['Scope']
                Write-Verbose ($LocalizedData.SpecifiedInstallationScope -f $Scope)
        
                if($Scope -eq "CurrentUser")
                {
                    $scriptDestination = $script:MyDocumentsScriptsPath
                    $moduleDestination = $script:MyDocumentsModulesPath
                }
                elseif($Scope -eq "AllUsers")
                {
                    $scriptDestination = $script:ProgramFilesScriptsPath
                    $moduleDestination = $script:programFilesModulesPath

                    if(-not (Test-RunningAsElevated))
                    {
                        # Throw an error when Install-Module/Script is used as a non-admin user and '-Scope CurrentUser' is not specified
                        ThrowError -ExceptionName "System.ArgumentException" `
                                    -ExceptionMessage $AdminPreviligeErrorMessage `
                                    -ErrorId $AdminPreviligeErrorId `
                                    -CallerPSCmdlet $PSCmdlet `
                                    -ErrorCategory InvalidArgument
                    }
                }
            }
            elseif($Location)
            {
                $IsSavePackage = $true
                $Scope = $null

                $moduleDestination = $Location
                $scriptDestination = $Location
            }
            # if no scope and no destination path and not elevated, then raise an error
            elseif(-not (Test-RunningAsElevated))
            {
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $AdminPreviligeErrorMessage `
                           -ErrorId $AdminPreviligeErrorId `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
            }

            if($options.ContainsKey('SkipPublisherCheck'))
            {
                $SkipPublisherCheck = $options['SkipPublisherCheck']

                if($SkipPublisherCheck.GetType().ToString() -eq 'System.String')
                {
                    if($SkipPublisherCheck -eq 'true')
                    {
                        $SkipPublisherCheck = $true
                    }
                    else
                    {
                        $SkipPublisherCheck = $false
                    }
                }
            }
            
            if($options.ContainsKey('AllowClobber'))
            {
                $AllowClobber = $options['AllowClobber']

                if($AllowClobber.GetType().ToString() -eq 'System.String')
                {
                    if($AllowClobber -eq 'false')
                    {
                        $AllowClobber = $false
                    }
                    elseif($AllowClobber -eq 'true')
                    {
                        $AllowClobber = $true
                    }
                }
            }

            if($options.ContainsKey('Force'))
            {
                $Force = $options['Force']

                if($Force.GetType().ToString() -eq 'System.String')
                {
                    if($Force -eq 'false')
                    {
                        $Force = $false
                    }
                    elseif($Force -eq 'true')
                    {
                        $Force = $true
                    }
                }
            }
            
            if($options.ContainsKey('Debug'))
            {
                $Debug = $options['Debug']

                if($Debug.GetType().ToString() -eq 'System.String')
                {
                    if($Debug -eq 'false')
                    {
                        $Debug = $false
                    }
                    elseif($Debug -eq 'true')
                    {
                        $Debug = $true
                    }
                }
            }            

            if($options.ContainsKey('NoPathUpdate'))
            {
                $NoPathUpdate = $options['NoPathUpdate']

                if($NoPathUpdate.GetType().ToString() -eq 'System.String')
                {
                    if($NoPathUpdate -eq 'false')
                    {
                        $NoPathUpdate = $false
                    }
                    elseif($NoPathUpdate -eq 'true')
                    {
                        $NoPathUpdate = $true
                    }
                }
            }

            if($options.ContainsKey('MinimumVersion'))
            {
                $MinimumVersion = $options['MinimumVersion']
            }

            if($options.ContainsKey('RequiredVersion'))
            {
                $RequiredVersion = $options['RequiredVersion']
            }
                        
            if($options.ContainsKey('InstallUpdate'))
            {
                $installUpdate = $options['InstallUpdate']

                if($installUpdate.GetType().ToString() -eq 'System.String')
                {
                    if($installUpdate -eq 'false')
                    {
                        $installUpdate = $false
                    }
                    elseif($installUpdate -eq 'true')
                    {
                        $installUpdate = $true
                    }
                }
            }            

            if($Scope -and ($artfactType -eq $script:PSArtifactTypeScript) -and (-not $installUpdate))
            {
                ValidateAndSet-PATHVariableIfUserAccepts -Scope $Scope `
                                                         -ScopePath $scriptDestination `
                                                         -Request $request `
                                                         -NoPathUpdate:$NoPathUpdate `
                                                         -Force:$Force
            }

            if($artfactType -eq $script:PSArtifactTypeModule)
            {
                $message = $LocalizedData.ModuleDestination -f @($moduleDestination)
            }
            else
            {
                $message = $LocalizedData.ScriptDestination -f @($scriptDestination, $moduleDestination)
            }            
            Write-Verbose $message            
        }

        Write-Debug "ArtfactType is $artfactType"

        if($artfactType -eq $script:PSArtifactTypeModule)
        {
            # Test if module is already installed
            $InstalledModuleInfo = if(-not $IsSavePackage){ Test-ModuleInstalled -Name $packageName -RequiredVersion $RequiredVersion }

            if(-not $Force -and $InstalledModuleInfo)
            {
                if($RequiredVersion -and (Test-ModuleSxSVersionSupport))
                {
                    # Check if the module with the required version is already installed otherwise proceed to install/update.
                    if($InstalledModuleInfo)
                    {
                        $message = $LocalizedData.ModuleWithRequiredVersionAlreadyInstalled -f ($InstalledModuleInfo.Version, $InstalledModuleInfo.Name, $InstalledModuleInfo.ModuleBase, $InstalledModuleInfo.Version)
                        Write-Error -Message $message -ErrorId "ModuleWithRequiredVersionAlreadyInstalled" -Category InvalidOperation

                        return
                    }
                }
                else
                {
                    if(-not $installUpdate)
                    {
                        if( (-not $MinimumVersion -and ($version -ne $InstalledModuleInfo.Version)) -or 
                            ($MinimumVersion -and ($MinimumVersion -gt $InstalledModuleInfo.Version)))
                        {
                            if($PSVersionTable.PSVersion -ge '5.0.0')
                            {
                                $message = $LocalizedData.ModuleAlreadyInstalledSxS -f ($InstalledModuleInfo.Version, $InstalledModuleInfo.Name, $InstalledModuleInfo.ModuleBase, $version, $InstalledModuleInfo.Version, $version)                            
                            }
                            else
                            {
                                $message = $LocalizedData.ModuleAlreadyInstalled -f ($InstalledModuleInfo.Version, $InstalledModuleInfo.Name, $InstalledModuleInfo.ModuleBase, $InstalledModuleInfo.Version, $version)
                            }
                            Write-Error -Message $message -ErrorId "ModuleAlreadyInstalled" -Category InvalidOperation
                        }
                        else
                        {
                            $message = $LocalizedData.ModuleAlreadyInstalledVerbose -f ($InstalledModuleInfo.Version, $InstalledModuleInfo.Name, $InstalledModuleInfo.ModuleBase)
                            Write-Verbose $message                
                        }

                        return
                    }
                    else
                    {
                        if($InstalledModuleInfo.Version -lt $version)
                        {
                            $message = $LocalizedData.FoundModuleUpdate -f ($InstalledModuleInfo.Name, $version)
                            Write-Verbose $message    
                        }
                        else
                        {
                            $message = $LocalizedData.NoUpdateAvailable -f ($InstalledModuleInfo.Name)
                            Write-Verbose $message
                            return
                        }
                    }
                }
            }
        }

        if($artfactType -eq $script:PSArtifactTypeScript)
        {
            # Test if script is already installed
            $InstalledScriptInfo = if(-not $IsSavePackage){ Test-ScriptInstalled -Name $packageName }

            Write-Debug "InstalledScriptInfo is $InstalledScriptInfo"

            if(-not $Force -and $InstalledScriptInfo)
            {
                if(-not $installUpdate)
                {
                    if( (-not $MinimumVersion -and ($version -ne $InstalledScriptInfo.Version)) -or 
                        ($MinimumVersion -and ($MinimumVersion -gt $InstalledScriptInfo.Version)))
                    {
                        $message = $LocalizedData.ScriptAlreadyInstalled -f ($InstalledScriptInfo.Version, $InstalledScriptInfo.Name, $InstalledScriptInfo.ScriptBase, $InstalledScriptInfo.Version, $version)
                        Write-Error -Message $message -ErrorId "ScriptAlreadyInstalled" -Category InvalidOperation
                    }
                    else
                    {
                        $message = $LocalizedData.ScriptAlreadyInstalledVerbose -f ($InstalledScriptInfo.Version, $InstalledScriptInfo.Name, $InstalledScriptInfo.ScriptBase)
                        Write-Verbose $message                
                    }

                    return
                }
                else
                {
                    if($InstalledScriptInfo.Version -lt $version)
                    {
                        $message = $LocalizedData.FoundScriptUpdate -f ($InstalledScriptInfo.Name, $version)
                        Write-Verbose $message
                    }
                    else
                    {
                        $message = $LocalizedData.NoScriptUpdateAvailable -f ($InstalledScriptInfo.Name)
                        Write-Verbose $message
                        return
                    }
                }
            }
            
            # Throw an error if there is a command with the same name and -force is not specified.
            if(-not $installUpdate -and 
               -not $IsSavePackage -and 
               -not $Force)
            {
                $cmd = Microsoft.PowerShell.Core\Get-Command -Name $packageName `
                                                             -ErrorAction SilentlyContinue `
                                                             -WarningAction SilentlyContinue
                if($cmd)
                {
                    $message = $LocalizedData.CommandAlreadyAvailable -f ($packageName)
                    Write-Error -Message $message -ErrorId CommandAlreadyAvailableWitScriptName -Category InvalidOperation
                    return
                }
            }
        }

        # create a temp folder and download the module
        $tempDestination = Microsoft.PowerShell.Management\Join-Path -Path $script:TempPath -ChildPath "$(Microsoft.PowerShell.Utility\Get-Random)"
        $null = Microsoft.PowerShell.Management\New-Item -Path $tempDestination -ItemType Directory -Force -Confirm:$false -WhatIf:$false

        try
        {
            $provider = $request.SelectProvider($providerName)
            if(-not $provider)
            {
                Write-Error -Message ($LocalizedData.PackageManagementProviderIsNotAvailable -f $providerName)

                return
            }

            if($request.IsCanceled)
            {
                return
            }

            Write-Verbose ($LocalizedData.SpecifiedLocationAndOGP -f ($provider.ProviderName, $providerName))

            $InstalledItemsList = $null
            $pkg = $script:FastPackRefHastable[$fastPackageReference]

            # If an item has dependencies, prepare the list of installed items and 
            # pass it to the NuGet provider to not download the already installed items.
            if($pkg.Dependencies.count -and 
               -not $IsSavePackage -and 
               -not $Force)
            {
                $InstalledItemsList = Microsoft.PowerShell.Core\Get-Module -ListAvailable | 
                                        Microsoft.PowerShell.Core\ForEach-Object {"$($_.Name)!#!$($_.Version)".ToLower()}

                if($artfactType -eq $script:PSArtifactTypeScript)
                {
                    $InstalledItemsList += $script:PSGetInstalledScripts.GetEnumerator() | 
                                               Microsoft.PowerShell.Core\ForEach-Object { 
                                                   "$($_.Value.PSGetItemInfo.Name)!#!$($_.Value.PSGetItemInfo.Version)".ToLower() 
                                               }
                }
                
                $InstalledItemsList | Select-Object -Unique -ErrorAction Ignore

                if($Debug)
                {
                    $InstalledItemsList | Microsoft.PowerShell.Core\ForEach-Object { Write-Debug -Message "Locally available Item: $_"}
                }
            } 
		
            $ProviderOptions = @{
                                    Destination=$tempDestination;
                                    ExcludeVersion=$true
                                }                                 

            if($InstalledItemsList)
            {
                $ProviderOptions['InstalledPackages'] = $InstalledItemsList
            }

            $newRequest = $request.CloneRequest( $ProviderOptions, @($SourceLocation), $request.Credential )

            if($artfactType -eq $script:PSArtifactTypeModule)
            {
                $message = $LocalizedData.DownloadingModuleFromGallery -f ($packageName, $version, $sourceLocation)
            }
            else
            {
                $message = $LocalizedData.DownloadingScriptFromGallery -f ($packageName, $version, $sourceLocation)
            }
            Write-Verbose $message

            $installedPkgs = $provider.InstallPackage($script:FastPackRefHastable[$fastPackageReference], $newRequest)

            foreach($pkg in $installedPkgs)
            {
                if($request.IsCanceled)
                {
                    return
                }

                $destinationModulePath = Microsoft.PowerShell.Management\Join-Path -Path $moduleDestination -ChildPath $pkg.Name

                # Side-by-Side module version is available on PowerShell 5.0 or later versions only
                # By default, PowerShell module versions will be installed/updated Side-by-Side.
                if(Test-ModuleSxSVersionSupport)
                {
                    $destinationModulePath = Microsoft.PowerShell.Management\Join-Path -Path $destinationModulePath -ChildPath $pkg.Version
                }

                $destinationscriptPath = $scriptDestination

                # Get actual artifact type from the package
                $packageType = $script:PSArtifactTypeModule
                $installLocation = $destinationModulePath
                $tempPackagePath = Microsoft.PowerShell.Management\Join-Path -Path $tempDestination -ChildPath $pkg.Name
                if(Microsoft.PowerShell.Management\Test-Path -Path $tempPackagePath)
                {
                    $packageFiles = Microsoft.PowerShell.Management\Get-ChildItem -Path $tempPackagePath -Recurse -Exclude "*.nupkg","*.nuspec"

                    if($packageFiles -and $packageFiles.GetType().ToString() -eq 'System.IO.FileInfo' -and $packageFiles.Name -eq "$($pkg.Name).ps1")
                    {
                        $packageType = $script:PSArtifactTypeScript
                        $installLocation = $destinationscriptPath
                    }
                }
                
                $AdditionalParams = @{}

                if(-not $IsSavePackage)
                {
                    # During the install operation:
                    #     InstalledDate should be the current Get-Date value
                    #     UpdatedDate should be null
                    #
                    # During the update operation:
                    #     InstalledDate should be from the previous version's InstalledDate otherwise current Get-Date value
                    #     UpdatedDate should be the current Get-Date value
                    #
                    $InstalledDate = Microsoft.PowerShell.Utility\Get-Date

                    if($installUpdate)
                    {
                        $AdditionalParams['UpdatedDate'] = Microsoft.PowerShell.Utility\Get-Date

                        $InstalledItemDetails = $null
                        if($packageType -eq $script:PSArtifactTypeModule)
                        {
                            $InstalledItemDetails = Get-InstalledModuleDetails -Name $pkg.Name | Select-Object -Last 1 -ErrorAction Ignore
                        }
                        elseif($packageType -eq $script:PSArtifactTypeScript)
                        {
                            $InstalledItemDetails = Get-InstalledScriptDetails -Name $pkg.Name | Select-Object -Last 1 -ErrorAction Ignore
                        }

                        if($InstalledItemDetails -and 
                           $InstalledItemDetails.PSGetItemInfo -and
                           (Get-Member -InputObject $InstalledItemDetails.PSGetItemInfo -Name 'InstalledDate') -and 
                           $InstalledItemDetails.PSGetItemInfo.InstalledDate)
                        {
                            $InstalledDate = $InstalledItemDetails.PSGetItemInfo.InstalledDate
                        }
                    }

                    $AdditionalParams['InstalledDate'] = $InstalledDate
                }

                $sid = New-SoftwareIdentityFromPackage -Package $pkg `
                                                       -SourceLocation $sourceLocation `
                                                       -PackageManagementProviderName $provider.ProviderName `
                                                       -Request $request `
                                                       -Type $packageType `
                                                       -InstalledLocation $installLocation `
                                                       @AdditionalParams

                # construct the PSGetItemInfo from SoftwareIdentity and persist it
                $psgItemInfo = New-PSGetItemInfo -SoftwareIdentity $pkg `
                                                 -PackageManagementProviderName $provider.ProviderName `
                                                 -SourceLocation $sourceLocation `
                                                 -Type $packageType `
                                                 -InstalledLocation $installLocation `
                                                 @AdditionalParams

                if($packageType -eq $script:PSArtifactTypeModule)
                {
                    if ($psgItemInfo.PowerShellGetFormatVersion -and 
                        ($script:SupportedPSGetFormatVersionMajors -notcontains $psgItemInfo.PowerShellGetFormatVersion.Major))
                    {
                        $message = $LocalizedData.NotSupportedPowerShellGetFormatVersion -f ($psgItemInfo.Name, $psgItemInfo.PowerShellGetFormatVersion, $psgItemInfo.Name)
                        Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                        continue
                    }
                
                    if(-not $psgItemInfo.PowerShellGetFormatVersion)
                    {
                        $sourceModulePath = Microsoft.PowerShell.Management\Join-Path $tempDestination $pkg.Name
                    }
                    else
                    {
                        $sourceModulePath = Microsoft.PowerShell.Management\Join-Path $tempDestination "$($pkg.Name)\Content\*\$script:ModuleReferences\$($pkg.Name)"
                    }

                    $CurrentModuleInfo = $null

                    # Validate the module
                    if(-not $IsSavePackage)
                    {
                        $CurrentModuleInfo = Test-ValidManifestModule -ModuleBasePath $sourceModulePath `
                                                                      -InstallLocation $InstallLocation `
                                                                      -AllowClobber:$AllowClobber `
                                                                      -SkipPublisherCheck:$SkipPublisherCheck `
                                                                      -IsUpdateOperation:$installUpdate

                        if(-not $CurrentModuleInfo)
                        {
                            # This Install-Package provider API gets called once per an item/package/SoftwareIdentity.
                            # Return if there is an error instead of continuing further to install the dependencies or current module.
                            #
                            return
                        }
                    }

                    # Test if module is already installed
                    $InstalledModuleInfo2 = if(-not $IsSavePackage){ Test-ModuleInstalled -Name $pkg.Name -RequiredVersion $pkg.Version }

                    if($pkg.Name -ne $packageName)
                    {
                        if(-not $Force -and $InstalledModuleInfo2)
                        {
                            if(Test-ModuleSxSVersionSupport)
                            {
                                if($pkg.version -eq $InstalledModuleInfo2.Version)
                                {
                                    if(-not $installUpdate)
                                    {
                                        $message = $LocalizedData.ModuleWithRequiredVersionAlreadyInstalled -f ($InstalledModuleInfo2.Version, $InstalledModuleInfo2.Name, $InstalledModuleInfo2.ModuleBase, $InstalledModuleInfo2.Version)
                                    }
                                    else
                                    {
                                        $message = $LocalizedData.NoUpdateAvailable -f ($pkg.Name)
                                    }

                                    Write-Verbose $message
                                    Continue
                                }
                            }
                            else
                            {
                                if(-not $installUpdate)
                                {
                                    $message = $LocalizedData.ModuleAlreadyInstalledVerbose -f ($InstalledModuleInfo2.Version, $InstalledModuleInfo2.Name, $InstalledModuleInfo2.ModuleBase)
                                    Write-Verbose $message
                                    Continue
                                }
                                else
                                {
                                    if($pkg.version -gt $InstalledModuleInfo2.Version)
                                    {
                                        $message = $LocalizedData.FoundModuleUpdate -f ($pkg.Name, $pkg.Version)
                                        Write-Verbose $message
                                    }
                                    else
                                    {
                                        $message = $LocalizedData.NoUpdateAvailable -f ($pkg.Name)
                                        Write-Verbose $message
                                        Continue
                                    }
                                }
                            }
                        }
                                    
                        if($IsSavePackage)
                        {
                            $DependencyInstallMessage = $LocalizedData.SavingDependencyModule -f ($pkg.Name, $pkg.Version, $packageName)
                        }
                        else
                        {
                            $DependencyInstallMessage = $LocalizedData.InstallingDependencyModule -f ($pkg.Name, $pkg.Version, $packageName)
                        }
                    
                        Write-Verbose  $DependencyInstallMessage
                    }

                    # check if module is in use
                    if($InstalledModuleInfo2)
                    {
                        $moduleInUse = Test-ModuleInUse -ModuleBasePath $InstalledModuleInfo2.ModuleBase `
                                                        -ModuleName $InstalledModuleInfo2.Name `
                                                        -ModuleVersion $InstalledModuleInfo2.Version `
                                                        -Verbose:$VerbosePreference `
                                                        -WarningAction $WarningPreference `
                                                        -ErrorAction $ErrorActionPreference `
                                                        -Debug:$DebugPreference
 
                        if($moduleInUse)
                        {
                            $message = $LocalizedData.ModuleIsInUse -f ($psgItemInfo.Name)
                            Write-Verbose $message
                            continue
                        }
                    }

                    Copy-Module -SourcePath $sourceModulePath -DestinationPath $destinationModulePath -PSGetItemInfo $psgItemInfo

                    if(-not $IsSavePackage)
                    {
                        # Write warning messages if externally managed module dependencies are not installed.
                        $ExternalModuleDependencies = Get-ExternalModuleDependencies -PSModuleInfo $CurrentModuleInfo
                        foreach($ExternalDependency in $ExternalModuleDependencies)
                        {
                            $depModuleInfo = Test-ModuleInstalled -Name $ExternalDependency

                            if(-not $depModuleInfo)
                            {
                                Write-Warning -Message ($LocalizedData.MissingExternallyManagedModuleDependency -f $ExternalDependency,$pkg.Name,$ExternalDependency)
                            }
                            else
                            {
                                Write-Verbose -Message ($LocalizedData.ExternallyManagedModuleDependencyIsInstalled -f $ExternalDependency)
                            }
                        }
                    }
                    
                    if($IsSavePackage)
                    {
                        $message = $LocalizedData.ModuleSavedSuccessfully -f ($psgItemInfo.Name, $installLocation)
                    }
                    else
                    {
                        $message = $LocalizedData.ModuleInstalledSuccessfully -f ($psgItemInfo.Name, $installLocation)
                    }                
                    Write-Verbose $message
                }


                if($packageType -eq $script:PSArtifactTypeScript)
                {
                    if ($psgItemInfo.PowerShellGetFormatVersion -and 
                        ($script:SupportedPSGetFormatVersionMajors -notcontains $psgItemInfo.PowerShellGetFormatVersion.Major))
                    {
                        $message = $LocalizedData.NotSupportedPowerShellGetFormatVersionScripts -f ($psgItemInfo.Name, $psgItemInfo.PowerShellGetFormatVersion, $psgItemInfo.Name)
                        Write-Error -Message $message -ErrorId "NotSupportedPowerShellGetFormatVersion" -Category InvalidOperation
                        continue
                    }

                    $sourceScriptPath = Microsoft.PowerShell.Management\Join-Path -Path $tempPackagePath -ChildPath "$($pkg.Name).ps1"
                    
                    $currentScriptInfo = $null
                    if(-not $IsSavePackage)
                    {
                        # Validate the script
                        $currentScriptInfo = Test-ScriptFileInfo -Path $sourceScriptPath -ErrorAction SilentlyContinue
                    
                        if(-not $currentScriptInfo)
                        {
                            $message = $LocalizedData.InvalidPowerShellScriptFile -f ($pkg.Name)
                            Write-Error -Message $message -ErrorId "InvalidPowerShellScriptFile" -Category InvalidOperation -TargetObject $pkg.Name
                            continue
                        }
                    }

                    # Test if script is already installed
                    $InstalledScriptInfo2 = if(-not $IsSavePackage){ Test-ScriptInstalled -Name $pkg.Name }

                    if($pkg.Name -ne $packageName)
                    {
                        if(-not $Force -and $InstalledScriptInfo2)
                        {
                            if(-not $installUpdate)
                            {
                                $message = $LocalizedData.ScriptAlreadyInstalledVerbose -f ($InstalledScriptInfo2.Version, $InstalledScriptInfo2.Name, $InstalledScriptInfo2.ScriptBase)
                                Write-Verbose $message
                                Continue
                            }
                            else
                            {
                                if($pkg.version -gt $InstalledScriptInfo2.Version)
                                {
                                    $message = $LocalizedData.FoundScriptUpdate -f ($pkg.Name, $pkg.Version)
                                    Write-Verbose $message
                                }
                                else
                                {
                                    $message = $LocalizedData.NoScriptUpdateAvailable -f ($pkg.Name)
                                    Write-Verbose $message
                                    Continue
                                }
                            }
                        }
                                    
                        if($IsSavePackage)
                        {
                            $DependencyInstallMessage = $LocalizedData.SavingDependencyScript -f ($pkg.Name, $pkg.Version, $packageName)
                        }
                        else
                        {
                            $DependencyInstallMessage = $LocalizedData.InstallingDependencyScript -f ($pkg.Name, $pkg.Version, $packageName)
                        }
                    
                        Write-Verbose  $DependencyInstallMessage
                    }

                    Write-Debug "SourceScriptPath is $sourceScriptPath and DestinationscriptPath is $destinationscriptPath"
                    Copy-ScriptFile -SourcePath $sourceScriptPath -DestinationPath $destinationscriptPath -PSGetItemInfo $psgItemInfo -Scope $Scope

                    if(-not $IsSavePackage)
                    {
                        # Write warning messages if externally managed module dependencies are not installed.
                        foreach($ExternalDependency in $currentScriptInfo.ExternalModuleDependencies)
                        {
                            $depModuleInfo = Test-ModuleInstalled -Name $ExternalDependency

                            if(-not $depModuleInfo)
                            {
                                Write-Warning -Message ($LocalizedData.ScriptMissingExternallyManagedModuleDependency -f $ExternalDependency,$pkg.Name,$ExternalDependency)
                            }
                            else
                            {
                                Write-Verbose -Message ($LocalizedData.ExternallyManagedModuleDependencyIsInstalled -f $ExternalDependency)
                            }
                        }

                        # Write warning messages if externally managed script dependencies are not installed.
                        foreach($ExternalDependency in $currentScriptInfo.ExternalScriptDependencies)
                        {
                            $depScriptInfo = Test-ScriptInstalled -Name $ExternalDependency

                            if(-not $depScriptInfo)
                            {
                                Write-Warning -Message ($LocalizedData.ScriptMissingExternallyManagedScriptDependency -f $ExternalDependency,$pkg.Name,$ExternalDependency)
                            }
                            else
                            {
                                Write-Verbose -Message ($LocalizedData.ScriptExternallyManagedScriptDependencyIsInstalled -f $ExternalDependency)
                            }
                        }
                    }
                                    
                    # Remove the old scriptfile if it's path different from the required destination script path when -Force is specified
                    if($Force -and 
                        $InstalledScriptInfo2 -and
                        -not $destinationscriptPath.StartsWith($InstalledScriptInfo2.ScriptBase, [System.StringComparison]::OrdinalIgnoreCase))
                    {
                        Microsoft.PowerShell.Management\Remove-Item -Path $InstalledScriptInfo2.Path `
                                                                    -Force `
                                                                    -ErrorAction SilentlyContinue `
                                                                    -WarningAction SilentlyContinue `
                                                                    -Confirm:$false -WhatIf:$false
                    }

                    if($IsSavePackage)
                    {
                        $message = $LocalizedData.ScriptSavedSuccessfully -f ($psgItemInfo.Name, $installLocation)
                    }
                    else
                    {
                        $message = $LocalizedData.ScriptInstalledSuccessfully -f ($psgItemInfo.Name, $installLocation)
                    }                
                    Write-Verbose $message
                }

                Write-Output -InputObject $sid
            }
        }
        finally
        {
            Microsoft.PowerShell.Management\Remove-Item $tempDestination -Force -Recurse -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
        }
    }
}

function Uninstall-Package
{ 
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $fastPackageReference
    )

    Write-Debug -Message ($LocalizedData.ProviderApiDebugMessage -f ('Uninstall-Package'))

    Write-Debug -Message ($LocalizedData.FastPackageReference -f $fastPackageReference)
    
    # take the fastPackageReference and get the package object again.
    $parts = $fastPackageReference -Split '[|]'
    $Force = $false

    $options = $request.Options
    if($options)
    {
        foreach( $o in $options.Keys )
        {
            Write-Debug -Message ("OPTION: {0} => {1}" -f ($o, $request.Options[$o]) )
        }
    }

    if($parts.Length -eq 5)
    {
        $providerName = $parts[0]
        $packageName = $parts[1]
        $version = $parts[2]
        $sourceLocation= $parts[3]
        $artfactType = $parts[4]

        if($request.IsCanceled)
        {
            return
        }
        
        if($options.ContainsKey('Force'))
        {
            $Force = $options['Force']

            if($Force.GetType().ToString() -eq 'System.String')
            {
                if($Force -eq 'false')
                {
                    $Force = $false
                }
                elseif($Force -eq 'true')
                {
                    $Force = $true
                }
            }
        }

        if($artfactType -eq $script:PSArtifactTypeModule)
        {
            $moduleName = $packageName
            $InstalledModuleInfo = $script:PSGetInstalledModules["$($moduleName)$($version)"] 

            if(-not $InstalledModuleInfo)
            {
                $message = $LocalizedData.ModuleUninstallationNotPossibleAsItIsNotInstalledUsingPowerShellGet -f $moduleName

                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "ModuleUninstallationNotPossibleAsItIsNotInstalledUsingPowerShellGet" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
            
                return
            }

            $moduleBase = $InstalledModuleInfo.PSGetItemInfo.InstalledLocation

            if(-not (Test-RunningAsElevated) -and $moduleBase.StartsWith($script:programFilesModulesPath, [System.StringComparison]::OrdinalIgnoreCase))
            {                            
                $message = $LocalizedData.AdminPrivilegesRequiredForUninstall -f ($moduleName, $moduleBase)

                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "AdminPrivilegesRequiredForUninstall" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation

                return
            }

            $dependentModuleScript = {
                                param ([string] $moduleName)
                                Microsoft.PowerShell.Core\Get-Module -ListAvailable | 
                                Microsoft.PowerShell.Core\Where-Object {                            
                                    ($moduleName -ne $_.Name) -and (
                                    ($_.RequiredModules -and $_.RequiredModules.Name -contains $moduleName) -or
                                    ($_.NestedModules -and $_.NestedModules.Name -contains $moduleName))
                                }
                            }
            $dependentModulesJob =  Microsoft.PowerShell.Core\Start-Job -ScriptBlock $dependentModuleScript -ArgumentList $moduleName
            Microsoft.PowerShell.Core\Wait-Job -job $dependentModulesJob
            $dependentModules = Microsoft.PowerShell.Core\Receive-Job -job $dependentModulesJob -ErrorAction Ignore

            if(-not $Force -and $dependentModules)
            {
                $message = $LocalizedData.UnableToUninstallAsOtherModulesNeedThisModule -f ($moduleName, $version, $moduleBase, $(($dependentModules.Name | Select-Object -Unique -ErrorAction Ignore) -join ','), $moduleName)

                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "UnableToUninstallAsOtherModulesNeedThisModule" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation

                return
            }

            $moduleInUse = Test-ModuleInUse -ModuleBasePath $moduleBase `
                                            -ModuleName $InstalledModuleInfo.PSGetItemInfo.Name`
                                            -ModuleVersion $InstalledModuleInfo.PSGetItemInfo.Version `
                                            -Verbose:$VerbosePreference `
                                            -WarningAction $WarningPreference `
                                            -ErrorAction $ErrorActionPreference `
                                            -Debug:$DebugPreference

            if($moduleInUse)
            {
                $message = $LocalizedData.ModuleIsInUse -f ($moduleName)

                ThrowError -ExceptionName "System.InvalidOperationException" `
                           -ExceptionMessage $message `
                           -ErrorId "ModuleIsInUse" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation

                return
            }

            $ModuleBaseFolderToBeRemoved = $moduleBase

            # With SxS version support, more than one version of the module can be installed.
            # - Remove the parent directory of the module version base when only one version is installed
            # - Don't remove the modulebase when it was installed before SxS version support and 
            #   other versions are installed under the module base folder
            # 
            if(Test-ModuleSxSVersionSupport)
            {
                $ModuleBaseWithoutVersion = $moduleBase
                $IsModuleInstalledAsSxSVersion = $false

                if($moduleBase.EndsWith("$version", [System.StringComparison]::OrdinalIgnoreCase))
                {
                    $IsModuleInstalledAsSxSVersion = $true
                    $ModuleBaseWithoutVersion = Microsoft.PowerShell.Management\Split-Path -Path $moduleBase -Parent
                }

                $InstalledVersionsWithSameModuleBase = @()
                Get-Module -Name $moduleName -ListAvailable | 
                    Microsoft.PowerShell.Core\ForEach-Object {
                        if($_.ModuleBase.StartsWith($ModuleBaseWithoutVersion, [System.StringComparison]::OrdinalIgnoreCase))
                        {
                            $InstalledVersionsWithSameModuleBase += $_.ModuleBase
                        }
                    }

                # Remove ..\ModuleName directory when only one module is installed with the same ..\ModuleName path 
                # like ..\ModuleName\1.0 or ..\ModuleName
                if($InstalledVersionsWithSameModuleBase.Count -eq 1)
                {
                    $ModuleBaseFolderToBeRemoved = $ModuleBaseWithoutVersion
                }
                elseif($ModuleBaseWithoutVersion -eq $moduleBase)
                {
                    # There are version specific folders under the same module base dir
                    # Throw an error saying uninstall other versions then uninstall this current version
                    $message = $LocalizedData.UnableToUninstallModuleVersion -f ($moduleName, $version, $moduleBase)

                    ThrowError -ExceptionName "System.InvalidOperationException" `
                               -ExceptionMessage $message `
                               -ErrorId "UnableToUninstallModuleVersion" `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation

                    return
                }
                # Otherwise specified version folder will be removed as current module base is assigned to $ModuleBaseFolderToBeRemoved
            }

            Microsoft.PowerShell.Management\Remove-Item -Path $ModuleBaseFolderToBeRemoved `
                                                        -Force -Recurse `
                                                        -ErrorAction SilentlyContinue `
                                                        -WarningAction SilentlyContinue `
                                                        -Confirm:$false -WhatIf:$false        
                                                    
            $message = $LocalizedData.ModuleUninstallationSucceeded -f $moduleName, $moduleBase
            Write-Verbose  $message       

            Write-Output -InputObject $InstalledModuleInfo.SoftwareIdentity
        }
        elseif($artfactType -eq $script:PSArtifactTypeScript)
        {
            $scriptName = $packageName
            $InstalledScriptInfo = $script:PSGetInstalledScripts["$($scriptName)$($version)"] 

            if(-not $InstalledScriptInfo)
            {
                $message = $LocalizedData.ScriptUninstallationNotPossibleAsItIsNotInstalledUsingPowerShellGet -f $scriptName
                ThrowError -ExceptionName "System.ArgumentException" `
                           -ExceptionMessage $message `
                           -ErrorId "ScriptUninstallationNotPossibleAsItIsNotInstalledUsingPowerShellGet" `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
            
                return
            }

            $scriptBase = $InstalledScriptInfo.PSGetItemInfo.InstalledLocation
            $installedScriptInfoPath = $script:MyDocumentsInstalledScriptInfosPath

            if($scriptBase.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                if(-not (Test-RunningAsElevated))
                {                            
                    $message = $LocalizedData.AdminPrivilegesRequiredForScriptUninstall -f ($scriptName, $scriptBase)

                    ThrowError -ExceptionName "System.InvalidOperationException" `
                               -ExceptionMessage $message `
                               -ErrorId "AdminPrivilegesRequiredForUninstall" `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation

                    return
                }

                $installedScriptInfoPath = $script:ProgramFilesInstalledScriptInfosPath
            }

            # Check if there are any dependent scripts
            $dependentScriptDetails = $script:PSGetInstalledScripts.Values | 
                                          Microsoft.PowerShell.Core\Where-Object {
                                              $_.PSGetItemInfo.Dependencies -contains $scriptName
                                          }

            $dependentScriptNames = $dependentScriptDetails | 
                                        Microsoft.PowerShell.Core\ForEach-Object { $_.PSGetItemInfo.Name }

            if(-not $Force -and $dependentScriptNames)
            {
                $message = $LocalizedData.UnableToUninstallAsOtherScriptsNeedThisScript -f 
                               ($scriptName, 
                                $version, 
                                $scriptBase, 
                                $(($dependentScriptNames | Select-Object -Unique -ErrorAction Ignore) -join ','), 
                                $scriptName)

                ThrowError -ExceptionName 'System.InvalidOperationException' `
                           -ExceptionMessage $message `
                           -ErrorId 'UnableToUninstallAsOtherScriptsNeedThisScript' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation
                return
            }

            $scriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $scriptBase `
                                                                        -ChildPath "$($scriptName).ps1"

            $installledScriptInfoFilePath = Microsoft.PowerShell.Management\Join-Path -Path $installedScriptInfoPath `
                                                                                      -ChildPath "$($scriptName)_$($script:InstalledScriptInfoFileName)" 

            # Remove the script file and it's corresponding InstalledScriptInfo.xml
            if(Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf)
            {
                Microsoft.PowerShell.Management\Remove-Item -Path $scriptFilePath `
                                                            -Force `
                                                            -ErrorAction SilentlyContinue `
                                                            -WarningAction SilentlyContinue `
                                                            -Confirm:$false -WhatIf:$false
            }

            if(Microsoft.PowerShell.Management\Test-Path -Path $installledScriptInfoFilePath -PathType Leaf)
            {
                Microsoft.PowerShell.Management\Remove-Item -Path $installledScriptInfoFilePath `
                                                            -Force `
                                                            -ErrorAction SilentlyContinue `
                                                            -WarningAction SilentlyContinue `
                                                            -Confirm:$false -WhatIf:$false
            }

            $message = $LocalizedData.ScriptUninstallationSucceeded -f $scriptName, $scriptBase
            Write-Verbose $message

            Write-Output -InputObject $InstalledScriptInfo.SoftwareIdentity
        }
    }
}

function Get-InstalledPackage
{ 
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [string]
        $Name,

        [Parameter()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [Version]
        $MaximumVersion
    )

    Write-Debug -Message ($LocalizedData.ProviderApiDebugMessage -f ('Get-InstalledPackage'))

    $options = $request.Options

    foreach( $o in $options.Keys )
    {
        Write-Debug ( "OPTION: {0} => {1}" -f ($o, $options[$o]) )
    }

    $artifactTypes = $script:PSArtifactTypeModule
    if($options.ContainsKey($script:PSArtifactType))
    {
        $artifactTypes = $options[$script:PSArtifactType]
    }

    if($artifactTypes -eq $script:All)
    {
        $artifactTypes = @($script:PSArtifactTypeModule,$script:PSArtifactTypeScript)
    }

    if($artifactTypes -contains $script:PSArtifactTypeModule)
    {
        Get-InstalledModuleDetails -Name $Name `
                                   -RequiredVersion $RequiredVersion `
                                   -MinimumVersion $MinimumVersion `
                                   -MaximumVersion $MaximumVersion | Microsoft.PowerShell.Core\ForEach-Object {$_.SoftwareIdentity}
    }

    if($artifactTypes -contains $script:PSArtifactTypeScript)
    {
        Get-InstalledScriptDetails -Name $Name `
                                   -RequiredVersion $RequiredVersion `
                                   -MinimumVersion $MinimumVersion `
                                   -MaximumVersion $MaximumVersion | Microsoft.PowerShell.Core\ForEach-Object {$_.SoftwareIdentity}
    }
}

#endregion

#region Internal Utility functions for the PackageManagement Provider Implementation

function Set-InstalledScriptsVariable
{
    # Initialize list of scripts installed by the PowerShellGet provider
    $script:PSGetInstalledScripts = [ordered]@{}    
    $scriptPaths = @($script:ProgramFilesInstalledScriptInfosPath, $script:MyDocumentsInstalledScriptInfosPath)

    foreach ($location in $scriptPaths)
    {
        # find all scripts installed using PowerShellGet
        $scriptInfoFiles = Get-ChildItem -Path $location `
                                         -Filter "*$script:InstalledScriptInfoFileName" `
                                         -ErrorAction SilentlyContinue `
                                         -WarningAction SilentlyContinue

        if($scriptInfoFiles)
        {
            foreach ($scriptInfoFile in $scriptInfoFiles)
            {
                $psgetItemInfo = DeSerialize-PSObject -Path $scriptInfoFile.FullName

                $scriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $psgetItemInfo.InstalledLocation `
                                                                            -ChildPath "$($psgetItemInfo.Name).ps1"

                # Remove the InstalledScriptInfo.xml file if the actual script file was manually uninstalled by the user
                if(-not (Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf))
                {
                    Microsoft.PowerShell.Management\Remove-Item -Path $scriptInfoFile.FullName -Force -ErrorAction SilentlyContinue

                    continue
                }

                $package = New-SoftwareIdentityFromPSGetItemInfo -PSGetItemInfo $psgetItemInfo

                if($package)
                {
                    $script:PSGetInstalledScripts["$($psgetItemInfo.Name)$($psgetItemInfo.Version)"] = @{
                                                                                                            SoftwareIdentity = $package
                                                                                                            PSGetItemInfo = $psgetItemInfo
                                                                                                        }
                }
            }
        }
    }
}

function Get-InstalledScriptDetails
{ 
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [string]
        $Name,

        [Parameter()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [Version]
        $MaximumVersion
    )

    Set-InstalledScriptsVariable

    # Keys in $script:PSGetInstalledScripts are "<ScriptName><ScriptVersion>", 
    # first filter the installed scripts using "$Name*" wildcard search
    # then apply $Name wildcard search to get the script name which meets the specified name with wildcards.
    #
    $wildcardPattern = New-Object System.Management.Automation.WildcardPattern "$Name*",$script:wildcardOptions
    $nameWildcardPattern = New-Object System.Management.Automation.WildcardPattern $Name,$script:wildcardOptions

    $script:PSGetInstalledScripts.GetEnumerator() | Microsoft.PowerShell.Core\ForEach-Object {
                                                        if($wildcardPattern.IsMatch($_.Key))
                                                        {
                                                            $InstalledScriptDetails = $_.Value

                                                            if(-not $Name -or $nameWildcardPattern.IsMatch($InstalledScriptDetails.PSGetItemInfo.Name))
                                                            {
                                                                if($RequiredVersion)
                                                                {
                                                                   if($RequiredVersion -eq $InstalledScriptDetails.PSGetItemInfo.Version)
                                                                   {
                                                                       $InstalledScriptDetails
                                                                   }
                                                                }
                                                                else
                                                                {
                                                                    if( (-not $MinimumVersion -or ($MinimumVersion -le $InstalledScriptDetails.PSGetItemInfo.Version)) -and 
                                                                        (-not $MaximumVersion -or ($MaximumVersion -ge $InstalledScriptDetails.PSGetItemInfo.Version)))
                                                                    {
                                                                        $InstalledScriptDetails
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
}

function Get-InstalledModuleDetails
{ 
    [CmdletBinding()]
    param
    (
        [Parameter()]
        [string]
        $Name,

        [Parameter()]
        [Version]
        $RequiredVersion,

        [Parameter()]
        [Version]
        $MinimumVersion,

        [Parameter()]
        [Version]
        $MaximumVersion
    )

    Set-InstalledModulesVariable

    # Keys in $script:PSGetInstalledModules are "<ModuleName><ModuleVersion>", 
    # first filter the installed modules using "$Name*" wildcard search
    # then apply $Name wildcard search to get the module name which meets the specified name with wildcards.
    #
    $wildcardPattern = New-Object System.Management.Automation.WildcardPattern "$Name*",$script:wildcardOptions
    $nameWildcardPattern = New-Object System.Management.Automation.WildcardPattern $Name,$script:wildcardOptions

    $script:PSGetInstalledModules.GetEnumerator() | Microsoft.PowerShell.Core\ForEach-Object {
                                                        if($wildcardPattern.IsMatch($_.Key))
                                                        {
                                                            $InstalledModuleDetails = $_.Value

                                                            if(-not $Name -or $nameWildcardPattern.IsMatch($InstalledModuleDetails.PSGetItemInfo.Name))
                                                            {
                                                                if($RequiredVersion)
                                                                {
                                                                   if($RequiredVersion -eq $InstalledModuleDetails.PSGetItemInfo.Version)
                                                                   {
                                                                       $InstalledModuleDetails
                                                                   }
                                                                }
                                                                else
                                                                {
                                                                    if( (-not $MinimumVersion -or ($MinimumVersion -le $InstalledModuleDetails.PSGetItemInfo.Version)) -and 
                                                                        (-not $MaximumVersion -or ($MaximumVersion -ge $InstalledModuleDetails.PSGetItemInfo.Version)))
                                                                    {
                                                                        $InstalledModuleDetails
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
}

function New-SoftwareIdentityFromPackage
{
    param
    (
        [Parameter(Mandatory=$true)]
        $Package,

        [Parameter(Mandatory=$true)]
        [string]
        $PackageManagementProviderName,

        [Parameter(Mandatory=$true)]
        [string]
        $SourceLocation,

        [Parameter()]
        [switch]
        $IsFromTrustedSource,

        [Parameter(Mandatory=$true)]
        $request,

        [Parameter(Mandatory=$true)]
        [string]
        $Type,

        [Parameter()]
        [string]
        $InstalledLocation,

        [Parameter()]
        [System.DateTime]
        $InstalledDate,

        [Parameter()]
        [System.DateTime]
        $UpdatedDate
    )

    $fastPackageReference = New-FastPackageReference -ProviderName $PackageManagementProviderName `
                                                     -PackageName $Package.Name `
                                                     -Version $Package.Version `
                                                     -Source $SourceLocation `
                                                     -ArtifactType $Type

    $links = New-Object -TypeName  System.Collections.ArrayList
    foreach($lnk in $Package.Links)
    {
        if( $lnk.Relationship -eq "icon" -or $lnk.Relationship -eq "license" -or $lnk.Relationship -eq "project" )
        {
            $links.Add( (New-Link -Href $lnk.HRef -RelationShip $lnk.Relationship )  )
        }
    }

    $entities = New-Object -TypeName  System.Collections.ArrayList
    foreach( $entity in $Package.Entities )
    {
        if( $entity.Role -eq "author" -or $entity.Role -eq "owner" )
        {
            $entities.Add( (New-Entity -Name $entity.Name -Role $entity.Role -RegId $entity.RegId -Thumbprint $entity.Thumbprint)  )
        }
    }

    $deps = (new-Object -TypeName  System.Collections.ArrayList)
    foreach( $dep in $pkg.Dependencies ) 
    {
        # Add each dependency and say it's from this provider.
        $newDep = New-Dependency -ProviderName $script:PSModuleProviderName `
                                 -PackageName $request.Services.ParsePackageName($dep) `
                                 -Version $request.Services.ParsePackageVersion($dep) `
                                 -Source $SourceLocation

        $deps.Add( $newDep )
    }


    $details =  New-Object -TypeName  System.Collections.Hashtable
	
	foreach ( $key in $Package.Metadata.Keys.LocalName)
	{
		if (!$details.ContainsKey($key))
		{
			$details.Add($key, (Get-First $Package.Metadata[$key]) )
		}
	}
	
    $details.Add( "PackageManagementProvider" , $PackageManagementProviderName )

    if($InstalledLocation)
    {
        $details.Add( $script:InstalledLocation , $InstalledLocation )
    }

    if($InstalledDate)
    {
        $details.Add( 'installeddate' , $InstalledDate.ToString() )
    }

    if($UpdatedDate)
    {
        $details.Add( 'updateddate' , $UpdatedDate.ToString() )
    }

    # Initialize package source name to the source location
    $sourceNameForSoftwareIdentity = $SourceLocation

    $sourceName = (Get-SourceName -Location $SourceLocation)
    
    if($sourceName)
    {
        $details.Add( "SourceName" , $sourceName )

        # Override the source name only if we are able to map source location to source name
        $sourceNameForSoftwareIdentity = $sourceName
    }

    $params = @{FastPackageReference = $fastPackageReference;
                Name = $Package.Name;
                Version = $Package.Version;
                versionScheme  = "MultiPartNumeric";
                Source = $sourceNameForSoftwareIdentity;
                Summary = $Package.Summary;
                SearchKey = $Package.Name;
                FullPath = $Package.FullPath;
                FileName = $Package.Name;
                Details = $details;
                Entities = $entities;
                Links = $links;
                Dependencies = $deps;
               }

    if($IsFromTrustedSource)
    {
        $params["FromTrustedSource"] = $true
    }

    $sid = New-SoftwareIdentity @params

    return $sid
}

function New-PackageSourceFromModuleSource
{
    param
    (
        [Parameter(Mandatory=$true)]
        $ModuleSource
    )

    $ScriptSourceLocation = $null
    if(Get-Member -InputObject $ModuleSource -Name $script:ScriptSourceLocation)
    {
        $ScriptSourceLocation = $ModuleSource.ScriptSourceLocation
    }

    $ScriptPublishLocation = $ModuleSource.PublishLocation
    if(Get-Member -InputObject $ModuleSource -Name $script:ScriptPublishLocation)
    {
        $ScriptPublishLocation = $ModuleSource.ScriptPublishLocation
    }

    $packageSourceDetails = @{}
    $packageSourceDetails["InstallationPolicy"] = $ModuleSource.InstallationPolicy
    $packageSourceDetails["PackageManagementProvider"] = (Get-ProviderName -PSCustomObject $ModuleSource)
    $packageSourceDetails[$script:PublishLocation] = $ModuleSource.PublishLocation
    $packageSourceDetails[$script:ScriptSourceLocation] = $ScriptSourceLocation
    $packageSourceDetails[$script:ScriptPublishLocation] = $ScriptPublishLocation

    $ModuleSource.ProviderOptions.GetEnumerator() | Microsoft.PowerShell.Core\ForEach-Object {
                                                        $packageSourceDetails[$_.Key] = $_.Value
                                                    }

    # create a new package source
    $src =  New-PackageSource -Name $ModuleSource.Name `
                              -Location $ModuleSource.SourceLocation `
                              -Trusted $ModuleSource.Trusted `
                              -Registered $ModuleSource.Registered `
                              -Details $packageSourceDetails

    Write-Verbose ( $LocalizedData.RepositoryDetails -f ($src.Name, $src.Location, $src.IsTrusted, $src.IsRegistered) )

    # return the package source object.
    Write-Output -InputObject $src
}

function New-ModuleSourceFromPackageSource
{
    param
    (
        [Parameter(Mandatory=$true)]
        $PackageSource
    )

    $moduleSource = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
            Name = $PackageSource.Name
            SourceLocation =  $PackageSource.Location
            Trusted=$PackageSource.IsTrusted
            Registered=$PackageSource.IsRegistered
            InstallationPolicy = $PackageSource.Details['InstallationPolicy']
            PackageManagementProvider=$PackageSource.Details['PackageManagementProvider']
            PublishLocation=$PackageSource.Details[$script:PublishLocation]
            ScriptSourceLocation=$PackageSource.Details[$script:ScriptSourceLocation]
            ScriptPublishLocation=$PackageSource.Details[$script:ScriptPublishLocation]
            ProviderOptions = @{}
        })

    $PackageSource.Details.GetEnumerator() | Microsoft.PowerShell.Core\ForEach-Object {
                                                if($_.Key -ne 'PackageManagementProvider' -and 
                                                   $_.Key -ne $script:PublishLocation -and
                                                   $_.Key -ne $script:ScriptPublishLocation -and
                                                   $_.Key -ne $script:ScriptSourceLocation -and
                                                   $_.Key -ne 'InstallationPolicy')
                                                {
                                                    $moduleSource.ProviderOptions[$_.Key] = $_.Value
                                                }
                                             }

    $moduleSource.PSTypeNames.Insert(0, "Microsoft.PowerShell.Commands.PSRepository")

    # return the module source object.
    Write-Output -InputObject $moduleSource
}

function New-FastPackageReference
{
    param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $ProviderName,
		
        [Parameter(Mandatory=$true)]
        [string]
        $PackageName,

        [Parameter(Mandatory=$true)]
        [string]
        $Version,

        [Parameter(Mandatory=$true)]
        [string]
        $Source,

        [Parameter(Mandatory=$true)]
        [string]
        $ArtifactType
    )

    return "$ProviderName|$PackageName|$Version|$Source|$ArtifactType"
}

function Get-First 
{
    param
    (
        [Parameter(Mandatory=$true)]
        $IEnumerator
    ) 

    foreach($item in $IEnumerator)
    {
        return $item
    }

    return $null
}

function Set-InstalledModulesVariable
{
    # Initialize list of modules installed by the PowerShellGet provider
    $script:PSGetInstalledModules = [ordered]@{}

    $modulePaths = @($script:ProgramFilesModulesPath, $script:MyDocumentsModulesPath)
    
    foreach ($location in $modulePaths)
    {
        # find all modules installed using PowerShellGet
        $GetChildItemParams = @{
            Path = $location
            Recurse = $true
            Filter = $script:PSGetItemInfoFileName
            ErrorAction = 'SilentlyContinue'
            WarningAction = 'SilentlyContinue'
        }

        if(IsWindows)
        {
            $GetChildItemParams['Attributes'] = 'Hidden'
        }

        $moduleBases = Get-ChildItem @GetChildItemParams | Foreach-Object { $_.Directory }

        
        foreach ($moduleBase in $moduleBases)
        {
            $PSGetItemInfoPath = Microsoft.PowerShell.Management\Join-Path $moduleBase.FullName $script:PSGetItemInfoFileName

            # Check if this module got installed using PSGet, read its contents to create a SoftwareIdentity object
            if (Microsoft.PowerShell.Management\Test-Path $PSGetItemInfoPath)
            {
                $psgetItemInfo = DeSerialize-PSObject -Path $PSGetItemInfoPath

                # Add InstalledLocation if this module was installed with older version of PowerShellGet
                if(-not (Get-Member -InputObject $psgetItemInfo -Name $script:InstalledLocation))
                {
                    Microsoft.PowerShell.Utility\Add-Member -InputObject $psgetItemInfo `
                                                            -MemberType NoteProperty `
                                                            -Name $script:InstalledLocation `
                                                            -Value $moduleBase.FullName
                }

                $package = New-SoftwareIdentityFromPSGetItemInfo -PSGetItemInfo $psgetItemInfo

                if($package)
                {
                    $script:PSGetInstalledModules["$($psgetItemInfo.Name)$($psgetItemInfo.Version)"] = @{
                                                                                                            SoftwareIdentity = $package
                                                                                                            PSGetItemInfo = $psgetItemInfo
                                                                                                        }
                }
            }
        }
    }
}

function New-SoftwareIdentityFromPSGetItemInfo
{
    param
    (
        [Parameter(Mandatory=$true)]
        $PSGetItemInfo
    )

    $SourceLocation = $psgetItemInfo.RepositorySourceLocation

    if(Get-Member -InputObject $PSGetItemInfo -Name $script:PSArtifactType)
    {
        $artifactType = $psgetItemInfo.Type
    }
    else
    {
        $artifactType = $script:PSArtifactTypeModule
    }

    $fastPackageReference = New-FastPackageReference -ProviderName (Get-ProviderName -PSCustomObject $psgetItemInfo) `
                                                     -PackageName $psgetItemInfo.Name `
                                                     -Version $psgetItemInfo.Version `
                                                     -Source $SourceLocation `
                                                     -ArtifactType $artifactType

    $links = New-Object -TypeName  System.Collections.ArrayList
    if($psgetItemInfo.IconUri)
    {
        $links.Add( (New-Link -Href $psgetItemInfo.IconUri -RelationShip "icon") )
    }
    
    if($psgetItemInfo.LicenseUri)
    {
        $links.Add( (New-Link -Href $psgetItemInfo.LicenseUri -RelationShip "license") )
    }

    if($psgetItemInfo.ProjectUri)
    {
        $links.Add( (New-Link -Href $psgetItemInfo.ProjectUri -RelationShip "project") )
    }
    
    $entities = New-Object -TypeName  System.Collections.ArrayList
    if($psgetItemInfo.Author -and $psgetItemInfo.Author.ToString())
    {
        $entities.Add( (New-Entity -Name $psgetItemInfo.Author -Role 'author') )
    }

    if($psgetItemInfo.CompanyName -and $psgetItemInfo.CompanyName.ToString())
    {
        $entities.Add( (New-Entity -Name $psgetItemInfo.CompanyName -Role 'owner') )
    }

    $details =  @{
                    description    = $psgetItemInfo.Description
                    copyright      = $psgetItemInfo.Copyright
                    published      = $psgetItemInfo.PublishedDate.ToString()
                    installeddate  = $null
                    updateddate    = $null
                    tags           = $psgetItemInfo.Tags
                    releaseNotes   = $psgetItemInfo.ReleaseNotes
                    PackageManagementProvider = (Get-ProviderName -PSCustomObject $psgetItemInfo)
                 }

    if((Get-Member -InputObject $psgetItemInfo -Name 'InstalledDate') -and $psgetItemInfo.InstalledDate)
    {
        $details['installeddate'] = $psgetItemInfo.InstalledDate.ToString()
    }

    if((Get-Member -InputObject $psgetItemInfo -Name 'UpdatedDate') -and $psgetItemInfo.UpdatedDate)
    {
        $details['updateddate'] = $psgetItemInfo.UpdatedDate.ToString()
    }

    if(Get-Member -InputObject $psgetItemInfo -Name $script:InstalledLocation)
    {
        $details[$script:InstalledLocation] = $psgetItemInfo.InstalledLocation
    }

    $details[$script:PSArtifactType] = $artifactType

    $sourceName = Get-SourceName -Location $SourceLocation
    if($sourceName)
    {
        $details["SourceName"] = $sourceName
    }

    $params = @{
                FastPackageReference = $fastPackageReference;
                Name = $psgetItemInfo.Name;
                Version = $psgetItemInfo.Version;
                versionScheme  = "MultiPartNumeric";
                Source = $SourceLocation;
                Summary = $psgetItemInfo.Description;
                Details = $details;
                Entities = $entities;
                Links = $links
               }

    if($sourceName -and $script:PSGetModuleSources[$sourceName].Trusted)
    {
        $params["FromTrustedSource"] = $true
    }

    $sid = New-SoftwareIdentity @params

    return $sid
}

#endregion

#region Common functions

function Get-EnvironmentVariable
{
    param
    (
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name, 

        [parameter(Mandatory = $true)]
        [int]
        $Target
    )

    if ($Target -eq $script:EnvironmentVariableTarget.Process) 
    {
        return [System.Environment]::GetEnvironmentVariable($Name)
    }
    elseif ($Target -eq $script:EnvironmentVariableTarget.Machine)
    {
        $itemPropertyValue = Microsoft.PowerShell.Management\Get-ItemProperty -Path $script:SystemEnvironmentKey -Name $Name -ErrorAction SilentlyContinue

        if($itemPropertyValue)
        {
            return $itemPropertyValue.$Name
        }
    }
    elseif ($Target -eq $script:EnvironmentVariableTarget.User)
    {
        $itemPropertyValue = Microsoft.PowerShell.Management\Get-ItemProperty -Path $script:UserEnvironmentKey -Name $Name -ErrorAction SilentlyContinue

        if($itemPropertyValue)
        {
            return $itemPropertyValue.$Name
        }
    }
}

function Set-EnvironmentVariable
{
    param
    (
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [String]
        $Name, 

        [parameter()]
        [String]
        $Value,

        [parameter(Mandatory = $true)]
        [int]
        $Target
    )

    if ($Target -eq $script:EnvironmentVariableTarget.Process) 
    {
        [System.Environment]::SetEnvironmentVariable($Name, $Value)

        return
    }
    elseif ($Target -eq $script:EnvironmentVariableTarget.Machine) 
    {
        if ($Name.Length -ge $script:SystemEnvironmentVariableMaximumLength)
        {
            $message = $LocalizedData.InvalidEnvironmentVariableName -f ($Name, $script:SystemEnvironmentVariableMaximumLength)
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId 'InvalidEnvironmentVariableName' `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $Name
            return
        }

        $Path = $script:SystemEnvironmentKey
    }
    elseif ($Target -eq $script:EnvironmentVariableTarget.User) 
    {
        if ($Name.Length -ge $script:UserEnvironmentVariableMaximumLength)
        {
            $message = $LocalizedData.InvalidEnvironmentVariableName -f ($Name, $script:UserEnvironmentVariableMaximumLength)
            ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId 'InvalidEnvironmentVariableName' `
                        -ErrorCategory InvalidArgument `
                        -ExceptionObject $Name
            return
        }

        $Path = $script:UserEnvironmentKey
    }

    if (!$Value) 
    {
        Microsoft.PowerShell.Management\Remove-ItemProperty $Path -Name $Name -ErrorAction SilentlyContinue
    }
    else 
    {
        Microsoft.PowerShell.Management\Set-ItemProperty $Path -Name $Name -Value $Value
    }
}

function DeSerialize-PSObject
{
    [CmdletBinding(PositionalBinding=$false)]    
    Param
    (
        [Parameter(Mandatory=$true)]        
        $Path
    )
    $filecontent = Microsoft.PowerShell.Management\Get-Content -Path $Path
    [System.Management.Automation.PSSerializer]::Deserialize($filecontent)    
}

function Log-ArtifactNotFoundInPSGallery
{
    [CmdletBinding()]
    Param
    (     
        [Parameter()]
        [string[]]
        $SearchedName,
                   
        [Parameter()]
        [string[]]
        $FoundName,

        [Parameter(Mandatory=$true)]
        [string]
        $operationName
    )

    if (-not $script:TelemetryEnabled)
    {            
        return
    }

    if(-not $SearchedName)
    {
        return
    }

    $SearchedNameNoWildCards = @()

    # Ignore wild cards  
    foreach ($artifactName in $SearchedName)
    {
        if (-not (Test-WildcardPattern $artifactName))
        {
            $SearchedNameNoWildCards += $artifactName
        }
    }

    # Find artifacts searched, but not found in the specified gallery
    $notFoundArtifacts = @()
    foreach ($element in $SearchedNameNoWildCards)
    {
        if (-not ($FoundName -contains $element))
        {
            $notFoundArtifacts += $element
        }
    }

    # Perform Telemetry only if searched artifacts are not available in specified Gallery
    if ($notFoundArtifacts)
    {
        [Microsoft.PowerShell.Commands.PowerShellGet.Telemetry]::TraceMessageArtifactsNotFound($notFoundArtifacts, $operationName)
    }   
}

# Function to record non-PSGallery registration for telemetry
# Function consumes the type of registration (i.e hosted (http(s)), non-hosted (file/unc)), locations, installation policy, provider and event name
function Log-NonPSGalleryRegistration
{
    [CmdletBinding()]
    Param
    (   
        [Parameter()]
        [string]
        $sourceLocation,

        [Parameter()]
        [string]
        $installationPolicy,

        [Parameter()]
        [string]
        $packageManagementProvider,

        [Parameter()]
        [string]
        $publishLocation,

        [Parameter()]
        [string]
        $scriptSourceLocation,
                   
        [Parameter()]
        [string]
        $scriptPublishLocation,

        [Parameter(Mandatory=$true)]
        [string]
        $operationName
    )

    if (-not $script:TelemetryEnabled)
    {            
        return
    }
    
    # Initialize source location type - this can be hosted (http(s)) or not hosted (unc/file)
    $sourceLocationType = "NON_WEB_HOSTED"
    if (Test-WebUri -uri $sourceLocation)
    {
        $sourceLocationType = "WEB_HOSTED"
    }

    # Create a hash of the source location
    # We cannot log the actual source location, since this might contain PII (Personally identifiable information) data
    $sourceLocationHash = Get-Hash -locationString $sourceLocation
    $publishLocationHash = Get-Hash -locationString $publishLocation
    $scriptSourceLocationHash = Get-Hash -locationString $scriptSourceLocation
    $scriptPublishLocationHash = Get-Hash -locationString $scriptPublishLocation
    
    # Log the telemetry event    
    [Microsoft.PowerShell.Commands.PowerShellGet.Telemetry]::TraceMessageNonPSGalleryRegistration($sourceLocationType, $sourceLocationHash, $installationPolicy, $packageManagementProvider, $publishLocationHash, $scriptSourceLocationHash, $scriptPublishLocationHash, $operationName)
}

# Returns a SHA1 hash of the specified string
function Get-Hash
{
    [CmdletBinding()]
    Param
    (
        [string]
        $locationString        
    )

    if(-not $locationString)
    {
        return ""
    }
    
    $sha1Object = New-Object System.Security.Cryptography.SHA1Managed
    $stringHash = $sha1Object.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($locationString));
    $stringHashInHex = [System.BitConverter]::ToString($stringHash)

    if ($stringHashInHex)
    {
        # Remove all dashes in the hex string
        return $stringHashInHex.Replace('-', '')
    }
    
    return ""
}

function Get-ValidModuleLocation
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $LocationString,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ParameterName,

        [Parameter()]
        $Credential,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential
    )

    # Get the actual Uri from the Location
    if(-not (Microsoft.PowerShell.Management\Test-Path $LocationString))
    {
        # Append '/api/v2/' to the $LocationString, return if that URI works.
        if(($LocationString -notmatch 'LinkID') -and 
           -not ($LocationString.EndsWith('/nuget/v2', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('/nuget/v2/', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('/nuget', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('/nuget/', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('index.json', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('index.json/', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('/api/v2', [System.StringComparison]::OrdinalIgnoreCase)) -and
           -not ($LocationString.EndsWith('/api/v2/', [System.StringComparison]::OrdinalIgnoreCase))
            )
        {
            $tempLocation = $null

            if($LocationString.EndsWith('/', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $tempLocation = $LocationString + 'api/v2/'
            }
            else
            {
                $tempLocation = $LocationString + '/api/v2/'
            }

            if($tempLocation)
            {
                # Ping and resolve the specified location
                $tempLocation = Resolve-Location -Location $tempLocation `
                                                 -LocationParameterName $ParameterName `
                                                 -Credential $Credential `
                                                 -Proxy $Proxy `
                                                 -ProxyCredential $ProxyCredential `
                                                 -ErrorAction SilentlyContinue `
                                                 -WarningAction SilentlyContinue                
                if($tempLocation)
                {
                   return $tempLocation
                }
                # No error if we can't resolve the URL appended with '/api/v2/'
            }
        }

        # Ping and resolve the specified location
        $LocationString = Resolve-Location -Location $LocationString `
                                           -LocationParameterName $ParameterName `
                                           -Credential $Credential `
                                           -Proxy $Proxy `
                                           -ProxyCredential $ProxyCredential `
                                           -CallerPSCmdlet $PSCmdlet   
    }

    return $LocationString
}

function Save-ModuleSources
{
    if($script:PSGetModuleSources)
    {
        if(-not (Microsoft.PowerShell.Management\Test-Path $script:PSGetAppLocalPath))
        {
            $null = Microsoft.PowerShell.Management\New-Item -Path $script:PSGetAppLocalPath `
                                                             -ItemType Directory -Force `
                                                             -ErrorAction SilentlyContinue `
                                                             -WarningAction SilentlyContinue `
                                                             -Confirm:$false -WhatIf:$false
        }        
        Microsoft.PowerShell.Utility\Out-File -FilePath $script:PSGetModuleSourcesFilePath -Force -InputObject ([System.Management.Automation.PSSerializer]::Serialize($script:PSGetModuleSources))
   }   
}

function Test-ModuleSxSVersionSupport
{
    # Side-by-Side module version is available on PowerShell 5.0 or later versions only
    # By default, PowerShell module versions will be installed/updated Side-by-Side.
    $PSVersionTable.PSVersion -ge '5.0.0'
}

function Test-ModuleInstalled
{
    [CmdletBinding(PositionalBinding=$false)]
    [OutputType("PSModuleInfo")]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name,

        [Parameter()]
        [Version]
        $RequiredVersion
    )

    # Check if module is already installed
    $availableModule = Microsoft.PowerShell.Core\Get-Module -ListAvailable -Name $Name -Verbose:$false | 
                           Microsoft.PowerShell.Core\Where-Object {-not (Test-ModuleSxSVersionSupport) -or -not $RequiredVersion -or ($RequiredVersion -eq $_.Version)} | 
                               Microsoft.PowerShell.Utility\Select-Object -Unique -ErrorAction Ignore

    return $availableModule
}

function Test-ScriptInstalled
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Name,

        [Parameter()]
        [Version]
        $RequiredVersion
    )

    $scriptInfo = $null
    $scriptFileName = "$Name.ps1"
    $scriptPaths = @($script:ProgramFilesScriptsPath, $script:MyDocumentsScriptsPath)    
    $scriptInfos = @()

    foreach ($location in $scriptPaths)
    {
        $scriptFilePath = Microsoft.PowerShell.Management\Join-Path -Path $location -ChildPath $scriptFileName

        if(Microsoft.PowerShell.Management\Test-Path -Path $scriptFilePath -PathType Leaf)
        {
            $scriptInfo = $null
            try
            {
                $scriptInfo = Test-ScriptFileInfo -Path $scriptFilePath -ErrorAction SilentlyContinue -WarningAction SilentlyContinue
            }
            catch
            {
                # Ignore any terminating error from the Test-ScriptFileInfo cmdlet,
                # if it does not contain valid Script metadata
                Write-Verbose -Message "$_"
            }

            if($scriptInfo)
            {
                $scriptInfos += $scriptInfo
            }
            else
            {
                # Since the script file doesn't contain the valid script metadata,
                # create dummy PSScriptInfo object with 0.0 version 
                $scriptInfo = New-PSScriptInfoObject -Path $scriptFilePath
                $scriptInfo.$script:Version = [Version]'0.0'

                $scriptInfos += $scriptInfo
            }
        }
    }

    $scriptInfo = $scriptInfos | Microsoft.PowerShell.Core\Where-Object {
                                                                (-not $RequiredVersion) -or ($RequiredVersion -eq $_.Version)
                                                            } | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

    return $scriptInfo
}

function New-PSScriptInfoObject
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $Path
    )

    $PSScriptInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{})
    $script:PSScriptInfoProperties | Microsoft.PowerShell.Core\ForEach-Object {
                                            Microsoft.PowerShell.Utility\Add-Member -InputObject $PSScriptInfo `
                                                                                    -MemberType NoteProperty `
                                                                                    -Name $_ `
                                                                                    -Value $null
                                        }

    $PSScriptInfo.$script:Name = [System.IO.Path]::GetFileNameWithoutExtension($Path)
    $PSScriptInfo.$script:Path = $Path
    $PSScriptInfo.$script:ScriptBase = (Microsoft.PowerShell.Management\Split-Path -Path $Path -Parent)

    return $PSScriptInfo
}

function Get-OrderedPSScriptInfoObject
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [PSCustomObject]
        $PSScriptInfo
    )

    $NewPSScriptInfo = Microsoft.PowerShell.Utility\New-Object PSCustomObject -Property ([ordered]@{
                            $script:Name = $PSScriptInfo.$script:Name
                            $script:Version = $PSScriptInfo.$script:Version
                            $script:Guid = $PSScriptInfo.$script:Guid
                            $script:Path = $PSScriptInfo.$script:Path
                            $script:ScriptBase = $PSScriptInfo.$script:ScriptBase
                            $script:Description = $PSScriptInfo.$script:Description
                            $script:Author = $PSScriptInfo.$script:Author
                            $script:CompanyName = $PSScriptInfo.$script:CompanyName
                            $script:Copyright = $PSScriptInfo.$script:Copyright
                            $script:Tags = $PSScriptInfo.$script:Tags
                            $script:ReleaseNotes = $PSScriptInfo.$script:ReleaseNotes
                            $script:RequiredModules = $PSScriptInfo.$script:RequiredModules
                            $script:ExternalModuleDependencies = $PSScriptInfo.$script:ExternalModuleDependencies
                            $script:RequiredScripts = $PSScriptInfo.$script:RequiredScripts
                            $script:ExternalScriptDependencies = $PSScriptInfo.$script:ExternalScriptDependencies
                            $script:LicenseUri = $PSScriptInfo.$script:LicenseUri
                            $script:ProjectUri = $PSScriptInfo.$script:ProjectUri
                            $script:IconUri = $PSScriptInfo.$script:IconUri
                            $script:DefinedCommands = $PSScriptInfo.$script:DefinedCommands
                            $script:DefinedFunctions = $PSScriptInfo.$script:DefinedFunctions
                            $script:DefinedWorkflows = $PSScriptInfo.$script:DefinedWorkflows
                        })

    $NewPSScriptInfo.PSTypeNames.Insert(0, "Microsoft.PowerShell.Commands.PSScriptInfo")

    return $NewPSScriptInfo
}

function Get-AvailableScriptFilePath
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter()]
        [string]
        $Name
    )

    $scriptInfo = $null
    $scriptFileName = '*.ps1'
    $scriptBasePaths = @($script:ProgramFilesScriptsPath, $script:MyDocumentsScriptsPath)    
    $scriptFilePaths = @()
    $wildcardPattern = $null

    if($Name)
    {
        if(Test-WildcardPattern -Name $Name)
        {
            $wildcardPattern = New-Object System.Management.Automation.WildcardPattern $Name,$script:wildcardOptions
        }
        else
        {
            $scriptFileName = "$Name.ps1"
        }

    }

    foreach ($location in $scriptBasePaths)
    {
        $scriptFiles = Get-ChildItem -Path $location `
                                     -Filter $scriptFileName `
                                     -ErrorAction SilentlyContinue `
                                     -WarningAction SilentlyContinue
        
        if($wildcardPattern)
        {
            $scriptFiles | Microsoft.PowerShell.Core\ForEach-Object {
                                if($wildcardPattern.IsMatch($_.BaseName))
                                {
                                    $scriptFilePaths += $_.FullName
                                }
                           }
        }
        else
        {
            $scriptFiles | Microsoft.PowerShell.Core\ForEach-Object { $scriptFilePaths += $_.FullName }
        }
    }

    return $scriptFilePaths
}

function Get-InstalledScriptFilePath
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter()]
        [string]
        $Name
    )

    $installedScriptFilePaths = @()
    $scriptFilePaths = Get-AvailableScriptFilePath @PSBoundParameters

    foreach ($scriptFilePath in $scriptFilePaths)
    {
        $scriptInfo = Test-ScriptInstalled -Name ([System.IO.Path]::GetFileNameWithoutExtension($scriptFilePath))

        if($scriptInfo)
        {
            $installedScriptInfoFilePath = $null
            $installedScriptInfoFileName = "$($scriptInfo.Name)_$script:InstalledScriptInfoFileName"

            if($scriptInfo.Path.StartsWith($script:ProgramFilesScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $installedScriptInfoFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesInstalledScriptInfosPath `
                                                                                         -ChildPath $installedScriptInfoFileName
            }
            elseif($scriptInfo.Path.StartsWith($script:MyDocumentsScriptsPath, [System.StringComparison]::OrdinalIgnoreCase))
            {
                $installedScriptInfoFilePath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsInstalledScriptInfosPath `
                                                                                         -ChildPath $installedScriptInfoFileName
            }

            if($installedScriptInfoFilePath -and (Microsoft.PowerShell.Management\Test-Path -Path $installedScriptInfoFilePath -PathType Leaf))
            {
                $installedScriptFilePaths += $scriptInfo.Path
            }
        }
    }

    return $installedScriptFilePaths
}


function Update-ModuleManifest
{
<#
.ExternalHelp PSGet.psm1-help.xml
#>
[CmdletBinding(SupportsShouldProcess=$true,
                   PositionalBinding=$false,
                   HelpUri='https://go.microsoft.com/fwlink/?LinkId=619311')]
    Param
    (
        [Parameter(Mandatory=$true,
                   Position=0,                    
                   ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [ValidateNotNullOrEmpty()]
        [Object[]]
        $NestedModules,

        [ValidateNotNullOrEmpty()]
        [Guid]
        $Guid,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Author,

        [Parameter()] 
        [ValidateNotNullOrEmpty()]
        [String]
        $CompanyName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Copyright,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $RootModule,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $ModuleVersion,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string]
        $Description,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.Reflection.ProcessorArchitecture]
        $ProcessorArchitecture,

        [Parameter()]
        [ValidateSet('Desktop','Core')]
        [string[]]
        $CompatiblePSEditions,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $PowerShellVersion,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $ClrVersion,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $DotNetFrameworkVersion,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $PowerShellHostName,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Version]
        $PowerShellHostVersion,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Object[]]
        $RequiredModules,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $TypesToProcess,
        
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $FormatsToProcess,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $ScriptsToProcess,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $RequiredAssemblies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $FileList,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [object[]]
        $ModuleList,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $FunctionsToExport,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $AliasesToExport,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $VariablesToExport,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $CmdletsToExport,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $DscResourcesToExport,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [System.Collections.Hashtable]
        $PrivateData,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $Tags,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $ProjectUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $LicenseUri,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $IconUri,

        [Parameter()]
        [string[]]
        $ReleaseNotes,
                
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $HelpInfoUri,

        [Parameter()]
        [switch]
        $PassThru,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String]
        $DefaultCommandPrefix,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $ExternalModuleDependencies,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [String[]]
        $PackageManagementProviders
    )

    if(-not (Microsoft.PowerShell.Management\Test-Path -Path $Path -PathType Leaf))
    {
        $message = $LocalizedData.UpdateModuleManifestPathCannotFound -f ($Path)
        ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $message `
                   -ErrorId "InvalidModuleManifestFilePath" `
                   -ExceptionObject $Path `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
    }

    $ModuleManifestHashTable = $null

    try
    {
        $ModuleManifestHashTable = Get-ManifestHashTable -Path $Path -CallerPSCmdlet $PSCmdlet
    }
    catch
    {
        $message = $LocalizedData.TestModuleManifestFail -f ($_.Exception.Message)
        ThrowError -ExceptionName "System.ArgumentException" `
                    -ExceptionMessage $message `
                    -ErrorId "InvalidModuleManifestFile" `
                    -ExceptionObject $Path `
                    -CallerPSCmdlet $PSCmdlet `
                    -ErrorCategory InvalidArgument
        return
    }
    
    #Get the original module manifest and migrate all the fields to the new module manifest, including the specified parameter values
    $moduleInfo = $null

    try
    {
        $moduleInfo = Microsoft.PowerShell.Core\Test-ModuleManifest -Path $Path -ErrorAction Stop
    }
    catch
    {
        # Throw an error only if Test-ModuleManifest did not return the PSModuleInfo object.
        # This enables the users to use Update-ModuleManifest cmdlet to update the metadata.
        if(-not $moduleInfo)
        {
            $message = $LocalizedData.TestModuleManifestFail -f ($_.Exception.Message)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "InvalidModuleManifestFile" `
                       -ExceptionObject $Path `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
            return
        }
    }
    
    #Params to pass to New-ModuleManifest module                                                                    
    $params = @{} 

    #NestedModules is read-only property
    if($NestedModules)
    {
        $params.Add("NestedModules",$NestedModules)
    }
    elseif($moduleInfo.NestedModules)
    {
        #Get the original module info from ManifestHashTab
        if($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("NestedModules"))
        {
            $params.Add("NestedModules",$ModuleManifestHashtable.NestedModules)
        }
    }

    #Guid is read-only property
    if($Guid)
    {
        $params.Add("Guid",$Guid)
    }
    elseif($moduleInfo.Guid)
    {
        $params.Add("Guid",$moduleInfo.Guid)
    }

    if($Author)
    {
        $params.Add("Author",$Author)
    }
    elseif($moduleInfo.Author)
    {
        $params.Add("Author",$moduleInfo.Author)
    }
    
    if($CompanyName)
    {
        $params.Add("CompanyName",$CompanyName)
    }
    elseif($moduleInfo.CompanyName)
    {
        $params.Add("CompanyName",$moduleInfo.CompanyName)
    }

    if($Copyright)
    {
        $params.Add("CopyRight",$Copyright)
    }
    elseif($moduleInfo.Copyright)
    {
        $params.Add("Copyright",$moduleInfo.Copyright)
    }

    if($RootModule)
    {
        $params.Add("RootModule",$RootModule)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("RootModule") -and $moduleInfo.RootModule)
    {
        $params.Add("RootModule",$ModuleManifestHashTable.RootModule)
    }

    if($ModuleVersion)
    {
        $params.Add("ModuleVersion",$ModuleVersion)
    }
    elseif($moduleInfo.Version)
    {
        $params.Add("ModuleVersion",$moduleInfo.Version)
    }
    
    if($Description)
    {
        $params.Add("Description",$Description)
    }
    elseif($moduleInfo.Description)
    {
        $params.Add("Description",$moduleInfo.Description)
    }

    if($ProcessorArchitecture)
    {
        $params.Add("ProcessorArchitecture",$ProcessorArchitecture)
    }
    #Check if ProcessorArchitecture has a value and is not 'None' on lower version PS
    elseif($moduleInfo.ProcessorArchitecture -and $moduleInfo.ProcessorArchitecture -ne 'None')
    {
        $params.Add("ProcessorArchitecture",$moduleInfo.ProcessorArchitecture)
    }

    if($PowerShellVersion)
    {
        $params.Add("PowerShellVersion",$PowerShellVersion)
    }
    elseif($moduleinfo.PowerShellVersion)
    {
        $params.Add("PowerShellVersion",$moduleinfo.PowerShellVersion)
    }

    if($ClrVersion)
    {
        $params.Add("ClrVersion",$ClrVersion)
    }
    elseif($moduleInfo.ClrVersion)
    {
        $params.Add("ClrVersion",$moduleInfo.ClrVersion)
    }

    if($DotNetFrameworkVersion)
    {
        $params.Add("DotNetFrameworkVersion",$DotNetFrameworkVersion)
    }
    elseif($moduleInfo.DotNetFrameworkVersion)
    {
        $params.Add("DotNetFrameworkVersion",$moduleInfo.DotNetFrameworkVersion)
    }

    if($PowerShellHostName)
    {
        $params.Add("PowerShellHostName",$PowerShellHostName)
    }
    elseif($moduleInfo.PowerShellHostName)
    {
        $params.Add("PowerShellHostName",$moduleInfo.PowerShellHostName)
    }

    if($PowerShellHostVersion)
    {
        $params.Add("PowerShellHostVersion",$PowerShellHostVersion)
    }
    elseif($moduleInfo.PowerShellHostVersion)
    {
        $params.Add("PowerShellHostVersion",$moduleInfo.PowerShellHostVersion)
    }

    if($RequiredModules)
    {
        $params.Add("RequiredModules",$RequiredModules)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("RequiredModules") -and $moduleInfo.RequiredModules)
    {
        $params.Add("RequiredModules",$ModuleManifestHashtable.RequiredModules)
    }

    if($TypesToProcess)
    {
        $params.Add("TypesToProcess",$TypesToProcess)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("TypesToProcess") -and $moduleInfo.ExportedTypeFiles)
    {
        $params.Add("TypesToProcess",$ModuleManifestHashTable.TypesToProcess)
    }

    if($FormatsToProcess)
    {
        $params.Add("FormatsToProcess",$FormatsToProcess)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("FormatsToProcess") -and $moduleInfo.ExportedFormatFiles)
    {
        $params.Add("FormatsToProcess",$ModuleManifestHashTable.FormatsToProcess)
    }

    if($ScriptsToProcess)
    {
        $params.Add("ScriptsToProcess",$ScriptstoProcess)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("ScriptsToProcess") -and $moduleInfo.Scripts)
    {
        $params.Add("ScriptsToProcess",$ModuleManifestHashTable.ScriptsToProcess)
    }

    if($RequiredAssemblies)
    {
        $params.Add("RequiredAssemblies",$RequiredAssemblies)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("RequiredAssemblies") -and $moduleInfo.RequiredAssemblies)
    {
        $params.Add("RequiredAssemblies",$moduleInfo.RequiredAssemblies)
    }

    if($FileList)
    {
        $params.Add("FileList",$FileList)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("FileList") -and $moduleInfo.FileList)
    {
        $params.Add("FileList",$ModuleManifestHashTable.FileList)
    }

    #Make sure every path defined under FileList is within module base
    $moduleBase = $moduleInfo.ModuleBase
    foreach($file in $params["FileList"])
    {
        #If path is not root path, append the module base to it and check if the file exists 
        if(-not [System.IO.Path]::IsPathRooted($file))
        {
            $combinedPath = Join-Path $moduleBase -ChildPath $file
        }
        else
        {
            $combinedPath = $file
        }
        if(-not (Microsoft.PowerShell.Management\Test-Path -Type Leaf -LiteralPath $combinedPath))
        {
            $message = $LocalizedData.FilePathInFileListNotWithinModuleBase -f ($file,$moduleBase)
            ThrowError -ExceptionName "System.ArgumentException" `
               -ExceptionMessage $message `
               -ErrorId "FilePathInFileListNotWithinModuleBase" `
               -ExceptionObject $file `
               -CallerPSCmdlet $PSCmdlet `
               -ErrorCategory InvalidArgument
               
            return
        }
    }

    if($ModuleList)
    {
        $params.Add("ModuleList",$ModuleList)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("ModuleList") -and $moduleInfo.ModuleList)
    {
        $params.Add("ModuleList",$ModuleManifestHashtable.ModuleList)
    }

    if($FunctionsToExport)
    {
        $params.Add("FunctionsToExport",$FunctionsToExport)
    }
   
    elseif($moduleInfo.ExportedFunctions)
    {
        #Since $moduleInfo.ExportedFunctions is a hashtable, we need to take the name of the 
        #functions and make them into a list
        $params.Add("FunctionsToExport",($moduleInfo.ExportedFunctions.Keys -split ' '))
    }
    

    if($AliasesToExport)
    {
        $params.Add("AliasesToExport",$AliasesToExport)
    }
    elseif($moduleInfo.ExportedAliases)
    {
        $params.Add("AliasesToExport",($moduleInfo.ExportedAliases.Keys -split ' '))
    }
    if($VariablesToExport)
    {
        $params.Add("VariablesToExport",$VariablesToExport)
    }
    elseif($moduleInfo.ExportedVariables)
    { 
        $params.Add("VariablesToExport",($moduleInfo.ExportedVariables.Keys -split ' '))
    }
    if($CmdletsToExport)
    {
        $params.Add("CmdletsToExport", $CmdletsToExport)
    }
    elseif($moduleInfo.ExportedCmdlets)
    {
        $params.Add("CmdletsToExport",($moduleInfo.ExportedCmdlets.Keys -split ' '))
    }
    if($DscResourcesToExport)
    {
        #DscResourcesToExport field is not available in PowerShell version lower than 5.0
        
        if  (($PSVersionTable.PSVersion -lt '5.0.0') -or ($PowerShellVersion -and $PowerShellVersion -lt '5.0') `
             -or (-not $PowerShellVersion -and $moduleInfo.PowerShellVersion -and $moduleInfo.PowerShellVersion -lt '5.0') `
             -or (-not $PowerShellVersion -and -not $moduleInfo.PowerShellVersion))
        {
                ThrowError -ExceptionName "System.ArgumentException" `
                   -ExceptionMessage $LocalizedData.ExportedDscResourcesNotSupportedOnLowerPowerShellVersion `
                   -ErrorId "ExportedDscResourcesNotSupported" `
                   -ExceptionObject $DscResourcesToExport `
                   -CallerPSCmdlet $PSCmdlet `
                   -ErrorCategory InvalidArgument
                return  
        }

        $params.Add("DscResourcesToExport",$DscResourcesToExport)
    }
    elseif(Microsoft.PowerShell.Utility\Get-Member -InputObject $moduleInfo -name "ExportedDscResources")
    {
        if($moduleInfo.ExportedDscResources)
        {
            $params.Add("DscResourcesToExport",$moduleInfo.ExportedDscResources)
        }
    }

    if($CompatiblePSEditions)
    {
        # CompatiblePSEditions field is not available in PowerShell version lower than 5.1
        #
        if  (($PSVersionTable.PSVersion -lt '5.1.0') -or ($PowerShellVersion -and $PowerShellVersion -lt '5.1') `
             -or (-not $PowerShellVersion -and $moduleInfo.PowerShellVersion -and $moduleInfo.PowerShellVersion -lt '5.1') `
             -or (-not $PowerShellVersion -and -not $moduleInfo.PowerShellVersion))
        {
                ThrowError -ExceptionName 'System.ArgumentException' `
                           -ExceptionMessage $LocalizedData.CompatiblePSEditionsNotSupportedOnLowerPowerShellVersion `
                           -ErrorId 'CompatiblePSEditionsNotSupported' `
                           -ExceptionObject $CompatiblePSEditions `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidArgument
                return  
        }

        $params.Add('CompatiblePSEditions', $CompatiblePSEditions)
    }
    elseif( (Microsoft.PowerShell.Utility\Get-Member -InputObject $moduleInfo -name 'CompatiblePSEditions') -and
            $moduleInfo.CompatiblePSEditions)
    {
        $params.Add('CompatiblePSEditions', $moduleInfo.CompatiblePSEditions)
    }

    if($HelpInfoUri)
    {
        $params.Add("HelpInfoUri",$HelpInfoUri)
    }
    elseif($moduleInfo.HelpInfoUri)
    {
        $params.Add("HelpInfoUri",$moduleInfo.HelpInfoUri)
    }

    if($DefaultCommandPrefix)
    {
        $params.Add("DefaultCommandPrefix",$DefaultCommandPrefix)
    }
    elseif($ModuleManifestHashTable -and $ModuleManifestHashTable.ContainsKey("DefaultCommandPrefix") -and $ModuleManifestHashTable.DefaultCommandPrefix)
    {
        $params.Add("DefaultCommandPrefix",$ModuleManifestHashTable.DefaultCommandPrefix)
    }

    #Create a temp file within the directory and generate a new temporary manifest with the input
    $tempPath = Microsoft.PowerShell.Management\Join-Path -Path $moduleInfo.ModuleBase -ChildPath "PSGet_$($moduleInfo.Name).psd1"
    $params.Add("Path",$tempPath)
    
    try
    {
        #Terminates if there is error creating new module manifest
        try{
            Microsoft.PowerShell.Core\New-ModuleManifest @params -Confirm:$false -WhatIf:$false
        }
        catch
        {
            $ErrorMessage = $LocalizedData.UpdatedModuleManifestNotValid -f ($Path, $_.Exception.Message)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $ErrorMessage `
                       -ErrorId "NewModuleManifestFailure" `
                       -ExceptionObject $params `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
            return
        }

        #Manually update the section in PrivateData since New-ModuleManifest works differently on different PS version
        $PrivateDataInput = ""
        $ExistingData = $moduleInfo.PrivateData
        $Data = @{}
        if($ExistingData)
        {
            foreach($key in $ExistingData.Keys)
            {
                if($key -ne "PSData"){
                    $Data.Add($key,$ExistingData[$key])
                }
                else
                {
                    $PSData = $ExistingData["PSData"]
                    foreach($entry in $PSData.Keys)
                            {
                    $Data.Add($entry,$PSData[$Entry])
                }
                }
            }
        }

        if($PrivateData)
        {
            foreach($key in $PrivateData.Keys)
            {
                #if user provides PSData within PrivateData, we will parse through the PSData
                if($key -ne "PSData")
                {
                    $Data[$key] = $PrivateData[$Key]
                }

                else
                {
                    $PSData = $ExistingData["PSData"]
                    foreach($entry in $PSData.Keys)
                    {
                        $Data[$entry] = $PSData[$entry]
                    }
                }
            }
        }

        #Tags is a read-only property
        if($Tags)
        {
           $Data["Tags"] = $Tags 
        }
       

        #The following Uris and ReleaseNotes cannot be empty
        if($ProjectUri)
        {
            $Data["ProjectUri"] = $ProjectUri
        }

        if($LicenseUri)
        {
            $Data["LicenseUri"] = $LicenseUri
        }
        if($IconUri)
        {
            $Data["IconUri"] = $IconUri
        }

        if($ReleaseNotes)
        {
            #If value is provided as an array, we append the string.
            $Data["ReleaseNotes"] = $($ReleaseNotes -join "`r`n")
        }
        
        if($ExternalModuleDependencies)
        {
            #ExternalModuleDependencies have to be specified either under $RequiredModules or $NestedModules
            #Extract all the module names specified in the moduleInfo of NestedModules and RequiredModules
            $DependentModuleNames = @()
            foreach($moduleInfo in $params["NestedModules"])
            {
                if($moduleInfo.GetType() -eq [System.Collections.Hashtable])
                {
                    $DependentModuleNames += $moduleInfo.ModuleName
                }
            }

            foreach($moduleInfo in $params["RequiredModules"])
            {
                if($moduleInfo.GetType() -eq [System.Collections.Hashtable])
                {
                    $DependentModuleNames += $moduleInfo.ModuleName
                }
            }

            foreach($dependency in $ExternalModuleDependencies)
            {
                if($params["NestedModules"] -notcontains $dependency -and 
                $params["RequiredModules"] -notContains $dependency -and 
                $DependentModuleNames -notcontains $dependency)
                {
                    $message = $LocalizedData.ExternalModuleDependenciesNotSpecifiedInRequiredOrNestedModules -f ($dependency)
                    ThrowError -ExceptionName "System.ArgumentException" `
                        -ExceptionMessage $message `
                        -ErrorId "InvalidExternalModuleDependencies" `
                        -ExceptionObject $Exception `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidArgument
                        return  
                    }
            }
            if($Data.ContainsKey("ExternalModuleDependencies"))
            {
                $Data["ExternalModuleDependencies"] = $ExternalModuleDependencies
            }
            else
            {
                $Data.Add("ExternalModuleDependencies", $ExternalModuleDependencies)
            }
        }
        if($PackageManagementProviders)
        {
            #Check if the provided value is within the relative path
            $ModuleBase = Microsoft.PowerShell.Management\Split-Path $Path -Parent
            $Files = Microsoft.PowerShell.Management\Get-ChildItem -Path $ModuleBase
            foreach($provider in $PackageManagementProviders)
            {
                if ($Files.Name -notcontains $provider)
                {
                    $message = $LocalizedData.PackageManagementProvidersNotInModuleBaseFolder -f ($provider,$ModuleBase)
                    ThrowError -ExceptionName "System.ArgumentException" `
                               -ExceptionMessage $message `
                               -ErrorId "InvalidPackageManagementProviders" `
                               -ExceptionObject $PackageManagementProviders `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidArgument
                    return  
                }
            }

            $Data["PackageManagementProviders"] = $PackageManagementProviders
        }
        $PrivateDataInput = Get-PrivateData -PrivateData $Data
        
        #Replace the PrivateData section by first locating the linenumbers of start line and endline.  
        $PrivateDataBegin = Select-String -Path $tempPath -Pattern "PrivateData ="
        $PrivateDataBeginLine = $PrivateDataBegin.LineNumber
    
        $newManifest = Microsoft.PowerShell.Management\Get-Content -Path $tempPath
        #Look up the endline of PrivateData section by finding the matching brackets since private data could 
        #consist of multiple pairs of brackets.
        $PrivateDataEndLine=0
        if($PrivateDataBegin -match "@{")
        {
            $leftBrace = 0
            $EndLineOfFile = $newManifest.Length-1
        
            For($i = $PrivateDataBeginLine;$i -lt $EndLineOfFile; $i++)
            {
                if($newManifest[$i] -match "{")
                {
                    $leftBrace ++
                }
                elseif($newManifest[$i] -match "}")
                {
                    if($leftBrace -gt 0)
                    {
                        $leftBrace --
                    }
                    else
                    {
                       $PrivateDataEndLine = $i
                       break
                    }
                }
            } 
        }

    
        try
        {
            if($PrivateDataEndLine -ne 0)
            {
                #If PrivateData section has more than one line, we will remove the old content and insert the new PrivataData
                $newManifest  | where {$_.readcount -le $PrivateDataBeginLine -or $_.readcount -gt $PrivateDataEndLine+1} `
                | ForEach-Object {
                    $_
                    if($_ -match "PrivateData = ")
                    {
                        $PrivateDataInput
                    }
                  } | Set-Content -Path $tempPath -Confirm:$false -WhatIf:$false
            }

            #In lower version, PrivateData is just a single line
            else
            {
                $PrivateDataForDownlevelPS = "PrivateData = @{ `n"+$PrivateDataInput

                $newManifest  | where {$_.readcount -le $PrivateDataBeginLine -or $_.readcount -gt $PrivateDataBeginLine } `
                | ForEach-Object {
                    $_
                    if($_ -match "PrivateData = ")
                    {
                       $PrivateDataForDownlevelPS
                    }
                } | Set-Content -Path $tempPath -Confirm:$false -WhatIf:$false
            }
 
            #Verify the new module manifest is valid
            $testModuleInfo = Microsoft.PowerShell.Core\Test-ModuleManifest -Path $tempPath `
                                                                        -Verbose:$VerbosePreference `
        }
        #Catch the exceptions from Test-ModuleManifest
        catch
        {
            $message = $LocalizedData.UpdatedModuleManifestNotValid -f ($Path, $_.Exception.Message)
       
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "UpdateManifestFileFail" `
                       -ExceptionObject $_.Exception `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
            return
        }
    
    
       $newContent = Microsoft.PowerShell.Management\Get-Content -Path $tempPath
   
       try{
           #Ask for confirmation of the new manifest before replacing the original one
           if($PSCmdlet.ShouldProcess($Path,$LocalizedData.UpdateManifestContentMessage+$newContent))
           {
                Microsoft.PowerShell.Management\Set-Content -Path $Path -Value $newContent -Confirm:$false -WhatIf:$false
           }

           #Return the new content if -PassThru is specified
           if($PassThru)
           {
      	        return $newContent
           }
      }
      catch
      {
            $message = $LocalizedData.ManifestFileReadWritePermissionDenied -f ($Path)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "ManifestFileReadWritePermissionDenied" `
                       -ExceptionObject $Path `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidArgument
      }
    }
    finally
    {
        Microsoft.PowerShell.Management\Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
    }
}

#Utility function to help form the content string for PrivateData
function Get-PrivateData
{
    param
    (
        [System.Collections.Hashtable]
        $PrivateData
    )

    if($PrivateData.Keys.Count -eq 0)
    {
        $content = "
    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        # Tags = @()

        # A URL to the license for this module.
        # LicenseUri = ''

        # A URL to the main website for this project.
        # ProjectUri = ''

        # A URL to an icon representing this module.
        # IconUri = ''

        # ReleaseNotes of this module
        # ReleaseNotes = ''

        # External dependent modules of this module
        # ExternalModuleDependencies = ''

    } # End of PSData hashtable

} # End of PrivateData hashtable"
        return $content
    }


    #Validate each of the property of PSData is of the desired data type
    $Tags= $PrivateData["Tags"] -join "','" | %{"'$_'"}
    $LicenseUri = $PrivateData["LicenseUri"]| %{"'$_'"}
    $ProjectUri = $PrivateData["ProjectUri"] | %{"'$_'"}
    $IconUri = $PrivateData["IconUri"] | %{"'$_'"}
    $ReleaseNotesEscape = $PrivateData["ReleaseNotes"] -Replace "'","''"
    $ReleaseNotes = $ReleaseNotesEscape | %{"'$_'"}
    $ExternalModuleDependencies = $PrivateData["ExternalModuleDependencies"] -join "','" | %{"'$_'"} 
    
    $DefaultProperties = @("Tags","LicenseUri","ProjectUri","IconUri","ReleaseNotes","ExternalModuleDependencies")

    $ExtraProperties = @()
    foreach($key in $PrivateData.Keys)
    {
        if($DefaultProperties -notcontains $key)
        {
            $PropertyString = "#"+"$key"+ " of this module"
            $PropertyString += "`r`n    "
            $PropertyString += $key +" = " + "'"+$PrivateData[$key]+"'"
            $ExtraProperties += ,$PropertyString
        }
    }

    $ExtraPropertiesString = ""
    $firstProperty = $true
    foreach($property in $ExtraProperties)
    {
        if($firstProperty)
        {
            $firstProperty = $false
        }
        else
        {
            $ExtraPropertiesString += "`r`n`r`n    "
        }
        $ExtraPropertiesString += $Property
    }

    $TagsLine ="# Tags = @()"
    if($Tags -ne "''")
    {
        $TagsLine = "Tags = "+$Tags
    }
    $LicenseUriLine = "# LicenseUri = ''"
    if($LicenseUri -ne "''")
    {
        $LicenseUriLine = "LicenseUri = "+$LicenseUri
    }
    $ProjectUriLine = "# ProjectUri = ''"
    if($ProjectUri -ne "''")
    {
        $ProjectUriLine = "ProjectUri = " +$ProjectUri
    }
    $IconUriLine = "# IconUri = ''"
    if($IconUri -ne "''")
    {
        $IconUriLine = "IconUri = " +$IconUri
    }           
    $ReleaseNotesLine = "# ReleaseNotes = ''"
    if($ReleaseNotes -ne "''")
    {
        $ReleaseNotesLine = "ReleaseNotes = "+$ReleaseNotes
    }
    $ExternalModuleDependenciesLine ="# ExternalModuleDependencies = ''"
    if($ExternalModuleDependencies -ne "''")
    {
        $ExternalModuleDependenciesLine = "ExternalModuleDependencies = "+$ExternalModuleDependencies
    }

    if(-not $ExtraPropertiesString -eq "")
    {
        $Content = "
    ExtraProperties

    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        $TagsLine

        # A URL to the license for this module.
        $LicenseUriLine

        # A URL to the main website for this project.
        $ProjectUriLine

        # A URL to an icon representing this module.
        $IconUriLine

        # ReleaseNotes of this module
        $ReleaseNotesLine

        # External dependent modules of this module
        $ExternalModuleDependenciesLine

    } # End of PSData hashtable
    
} # End of PrivateData hashtable"
        
        #Replace the Extra PrivateData in the block
        $Content -replace "ExtraProperties", $ExtraPropertiesString
    }
    else
    {
        $content = "
    PSData = @{

        # Tags applied to this module. These help with module discovery in online galleries.
        $TagsLine

        # A URL to the license for this module.
        $LicenseUriLine

        # A URL to the main website for this project.
        $ProjectUriLine

        # A URL to an icon representing this module.
        $IconUriLine

        # ReleaseNotes of this module
        $ReleaseNotesLine

        # External dependent modules of this module
        $ExternalModuleDependenciesLine

    } # End of PSData hashtable
    
 } # End of PrivateData hashtable" 
        return $content
    }
}

function Copy-ScriptFile
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $SourcePath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $DestinationPath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [PSCustomObject]
        $PSGetItemInfo,

        [Parameter()]
        [string]
        $Scope
    )
    
    # Copy the script file to destination
    if(-not (Microsoft.PowerShell.Management\Test-Path -Path $DestinationPath))
    {
        $null = Microsoft.PowerShell.Management\New-Item -Path $DestinationPath `
                                                         -ItemType Directory `
                                                         -Force `
                                                         -ErrorAction SilentlyContinue `
                                                         -WarningAction SilentlyContinue `
                                                         -Confirm:$false `
                                                         -WhatIf:$false
    }

    Microsoft.PowerShell.Management\Copy-Item -Path $SourcePath -Destination $DestinationPath -Force -Confirm:$false -WhatIf:$false -Verbose

    if($Scope)
    {
        # Create <Name>_InstalledScriptInfo.xml
        $InstalledScriptInfoFileName = "$($PSGetItemInfo.Name)_$script:InstalledScriptInfoFileName"

        if($scope -eq 'AllUsers')
        {
            $scriptInfopath = Microsoft.PowerShell.Management\Join-Path -Path $script:ProgramFilesInstalledScriptInfosPath `
                                                                        -ChildPath $InstalledScriptInfoFileName
        }
        else
        {
            $scriptInfopath = Microsoft.PowerShell.Management\Join-Path -Path $script:MyDocumentsInstalledScriptInfosPath `
                                                                        -ChildPath $InstalledScriptInfoFileName
        }

        Microsoft.PowerShell.Utility\Out-File -FilePath $scriptInfopath `
                                              -Force `
                                              -InputObject ([System.Management.Automation.PSSerializer]::Serialize($PSGetItemInfo))
    }
}

function Copy-Module
{
    [CmdletBinding(PositionalBinding=$false)]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $SourcePath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $DestinationPath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        [PSCustomObject]
        $PSGetItemInfo
    )
    
    if(Microsoft.PowerShell.Management\Test-Path $DestinationPath)
    {
        Microsoft.PowerShell.Management\Remove-Item -Path $DestinationPath -Recurse -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
    }

    # Copy the module to destination
    $null = Microsoft.PowerShell.Management\New-Item -Path $DestinationPath -ItemType Directory -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
    Microsoft.PowerShell.Management\Copy-Item -Path "$SourcePath\*" -Destination $DestinationPath -Force -Recurse -Confirm:$false -WhatIf:$false
    
    # Remove the *.nupkg file
    if(Microsoft.PowerShell.Management\Test-Path "$DestinationPath\$($PSGetItemInfo.Name).nupkg")
    {
        Microsoft.PowerShell.Management\Remove-Item -Path "$DestinationPath\$($PSGetItemInfo.Name).nupkg" -Force -ErrorAction SilentlyContinue -WarningAction SilentlyContinue -Confirm:$false -WhatIf:$false
    }
                    
    # Create PSGetModuleInfo.xml
    $psgetItemInfopath = Microsoft.PowerShell.Management\Join-Path $DestinationPath $script:PSGetItemInfoFileName        

    Microsoft.PowerShell.Utility\Out-File -FilePath $psgetItemInfopath -Force -InputObject ([System.Management.Automation.PSSerializer]::Serialize($PSGetItemInfo))
    
    [System.IO.File]::SetAttributes($psgetItemInfopath, [System.IO.FileAttributes]::Hidden)
}

function Test-FileInUse
{
    [CmdletBinding()]
    [OutputType([bool])]
    param
    (
        [string]
        $FilePath
    )

    if(Microsoft.PowerShell.Management\Test-Path -LiteralPath $FilePath -PathType Leaf)
    {
        # Attempts to open a file and handles the exception if the file is already open/locked
        try
        {
            $fileInfo = New-Object System.IO.FileInfo $FilePath
            $fileStream = $fileInfo.Open( [System.IO.FileMode]::Open, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None )

            if ($fileStream)
            {
                $fileStream.Close()
            }
        }
        catch
        {
            Write-Debug "In Test-FileInUse function, unable to open the $FilePath file in ReadWrite access. $_"
            return $true
        }
    }

    return $false
}

function Test-ModuleInUse
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleBasePath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleName,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Version]
        $ModuleVersion
    )

    $FileList = Get-ChildItem -Path $ModuleBasePath `
                              -File `
                              -Recurse `
                              -ErrorAction SilentlyContinue `
                              -WarningAction SilentlyContinue
    $IsModuleInUse = $false

    foreach($file in $FileList)
    {
        $IsModuleInUse = Test-FileInUse -FilePath $file.FullName

        if($IsModuleInUse)
        {
            break
        }
    }

    if($IsModuleInUse)
    {
        $message = $LocalizedData.ModuleVersionInUse -f ($ModuleVersion, $ModuleName)
        Write-Error -Message $message -ErrorId 'ModuleIsInUse' -Category InvalidOperation

        return $true
    }

    return $false
}

function Validate-ModuleAuthenticodeSignature
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        $CurrentModuleInfo,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $InstallLocation,

        [Parameter()]
        [Switch]
        $IsUpdateOperation,

        [Parameter()]
        [Switch]
        $SkipPublisherCheck
    )

    $InstalledModuleDetails = $null
    $InstalledModuleInfo = Test-ModuleInstalled -Name $CurrentModuleInfo.Name
    if($InstalledModuleInfo)
    {
        $InstalledModuleDetails = Get-InstalledModuleAuthenticodeSignature -InstalledModuleInfo $InstalledModuleInfo `
                                                                           -InstallLocation $InstallLocation
    }

    # Skip the publisher check when -SkipPublisherCheck is specified and 
    # it is not an update operation.
    if(-not $IsUpdateOperation -and $SkipPublisherCheck)
    {
        $Message = $LocalizedData.SkippingPublisherCheck -f ($CurrentModuleInfo.Version, $CurrentModuleInfo.Name)
        Write-Verbose -Message $message

        return $true
    }

    # Validate the catalog signature for the current module being installed.
    $ev = $null
    $CurrentModuleDetails = ValidateAndGet-AuthenticodeSignature -ModuleInfo $CurrentModuleInfo -ErrorVariable ev

    if($ev)
    {
        return $false
    }

    if($InstalledModuleInfo)
    {
        $CurrentModuleAuthenticodePublisher = $null
        $IsCurrentModuleSignedByMicrosoft = $false

        if($CurrentModuleDetails)
        {
            $CurrentModuleAuthenticodePublisher = $CurrentModuleDetails.Publisher
            $IsCurrentModuleSignedByMicrosoft = $CurrentModuleDetails.IsMicrosoftCertificate

            $message = $LocalizedData.NewModuleVersionDetailsForPublisherValidation -f ($CurrentModuleInfo.Name, 
                                                                                        $CurrentModuleInfo.Version,
                                                                                        $CurrentModuleDetails.Publisher,
                                                                                        $CurrentModuleDetails.IsMicrosoftCertificate)
            Write-Verbose $message
        }

        $InstalledModuleAuthenticodePublisher = $null
        $IsInstalledModuleSignedByMicrosoft = $false
        $InstalledModuleVersion = [Version]'0.0'

        if($InstalledModuleDetails)
        {
            $InstalledModuleAuthenticodePublisher = $InstalledModuleDetails.Publisher
            $IsInstalledModuleSignedByMicrosoft = $InstalledModuleDetails.IsMicrosoftCertificate
            $InstalledModuleVersion = $InstalledModuleDetails.Version

            $message = $LocalizedData.SourceModuleDetailsForPublisherValidation -f ($CurrentModuleInfo.Name, 
                                                                                    $InstalledModuleDetails.Version,
                                                                                    $InstalledModuleDetails.ModuleBase,
                                                                                    $InstalledModuleDetails.Publisher,
                                                                                    $InstalledModuleDetails.IsMicrosoftCertificate)
            Write-Verbose $message
        }

        Write-Debug -Message "Previously-installed module publisher: $InstalledModuleAuthenticodePublisher"
        Write-Debug -Message "Current module publisher: $CurrentModuleAuthenticodePublisher"
        Write-Debug -Message "Is previously-installed module signed by Microsoft: $IsInstalledModuleSignedByMicrosoft"
        Write-Debug -Message "Is current module signed by Microsoft: $IsCurrentModuleSignedByMicrosoft"

        if($InstalledModuleAuthenticodePublisher)
        {
            if(-not $CurrentModuleAuthenticodePublisher)
            {
                $Message = $LocalizedData.ModuleIsNotCatalogSigned -f ($CurrentModuleInfo.Version, $CurrentModuleInfo.Name, "$($CurrentModuleInfo.Name).cat", $InstalledModuleAuthenticodePublisher, $InstalledModuleDetails.Version, $InstalledModuleDetails.ModuleBase)
                ThrowError -ExceptionName 'System.InvalidOperationException' `
                            -ExceptionMessage $message `
                            -ErrorId 'ModuleIsNotCatalogSigned' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation
                return $false
            }
            elseif($InstalledModuleAuthenticodePublisher -eq $CurrentModuleAuthenticodePublisher)
            {
                $Message = $LocalizedData.AuthenticodeIssuerMatch -f ($CurrentModuleAuthenticodePublisher, $CurrentModuleInfo.Name, $CurrentModuleInfo.Version, $InstalledModuleAuthenticodePublisher, $InstalledModuleInfo.Name, $InstalledModuleVersion)
                Write-Verbose -Message $message
            }
            elseif($IsInstalledModuleSignedByMicrosoft)
            {
                if($IsCurrentModuleSignedByMicrosoft)
                {
                    $Message = $LocalizedData.PublishersMatch -f ($CurrentModuleAuthenticodePublisher, $CurrentModuleInfo.Name, $CurrentModuleInfo.Version, $InstalledModuleAuthenticodePublisher, $InstalledModuleInfo.Name, $InstalledModuleVersion)
                    Write-Verbose -Message $message
                }
                else
                {
                    $Message = $LocalizedData.PublishersMismatch -f ($InstalledModuleInfo.Name, $InstalledModuleVersion, $CurrentModuleInfo.Name, $CurrentModuleAuthenticodePublisher, $CurrentModuleInfo.Version)
                    ThrowError -ExceptionName 'System.InvalidOperationException' `
                               -ExceptionMessage $message `
                               -ErrorId 'PublishersMismatch' `
                               -CallerPSCmdlet $PSCmdlet `
                               -ErrorCategory InvalidOperation

                    return $false
                }
            }
            else
            {
                $Message = $LocalizedData.AuthenticodeIssuerMismatch -f ($CurrentModuleAuthenticodePublisher, $CurrentModuleInfo.Name, $CurrentModuleInfo.Version, $InstalledModuleAuthenticodePublisher, $InstalledModuleInfo.Name, $InstalledModuleVersion)
                ThrowError -ExceptionName 'System.InvalidOperationException' `
                            -ExceptionMessage $message `
                            -ErrorId 'AuthenticodeIssuerMismatch' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation
                return $false
            }
        }
    }

    return $true
}

function Validate-ModuleCommandAlreadyAvailable
{
    [CmdletBinding()]
    param
    (
        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $CurrentModuleInfo,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $InstallLocation,

        [Parameter()]
        [Switch]
        $AllowClobber,

        [Parameter()]
        [Switch]
        $IsUpdateOperation        
    )
    
    <#
        Install-Module must generate an error message when there is a conflict.
        User can specify -AllowClobber to avoid the message.
        Scenario: A large module could be separated into 2 smaller modules.
        Reason 1: the consumer might have to change code (aka: import-module) to use the command from the new module.
        Reason 2: it is too confusing to troubleshoot this problem if the user isn't informed right away.
    #>
    # When new module has some commands, no clobber error if 
    # - AllowClobber is specified, or
    # - Installing to the same module base, or
    # - Update operation
    if($CurrentModuleInfo.ExportedCommands.Keys.Count -and
       -not $AllowClobber -and 
       -not $IsUpdateOperation)
    {
        # Remove the version folder on 5.0 to get the actual module base folder without version
        if(Test-ModuleSxSVersionSupport)
        {
            $InstallLocation = Microsoft.PowerShell.Management\Split-Path -Path $InstallLocation
        }

        $InstalledModuleInfo = Test-ModuleInstalled -Name $CurrentModuleInfo.Name
        if(-not $InstalledModuleInfo -or -not $InstalledModuleInfo.ModuleBase.StartsWith($InstallLocation, [System.StringComparison]::OrdinalIgnoreCase))
        {
            # Throw an error if there is a command with the same name from a different source.
            # Get-Command loads the module if a command is already available.
            # To avoid that, appending '*' at the end for each name then comparing the results.
            $CommandNames = $CurrentModuleInfo.ExportedCommands.Values.Name
            $CommandNamesWithWildcards = $CommandNames | Microsoft.PowerShell.Core\Foreach-Object { "$_*" }

            $AvailableCommand = Microsoft.PowerShell.Core\Get-Command -Name $CommandNamesWithWildcards `
                                                                      -ErrorAction SilentlyContinue `
                                                                      -WarningAction SilentlyContinue | 
                                    Microsoft.PowerShell.Core\Where-Object { ($CommandNames -contains $_.Name) -and 
                                                                             ($_.Source -ne $CurrentModuleInfo.Name) } |
                                        Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore
            if($AvailableCommand)
            {
                $message = $LocalizedData.ModuleCommandAlreadyAvailable -f ($AvailableCommand.Name, $CurrentModuleInfo.Name)
                ThrowError -ExceptionName 'System.InvalidOperationException' `
                           -ExceptionMessage $message `
                           -ErrorId 'CommandAlreadyAvailable' `
                           -CallerPSCmdlet $PSCmdlet `
                           -ErrorCategory InvalidOperation

                return $false
            }
        }
    }

    return $true
}

function ValidateAndGet-AuthenticodeSignature
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $ModuleInfo
    )
    
    $ModuleDetails = $null
    $AuthenticodeSignature = $null

    $ModuleName = $ModuleInfo.Name
    $ModuleBasePath = $ModuleInfo.ModuleBase
    $CatalogFileName = "$ModuleName.cat"
    $CatalogFilePath = Microsoft.PowerShell.Management\Join-Path -Path $ModuleBasePath -ChildPath $CatalogFileName

    if(Microsoft.PowerShell.Management\Test-Path -Path $CatalogFilePath -PathType Leaf)
    {
        $message = $LocalizedData.CatalogFileFound -f ($CatalogFileName, $ModuleName)
        Write-Verbose -Message $message

        $AuthenticodeSignature = Microsoft.PowerShell.Security\Get-AuthenticodeSignature -FilePath $CatalogFilePath

        if(-not $AuthenticodeSignature -or ($AuthenticodeSignature.Status -ne "Valid"))
        {
            $message = $LocalizedData.InvalidModuleAuthenticodeSignature -f ($ModuleName, $CatalogFileName)
            ThrowError -ExceptionName 'System.InvalidOperationException' `
                        -ExceptionMessage $message `
                        -ErrorId 'InvalidAuthenticodeSignature' `
                        -CallerPSCmdlet $PSCmdlet `
                        -ErrorCategory InvalidOperation

            return
        }
        
        Write-Verbose -Message ($LocalizedData.ValidAuthenticodeSignature -f @($CatalogFileName, $ModuleName))
        
        if(Get-Command -Name Test-FileCatalog -Module Microsoft.PowerShell.Security -ErrorAction SilentlyContinue)
        {
            Write-Verbose -Message ($LocalizedData.ValidatingCatalogSignature -f @($ModuleName, $CatalogFileName))
            
            # Skip the PSGetModuleInfo.xml and ModuleName.cat files in the catalog validation
            $TestFileCatalogResult = Microsoft.PowerShell.Security\Test-FileCatalog -Path $ModuleBasePath `
                                                                                    -CatalogFilePath $CatalogFilePath `
                                                                                    -FilesToSkip $script:PSGetItemInfoFileName,'*.cat' `
                                                                                    -Detailed `
                                                                                    -ErrorAction SilentlyContinue
            if(-not $TestFileCatalogResult -or 
                ($TestFileCatalogResult.Status -ne "Valid") -or 
                ($TestFileCatalogResult.Signature.Status -ne "Valid"))
            {
                $message = $LocalizedData.InvalidCatalogSignature -f ($ModuleName, $CatalogFileName)
                ThrowError -ExceptionName 'System.InvalidOperationException' `
                            -ExceptionMessage $message `
                            -ErrorId 'InvalidCatalogSignature' `
                            -CallerPSCmdlet $PSCmdlet `
                            -ErrorCategory InvalidOperation
                return
            }
            else
            {
                Write-Verbose -Message ($LocalizedData.ValidCatalogSignature -f @($CatalogFileName, $ModuleName))
            }
        }
    }
    else
    {
        Write-Verbose -Message ($LocalizedData.CatalogFileNotFoundInNewModule -f ($CatalogFileName, $ModuleName))
    }

    if($AuthenticodeSignature)
    {
        $ModuleDetails = @{}
        $ModuleDetails['AuthenticodeSignature'] = $AuthenticodeSignature
        $ModuleDetails['Version'] = $ModuleInfo.Version
        $ModuleDetails['ModuleBase']=$ModuleInfo.ModuleBase
        $ModuleDetails['IsMicrosoftCertificate'] = Test-MicrosoftCertificate -AuthenticodeSignature $AuthenticodeSignature
        $ModuleDetails['Publisher'] = Get-AuthenticodePublisher -AuthenticodeSignature $AuthenticodeSignature

        $message = $LocalizedData.NewModuleVersionDetailsForPublisherValidation -f ($ModuleInfo.Name, $ModuleInfo.Version, $ModuleDetails.Publisher, $ModuleDetails.IsMicrosoftCertificate)
        Write-Debug $message
    }

    return $ModuleDetails
}

function Get-AuthenticodePublisher
{
    [CmdletBinding()]
    Param
    (
        [Parameter(Mandatory=$true)]
        [System.Management.Automation.Signature]
        $AuthenticodeSignature
    )    

    if($AuthenticodeSignature.SignerCertificate)
    {
        $chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
        $null = $chain.Build($AuthenticodeSignature.SignerCertificate)

        $certStoreLocations = @('cert:\LocalMachine\Root',
                                'cert:\LocalMachine\AuthRoot',
                                'cert:\CurrentUser\Root',
                                'cert:\CurrentUser\AuthRoot')

        foreach($element in $chain.ChainElements.Certificate)
        {
            foreach($certStoreLocation in $certStoreLocations)
            {
                $rootCertificateAuthority = Microsoft.PowerShell.Management\Get-ChildItem -Path $certStoreLocation | 
                                                Microsoft.PowerShell.Core\Where-Object { $_.Subject -eq $element.Subject }
                if($rootCertificateAuthority)
                {
                    return $rootCertificateAuthority.Subject
                }
            }
        }
    }
}

function Get-InstalledModuleAuthenticodeSignature
{
    [CmdletBinding()]
    Param(
        [Parameter(Mandatory=$true)]
        [PSModuleInfo]
        $InstalledModuleInfo,

        [Parameter(Mandatory=$true)]
        [string]
        $InstallLocation
    )

    $ModuleName = $InstalledModuleInfo.Name

    # Priority order for getting the published details of the installed module:
    # 1. Latest version under the $InstallLocation
    # 2. Latest available version in $PSModulePath
    # 3. $InstalledModuleInfo    
    $AvailableModules = Microsoft.PowerShell.Core\Get-Module -ListAvailable `
                                                             -Name $ModuleName `
                                                             -ErrorAction SilentlyContinue `
                                                             -WarningAction SilentlyContinue `
                                                             -Verbose:$false | 
                            Microsoft.PowerShell.Utility\Sort-Object -Property Version -Descending

    # Remove the version folder on 5.0 to get the actual module base folder without version
    if(Test-ModuleSxSVersionSupport)
    {
        $InstallLocation = Microsoft.PowerShell.Management\Split-Path -Path $InstallLocation
    }

    $SourceModule = $AvailableModules | Microsoft.PowerShell.Core\Where-Object {
                                            $_.ModuleBase.StartsWith($InstallLocation, [System.StringComparison]::OrdinalIgnoreCase) 
                                        } | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore

    if(-not $SourceModule)
    {
        $SourceModule = $AvailableModules | Microsoft.PowerShell.Utility\Select-Object -First 1 -ErrorAction Ignore
    }
    else
    {
        $SourceModule = $InstalledModuleInfo
    }

    $SignedFilePath = $SourceModule.Path
        
    $CatalogFileName = "$ModuleName.cat"
    $CatalogFilePath = Microsoft.PowerShell.Management\Join-Path -Path $SourceModule.ModuleBase -ChildPath $CatalogFileName

    if(Microsoft.PowerShell.Management\Test-Path -Path $CatalogFilePath -PathType Leaf)
    {
        $message = $LocalizedData.CatalogFileFound -f ($CatalogFileName, $ModuleName)
        Write-Debug -Message $message

        $SignedFilePath = $CatalogFilePath
    }
    else
    {
        Write-Debug -Message ($LocalizedData.CatalogFileNotFoundInAvailableModule -f ($CatalogFileName, $ModuleName))
    }

    $message = "Using the previously-installed module '{0}' with version '{1}' under '{2}' for getting the publisher details." -f ($SourceModule.Name, $SourceModule.Version, $SourceModule.ModuleBase)
    Write-Debug -Message $message
        
    $message = "Using the '{0}' file for getting the authenticode signature." -f ($SignedFilePath)
    Write-Debug -Message $message

    $AuthenticodeSignature = Microsoft.PowerShell.Security\Get-AuthenticodeSignature -FilePath $SignedFilePath
    $ModuleDetails = $null

    if($AuthenticodeSignature)
    {
        $ModuleDetails = @{}
        $ModuleDetails['AuthenticodeSignature'] = $AuthenticodeSignature
        $ModuleDetails['Version'] = $SourceModule.Version
        $ModuleDetails['ModuleBase']=$SourceModule.ModuleBase
        $ModuleDetails['IsMicrosoftCertificate'] = Test-MicrosoftCertificate -AuthenticodeSignature $AuthenticodeSignature
        $ModuleDetails['Publisher'] = Get-AuthenticodePublisher -AuthenticodeSignature $AuthenticodeSignature

        $message = $LocalizedData.SourceModuleDetailsForPublisherValidation -f ($ModuleName, $SourceModule.Version, $SourceModule.ModuleBase, $ModuleDetails.Publisher, $ModuleDetails.IsMicrosoftCertificate)
        Write-Debug $message
    }

    return $ModuleDetails
}

function Test-MicrosoftCertificate
{
    [CmdletBinding()]
    param(
        [Parameter(Mandatory=$true)]
        [System.Management.Automation.Signature]
        $AuthenticodeSignature
    )

    if($AuthenticodeSignature.SignerCertificate)
    {
        try
        {
            $X509Chain = New-Object System.Security.Cryptography.X509Certificates.X509Chain
            $null = $X509Chain.Build($AuthenticodeSignature.SignerCertificate)
        }
        catch
        {
            return $false
        }

        $SafeX509ChainHandle = [Microsoft.PowerShell.Commands.PowerShellGet.Win32Helpers]::CertDuplicateCertificateChain($X509Chain.ChainContext)
        return [Microsoft.PowerShell.Commands.PowerShellGet.Win32Helpers]::IsMicrosoftCertificate($SafeX509ChainHandle)
    }

    return $false
}

function Test-ValidManifestModule
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $ModuleBasePath,

        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $InstallLocation,

        [Parameter()]
        [Switch]
        $SkipPublisherCheck,

        [Parameter()]
        [Switch]
        $AllowClobber,

        [Parameter()]
        [Switch]
        $IsUpdateOperation
    )

    $moduleName = Microsoft.PowerShell.Management\Split-Path $ModuleBasePath -Leaf
    $manifestPath = Microsoft.PowerShell.Management\Join-Path $ModuleBasePath "$moduleName.psd1"
    $PSModuleInfo = $null

    if(Microsoft.PowerShell.Management\Test-Path $manifestPath)
    {
       $PSModuleInfo = Microsoft.PowerShell.Core\Test-ModuleManifest -Path $manifestPath -ErrorAction SilentlyContinue -WarningAction SilentlyContinue

        if(-not $PSModuleInfo)
        {
            $message = $LocalizedData.InvalidPSModule -f ($moduleName)
            ThrowError -ExceptionName 'System.InvalidOperationException' `
                       -ExceptionMessage $message `
                       -ErrorId 'InvalidManifestModule' `
                       -CallerPSCmdlet $PSCmdlet `
                       -ErrorCategory InvalidOperation
        }
        elseif(IsWindows)
        {
            $ValidationResult = Validate-ModuleAuthenticodeSignature -CurrentModuleInfo $PSModuleInfo `
                                                                     -InstallLocation $InstallLocation `
                                                                     -IsUpdateOperation:$IsUpdateOperation `
                                                                     -SkipPublisherCheck:$SkipPublisherCheck

            if($ValidationResult)
            {
                # Checking for the possible command clobbering.
                $ValidationResult = Validate-ModuleCommandAlreadyAvailable -CurrentModuleInfo $PSModuleInfo `
                                                                           -InstallLocation $InstallLocation `
                                                                           -AllowClobber:$AllowClobber `
                                                                           -IsUpdateOperation:$IsUpdateOperation

                                                                       
            }

            if(-not $ValidationResult)
            {
                $PSModuleInfo = $null
            }
        }
    }

    return $PSModuleInfo
}

function Get-ScriptSourceLocation
{
    [CmdletBinding()]
    Param
    (
        [Parameter()]
        [String]
        $Location,

        [Parameter()]
        $Credential,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential
    )

    $scriptLocation = $null

    if($Location)
    {
        # For local dir or SMB-share locations, ScriptSourceLocation is SourceLocation.
        if(Microsoft.PowerShell.Management\Test-Path -Path $Location)
        {
            $scriptLocation = $Location
        }
        else
        {
            $tempScriptLocation = $null

            if($Location.EndsWith('/api/v2', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $tempScriptLocation = $Location + '/items/psscript/'
            }
            elseif($Location.EndsWith('/api/v2/', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $tempScriptLocation = $Location + 'items/psscript/'
            }

            if($tempScriptLocation)
            {
                # Ping and resolve the specified location
                $scriptLocation = Resolve-Location -Location $tempScriptLocation `
                                                   -LocationParameterName 'ScriptSourceLocation' `
                                                   -Credential $Credential `
                                                   -Proxy $Proxy `
                                                   -ProxyCredential $ProxyCredential `
                                                   -ErrorAction SilentlyContinue `
                                                   -WarningAction SilentlyContinue
            }
        }
    }

    return $scriptLocation
}

function Get-PublishLocation
{
    [CmdletBinding()]
    Param
    (
        [Parameter()]
        [String]
        $Location
    )

    $PublishLocation = $null

    if($Location)
    {
        # For local dir or SMB-share locations, ScriptPublishLocation is PublishLocation.
        if(Microsoft.PowerShell.Management\Test-Path -Path $Location)
        {
            $PublishLocation = $Location
        }
        else
        {
            $tempPublishLocation = $null

            if($Location.EndsWith('/api/v2', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $tempPublishLocation = $Location + '/package/'
            }
            elseif($Location.EndsWith('/api/v2/', [System.StringComparison]::OrdinalIgnoreCase))
            {
                $tempPublishLocation = $Location + 'package/'
            }

            if($tempPublishLocation)
            {
                $PublishLocation = $tempPublishLocation
            }
        }
    }

    return $PublishLocation
}

function Resolve-Location
{
    [CmdletBinding()]
    [OutputType([string])]
    Param
    (
        [Parameter(Mandatory=$true)]
        [string]
        $Location,

        [Parameter(Mandatory=$true)]
        [string]
        $LocationParameterName,
        
        [Parameter()]
        $Credential,

        [Parameter()]
        $Proxy,

        [Parameter()]
        $ProxyCredential,

        [Parameter()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet
    )

    # Ping and resolve the specified location
    if(-not (Test-WebUri -uri $Location))
    {
        if(Microsoft.PowerShell.Management\Test-Path -Path $Location)
        {
            return $Location
        }
        elseif($CallerPSCmdlet)
        {
            $message = $LocalizedData.PathNotFound -f ($Location)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "PathNotFound" `
                       -CallerPSCmdlet $CallerPSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
    }
    else
    {
        $pingResult = Ping-Endpoint -Endpoint $Location -Credential $Credential -Proxy $Proxy -ProxyCredential $ProxyCredential
        $statusCode = $null
        $exception = $null
        $resolvedLocation = $null
        if($pingResult -and $pingResult.ContainsKey($Script:ResponseUri))
        {
            $resolvedLocation = $pingResult[$Script:ResponseUri]
        }

        if($pingResult -and $pingResult.ContainsKey($Script:StatusCode))
        {
            $statusCode = $pingResult[$Script:StatusCode]
        }

        Write-Debug -Message "Ping-Endpoint: location=$Location, statuscode=$statusCode, resolvedLocation=$resolvedLocation"

        if((($statusCode -eq 200) -or ($statusCode -eq 401)) -and $resolvedLocation)
        {
            return $resolvedLocation
        }
        elseif($CallerPSCmdlet)
        {
            $message = $LocalizedData.InvalidWebUri -f ($Location, $LocationParameterName)
            ThrowError -ExceptionName "System.ArgumentException" `
                       -ExceptionMessage $message `
                       -ErrorId "InvalidWebUri" `
                       -CallerPSCmdlet $CallerPSCmdlet `
                       -ErrorCategory InvalidArgument `
                       -ExceptionObject $Location
        }
    }
}

function Test-WebUri
{
    [CmdletBinding()]
    [OutputType([bool])]
    Param
    (
        [Parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        [Uri]
        $uri
    )

    return ($uri.AbsoluteURI -ne $null) -and ($uri.Scheme -match '[http|https]')
}

function Test-WildcardPattern
{
    [CmdletBinding()]
    [OutputType([bool])]
    param(
        [Parameter(Mandatory=$true)]
        [ValidateNotNull()]
        $Name
    )

    return [System.Management.Automation.WildcardPattern]::ContainsWildcardCharacters($Name)    
}

# Utility to throw an errorrecord
function ThrowError
{
    param
    (        
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.Management.Automation.PSCmdlet]
        $CallerPSCmdlet,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]        
        $ExceptionName,

        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ExceptionMessage,
        
        [System.Object]
        $ExceptionObject,
        
        [parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [System.String]
        $ErrorId,

        [parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [System.Management.Automation.ErrorCategory]
        $ErrorCategory
    )
        
    $exception = New-Object $ExceptionName $ExceptionMessage;
    $errorRecord = New-Object System.Management.Automation.ErrorRecord $exception, $ErrorId, $ErrorCategory, $ExceptionObject    
    $CallerPSCmdlet.ThrowTerminatingError($errorRecord)
}


#endregion

# Create install locations for scripts if they are not already created
if(-not (Microsoft.PowerShell.Management\Test-Path -Path $script:ProgramFilesInstalledScriptInfosPath) -and (Test-RunningAsElevated))
{
    $null = Microsoft.PowerShell.Management\New-Item -Path $script:ProgramFilesInstalledScriptInfosPath `
                                                     -ItemType Directory `
                                                     -Force `
                                                     -Confirm:$false `
                                                     -WhatIf:$false
}

if(-not (Microsoft.PowerShell.Management\Test-Path -Path $script:MyDocumentsInstalledScriptInfosPath))
{
    $null = Microsoft.PowerShell.Management\New-Item -Path $script:MyDocumentsInstalledScriptInfosPath `
                                                     -ItemType Directory `
                                                     -Force `
                                                     -Confirm:$false `
                                                     -WhatIf:$false
}

Set-Alias -Name fimo -Value Find-Module
Set-Alias -Name inmo -Value Install-Module
Set-Alias -Name upmo -Value Update-Module
Set-Alias -Name pumo -Value Publish-Module
Set-Alias -Name uimo -Value Uninstall-Module

Export-ModuleMember -Function Find-Module, `
                              Save-Module, `
                              Install-Module, `
                              Update-Module, `
                              Publish-Module, `
                              Uninstall-Module, `
                              Get-InstalledModule, `
                              Find-Command, `
                              Find-DscResource, `
                              Find-RoleCapability, `
                              Install-Script, `
                              Find-Script, `
                              Save-Script, `
                              Update-Script, `
                              Publish-Script,  `
                              Get-InstalledScript, `
                              Uninstall-Script, `
                              Test-ScriptFileInfo, `
                              New-ScriptFileInfo, `
                              Update-ScriptFileInfo, `
                              Get-PSRepository, `
                              Register-PSRepository, `
                              Unregister-PSRepository, `
                              Set-PSRepository, `
                              Find-Package, `
                              Get-PackageDependencies, `
                              Download-Package, `
                              Install-Package, `
                              Uninstall-Package, `
                              Get-InstalledPackage, `
                              Remove-PackageSource, `
                              Resolve-PackageSource, `
                              Add-PackageSource, `
                              Get-DynamicOptions, `
                              Initialize-Provider, `
                              Get-Feature, `
                              Get-PackageProviderName, `
                              Update-ModuleManifest `
                    -Alias    fimo, `
                              inmo, `
                              upmo, `
                              pumo
