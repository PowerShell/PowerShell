# Localized PSODataUtils.psd1

ConvertFrom-StringData @'
###PSLOC
SelectedAdapter=Dot sourcing '{0}'.
ArchitectureNotSupported=This module is not supported on your processor architecture ({0}).
ArguementNullError=Failed to generate proxy as '{0}' is pointing to $null in '{1}'.
EmptyMetadata=Read metadata was empty. Url: {0}.
InvalidEndpointAddress=Invalid endpoint address ({0}). Web response with status code '{1}' was obtained while accessing this endpoint address.
NoEntitySets=Metadata from URI '{0}' does not contain Entity Sets. No output will be written.
NoEntityTypes=Metadata from URI '{0}' does not contain Entity Types. No output will be written.
MetadataUriDoesNotExist=Metadata specified at the URI '{0}' does not exist. No output will be written.
InValidIdentifierInMetadata=Metadata specified at URI '{0}' contains an invalid Identifier '{1}'. Only valid C# identifiers are supported in the generated complex types during the proxy creation.
InValidMetadata=Failed to process metadata specified at URI '{0}'. No output will be written.
InValidXmlInMetadata=Metadata specified at URI '{0}' contains an invalid XML. No output will be written.
ODataVersionNotFound=Metadata specified at URI '{0}' does not contain the OData Version. No output will be written.
ODataVersionNotSupported=The OData version '{0}' specified in the metadata located at the URI '{1}' is not supported. Only OData versions between '{2}' and '{3}' are supported by '{4}' during proxy generation. No output will be written.
InValidSchemaNamespace=Metadata specified at URI '{0}' is invalid. NULL or Empty values are not supported for Namespace attribute in the schema.
InValidSchemaNamespaceConflictWithClassName=Metadata specified at URI '{0}' contains invalid Namespace {1} name, which conflicts with another type name. To avoid compilation error {1} will be changed to {2}.
InValidSchemaNamespaceContainsInvalidChars=Metadata specified at URI '{0}' contains invalid Namespace name {1} with a combination of dots and numbers in it, which is not allowed in .Net. To avoid compilation error {1} will be changed to {2}.
InValidUri=URI '{0}' is invalid. No output will be written.
RedfishNotEnabled=This version of Microsoft.PowerShell.ODataUtils doesn’t support Redfish, please run: ‘update-module Microsoft.PowerShell.ODataUtils’ to get Redfish support.
EntitySetUndefinedType=Metadata from URI '{0}' does not contain the Type for Entity Set '{1}'. No output will be written.
XmlWriterInitializationError=There was an error initiating XmlWriter for writing the {0} CDXML module.
EmptySchema=Edmx.DataServices.Schema node should not be null.
VerboseReadingMetadata=Reading metadata from uri {0}.
VerboseParsingMetadata=Parsing metadata...
VerboseVerifyingMetadata=Verifying metadata...
VerboseSavingModule=Saving output module to path {0}.
VerboseSavedCDXML=Saved CDXML module for {0} to {1}.
VerboseSavedServiceActions=Saved Service Actions CDXML module for to {0}.
VerboseSavedModuleManifest=Saved module manifest at {0}.
AssociationNotFound=Association {0} not found in Metadata.Associations.
TooManyMatchingAssociationTypes=Found {0} {1} associations in Metadata.Associations. Expected only one.
ZeroMatchingAssociationTypes=Navigation property {0} not found on association {1}.
WrongCountEntitySet=Expected one EntitySet for EntityType {0}, but got {1}.
EntityNameConflictError=Proxy creation is not supported when multiple EntitySets are mapped to the same EntityType. The metadata located at the URI '{0}' contains EntitySets '{1}' and '{2}' that are mapped to the same EntityType '{3}'.
VerboseSavedTypeDefinationModule=Saved Type definition module '{0}' at '{1}'.
VerboseAddingTypeDefinationToGeneratedModule=Adding Type definition for '{0}' to '{1}' module.
OutputPathNotFound=Could not find a part of the path '{0}'.
ModuleAlreadyExistsAndForceParameterIsNotSpecified=The directory '{0}' already exists.  Use the -Force parameter if you want to overwrite the directory and files within the directory.
InvalidOutputModulePath=Path '{0}' specified to -OutputModule parameter does not contain the module name.
OutputModulePathIsNotUnique=Path '{0}' specified to -OutputModule parameter resolves to multiple paths in the file system. Provide a unique file system path to -OutputModule parameter.
OutputModulePathIsNotFileSystemPath=Path '{0}' specified to -OutputModule parameter is not a file system. Provide a unique file system path to -OutputModule parameter.
SkipEntitySetProxyCreation=CDXML module creation has been skipped for the Entity Set '{0}' because its Entity Type '{1}' contains a property '{2}' that collides with one of the default properties of the generated cmdlets.
EntitySetProxyCreationWithWarning=CDXML module creation for the Entity Set '{0}' succeeded but contains a property '{1}' in the Entity Type '{2}' that collides with one of the default properties of the generated cmdlets.
SkipEntitySetConflictCommandCreation=CDXML module creation has been skipped for the Entity Set '{0}' because the exported command '{1}' conflicts with the inbox command.
EntitySetConflictCommandCreationWithWarning=CDXML module creation for the Entity Set '{0}' succeeded but contains a command '{1}' that collides with the inbox command.
SkipConflictServiceActionCommandCreation=CDXML module creation has been skipped for the Service Action '{0}' because the exported command '{1}' conflicts with the inbox command.
ConflictServiceActionCommandCreationWithWarning=CDXML module creation for the Service Action '{0}' succeeded but contains a command '{1}' that collides with the inbox command.
AllowUnsecureConnectionMessage=The cmdlet '{0}' is trying to establish an Unsecure connection with the OData endpoint through the URI '{1}'. Either supply a secure URI to the -{2} parameter or use -AllowUnsecureConnection switch parameter if you intend to use the current URI.
ProgressBarMessage=Creating proxy for the OData endpoint at the URI '{0}'.
###PSLOC

'@