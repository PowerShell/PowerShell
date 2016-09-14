# Localized ArchiveResources.psd1

ConvertFrom-StringData @'
###PSLOC
PathNotFoundError=The path '{0}' either does not exist or is not a valid file system path.
ExpandArchiveInValidDestinationPath=The path '{0}' is not a valid file system directory path.
InvalidZipFileExtensionError={0} is not a supported archive file format. {1} is the only supported archive file format.
ArchiveFileIsReadOnly=The attributes of the archive file {0} is set to 'ReadOnly' hence it cannot be updated. If you intend to update the existing archive file, remove the 'ReadOnly' attribute on the archive file else use -Force parameter to override and create a new archive file.
ZipFileExistError=The archive file {0} already exists. Use the -Update parameter to update the existing archive file or use the -Force parameter to overwrite the existing archive file.
DuplicatePathFoundError=The input to {0} parameter contains a duplicate path '{1}'. Provide a unique set of paths as input to {2} parameter.
ArchiveFileIsEmpty=The archive file {0} is empty.
CompressProgressBarText=The archive file '{0}' creation is in progress...
ExpandProgressBarText=The archive file '{0}' expansion is in progress...
AppendArchiveFileExtensionMessage=The archive file path '{0}' supplied to the DestinationPath parameter does not include .zip extension. Hence .zip is appended to the supplied DestinationPath path and the archive file would be created at '{1}'.
AddItemtoArchiveFile=Adding '{0}'.
CreateFileAtExpandedPath=Created '{0}'.
InvalidArchiveFilePathError=The archive file path '{0}' specified as input to the {1} parameter is resolving to multiple file system paths. Provide a unique path to the {2} parameter where the archive file has to be created.
InvalidExpandedDirPathError=The directory path '{0}' specified as input to the DestinationPath parameter is resolving to multiple file system paths. Provide a unique path to the Destination parameter where the archive file contents have to be expanded.
FileExistsError=Failed to create file '{0}' while expanding the archive file '{1}' contents as the file '{2}' already exists. Use the -Force parameter if you want to overwrite the existing directory '{3}' contents when expanding the archive file.
DeleteArchiveFile=The partially created archive file '{0}' is deleted as it is not usable.
InvalidDestinationPath=The destination path '{0}' does not contain a valid archive file name.
PreparingToCompressVerboseMessage=Preparing to compress...
PreparingToExpandVerboseMessage=Preparing to expand...
###PSLOC
'@
