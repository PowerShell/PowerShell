Import-LocalizedData LocalizedData -FileName Microsoft.PowerShell.ODataUtilsStrings.psd1


# This module doesn't support Arm because of Add-Type cmdlet
$ProcessorArchitecture = (Get-WmiObject -query "Select Architecture from Win32_Processor").Architecture

# 0 = x86
# 1 = MIPS
# 2 = Alpha
# 3 = PowerPC
# 5 = ARM
# 6 = Itanium
# 9 = x64
if ($ProcessorArchitecture -eq 5)
{
    throw $LocalizedData.ArchitectureNotSupported -f "ARM"
}

. "$PSScriptRoot\Microsoft.PowerShell.ODataUtilsHelper.ps1"

#########################################################
# Generates PowerShell module containing client side 
# proxy cmdlets that can be used to interact with an 
# OData based server side endpoint.
######################################################### 
function Export-ODataEndpointProxy 
{
    [CmdletBinding(
    DefaultParameterSetName='CDXML',
    SupportsShouldProcess=$true,
    HelpUri="https://go.microsoft.com/fwlink/?LinkId=510069")]
    [OutputType([System.IO.FileInfo])]
    param
    (
        [Parameter(Position=0, Mandatory=$true, ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $Uri,

        [Parameter(Position=1, Mandatory=$true, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $OutputModule,

        [Parameter(Position=2, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $MetadataUri,

        [Parameter(Position=3, ValueFromPipelineByPropertyName=$true)]
        [PSCredential] $Credential,
	
        [Parameter(Position=4, ValueFromPipelineByPropertyName=$true)]
        [ValidateSet('Put', 'Post', 'Patch')]
        [string] $CreateRequestMethod='Post',
	
        [Parameter(Position=5, ValueFromPipelineByPropertyName=$true)]
        [ValidateSet('Put', 'Post', 'Patch')]
        [string] $UpdateRequestMethod='Patch',

        [Parameter(Position=6, ValueFromPipelineByPropertyName=$true)]
        [ValidateSet('ODataAdapter', 'NetworkControllerAdapter', 'ODataV4Adapter')]
        [string] $CmdletAdapter='ODataAdapter',

        [Parameter(Position=7, ValueFromPipelineByPropertyName=$true)]
        [Hashtable] $ResourceNameMapping,

        [parameter (Position=8,ValueFromPipelineByPropertyName=$true)]
        [switch] $Force,

        [Parameter(Position=9, ValueFromPipelineByPropertyName=$true)]
        [Hashtable] $CustomData,

        [parameter (Position=10,ValueFromPipelineByPropertyName=$true)]
        [switch] $AllowClobber,

        [parameter (Position=11,ValueFromPipelineByPropertyName=$true)]
        [switch] $AllowUnsecureConnection,

        [parameter (Position=12,ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNull()]
        [Hashtable] $Headers
    )

    BEGIN 
    {
        if (!$MetadataUri)
        {
            $Uri = $Uri.TrimEnd('/')
            $MetadataUri = $Uri + '/$metadata'
            $PSBoundParameters["MetadataUri"] = $MetadataUri
        }

        # Validate to make sure that a valid URI is supplied as input.
        try
        {
            $connectionUri = [System.Uri]::new($Uri)
        }
        catch
        {
            $errorMessage = ($LocalizedData.InValidUri -f $Uri)
            $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidUriFormat" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $Uri
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        # Block Redfish-support for in-box version of the module and advise user to use a version from PS Gallery instead.
        # According to Redfish specification DSP0266v1.0.1 Redfish Service Metadata Document and Redfish Service Root URIs (used by Export-ODataEndpointProxy) are required to start with '/redfish/v1' (section "6.3 Redfish-Defined URIs and Relative URI Rules").
        # We use this as indicator of whether Export-ODataEndpointProxy was attempted against a Redfish endpoint.
        if($connectionUri.AbsolutePath.StartsWith('/redfish/',[StringComparison]::OrdinalIgnoreCase))
        {
            $errorMessage = $LocalizedData.RedfishNotEnabled
            $exception = [System.InvalidOperationException]::new($errorMessage)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyRedfishNotEnabled" $errorMessage ([System.Management.Automation.ErrorCategory]::NotEnabled) $exception $Uri
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        if($connectionUri.Scheme -eq "http" -and !$AllowUnsecureConnection.IsPresent)
        {
            $errorMessage = ($LocalizedData.AllowUnsecureConnectionMessage -f $PSCmdlet.MyInvocation.MyCommand.Name, $Uri, "Uri")
            $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyUnSecureConnection" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $Uri
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        $OutputModuleExists = Test-Path -Path $OutputModule -PathType Container

        if($OutputModuleExists -and ($Force -eq $false))
        {
            $errorMessage = ($LocalizedData.ModuleAlreadyExistsAndForceParameterIsNotSpecified -f $OutputModule)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyOutputModuleExists" $errorMessage ([System.Management.Automation.ErrorCategory]::ResourceExists) $null $OutputModule
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }
        
        $isWhatIf = $psboundparameters.ContainsKey("WhatIf")

        if(!$OutputModuleExists)
        {
            if(!$isWhatIf)
            {
                $OutputModule = (New-Item -Path $OutputModule -ItemType Directory).FullName
            }
        }
        else
        {
            $resolvedOutputModulePath = Resolve-Path -Path $OutputModule -ErrorAction Stop -Verbose
            if($resolvedOutputModulePath.Count -gt 1)
            {    
                $errorMessage = ($LocalizedData.OutputModulePathIsNotUnique -f $OutputModule)
                $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyOutputModulePathIsNotUnique" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $OutputModule
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }
            
            # Make sure that the path specified is a valid file system directory path.
            if([system.IO.Directory]::Exists($resolvedOutputModulePath))
            {
                $OutputModule = $resolvedOutputModulePath
            }
            else
            {
                $errorMessage = ($LocalizedData.OutputModulePathIsNotFileSystemPath -f $OutputModule)
                $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyPathIsNotFileSystemPath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $OutputModule
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            } 
        }

        $rootDir = [System.IO.Directory]::GetDirectoryRoot($OutputModule)

        if($rootDir -eq $OutputModule)
        {
            $errorMessage = ($LocalizedData.InvalidOutputModulePath -f $OutputModule)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidOutputModulePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $OutputModule
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        if(!$isWhatIf)
        {
            $progressBarStatus = ($LocalizedData.ProgressBarMessage -f $Uri)
            ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 0 100 100 1
        }

        # Add parameters to $PSBoundParameters, which were not passed by user, but the default value is set
        $parametersWithDefaultValue = @("CreateRequestMethod", "UpdateRequestMethod", "CmdletAdapter")

        foreach ($parameterWithDefaultValue in $parametersWithDefaultValue)
        {
            if (!$PSBoundParameters.ContainsKey($parameterWithDefaultValue))
            {
                $PSBoundParameters.Add($parameterWithDefaultValue, (Get-Variable $parameterWithDefaultValue).Value)
            }
        }
    }

    END 
    {
        if($pscmdlet.ShouldProcess($Uri))
        {
            try
            {
                $PSBoundParameters.Add("ProgressBarStatus", $progressBarStatus)
                $PSBoundParameters.Add("PSCmdlet", $PSCmdlet)

                # Import module based on selected CmdletAdapter
                $adapterToImport = $CmdletAdapter
                
                # NetworkControllerAdapter relies on ODataAdapter
                if ($CmdletAdapter -eq 'NetworkControllerAdapter')
                {
                    $adapterToImport = 'ODataAdapter'
                }
 
                Write-Debug ($LocalizedData.SelectedAdapter -f $adapterPSScript)

                $adapterPSScript = "$PSScriptRoot\Microsoft.PowerShell." + $adapterToImport + ".ps1"
                
                . $adapterPSScript
                ExportODataEndpointProxy @PSBoundParameters
            }
            catch
            {
                $errorMessage = ($LocalizedData.InValidMetadata -f $Uri)
                $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
                $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidInput" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $Uri
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }
            finally
            {
                Write-Progress -Activity "Export-ODataEndpointProxy" -Completed
            }
        }
    }
}

Export-ModuleMember -Function @('Export-ODataEndpointProxy')
