# Base class definitions used by the actual Adapter modules
$global:BaseClassDefinitions = @"
using System; 

namespace ODataUtils
{
    public class TypeProperty
    {
        public String Name;

        // OData Type Name, e.g. Edm.Int32
        public String TypeName;

        public bool IsKey;
        public bool IsMandatory;
        public bool? IsNullable;
    }

    public class NavigationProperty
    {
        public String Name;

        public String FromRole;
        public String ToRole;
        public String AssociationNamespace;
        public String AssociationName;
    }

    public class EntityTypeBase
    {
        public String Namespace;        
        public String Name;
        public TypeProperty[] EntityProperties;
        public bool IsEntity;
        public EntityTypeBase BaseType;
    }

    public class AssociationType
    {
        public String Namespace;

        public EntityTypeBase EndType1;
        public String Multiplicity1;
        public String NavPropertyName1;

        public EntityTypeBase EndType2;
        public String Multiplicity2;
        public String NavPropertyName2;
    }
    
    public class EntitySet
    {
        public String Namespace;
        public String Name;
        public EntityTypeBase Type;
    }

    public class AssociationSet
    {
        public String Namespace;
        public String Name;
        public AssociationType Type;
    }

    public class Action
    {
        public String Namespace;
        public String Verb;
        public EntitySet EntitySet;
        public Boolean IsSideEffecting;
        public Boolean IsBindable;
        public Boolean IsSingleInstance;
        public TypeProperty[] Parameters;
    }

    public class MetadataBase
    {
        // Desired destination namespace
        public String Namespace;
    }

    public class CmdletParameter
    {
        public CmdletParameter()
        {
        }

        public CmdletParameter(String type, String name)
        {
            this.Type = type;
            this.Name = name;
            this.Qualifiers = new String[] { "Parameter(ValueFromPipelineByPropertyName=`$true)" };
        }

        public String[] Qualifiers;
        public String Type;
        public String Name;
    }

    public class CmdletParameters
    {
        public CmdletParameter[] Parameters;
    }

    public class ReferentialConstraint
    {
        public String Property;
        public String ReferencedProperty;
    }

    public class OnDelete
    {
        public String Action;
    }

    public class NavigationPropertyV4
    {
        public String Name;
        public String Type;
        public bool Nullable;
        public String Partner;
        public bool ContainsTarget;
        public ReferentialConstraint[] ReferentialConstraints;
        public OnDelete OnDelete;
    }

    public class NavigationPropertyBinding
    {
        public String Path;
        public String Target;
    }

    public class EntityTypeV4 : EntityTypeBase
    {
        public String Alias;
        public NavigationPropertyV4[] NavigationProperties;
        public String BaseTypeStr;
    }
       
    public class SingletonType
    {
        public String Namespace;
        public String Alias;
        public String Name;
        public String Type;
        public NavigationPropertyBinding[] NavigationPropertyBindings;
    } 

    public class EntitySetV4
    {
        public String Namespace;
        public String Alias;
        public String Name;
        public EntityTypeV4 Type;
    }

    public class EnumMember
    {
        public String Name;
        public String Value;
    }

    public class EnumType
    {
        public String Namespace;
        public String Alias;
        public String Name;
        public String UnderlyingType;
        public bool IsFlags;
        public EnumMember[] Members;
    }

    public class ActionV4
    {
        public String Namespace;
        public String Alias;
        public String Name;
        public String Action;
        public EntitySetV4 EntitySet;
        public TypeProperty[] Parameters;
    }

    public class FunctionV4
    {
        public String Namespace;
        public String Alias;
        public String Name;
        public bool Function;
        public String EntitySet;
        public String ReturnType;
        public Parameter[] Parameters;
    }

    public class Parameter
    {
        public String Name;
        public String Type;
        public bool Nullable;
    }

    public class ReferenceInclude
    {
        public String Namespace;
        public String Alias;
    }

    public class Reference
    {
        public String Uri;
    }

    public class MetadataV4 : MetadataBase
    {
        public string ODataVersion;
        public string Uri;
        public string MetadataUri;
        public string Alias;
        public Reference[] References;
        public string DefaultEntityContainerName;
        public EntitySetV4[] EntitySets;        
        public EntityTypeV4[] EntityTypes;
        public SingletonType[] SingletonTypes;        
        public EntityTypeV4[] ComplexTypes;
        public EntityTypeV4[] TypeDefinitions;
        public EnumType[] EnumTypes;
        public ActionV4[] Actions;
        public FunctionV4[] Functions;
    }

    public class ReferencedMetadata
    {
        public System.Collections.ArrayList References;
    }

    public class ODataEndpointProxyParameters
    {
        public String Uri;
        public String MetadataUri;
        public System.Management.Automation.PSCredential Credential;
        public String OutputModule;

        public bool Force;
        public bool AllowClobber;
        public bool AllowUnsecureConnection;
    }

    public class EntityType : EntityTypeBase
    {
        public NavigationProperty[] NavigationProperties;
    }
    
    public class Metadata : MetadataBase
    {
        public String DefaultEntityContainerName;
        public EntitySet[] EntitySets;        
        public EntityType[] EntityTypes;
        public EntityType[] ComplexTypes;
        public AssociationSet[] Associations;
        public Action[] Actions;
    }
}
"@

#########################################################
# GetMetaData is a helper function used to fetch metadata 
# from the specified file or web URL.
######################################################### 
function GetMetaData 
{
    param
    (
        [string] $metaDataUri,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet,
        [PSCredential] $credential,
        [Hashtable]    $headers
    )

    # $metaDataUri is already validated at the cmdlet layer.
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "GetMetaData") }
    Write-Verbose ($LocalizedData.VerboseReadingMetadata -f $metaDataUri)

    try
    {
        $uri = [System.Uri]::new($metadataUri)

        # By default, proxy generation is supported on secured Uri (i.e., https).
        # However if the user trusts the unsecure http uri, then they can override
        # the security check by specifying -AllowSecureConnection parameter during
        # proxy generation. 
        if($uri.Scheme -eq "http" -and !$AllowUnsecureConnection.IsPresent)
        {
            $errorMessage = ($LocalizedData.AllowUnsecureConnectionMessage -f $callerPSCmdlet.MyInvocation.MyCommand.Name, $uri, "MetaDataUri")
            $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyUnSecureConnection" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $uri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }
    }
    catch
    {
        $errorMessage = ($LocalizedData.InValidMetadata -f $MetadataUri)
        $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetadataUriFormat" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $MetadataUri
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }

    if($uri.IsFile)
    {
        if ($credential -ne $null)
        {
            $fileExists = Test-Path -Path $metaDataUri -PathType Leaf -Credential $credential -ErrorAction Stop
        }
        else
        {
            $fileExists = Test-Path -Path $metaDataUri -PathType Leaf -ErrorAction Stop
        }

        if($fileExists)
        {
            $metaData = Get-Content -Path $metaDataUri -ErrorAction Stop
        }
        else
        {
            $errorMessage = ($LocalizedData.MetadataUriDoesNotExist -f $MetadataUri)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyMetadataFileDoesNotExist" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $MetadataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }
    }
    else
    {
        try
        {
            $cmdParams = @{'Uri'= $metaDataUri ; 'UseBasicParsing'=$true; 'ErrorAction'= 'Stop'}

            if ($credential -ne $null)
            {
                $cmdParams.Add('Credential', $credential)
            }

            if ($headers -ne $null)
            {
                $cmdParams.Add('Headers', $headers)
            }

            $webResponse = Invoke-WebRequest @cmdParams
        }
        catch
        {
            $errorMessage = ($LocalizedData.MetadataUriDoesNotExist -f $MetadataUri)
            $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyMetadataUriDoesNotExist" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $MetadataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }

        if($webResponse -ne $null)
        {
            if ($webResponse.StatusCode -eq 200)
            {
                $metaData = $webResponse.Content

                if ($metadata -eq $null)
                {
                    $errorMessage = ($LocalizedData.EmptyMetadata -f $MetadataUri)
                    $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyMetadataIsEmpty" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $MetadataUri
                    $callerPSCmdlet.ThrowTerminatingError($errorRecord)
                }
            }
            else
            {
                $errorMessage = ($LocalizedData.InvalidEndpointAddress -f $MetadataUri, $webResponse.StatusCode)
                $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidEndpointAddress" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $null $MetadataUri
                $callerPSCmdlet.ThrowTerminatingError($errorRecord)
            }
        }
    }

    if($metaData -ne $null)
    {
        try
        {
            [xml] $metadataXML = $metaData
        }
        catch
        {
            $errorMessage = ($LocalizedData.InValidMetadata -f $MetadataUri)
            $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetadata" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $MetadataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }
    }

    return $metadataXML
}

#########################################################
# VerifyMetadataHelper is a helper function used to 
# validate if Error/Warning message has to be displayed 
# during command collision. 
#########################################################
function VerifyMetadataHelper 
{
    param
    (
        [string]   $localizedDataErrorString,
        [string]   $localizedDataWarningString,
        [string]   $entitySetName,
        [string]   $currentCommandName,
        [string]   $metaDataUri,
        [boolean]  $allowClobber,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    if($localizedDataErrorString -eq $null) { throw ($LocalizedData.ArguementNullError -f "localizedDataErrorString", "VerifyMetadataHelper") }
    if($localizedDataWarningString -eq $null) { throw ($LocalizedData.ArguementNullError -f "localizedDataWarningString", "VerifyMetadataHelper") }

    if(!$allowClobber)
    {
        # Write Error message and skip current Entity Set.
        $errorMessage = ($localizedDataErrorString -f $entitySetName, $currentCommandName)
        $exception = [System.InvalidOperationException]::new($errorMessage)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointDefaultPropertyCollision" $null ([System.Management.Automation.ErrorCategory]::InvalidOperation) $exception $metaDataUri
        $callerPSCmdlet.WriteError($errorRecord)
    }
    else
    {                   
        $warningMessage = ($localizedDataWarningString -f $entitySetName, $currentCommandName)
        $callerPSCmdlet.WriteWarning($warningMessage)
    }
}

#########################################################
# CreateErrorRecordHelper is a helper function used to 
# create an error record.
#########################################################
function CreateErrorRecordHelper 
{
    param 
    (
        [string] $errorId,
        [string] $errorMessage,
        [System.Management.Automation.ErrorCategory] $errorCategory,
        [Exception] $exception,
        [object] $targetObject
    )

    if($exception -eq $null)
    {
        $exception = New-Object System.IO.IOException $errorMessage
    }

    $errorRecord = New-Object System.Management.Automation.ErrorRecord $exception, $errorId, $errorCategory, $targetObject
    return $errorRecord
}

#########################################################
# ProgressBarHelper is a helper function used to 
# used to display progress message. 
#########################################################
function ProgressBarHelper 
{
    param 
    (
        [string] $cmdletName,
        [string] $status,
        [double] $previousSegmentWeight,
        [double] $currentSegmentWeight,
        [int]    $totalNumberofEntries,
        [int]    $currentEntryCount
    )

    if($cmdletName -eq $null) { throw ($LocalizedData.ArguementNullError -f "CmdletName", "ProgressBarHelper") }
    if($status -eq $null) { throw ($LocalizedData.ArguementNullError -f "Status", "ProgressBarHelper") }

    if($currentEntryCount -gt 0 -and 
       $totalNumberofEntries -gt 0 -and 
       $previousSegmentWeight -ge 0 -and 
       $currentSegmentWeight -gt 0)
    {
        $entryDefaultWeight = $currentSegmentWeight/[double]$totalNumberofEntries
        $percentComplete = $previousSegmentWeight + ($entryDefaultWeight * $currentEntryCount)
        Write-Progress -Activity $cmdletName -Status $status -PercentComplete $percentComplete 
    }
}

#########################################################
# Convert-ODataTypeToCLRType is a helper function used to 
# Convert OData type to its CLR equivalent.
#########################################################
function Convert-ODataTypeToCLRType 
{
    param
    (
        [string] $typeName,
        [Hashtable] $complexTypeMapping
    )

    if($typeName -eq $null) { throw ($LocalizedData.ArguementNullError -f "TypeName", "Convert-ODataTypeToCLRType ") }

    switch ($typeName) 
    {
        "Edm.Binary" {"Byte[]"}
        "Edm.Boolean" {"Boolean"}
        "Edm.Byte" {"Byte"}
        "Edm.DateTime" {"DateTime"}
        "Edm.Decimal" {"Decimal"}
        "Edm.Double" {"Double"}
        "Edm.Single" {"Single"}
        "Edm.Guid" {"Guid"}
        "Edm.Int16" {"Int16"}
        "Edm.Int32" {"Int32"}
        "Edm.Int64" {"Int64"}
        "Edm.SByte" {"SByte"}
        "Edm.String" {"String"}
        "Edm.PropertyPath"  {"String"}
        "switch" {"switch"}
        "Edm.DateTimeOffset" {"DateTimeOffset"}
        default 
        {
            if($complexTypeMapping -ne $null -and 
               $complexTypeMapping.Count -gt 0 -and 
               $complexTypeMapping.ContainsKey($typeName))
            {
                $typeName
            }
            else
            {
                $regex = "Collection\((.+)\)"
                if ($typeName -match $regex)
                {
                    $insideTypeName = Convert-ODataTypeToCLRType $Matches[1] $complexTypeMapping
                    "$insideTypeName[]"
                }
                else
                {
                    "PSObject"
                }
            }
        }
    }
}

#########################################################
# SaveCDXMLHeader is a helper function used 
# to save CDXML headers common to all 
# PSODataUtils modules.
#########################################################
function SaveCDXMLHeader 
{
    param
    (
        [System.Xml.XmlWriter] $xmlWriter,
        [string] $uri,
        [string] $className,
        [string] $defaultNoun,
        [string] $cmdletAdapter
    )

    # $uri & $cmdletAdapter are already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLHeader") }
    if($defaultNoun -eq $null) { throw ($LocalizedData.ArguementNullError -f "DefaultNoun", "SaveCDXMLHeader") }

    if ($className -eq 'ServiceActions' -Or $cmdletAdapter -eq "NetworkControllerAdapter")
    {
        $entityName = ''
    }
    else
    {
        $entityName = $className
    }

    if ($uri[-1] -ne '/')
    {
        $fullName = "$uri/$entityName"
    }
    else
    {
        $fullName = "$uri$entityName"
    }

    $xmlWriter.Formatting = 'Indented'
    $xmlWriter.Indentation = 2
    $xmlWriter.IndentChar = ' '

    $xmlWriter.WriteStartDocument()

    $today=Get-Date
    $xmlWriter.WriteComment("This module was autogenerated by PSODataUtils on $today.")

    $xmlWriter.WriteStartElement('PowerShellMetadata')
    $xmlWriter.WriteAttributeString('xmlns', 'http://schemas.microsoft.com/cmdlets-over-objects/2009/11')

        $xmlWriter.WriteStartElement('Class')
        $xmlWriter.WriteAttributeString('ClassName', $fullName)
        $xmlWriter.WriteAttributeString('ClassVersion', '1.0.0')
        
        $DotNetAdapter = 'Microsoft.PowerShell.Cmdletization.OData.ODataCmdletAdapter'

        if ($CmdletAdapter -eq "NetworkControllerAdapter") {
            $DotNetAdapter = 'Microsoft.PowerShell.Cmdletization.OData.NetworkControllerCmdletAdapter'
        }
        elseif ($CmdletAdapter -eq "ODataV4Adapter") {
            $DotNetAdapter = 'Microsoft.PowerShell.Cmdletization.OData.ODataV4CmdletAdapter'
        }
        
        $xmlWriter.WriteAttributeString('CmdletAdapter', $DotNetAdapter + ', Microsoft.PowerShell.Cmdletization.OData, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35')

        $xmlWriter.WriteElementString('Version', '1.0')
        $xmlWriter.WriteElementString('DefaultNoun', $defaultNoun)

    $xmlWriter
}

#########################################################
# SaveCDXMLFooter is a helper function used 
# to save CDXML closing attributes corresponding 
# to SaveCDXMLHeader function.
#########################################################
function SaveCDXMLFooter 
{
    param
    (
        [System.Xml.XmlWriter] $xmlWriter
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLFooter") }

    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndDocument()

    $xmlWriter.Flush()
    $xmlWriter.Close()
}

#########################################################
# AddParametersNode is a helper function used 
# to add parameters to the generated proxy cmdlet, 
# based on mandatoryProperties and otherProperties.
# PrefixForKeys is used by associations to append a 
# prefix to PowerShell parameter name.
#########################################################
function AddParametersNode 
{
    param
    (
        [Parameter(Mandatory=$true)]
        [System.Xml.XmlWriter]       $xmlWriter,
        [ODataUtils.TypeProperty[]]  $keyProperties,
        [ODataUtils.TypeProperty[]]  $mandatoryProperties,
        [ODataUtils.TypeProperty[]]  $otherProperties,
        [string]    $prefixForKeys,
        [boolean]   $addForceParameter,
        [boolean]   $addParametersElement,
        [Hashtable] $complexTypeMapping
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "AddParametersNode") }

    if(($keyProperties.Length -gt 0) -or 
       ($mandatoryProperties.Length -gt 0) -or 
       ($otherProperties.Length -gt 0) -or
       ($addForceParameter))
    {
        if($addParametersElement)
        {
            $xmlWriter.WriteStartElement('Parameters')
        }

        $pos = 0

        if ($keyProperties -ne $null)
        {
            $pos = AddParametersCDXML $xmlWriter $keyProperties $pos $true $prefixForKeys ":Key" $complexTypeMapping
        }

        if ($mandatoryProperties -ne $null)
        {
            $pos = AddParametersCDXML $xmlWriter $mandatoryProperties $pos $true $null $null $complexTypeMapping
        }

        if ($otherProperties -ne $null)
        {
            $pos = AddParametersCDXML $xmlWriter $otherProperties $pos $false $null $null $complexTypeMapping
        }

        if($addForceParameter)
        {
            $forceParameter = [ODataUtils.TypeProperty] @{
                "Name" = "Force";
                "TypeName" = "switch";
                "IsNullable" = $false
            }

            $pos = AddParametersCDXML $xmlWriter $forceParameter $pos $false $null $null $complexTypeMapping
        }

        if($addParametersElement)
        {
            $xmlWriter.WriteEndElement()
        }
    }
}

#########################################################
# AddParametersCDXML is a helper function used 
# to add Parameter node to CDXML based on properties.
# Prefix is appended to PS parameter names, used for 
# associations. Suffix is appended to all parameter 
# names, for ex. to differentiate keys. returns new $pos
#########################################################
function AddParametersCDXML 
{
    param
    (
        [Parameter(Mandatory=$true)]
        [System.Xml.XmlWriter] $xmlWriter,
        [ODataUtils.TypeProperty[]] $properties,
        [Parameter(Mandatory=$true)]
        [int] $pos,
        [bool] $isMandatory,
        [string] $prefix,
        [string] $suffix,
        [Hashtable] $complexTypeMapping
    )

    $properties | ? { $_ -ne $null } | % {
        $xmlWriter.WriteStartElement('Parameter')
        $xmlWriter.WriteAttributeString('ParameterName', $_.Name + $suffix)
            $xmlWriter.WriteStartElement('Type')
            $PSTypeName = Convert-ODataTypeToCLRType $_.TypeName $complexTypeMapping
            $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
            $xmlWriter.WriteEndElement()

            $xmlWriter.WriteStartElement('CmdletParameterMetadata')
            $xmlWriter.WriteAttributeString('PSName', $prefix + $_.Name)
            $xmlWriter.WriteAttributeString('IsMandatory', ($isMandatory).ToString().ToLowerInvariant())
            $xmlWriter.WriteAttributeString('Position', $pos)
            if($isMandatory)
            {
                $xmlWriter.WriteAttributeString('ValueFromPipelineByPropertyName', 'true')
            }
            $xmlWriter.WriteEndElement()
        $xmlWriter.WriteEndElement()

        $pos++
    }

    $pos
}

#########################################################
# GenerateSetProxyCmdlet is a helper function used 
# to generate Set-* proxy cmdlet. The proxy cmdlet is
# generated in the CDXML compliant format. 
#########################################################
function GenerateSetProxyCmdlet 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [object[]]  $keyProperties,
        [object[]]  $nonKeyProperties,
        [Hashtable] $complexTypeMapping
    )

    # $cmdletAdapter is already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "GenerateSetProxyCmdlet") }

    $xmlWriter.WriteStartElement('Cmdlet')
        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Set')
        $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', 'Default')
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', 'Update')
        $xmlWriter.WriteAttributeString('CmdletParameterSet', 'Default')

            AddParametersNode $xmlWriter $keyProperties $null $nonKeyProperties $null $true $true $complexTypeMapping
        $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()
}
