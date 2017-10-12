#####################################################################################################
#
# Registers the WinRM endpoint for this instance of PowerShell.
#
# If the parameters '-PowerShellHome' were specified, it means that the script will be registering
# an instance of PowerShell from another instance of PowerShell.
#
# If no parameter is specified, it means that this instance of PowerShell is registering itself.
#
# Assumptions:
#     1. The CoreCLR and the the PowerShell assemblies are side-by-side in $PSHOME
#     2. Plugins are registered by version number. Only one plugin can be automatically registered
#        per PowerShell version. However, multiple endpoints may be manually registered for a given
#        plugin.
#
#####################################################################################################
[CmdletBinding(DefaultParameterSetName = "NotByPath")]
param
(
    [parameter(Mandatory = $true, ParameterSetName = "ByPath")]
    [string]
    $PowerShellHome
)

function Register-WinRmPlugin
{
    param
    (
        #
        # Expected Example:
        # %windir%\\system32\\PowerShell\\6.0.0\\pwrshplugin.dll
        #
        [string]
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $pluginAbsolutePath,

        #
        # Expected Example: powershell.6.0.0-beta.3
        #
        [string]
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $pluginEndpointName
    )

    $header = "Windows Registry Editor Version 5.00`n`n"

    $regKeyFormatString = "[HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\WSMAN\Plugin\{0}]`n"

    $regKeyName = '"ConfigXML"="{0}"'

    #
    # Example Values:
    #
    # Filename = %windir%\\system32\\PowerShell\\6.0.0\\pwrshplugin.dll
    # Name = PowerShell.6.0.0
    #
    $pluginArchitecture = "64"
    if ($env:PROCESSOR_ARCHITECTURE -match "x86")
    {
        $pluginArchitecture = "32"
    }
    $regKeyValueFormatString = '<PlugInConfiguration xmlns=\"http://schemas.microsoft.com/wbem/wsman/1/config/PluginConfiguration\" Name=\"{0}\" Filename=\"{1}\" SDKVersion=\"2\" XmlRenderingType=\"text\" Enabled=\"True\" OutputBufferingMode=\"Block\" ProcessIdleTimeoutSec=\"0\" Architecture=\"{2}\" UseSharedProcess=\"false\" RunAsUser=\"\" RunAsPassword=\"\" AutoRestart=\"false\"><InitializationParameters><Param Name=\"PSVersion\" Value=\"5.0\"/></InitializationParameters><Resources><Resource ResourceUri=\"http://schemas.microsoft.com/powershell/{0}\" SupportsOptions=\"true\" ExactMatch=\"true\"><Security Uri=\"http://schemas.microsoft.com/powershell/{0}\" ExactMatch=\"true\" Sddl=\"O:NSG:BAD:P(A;;GA;;;BA)S:P(AU;FA;GA;;;WD)(AU;SA;GXGW;;;WD)\"/><Capability Type=\"Shell\"/></Resource></Resources><Quotas IdleTimeoutms=\"7200000\" MaxConcurrentUsers=\"5\" MaxProcessesPerShell=\"15\" MaxMemoryPerShellMB=\"1024\" MaxShellsPerUser=\"25\" MaxConcurrentCommandsPerShell=\"1000\" MaxShells=\"25\" MaxIdleTimeoutms=\"43200000\"/></PlugInConfiguration>'
    $valueString = $regKeyValueFormatString -f $pluginEndpointName, $pluginAbsolutePath, $pluginArchitecture
    $keyValuePair = $regKeyName -f $valueString

    $regKey = $regKeyFormatString -f $pluginEndpointName

    $fileName = "$pluginEndpointName.reg"

    Set-Content -path .\$fileName "$header$regKey$keyValuePair`n"

    Write-Verbose "Performing WinRM registration with: $fileName"
    reg.exe import .\$fileName

    # Clean up
    Remove-Item .\$fileName
}

function Generate-PluginConfigFile
{
    param
    (
        [string]
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $pluginFile,

        [string]
        [parameter(Mandatory=$true)]
        [ValidateNotNullOrEmpty()]
        $targetPsHomeDir
    )

    # This always overwrites the file with a new version of it if the
    # script is invoked multiple times.
    Set-Content -Path $pluginFile -Value "PSHOMEDIR=$targetPsHomeDir"
    Add-Content -Path $pluginFile -Value "CORECLRDIR=$targetPsHomeDir"

    Write-Verbose "Created Plugin Config File: $pluginFile"
}

######################
#                    #
# Install the plugin #
#                    #
######################

if (! ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))
{
    Write-Error "WinRM registration requires Administrator rights. To run this cmdlet, start PowerShell with the `"Run as administrator`" option."
    Break
}

if ($PsCmdlet.ParameterSetName -eq "ByPath")
{
    $targetPsHome = $PowerShellHome
    $targetPsVersion = & "$targetPsHome\pwsh" -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
}
else
{
    ## Get the PSHome and PSVersion using the current powershell instance
    $targetPsHome = $PSHOME
    $targetPsVersion = $PSVersionTable.PSVersion.ToString()
}

Write-Verbose "Using PowerShell Version: $targetPsVersion" -Verbose

$pluginBasePath = Join-Path "$env:WINDIR\System32\PowerShell" $targetPsVersion

$resolvedPluginAbsolutePath = ""
if (! (Test-Path $pluginBasePath))
{
    Write-Verbose "Creating $pluginBasePath"
    $resolvedPluginAbsolutePath = New-Item -Type Directory -Path $pluginBasePath
}
else
{
    $resolvedPluginAbsolutePath = Resolve-Path $pluginBasePath
}

# The registration reg file requires "\\" instead of "\" in its path so it is properly escaped in the XML
$pluginRawPath = Join-Path $resolvedPluginAbsolutePath "pwrshplugin.dll"
$fixedPluginPath = $pluginRawPath -replace '\\','\\'

# This is forced to ensure the the file is placed correctly
Copy-Item $targetPsHome\pwrshplugin.dll $resolvedPluginAbsolutePath -Force -Verbose

$pluginFile = Join-Path $resolvedPluginAbsolutePath "RemotePowerShellConfig.txt"
Generate-PluginConfigFile $pluginFile $targetPsHome

$pluginEndpointName = "powershell.$targetPsVersion"

# Register the plugin
Register-WinRmPlugin $fixedPluginPath $pluginEndpointName

####################################################################
#                                                                  #
# Validations to confirm that everything was registered correctly. #
#                                                                  #
####################################################################

if (! (Test-Path $pluginFile))
{
    throw "WinRM Plugin configuration file not created. Expected = $pluginFile"
}

if (! (Test-Path $resolvedPluginAbsolutePath\pwrshplugin.dll))
{
    throw "WinRM Plugin DLL missing. Expected = $resolvedPluginAbsolutePath\pwrshplugin.dll"
}

try
{
    Write-Host "`nGet-PSSessionConfiguration $pluginEndpointName" -foregroundcolor "green"
    Get-PSSessionConfiguration $pluginEndpointName
}
catch [Microsoft.PowerShell.Commands.WriteErrorException]
{
    Write-Error "No remoting session configuration matches the name $pluginEndpointName."
}

Write-Host "Restarting WinRM to ensure that the plugin configuration change takes effect.`nThis is required for WinRM running on Windows SKUs prior to Windows 10." -foregroundcolor "green"
Restart-Service winrm

