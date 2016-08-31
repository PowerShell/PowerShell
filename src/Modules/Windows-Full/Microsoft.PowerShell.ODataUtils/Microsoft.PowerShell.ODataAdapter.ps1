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

    [xml] $metadataXML = GetMetaData $MetadataUri $PSCmdlet $Credential $Headers

    [ODataUtils.Metadata] $metaData = ParseMetadata $metadataXML $MetadataUri $CmdletAdapter $PSCmdlet

    VerifyMetaData $MetadataUri $metaData $AllowClobber.IsPresent $PSCmdlet $progressBarStatus $CmdletAdapter $CustomData $ResourceNameMapping
                
    GenerateClientSideProxyModule $metaData $MetadataUri $Uri $OutputModule $CreateRequestMethod $UpdateRequestMethod $CmdletAdapter $ResourceNameMapping $CustomData $ProgressBarStatus $PSCmdlet
}

#########################################################
# ParseMetaData is a helper function used to parse the 
# metadata to convert it in to an object structure for 
# further consumption during proxy generation.
######################################################### 
function ParseMetaData 
{
    param
    (
        [xml]    $metadataXml,
        [string] $metaDataUri,
        [string] $cmdletAdapter,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    # $metaDataUri is already validated at the cmdlet layer.
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "ParseMetadata") }

    if($metadataXml -eq $null)
    {
        $errorMessage = ($LocalizedData.InValidXmlInMetadata -f $metaDataUri)
        $exception = [System.InvalidOperationException]::new($errorMessage, $_.Exception)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetadataUriFormat" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metaDataUri
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }

    Write-Verbose $LocalizedData.VerboseParsingMetadata

    # Check the OData version in the fetched metadata to make sure that
    # OData version (and hence the protocol) used in the metadata is
    # supported by the adapter used for executing the generated
    # proxy cmdlets.
    if(($metadataXML -ne $null) -and ($metadataXML.Edmx -ne $null))
    {
        if($null -eq $metadataXML.Edmx.Version)
        {
            $errorMessage = ($LocalizedData.ODataVersionNotFound -f $MetadataUri)
            $exception = [System.InvalidOperationException]::new($errorMessage)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyODataVersionNotFound" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $MetadataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }

        $metaDataVersion = New-Object -TypeName System.Version -ArgumentList @($metadataXML.Edmx.Version)

        # When we support plug-in model, We would have to fetch the 
        # $minSupportedVersionString & $maxSupportedVersionString 
        # from the plug-in instead of using an hardcoded value.
        $minSupportedVersionString = '1.0'
        $maxSupportedVersionString = '3.0'
        $minSupportedVersion = New-Object -TypeName System.Version -ArgumentList @($minSupportedVersionString)
        $maxSupportedVersion = New-Object -TypeName System.Version -ArgumentList @($maxSupportedVersionString)

        $minVersionComparisonResult = $minSupportedVersion.CompareTo($metaDataVersion)
        $maxVersionComparisonResult = $maxSupportedVersion.CompareTo($metaDataVersion)

        if(-not($minVersionComparisonResult -lt $maxVersionComparisonResult))
        {
            $errorMessage = ($LocalizedData.ODataVersionNotSupported -f $metadataXML.Edmx.Version, $MetadataUri, $minSupportedVersionString, $maxSupportedVersionString, $CmdletAdapter)
            $exception = [System.NotSupportedException]::new($errorMessage)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyODataVersionNotSupported" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $MetadataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }
    }
    else
    {
        $errorMessage = ($LocalizedData.InValidMetadata -f $MetadataUri)
        $exception = [System.InvalidOperationException]::new($errorMessage)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetadata" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $MetadataUri
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }

    foreach ($schema in $MetadataXML.Edmx.DataServices.Schema)
    {
        if (($schema -ne $null) -and [string]::IsNullOrEmpty($schema.NameSpace ))
        {
            $callerPSCmdlet = $callerPSCmdlet -as [System.Management.Automation.PSCmdlet]
            $errorMessage = ($LocalizedData.InValidSchemaNamespace -f $metaDataUri)
            $exception = [System.InvalidOperationException]::new($errorMessage)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidSchemaNamespace" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metaDataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }
    }

    $metaData = New-Object -TypeName ODataUtils.Metadata
    
    # this is a processing queue for those types that require base types that haven't been defined yet
    $entityAndComplexTypesQueue = @{}

    foreach ($schema in $metadataXml.Edmx.DataServices.Schema)
    {
        if ($schema -eq $null)
        {
            Write-Error $LocalizedData.EmptySchema
            continue
        }

        if ($metadata.Namespace -eq $null)
        {
            $metaData.Namespace = $schema.Namespace
        }

        foreach ($entityType in $schema.EntityType)
        {
            $baseType = $null

            if ($entityType.BaseType -ne $null)
            {
                # add it to the processing queue
                $baseType = GetBaseType $entityType $metaData
                if ($baseType -eq $null)
                {
                    $entityAndComplexTypesQueue[$entityType.BaseType] += @(@{type='EntityType'; value=$entityType})
                    continue
                }
            }

            [ODataUtils.EntityType] $newType = ParseMetadataTypeDefinition $entityType $baseType $metaData $schema.Namespace $true
            $metaData.EntityTypes += $newType
            AddDerivedTypes $newType $entityAndComplexTypesQueue $metaData $schema.Namespace
        }

        foreach ($complexType in $schema.ComplexType)
        {
            $baseType = $null

            if ($complexType.BaseType -ne $null)
            {
                # add it to the processing queue
                $baseType = GetBaseType $complexType $metaData
                if ($baseType -eq $null)
                {
                    $entityAndComplexTypesQueue[$entityType.BaseType] += @(@{type='ComplexType'; value=$complexType})
                    continue
                }
            }

            [ODataUtils.EntityType] $newType = ParseMetadataTypeDefinition $complexType $baseType $metaData $schema.Namespace $false
            $metaData.ComplexTypes += $newType
            AddDerivedTypes $newType $entityAndComplexTypesQueue $metaData $schema.Namespace
        }
    }

    foreach ($schema in $metadataXml.Edmx.DataServices.Schema)
    {
        foreach ($entityContainer in $schema.EntityContainer)
        {
            if ($entityContainer.IsDefaultEntityContainer)
            {
                $metaData.DefaultEntityContainerName = $entityContainer.Name
            }

            $entityTypeToEntitySetMapping = @{};
            foreach ($entitySet in $entityContainer.EntitySet)
            {
                $entityType = $metaData.EntityTypes | Where-Object { $_.Name -eq $entitySet.EntityType.Split('.')[-1] }
                $entityTypeName = $entityType.Name

                if($entityTypeToEntitySetMapping.ContainsKey($entityTypeName))
                {
                    $existingEntitySetName = $entityTypeToEntitySetMapping[$entityTypeName]

                    $errorMessage = ($LocalizedData.EntityNameConflictError -f $metaDataUri, $existingEntitySetName, $entitySet.Name, $entityTypeName)
                    $exception = [System.NotSupportedException]::new($errorMessage)
                    $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyEntityTypeMappingError" $null ([System.Management.Automation.ErrorCategory]::InvalidData) $exception $metaDataUri
                    $callerPSCmdlet.ThrowTerminatingError($errorRecord)
                }
                else
                {
                    $entityTypeToEntitySetMapping.Add($entityTypeName, $entitySet.Name)
                }

                $newEntitySet = [ODataUtils.EntitySet] @{
                    "Namespace" = $schema.Namespace;
                    "Name" = $entitySet.Name;
                    "Type" = $entityType;
                }

                $metaData.EntitySets += $newEntitySet
            }
        }
    }

    foreach ($schema in $metadataXml.Edmx.DataServices.Schema)
    {
        foreach ($association in $schema.Association)
        {
            $newAssociationType = [ODataUtils.AssociationType] @{
                "Namespace" = $schema.Namespace;
                "EndType1" = $metaData.EntityTypes | Where-Object { $_.Name -eq $association.End[0].Type.Split('.')[-1] };
                "NavPropertyName1" = $association.End[0].Role;
                "Multiplicity1" = $association.End[0].Multiplicity;

                "EndType2" = $metaData.EntityTypes | Where-Object { $_.Name -eq $association.End[1].Type.Split('.')[-1] };
                "NavPropertyName2" = $association.End[1].Role;
                "Multiplicity2" = $association.End[1].Multiplicity;
            }

            $newAssociation = [ODataUtils.AssociationSet] @{
                "Namespace" = $schema.Namespace;
                "Name" = $association.Name;
                "Type" = $newAssociationType;
            }
            
            $metaData.Associations += $newAssociation
        }
    }

    foreach ($schema in $metadataXml.Edmx.DataServices.Schema)
    {
        foreach ($action in $schema.EntityContainer.FunctionImport)
        {
            # HttpMethod is only used for legacy Service Operations
            if ($action.HttpMethod -eq $null)
            {
                if ($action.IsSideEffecting -ne $null)
                {
                    $isSideEffecting = $action.IsSideEffecting
                }
                else
                {
                    $isSideEffecting = $true
                }

                $newAction = [ODataUtils.Action] @{
                    "Namespace" = $schema.Namespace;
                    "Verb" = $action.Name;
                    "IsSideEffecting" = $isSideEffecting;
                    "IsBindable" = $action.IsBindable;
                    # we don't care about IsAlwaysBindable, since we populate actions information from $metaData
                    # so we can't know the state of the entity
                }
                
                # Actions are always SideEffecting, otherwise it's an OData function
                if ($newAction.IsSideEffecting -ne $false)
                {
                    foreach ($parameter in $action.Parameter)
                    {
                        if ($parameter.Nullable -ne $null)
                        {
                            $parameterIsNullable = [System.Convert]::ToBoolean($parameter.Nullable);
                        }

                        $newParameter = [ODataUtils.TypeProperty] @{
                            "Name" = $parameter.Name;
                            "TypeName" = $parameter.Type;
                            "IsNullable" = $parameterIsNullable
                        }

                        $newAction.Parameters += $newParameter
                    }

                    # IsBindable means it operates on Entity/ies
                    if ($newAction.IsBindable)
                    {
                        $regex = "Collection\((.+)\)"

                        if ($newAction.Parameters[0].TypeName -match $regex)
                        {
                            # action operating on a collection of entities
                            $insideTypeName = Convert-ODataTypeToCLRType $Matches[1]

                            $newAction.EntitySet = $metaData.EntitySets | Where-Object { ($_.Type.Namespace + "." + $_.Type.Name) -eq $insideTypeName }
                            $newAction.IsSingleInstance = $false
                        }
                        else
                        {
                            # actions operating on a single instance
                            $newAction.EntitySet = $metaData.EntitySets | Where-Object { ($_.Type.Namespace + "." + $_.Type.Name) -eq $newAction.Parameters[0].TypeName }

                            $newAction.IsSingleInstance = $true
                        }
                    }

                    $metaData.Actions += $newAction
                }
            }
        }
    }

    $metaData
}

#########################################################
# VerifyMetaData is a helper function used to validate 
# the processed metadata to make sure client side proxy
# can be created for the supplied metadata.
######################################################### 
function VerifyMetaData 
{
    param
    (
        [string]    $metaDataUri,
        [ODataUtils.Metadata]  $metaData,
        [boolean]   $allowClobber,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet,
        [string]    $progressBarStatus,
        [string]    $cmdletAdapter,
        [Hashtable] $customData,
        [Hashtable] $resourceNameMapping
    )

    # $metaDataUri & $cmdletAdapter is already validated at the cmdlet layer.
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "VerifyMetaData") }
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "VerifyMetaData") }
    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "VerifyMetaData") }

    Write-Verbose $LocalizedData.VerboseVerifyingMetadata

    if ($metadata.EntitySets.Count -le 0)
    {
        $errorMessage = ($LocalizedData.NoEntitySets -f $metaDataUri)
        $exception = [System.InvalidOperationException]::new($errorMessage)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetaDataUri" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metaDataUri
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }

    if ($metadata.EntityTypes.Count -le 0)
    {
        $errorMessage = ($LocalizedData.NoEntityTypes -f $metaDataUri)
        $exception = [System.InvalidOperationException]::new($errorMessage)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetaDataUri" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metaDataUri
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }
    
    # All the generated proxy cmdlets would have the following parameters added.
    # The ODataAdapter has the default implementation on how to handle the 
    # scenario when these parameters are used during proxy invocations.
    # The default implementation can be overridden using adapter derivation model. 
    $reservedProperties = @("Filter", "IncludeTotalResponseCount", "OrderBy", "Select", "Skip", "Top", "ConnectionUri", "CertificateThumbprint", "Credential")
    $validEntitySets = @()
    $sessionCommands = Get-Command -All
    
    foreach ($entitySet in $metaData.EntitySets)
    {
        if ($entitySet.Type -eq $null)
        {
            $errorMessage = ($LocalizedData.EntitySetUndefinedType -f $metaDataUri, $entitySet.Name)
            $exception = [System.InvalidOperationException]::new($errorMessage)
            $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidMetaDataUri" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $metaDataUri
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }

        if ($cmdletAdapter -eq "NetworkControllerAdapter" -And $customData -And $customData.Contains($entitySet.Name) -eq $false)
        {
            continue
        }

        $hasConflictingProperty = $false
        $hasConflictingCommand = $false

        $entityAndNavigationProperties = (GetAllProperties $entitySet.Type) + (GetAllProperties $entitySet.Type -IncludeOnlyNavigationProperties)
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
                    $errorRecord = CreateErrorRecordHelper "ODataEndpointDefaultPropertyCollision" $null ([System.Management.Automation.ErrorCategory]::InvalidOperation) $exception $metaDataUri
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
            # The generated command Noun can be set using ResourceNameMapping
            $generatedCommandName = $entitySet.Name
            if ($resourceNameMapping -And $resourceNameMapping.Contains($entitySet.Name)) {
                $generatedCommandName = $resourceNameMapping[$entitySet.Name]
            }

            if(($currentCommand.Noun -ne $null -and $currentCommand.Noun -eq $generatedCommandName) -and 
            ($currentCommand.Verb -eq "Get" -or 
            $currentCommand.Verb -eq "Set" -or 
            $currentCommand.Verb -eq "New" -or 
            $currentCommand.Verb -eq "Remove"))
            {
                $hasConflictingCommand = $true
                VerifyMetadataHelper $LocalizedData.SkipEntitySetConflictCommandCreation `
                $LocalizedData.EntitySetConflictCommandCreationWithWarning `
                $entitySet.Name $currentCommand.Name $metaDataUri $allowClobber $callerPSCmdlet
            }
        }

        foreach($currentAction in $metaData.Actions)
        {
            $actionCommand = "Invoke-" + "$($entitySet.Name)$($currentAction.Verb)"
        
            foreach($currentCommand in $sessionCommands)
            {
                if($actionCommand -eq $currentCommand.Name)
                {
                    $hasConflictingCommand = $true
                    VerifyMetadataHelper $LocalizedData.SkipEntitySetConflictCommandCreation `
                    $LocalizedData.EntitySetConflictCommandCreationWithWarning $entitySet.Name `
                    $currentCommand.Name $metaDataUri $allowClobber $callerPSCmdlet
                }
            }
        }

        if(!($hasConflictingProperty -or $hasConflictingCommand)-or $allowClobber)
        {
            $validEntitySets += $entitySet
        }
    }
    
    if ($cmdletAdapter -ne "NetworkControllerAdapter") {
    
        $metaData.EntitySets = $validEntitySets
    
        $validServiceActions = @()        
        $hasConflictingServiceActionCommand = $true
        foreach($currentAction in $metaData.Actions)
        {
            $serviceActionCommand = "Invoke-" + "$($currentAction.Verb)"
    
            foreach($currentCommand in $sessionCommands)
            {
                if($serviceActionCommand -eq $currentCommand.Name)
                {
                    $hasConflictingServiceActionCommand = $true
                    VerifyMetadataHelper $LocalizedData.SkipConflictServiceActionCommandCreation `
                    $LocalizedData.ConflictServiceActionCommandCreationWithWarning $entitySet.Name `
                    $currentCommand.Name $metaDataUri $allowClobber $callerPSCmdlet
                }
            }
    
            if(!$hasConflictingServiceActionCommand -or $allowClobber)
            {
                $validServiceActions += $currentAction
            }
        }
    
        $metaData.Actions = $validServiceActions
    }
    
    # Update Progress bar.
    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 5 20 1  1
}

#########################################################
# GenerateClientSideProxyModule is a helper function used 
# to generate a PowerShell module that serves as a client
# side proxy for interacting with the server side 
# OData endpoint. The proxy module contains proxy cmdlets
# implemented in CDXML modules and they are exposed 
# through module manifest as nested modules.
# One CDXML module is created for each EntitySet 
# described in the metadata. Each CDXML module contains
# CRUD & Service Action specific proxy cmdlets targeting
# the underlying EntityType. There is 1:M mapping between
# EntitySet & its underlying EntityTypes (i.e., all
# entities with in the single EntitySet will be of the
# same EntityType but there can be multiple entities 
# of the same type with in an EntitySet).    
#########################################################
function GenerateClientSideProxyModule 
{
    param
    (
        [ODataUtils.Metadata] $metaData,
        [string]    $metaDataUri,
        [string]    $uri,
        [string]    $outputModule,
        [string]    $createRequestMethod,
        [string]    $updateRequestMethod,    
        [string]    $cmdletAdapter,   
        [Hashtable] $resourceNameMapping,  
        [Hashtable] $customData,
        [string]    $progressBarStatus,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    # $uri, $outputModule, $metaDataUri, $createRequestMethod, $updateRequestMethod, & $cmdletAdapter is already validated at the cmdlet layer.
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateClientSideProxyModule") }
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "GenerateClientSideProxyModule") }
    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "GenerateClientSideProxyModule") }

    # This function performs the following set of tasks 
    # while creating the client side proxy module:
    # 1. If the server side endpoint exposes complex types,
    #    the client side proxy complex types are created
    #    as C# class in ComplexTypeDefinitions.psm1 
    # 2. Creates proxy cmdlets for CRUD operations.
    # 3. Creates proxy cmdlets for Service action operations.
    # 4. Creates module manifest.

    Write-Verbose ($LocalizedData.VerboseSavingModule -f $outputModule)

    $typeDefinitionFileName = "ComplexTypeDefinitions.psm1"
    $complexTypeMapping = GenerateComplexTypeDefinition $metaData $metaDataUri $outputModule $typeDefinitionFileName $cmdletAdapter $callerPSCmdlet

    ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 20 20 1  1

    $complexTypeFileDefinitionPath = Join-Path -Path $outputModule -ChildPath $typeDefinitionFileName

    if(Test-Path -Path $complexTypeFileDefinitionPath)
    {
        $proxyFile = New-Object -TypeName System.IO.FileInfo -ArgumentList $complexTypeFileDefinitionPath | Get-Item
        if($callerPSCmdlet -ne $null)
        { 
            $callerPSCmdlet.WriteObject($proxyFile)
        }
    }
    
    $currentEntryCount = 0
    foreach ($entitySet in $metaData.EntitySets)
    {
        $currentEntryCount += 1
        if ($cmdletAdapter -eq "NetworkControllerAdapter" -And $customData -And $customData.Contains($entitySet.Name) -eq $false)
        {
            ProgressBarHelper "Export-ODataEndpointProxy" $progressBarStatus 40 20 $metaData.EntitySets.Count  $currentEntryCount
            continue
        }
         
        GenerateCRUDProxyCmdlet $entitySet $metaData $uri $outputModule $createRequestMethod $updateRequestMethod $cmdletAdapter $resourceNameMapping $customData $complexTypeMapping "Export-ODataEndpointProxy" $progressBarStatus 40 20 $metaData.EntitySets.Count $currentEntryCount $callerPSCmdlet
    }

    GenerateServiceActionProxyCmdlet $metaData $uri "$outputModule\ServiceActions.cdxml" $complexTypeMapping $progressBarStatus $callerPSCmdlet

    $moduleDirInfo = [System.IO.DirectoryInfo]::new($outputModule)
    $moduleManifestName = $moduleDirInfo.Name + ".psd1"
    GenerateModuleManifest $metaData $outputModule\$moduleManifestName @($typeDefinitionFileName, 'ServiceActions.cdxml') $resourceNameMapping $progressBarStatus $callerPSCmdlet
}

#########################################################
# GenerateCRUDProxyCmdlet is a helper function used 
# to generate Get, Set, New & Remove proxy cmdlet. 
# The proxy cmdlet is generated in the CDXML 
# compliant format. 
#########################################################
function GenerateCRUDProxyCmdlet 
{
    param
    (
        [ODataUtils.EntitySet] $entitySet,
        [ODataUtils.Metadata] $metaData,
        [string] $uri,
        [string] $outputModule,
        [string] $createRequestMethod,
        [string] $UpdateRequestMethod,
        [string] $cmdletAdapter,
        [Hashtable] $resourceNameMapping,  
        [Hashtable] $customData,
        [Hashtable] $complexTypeMapping,
        [string] $progressBarActivityName,
        [string] $progressBarStatus,
        [double] $previousSegmentWeight,
        [double] $currentSegmentWeight,
        [int] $totalNumberofEntries,
        [int] $currentEntryCount,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    # $uri, $outputModule, $metaDataUri, $createRequestMethod, $updateRequestMethod, & $cmdletAdapter is already validated at the cmdlet layer.
    if($entitySet -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntitySet", "GenerateClientSideProxyModule") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateClientSideProxyModule") }
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "GenerateClientSideProxyModule") }
    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "GenerateClientSideProxyModule") }

    $entitySetName = $entitySet.Name 
    if(($resourceNameMapping -ne $null) -and 
    $resourceNameMapping.ContainsKey($entitySetName))
    {
        $entitySetName = $resourceNameMapping[$entitySetName]
    }
    else
    {
        $entitySetName = $entitySet.Type.Name
    }

    $Path = "$OutputModule\$entitySetName.cdxml"

    $xmlWriter = New-Object System.XMl.XmlTextWriter($Path,$Null)

    if ($xmlWriter -eq $null)
    {
        throw ($LocalizedData.XmlWriterInitializationError -f $entitySet.Name)
    }

    $xmlWriter = SaveCDXMLHeader $xmlWriter $uri $entitySet.Name $entitySetName $cmdletAdapter

    # Get the keys depending on whether the url contains variables or not
    if ($CmdletAdapter -ne "NetworkControllerAdapter")
    {
        $keys = (GetAllProperties $entitySet.Type) | Where-Object { $_.IsKey }
    }
    else
    {
        $name = $entitySet.Name
    	$keys = GetKeys $entitySet $customData.$name 'Get'
    }

    $navigationProperties = GetAllProperties $entitySet.Type -IncludeOnlyNavigationProperties

    GenerateGetProxyCmdlet $xmlWriter $metaData $keys $navigationProperties $cmdletAdapter $complexTypeMapping

    $nonKeyProperties = (GetAllProperties $entitySet.Type) | ? { -not $_.isKey }
    $nullableProperties = $nonKeyProperties | ? { $_.isNullable }
    $nonNullableProperties = $nonKeyProperties | ? { -not $_.isNullable }

    $xmlWriter.WriteStartElement('StaticCmdlets')

        $keyProperties = $keys

        # Do operations specifically needed for NetworkController cmdlets
        if ($CmdletAdapter -eq "NetworkControllerAdapter")
        {
    	    $keyProperties = GetKeys $entitySet $customData.$name 'New'
            $additionalProperties = GetNetworkControllerAdditionalProperties $navigationProperties $metaData
            $nullableProperties = UpdateNetworkControllerSpecificProperties $nullableProperties $additionalProperties $keyProperties $true
            $nonNullableProperties = UpdateNetworkControllerSpecificProperties $nonNullableProperties $additionalProperties $keyProperties $false
        }

        GenerateNewProxyCmdlet $xmlWriter $metaData $keyProperties $nonNullableProperties $nullableProperties $navigationProperties $cmdletAdapter $complexTypeMapping

        if ($CmdletAdapter -ne "NetworkControllerAdapter")
        {
            GenerateSetProxyCmdlet $xmlWriter $keyProperties $nonKeyProperties $complexTypeMapping
        }

        if ($CmdletAdapter -eq "NetworkControllerAdapter")
        {
    	    $keyProperties = GetKeys $entitySet $customData.$name 'Remove'
        }

        GenerateRemoveProxyCmdlet $xmlWriter $metaData $keyProperties $navigationProperties $cmdletAdapter $complexTypeMapping

        $entityActions = $metaData.Actions | Where-Object { ($_.EntitySet.Namespace -eq $entitySet.Namespace) -and ($_.EntitySet.Name -eq $entitySet.Name) }

        if ($entityActions.Length -gt 0)
        {
            foreach($action in $entityActions)
            {
                $xmlWriter = GenerateActionProxyCmdlet $xmlWriter $metaData $action $entitySet.Name $true $keys $complexTypeMapping
            }
        }

    $xmlWriter.WriteEndElement()

    $xmlWriter.WriteStartElement('CmdletAdapterPrivateData')

        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'EntityTypeName')
        $xmlWriter.WriteString("$($entitySet.Type.Namespace).$($entitySet.Type.Name)")
        $xmlWriter.WriteEndElement()
        $xmlWriter.WriteStartElement('Data')
        $xmlWriter.WriteAttributeString('Name', 'EntitySetName')
        $xmlWriter.WriteString("$($entitySet.Namespace).$($entitySet.Name)")
        $xmlWriter.WriteEndElement()

        # Add the customUri to privateData
        if ($CmdletAdapter -eq "NetworkControllerAdapter")
        {
            $xmlWriter.WriteStartElement('Data')
            $xmlWriter.WriteAttributeString('Name', "CustomUriSuffix")
            $xmlWriter.WriteString($CustomData.$name)
            $xmlWriter.WriteEndElement()
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

    ProcessStreamHelper ($LocalizedData.VerboseSavedCDXML -f $($entitySetName), $Path) $progressBarActivityName $progressBarStatus $previousSegmentWeight $currentSegmentWeight $totalNumberofEntries $currentEntryCount $Path $callerPSCmdlet
}

#########################################################
# GenerateGetProxyCmdlet is a helper function used 
# to generate Get-* proxy cmdlet. The proxy cmdlet is
# generated in the CDXML compliant format. 
#########################################################
function GenerateGetProxyCmdlet 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.Metadata] $metaData, 
        [object[]]  $keys,
        [object[]]  $navigationProperties,
        [string]    $cmdletAdapter,
        [Hashtable] $complexTypeMapping
    )
    
    # $cmdletAdapter is already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "GenerateGetProxyCmdlet") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateGetProxyCmdlet") }

    $xmlWriter.WriteStartElement('InstanceCmdlets')
        $xmlWriter.WriteStartElement('GetCmdletParameters')
            $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', 'Default')

            # adding key parameters and association parameters to QueryableProperties, each in a different parameter set
            # to be used by GET cmdlet
            if (($keys -ne $null -and $keys.Length -gt 0) -or (($navigationProperties -ne $null -and $navigationProperties.Length -gt 0) -and $cmdletAdapter -ne "NetworkControllerAdapter"))
            {
                $xmlWriter.WriteStartElement('QueryableProperties')
                $position = 0
                
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
                            if($_.IsMandatory)
                            {
                                $xmlWriter.WriteAttributeString('ValueFromPipelineByPropertyName', 'true')
                            }
                            $xmlWriter.WriteEndElement()
                        $xmlWriter.WriteEndElement()
                    $xmlWriter.WriteEndElement()

                    $position++
                }

                # This behaviour is different for NetworkController specific cmdlets.
                if ($CmdletAdapter -ne "NetworkControllerAdapter")
                {
                    $navigationProperties | ? { $_ -ne $null } | % {
                    $associatedType = GetAssociatedType $metaData $_
                    $associatedEntitySet = GetEntitySetForEntityType $metaData $associatedType
                    $nvgProperty = $_

                        (GetAllProperties $associatedType)  | ? { $_.IsKey } | % {
                            $xmlWriter.WriteStartElement('Property')
                            $xmlWriter.WriteAttributeString('PropertyName', $associatedEntitySet.Name + ':' + $_.Name + ':Key')

                                $xmlWriter.WriteStartElement('Type')
                                $PSTypeName = Convert-ODataTypeToCLRType $_.TypeName $complexTypeMapping
                                $xmlWriter.WriteAttributeString('PSType', $PSTypeName)
                                $xmlWriter.WriteEndElement()

                                $xmlWriter.WriteStartElement('RegularQuery')
                                    $xmlWriter.WriteStartElement('CmdletParameterMetadata')
                                    $xmlWriter.WriteAttributeString('PSName', 'Associated' + $nvgProperty.Name + $_.Name)
                                    $xmlWriter.WriteAttributeString('CmdletParameterSets', $nvgProperty.AssociationName)
                                    $xmlWriter.WriteAttributeString('IsMandatory', 'true')
                                    $xmlWriter.WriteAttributeString('ValueFromPipelineByPropertyName', 'true')
                                    $xmlWriter.WriteEndElement()
                                $xmlWriter.WriteEndElement()
                            $xmlWriter.WriteEndElement()
                        }
                    }
                    

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
                            $minValue = 1
                            # For Skip Query parameter we want to support 0 as the 
                            # minimum skip value in order to support client side paging.
                            if($currentQueryParameter -eq 'Skip')
                            {
                                $minValue = 0
                            }
                            $xmlWriter.WriteStartElement('ValidateRange')
                            $xmlWriter.WriteAttributeString('Min', $minValue)
                            $xmlWriter.WriteAttributeString('Max', [int]::MaxValue)
                            $xmlWriter.WriteEndElement()
                        }

                        $xmlWriter.WriteEndElement()
                        $xmlWriter.WriteEndElement()
                        $xmlWriter.WriteEndElement()
                    }
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

#########################################################
# GenerateNewProxyCmdlet is a helper function used 
# to generate New-* proxy cmdlet. The proxy cmdlet is
# generated in the CDXML compliant format. 
#########################################################
function GenerateNewProxyCmdlet 
{
    param
    (
        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.Metadata] $metaData,
        [object[]]  $keyProperties,
        [object[]]  $nonNullableProperties,
        [object[]]  $nullableProperties,
        [object[]]  $navigationProperties,
        [string]    $cmdletAdapter,
        [Hashtable] $complexTypeMapping
    )

    # $cmdletAdapter is already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "GenerateNewProxyCmdlet") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateNewProxyCmdlet") }

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

        # This behaviour is different for NetworkControllerCmdlets
        if ($CmdletAdapter -ne "NetworkControllerAdapter")
        {
            $navigationProperties | ? { $_ -ne $null } | % {
                $associatedType = GetAssociatedType $metaData $_
                $associatedEntitySet = GetEntitySetForEntityType $metaData $associatedType

                $xmlWriter.WriteStartElement('Method')
                $xmlWriter.WriteAttributeString('MethodName', "Association:Create:$($associatedEntitySet.Name)")
                $xmlWriter.WriteAttributeString('CmdletParameterSet', $_.Name)
                    
                $associatedKeys = ((GetAllProperties $associatedType) | ? { $_.isKey })

                AddParametersNode $xmlWriter $associatedKeys $keyProperties $null "Associated$($_.Name)" $true $true $complexTypeMapping
                $xmlWriter.WriteEndElement()
            }
        }
    
        $xmlWriter.WriteEndElement()
}

#########################################################
# GenerateRemoveProxyCmdlet is a helper function used 
# to generate Remove-* proxy cmdlet. The proxy cmdlet is
# generated in the CDXML compliant format. 
#########################################################
function GenerateRemoveProxyCmdlet 
{
    param
    (

        [System.XMl.XmlTextWriter] $xmlWriter,
        [ODataUtils.Metadata] $metaData,
        [object[]] $keyProperties,
        [object[]] $navigationProperties,
        [string] $cmdletAdapter,
        [Hashtable] $complexTypeMapping
    )

    # $metaData, $cmdletAdapter & $cmdletAdapter are already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "GenerateRemoveProxyCmdlet") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateRemoveProxyCmdlet") }

    $xmlWriter.WriteStartElement('Cmdlet')
        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Remove')
        $xmlWriter.WriteAttributeString('DefaultCmdletParameterSet', 'Default')
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', 'Delete')
        $xmlWriter.WriteAttributeString('CmdletParameterSet', 'Default')

            # This behaviour is different for NetworkControllerCmdlets
            if ($CmdletAdapter -eq "NetworkControllerAdapter")
            {
                # Add etag for NetworkControllerCmdlets
                $otherProperties = @([ODataUtils.TypeProperty] @{
                    "Name" = "Etag";
                    "TypeName" = "Edm.String";
                    "IsNullable" = $true;
                })

                AddParametersNode $xmlWriter $keyProperties $null $otherProperties $null $true $true $complexTypeMapping
            }
            else
            {
                AddParametersNode $xmlWriter $keyProperties $null $null $null $true $true $complexTypeMapping
            }

        $xmlWriter.WriteEndElement()

        # This behaviour is different for NetworkControllerCmdlets
        if ($CmdletAdapter -ne "NetworkControllerAdapter")
        {
            $navigationProperties | ? { $_ -ne $null } | % {

                $associatedType = GetAssociatedType $metaData $_
                $associatedEntitySet = GetEntitySetForEntityType $metaData $associatedType

                $xmlWriter.WriteStartElement('Method')
                $xmlWriter.WriteAttributeString('MethodName', "Association:Delete:$($associatedEntitySet.Name)")
                $xmlWriter.WriteAttributeString('CmdletParameterSet', $_.Name)
                
                    $associatedType = GetAssociatedType $metaData $_
                    $associatedKeys = ((GetAllProperties $associatedType) | ? { $_.isKey })

                AddParametersNode $xmlWriter $associatedKeys $keyProperties $null "Associated$($_.Name)" $true $true $complexTypeMapping
                $xmlWriter.WriteEndElement()
            }
        }
    $xmlWriter.WriteEndElement()
}

#########################################################
# GenerateActionProxyCmdlet is a helper function used 
# to generate Invoke-* proxy cmdlet. These proxy cmdlets
# support Instance/Service level actions. They are 
# generated in the CDXML compliant format. 
#########################################################
function GenerateActionProxyCmdlet 
{
    param
    (
        [System.Xml.XmlWriter]    $xmlWriter,
        [ODataUtils.Metadata]     $metaData,
        [ODataUtils.Action]       $action,
        [string]                  $noun,
        [bool]                    $isInstanceAction,
        [ODataUtils.TypeProperty] $keys,
        [Hashtable]               $complexTypeMapping
    )

    # $metaData is already validated at the cmdlet layer.
    if($xmlWriter -eq $null) { throw ($LocalizedData.ArguementNullError -f "xmlWriter", "GenerateActionProxyCmdlet") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateActionProxyCmdlet") }
    if($action -eq $null) { throw ($LocalizedData.ArguementNullError -f "Action", "GenerateActionProxyCmdlet") }
    if($noun -eq $null) { throw ($LocalizedData.ArguementNullError -f "Noun", "GenerateActionProxyCmdlet") }

    $xmlWriter.WriteStartElement('Cmdlet')

        $xmlWriter.WriteStartElement('CmdletMetadata')
        $xmlWriter.WriteAttributeString('Verb', 'Invoke')
        $xmlWriter.WriteAttributeString('Noun', "$($noun)$($action.Verb)")
        $xmlWriter.WriteAttributeString('ConfirmImpact', 'Medium')
        $xmlWriter.WriteEndElement()

        $xmlWriter.WriteStartElement('Method')
        $xmlWriter.WriteAttributeString('MethodName', "Action:$($action.Verb):$($action.EntitySet.Name)")

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
                    $xmlWriter.WriteAttributeString('ValueFromPipelineByPropertyName', 'true')
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
                        $xmlWriter.WriteAttributeString('ValueFromPipelineByPropertyName', 'true')
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
# GenerateServiceActionProxyCmdlet is a helper function 
# used to generate Invoke-* proxy cmdlet. These proxy 
# cmdlets support all Service-level actions. They are 
# generated in the CDXML compliant format. 
#########################################################
function GenerateServiceActionProxyCmdlet 
{
    param
    (
        [Parameter(Mandatory=$true)]
        [ODataUtils.Metadata] $metaData,
        [Parameter(Mandatory=$true)]
        [string] $uri,
        [Parameter(Mandatory=$true)]
        [string] $path,
        [Hashtable] $complexTypeMapping,
        [string] $progressBarStatus,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    # $uri is already validated at the cmdlet layer.
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateServiceActionProxyCmdlet") }

    $xmlWriter = New-Object System.XMl.XmlTextWriter($path,$Null)

    if ($xmlWriter -eq $null)
    {
        throw $LocalizedData.XmlWriterInitializationError -f "ServiceActions"
    }

    $xmlWriter = SaveCDXMLHeader $xmlWriter $uri 'ServiceActions' 'ServiceActions'

    $actions = $metaData.Actions | Where-Object { $_.EntitySet -eq $null }

    if ($actions.Length -gt 0)
    {
        $xmlWriter.WriteStartElement('StaticCmdlets')

        foreach ($action in $actions)
        {
            $xmlWriter = GenerateActionProxyCmdlet $xmlWriter $metaData $action '' $false $null $complexTypeMapping
        }

        $xmlWriter.WriteEndElement()
    }

    $xmlWriter.WriteStartElement('CmdletAdapterPrivateData')
    $xmlWriter.WriteStartElement('Data')
    $xmlWriter.WriteAttributeString('Name', 'Namespace')
    $xmlWriter.WriteString("$($EntitySet.Namespace)")
    $xmlWriter.WriteEndElement()
    $xmlWriter.WriteEndElement()

    SaveCDXMLFooter $xmlWriter

    ProcessStreamHelper ($LocalizedData.VerboseSavedServiceActions -f $path) "Export-ODataEndpointProxy" $progressBarStatus 60 20 1 1 $path $callerPSCmdlet
}

#########################################################
# GenerateModuleManifest is a helper function used 
# to generate a wrapper module manifest file. The
# generated module manifest is persisted to the disk at
# the specified OutPutModule path. When the module 
# manifest is imported, the following commands will 
# be imported:
# 1. Get, Set, New & Remove proxy cmdlets.
# 2. If the server side Odata endpoint exposes complex
#    types, then the corresponding client side proxy
#    complex types imported.
# 3. Service Action proxy cmdlets.   
#########################################################
function GenerateModuleManifest 
{
    param
    (
        [ODataUtils.Metadata] $metaData,
        [String]              $modulePath,
        [string[]]            $additionalModules,
        [Hashtable]           $resourceNameMapping,
        [string]              $progressBarStatus,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateModuleManifest") }
    if($modulePath -eq $null) { throw ($LocalizedData.ArguementNullError -f "ModulePath", "GenerateModuleManifest") }
    if($progressBarStatus -eq $null) { throw ($LocalizedData.ArguementNullError -f "ProgressBarStatus", "GenerateModuleManifest") }

    $NestedModules = @()
    foreach ($entitySet in $metaData.EntitySets)
    {
        $entitySetName = $entitySet.Name 
        if(($resourceNameMapping -ne $null) -and 
        $resourceNameMapping.ContainsKey($entitySetName))
        {
            $entitySetName = $resourceNameMapping[$entitySetName]
        }
        else
        {
            $entitySetName = $entitySet.Type.Name
        }

        $NestedModules += "$OutputModule\$($entitySetName).cdxml"
    }

    New-ModuleManifest -Path $modulePath -NestedModules ($AdditionalModules + $NestedModules)

    ProcessStreamHelper ($LocalizedData.VerboseSavedModuleManifest -f $modulePath) "Export-ODataEndpointProxy" $progressBarStatus 80 20 1 1 $modulePath $callerPSCmdlet
}

#########################################################
# GetBaseType is a helper function used to fetch the 
# base type of the given type. 
#########################################################
function GetBaseType 
{
    param
    (
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.Metadata] $metaData
    )

    if ($metadataEntityDefinition -ne $null -and 
    $metaData -ne $null -and 
    $metadataEntityDefinition.BaseType -ne $null)
    {
        $baseType = $metaData.EntityTypes | Where {$_.Namespace+"."+$_.Name -eq $metadataEntityDefinition.BaseType}
        if ($baseType -eq $null)
        {
            $baseType = $metaData.ComplexTypes | Where {$_.Namespace+"."+$_.Name -eq $metadataEntityDefinition.BaseType}
        }
    }

    if ($baseType -ne $null)
    {
        $baseType[0]
    }
}

#########################################################
# AddDerivedTypes is a helper function used to process
# derived types of a newly added type, that were 
# previously waiting in the queue.
#########################################################
function AddDerivedTypes 
{
    param
    (
        [ODataUtils.EntityType] $baseType,
        [Hashtable]$entityAndComplexTypesQueue,    
        [ODataUtils.Metadata] $metaData,
        [string] $namespace
    )

    # $metaData is already validated at the cmdlet layer.
    if($baseType -eq $null) { throw ($LocalizedData.ArguementNullError -f "BaseType", "AddDerivedTypes") }
    if($entityAndComplexTypesQueue -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntityAndComplexTypesQueue", "AddDerivedTypes") }
    if($namespace -eq $null) { throw ($LocalizedData.ArguementNullError -f "Namespace", "AddDerivedTypes") }

    $baseTypeFullName = $baseType.Namespace + '.' + $baseType.Name

    if ($entityAndComplexTypesQueue.ContainsKey($baseTypeFullName))
    {
        foreach ($type in $entityAndComplexTypesQueue[$baseTypeFullName])
        {
            if ($type.type -eq 'EntityType')
            {
                $newType = ParseMetadataTypeDefinition ($type.value) $baseType $metaData $namespace $true
                $metaData.EntityTypes += $newType
            }
            else
            {
                $newType = ParseMetadataTypeDefinition ($type.value) $baseType $metaData $namespace $false
                $metaData.ComplexTypes += $newType
            }

            AddDerivedTypes $newType $entityAndComplexTypesQueue $metaData $namespace
        }
    }
}

#########################################################
# ParseMetadataTypeDefinition is a helper function used 
# to parse types definitions element of metadata xml.
#########################################################
function ParseMetadataTypeDefinition 
{
    param
    (
        [Parameter(Mandatory=$true)]
        [System.Xml.XmlElement] $metadataEntityDefinition,
        [ODataUtils.EntityType] $baseType,
        [ODataUtils.Metadata] $metaData,
        [string] $namespace,
        [bool] $isEntity
    )

    # $metaData is already validated at the cmdlet layer.
    if($metadataEntityDefinition -eq $null) { throw ($LocalizedData.ArguementNullError -f "MetadataEntityDefinition", "ParseMetadataTypeDefinition") }
    if($namespace -eq $null) { throw ($LocalizedData.ArguementNullError -f "Namespace", "ParseMetadataTypeDefinition") }

    $newEntityType = [ODataUtils.EntityType] @{
        "Namespace" = $namespace;
        "Name" = $metadataEntityDefinition.Name;
        "IsEntity" = $isEntity;
        "BaseType" = $baseType;
    }

    # properties defined on EntityType
    $newEntityType.EntityProperties = $metadataEntityDefinition.Property | % {
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

    # navigation properties defined on EntityType
    $newEntityType.NavigationProperties = $metadataEntityDefinition.NavigationProperty | % {
        if ($_ -ne $null)
        {
            ($AssociationNamespace, $AssociationName) = SplitNamespaceAndName $_.Relationship
            [ODataUtils.NavigationProperty] @{
                "Name" = $_.Name;
                "FromRole" = $_.FromRole;
                "ToRole" = $_.ToRole;
                "AssociationNamespace" = $AssociationNamespace;
                "AssociationName" = $AssociationName;
            }
        }
    }

    foreach ($entityTypeKey in $metadataEntityDefinition.Key.PropertyRef)
    {
        ((GetAllProperties $newEntityType) | Where-Object { $_.Name -eq $entityTypeKey.Name }).IsKey = $true
    }

    $newEntityType
}

#########################################################
# GetAllProperties is a helper function used to fetch 
# the entity properties or navigation properties of 
# the entity type as well as that of complete base 
# type hierarchy.
#########################################################
function GetAllProperties 
{
    param
    (
        [ODataUtils.EntityType] $entityType,
        [switch] $IncludeOnlyNavigationProperties 
    )

    if($entityType -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntityType", "GetAllProperties") }

    $requestedProperties = @()

    # Populate EntityType property from current EntityType as well 
    # as from the corresponding base types recursively if 
    # $IncludeOnlyNavigationProperties switch parameter is used then follow
    # the same routine for navigation properties. 
    $currentEntityType = $entityType
    while($currentEntityType -ne $null)
    {
        if($IncludeOnlyNavigationProperties.IsPresent)
        {
            $chosenProperties = $currentEntityType.NavigationProperties
        }
        else
        {
            $chosenProperties = $currentEntityType.EntityProperties
        }

        $requestedProperties += $chosenProperties
        $currentEntityType = $currentEntityType.BaseType
    }

    return $requestedProperties
}

#########################################################
# SplitNamespaceAndName is a helper function used 
# to split Namespace and actual Name.
# e.g. "a.b.c" is namespace "a.b" and name "c"
#########################################################
function SplitNamespaceAndName 
{
    param
    (
        [string] $fullyQualifiedName
    )

    if($fullyQualifiedName -eq $null) { throw ($LocalizedData.ArguementNullError -f "FUllyQualifiedName", "SplitNamespaceAndName") }

    $sa = $fullyQualifiedName -split "(.*)\.(.*)"

    if ($sa.Length -gt 1)
    {
        # return Namespace
        $sa[1]

        # return Name
        $sa[2]
    }
    else
    {
        # return Namespace
        ""

        # return Name
        $sa[0]
    }
}

#########################################################
# GetEntitySetForEntityType is a helper function used 
# to fetch EntitySet for a given EntityType by 
# searching the inheritance hierarchy in the 
# supplied metadata.
#########################################################
function GetEntitySetForEntityType 
{
    param
    (
        [ODataUtils.Metadata] $metaData,
        [ODataUtils.EntityType] $entityType
    )

    # $metaData is already validated at the cmdlet layer.
    if($entityType -eq $null) { throw ($LocalizedData.ArguementNullError -f "EntityType", "GetEntitySetForEntityType") }

    $result = $metaData.EntitySets | ? { ($_.Type.Namespace -eq $entityType.Namespace) -and ($_.Type.Name -eq $entityType.Name) }

    if (($result.Count -eq 0) -and ($entityType.BaseType -ne $null))
    {
        GetEntitySetForEntityType $metaData $entityType.BaseType
    }
    elseif ($result.Count -gt 1)
    {
        throw ($LocalizedData.WrongCountEntitySet -f (($entityType.Namespace + "." + $entityType.Name), $result.Count))
    }

    $result
}

#########################################################
# ProcessStreamHelper is a helper function that performs 
# the following utility tasks:
# 1. Writes verbose messages to the stream.
# 2. Writes FileInfo objects for the proxy modules 
#    saved to the disk. This is done to keep the user 
#    experience in consistent with Export-PSSession.
# 3. Updates progress bar.
#########################################################
function ProcessStreamHelper 
{
    param
    (
        [string] $verboseMessage,
        [string] $progressBarActivityName,
        [string] $status,
        [double] $previousSegmentWeight,
        [double] $currentSegmentWeight,
        [int]    $totalNumberofEntries,
        [int]    $currentEntryCount,
        [string] $path,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    Write-Verbose -Message $verboseMessage
    ProgressBarHelper $progressBarActivityName $status $previousSegmentWeight $currentSegmentWeight $totalNumberofEntries $currentEntryCount
    $proxyFile = New-Object -TypeName System.IO.FileInfo -ArgumentList $path | Get-Item
    if($callerPSCmdlet -ne $null)
    {
        $callerPSCmdlet.WriteObject($proxyFile)
    }
}

#########################################################
# GetAssociatedType is a helper function used 
# to fetch associated instance's EntityType 
# for a given Navigation property in the 
# supplied metadata.
#########################################################
function GetAssociatedType 
{
    param
    (
        [ODataUtils.Metadata] $Metadata,
        [ODataUtils.NavigationProperty] $navProperty
    )

    # $metaData is already validated at the cmdlet layer.
    if($navProperty -eq $null) { throw ($LocalizedData.ArguementNullError -f "NavigationProperty", "GetAssociatedType") }

    $associationName = $navProperty.AssociationName
    $association = $Metadata.Associations | ? { $_.Name -eq $associationName }
    $associationType = $association.Type

    if ($associationType.Count -lt 1)
    {
        throw ($LocalizedData.AssociationNotFound -f $associationName)
    }
    elseif ($associationType.Count -gt 1)
    {
        throw ($LocalizedData.TooManyMatchingAssociationTypes -f $associationType.Count, $associationName)
    }

    if ($associationType.NavPropertyName1 -eq $navProperty.ToRole)
    {
        $associatedType = $associationType.EndType1
    }
    elseif ($associationType.NavPropertyName2 -eq $navProperty.ToRole)
    {
        $associatedType = $associationType.EndType2
    }
    else
    {
        throw ($LocalizedData.ZeroMatchingAssociationTypes -f $navProperty.ToRole, $association.Name)
    }

    # return associated EntityType
    $associatedType
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
# AddParametersNode is a helper function used 
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
# GenerateComplexTypeDefinition is a helper function used 
# to generate complex type definition from the metadata.
#########################################################
function GenerateComplexTypeDefinition 
{
    param
    (
        [ODataUtils.Metadata] $metaData,
        [string] $metaDataUri,
        [string] $OutputModule,
        [string] $typeDefinitionFileName,
        [string] $cmdletAdapter,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    #metadataUri, $OutputModule & $cmdletAdapter are already validated at the cmdlet layer.
    if($typeDefinationFileName -eq $null) { throw ($LocalizedData.ArguementNullError -f "TypeDefinationFileName", "GenerateComplexTypeDefination") }
    if($metaData -eq $null) { throw ($LocalizedData.ArguementNullError -f "metadata", "GenerateComplexTypeDefination") }
    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "GenerateComplexTypeDefination") }

    $Path = "$OutputModule\$typeDefinitionFileName"

    # We are currently generating classes for EntityType & ComplexType 
    # definition exposed in the metadata.
    $typesToBeGenerated = $metaData.EntityTypes+$metadata.ComplexTypes

    if($typesToBeGenerated -ne $null -and $typesToBeGenerated.Count -gt 0)
    {
        $complexTypeMapping = @{}
        $entityTypeNameSpaceMapping = @{}

        foreach ($entityType in $typesToBeGenerated)
        {
            if ($entityType -ne $null)
            {
                $entityTypeFullName = $entityType.Namespace + '.' + $entityType.Name
                if(!$complexTypeMapping.ContainsKey($entityTypeFullName))
                {
                    $complexTypeMapping.Add($entityTypeFullName, $entityType.Name)
                }

                if(!$entityTypeNameSpaceMapping.ContainsKey($entityType.Namespace))
                {
                    $entityTypes = @()
                    $entityTypeNameSpaceMapping.Add($entityType.Namespace, $entityTypes)
                }

                $entityTypeNameSpaceMapping[$entityType.Namespace] += $entityType
            }
        }

        if($entityTypeNameSpaceMapping.Count -gt 0)
        {
$output = @"
`$typeDefinitions = @"
using System;
using System.Management.Automation;

"@

            foreach($currentNameSpace in $entityTypeNameSpaceMapping.Keys)
            {
                $entityTypes = $entityTypeNameSpaceMapping[$currentNameSpace]

                $output += "`r`nnamespace $(ValidateComplexTypeIdentifier $currentNameSpace $true $metaDataUri $callerPSCmdlet)`r`n{"
                
                foreach ($entityType in $entityTypes)
                {
                    $entityTypeFullName = (ValidateComplexTypeIdentifier $entityType.Namespace $true $metaDataUri $callerPSCmdlet) + '.' + $entityType.Name
                    Write-Verbose ($LocalizedData.VerboseAddingTypeDefinationToGeneratedModule -f $entityTypeFullName, "$OutputModule\$typeDefinationFileName")

                    if($entityType.BaseType -ne $null)
                    {
                        $entityBaseFullName = (ValidateComplexTypeIdentifier $entityType.BaseType.Namespace $true $metaDataUri $callerPSCmdlet) + '.' + (ValidateComplexTypeIdentifier $entityType.BaseType.Name $false $metaDataUri $callerPSCmdlet)
                        $output += "`r`n  public class $(ValidateComplexTypeIdentifier $entityType.Name $false $metaDataUri $callerPSCmdlet) : $($entityBaseFullName)`r`n  {"
                    }
                    else
                    {
                        $output += "`r`n  public class $(ValidateComplexTypeIdentifier $entityType.Name $false $metaDataUri $callerPSCmdlet)`r`n  {"
                    }

                    $properties = $null

                    for($index = 0; $index -lt $entityType.EntityProperties.Count; $index++)
                    {
                        $property = $entityType.EntityProperties[$index]
                        $typeName = Convert-ODataTypeToCLRType $property.TypeName $complexTypeMapping
                        $properties += "`r`n     public $typeName  $(ValidateComplexTypeIdentifier $property.Name $false $metaDataUri $callerPSCmdlet);"
                    }

                    # Navigation properties are treated like any other property for NetworkController scenario.
                    if ($cmdletAdapter -eq "NetworkControllerAdapter")
                    {
                        for($index = 0; $index -lt $entityType.NavigationProperties.Count; $index++)
                        {
                            $property = $entityType.NavigationProperties[$index]
                            $navigationTypeName = GetNavigationPropertyTypeName $property $metaData
                            $typeName = Convert-ODataTypeToCLRType $navigationTypeName $complexTypeMapping
                            $properties += "`r`n     public $typeName  $(ValidateComplexTypeIdentifier $property.Name $false $metaDataUri $callerPSCmdlet);"
                        }           
                    }

                    $output += $properties
                    $output += "`r`n  }`r`n"
                }

                $output += "}`r`n"
            }
            $output += """@`r`n"

            $output += "Add-Type -TypeDefinition `$typeDefinitions `r`n"
            $output | Out-File -FilePath $Path
            Write-Verbose ($LocalizedData.VerboseSavedTypeDefinationModule -f $typeDefinationFileName, $OutputModule)
        }
     }

     return $complexTypeMapping
}

# Creating a single instance of CSharpCodeProvider that would be used 
# for Identifier validation in the ValidateComplexTypeIdentifier helper method.
$cSharpCodeProvider = [Microsoft.CSharp.CSharpCodeProvider]::new()

#########################################################
# ValidateComplexTypeIdentifier is a helper function to 
# make sure that the type names defined in the 
# metadata are valid C# Identifier names. This validation 
# is performed to make sure that there are no security 
# threat from importing the generated complex type 
# (which is created using the metadata file).
# This method return the identifier name if its a 
# valid identifier, else a terminating error in thrown. 
#########################################################
function ValidateComplexTypeIdentifier 
{
    param
    (
        [string] $identifierName,
        [bool]   $isNameSpaceName,
        [string] $metaDataUri,
        [System.Management.Automation.PSCmdlet] $callerPSCmdlet
    )

    if($callerPSCmdlet -eq $null) { throw ($LocalizedData.ArguementNullError -f "PSCmdlet", "ValidateComplexTypeIdentifier") }

    if($isNameSpaceName)
    {
        $independentIdentifiers = $identifierName.Split('.')
        $result = $true
        foreach($currentIdentifier in $independentIdentifiers)
        {
            if(![System.CodeDom.Compiler.CodeGenerator]::IsValidLanguageIndependentIdentifier($currentIdentifier))
            {
                $result = $false
                break
            }
        }
    }
    else
    {
        $result = $cSharpCodeProvider.IsValidIdentifier($identifierName)
    }

    if(!$result)
    {
        $errorMessage = ($LocalizedData.InValidIdentifierInMetadata -f $metaDataUri, $identifierName)
        $errorRecord = CreateErrorRecordHelper "ODataEndpointProxyInvalidIdentifier" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidData) $null $identifierName
        $callerPSCmdlet.ThrowTerminatingError($errorRecord)
    }
    else
    {
        return $identifierName
    }
}

#########################################################
# GetKeys is a helper function used to 
# return the keys for the entity if customUri 
# is specified.
#########################################################
function GetKeys 
{
    param
    (
        [ODataUtils.EntitySet] $entitySet,
        [string] $customUri,
        [string] $actionName
    )

    # Get the original keys
    $key = (GetAllProperties $entitySet.Type) | Where-Object { $_.IsKey }

    # Get the keys with delimiters
    $keys = $customUri -split "/" | % {
        if ($_ -match '{*}')
        {
            [ODataUtils.TypeProperty] @{
                "Name" = $_.Substring($_.IndexOf('{')+1,$_.IndexOf('}')-$_.IndexOf('{')-1);
                "TypeName" = "Edm.String";
                "IsNullable" = $false;
                "IsMandatory" = $true;
            }
        }
        elseif ($_ -match '\[*\]')
        {
            if ($ActionName -eq 'Get') {
                [ODataUtils.TypeProperty] @{
                    "Name" = $_.Substring($_.IndexOf('[')+1,$_.IndexOf(']')-$_.IndexOf('[')-1);
                    "TypeName" = "Edm.String";
                    "IsNullable" = $false;
                    "IsMandatory" = $false;
                }
            }
            else {
                [ODataUtils.TypeProperty] @{
                    "Name" = $_.Substring($_.IndexOf('[')+1,$_.IndexOf(']')-$_.IndexOf('[')-1);
                    "TypeName" = "Edm.String";
                    "IsNullable" = $false;
                    "IsMandatory" = $true;
                }
            }
        }
    }

    # Now combine the two keys and avoid duplication
    # Make a list of names already present in the new keys
    # Foreach old key check if that key is present in the new keyList
    # Else add the key to new key list
    $keyParams = $keys | ForEach-Object {$_.Name}
    
    if ($keyParams -eq $null -Or $keyParams.Count -eq 0) {
        $keys = $key
    }
    else {
        if ($keyParams.Count -eq 1) {
            $keys = @($keys)
        }

        $key | ForEach-Object {
            if ($keyParams.Contains($_.Name) -eq $false)
            {
                $keys += $_
            }
        }
    }

    $keys
}

#########################################################
# GetNetworkControllerAdditionalProperties is a helper 
# function used to fetch network controller specific
# additional properties.
#########################################################
function GetNetworkControllerAdditionalProperties 
{
    param 
    (
        $navigationProperties,
        $metaData
    )

    # Additional properties contains the types present as navigation properties

    $additionalProperties = $navigationProperties | ? { $_ -ne $null } | %{
        $typeName = GetNavigationPropertyTypeName $_ $metaData

        if ($_.Name -eq "Properties") {
            $isNullable = $false
        }
        else {
            $isNullable = $true
        }

        [ODataUtils.TypeProperty] @{
            "Name" = $_.Name;
            "TypeName" = $typeName
            "IsNullable" = $isNullable;
        }
    }
   
    # Add etag to the additionalProperties

    if ($additionalProperties -ne $null)
    {
        if ($additionalProperties.Count -eq 1) {
            $additionalProperties = @($additionalProperties)
        }

        $additionalProperties += [ODataUtils.TypeProperty] @{
            "Name" = "Etag";
            "TypeName" = "Edm.String";
            "IsNullable" = $true;
        }
    }
    else
    {
      $additionalProperties = [ODataUtils.TypeProperty] @{
            "Name" = "Etag";
            "TypeName" = "Edm.String";
            "IsNullable" = $true;
        }  
    } 

    $additionalProperties
}

#########################################################
# UpdateNetworkControllerSpecificProperties is a 
# helper function used to append additionalProperties 
# to nullable/nonNullable Properties. This is network controller 
# specific logic.
#########################################################
function UpdateNetworkControllerSpecificProperties 
{
    param 
    (
        $nullableProperties,
        $additionalProperties,
        $keyProperties,
        $isNullable
    )

    if ($isNullable) {
        $additionalProperties = $additionalProperties | ? { $_.isNullable }
    }
    else {
        $additionalProperties = $additionalProperties | ? { -not $_.isNullable }
    }

    if ($nullableProperties -eq $null)
    {
        $nullableProperties = $additionalProperties
    }
    else {
        if ($nullableProperties.Count -eq 1) {
       	    $nullableProperties = @($nullableProperties)
        }
        if ($additionalProperties -ne $null) {
            $nullableProperties += $additionalProperties
        }
    }

    if ($nullableProperties -ne $null -And $keyProperties -ne $null)
    {
        if ($keyProperties.Count -eq 1) {
            $keyProperties = @($keyProperties)
        }

        $keys = $keyProperties | ForEach-Object {$_.Name} 

        if ($keys.Count -eq 1) {
            $keys = @($keys)
        }

        $nullableProperties = $nullableProperties | Where-Object {$keys.Contains($_.Name) -eq $false}
    }

    $nullableProperties
}

#########################################################
# GetNavigationPropertyTypeName is a 
# helper function used to fetch the type corresponding 
# to navigation property in this metadata. This is 
# network controller specific logic.
#########################################################
function GetNavigationPropertyTypeName 
{
    param 
    (
        $navigationProperty,
        $metaData
    )

    foreach($association in $metaData.Associations)
    {
        if ($association.Name -ne $navigationProperty.AssociationName -Or $association.Namespace -ne $navigationProperty.AssociationNamespace)
        {
            continue
        }

        # Now get the type for this association

        if ($association.Type.NavPropertyName1 -eq $navigationProperty.Name)
        {
            $type = $association.Type.EndType1
            $multiplicity = $association.Type.Multiplicity1
        }
        elseif ($associationType.NavPropertyName2 -eq $navigationProperty.Name)
        {
            $type = $association.Type.EndType2
            $multiplicity = $association.Type.Multiplicity2
        }
        
        break
    }

    $fullName = $type.Namespace + '.' + $type.Name
    
    # Check the multiplicity and convert to array if needed
    if ($multiplicity -eq "*")
    {
        $fullName = "Collection($fullName)"
    }

    $fullName
}
