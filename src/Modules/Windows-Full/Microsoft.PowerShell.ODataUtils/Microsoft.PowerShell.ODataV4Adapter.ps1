Import-LocalizedData LocalizedData -FileName Microsoft.PowerShell.ODataUtilsStrings.psd1

# Add .NET classes used by the module
Add-Type -TypeDefinition $global:BaseClassDefinitions

#########################################################
# Generates PowerShell module containing client side 
# proxy cmdlets that can be used to interact with an 
# OData based server side endpoint.
######################################################### 
function ExportODataEndpointProxy 
{
    param
    (
        [string] $Uri,
        [string] $OutputModule,
        [string] $MetadataUri,
        [PSCredential] $Credential,
        [string] $CreateRequestMethod,
        [string] $UpdateRequestMethod,
        [string] $CmdletAdapter,
        [Hashtable] $ResourceNameMapping,
        [switch] $Force,
        [Hashtable] $CustomData,
        [switch] $AllowClobber,
        [switch] $AllowUnsecureConnection,
        [Hashtable] $Headers,
        [string] $ProgressBarStatus,
        [System.Management.Automation.PSCmdlet] $PSCmdlet
    )

    # Record of all metadata XML files which have been opened for parsing
    # used to avoid parsing the same file twice, if referenced in multiple
    # metadata files
    $script:processedFiles = @()
    
    # Record of all referenced and parsed metadata files (including entry point metadata)  
    $script:GlobalMetadata = New-Object System.Collections.ArrayList

    # The namespace name might have invalid characters or might be conflicting with class names in inheritance scenarios
    # We will be normalizing these namespaces and saving them into $normalizedNamespaces, where key is the original namespace and value is normalized namespace
    $script:normalizedNamespaces = @{}

    # This information will be used during recursive referenced metadata files loading
    $ODataEndpointProxyParameters = [ODataUtils.ODataEndpointProxyParameters] @{
        "MetadataUri" = $MetadataUri;
        "Uri" = $Uri;
        "Credential" = $Credential;
        "OutputModule" = $OutputModule;
        "Force" = $Force;
        "AllowClobber" = $AllowClobber;
        "AllowUnsecureConnection" = $AllowUnsecureConnection;
    }

    # Recursively fetch all metadatas (referenced by entry point metadata)
    $GlobalMetadata = GetTypeInfo -callerPSCmdlet $pscmdlet -MetadataUri $MetadataUri -ODataEndpointProxyParameters $ODataEndpointProxyParameters -Headers $Headers
    # Now that we are done with recursive metadata references parsing we can get rid of this variable
    $script:GlobalMetadata = $null

    VerifyMetadata $GlobalMetadata $AllowClobber.IsPresent $PSCmdlet $ProgressBarStatus

    # Get Uri Resource path key format. It can be either 'EmbeddedKey' or 'SeparateKey'. 
    # If not provided, default value will be set to 'EmbeddedKey'.
    $UriResourcePathKeyFormat = 'EmbeddedKey'
    if ($CustomData -and $CustomData.ContainsKey("UriResourcePathKeyFormat"))
    {
        $UriResourcePathKeyFormat = $CustomData."UriResourcePathKeyFormat"
    }

    GenerateClientSideProxyModule $GlobalMetadata $ODataEndpointProxyParameters $OutputModule $CreateRequestMethod $UpdateRequestMethod $CmdletAdapter $ResourceNameMapping $CustomData $UriResourcePathKeyFormat $ProgressBarStatus $script:normalizedNamespaces
}

#########################################################
# GetTypeInfo is a helper method used to get all the types 
# from metadata files in a recursive manner
#########################################################
function GetTypeInfo 
{
    param
    (
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet,
        [string] $MetadataUri,
        [ODataUtils.ODataEndpointProxyParameters] $ODataEndpointProxyParameters,
        [Hashtable] $Headers
    )

    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "callerPSCmdlet", "GetTypeInfo") }

    $metadataSet = New-Object System.Collections.ArrayList
    $metadataXML = GetMetaData $MetadataUri $callerPSCmdlet $ODataEndpointProxyParameters.Credential $Headers $ODataEndpointProxyParameters.AllowUnsecureConnection
    $script:processedFiles += $MetadataUri
    
    # parses all referenced metadata XML files recursively
    foreach ($reference in $metadataXML.Edmx.Reference) 
    {
        if (-not $script:processedFiles.Contains($reference.Uri)) 
        {
            $tmpMetadataSet = $null
            $tmpMetadataSet = GetTypeInfo -callerPSCmdlet $callerPSCmdlet -MetadataUri $reference.Uri -ODataEndpointProxyParameters $ODataEndpointProxyParameters
            AddMetadataToMetadataSet -Metadatas $metadataSet -NewMetadata $tmpMetadataSet
        }
    }

    $metadatas = ParseMetadata -MetadataXML $metadataXML -ODataVersion $metadataXML.Edmx.Version -MetadataUri $MetadataUri -Uri $ODataEndpointProxyParameters.Uri -MetadataSet $script:GlobalMetadata
    AddMetadataToMetadataSet -Metadatas $script:GlobalMetadata -NewMetadata $metadatas
    AddMetadataToMetadataSet -Metadatas $metadataSet -NewMetadata $metadatas

    return $metadataSet
}

function AddMetadataToMetadataSet
{
    param
    (
        [System.Collections.ArrayList] $Metadatas,
        $NewMetadata
    )

    if($NewMetadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "NewMetadata", "AddMetadataToMetadataSet") }

    if ($NewMetadata.GetType().Name -eq 'MetadataV4')
    {
        $Metadatas.Add($NewMetadata) | Out-Null
    }
    else
    {
        $Metadatas.AddRange($NewMetadata) | Out-Null
    }
}

#########################################################
# Normalization of Namespace name will be required in following scenarios:
# 1. Namespace name contains combination of dots and numbers
# 2. Namespace name collides with Class name (EntityType, EntitySet, etc.)
# If normalization is needed, all dots will be replaced with underscores and Ns suffix added
# User will receive warning notifying her about the namespace name change
#########################################################
function NormalizeNamespace
{
    param
    (
        [string] $MetadataNamespace,
        [string] $MetadataUri,
        [Hashtable] $NormalizedNamespaces,
        [boolean] $DoesNamespaceConflictsWithClassName
    )

    $doesNamespaceContainsInvalidChars = $false

    # Check if namespace name contains invalid combination if dots and numbers
    if ($MetadataNamespace -match '\.[0-9]' -or $MetadataNamespace -match '[0-9]\.')
    {
        # Normalization needed
        $doesNamespaceContainsInvalidChars = $true
    }

    # Normalize if needed
    if ($doesNamespaceContainsInvalidChars -or $DoesNamespaceConflictsWithClassName)
    {
        if ($NormalizedNamespaces.ContainsKey($MetadataNamespace))
        {
            # It's possible we've already attempted to normalize that namespace. In that case we'll update normalized name.
            $NormalizedNamespaces[$MetadataNamespace] = NormalizeNamespaceHelper $NormalizedNamespaces[$MetadataNamespace] $doesNamespaceContainsInvalidChars $DoesNamespaceConflictsWithClassName
        }
        else
        {
            $NormalizedNamespaces.Add($MetadataNamespace, (NormalizeNamespaceHelper $MetadataNamespace $doesNamespaceContainsInvalidChars $DoesNamespaceConflictsWithClassName))
        }
    }

    # Print warning 
    if ($doesNamespaceContainsInvalidChars)
    {
        # Normalization needed
        $warningMessage = ($LocalizedData.InValidSchemaNamespaceContainsInvalidChars -f $MetadataUri, $MetadataNamespace, $NormalizedNamespaces[$MetadataNamespace])
        Write-Warning $warningMessage
    }
    if ($DoesNamespaceConflictsWithClassName)
    {
        # Collision between namespace name and type name detected (example: namespace TaskService { class Service : Service.BasicService { ... } ... })
        # Normalization needed
        $warningMessage = ($LocalizedData.InValidSchemaNamespaceConflictWithClassName -f $MetadataUri, $MetadataNamespace, $NormalizedNamespaces[$MetadataNamespace])
        Write-Warning $warningMessage
    }
}

function NormalizeNamespaceCollisionWithClassName
{
    param
    (
        [string] $InheritingType,
        [string] $BaseTypeName,
        [string] $MetadataUri
    )

    if (![string]::IsNullOrEmpty($BaseTypeName))
    {
        $dotNetNamespace = ''
        if ($BaseTypeName.LastIndexOf(".") -gt 0)
        {
            # BaseTypeStr contains Namespace and TypeName. Extract Namespace name.
            $dotNetNamespace = $BaseTypeName.SubString(0, $BaseTypeName.LastIndexOf("."))
        }
            
        if (![string]::IsNullOrEmpty($dotNetNamespace) -and $InheritingType -eq $dotNetNamespace)
        {
            # Collision between namespace name and type name detected (example: namespace TaskService { class Service : Service.BasicService { ... } ... })
            # Normalization needed
            NormalizeNamespace $dotNetNamespace $MetadataUri $script:normalizedNamespaces $true
            break
        }
    }
}

#########################################################
# This helper method is used by functions, 
# writing directly to CDXML files or to .Net namespace/class definitions ComplexTypes file
#########################################################
function GetNamespace
{
    param
    (
        [string] $Namespace,
        $NormalizedNamespaces,
        [boolean] $isClassNameIncluded = $false
    )

    $dotNetNamespace = $Namespace
    $dotNetClassName = ''

    # Extract only namespace name
    if ($isClassNameIncluded)
    {
        if ($Namespace.LastIndexOf(".") -gt 0)
        {
            # For example, from following namespace (Namespace.TypeName) Service.1.0.0.Service we'll extract only namespace name, which is Service.1.0.0 
            $dotNetNamespace = $Namespace.SubString(0, $Namespace.LastIndexOf("."))
            $dotNetClassName = $Namespace.SubString($Namespace.LastIndexOf(".") + 1, $Namespace.Length - $Namespace.LastIndexOf(".") - 1) 
        }    
    }

    # Check if the namespace has to be normalized.
    if ($NormalizedNamespaces.ContainsKey($dotNetNamespace))
    {
        $dotNetNamespace = $NormalizedNamespaces.Get_Item($dotNetNamespace)
    }
    
    if (![string]::IsNullOrEmpty($dotNetClassName))
    {
        return ($dotNetNamespace + "." + $dotNetClassName)
    }
    else 
    {
        return $dotNetNamespace
    }
}

function NormalizeNamespaceHelper 
{
    param
    (
        [string] $Namespace,
        [boolean] $DoesNamespaceContainsInvalidChars,
        [boolean] $DoesNamespaceConflictsWithClassName
    )

    # For example, following namespace: Service.1.0.0
    # Will change to: Service_1_0_0
    # Ns postfix in Namespace name will allow to differentiate between this namespace 
    # and a colliding type name from different namespace
    $updatedNs = $Namespace
    if ($DoesNamespaceContainsInvalidChars)
    {
        $updatedNs = $updatedNs.Replace('.', '_')
    }
    if ($DoesNamespaceConflictsWithClassName)
    {
        $updatedNs = $updatedNs + "Ns"
    }

    $updatedNs
}

#########################################################
# Processes EntityTypes (OData V4 schema) from plain text 
# xml metadata into our custom structure
#########################################################
function ParseEntityTypes
{
    param
    (
        [System.Xml.XmlElement] $SchemaXML,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        [hashtable] $EntityAndComplexTypesQueue,
        [string] $CustomNamespace,
        [AllowEmptyString()]
        [string] $Alias
    )

    if($SchemaXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaXML", "ParseEntityTypes") }

    foreach ($entityType in $SchemaXML.EntityType)
    {
        $baseType = $null

        if ($entityType.BaseType -ne $null)
        {
            # add it to the processing queue
            $baseType = GetBaseType $entityType $Metadata $SchemaXML.Namespace $GlobalMetadata
            if ($baseType -eq $null)
            {
                $EntityAndComplexTypesQueue[$entityType.BaseType] += @(@{type='EntityType'; value=$entityType})
            }

            # Check if Namespace has to be normalized because of the collision with the inheriting Class name
            NormalizeNamespaceCollisionWithClassName -InheritingType $entityType.Name -BaseTypeName $entityType.BaseType -MetadataUri $Metadata.Uri
        }
        
        [ODataUtils.EntityTypeV4] $newType = ParseMetadataTypeDefinition $entityType $baseType $Metadata $schema.Namespace $Alias $true $entityType.BaseType
        $Metadata.EntityTypes += $newType
        AddDerivedTypes $newType $entityAndComplexTypesQueue $Metadata $SchemaXML.Namespace
    }
}

#########################################################
# Processes ComplexTypes from plain text xml metadata 
# into our custom structure
#########################################################
function ParseComplexTypes
{
    param
    (
        [System.Xml.XmlElement] $SchemaXML,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        [hashtable] $EntityAndComplexTypesQueue,
        [string] $CustomNamespace,
        [AllowEmptyString()]
        [string] $Alias
    )

    if($SchemaXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaXML", "ParseComplexTypes") }
    
    foreach ($complexType in $SchemaXML.ComplexType)
    {
        $baseType = $null

        if ($complexType.BaseType -ne $null)
        {
            # add it to the processing queue
            $baseType = GetBaseType $complexType $metadata $SchemaXML.Namespace $GlobalMetadata
            if ($baseType -eq $null -and $entityAndComplexTypesQueue -ne $null -and $entityAndComplexTypesQueue.ContainsKey($complexType.BaseType))
            {
                $entityAndComplexTypesQueue[$complexType.BaseType] += @(@{type='ComplexType'; value=$complexType})
                continue
            }
            
            # Check if Namespace has to be normalized because of the collision with the inheriting Class name
            NormalizeNamespaceCollisionWithClassName -InheritingType $complexType.Name -BaseTypeName $complexType.BaseType -MetadataUri $Metadata.Uri
        }

        [ODataUtils.EntityTypeV4] $newType = ParseMetadataTypeDefinition $complexType $baseType $Metadata $schema.Namespace -Alias $Alias $false $complexType.BaseType
        $Metadata.ComplexTypes += $newType
        AddDerivedTypes $newType $entityAndComplexTypesQueue $metadata $schema.Namespace
    }
}

#########################################################
# Processes TypeDefinition from plain text xml metadata 
# into our custom structure
#########################################################
function ParseTypeDefinitions
{
    param
    (
        [System.Xml.XmlElement] $SchemaXML,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        [string] $CustomNamespace,
        [AllowEmptyString()]
        [string] $Alias
    )
    
    if($SchemaXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaXML", "ParseTypeDefinitions") }
    

    foreach ($typeDefinition in $SchemaXML.TypeDefinition)
    {
        $newType = [ODataUtils.EntityTypeV4] @{
            "Namespace" = $Metadata.Namespace;
            "Alias" = $Metadata.Alias;
            "Name" = $typeDefinition.Name;
            "BaseTypeStr" = $typeDefinition.UnderlyingType;
        }
        $Metadata.TypeDefinitions += $newType
    }
}

#########################################################
# Processes EnumTypes from plain text xml metadata 
# into our custom structure
#########################################################
function ParseEnumTypes
{
    param
    (
        [System.Xml.XmlElement] $SchemaXML,
        [ODataUtils.MetadataV4] $Metadata
    )

    if($SchemaXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaXML", "ParseEnumTypes") }
    
    foreach ($enum in $SchemaXML.EnumType)
    {        
        $newEnumType = [ODataUtils.EnumType] @{
            "Namespace" = $Metadata.Namespace;
            "Alias" = $Metadata.Alias;
            "Name" = $enum.Name;
            "UnderlyingType" = $enum.UnderlyingType;
            "IsFlags" = $enum.IsFlags;
            "Members" = @()
        }

        if (!$newEnumType.UnderlyingType)
        {
            # If no type specified set the default type which is Edm.Int32
            $newEnumType.UnderlyingType = "Edm.Int32" 
        }

        if ($newEnumType.IsFlags -eq $null)
        {
            # If no value is specified for IsFlags, its value defaults to false.
            $newEnumType.IsFlags = $false
        }

        $enumValue = 0
        $currentEnumValue = 0

        # Now parse EnumType elements
        foreach ($element in $enum.Member)
        {
                    
            if ($element.Value -eq "" -and $newEnumType.IsFlags -eq $true)
            {
                # When IsFlags set to true each edm:Member element MUST specify a non-negative integer Value in the value attribute
                $errorMessage = ($LocalizedData.InValidMetadata)
                $detailedErrorMessage = "When IsFlags set to true each edm:Member element MUST specify a non-negative integer Value in the value attribute in " + $newEnumType.Name + " EnumType"
                $exception = [System.InvalidOperationException]::new($errorMessage, $detailedErrorMessage)
                $errorRecord = CreateErrorRecordHelper "InValidMetadata" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $detailedErrorMessage nu
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }
            elseif (($element.Value -eq $null) -or ($element.Value.GetType().Name -eq "Int32" -and $element.Value -eq ""))
            {
                # If no values are specified, the members are assigned consecutive integer values in the order of their appearance, 
                # starting with zero for the first member.
                $currentEnumValue = $enumValue
            }
            else
            {
                $currentEnumValue = $element.Value
            }

            $tmp = [ODataUtils.EnumMember] @{
                "Name" = $element.Name;
                "Value" = $currentEnumValue;
            }

            $newEnumType.Members += $tmp
            $enumValue++
        }                
     
        $Metadata.EnumTypes += $newEnumType
    }
}

#########################################################
# Processes SingletonTypes from plain text xml metadata 
# into our custom structure
#########################################################
function ParseSingletonTypes
{
    param
    (
        [System.Xml.XmlElement] $SchemaEntityContainerXML,
        [ODataUtils.MetadataV4] $Metadata
    )

    if($SchemaEntityContainerXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaEntityContainerXML", "ParseSingletonTypes") }
    
    foreach ($singleton in $SchemaEntityContainerXML.Singleton)
    {
        $navigationPropertyBindings = @()

        foreach ($navigationPropertyBinding in $singleton.NavigationPropertyBinding)
        {            
            $tmp = [ODataUtils.NavigationPropertyBinding] @{
                "Path" = $navigationPropertyBinding.Path;
                "Target" = $navigationPropertyBinding.Target;
            }

            $navigationPropertyBindings += $tmp
        }

        $newSingletonType = [ODataUtils.SingletonType] @{
            "Namespace" = $Metadata.Namespace;
            "Alias" = $Metadata.Alias;
            "Name" = $singleton.Name;
            "Type" = $singleton.Type;
            "NavigationPropertyBindings" = $navigationPropertyBindings;
        }

        $Metadata.SingletonTypes += $newSingletonType
    }
}

#########################################################
# Processes EntitySets from plain text xml metadata 
# into our custom structure
#########################################################
function ParseEntitySets
{
    param
    (
        [System.Xml.XmlElement] $SchemaEntityContainerXML,
        [ODataUtils.MetadataV4] $Metadata,
        [string] $Namespace,
        [AllowEmptyString()]
        [string] $Alias
    )
    
    if($SchemaEntityContainerXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaEntityContainerXML", "ParseEntitySets") }

    $entityTypeToEntitySetMapping = @{};
    foreach ($entitySet in $SchemaEntityContainerXML.EntitySet)
    {
        $entityType = $metadata.EntityTypes | Where-Object { $_.Name -eq $entitySet.EntityType.Split('.')[-1] }
        $entityTypeName = $entityType.Name

        if($entityTypeToEntitySetMapping.ContainsKey($entityTypeName))
        {
            $existingEntitySetName = $entityTypeToEntitySetMapping[$entityTypeName]
            throw ($LocalizedData.EntityNameConflictError -f $entityTypeName, $existingEntitySetName, $entitySet.Name, $entityTypeName )
        }
        else
        {
            $entityTypeToEntitySetMapping.Add($entityTypeName, $entitySet.Name)
        }

        $newEntitySet = [ODataUtils.EntitySetV4] @{
            "Namespace" = $Namespace;
            "Alias" = $Alias;
            "Name" = $entitySet.Name;
            "Type" = $entityType;
        }
        
        $Metadata.EntitySets += $newEntitySet
    }
}

#########################################################
# Processes Actions from plain text xml metadata 
# into our custom structure
#########################################################
function ParseActions
{
    param
    (
        [System.Object[]] $SchemaActionsXML,
        [ODataUtils.MetadataV4] $Metadata
    )

    if($SchemaActionsXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaActionsXML", "ParseActions") }
    
    foreach ($action in $SchemaActionsXML)
    {
        # HttpMethod is only used for legacy Service Operations
        if ($action.HttpMethod -eq $null)
        {
            $newAction = [ODataUtils.ActionV4] @{
                "Namespace" = $Metadata.Namespace;
                "Alias" = $Metadata.Alias;
                "Name" = $action.Name;
                "Action" = $Metadata.Namespace + '.' + $action.Name;
            }
                
            # Actions are always SideEffecting, otherwise it's an OData function
            foreach ($parameter in $action.Parameter)
            {
                if ($parameter.Nullable -ne $null)
                {
                    $parameterIsNullable = [System.Convert]::ToBoolean($parameter.Nullable);
                }
                else
                {
                    $parameterIsNullable = $true
                }

                $newParameter = [ODataUtils.TypeProperty] @{
                    "Name" = $parameter.Name;
                    "TypeName" = $parameter.Type;
                    "IsNullable" = $parameterIsNullable;
                }

                $newAction.Parameters += $newParameter
            }

            if ($action.EntitySet -ne $null)
            {
                $newAction.EntitySet = $metadata.EntitySets | Where-Object { $_.Name -eq $action.EntitySet }
            }

            $Metadata.Actions += $newAction
        }
    }
}

#########################################################
# Processes Functions from plain text xml metadata 
# into our custom structure
#########################################################
function ParseFunctions
{
    param
    (
        [System.Object[]] $SchemaFunctionsXML,
        [ODataUtils.MetadataV4] $Metadata
    )

    if($SchemaFunctionsXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "SchemaFunctionsXML", "ParseFunctions") }
    
    foreach ($function in $SchemaFunctionsXML)
    {
        # HttpMethod is only used for legacy Service Operations
        if ($function.HttpMethod -eq $null)
        {
            $newFunction = [ODataUtils.FunctionV4] @{
                "Namespace" = $Metadata.Namespace;
                "Alias" = $Metadata.Alias;
                "Name" = $function.Name;
                "Function" = $Metadata.Namespace + '.' + $function.Name;
                "EntitySet" = $function.EntitySetPath;
                "ReturnType" = $function.ReturnType;
            }

            # Future TODO - consider removing this hack once all the service we run against fix this issue
            # Hack - sometimes service does not return ReturnType, however this information can be found in InnerXml
            if ($newFunction.ReturnType -eq '' -or $newFunction.ReturnType -eq 'System.Xml.XmlElement')
            {
                try
                {
                    [xml] $innerXML = '<Params>' + $function.InnerXml + '</Params>'
                    $newFunction.Returntype = $innerXML.Params.ReturnType.Type
                }
                catch
                {
                    # Do nothing
                }
            }

            # Keep only EntityType name
            $newFunction.ReturnType = $newFunction.ReturnType.Replace('Collection(', '')
            $newFunction.ReturnType = $newFunction.ReturnType.Replace(')', '')

            # Actions are always SideEffecting, otherwise it's an OData function
            foreach ($parameter in $function.Parameter)
            {
                if ($parameter.Nullable -ne $null)
                {
                    $parameterIsNullable = [System.Convert]::ToBoolean($parameter.Nullable);
                }

                $newParameter = [ODataUtils.Parameter] @{
                    "Name" = $parameter.Name;
                    "Type" = $parameter.Type;
                    "Nullable" = $parameterIsNullable;
                }

                $newFunction.Parameters += $newParameter
            }

            $Metadata.Functions += $newFunction
        }
    }
}

#########################################################
# Processes plain text xml metadata (OData V4 schema version) into our custom structure
# MetadataSet contains all parsed so far referenced Metadatas (for base class lookup)
#########################################################
function ParseMetadata 
{
    param
    (
        [xml] $MetadataXML,
        [string] $ODataVersion,
        [string] $MetadataUri,
        [string] $Uri,
        [System.Collections.ArrayList] $MetadataSet
    )

    if($MetadataXML -eq $null) { throw ($LocalizedData.ArguementNullError -f "MetadataXML", "ParseMetadata") }

    # This is a processing queue for those types that require base types that haven't been defined yet
    $entityAndComplexTypesQueue = @{}
    [System.Collections.ArrayList] $metadatas = [System.Collections.ArrayList]::new()

    foreach ($schema in $MetadataXML.Edmx.DataServices.Schema)
    {
        if ($schema -eq $null)
        {
            Write-Error $LocalizedData.EmptySchema
            continue
        }

        [ODataUtils.MetadataV4] $metadata = [ODataUtils.MetadataV4]::new()
        $metadata.ODataVersion = $ODataVersion
        $metadata.MetadataUri = $MetadataUri
        $metadata.Uri = $Uri
        $metadata.Namespace = $schema.Namespace
        $metadata.Alias = $schema.Alias

        ParseEntityTypes -SchemaXML $schema -metadata $metadata -GlobalMetadata $MetadataSet -EntityAndComplexTypesQueue $entityAndComplexTypesQueue -CustomNamespace $CustomNamespace -Alias $metadata.Alias
        ParseComplexTypes -SchemaXML $schema -metadata $metadata -GlobalMetadata $MetadataSet -EntityAndComplexTypesQueue $entityAndComplexTypesQueue -CustomNamespace $CustomNamespace -Alias $metadata.Alias
        ParseTypeDefinitions -SchemaXML $schema -metadata $metadata -GlobalMetadata $MetadataSet -CustomNamespace $CustomNamespace -Alias $metadata.Alias
        ParseEnumTypes -SchemaXML $schema -metadata $metadata

        foreach ($entityContainer in $schema.EntityContainer)
        {
            if ($entityContainer.IsDefaultEntityContainer)
            {
                $metadata.DefaultEntityContainerName = $entityContainer.Name
            }

            ParseSingletonTypes -SchemaEntityContainerXML $entityContainer -Metadata $metadata
            ParseEntitySets -SchemaEntityContainerXML $entityContainer -Metadata $metadata -Namespace $schema.Namespace -Alias $schema.Alias
        }

        if ($schema.Action)
        {
            ParseActions -SchemaActionsXML $schema.Action -Metadata $metadata
        }

        if ($schema.Function)
        {
            ParseFunctions -SchemaFunctionsXML $schema.Function -Metadata $metadata
        }

        # In this call we check if the Namespace or Alias have to be normalized because it has invalid combination of dots and numbers.
        # Note: In ParseEntityTypes and ParseComplexTypes we check for scenario where namespace/alias collide with inheriting class name.
        NormalizeNamespace $metadata.Namespace $metadata.Uri $script:normalizedNamespaces $false
        NormalizeNamespace $metadata.Alias $metadata.Uri $script:normalizedNamespaces $false

        $metadatas.Add($metadata) | Out-Null
    }

    $metadatas
}

#########################################################
# Verifies processed metadata for correctness
#########################################################
function VerifyMetadata 
{
    param
    (
        [System.Collections.ArrayList] $metadataSet,
        [boolean] $allowClobber,
        $callerPSCmdlet,
        [string] $progressBarStatus
    )

    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "VerifyMetaData") }
    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "VerifyMetaData") }

    Write-Verbose $LocalizedData.VerboseVerifyingMetadata

    $reservedProperties = @("Filter", "OrderBy", "Skip", "Top", "ConnectionUri", "CertificateThumbPrint", "Credential")
    $validEntitySets = @()
    $sessionCommands = Get-Command -All
    

    foreach ($metadata in $metadataSet)
    {
        foreach ($entitySet in $metadata.EntitySets)
        {
            if ($entitySet.Type -eq $null)
            {
                $errorMessage = ($LocalizedData.EntitySetUndefinedType -f $metadata.MetadataUri, $entitySet.Name)
                $exception = [System.InvalidOperationException]::new($errorMessage)
                $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetaDataUri" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metadata.MetadataUri
                $callerPSCmdlet.ThrowTerminatingError($errorRecord)
            }

            $hasConflictingProperty = $false
            $hasConflictingCommand = $false
            $entityAndNavigationProperties = $entitySet.Type.EntityProperties + $entitySet.Type.NavigationProperties
            foreach($entityProperty in $entityAndNavigationProperties)
            {
                if($reservedProperties.Contains($entityProperty.Name))
                {
                    $hasConflictingProperty = $true
                    if(!$allowClobber)
                    {
                        # Write Error message and skip current Entity Set.
                        $errorMessage = ($LocalizedData.SkipEntitySetProxyCreation -f $entitySet.Name, $entitySet.Type.Name, $entityProperty.Name)
                        $exception = [System.InvalidOperationException]::new($errorMessage)
                        $errorRecord = CreateErrorRecordHelper "ODataEndpointDefaultPropertyCollision" $null ([System.Management.Automation.ErrorCategory]::InvalidOperation) $exception $metadata.MetadataUri
                        $callerPSCmdlet.WriteError($errorRecord)
                    }
                    else
                    {                    
                        $warningMessage = ($LocalizedData.EntitySetProxyCreationWithWarning -f $entitySet.Name, $entityProperty.Name, $entitySet.Type.Name)
                        $callerPSCmdlet.WriteWarning($warningMessage)
                    }
                }
            }

            foreach($currentCommand in $sessionCommands)
            {
                if(($null -ne $currentCommand.Noun -and $currentCommand.Noun -eq $entitySet.Name) -and 
                ($currentCommand.Verb -eq "Get" -or 
                $currentCommand.Verb -eq "Set" -or 
                $currentCommand.Verb -eq "New" -or 
                $currentCommand.Verb -eq "Remove"))
                {
                    $hasConflictingCommand = $true
                    VerifyMetadataHelper $LocalizedData.SkipEntitySetConflictCommandCreation `
                    $LocalizedData.EntitySetConflictCommandCreationWithWarning `
                    $entitySet.Name $currentCommand.Name $metadata.MetadataUri $allowClobber $callerPSCmdlet
                }
            }

            foreach($currentAction in $metadata.Actions)
            {
                $actionCommand = "Invoke-" + "$($entitySet.Name)$($currentAction.Verb)"
        
                foreach($currentCommand in $sessionCommands)
                {
                    if($actionCommand -eq $currentCommand.Name)
                    {
                        $hasConflictingCommand = $true
                        VerifyMetadataHelper $LocalizedData.SkipEntitySetConflictCommandCreation `
                        $LocalizedData.EntitySetConflictCommandCreationWithWarning $entitySet.Name `
                        $currentCommand.Name $metadata.MetadataUri $allowClobber $callerPSCmdlet
                    }
                }
            }

            if(!($hasConflictingProperty -or $hasConflictingCommand)-or $allowClobber)
            {
                $validEntitySets += $entitySet
            }
        }

        $metadata.EntitySets = $validEntitySets

        $validServiceActions = @()
        $hasConflictingServiceActionCommand
        foreach($currentAction in $metadata.Actions)
        {
            $serviceActionCommand = "Invoke-" + "$($currentAction.Verb)"

            foreach($currentCommand in $sessionCommands)
            {
                if($serviceActionCommand -eq $currentCommand.Name)
                {
                    $hasConflictingServiceActionCommand = $true
                    VerifyMetadataHelper $LocalizedData.SkipConflictServiceActionCommandCreation `
                    $LocalizedData.ConflictServiceActionCommandCreationWithWarning $entitySet.Name `
                    $currentCommand.Name $metadata.MetadataUri $allowClobber $callerPSCmdlet
                }
            }

            if(!$hasConflictingServiceActionCommand -or $allowClobber)
            {
                $validServiceActions += $currentAction
            }
        }

        $metadata.Actions = $validServiceActions
    }

    # Update Progress bar.
    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 5 20 1  1
}

#########################################################
# Takes xml definition of a class from metadata document, 
# plus existing metadata structure and finds its base class
#########################################################
function GetBaseType 
{
    param
    (
        [System.Xml.XmlElement] $MetadataEntityDefinition,
        [ODataUtils.MetadataV4] $Metadata,
        [string] $Namespace,
        [System.Collections.ArrayList] $GlobalMetadata
    )

    if ($metadataEntityDefinition -ne $null -and 
        $metaData -ne $null -and 
        $MetadataEntityDefinition.BaseType -ne $null)
    {
        $baseType = $Metadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $MetadataEntityDefinition.BaseType -or $_.Alias + "." + $_.Name -eq $MetadataEntityDefinition.BaseType }
        if ($baseType -eq $null)
        {
            $baseType = $Metadata.ComplexTypes | Where { $_.Namespace + "." + $_.Name -eq $MetadataEntityDefinition.BaseType -or $_.Alias + "." + $_.Name -eq $MetadataEntityDefinition.BaseType }
        }

        if ($baseType -eq $null)
        {
            # Look in other metadatas, since the class can be defined in referenced metadata
            foreach ($referencedMetadata in $GlobalMetadata)
            {
                if (($baseType = $referencedMetadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $MetadataEntityDefinition.BaseType -or $_.Alias + "." + $_.Name -eq $MetadataEntityDefinition.BaseType }) -ne $null -or
                    ($baseType = $referencedMetadata.ComplexTypes | Where { $_.Namespace + "." + $_.Name -eq $MetadataEntityDefinition.BaseType -or $_.Alias + "." + $_.Name -eq $MetadataEntityDefinition.BaseType }) -ne $null)
                {
                    # Found base class
                    break
                }
            }
        }
    }

    if ($baseType -ne $null)
    {
        $baseType[0]
    }
}

#########################################################
# Takes base class name and global metadata structure 
# and finds its base class
#########################################################
function GetBaseTypeByName 
{
    param
    (
        [String] $BaseTypeStr,
        [System.Collections.ArrayList] $GlobalMetadata
    )

    if ($BaseTypeStr -ne $null)
    {
        
        # Look for base class definition in all referenced metadatas (including entry point)
        foreach ($referencedMetadata in $GlobalMetadata)
        {
            if (($baseType = $referencedMetadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $BaseTypeStr -or $_.Alias + "." + $_.Name -eq $BaseTypeStr }) -ne $null -or
                ($baseType = $referencedMetadata.ComplexTypes | Where { $_.Namespace + "." + $_.Name -eq $BaseTypeStr -or $_.Alias + "." + $_.Name -eq $BaseTypeStr }) -ne $null)
            {
                # Found base class
                break
            }
        }
    }

    if ($baseType -ne $null)
    {
        $baseType[0]
    }
    else
    { 
        $null
    }
}

#########################################################
# Processes derived types of a newly added type, 
# that were previously waiting in the queue
#########################################################
function AddDerivedTypes {
    param(
    [ODataUtils.EntityTypeV4] $baseType,
    $entityAndComplexTypesQueue,
    [ODataUtils.MetadataV4] $metadata,
    [string] $namespace
    )

    if($baseType -eq $null) { throw ($LocalizedData.ArguementNullError -f "BaseType", "AddDerivedTypes") }
    if($entityAndComplexTypesQueue -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntityAndComplexTypesQueue", "AddDerivedTypes") }
    if($namespace -eq $null) { throw ($LocalizedData.ArguementNullError -f "Namespace", "AddDerivedTypes") }

    $baseTypeFullName = $baseType.Namespace + '.' + $baseType.Name
    $baseTypeShortName = $baseType.Alias + '.' + $baseType.Name

    if ($entityAndComplexTypesQueue.ContainsKey($baseTypeFullName) -or $entityAndComplexTypesQueue.ContainsKey($baseTypeShortName))
    {
        $types = $entityAndComplexTypesQueue[$baseTypeFullName] + $entityAndComplexTypesQueue[$baseTypeShortName]
        
        foreach ($type in $types)
        {
            if ($type.type -eq 'EntityType')
            {
                $newType = ParseMetadataTypeDefinition ($type.value) $baseType $metadata $namespace $true
                $metadata.EntityTypes += $newType
            }
            else
            {
                $newType = ParseMetadataTypeDefinition ($type.value) $baseType $metadata $namespace $false
                $metadata.ComplexTypes += $newType
            }

            AddDerivedTypes $newType $entityAndComplexTypesQueue $metadata $namespace
        }
    }
}

#########################################################
# Parses types definitions element of metadata xml
#########################################################
function ParseMetadataTypeDefinitionHelper 
{
    param
    (
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.EntityTypeV4] $baseType,
        [string] $baseTypeStr,
        [ODataUtils.MetadataV4] $metadata,
        [string] $namespace,
        [AllowEmptyString()]
        [string] $alias,
        [bool] $isEntity
    )
    
    if($metadataEntityDefinition -eq $null) { throw ($LocalizedData.ArguementNullError -f "MetadataEntityDefinition", "ParseMetadataTypeDefinition") }
    if($namespace -eq $null) { throw ($LocalizedData.ArguementNullError -f "Namespace", "ParseMetadataTypeDefinition") }

    [ODataUtils.EntityTypeV4] $newEntityType = CreateNewEntityType -metadataEntityDefinition $metadataEntityDefinition -baseType $baseType -baseTypeStr $baseTypeStr -namespace $namespace -alias $alias -isEntity $isEntity

    if ($baseType -ne $null)
    {
        # Add properties inherited from BaseType
        ParseMetadataBaseTypeDefinitionHelper $newEntityType $baseType
    }

    # properties defined on EntityType
    $newEntityType.EntityProperties += $metadataEntityDefinition.Property | % {
        if ($_ -ne $null)
        {
            if ($_.Nullable -ne $null)
            {
                $newPropertyIsNullable = [System.Convert]::ToBoolean($_.Nullable)
            }
            else
            {
                $newPropertyIsNullable = $true
            }

            [ODataUtils.TypeProperty] @{
                "Name" = $_.Name;
                "TypeName" = $_.Type;
                "IsNullable" = $newPropertyIsNullable;
            }
        }
    }

    # odataId property will be inherited from base type, if it exists.
    # Otherwise, it should be added to current type 
    if ($baseType -eq $null)
    {
        # @odata.Id property (renamed to odataId) is required for dynamic Uri creation
        # This property is only available when user executes auto-generated cmdlet with -AllowAdditionalData, 
        # but ODataAdapter needs it to construct Uri to access navigation properties. 
        # Thus, we need to fetch this info for scenario when -AllowAdditionalData isn't used.
        $newEntityType.EntityProperties += [ODataUtils.TypeProperty] @{
                "Name" = "odataId";
                "TypeName" = "Edm.String";
                "IsNullable" = $True;
            }
    }

    # Property name can't be identical to entity type name. 
    # If such property exists, "Property" suffix will be added to its name. 
    foreach ($property in $newEntityType.EntityProperties)
    {
        if ($property.Name -eq $newEntityType.Name)
        {
            $property.Name += "Property"
        }
    }

    if ($metadataEntityDefinition -ne $null -and $metadataEntityDefinition.Key -ne $null)
    {
        foreach ($entityTypeKey in $metadataEntityDefinition.Key.PropertyRef)
        {
            ($newEntityType.EntityProperties | Where-Object { $_.Name -eq $entityTypeKey.Name }).IsKey = $true
        }
    }

    $newEntityType
}

#########################################################
# Add base class entity and navigation properties to inheriting class
#########################################################
function ParseMetadataBaseTypeDefinitionHelper
{
    param
    (
        [ODataUtils.EntityTypeV4] $EntityType,
        [ODataUtils.EntityTypeV4] $BaseType
    )

    if ($EntityType -ne $null -and $BaseType -ne $null)
    {
        # Add properties inherited from BaseType
        $EntityType.EntityProperties += $BaseType.EntityProperties
        $EntityType.NavigationProperties += $BaseType.NavigationProperties
    }
}

#########################################################
# Create new EntityType object
#########################################################
function CreateNewEntityType
{
    param
    (
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.EntityTypeV4] $baseType,
        [string] $baseTypeStr,
        [string] $namespace,
        [AllowEmptyString()]
        [string] $alias,
        [bool] $isEntity
    )
    $newEntityType = [ODataUtils.EntityTypeV4] @{
        "Namespace" = $namespace;
        "Alias" = $alias;
        "Name" = $metadataEntityDefinition.Name;
        "IsEntity" = $isEntity;
        "BaseType" = $baseType;
        "BaseTypeStr" = $baseTypeStr;
    }

    $newEntityType
}

#########################################################
# Parses navigation properties from metadata xml
#########################################################
function ParseMetadataTypeDefinitionNavigationProperties
{
    param
    (
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.EntityTypeV4] $entityType
    )

    # navigation properties defined on EntityType
    $newEntityType.NavigationProperties = @{}
    $newEntityType.NavigationProperties.Clear()

    foreach ($navigationProperty in $metadataEntityDefinition.NavigationProperty)
    {
        $tmp = [ODataUtils.NavigationPropertyV4] @{
                "Name" = $navigationProperty.Name;
                "Type" = $navigationProperty.Type;
                "Nullable" = $navigationProperty.Nullable;
                "Partner" = $navigationProperty.Partner;
                "ContainsTarget" = $navigationProperty.ContainsTarget;
                "OnDelete" = $navigationProperty.OnDelete;
            }

        $referentialConstraints = @{}
        foreach ($constraint in $navigationProperty.ReferentialConstraints)
        {
            $tmp = [ODataUtils.ReferencedConstraint] @{
                "Property" = $constraint.Property;
                "ReferencedProperty" = $constraint.ReferencedProperty;
            }
        }

        $newEntityType.NavigationProperties += $tmp
    }
}

#########################################################
# Parses types definitions element of metadata xml for OData V4 schema
#########################################################
function ParseMetadataTypeDefinition 
{
    param
    (
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.EntityTypeV4] $baseType,
        [ODataUtils.MetadataV4] $metadata,
        [string] $namespace,
        [AllowEmptyString()]
        [string] $alias,
        [bool] $isEntity,
        [string] $baseTypeStr
    )

    if($metadataEntityDefinition -eq $null) { throw ($LocalizedData.ArguementNullError -f "MetadataEntityDefinition", "ParseMetadataTypeDefinition") }
    if($namespace -eq $null) { throw ($LocalizedData.ArguementNullError -f "Namespace", "ParseMetadataTypeDefinition") }

    [ODataUtils.EntityTypeV4] $newEntityType = ParseMetadataTypeDefinitionHelper -metadataEntityDefinition $metadataEntityDefinition -baseType $baseType -baseTypeStr $baseTypeStr -metadata $metadata -namespace $namespace -alias $alias -isEntity $isEntity
    ParseMetadataTypeDefinitionNavigationProperties -metadataEntityDefinition $metadataEntityDefinition -entityType $newEntityType

    $newEntityType
}

#########################################################
# Create psd1 and cdxml files required to auto-generate 
# cmdlets for given service.
#########################################################
function GenerateClientSideProxyModule 
{
    param
    (
        [System.Collections.ArrayList] $GlobalMetadata,
        [ODataUtils.ODataEndpointProxyParameters] $ODataEndpointProxyParameters,
        [string] $OutputModule,
        [string] $CreateRequestMethod,
        [string] $UpdateRequestMethod,
        [string] $CmdletAdapter,
        [Hashtable] $resourceNameMappings,
        [Hashtable] $CustomData,
        [string] $UriResourcePathKeyFormat,
        [string] $progressBarStatus,
        $NormalizedNamespaces
    )

    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "GenerateClientSideProxyModule") }

    Write-Verbose ($LocalizedData.VerboseSavingModule -f $OutputModule)
    
    # Save ComplexTypes for all metadata schemas in single file
    $typeDefinitionFileName = "ComplexTypeDefinitions.psm1"
    $complexTypeMapping = GenerateComplexTypeDefinition $GlobalMetadata $OutputModule $typeDefinitionFileName $NormalizedNamespaces

    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 20 20 1  1

    $actions = @()
    $functions = @()
    
    $currentEntryCount = 0
    foreach ($Metadata in $GlobalMetadata)
    {
        foreach ($entitySet in $Metadata.EntitySets)
        {
            $currentEntryCount += 1
            SaveCDXML $entitySet $Metadata $GlobalMetadata $Metadata.Uri $OutputModule $CreateRequestMethod $UpdateRequestMethod $CmdletAdapter $resourceNameMappings $CustomData $complexTypeMapping $UriResourcePathKeyFormat $NormalizedNamespaces

            ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 40 20 $Metadata.EntitySets.Count $currentEntryCount
        }

        $currentEntryCount = 0
        foreach ($singleton in $Metadata.SingletonTypes)
        {
            $currentEntryCount += 1
            SaveCDXMLSingletonCmdlets $singleton $Metadata $GlobalMetadata $Metadata.Uri $OutputModule $CreateRequestMethod $UpdateRequestMethod $CmdletAdapter $resourceNameMappings $CustomData $complexTypeMapping $NormalizedNamespaces

            ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 40 20 $Metadata.Singletons.Count $currentEntryCount
        }

        $actions += $Metadata.Actions | Where-Object { $_.EntitySet -eq '' -or $_.EntitySet -eq $null }
        $functions += $Metadata.Functions | Where-Object { $_.EntitySet -eq '' -or $_.EntitySet -eq $null }
    }

    if ($actions.Count -gt 0 -or $functions.Count -gt 0)
    {
        # Save Service Actions for all metadata schemas in single file
        SaveServiceActionsCDXML $GlobalMetadata $ODataEndpointProxyParameters "$OutputModule\ServiceActions.cdxml" $complexTypeMapping $progressBarStatus $CmdletAdapter
    }

    $moduleDirInfo = [System.IO.DirectoryInfo]::new($OutputModule)
    $moduleManifestName = $moduleDirInfo.Name + ".psd1"

    if ($actions.Count -gt 0 -or $functions.Count -gt 0)
    {
        $additionalModules = @($typeDefinitionFileName, 'ServiceActions.cdxml')
    }
    else
    {
        $additionalModules = @($typeDefinitionFileName)
    }

    GenerateModuleManifest $GlobalMetadata $OutputModule\$moduleManifestName $additionalModules $resourceNameMappings $progressBarStatus
}

#########################################################
# Generates CDXML module for a specific OData EntitySet
#########################################################
function SaveCDXML 
{
    param
    (
        [ODataUtils.EntitySetV4] $EntitySet,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        [string] $Uri,
        [string] $OutputModule,
        [string] $CreateRequestMethod,
        [string] $UpdateRequestMethod,
        [string] $CmdletAdapter,
        [Hashtable] $resourceNameMappings,
        [Hashtable] $CustomData,
        [Hashtable] $complexTypeMapping,
        [string] $UriResourcePathKeyFormat,
        $normalizedNamespaces
    )

    if($EntitySet -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntitySet", "GenerateClientSideProxyModule") }
    if($Metadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateClientSideProxyModule") }

    $entitySetName = $EntitySet.Name 
    if(($null -ne $resourceNameMappings) -and 
    $resourceNameMappings.ContainsKey($entitySetName))
    {
        $entitySetName = $resourceNameMappings[$entitySetName]
    }
    else
    {
        $entitySetName = $EntitySet.Type.Name
    }

    $Path = "$OutputModule\$entitySetName.cdxml"

    $xmlWriter = New-Object System.XMl.XmlTextWriter($Path,$Null)

    if ($xmlWriter -eq $null)
    {
        throw ($LocalizedData.XmlWriterInitializationError -f $EntitySet.Name)
    }

    $xmlWriter = SaveCDXMLHeader $xmlWriter $Uri $EntitySet.Name $entitySetName $CmdletAdapter

    # Get the keys 
    $keys = $EntitySet.Type.EntityProperties | Where-Object { $_.IsKey }
    
    $navigationProperties = $EntitySet.Type.NavigationProperties

    SaveCDXMLInstanceCmdlets $xmlWriter $Metadata $GlobalMetadata $EntitySet.Type $keys $navigationProperties $CmdletAdapter $complexTypeMapping $false

    $nonKeyProperties = $EntitySet.Type.EntityProperties | ? { -not $_.isKey }
    $nullableProperties = $nonKeyProperties | ? { $_.isNullable }
    $nonNullableProperties = $nonKeyProperties | ? { -not $_.isNullable }

    $xmlWriter.WriteStartElement('StaticCmdlets')

        $keyProperties = $keys

        SaveCDXMLNewCmdlet $xmlWriter $Metadata $GlobalMetadata $keyProperties $nonNullableProperties $nullableProperties $navigationProperties $CmdletAdapter $complexTypeMapping
        
        GenerateSetProxyCmdlet $xmlWriter $keyProperties $nonKeyProperties $complexTypeMapping

        SaveCDXMLRemoveCmdlet $xmlWriter $Metadata $GlobalMetadata $keyProperties $navigationProperties $CmdletAdapter $complexTypeMapping

        $entityActions = $Metadata.Actions | Where-Object { ($_.EntitySet.Namespace -eq $EntitySet.Namespace) -and ($_.EntitySet.Name -eq $EntitySet.Name) }

        if ($entityActions.Length -gt 0)
        {
            foreach($action in $entityActions)
            {
                $xmlWriter = SaveCDXMLAction $xmlWriter $Metadata $action $EntitySet.Name $true $keys $complexTypeMapping
            }
        }

        $entityFunctions = $Metadata.Functions | Where-Object { ($_.EntitySet.Namespace -eq $EntitySet.Namespace) -and ($_.EntitySet.Name -eq $EntitySet.Name) }

        if ($entityFunctions.Length -gt 0)
        {
            foreach($function in $entityFunctions)
            {
                $xmlWriter = SaveCDXMLFunction $xmlWriter $Metadata $function $EntitySet.Name $true $keys $complexTypeMapping
            }
        }

    $xmlWriter.WriteEndElement()
    
    $normalizedDotNetNamespace = GetNamespace $EntitySet.Type.Namespace $normalizedNamespaces
    $normalizedDotNetAlias = GetNamespace $EntitySet.Alias $normalizedNamespaces
    $normalizedDotNetEntitySetNamespace = $normalizedDotNetNamespace

    $xmlWriter.WriteStartElement('CmdletAdapterPrivateData')

        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'EntityTypeName')
        $xmlWriter.WriteString("$($normalizedDotNetNamespace).$($EntitySet.Type.Name)")
        $xmlWriter.WriteEndElement()
        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'EntityTypeAliasName')
        if (!$EntitySet.Alias)
        {
            $xmlWriter.WriteString("")
        }
        else
        {
            $xmlWriter.WriteString("$($normalizedDotNetAlias).$($EntitySet.Type.Name)")
        }
        $xmlWriter.WriteEndElement()
        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'EntitySetName')
        $xmlWriter.WriteString("$($normalizedDotNetEntitySetNamespace).$($EntitySet.Name)")
        $xmlWriter.WriteEndElement()
        
        # Add URI resource path format (webservice.svc/ResourceName/ResourceId vs webservice.svc/ResourceName(QueryKeyName=ResourceId))
        if  ($UriResourcePathKeyFormat -ne $null -and $UriResourcePathKeyFormat -ne '')
        {
            $xmlWriter.WriteStartElement('Data')
            $xmlWriter.WriteAttributeString('Name', 'UriResourcePathKeyFormat')
            $xmlWriter.WriteString("$UriResourcePathKeyFormat")
            $xmlWriter.WriteEndElement()
        }

        # Add information about navigation properties and their types 
        # Used in scenario where user requests navigation property in -Select query
        foreach ($navProperty in $navigationProperties)
        {
            if ($navProperty)
            {
                $xmlWriter.WriteStartElement('Data')
                $xmlWriter.WriteAttributeString('Name', $navProperty.Name + 'NavigationProperty')
                $xmlWriter.WriteString($navProperty.Type)
                $xmlWriter.WriteEndElement()
            }
        }
                
        # Add CreateRequestMethod and UpdateRequestMethod to privateData
        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'CreateRequestMethod')
        $xmlWriter.WriteString("$CreateRequestMethod")
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'UpdateRequestMethod')
        $xmlWriter.WriteString("$UpdateRequestMethod")
        $xmlWriter.WriteEndElement()

    $xmlWriter.WriteEndElement()

    SaveCDXMLFooter $xmlWriter

    Write-Verbose ($LocalizedData.VerboseSavedCDXML -f $($entitySetName), $Path)
}

#########################################################
# Save Singleton Cmdlets to CDXML
#########################################################
function SaveCDXMLSingletonCmdlets 
{
    param
    (
        [ODataUtils.SingletonType] $Singleton,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        [string] $Uri,
        [string] $OutputModule,
        [string] $CreateRequestMethod,
        [string] $UpdateRequestMethod,
        [string] $CmdletAdapter,
        [Hashtable] $resourceNameMappings,
        [Hashtable] $CustomData,
        [Hashtable] $complexTypeMapping,
        $normalizedNamespaces
    )

    if($Singleton -eq $null) { throw ($LocalizedData.ArguementNullError -f "Singleton", "SaveCDXMLSingletonCmdlets") }
    if($Metadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "Metadata", "SaveCDXMLSingletonCmdlets") }

    $singletonName = $singleton.Name
    $singletonType = $singleton.Type

    $Path = "$OutputModule\$singletonName" + "Singleton" + ".cdxml"

    $xmlWriter = New-Object System.XMl.XmlTextWriter($Path,$Null)

    if ($xmlWriter -eq $null)
    {
        throw ($LocalizedData.XmlWriterInitializationError -f $singletonName)
    }

	# Get associated EntityType
	$associatedEntityType = $Metadata.EntityTypes | Where-Object { $_.Namespace + "." + $_.Name -eq $singletonType -or $_.Alias + "." + $_.Name -eq $singletonType}
	
    if ($associatedEntityType -eq $null)
    {
        # Look in other metadatas, since the class can be defined in referenced metadata
        foreach ($referencedMetadata in $GlobalMetadata)
        {
            if (($associatedEntityType = $referencedMetadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $singletonType -or $_.Alias + "." + $_.Name -eq $singletonType }) -ne $null)
            {
                # Found associated class
                break
            }
        }
    }

    if ($associatedEntityType -ne $null)
	{
		$xmlWriter = SaveCDXMLHeader $xmlWriter $Uri $singletonName $singletonName $CmdletAdapter

        if ($associatedEntityType.BaseType -eq $null -and $associatedEntityType.BaseTypeStr -ne $null -and $associatedEntityType.BaseTypeStr -ne '')
        {
            $associatedEntitybaseType = GetBaseTypeByName $associatedEntityType.BaseTypeStr $GlobalMetadata

            # Make another pass on base class to make sure its properties were added to associated entity type
            ParseMetadataBaseTypeDefinitionHelper $associatedEntityType $associatedEntitybaseType
        }

		# Get the keys depending on whether the url contains variables or not
		$keys = $associatedEntityType.EntityProperties | Where-Object { $_.IsKey }

		$navigationProperties = $associatedEntityType.NavigationProperties

		SaveCDXMLInstanceCmdlets $xmlWriter $Metadata $GlobalMetadata $associatedEntityType $keys $navigationProperties $CmdletAdapter $complexTypeMapping $true 

		$nonKeyProperties = $associatedEntityType.EntityProperties | ? { -not $_.isKey }
		$nullableProperties = $nonKeyProperties | ? { $_.isNullable }
		$nonNullableProperties = $nonKeyProperties | ? { -not $_.isNullable }

		$xmlWriter.WriteStartElement('StaticCmdlets')

			$keyProperties = $keys

			GenerateSetProxyCmdlet $xmlWriter $keyProperties $nonKeyProperties $complexTypeMapping

			$entityActions = $Metadata.Actions | Where-Object { $_.EntitySet.Name -eq $associatedEntityType.Name }

			if ($entityActions.Length -gt 0)
			{
				foreach($action in $entityActions)
				{
					$xmlWriter = SaveCDXMLAction $xmlWriter $Metadata $action $EntitySet.Name $true $keys $complexTypeMapping
				}
			}
			
			$entityFunctions = $Metadata.Functions | Where-Object { $_.EntitySet.Name -eq $associatedEntityType.Name }

			if ($entityFunctions.Length -gt 0)
			{
				foreach($function in $entityFunctions)
				{
					$xmlWriter = SaveCDXMLFunction $xmlWriter $Metadata $function $associatedEntityType.Name $true $keys $complexTypeMapping
				}
			}

		$xmlWriter.WriteEndElement()

        $normalizedDotNetNamespace = GetNamespace $associatedEntityType.Namespace $normalizedNamespaces
        $normalizedDotNetAlias = GetNamespace $associatedEntityType.Alias $normalizedNamespaces

		$xmlWriter.WriteStartElement('CmdletAdapterPrivateData')

			$xmlWriter.WriteStartElement('Data')
			$xmlWriter.WriteAttributeString('Name', 'EntityTypeAliasName')
            if (!$associatedEntityType.Alias)
            {
                $xmlWriter.WriteString("")
            }
            else
            {
                $xmlWriter.WriteString("$($normalizedDotNetAlias).$($associatedEntityType.Name)")
            }
			$xmlWriter.WriteEndElement()
			$xmlWriter.WriteStartElement('Data')
			$xmlWriter.WriteAttributeString('Name', 'EntityTypeName')
			$xmlWriter.WriteString("$($normalizedDotNetNamespace).$($associatedEntityType.Name)")
			$xmlWriter.WriteEndElement()
            $xmlWriter.WriteStartElement('Data')
			$xmlWriter.WriteAttributeString('Name', 'IsSingleton')
			$xmlWriter.WriteString("True")
			$xmlWriter.WriteEndElement()

            # Add information about navigation properties and their types 
            # Used in scenario where user requests navigation property in -Select query
            foreach ($navProperty in $navigationProperties)
            {
                if ($navProperty)
                {
                    $xmlWriter.WriteStartElement('Data')
                    $xmlWriter.WriteAttributeString('Name', $navProperty.Name + 'NavigationProperty')
                    $xmlWriter.WriteString($navProperty.Type)
                    $xmlWriter.WriteEndElement()
                }
            }

			# Add UpdateRequestMethod to privateData
			$xmlWriter.WriteStartElement('Data')
			$xmlWriter.WriteAttributeString('Name', 'UpdateRequestMethod')
			$xmlWriter.WriteString("$UpdateRequestMethod")
			$xmlWriter.WriteEndElement()

		$xmlWriter.WriteEndElement()

		SaveCDXMLFooter $xmlWriter

		Write-Verbose ($LocalizedData.VerboseSavedCDXML -f $($associatedEntityType.Name), $Path)
    }
}

#########################################################
# Saves InstanceCmdlets node to CDXML
#########################################################
function SaveCDXMLInstanceCmdlets 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.MetadataV4] $Metadata, 
        [System.Collections.ArrayList] $GlobalMetadata,
        [ODataUtils.EntityTypeV4] $EntityType,
        $keys,
        $navigationProperties,
        $CmdletAdapter,
        [Hashtable] $complexTypeMapping,
        [bool] $isSingleton
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLInstanceCmdlets") }
    if($Metadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "Metadata", "SaveCDXMLInstanceCmdlets") }

    $xmlWriter.WriteStartElement('InstanceCmdlets')
        $xmlWriter.WriteStartElement('GetCmdletParameters')
            # adding key parameters and association parameters to QueryableProperties, each in a different parameter set
            # to be used by GET cmdlet
            if (($keys.Length -gt 0) -or ($navigationProperties.Length -gt 0))
            { 
                $queryableNavProperties = @{} 
                
                if ($isSingleton -eq $false)
                {
                    foreach ($navProperty in $navigationProperties)
                    {
                        if ($navProperty -ne $null)
                        {
                            $associatedType = GetAssociatedType $Metadata $GlobalMetadata $navProperty
                            $associatedTypeKeyProperties = $associatedType.EntityProperties | ? { $_.IsKey }
                        
                            # Make sure associated parameter (based on navigation property) has EntitySet or Singleton, which makes it accessible from the service root
                            # Otherwise the Uri for associated navigation property won't be valid
                            if ($associatedTypeKeyProperties.Length -gt 0 -and (ShouldBeAssociatedParameter $GlobalMetadata $EntityType $associatedType $isSingleton))
                            {                            
                                $queryableNavProperties.Add($navProperty, $associatedTypeKeyProperties)
                            }
                        }
                    }
                }
                
                $defaultCmdletParameterSet = 'Default'
                if ($isSingleton -eq $true -and $queryableNavProperties.Count -gt 0)
                {
                    foreach($item in $queryableNavProperties.GetEnumerator()) 
                    {
                        $defaultCmdletParameterSet = $item.Key.Name
                        break
                    }
                }
                $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', $defaultCmdletParameterSet)

                
                $xmlWriter.WriteStartElement('QueryableProperties')
                
                $position = 0
                
                if ($isSingleton -eq $false)
                {
                    $keys | ? { $_ -ne $null } | % {
                            $xmlWriter.WriteStartElement('Property')
                            $xmlWriter.WriteAttributeString('PropertyName', $_.Name)

                                $xmlWriter.WriteStartElement('Type')
                                $PSTypeName = Convert-ODataTypeToCLRType $_.TypeName $complexTypeMapping
                                $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                                $xmlWriter.WriteEndElement()

                                $xmlWriter.WriteStartElement('RegularQuery')
                                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                                    $xmlWriter.WriteAttributeString('PSName', $_.Name)
                                    $xmlWriter.WriteAttributeString('CmdletParameterSets', 'Default')
                                    $xmlWriter.WriteAttributeString('IsMandatory', $_.IsMandatory.ToString().ToLower())
                                    $xmlWriter.WriteAttributeString('Position', $position)
                                    $xmlWriter.WriteEndElement()
                                $xmlWriter.WriteEndElement()
                            $xmlWriter.WriteEndElement()

                            $position++
                        }
                }
    
                if ($queryableNavProperties.Count -gt 0)
                {
                    foreach($item in $queryableNavProperties.GetEnumerator()) 
                    {
                        $xmlWriter.WriteStartElement('Property')
                        $xmlWriter.WriteAttributeString('PropertyName', $item.Key.Name + ':' + $item.Value.Name + ':Key')

                            $xmlWriter.WriteStartElement('Type')
                            $PSTypeName = Convert-ODataTypeToCLRType $item.Value.TypeName $complexTypeMapping
                            $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                            $xmlWriter.WriteEndElement()

                            $xmlWriter.WriteStartElement('RegularQuery')
                                $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                                $xmlWriter.WriteAttributeString('PSName', 'Associated' + $item.Key.Name + $item.Value.Name)
                                $xmlWriter.WriteAttributeString('CmdletParameterSets', $item.Key.Name)
                                $xmlWriter.WriteAttributeString('IsMandatory', 'false')
                                $xmlWriter.WriteEndElement()
                            $xmlWriter.WriteEndElement()
                        $xmlWriter.WriteEndElement()
                    }
                }

                if ($isSingleton -eq $false)
                {
                    # Add Query Parameters (i.e., Top, Skip, OrderBy, Filter) to the generated Get-* cmdlets.
                    $queryParameters = 
                    @{
                        "Filter" = "Edm.String";
                        "IncludeTotalResponseCount" = "switch";
                        "OrderBy" = "Edm.String";
                        "Select" = "Edm.String";  
                        "Skip" = "Edm.Int32"; 
                        "Top" = "Edm.Int32";
                    }
                }
                else
                {
                    $queryParameters = 
                    @{
                        "Select" = "Edm.String";
                    }
                }
                
                foreach($currentQueryParameter in $queryParameters.Keys)
                {
                    $xmlWriter.WriteStartElement('Property')
                    $xmlWriter.WriteAttributeString('PropertyName', "QueryOption:" + $currentQueryParameter)
                    $xmlWriter.WriteStartElement('Type')
                    $PSTypeName = Convert-ODataTypeToCLRType $queryParameters[$currentQueryParameter]
                    $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                    $xmlWriter.WriteEndElement()
                    $xmlWriter.WriteStartElement('RegularQuery')
                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                    $xmlWriter.WriteAttributeString('PSName', $currentQueryParameter)

                    if($queryParameters[$currentQueryParameter] -eq "Edm.String")
                    {
                        $xmlWriter.WriteStartElement('ValidateNotNullOrEmpty')
                        $xmlWriter.WriteEndElement()
                    }

                    if($queryParameters[$currentQueryParameter] -eq "Edm.Int32")
                    {
                        $xmlWriter.WriteStartElement('ValidateRange')
                        $xmlWriter.WriteAttributeString('Min', "1")
                        $xmlWriter.WriteAttributeString('Max', [int]::MaxValue)
                        $xmlWriter.WriteEndElement()
                    }

                    $xmlWriter.WriteEndElement()
                    $xmlWriter.WriteEndElement()
                    $xmlWriter.WriteEndElement()
                }                                        
                
                    
                $xmlWriter.WriteEndElement()
            }
        
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('GetCmdlet')
            $xmlWriter.WriteStartElement('CmdletMetadata')
            $xmlWriter.WriteAttributeString('Verb', 'Get')
            $xmlWriter.WriteEndElement()
        $xmlWriter.WriteEndElement()

    $xmlWriter.WriteEndElement()
}

# Helper Method
# Returns true if navigation property of $AssociatedType has corresponding EntitySet or Singleton
# If yes, then it should become an associated parameter in CDXML
function ShouldBeAssociatedParameter
{
    param
    (
        [System.Collections.ArrayList] $GlobalMetadata,
        [ODataUtils.EntityTypeV4] $EntityType,
        [ODataUtils.EntityTypeV4] $AssociatedType
    )

    # Check if associated type has navigation property, which links back to current type
    $associatedTypeNavProperties = $AssociatedType.NavigationProperties | ? { 
        $_.Type -eq ($EntityType.Namespace + "." + $EntityType.Name) -or 
        $_.Type -eq ($EntityType.Alias + "." + $EntityType.Name) -or
        $_.Type -eq ("Collection(" + $EntityType.Namespace + "." + $EntityType.Name + ")") -or 
        $_.Type -eq ("Collection(" + $EntityType.Alias + "." + $EntityType.Name + ")")
    }

    if ($associatedTypeNavProperties.Length -lt 1)
    {
        return $false
    }

    # Now check if associated parameter type (i.e, type of navigation property) has corresponding EntitySet or Singleton, 
    # which makes it accessible from the service root.
    # Otherwise the Uri for associated navigation property won't be valid
    foreach ($currentMetadata in $GlobalMetadata)
    {        
        # Look for EntitySet with given type
        foreach ($currentEntitySet in $currentMetadata.EntitySets)
        {
            if ($currentEntitySet.Type.Namespace -eq $EntityType.Namespace -and
                $currentEntitySet.Type.Name -eq $EntityType.Name)
            {
                return $true
            }
        }
        
        # Look for Singleton with given type
        foreach ($currentSingleton in $currentMetadata.Singletons)
        {                
            if ($currentSingleton.Type.Namespace -eq $EntityType.Namespace -and
                $currentSingleton.Type.Name -eq $EntityType.Name)
            {
                return $true
            }
        }
    }

    return $false
}

#########################################################
# Saves NewCmdlet node to CDXML
#########################################################
function SaveCDXMLNewCmdlet 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        $keyProperties,
        $nonNullableProperties,
        $nullableProperties,
        $navigationProperties,  
        $CmdletAdapter,
        $complexTypeMapping
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLNewCmdlet") }
    if($Metadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "Metadata", "SaveCDXMLNewCmdlet") }
    
    $xmlWriter.WriteStartElement('Cmdlet')
        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'New')
        $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', 'Default')
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', 'Create')
        $xmlWriter.WriteAttributeString('CmdletParameterSet', 'Default')
               
        AddParametersNode $xmlWriter $keyProperties $nonNullableProperties $nullableProperties $null $true $true $complexTypeMapping

        $xmlWriter.WriteEndElement()

        $navigationProperties | ? { $_ -ne $null } | % {
            $associatedType = GetAssociatedType $Metadata $GlobalMetadata $_
            $associatedEntitySet = GetEntitySetForEntityType $Metadata $associatedType

            $xmlWriter.WriteStartElement('Method')
            $xmlWriter.WriteAttributeString('MethodName', "Association:Create:$($associatedEntitySet.Name)")
            $xmlWriter.WriteAttributeString('CmdletParameterSet', $_.Name)
                    
            $associatedKeys = ($associatedType.EntityProperties | ? { $_.isKey })

            AddParametersNode $xmlWriter $associatedKeys $keyProperties $null "Associated$($_.Name)" $true $true $complexTypeMapping

            $xmlWriter.WriteEndElement()
        }
        
        $xmlWriter.WriteEndElement()
}

#########################################################
# Get corresponding EntityType for given EntitySet
#########################################################
function GetEntitySetForEntityType {
    param(
    [ODataUtils.MetadataV4] $Metadata,
    [ODataUtils.EntityTypeV4] $entityType
    )

    if($entityType -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntityType", "GetEntitySetForEntityType") }

    $result = $Metadata.EntitySets | ? { ($_.Type.Namespace -eq $entityType.Namespace) -and ($_.Type.Name -eq $entityType.Name) }

    if (($result.Count -eq 0) -and ($entityType.BaseType -ne $null))
    {
        GetEntitySetForEntityType $Metadata $entityType.BaseType
    }
    elseif ($result.Count -gt 1)
    {
        throw ($LocalizedData.WrongCountEntitySet -f (($entityType.Namespace + "." + $entityType.Name), $result.Count))
    }

    $result
}

#########################################################
# Saves RemoveCmdlet node to CDXML
#########################################################
function SaveCDXMLRemoveCmdlet 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.MetadataV4] $Metadata,
        [System.Collections.ArrayList] $GlobalMetadata,
        $keyProperties,
        $navigationProperties,
        $CmdletAdapter,
        $complexTypeMapping
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLRemoveCmdlet") }
    if($Metadata -eq $null) { throw ($LocalizedData.ArguementNullError -f "Metadata", "SaveCDXMLRemoveCmdlet") }
    
    $xmlWriter.WriteStartElement('Cmdlet')
        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Remove')
        $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', 'Default')
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', 'Delete')
        $xmlWriter.WriteAttributeString('CmdletParameterSet', 'Default')

        AddParametersNode $xmlWriter $keyProperties $nul $null $null $true $true $complexTypeMapping
            
        $xmlWriter.WriteEndElement()

        $navigationProperties | ? { $_ -ne $null } | % {

            $associatedType = GetAssociatedType $Metadata $GlobalMetadata $_
            $associatedEntitySet = GetEntitySetForEntityType $Metadata $associatedType

         $xmlWriter.WriteStartElement('Method')
            $xmlWriter.WriteAttributeString('MethodName', "Association:Delete:$($associatedEntitySet.Name)")
            $xmlWriter.WriteAttributeString('CmdletParameterSet', $_.Name)
                
                $associatedType = GetAssociatedType $Metadata $GlobalMetadata $_
                $associatedKeys = ($associatedType.EntityProperties | ? { $_.isKey })

            AddParametersNode $xmlWriter $associatedKeys $keyProperties $null "Associated$($_.Name)" $true $true $complexTypeMapping

            $xmlWriter.WriteEndElement()
        }
    $xmlWriter.WriteEndElement()
}

#########################################################
# Gets associated instance's EntityType for a given navigation property
#########################################################
function GetAssociatedType {
    param(
    [ODataUtils.MetadataV4] $Metadata,
    [System.Collections.ArrayList] $GlobalMetadata,
    [ODataUtils.NavigationPropertyV4] $navProperty    
    )

    $associationType = $navProperty.Type
    $associationType = $associationType.Replace($Metadata.Namespace + ".", "")
    $associationType = $associationType.Replace($Metadata.Alias + ".", "")
    $associationType = $associationType.Replace("Collection(", "")
    $associationType = $associationType.Replace(")", "")

    $associatedType = $Metadata.EntityTypes | ? { $_.Name -eq $associationType }
    
    if (!$associatedType -and $GlobalMetadata -ne $null)
    {
        $associationFullTypeName = $navProperty.Type.Replace("Collection(", "").Replace(")", "")

        foreach ($referencedMetadata in $GlobalMetadata)
        {
            if (($associatedType = $referencedMetadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $associationFullTypeName -or $_.Alias + "." + $_.Name -eq $associationFullTypeName }) -ne $null -or
                ($associatedType = $referencedMetadata.ComplexTypes | Where { $_.Namespace + "." + $_.Name -eq $associationFullTypeName -or $_.Alias + "." + $_.Name -eq $associationFullTypeName }) -ne $null -or 
                ($associatedType = $referencedMetadata.EnumTypes | Where { $_.Namespace + "." + $_.Name -eq $associationFullTypeName -or $_.Alias + "." + $_.Name -eq $associationFullTypeName }) -ne $null)
            {
                # Found associated class
                break
            }
        }
    }

    if ($associatedType.Count -lt 1)
    {
        throw ($LocalizedData.AssociationNotFound -f $associationType)
    }
    elseif ($associatedType.Count -gt 1)
    {
        throw ($LocalizedData.TooManyMatchingAssociationTypes -f $associatedType.Count, $associationType)
    }

    # return associated EntityType
    $associatedType
}

#########################################################
# Saves CDXML for Instance/Service level actions
#########################################################
function SaveCDXMLAction 
{
    param
    (
        [System.Xml.XmlWriter] $xmlWriter,
        [ODataUtils.ActionV4] $action,
        [AllowEmptyString()]
        [string] $noun,
        [bool] $isInstanceAction,
        [ODataUtils.TypeProperty] $keys,
        [Hashtable] $complexTypeMapping
    )

    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLAction") }
    if($action -eq $null) { throw ($LocalizedData.ArguementNullError -f "action", "SaveCDXMLAction") }

    $xmlWriter.WriteStartElement('Cmdlet')

        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Invoke')
        $xmlWriter.WriteAttributeString('Noun', "$($noun)$($action.Name)")
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', "Action:$($action.Name):$($action.EntitySet.Name)")

            $xmlWriter.WriteStartElement('Parameters')

            $keys | ? { $_ -ne $null } | % {
                $xmlWriter.WriteStartElement('Parameter')
                $xmlWriter.WriteAttributeString('ParameterName', $_.Name + ':Key')

                    $xmlWriter.WriteStartElement('Type')
                    $PSTypeName = Convert-ODataTypeToCLRType $_.TypeName $complexTypeMapping
                    $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                    $xmlWriter.WriteEndElement()

                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                    $xmlWriter.WriteAttributeString('PSName', $_.Name)
                    $xmlWriter.WriteAttributeString('IsMandatory', 'true')
                    $xmlWriter.WriteEndElement()
                $xmlWriter.WriteEndElement()
            }

            $i = -1
            foreach ($parameter in $action.Parameters)
            {
                $i++

                # for Instance actions, first parameter is Entity Set which we refer to using keys
                if ($isInstanceAction -and ($i -eq 0))
                {
                    continue
                }

                $xmlWriter.WriteStartElement('Parameter')
                $xmlWriter.WriteAttributeString('ParameterName', $parameter.Name)

                    $xmlWriter.WriteStartElement('Type')
                    $PSTypeName = Convert-ODataTypeToCLRType $parameter.TypeName
                    $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                    $xmlWriter.WriteEndElement()

                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                    $xmlWriter.WriteAttributeString('PSName', $parameter.Name)
                    if (-not $parameter.IsNullable)
                    {
                        $xmlWriter.WriteAttributeString('IsMandatory', 'true')
                    }
                    $xmlWriter.WriteEndElement()
                $xmlWriter.WriteEndElement()
            }

            # Add -Force parameter to Service Action cmdlets.
            AddParametersNode $xmlWriter $null $null $null $null $true $false $complexTypeMapping

            $xmlWriter.WriteEndElement()
        $xmlWriter.WriteEndElement()

    $xmlWriter.WriteEndElement()

    $xmlWriter
}

#########################################################
# Saves CDXML for Instance/Service level functions
#########################################################
function SaveCDXMLFunction 
{
    param
    (
        [System.Xml.XmlWriter] $xmlWriter,
        [ODataUtils.FunctionV4] $function,
        [AllowEmptyString()]
        [string] $noun,
        [bool] $isInstanceAction,
        [ODataUtils.TypeProperty] $keys,
        [Hashtable] $complexTypeMapping
    )
    
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "SaveCDXMLFunction") }
    if($function -eq $null) { throw ($LocalizedData.ArguementNullError -f "function", "SaveCDXMLFunction") }

    $xmlWriter.WriteStartElement('Cmdlet')

        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Invoke')
        $xmlWriter.WriteAttributeString('Noun', "$($noun)$($function.Name)")
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        if (!$function.EntitySet)
        {
            $xmlWriter.WriteAttributeString('MethodName', "Action:$($function.Name):$($function.ReturnType)")
        }
        else
        {
            $xmlWriter.WriteAttributeString('MethodName', "Action:$($function.Name):$($function.EntitySet)")            
        }

            $xmlWriter.WriteStartElement('Parameters')

            $keys | ? { $_ -ne $null } | % {
                $xmlWriter.WriteStartElement('Parameter')
                $xmlWriter.WriteAttributeString('ParameterName', $_.Name + ':Key')

                    $xmlWriter.WriteStartElement('Type')
                    $PSTypeName = Convert-ODataTypeToCLRType $_.TypeName $complexTypeMapping
                    $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                    $xmlWriter.WriteEndElement()

                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                    $xmlWriter.WriteAttributeString('PSName', $_.Name)
                    $xmlWriter.WriteAttributeString('IsMandatory', 'true')
                    $xmlWriter.WriteEndElement()
                $xmlWriter.WriteEndElement()
            }

            $i = -1
            foreach ($parameter in $function.Parameters)
            {
                $i++

                # for Instance actions, first parameter is Entity Set which we refer to using keys
                if ($isInstanceAction -and ($i -eq 0))
                {
                    continue
                }

                $xmlWriter.WriteStartElement('Parameter')
                $xmlWriter.WriteAttributeString('ParameterName', $parameter.Name)

                    $xmlWriter.WriteStartElement('Type')
                    $PSTypeName = Convert-ODataTypeToCLRType $parameter.Type
                    $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                    $xmlWriter.WriteEndElement()

                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                    $xmlWriter.WriteAttributeString('PSName', $parameter.Name)
                    if (-not $parameter.IsNullable)
                    {
                        $xmlWriter.WriteAttributeString('IsMandatory', 'true')
                    }
                    $xmlWriter.WriteEndElement()
                $xmlWriter.WriteEndElement()
            }

            # Add -Force parameter to Service Function cmdlets.
            AddParametersNode $xmlWriter $null $null $null $null $true $false $complexTypeMapping

            $xmlWriter.WriteEndElement()
        $xmlWriter.WriteEndElement()

    $xmlWriter.WriteEndElement()

    $xmlWriter
}

#########################################################
# Saves CDXML for Service-level actions and functions
#########################################################
function SaveServiceActionsCDXML 
{
    param
    (
        [System.Collections.ArrayList] $GlobalMetadata,
        [ODataUtils.ODataEndpointProxyParameters] $ODataEndpointProxyParameters,
        [string] $Path,
        [Hashtable] $complexTypeMapping,
        [string] $progressBarStatus,
        [string] $CmdletAdapter
    )

    $xmlWriter = New-Object System.XMl.XmlTextWriter($Path,$Null)

    if ($xmlWriter -eq $null)
    {
        throw $LocalizedData.XmlWriterInitializationError -f "ServiceActions"
    }

    $xmlWriter = SaveCDXMLHeader $xmlWriter $ODataEndpointProxyParameters.Uri 'ServiceActions' 'ServiceActions' -CmdletAdapter $CmdletAdapter

    $actions = @()
    $functions = @()

    foreach ($Metadata in $GlobalMetadata)
    {
        $actions += $Metadata.Actions | Where-Object { $_.EntitySet -eq '' -or $_.EntitySet -eq $null }
        $functions += $Metadata.Functions | Where-Object { $_.EntitySet -eq '' -or $_.EntitySet -eq $null }
    }

    if ($actions.Length -gt 0 -or $functions.Length -gt 0)
    {
        $xmlWriter.WriteStartElement('StaticCmdlets')
    }

    # Save actions
    if ($actions.Length -gt 0)
    {
        foreach ($action in $actions)
        {
            if ($action -ne $null)
            {
                $xmlWriter = SaveCDXMLAction $xmlWriter $action '' $false $null $complexTypeMapping
            }
        }
    }

    # Save functions
    if ($functions.Length -gt 0)
    {
        foreach ($function in $functions)
        {
            if ($function -ne $null)
            {
                $xmlWriter = SaveCDXMLFunction $xmlWriter $function '' $false $null $complexTypeMapping
            }
        }
    }

    if ($actions.Length -gt 0 -or $functions.Length -gt 0)
    {
        $xmlWriter.WriteEndElement()
    }

    $xmlWriter.WriteStartElement('CmdletAdapterPrivateData')
    $xmlWriter.WriteStartElement('Data')
    $xmlWriter.WriteAttributeString('Name', 'Namespace')
    $xmlWriter.WriteString("$($EntitySet.Namespace)")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteStartElement('Data')
    $xmlWriter.WriteAttributeString('Name', 'Alias')
    if (!$EntitySet.Alias)
    {
        $xmlWriter.WriteString("")
    }
    else
    {
        $xmlWriter.WriteString("$($EntitySet.Alias)")
    }
    $xmlWriter.WriteEndElement()
    
    $xmlWriter.WriteStartElement('Data')
    $xmlWriter.WriteAttributeString('Name', 'CreateRequestMethod')
    $xmlWriter.WriteString("Post")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()

    SaveCDXMLFooter $xmlWriter

    Write-Verbose ($LocalizedData.VerboseSavedServiceActions -f $Path)

    # Write progress bar message
    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 60 20 1 1
}

#########################################################
# GenerateModuleManifest is a helper function used 
# to generate a wrapper module manifest file. The
# generated module manifest is persisted to the disk at
# the specified OutputModule path. When the module 
# manifest is imported, the following commands will 
# be imported:
# 1. Get, Set, New & Remove proxy cmdlets for entity 
#    sets and singletons.
# 2. If the server side Odata endpoint exposes complex
#    types, enum types, type definitions, then the corresponding 
#    client side proxy types imported.
# 3. Service Action/Function proxy cmdlets.   
#########################################################
function GenerateModuleManifest 
{
    param
    (
        [System.Collections.ArrayList] $GlobalMetadata,
        [String] $ModulePath,
        [string[]] $AdditionalModules,
        [Hashtable] $resourceNameMappings,
        [string] $progressBarStatus
    )

    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "progressBarStatus", "GenerateModuleManifest") }

    $NestedModules = @()

    foreach ($Metadata in $GlobalMetadata)
    {
        foreach ($entitySet in $Metadata.EntitySets)
        {
            $entitySetName = $entitySet.Name 
            if(($null -ne $resourceNameMappings) -and 
            $resourceNameMappings.ContainsKey($entitySetName))
            {
                $entitySetName = $resourceNameMappings[$entitySetName]
            }
            else
            {
                $entitySetName = $entitySet.Type.Name
            }

            $NestedModules += "$OutputModule\$($entitySetName).cdxml"
        }
    
        foreach ($singleton in $Metadata.SingletonTypes)
        {
            $singletonName = $singleton.Name 
            $NestedModules += "$OutputModule\$($singletonName)" + "Singleton" + ".cdxml"
        }
    }
    
    New-ModuleManifest -Path $ModulePath -NestedModules ($AdditionalModules + $NestedModules)

    Write-Verbose ($LocalizedData.VerboseSavedModuleManifest -f $ModulePath)

    # Update the Progress Bar.
    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 80 20 1 1
}

#########################################################
# This is a helper function used to generate complex 
# type definition from the metadata.
#########################################################
function GenerateComplexTypeDefinition 
{
    param
    (
        [System.Collections.ArrayList] $GlobalMetadata,
        [string] $OutputModule,
        [string] $typeDefinationFileName,
        $normalizedNamespaces
    )

    $Path = "$OutputModule\$typeDefinationFileName"
    $date = Get-Date

    $output = @"
# This module was generated by PSODataUtils on $date.

`$typeDefinitions = @"
using System;
using System.Management.Automation;
using System.ComponentModel;

"@
    # We are currently generating classes for EntityType & ComplexType 
    # definition exposed in the metadata.
    
    $complexTypeMapping = @{}

    # First, create complex type mappings for all metadata files at once
    foreach ($metadata in $GlobalMetadata)
    {
        $typesToBeGenerated = $metadata.EntityTypes+$metadata.ComplexTypes
        $enumTypesToBeGenerated = $metadata.EnumTypes
        $typeDefinitionsToBeGenerated = $metadata.TypeDefinitions

        foreach ($entityType in $typesToBeGenerated)
        {
            if ($entityType -ne $null)
            {
                $entityTypeFullName = $entityType.Namespace + '.' + $entityType.Name
                if(!$complexTypeMapping.ContainsKey($entityTypeFullName))
                {
                    $complexTypeMapping.Add($entityTypeFullName, $entityType.Name)
                }

                # In short name we use Alias instead of Namespace
                # We will add short name to $complexTypeMapping to enable Alias based search
                if ($entityType.Alias -ne $null -and $entityType.Alias -ne "")
                {
                    $entityTypeShortName = $entityType.Alias + '.' + $entityType.Name
                    if(!$complexTypeMapping.ContainsKey($entityTypeShortName))
                    {
                        $complexTypeMapping.Add($entityTypeShortName, $entityType.Name)
                    }
                }
            }
        }

        foreach ($enumType in $enumTypesToBeGenerated)
        {
            if ($enumType -ne $null)
            {
                $enumTypeFullName = $enumType.Namespace + '.' + $enumType.Name
                if(!$complexTypeMapping.ContainsKey($enumTypeFullName))
                {
                    $complexTypeMapping.Add($enumTypeFullName, $enumType.Name)
                }

                if (($enumType.Alias -ne $null -and $enumType.Alias -ne "") -or ($metadata.Alias -ne $null -and $metadata.Alias -ne ""))
                {
                    if ($enumType.Alias -ne $null -and $enumType.Alias -ne "")
                    {
                        $alias = $enumType.Alias
                    }
                    else
                    {
                        $alias = $metadata.Alias
                    }

                    $enumTypeShortName = $alias + '.' + $enumType.Name
                    if(!$complexTypeMapping.ContainsKey($enumTypeShortName))
                    {
                        $complexTypeMapping.Add($enumTypeShortName, $enumType.Name)
                    }
                }
            }
        }

        foreach ($typeDefinition in $typeDefinitionsToBeGenerated)
        {
            if ($typeDefinition -ne $null)
            {
                $typeDefinitionFullName = $typeDefinition.Namespace + '.' + $typeDefinition.Name
                if(!$complexTypeMapping.ContainsKey($typeDefinitionFullName))
                {
                    $complexTypeMapping.Add($typeDefinitionFullName, $typeDefinition.Name)
                }

                # In short name we use Alias instead of Namespace
                # We will add short name to $complexTypeMapping to enable Alias based search
                if ($typeDefinition.Alias)
                {
                    $typeDefinitionShortName = $typeDefinition.Alias + '.' + $typeDefinition.Name
                    if(!$complexTypeMapping.ContainsKey($typeDefinitionShortName))
                    {
                        $complexTypeMapping.Add($typeDefinitionShortName, $typeDefinition.Name)
                    }
                }
            }
        }
    }

    # Now classes definitions will be generated
    foreach ($metadata in $GlobalMetadata)
    {
        $typesToBeGenerated = $metadata.EntityTypes+$metadata.ComplexTypes
        $enumTypesToBeGenerated = $metadata.EnumTypes
        $typeDefinitionsToBeGenerated = $metadata.TypeDefinitions

        if($typesToBeGenerated.Count -gt 0 -or $enumTypesToBeGenerated.Count -gt 0)
        {
            if ($metadata.Alias -ne $null -and $metadata.Alias -ne "")
            {            
                # Check if this namespace has to be normalized in the .Net namespace/class definitions file.
                $dotNetAlias = GetNamespace $metadata.Alias $normalizedNamespaces

                $output += @"

namespace $($dotNetAlias)
{
"@
            }
            else
            {   
                # Check if this namespace has to be normalized in the .Net namespace/class definitions file.
                $dotNetNamespace = GetNamespace $metadata.Namespace $normalizedNamespaces
         
                $output += @"
                                
namespace $($dotNetNamespace)
{
"@
            }

            foreach ($typeDefinition in $typeDefinitionsToBeGenerated)
            {
                if ($typeDefinition -ne $null)
                {
                    Write-Verbose ($LocalizedData.VerboseAddingTypeDefinationToGeneratedModule -f $typeDefinitionFullName, "$OutputModule\$typeDefinationFileName")

                    $output += "`n  public class $($typeDefinition.Name)`n  {"
                    $typeName = Convert-ODataTypeToCLRType $typeDefinition.BaseTypeStr $complexTypeMapping
                    $dotNetPropertyNamespace = GetNamespace $typeName $normalizedNamespaces $true
                    $output += "`n     public $dotNetPropertyNamespace value;"
                    $output += @"
`n  }
"@
                }
            }
            
            $DotNETKeywords = ("abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "in", "int", "interface", "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "out", "override", "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while", "add", "alias", "ascending", "async", "await", "descending", "dynamic", "from", "get", "global", "group", "into", "join", "let", "orderby", "partial", "partial", "remove", "select", "set", "value", "var", "where", "yield")

            foreach ($enumType in $enumTypesToBeGenerated)
            {
                if ($enumType -ne $null)
                {
                    $enumTypeFullName = $enumType.Namespace + '.' + $enumType.Name
                
                    Write-Verbose ($LocalizedData.VerboseAddingTypeDefinationToGeneratedModule -f $enumTypeFullName, "$OutputModule\$typeDefinationFileName")

                    $output += "`n  public enum $($enumType.Name)`n  {"
                    
                    $properties = $null

                    for($index = 0; $index -lt $enumType.Members.Count; $index++)
                    {
                        $memberName = $enumType.Members[$index].Name
                        $formattedMemberName = [System.Text.RegularExpressions.Regex]::Replace($memberName, "[^0-9a-zA-Z]", "_");
                        $memberValue = $enumType.Members[$index].Value
                        
                        if ($DotNETKeywords -contains $formattedMemberName)
                        {
                            # If member name is a known keyword in .Net, add '@' prefix
                            $formattedMemberName = '@' + $formattedMemberName
                        }

                        if ($formattedMemberName -match "^[0-9]*$")
                        {
                            # If member name is a numeric value, add 'm_' prefix
                            $formattedMemberName = 'm_' + $formattedMemberName
                        }

                        if ($memberName -ne $formattedMemberName -or $formattedMemberName -like '@*' -or  $formattedMemberName -like 'm_*')
                        {
                            # Add Description attribute to preserve original value
                            $properties += "`n     [Description(`"$($memberName)`")]$formattedMemberName"
                        }
                        else
                        {
                            $properties += "`n     $memberName"
                        }

                        if ($memberValue)
                        {
                            $properties += " = $memberValue,"
                        }
                        else
                        {
                            $properties += ","
                        }
                    }

                    $output += $properties
                    $output += @"
`n  }
"@
                }
            }

            foreach ($entityType in $typesToBeGenerated)
            {
                if ($entityType -ne $null)
                {
                    $entityTypeFullName = $entityType.Namespace + '.' + $entityType.Name
                
                    Write-Verbose ($LocalizedData.VerboseAddingTypeDefinationToGeneratedModule -f $entityTypeFullName, "$OutputModule\$typeDefinationFileName")

                    if ($entityType.BaseTypeStr -ne $null -and $entityType.BaseTypeStr -ne '' -and $entityType.BaseType -eq $null)
                    {
                        # This class inherits from another class, but we were not able to find base class during Parsing.
                        # We'll make another attempt.
                        foreach ($referencedMetadata in $GlobalMetadata)
                        {
                            if (($baseType = $referencedMetadata.EntityTypes | Where { $_.Namespace + "." + $_.Name -eq $entityType.BaseTypeStr -or $_.Alias + "." + $_.Name -eq $entityType.BaseTypeStr }) -ne $null -or
                                ($baseType = $referencedMetadata.ComplexTypes | Where { $_.Namespace + "." + $_.Name -eq $entityType.BaseTypeStr -or $_.Alias + "." + $_.Name -eq $entityType.BaseTypeStr }) -ne $null)
                            {
                                # Found base class
                                $entityType.BaseType = $baseType
                                break
                            }
                        }
                    }

                    if($null -ne $entityType.BaseType)
                    {
                        if ((![string]::IsNullOrEmpty($entityType.BaseType.Alias) -and $entityType.BaseType.Alias -eq $entityType.Alias) -or
                            (![string]::IsNullOrEmpty($entityType.BaseType.Namespace) -and $entityType.BaseType.Namespace -eq $entityType.Namespace))
                        {
                            $fullBaseTypeName = $entityType.BaseType.Name
                        }
                        else
                        {
                            # Base type can be defined in different namespace. For that reason we include namespace or alias.
                            if (![string]::IsNullOrEmpty($entityType.BaseType.Alias))
                            {
                                # Check if derived alias has to be normalized.
                                $normalizedDotNetAlias = GetNamespace $entityType.BaseType.Alias $normalizedNamespaces
                                $fullBaseTypeName = $normalizedDotNetAlias + "." + $entityType.BaseType.Name
                            }
                            else
                            {
                                # Check if derived namespace has to be normalized.
                                $normalizedDotNetNamespace = GetNamespace $entityType.BaseType.Namespace $normalizedNamespaces
                                $fullBaseTypeName = $normalizedDotNetNamespace + "." + $entityType.BaseType.Name
                            }
                        }

                        $output += "`n  public class $($entityType.Name) : $($fullBaseTypeName)`n  {"
                    }
                    else
                    {
                        $output += "`n  public class $($entityType.Name)`n  {"
                    }

                    $properties = $null

                    for($index = 0; $index -lt $entityType.EntityProperties.Count; $index++)
                    {
                        $property = $entityType.EntityProperties[$index]
                        $typeName = Convert-ODataTypeToCLRType $property.TypeName $complexTypeMapping

                        if ($typeName.StartsWith($metadata.Namespace + "."))
                        {
                            $dotNetPropertyNamespace = $typeName.Replace($metadata.Namespace + ".", "")
                        }
                        elseif ($typeName.StartsWith($metadata.Alias + "."))
                        {
                            $dotNetPropertyNamespace = $typeName.Replace($metadata.Alias + ".", "")
                        }
                        else
                        {
                            $dotNetPropertyNamespace = GetNamespace $typeName $normalizedNamespaces $true
                        }

                        $properties += "`n     public $dotNetPropertyNamespace  $($property.Name);"
                    }

                    $output += $properties
                    $output += @"
`n  }
"@
                }
            }

            $output += "`n}`n"
        }
    }
    $output += """@`n"
    $output += "Add-Type -TypeDefinition `$typeDefinitions -IgnoreWarnings`n"
    $output | Out-File -FilePath $Path
    Write-Verbose ($LocalizedData.VerboseSavedTypeDefinationModule -f $typeDefinationFileName, $OutputModule)

    return $complexTypeMapping
}