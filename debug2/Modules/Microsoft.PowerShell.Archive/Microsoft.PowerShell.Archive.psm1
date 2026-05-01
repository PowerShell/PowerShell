data LocalizedData
{
    # culture="en-US"
    ConvertFrom-StringData @'
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
    BadArchiveEntry=Can not process invalid archive entry '{0}'.
    CreateFileAtExpandedPath=Created '{0}'.
    InvalidArchiveFilePathError=The archive file path '{0}' specified as input to the {1} parameter is resolving to multiple file system paths. Provide a unique path to the {2} parameter where the archive file has to be created.
    InvalidExpandedDirPathError=The directory path '{0}' specified as input to the DestinationPath parameter is resolving to multiple file system paths. Provide a unique path to the Destination parameter where the archive file contents have to be expanded.
    FileExistsError=Failed to create file '{0}' while expanding the archive file '{1}' contents as the file '{2}' already exists. Use the -Force parameter if you want to overwrite the existing directory '{3}' contents when expanding the archive file.
    DeleteArchiveFile=The partially created archive file '{0}' is deleted as it is not usable.
    InvalidDestinationPath=The destination path '{0}' does not contain a valid archive file name.
    PreparingToCompressVerboseMessage=Preparing to compress...
    PreparingToExpandVerboseMessage=Preparing to expand...
    ItemDoesNotAppearToBeAValidZipArchive=File '{0}' does not appear to be a valid zip archive.
'@
}

Import-LocalizedData LocalizedData -filename ArchiveResources -ErrorAction Ignore

$zipFileExtension = ".zip"

<############################################################################################
# The Compress-Archive cmdlet can be used to zip/compress one or more files/directories.
############################################################################################>
function Compress-Archive
{
    [CmdletBinding(
    DefaultParameterSetName="Path",
    SupportsShouldProcess=$true,
    HelpUri="https://go.microsoft.com/fwlink/?linkid=2096473")]
    [OutputType([System.IO.File])]
    param
    (
        [parameter (mandatory=$true, Position=0, ParameterSetName="Path", ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [parameter (mandatory=$true, Position=0, ParameterSetName="PathWithForce", ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [parameter (mandatory=$true, Position=0, ParameterSetName="PathWithUpdate", ValueFromPipeline=$true, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path,

        [parameter (mandatory=$true, ParameterSetName="LiteralPath", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$true)]
        [parameter (mandatory=$true, ParameterSetName="LiteralPathWithForce", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$true)]
        [parameter (mandatory=$true, ParameterSetName="LiteralPathWithUpdate", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath")]
        [string[]] $LiteralPath,

        [parameter (mandatory=$true,
        Position=1,
        ValueFromPipeline=$false,
        ValueFromPipelineByPropertyName=$false)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationPath,

        [parameter (
        mandatory=$false,
        ValueFromPipeline=$false,
        ValueFromPipelineByPropertyName=$false)]
        [ValidateSet("Optimal","NoCompression","Fastest")]
        [string]
        $CompressionLevel = "Optimal",

        [parameter(mandatory=$true, ParameterSetName="PathWithUpdate", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$false)]
        [parameter(mandatory=$true, ParameterSetName="LiteralPathWithUpdate", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$false)]
        [switch]
        $Update = $false,

        [parameter(mandatory=$true, ParameterSetName="PathWithForce", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$false)]
        [parameter(mandatory=$true, ParameterSetName="LiteralPathWithForce", ValueFromPipeline=$false, ValueFromPipelineByPropertyName=$false)]
        [switch]
        $Force = $false,

        [switch]
        $PassThru = $false
    )

    BEGIN
    {
        # Ensure the destination path is in a non-PS-specific format
        $DestinationPath = $PSCmdlet.GetUnresolvedProviderPathFromPSPath($DestinationPath)

        $inputPaths = @()
        $destinationParentDir = [system.IO.Path]::GetDirectoryName($DestinationPath)
        if($null -eq $destinationParentDir)
        {
            $errorMessage = ($LocalizedData.InvalidDestinationPath -f $DestinationPath)
            ThrowTerminatingErrorHelper "InvalidArchiveFilePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
        }

        if($destinationParentDir -eq [string]::Empty)
        {
            $destinationParentDir = '.'
        }

        $archiveFileName = [system.IO.Path]::GetFileName($DestinationPath)
        $destinationParentDir = GetResolvedPathHelper $destinationParentDir $false $PSCmdlet

        if($destinationParentDir.Count -gt 1)
        {
            $errorMessage = ($LocalizedData.InvalidArchiveFilePathError -f $DestinationPath, "DestinationPath", "DestinationPath")
            ThrowTerminatingErrorHelper "InvalidArchiveFilePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
        }

        IsValidFileSystemPath $destinationParentDir | Out-Null
        $DestinationPath = Join-Path -Path $destinationParentDir -ChildPath $archiveFileName

        # GetExtension API does not validate for the actual existence of the path.
        $extension = [system.IO.Path]::GetExtension($DestinationPath)

        # If user does not specify an extension, we append the .zip extension automatically.
        If($extension -eq [string]::Empty)
        {
            $DestinationPathWithOutExtension = $DestinationPath
            $DestinationPath = $DestinationPathWithOutExtension + $zipFileExtension
            $appendArchiveFileExtensionMessage = ($LocalizedData.AppendArchiveFileExtensionMessage -f $DestinationPathWithOutExtension, $DestinationPath)
            Write-Verbose $appendArchiveFileExtensionMessage
        }

        $archiveFileExist = Test-Path -LiteralPath $DestinationPath -PathType Leaf

        if($archiveFileExist -and ($Update -eq $false -and $Force -eq $false))
        {
            $errorMessage = ($LocalizedData.ZipFileExistError -f $DestinationPath)
            ThrowTerminatingErrorHelper "ArchiveFileExists" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
        }

        # If archive file already exists and if -Update is specified, then we check to see
        # if we have write access permission to update the existing archive file.
        if($archiveFileExist -and $Update -eq $true)
        {
            $item = Get-Item -Path $DestinationPath
            if($item.Attributes.ToString().Contains("ReadOnly"))
            {
                $errorMessage = ($LocalizedData.ArchiveFileIsReadOnly -f $DestinationPath)
                ThrowTerminatingErrorHelper "ArchiveFileIsReadOnly" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidOperation) $DestinationPath
            }
        }

        $isWhatIf = $psboundparameters.ContainsKey("WhatIf")
        if(!$isWhatIf)
        {
            $preparingToCompressVerboseMessage = ($LocalizedData.PreparingToCompressVerboseMessage)
            Write-Verbose $preparingToCompressVerboseMessage

            $progressBarStatus = ($LocalizedData.CompressProgressBarText -f $DestinationPath)
            ProgressBarHelper "Compress-Archive" $progressBarStatus 0 100 100 1
        }
    }
    PROCESS
    {
        if($PsCmdlet.ParameterSetName -eq "Path" -or
        $PsCmdlet.ParameterSetName -eq "PathWithForce" -or
        $PsCmdlet.ParameterSetName -eq "PathWithUpdate")
        {
            $inputPaths += $Path
        }

        if($PsCmdlet.ParameterSetName -eq "LiteralPath" -or
        $PsCmdlet.ParameterSetName -eq "LiteralPathWithForce" -or
        $PsCmdlet.ParameterSetName -eq "LiteralPathWithUpdate")
        {
            $inputPaths += $LiteralPath
        }
    }
    END
    {
        # If archive file already exists and if -Force is specified, we delete the
        # existing archive file and create a brand new one.
        if(($PsCmdlet.ParameterSetName -eq "PathWithForce" -or
        $PsCmdlet.ParameterSetName -eq "LiteralPathWithForce") -and $archiveFileExist)
        {
            Remove-Item -Path $DestinationPath -Force -ErrorAction Stop
        }

        # Validate Source Path depending on parameter set being used.
        # The specified source path contains one or more files or directories that needs
        # to be compressed.
        $isLiteralPathUsed = $false
        if($PsCmdlet.ParameterSetName -eq "LiteralPath" -or
        $PsCmdlet.ParameterSetName -eq "LiteralPathWithForce" -or
        $PsCmdlet.ParameterSetName -eq "LiteralPathWithUpdate")
        {
            $isLiteralPathUsed = $true
        }

        ValidateDuplicateFileSystemPath $PsCmdlet.ParameterSetName $inputPaths
        $resolvedPaths = GetResolvedPathHelper $inputPaths $isLiteralPathUsed $PSCmdlet
        IsValidFileSystemPath $resolvedPaths | Out-Null

        $sourcePath = $resolvedPaths;

        # CSVHelper: This is a helper function used to append comma after each path specified by
        # the $sourcePath array. The comma separated paths are displayed in the -WhatIf message.
        $sourcePathInCsvFormat = CSVHelper $sourcePath
        if($pscmdlet.ShouldProcess($sourcePathInCsvFormat))
        {
            try
            {
                # StopProcessing is not available in Script cmdlets. However the pipeline execution
                # is terminated when ever 'CTRL + C' is entered by user to terminate the cmdlet execution.
                # The finally block is executed whenever pipeline is terminated.
                # $isArchiveFileProcessingComplete variable is used to track if 'CTRL + C' is entered by the
                # user.
                $isArchiveFileProcessingComplete = $false

                $numberOfItemsArchived = CompressArchiveHelper $sourcePath $DestinationPath $CompressionLevel $Update

                $isArchiveFileProcessingComplete = $true
            }
            finally
            {
                # The $isArchiveFileProcessingComplete would be set to $false if user has typed 'CTRL + C' to
                # terminate the cmdlet execution or if an unhandled exception is thrown.
                # $numberOfItemsArchived contains the count of number of files or directories add to the archive file.
                # If the newly created archive file is empty then we delete it as it's not usable.
                if(($isArchiveFileProcessingComplete -eq $false) -or
                ($numberOfItemsArchived -eq 0))
                {
                    $DeleteArchiveFileMessage = ($LocalizedData.DeleteArchiveFile -f $DestinationPath)
                    Write-Verbose $DeleteArchiveFileMessage

                    # delete the partial archive file created.
                    if (Test-Path $DestinationPath) {
                        Remove-Item -LiteralPath $DestinationPath -Force -Recurse -ErrorAction SilentlyContinue
                    }
                }
                elseif ($PassThru)
                {
                    Get-Item -LiteralPath $DestinationPath
                }
            }
        }
    }
}

<############################################################################################
# The Expand-Archive cmdlet can be used to expand/extract an zip file.
############################################################################################>
function Expand-Archive
{
    [CmdletBinding(
    DefaultParameterSetName="Path",
    SupportsShouldProcess=$true,
    HelpUri="https://go.microsoft.com/fwlink/?linkid=2096769")]
    [OutputType([System.IO.FileSystemInfo])]
    param
    (
        [parameter (
        mandatory=$true,
        Position=0,
        ParameterSetName="Path",
        ValueFromPipeline=$true,
        ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [string] $Path,

        [parameter (
        mandatory=$true,
        ParameterSetName="LiteralPath",
        ValueFromPipelineByPropertyName=$true)]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath")]
        [string] $LiteralPath,

        [parameter (mandatory=$false,
        Position=1,
        ValueFromPipeline=$false,
        ValueFromPipelineByPropertyName=$false)]
        [ValidateNotNullOrEmpty()]
        [string] $DestinationPath,

        [parameter (mandatory=$false,
        ValueFromPipeline=$false,
        ValueFromPipelineByPropertyName=$false)]
        [switch] $Force,

        [switch]
        $PassThru = $false
    )

    BEGIN
    {
       $isVerbose = $psboundparameters.ContainsKey("Verbose")
       $isConfirm = $psboundparameters.ContainsKey("Confirm")

        $isDestinationPathProvided = $true
        if($DestinationPath -eq [string]::Empty)
        {
            $resolvedDestinationPath = (Get-Location).ProviderPath
            $isDestinationPathProvided = $false
        }
        else
        {
            $destinationPathExists = Test-Path -Path $DestinationPath -PathType Container
            if($destinationPathExists)
            {
                $resolvedDestinationPath = GetResolvedPathHelper $DestinationPath $false $PSCmdlet
                if($resolvedDestinationPath.Count -gt 1)
                {
                    $errorMessage = ($LocalizedData.InvalidExpandedDirPathError -f $DestinationPath)
                    ThrowTerminatingErrorHelper "InvalidDestinationPath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
                }

                # At this point we are sure that the provided path resolves to a valid single path.
                # Calling Resolve-Path again to get the underlying provider name.
                $suppliedDestinationPath = Resolve-Path -Path $DestinationPath
                if($suppliedDestinationPath.Provider.Name-ne "FileSystem")
                {
                    $errorMessage = ($LocalizedData.ExpandArchiveInValidDestinationPath -f $DestinationPath)
                    ThrowTerminatingErrorHelper "InvalidDirectoryPath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
                }
            }
            else
            {
                $createdItem = New-Item -Path $DestinationPath -ItemType Directory -Confirm:$isConfirm -Verbose:$isVerbose -ErrorAction Stop
                if($createdItem -ne $null -and $createdItem.PSProvider.Name -ne "FileSystem")
                {
                    Remove-Item "$DestinationPath" -Force -Recurse -ErrorAction SilentlyContinue
                    $errorMessage = ($LocalizedData.ExpandArchiveInValidDestinationPath -f $DestinationPath)
                    ThrowTerminatingErrorHelper "InvalidDirectoryPath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
                }

                $resolvedDestinationPath = GetResolvedPathHelper $DestinationPath $true $PSCmdlet
            }
        }

        $isWhatIf = $psboundparameters.ContainsKey("WhatIf")
        if(!$isWhatIf)
        {
            $preparingToExpandVerboseMessage = ($LocalizedData.PreparingToExpandVerboseMessage)
            Write-Verbose $preparingToExpandVerboseMessage

            $progressBarStatus = ($LocalizedData.ExpandProgressBarText -f $DestinationPath)
            ProgressBarHelper "Expand-Archive" $progressBarStatus 0 100 100 1
        }
    }
    PROCESS
    {
        switch($PsCmdlet.ParameterSetName)
        {
            "Path"
            {
                $resolvedSourcePaths = GetResolvedPathHelper $Path $false $PSCmdlet

                if($resolvedSourcePaths.Count -gt 1)
                {
                    $errorMessage = ($LocalizedData.InvalidArchiveFilePathError -f $Path, $PsCmdlet.ParameterSetName, $PsCmdlet.ParameterSetName)
                    ThrowTerminatingErrorHelper "InvalidArchiveFilePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $Path
                }
            }
            "LiteralPath"
            {
                $resolvedSourcePaths = GetResolvedPathHelper $LiteralPath $true $PSCmdlet

                if($resolvedSourcePaths.Count -gt 1)
                {
                    $errorMessage = ($LocalizedData.InvalidArchiveFilePathError -f $LiteralPath, $PsCmdlet.ParameterSetName, $PsCmdlet.ParameterSetName)
                    ThrowTerminatingErrorHelper "InvalidArchiveFilePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $LiteralPath
                }
            }
        }

        ValidateArchivePathHelper $resolvedSourcePaths

        if($pscmdlet.ShouldProcess($resolvedSourcePaths))
        {
            $expandedItems = @()

            try
            {
                # StopProcessing is not available in Script cmdlets. However the pipeline execution
                # is terminated when ever 'CTRL + C' is entered by user to terminate the cmdlet execution.
                # The finally block is executed whenever pipeline is terminated.
                # $isArchiveFileProcessingComplete variable is used to track if 'CTRL + C' is entered by the
                # user.
                $isArchiveFileProcessingComplete = $false

                # The User has not provided a destination path, hence we use '$pwd\ArchiveFileName' as the directory where the
                # archive file contents would be expanded. If the path '$pwd\ArchiveFileName' already exists then we use the
                # Windows default mechanism of appending a counter value at the end of the directory name where the contents
                # would be expanded.
                if(!$isDestinationPathProvided)
                {
                    $archiveFile = New-Object System.IO.FileInfo $resolvedSourcePaths
                    $resolvedDestinationPath = Join-Path -Path $resolvedDestinationPath -ChildPath $archiveFile.BaseName
                    $destinationPathExists = Test-Path -LiteralPath $resolvedDestinationPath -PathType Container

                    if(!$destinationPathExists)
                    {
                        New-Item -Path $resolvedDestinationPath -ItemType Directory -Confirm:$isConfirm -Verbose:$isVerbose -ErrorAction Stop | Out-Null
                    }
                }

                ExpandArchiveHelper $resolvedSourcePaths $resolvedDestinationPath ([ref]$expandedItems) $Force $isVerbose $isConfirm

                $isArchiveFileProcessingComplete = $true
            }
            finally
            {
                # The $isArchiveFileProcessingComplete would be set to $false if user has typed 'CTRL + C' to
                # terminate the cmdlet execution or if an unhandled exception is thrown.
                if($isArchiveFileProcessingComplete -eq $false)
                {
                    if($expandedItems.Count -gt 0)
                    {
                        # delete the expanded file/directory as the archive
                        # file was not completely expanded.
                        $expandedItems | % { Remove-Item "$_" -Force -Recurse }
                    }
                }
                elseif ($PassThru -and $expandedItems.Count -gt 0)
                {
                    # Return the expanded items, being careful to remove trailing directory separators from
                    # any folder paths for consistency
                    $trailingDirSeparators = '\' + [System.IO.Path]::DirectorySeparatorChar + '+$'
                    Get-Item -LiteralPath ($expandedItems -replace $trailingDirSeparators)
                }
            }
        }
    }
}

<############################################################################################
# GetResolvedPathHelper: This is a helper function used to resolve the user specified Path.
# The path can either be absolute or relative path.
############################################################################################>
function GetResolvedPathHelper
{
    param
    (
        [string[]] $path,
        [boolean] $isLiteralPath,
        [System.Management.Automation.PSCmdlet]
        $callerPSCmdlet
    )

    $resolvedPaths =@()

    # null and empty check are are already done on Path parameter at the cmdlet layer.
    foreach($currentPath in $path)
    {
        try
        {
            if($isLiteralPath)
            {
                $currentResolvedPaths = Resolve-Path -LiteralPath $currentPath -ErrorAction Stop
            }
            else
            {
                $currentResolvedPaths = Resolve-Path -Path $currentPath -ErrorAction Stop
            }
        }
        catch
        {
            $errorMessage = ($LocalizedData.PathNotFoundError -f $currentPath)
            $exception = New-Object System.InvalidOperationException $errorMessage, $_.Exception
            $errorRecord = CreateErrorRecordHelper "ArchiveCmdletPathNotFound" $null ([System.Management.Automation.ErrorCategory]::InvalidArgument) $exception $currentPath
            $callerPSCmdlet.ThrowTerminatingError($errorRecord)
        }

        foreach($currentResolvedPath in $currentResolvedPaths)
        {
            $resolvedPaths += $currentResolvedPath.ProviderPath
        }
    }

    $resolvedPaths
}

function Add-CompressionAssemblies {
    Add-Type -AssemblyName System.IO.Compression
    if ($psedition -eq "Core")
    {
        Add-Type -AssemblyName System.IO.Compression.ZipFile
    }
    else
    {
        Add-Type -AssemblyName System.IO.Compression.FileSystem
    }
}

function IsValidFileSystemPath
{
    param
    (
        [string[]] $path
    )

    $result = $true;

    # null and empty check are are already done on Path parameter at the cmdlet layer.
    foreach($currentPath in $path)
    {
        if(!([System.IO.File]::Exists($currentPath) -or [System.IO.Directory]::Exists($currentPath)))
        {
            $errorMessage = ($LocalizedData.PathNotFoundError -f $currentPath)
            ThrowTerminatingErrorHelper "PathNotFound" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $currentPath
        }
    }

    return $result;
}


function ValidateDuplicateFileSystemPath
{
    param
    (
        [string] $inputParameter,
        [string[]] $path
    )

    $uniqueInputPaths = @()

    # null and empty check are are already done on Path parameter at the cmdlet layer.
    foreach($currentPath in $path)
    {
        $currentInputPath = $currentPath.ToUpper()
        if($uniqueInputPaths.Contains($currentInputPath))
        {
            $errorMessage = ($LocalizedData.DuplicatePathFoundError -f $inputParameter, $currentPath, $inputParameter)
            ThrowTerminatingErrorHelper "DuplicatePathFound" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $currentPath
        }
        else
        {
            $uniqueInputPaths += $currentInputPath
        }
    }
}

function CompressionLevelMapper
{
    param
    (
        [string] $compressionLevel
    )

    $compressionLevelFormat = [System.IO.Compression.CompressionLevel]::Optimal

    # CompressionLevel format is already validated at the cmdlet layer.
    switch($compressionLevel.ToString())
    {
        "Fastest"
        {
            $compressionLevelFormat = [System.IO.Compression.CompressionLevel]::Fastest
        }
        "NoCompression"
        {
            $compressionLevelFormat = [System.IO.Compression.CompressionLevel]::NoCompression
        }
    }

    return $compressionLevelFormat
}

function CompressArchiveHelper
{
    param
    (
        [string[]] $sourcePath,
        [string]   $destinationPath,
        [string]   $compressionLevel,
        [bool]     $isUpdateMode
    )

    $numberOfItemsArchived = 0
    $sourceFilePaths = @()
    $sourceDirPaths = @()

    foreach($currentPath in $sourcePath)
    {
        $result = Test-Path -LiteralPath $currentPath -Type Leaf
        if($result -eq $true)
        {
            $sourceFilePaths += $currentPath
        }
        else
        {
            $sourceDirPaths += $currentPath
        }
    }

    # The Source Path contains one or more directory (this directory can have files under it) and no files to be compressed.
    if($sourceFilePaths.Count -eq 0 -and $sourceDirPaths.Count -gt 0)
    {
        $currentSegmentWeight = 100/[double]$sourceDirPaths.Count
        $previousSegmentWeight = 0
        foreach($currentSourceDirPath in $sourceDirPaths)
        {
            $count = CompressSingleDirHelper $currentSourceDirPath $destinationPath $compressionLevel $true $isUpdateMode $previousSegmentWeight $currentSegmentWeight
            $numberOfItemsArchived += $count
            $previousSegmentWeight += $currentSegmentWeight
        }
    }

    # The Source Path contains only files to be compressed.
    elseIf($sourceFilePaths.Count -gt 0 -and $sourceDirPaths.Count -eq 0)
    {
        # $previousSegmentWeight is equal to 0 as there are no prior segments.
        # $currentSegmentWeight is set to 100 as all files have equal weightage.
        $previousSegmentWeight = 0
        $currentSegmentWeight = 100

        $numberOfItemsArchived = CompressFilesHelper $sourceFilePaths $destinationPath $compressionLevel $isUpdateMode $previousSegmentWeight $currentSegmentWeight
    }
    # The Source Path contains one or more files and one or more directories (this directory can have files under it) to be compressed.
    elseif($sourceFilePaths.Count -gt 0 -and $sourceDirPaths.Count -gt 0)
    {
        # each directory is considered as an individual segments & all the individual files are clubed in to a separate segment.
        $currentSegmentWeight = 100/[double]($sourceDirPaths.Count +1)
        $previousSegmentWeight = 0

        foreach($currentSourceDirPath in $sourceDirPaths)
        {
            $count = CompressSingleDirHelper $currentSourceDirPath $destinationPath $compressionLevel $true $isUpdateMode $previousSegmentWeight $currentSegmentWeight
            $numberOfItemsArchived += $count
            $previousSegmentWeight += $currentSegmentWeight
        }

        $count = CompressFilesHelper $sourceFilePaths $destinationPath $compressionLevel $isUpdateMode $previousSegmentWeight $currentSegmentWeight
        $numberOfItemsArchived += $count
    }

    return $numberOfItemsArchived
}

function CompressFilesHelper
{
    param
    (
        [string[]] $sourceFilePaths,
        [string]   $destinationPath,
        [string]   $compressionLevel,
        [bool]     $isUpdateMode,
        [double]   $previousSegmentWeight,
        [double]   $currentSegmentWeight
    )

    $numberOfItemsArchived = ZipArchiveHelper $sourceFilePaths $destinationPath $compressionLevel $isUpdateMode $null $previousSegmentWeight $currentSegmentWeight

    return $numberOfItemsArchived
}

function CompressSingleDirHelper
{
    param
    (
        [string] $sourceDirPath,
        [string] $destinationPath,
        [string] $compressionLevel,
        [bool]   $useParentDirAsRoot,
        [bool]   $isUpdateMode,
        [double] $previousSegmentWeight,
        [double] $currentSegmentWeight
    )

    [System.Collections.Generic.List[System.String]]$subDirFiles = @()

    if($useParentDirAsRoot)
    {
        $sourceDirInfo = New-Object -TypeName System.IO.DirectoryInfo -ArgumentList $sourceDirPath
        $sourceDirFullName = $sourceDirInfo.Parent.FullName

        # If the directory is present at the drive level the DirectoryInfo.Parent include directory separator. example: C:\
        # On the other hand if the directory exists at a deper level then DirectoryInfo.Parent
        # has just the path (without an ending directory separator). example C:\source
        if($sourceDirFullName.Length -eq 3)
        {
            $modifiedSourceDirFullName = $sourceDirFullName
        }
        else
        {
            $modifiedSourceDirFullName = $sourceDirFullName + [System.IO.Path]::DirectorySeparatorChar
        }
    }
    else
    {
        $sourceDirFullName = $sourceDirPath
        $modifiedSourceDirFullName = $sourceDirFullName + [System.IO.Path]::DirectorySeparatorChar
    }

    $dirContents = Get-ChildItem -LiteralPath $sourceDirPath -Recurse
    foreach($currentContent in $dirContents)
    {
        $isContainer = $currentContent -is [System.IO.DirectoryInfo]
        if(!$isContainer)
        {
            $subDirFiles.Add($currentContent.FullName)
        }
        else
        {
            # The currentContent points to a directory.
            # We need to check if the directory is an empty directory, if so such a
            # directory has to be explicitly added to the archive file.
            # if there are no files in the directory the GetFiles() API returns an empty array.
            $files = $currentContent.GetFiles()
            if($files.Count -eq 0)
            {
                $subDirFiles.Add($currentContent.FullName + [System.IO.Path]::DirectorySeparatorChar)
            }
        }
    }

    $numberOfItemsArchived = ZipArchiveHelper $subDirFiles.ToArray() $destinationPath $compressionLevel $isUpdateMode $modifiedSourceDirFullName $previousSegmentWeight $currentSegmentWeight

    return $numberOfItemsArchived
}

function ZipArchiveHelper
{
    param
    (
        [System.Collections.Generic.List[System.String]] $sourcePaths,
        [string]   $destinationPath,
        [string]   $compressionLevel,
        [bool]     $isUpdateMode,
        [string]   $modifiedSourceDirFullName,
        [double]   $previousSegmentWeight,
        [double]   $currentSegmentWeight
    )

    $numberOfItemsArchived = 0
    $fileMode = [System.IO.FileMode]::Create
    $result = Test-Path -LiteralPath $DestinationPath -Type Leaf
    if($result -eq $true)
    {
        $fileMode = [System.IO.FileMode]::Open
    }

    Add-CompressionAssemblies

    try
    {
        # At this point we are sure that the archive file has write access.
        $archiveFileStreamArgs = @($destinationPath, $fileMode)
        $archiveFileStream = New-Object -TypeName System.IO.FileStream -ArgumentList $archiveFileStreamArgs

        $zipArchiveArgs = @($archiveFileStream, [System.IO.Compression.ZipArchiveMode]::Update, $false)
        $zipArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $zipArchiveArgs

        $currentEntryCount = 0
        $progressBarStatus = ($LocalizedData.CompressProgressBarText -f $destinationPath)
        $bufferSize = 4kb
        $buffer = New-Object Byte[] $bufferSize

        foreach($currentFilePath in $sourcePaths)
        {
            if($modifiedSourceDirFullName -ne $null -and $modifiedSourceDirFullName.Length -gt 0)
            {
                $index = $currentFilePath.IndexOf($modifiedSourceDirFullName, [System.StringComparison]::OrdinalIgnoreCase)
                $currentFilePathSubString = $currentFilePath.Substring($index, $modifiedSourceDirFullName.Length)
                $relativeFilePath = $currentFilePath.Replace($currentFilePathSubString, "").Trim()
            }
            else
            {
                $relativeFilePath = [System.IO.Path]::GetFileName($currentFilePath)
            }

            # Update mode is selected.
            # Check to see if archive file already contains one or more zip files in it.
            if($isUpdateMode -eq $true -and $zipArchive.Entries.Count -gt 0)
            {
                $entryToBeUpdated = $null

                # Check if the file already exists in the archive file.
                # If so replace it with new file from the input source.
                # If the file does not exist in the archive file then default to
                # create mode and create the entry in the archive file.

                foreach($currentArchiveEntry in $zipArchive.Entries)
                {
                    if(ArchivePathCompareHelper $currentArchiveEntry.FullName $relativeFilePath)
                    {
                        $entryToBeUpdated = $currentArchiveEntry
                        break
                    }
                }

                if($entryToBeUpdated -ne $null)
                {
                    $addItemtoArchiveFileMessage = ($LocalizedData.AddItemtoArchiveFile -f $currentFilePath)
                    $entryToBeUpdated.Delete()
                }
            }

            $compression = CompressionLevelMapper $compressionLevel

            # If a directory needs to be added to an archive file,
            # by convention the .Net API's expect the path of the directory
            # to end with directory separator to detect the path as an directory.
            if(!$relativeFilePath.EndsWith([System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase))
            {
                try
                {
                    try
                    {
                        $currentFileStream = [System.IO.File]::Open($currentFilePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::Read)
                    }
                    catch
                    {
                        # Failed to access the file. Write a non terminating error to the pipeline
                        # and move on with the remaining files.
                        $exception = $_.Exception
                        if($null -ne $_.Exception -and
                        $null -ne $_.Exception.InnerException)
                        {
                            $exception = $_.Exception.InnerException
                        }
                        $errorRecord = CreateErrorRecordHelper "CompressArchiveUnauthorizedAccessError" $null ([System.Management.Automation.ErrorCategory]::PermissionDenied) $exception $currentFilePath
                        Write-Error -ErrorRecord $errorRecord
                    }

                    if($null -ne $currentFileStream)
                    {
                        $srcStream = New-Object System.IO.BinaryReader $currentFileStream

                        $entryPath = DirectorySeparatorNormalizeHelper $relativeFilePath
                        $currentArchiveEntry = $zipArchive.CreateEntry($entryPath, $compression)

                        # Updating  the File Creation time so that the same timestamp would be retained after expanding the compressed file.
                        # At this point we are sure that Get-ChildItem would succeed.
                        $lastWriteTime = (Get-Item -LiteralPath $currentFilePath).LastWriteTime
                        if ($lastWriteTime.Year -lt 1980)
                        {
                            Write-Warning "'$currentFilePath' has LastWriteTime earlier than 1980. Compress-Archive will store any files with LastWriteTime values earlier than 1980 as 1/1/1980 00:00."
                            $lastWriteTime = [DateTime]::Parse('1980-01-01T00:00:00')
                        }

                        $currentArchiveEntry.LastWriteTime = $lastWriteTime

                        $destStream = New-Object System.IO.BinaryWriter $currentArchiveEntry.Open()

                        while($numberOfBytesRead = $srcStream.Read($buffer, 0, $bufferSize))
                        {
                            $destStream.Write($buffer, 0, $numberOfBytesRead)
                            $destStream.Flush()
                        }

                        $numberOfItemsArchived += 1
                        $addItemtoArchiveFileMessage = ($LocalizedData.AddItemtoArchiveFile -f $currentFilePath)
                    }
                }
                finally
                {
                    If($null -ne $currentFileStream)
                    {
                        $currentFileStream.Dispose()
                    }
                    If($null -ne $srcStream)
                    {
                        $srcStream.Dispose()
                    }
                    If($null -ne $destStream)
                    {
                        $destStream.Dispose()
                    }
                }
            }
            else
            {
                $entryPath = DirectorySeparatorNormalizeHelper $relativeFilePath
                $currentArchiveEntry = $zipArchive.CreateEntry($entryPath, $compression)
                $numberOfItemsArchived += 1
                $addItemtoArchiveFileMessage = ($LocalizedData.AddItemtoArchiveFile -f $currentFilePath)
            }

            if($null -ne $addItemtoArchiveFileMessage)
            {
                Write-Verbose $addItemtoArchiveFileMessage
            }

            $currentEntryCount += 1
            ProgressBarHelper "Compress-Archive" $progressBarStatus $previousSegmentWeight $currentSegmentWeight $sourcePaths.Count  $currentEntryCount
        }
    }
    finally
    {
        If($null -ne $zipArchive)
        {
            $zipArchive.Dispose()
        }

        If($null -ne $archiveFileStream)
        {
            $archiveFileStream.Dispose()
        }

        # Complete writing progress.
        Write-Progress -Activity "Compress-Archive" -Completed
    }

    return $numberOfItemsArchived
}

<############################################################################################
# ValidateArchivePathHelper: This is a helper function used to validate the archive file
# path & its file format. The only supported archive file format is .zip
############################################################################################>
function ValidateArchivePathHelper
{
    param
    (
        [string] $archiveFile
    )

    if(-not [System.IO.File]::Exists($archiveFile))
    {
        $errorMessage = ($LocalizedData.PathNotFoundError -f $archiveFile)
        ThrowTerminatingErrorHelper "PathNotFound" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $archiveFile
    }
}

<############################################################################################
# ExpandArchiveHelper: This is a helper function used to expand the archive file contents
# to the specified directory.
############################################################################################>
function ExpandArchiveHelper
{
    param
    (
        [string]  $archiveFile,
        [string]  $expandedDir,
        [ref]     $expandedItems,
        [boolean] $force,
        [boolean] $isVerbose,
        [boolean] $isConfirm
    )

    Add-CompressionAssemblies

    try
    {
        # The existence of archive file has already been validated by ValidateArchivePathHelper
        # before calling this helper function.
        $archiveFileStreamArgs = @($archiveFile, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
        $archiveFileStream = New-Object -TypeName System.IO.FileStream -ArgumentList $archiveFileStreamArgs

        $zipArchiveArgs = @($archiveFileStream, [System.IO.Compression.ZipArchiveMode]::Read, $false)
        try
        {
            $zipArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $zipArchiveArgs
        }
        catch [System.IO.InvalidDataException]
        {
            # Failed to open the file for reading as a zip archive. Wrap the exception
            # and re-throw it indicating it does not appear to be a valid zip file.
            $exception = $_.Exception
            if($null -ne $_.Exception -and
               $null -ne $_.Exception.InnerException)
            {
                $exception = $_.Exception.InnerException
            }
            # Load the WindowsBase.dll assembly to get access to the System.IO.FileFormatException class
            [System.Reflection.Assembly]::Load('WindowsBase,Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35')
            $invalidFileFormatException = New-Object -TypeName System.IO.FileFormatException -ArgumentList @(
                ($LocalizedData.ItemDoesNotAppearToBeAValidZipArchive -f $archiveFile)
                $exception
            )
            throw $invalidFileFormatException
        }

        if($zipArchive.Entries.Count -eq 0)
        {
            $archiveFileIsEmpty = ($LocalizedData.ArchiveFileIsEmpty -f $archiveFile)
            Write-Verbose $archiveFileIsEmpty
            return
        }

        $currentEntryCount = 0
        $progressBarStatus = ($LocalizedData.ExpandProgressBarText -f $archiveFile)

        # Ensures that the last character on the extraction path is the directory separator char.
        # Without this, a bad zip file could try to traverse outside of the expected extraction path.
        # At this point $expandedDir is a fully qualified path without any relative segments.
        if (-not $expandedDir.EndsWith([System.IO.Path]::DirectorySeparatorChar))
        {
	        $expandedDir += [System.IO.Path]::DirectorySeparatorChar
        }

        # The archive entries can either be empty directories or files.
        foreach($currentArchiveEntry in $zipArchive.Entries)
        {
            # Windows filesystem provider will internally convert from `/` to `\`
            $currentArchiveEntryPath = Join-Path -Path $expandedDir -ChildPath $currentArchiveEntry.FullName

            # Remove possible relative segments from target
            # This is similar to [System.IO.Path]::GetFullPath($currentArchiveEntryPath) but uses PS current dir instead of process-wide current dir
            $currentArchiveEntryPath = $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($currentArchiveEntryPath)
            
            # Check that expanded relative paths and absolute paths from the archive are Not going outside of target directory
            # Ordinal match is safest, case-sensitive volumes can be mounted within volumes that are case-insensitive.
            if (-not ($currentArchiveEntryPath.StartsWith($expandedDir, [System.StringComparison]::Ordinal)))
            {
                $BadArchiveEntryMessage = ($LocalizedData.BadArchiveEntry -f $currentArchiveEntry.FullName)
                # notify user of bad archive entry
                Write-Error $BadArchiveEntryMessage
                # move on to the next entry in the archive
                continue
            }

            $extension = [system.IO.Path]::GetExtension($currentArchiveEntryPath)

            # The current archive entry is an empty directory
            # The FullName of the Archive Entry representing a directory would end with a trailing directory separator.
            if($extension -eq [string]::Empty -and
            $currentArchiveEntryPath.EndsWith([System.IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase))
            {
                $pathExists = Test-Path -LiteralPath $currentArchiveEntryPath

                # The current archive entry expects an empty directory.
                # Check if the existing directory is empty. If it's not empty
                # then it means that user has added this directory by other means.
                if($pathExists -eq $false)
                {
                    New-Item $currentArchiveEntryPath -Type Directory -Confirm:$isConfirm | Out-Null

                    if(Test-Path -LiteralPath $currentArchiveEntryPath -PathType Container)
                    {
                        $addEmptyDirectorytoExpandedPathMessage = ($LocalizedData.AddItemtoArchiveFile -f $currentArchiveEntryPath)
                        Write-Verbose $addEmptyDirectorytoExpandedPathMessage

                        $expandedItems.Value += $currentArchiveEntryPath
                    }
                }
            }
            else
            {
                try
                {
                    $currentArchiveEntryFileInfo = New-Object -TypeName System.IO.FileInfo -ArgumentList $currentArchiveEntryPath
                    $parentDirExists = Test-Path -LiteralPath $currentArchiveEntryFileInfo.DirectoryName -PathType Container

                    # If the Parent directory of the current entry in the archive file does not exist, then create it.
                    if($parentDirExists -eq $false)
                    {
                        # note that if any ancestor of this directory doesn't exist, we don't recursively create each one as New-Item
                        # takes care of this already, so only one DirectoryInfo is returned instead of one for each parent directory
                        # that only contains directories
                        New-Item $currentArchiveEntryFileInfo.DirectoryName -Type Directory -Confirm:$isConfirm | Out-Null

                        if(!(Test-Path -LiteralPath $currentArchiveEntryFileInfo.DirectoryName -PathType Container))
                        {
                            # The directory referred by $currentArchiveEntryFileInfo.DirectoryName was not successfully created.
                            # This could be because the user has specified -Confirm parameter when Expand-Archive was invoked
                            # and authorization was not provided when confirmation was prompted. In such a scenario,
                            # we skip the current file in the archive and continue with the remaining archive file contents.
                            Continue
                        }

                        $expandedItems.Value += $currentArchiveEntryFileInfo.DirectoryName
                    }

                    $hasNonTerminatingError = $false

                    # Check if the file in to which the current archive entry contents
                    # would be expanded already exists.
                    if($currentArchiveEntryFileInfo.Exists)
                    {
                        if($force)
                        {
                            Remove-Item -LiteralPath $currentArchiveEntryFileInfo.FullName -Force -ErrorVariable ev -Verbose:$isVerbose -Confirm:$isConfirm
                            if($ev -ne $null)
                            {
                                $hasNonTerminatingError = $true
                            }

                            if(Test-Path -LiteralPath $currentArchiveEntryFileInfo.FullName -PathType Leaf)
                            {
                                # The file referred by $currentArchiveEntryFileInfo.FullName was not successfully removed.
                                # This could be because the user has specified -Confirm parameter when Expand-Archive was invoked
                                # and authorization was not provided when confirmation was prompted. In such a scenario,
                                # we skip the current file in the archive and continue with the remaining archive file contents.
                                Continue
                            }
                        }
                        else
                        {
                            # Write non-terminating error to the pipeline.
                            $errorMessage = ($LocalizedData.FileExistsError -f $currentArchiveEntryFileInfo.FullName, $archiveFile, $currentArchiveEntryFileInfo.FullName, $currentArchiveEntryFileInfo.FullName)
                            $errorRecord = CreateErrorRecordHelper "ExpandArchiveFileExists" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidOperation) $null $currentArchiveEntryFileInfo.FullName
                            Write-Error -ErrorRecord $errorRecord
                            $hasNonTerminatingError = $true
                        }
                    }

                    if(!$hasNonTerminatingError)
                    {
                        # The ExtractToFile() method doesn't handle whitespace correctly, strip whitespace which is consistent with how Explorer handles archives
                        # There is an edge case where an archive contains files whose only difference is whitespace, but this is uncommon and likely not legitimate
                        [string[]] $parts = $currentArchiveEntryPath.Split([System.IO.Path]::DirectorySeparatorChar) | % { $_.Trim() }
                        $currentArchiveEntryPath = [string]::Join([System.IO.Path]::DirectorySeparatorChar, $parts)

                        [System.IO.Compression.ZipFileExtensions]::ExtractToFile($currentArchiveEntry, $currentArchiveEntryPath, $false)

                        # Add the expanded file path to the $expandedItems array,
                        # to keep track of all the expanded files created while expanding the archive file.
                        # If user enters CTRL + C then at that point of time, all these expanded files
                        # would be deleted as part of the clean up process.
                        $expandedItems.Value += $currentArchiveEntryPath

                        $addFiletoExpandedPathMessage = ($LocalizedData.CreateFileAtExpandedPath -f $currentArchiveEntryPath)
                        Write-Verbose $addFiletoExpandedPathMessage
                    }
                }
                finally
                {
                    If($null -ne $destStream)
                    {
                        $destStream.Dispose()
                    }

                    If($null -ne $srcStream)
                    {
                        $srcStream.Dispose()
                    }
                }
            }

            $currentEntryCount += 1
            # $currentSegmentWeight is Set to 100 giving equal weightage to each file that is getting expanded.
            # $previousSegmentWeight is set to 0 as there are no prior segments.
            $previousSegmentWeight = 0
            $currentSegmentWeight = 100
            ProgressBarHelper "Expand-Archive" $progressBarStatus $previousSegmentWeight $currentSegmentWeight $zipArchive.Entries.Count  $currentEntryCount
        }
    }
    finally
    {
        If($null -ne $zipArchive)
        {
            $zipArchive.Dispose()
        }

        If($null -ne $archiveFileStream)
        {
            $archiveFileStream.Dispose()
        }

        # Complete writing progress.
        Write-Progress -Activity "Expand-Archive" -Completed
    }
}

<############################################################################################
# ProgressBarHelper: This is a helper function used to display progress message.
# This function is used by both Compress-Archive & Expand-Archive to display archive file
# creation/expansion progress.
############################################################################################>
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

<############################################################################################
# CSVHelper: This is a helper function used to append comma after each path specified by
# the SourcePath array. This helper function is used to display all the user supplied paths
# in the WhatIf message.
############################################################################################>
function CSVHelper
{
    param
    (
        [string[]] $sourcePath
    )

    # SourcePath has already been validated by the calling function.
    if($sourcePath.Count -gt 1)
    {
        $sourcePathInCsvFormat = "`n"
        for($currentIndex=0; $currentIndex -lt $sourcePath.Count; $currentIndex++)
        {
            if($currentIndex -eq $sourcePath.Count - 1)
            {
                $sourcePathInCsvFormat += $sourcePath[$currentIndex]
            }
            else
            {
                $sourcePathInCsvFormat += $sourcePath[$currentIndex] + "`n"
            }
        }
    }
    else
    {
        $sourcePathInCsvFormat = $sourcePath
    }

    return $sourcePathInCsvFormat
}

<############################################################################################
# ThrowTerminatingErrorHelper: This is a helper function used to throw terminating error.
############################################################################################>
function ThrowTerminatingErrorHelper
{
    param
    (
        [string] $errorId,
        [string] $errorMessage,
        [System.Management.Automation.ErrorCategory] $errorCategory,
        [object] $targetObject,
        [Exception] $innerException
    )

    if($innerException -eq $null)
    {
        $exception = New-object System.IO.IOException $errorMessage
    }
    else
    {
        $exception = New-Object System.IO.IOException $errorMessage, $innerException
    }

    $exception = New-Object System.IO.IOException $errorMessage
    $errorRecord = New-Object System.Management.Automation.ErrorRecord $exception, $errorId, $errorCategory, $targetObject
    $PSCmdlet.ThrowTerminatingError($errorRecord)
}

<############################################################################################
# CreateErrorRecordHelper: This is a helper function used to create an ErrorRecord
############################################################################################>
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

    if($null -eq $exception)
    {
        $exception = New-Object System.IO.IOException $errorMessage
    }

    $errorRecord = New-Object System.Management.Automation.ErrorRecord $exception, $errorId, $errorCategory, $targetObject
    return $errorRecord
}

<############################################################################################
# DirectorySeparatorNormalizeHelper: This is a helper function used to normalize separators
# when compressing archives, creating cross platform archives. 
# 
# The approach taken is leveraging the fact that .net on Windows all the way back to 
# Framework 1.1 specifies `\` as DirectoryPathSeparatorChar and `/` as
# AltDirectoryPathSeparatorChar, while other platforms in .net Core use `/` for
# DirectoryPathSeparatorChar and AltDirectoryPathSeparatorChar. When using a *nix platform,
# the replacements will be no-ops, while Windows will convert all `\` to `/` for the
# purposes of the ZipEntry FullName.
############################################################################################>
function DirectorySeparatorNormalizeHelper
{
    param
    (
        [string] $archivePath
    )

    if($null -eq $archivePath)
    {
        return $archivePath
    }

    return $archivePath.replace([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar)
}

<############################################################################################
# ArchivePathCompareHelper: This is a helper function used to compare with normalized 
# separators.
############################################################################################>
function ArchivePathCompareHelper
{
    param
    (
        [string] $pathArgA,
        [string] $pathArgB
    )

    $normalizedPathArgA = DirectorySeparatorNormalizeHelper $pathArgA
    $normalizedPathArgB = DirectorySeparatorNormalizeHelper $pathArgB

    return $normalizedPathArgA -eq $normalizedPathArgB
}

# SIG # Begin signature block
# MIIjigYJKoZIhvcNAQcCoIIjezCCI3cCAQExDzANBglghkgBZQMEAgEFADB5Bgor
# BgEEAYI3AgEEoGswaTA0BgorBgEEAYI3AgEeMCYCAwEAAAQQH8w7YFlLCE63JNLG
# KX7zUQIBAAIBAAIBAAIBAAIBADAxMA0GCWCGSAFlAwQCAQUABCCxWUVaNUPabUNe
# T6XgWNeKW3CfCVc/X+PBlTJ8w9y52qCCDYUwggYDMIID66ADAgECAhMzAAABUptA
# n1BWmXWIAAAAAAFSMA0GCSqGSIb3DQEBCwUAMH4xCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01pY3Jvc29mdCBDb2RlIFNpZ25p
# bmcgUENBIDIwMTEwHhcNMTkwNTAyMjEzNzQ2WhcNMjAwNTAyMjEzNzQ2WjB0MQsw
# CQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9u
# ZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMR4wHAYDVQQDExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIB
# AQCxp4nT9qfu9O10iJyewYXHlN+WEh79Noor9nhM6enUNbCbhX9vS+8c/3eIVazS
# YnVBTqLzW7xWN1bCcItDbsEzKEE2BswSun7J9xCaLwcGHKFr+qWUlz7hh9RcmjYS
# kOGNybOfrgj3sm0DStoK8ljwEyUVeRfMHx9E/7Ca/OEq2cXBT3L0fVnlEkfal310
# EFCLDo2BrE35NGRjG+/nnZiqKqEh5lWNk33JV8/I0fIcUKrLEmUGrv0CgC7w2cjm
# bBhBIJ+0KzSnSWingXol/3iUdBBy4QQNH767kYGunJeY08RjHMIgjJCdAoEM+2mX
# v1phaV7j+M3dNzZ/cdsz3oDfAgMBAAGjggGCMIIBfjAfBgNVHSUEGDAWBgorBgEE
# AYI3TAgBBggrBgEFBQcDAzAdBgNVHQ4EFgQU3f8Aw1sW72WcJ2bo/QSYGzVrRYcw
# VAYDVR0RBE0wS6RJMEcxLTArBgNVBAsTJE1pY3Jvc29mdCBJcmVsYW5kIE9wZXJh
# dGlvbnMgTGltaXRlZDEWMBQGA1UEBRMNMjMwMDEyKzQ1NDEzNjAfBgNVHSMEGDAW
# gBRIbmTlUAXTgqoXNzcitW2oynUClTBUBgNVHR8ETTBLMEmgR6BFhkNodHRwOi8v
# d3d3Lm1pY3Jvc29mdC5jb20vcGtpb3BzL2NybC9NaWNDb2RTaWdQQ0EyMDExXzIw
# MTEtMDctMDguY3JsMGEGCCsGAQUFBwEBBFUwUzBRBggrBgEFBQcwAoZFaHR0cDov
# L3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9jZXJ0cy9NaWNDb2RTaWdQQ0EyMDEx
# XzIwMTEtMDctMDguY3J0MAwGA1UdEwEB/wQCMAAwDQYJKoZIhvcNAQELBQADggIB
# AJTwROaHvogXgixWjyjvLfiRgqI2QK8GoG23eqAgNjX7V/WdUWBbs0aIC3k49cd0
# zdq+JJImixcX6UOTpz2LZPFSh23l0/Mo35wG7JXUxgO0U+5drbQht5xoMl1n7/TQ
# 4iKcmAYSAPxTq5lFnoV2+fAeljVA7O43szjs7LR09D0wFHwzZco/iE8Hlakl23ZT
# 7FnB5AfU2hwfv87y3q3a5qFiugSykILpK0/vqnlEVB0KAdQVzYULQ/U4eFEjnis3
# Js9UrAvtIhIs26445Rj3UP6U4GgOjgQonlRA+mDlsh78wFSGbASIvK+fkONUhvj8
# B8ZHNn4TFfnct+a0ZueY4f6aRPxr8beNSUKn7QW/FQmn422bE7KfnqWncsH7vbNh
# G929prVHPsaa7J22i9wyHj7m0oATXJ+YjfyoEAtd5/NyIYaE4Uu0j1EhuYUo5VaJ
# JnMaTER0qX8+/YZRWrFN/heps41XNVjiAawpbAa0fUa3R9RNBjPiBnM0gvNPorM4
# dsV2VJ8GluIQOrJlOvuCrOYDGirGnadOmQ21wPBoGFCWpK56PxzliKsy5NNmAXcE
# x7Qb9vUjY1WlYtrdwOXTpxN4slzIht69BaZlLIjLVWwqIfuNrhHKNDM9K+v7vgrI
# bf7l5/665g0gjQCDCN6Q5sxuttTAEKtJeS/pkpI+DbZ/MIIHejCCBWKgAwIBAgIK
# YQ6Q0gAAAAAAAzANBgkqhkiG9w0BAQsFADCBiDELMAkGA1UEBhMCVVMxEzARBgNV
# BAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoTFU1pY3Jv
# c29mdCBDb3Jwb3JhdGlvbjEyMDAGA1UEAxMpTWljcm9zb2Z0IFJvb3QgQ2VydGlm
# aWNhdGUgQXV0aG9yaXR5IDIwMTEwHhcNMTEwNzA4MjA1OTA5WhcNMjYwNzA4MjEw
# OTA5WjB+MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UE
# BxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSgwJgYD
# VQQDEx9NaWNyb3NvZnQgQ29kZSBTaWduaW5nIFBDQSAyMDExMIICIjANBgkqhkiG
# 9w0BAQEFAAOCAg8AMIICCgKCAgEAq/D6chAcLq3YbqqCEE00uvK2WCGfQhsqa+la
# UKq4BjgaBEm6f8MMHt03a8YS2AvwOMKZBrDIOdUBFDFC04kNeWSHfpRgJGyvnkmc
# 6Whe0t+bU7IKLMOv2akrrnoJr9eWWcpgGgXpZnboMlImEi/nqwhQz7NEt13YxC4D
# dato88tt8zpcoRb0RrrgOGSsbmQ1eKagYw8t00CT+OPeBw3VXHmlSSnnDb6gE3e+
# lD3v++MrWhAfTVYoonpy4BI6t0le2O3tQ5GD2Xuye4Yb2T6xjF3oiU+EGvKhL1nk
# kDstrjNYxbc+/jLTswM9sbKvkjh+0p2ALPVOVpEhNSXDOW5kf1O6nA+tGSOEy/S6
# A4aN91/w0FK/jJSHvMAhdCVfGCi2zCcoOCWYOUo2z3yxkq4cI6epZuxhH2rhKEmd
# X4jiJV3TIUs+UsS1Vz8kA/DRelsv1SPjcF0PUUZ3s/gA4bysAoJf28AVs70b1FVL
# 5zmhD+kjSbwYuER8ReTBw3J64HLnJN+/RpnF78IcV9uDjexNSTCnq47f7Fufr/zd
# sGbiwZeBe+3W7UvnSSmnEyimp31ngOaKYnhfsi+E11ecXL93KCjx7W3DKI8sj0A3
# T8HhhUSJxAlMxdSlQy90lfdu+HggWCwTXWCVmj5PM4TasIgX3p5O9JawvEagbJjS
# 4NaIjAsCAwEAAaOCAe0wggHpMBAGCSsGAQQBgjcVAQQDAgEAMB0GA1UdDgQWBBRI
# bmTlUAXTgqoXNzcitW2oynUClTAZBgkrBgEEAYI3FAIEDB4KAFMAdQBiAEMAQTAL
# BgNVHQ8EBAMCAYYwDwYDVR0TAQH/BAUwAwEB/zAfBgNVHSMEGDAWgBRyLToCMZBD
# uRQFTuHqp8cx0SOJNDBaBgNVHR8EUzBRME+gTaBLhklodHRwOi8vY3JsLm1pY3Jv
# c29mdC5jb20vcGtpL2NybC9wcm9kdWN0cy9NaWNSb29DZXJBdXQyMDExXzIwMTFf
# MDNfMjIuY3JsMF4GCCsGAQUFBwEBBFIwUDBOBggrBgEFBQcwAoZCaHR0cDovL3d3
# dy5taWNyb3NvZnQuY29tL3BraS9jZXJ0cy9NaWNSb29DZXJBdXQyMDExXzIwMTFf
# MDNfMjIuY3J0MIGfBgNVHSAEgZcwgZQwgZEGCSsGAQQBgjcuAzCBgzA/BggrBgEF
# BQcCARYzaHR0cDovL3d3dy5taWNyb3NvZnQuY29tL3BraW9wcy9kb2NzL3ByaW1h
# cnljcHMuaHRtMEAGCCsGAQUFBwICMDQeMiAdAEwAZQBnAGEAbABfAHAAbwBsAGkA
# YwB5AF8AcwB0AGEAdABlAG0AZQBuAHQALiAdMA0GCSqGSIb3DQEBCwUAA4ICAQBn
# 8oalmOBUeRou09h0ZyKbC5YR4WOSmUKWfdJ5DJDBZV8uLD74w3LRbYP+vj/oCso7
# v0epo/Np22O/IjWll11lhJB9i0ZQVdgMknzSGksc8zxCi1LQsP1r4z4HLimb5j0b
# pdS1HXeUOeLpZMlEPXh6I/MTfaaQdION9MsmAkYqwooQu6SpBQyb7Wj6aC6VoCo/
# KmtYSWMfCWluWpiW5IP0wI/zRive/DvQvTXvbiWu5a8n7dDd8w6vmSiXmE0OPQvy
# CInWH8MyGOLwxS3OW560STkKxgrCxq2u5bLZ2xWIUUVYODJxJxp/sfQn+N4sOiBp
# mLJZiWhub6e3dMNABQamASooPoI/E01mC8CzTfXhj38cbxV9Rad25UAqZaPDXVJi
# hsMdYzaXht/a8/jyFqGaJ+HNpZfQ7l1jQeNbB5yHPgZ3BtEGsXUfFL5hYbXw3MYb
# BL7fQccOKO7eZS/sl/ahXJbYANahRr1Z85elCUtIEJmAH9AAKcWxm6U/RXceNcbS
# oqKfenoi+kiVH6v7RyOA9Z74v2u3S5fi63V4GuzqN5l5GEv/1rMjaHXmr/r8i+sL
# gOppO6/8MO0ETI7f33VtY5E90Z1WTk+/gFcioXgRMiF670EKsT/7qMykXcGhiJtX
# cVZOSEXAQsmbdlsKgEhr/Xmfwb1tbWrJUnMTDXpQzTGCFVswghVXAgEBMIGVMH4x
# CzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRt
# b25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xKDAmBgNVBAMTH01p
# Y3Jvc29mdCBDb2RlIFNpZ25pbmcgUENBIDIwMTECEzMAAAFSm0CfUFaZdYgAAAAA
# AVIwDQYJYIZIAWUDBAIBBQCgga4wGQYJKoZIhvcNAQkDMQwGCisGAQQBgjcCAQQw
# HAYKKwYBBAGCNwIBCzEOMAwGCisGAQQBgjcCARUwLwYJKoZIhvcNAQkEMSIEIDtq
# icg7O3t+8aEthrg0b2qP4aWs9LkzLpmfTq0zfyrgMEIGCisGAQQBgjcCAQwxNDAy
# oBSAEgBNAGkAYwByAG8AcwBvAGYAdKEagBhodHRwOi8vd3d3Lm1pY3Jvc29mdC5j
# b20wDQYJKoZIhvcNAQEBBQAEggEAEc8jC370rcVzKGJGuP/id6ETZxvJvR51hEBc
# wIQIV6RfjKKR4eH9yHGDHhx1uWvdzkiYAApbviY1ISYpLsMz1/ugN0QTcBJt2PGr
# 8abeBLzUH6whhzY8xVnk3CGox9DkgS72NuQ8GkzwbXXwZ/EgNNT8j9JtqC/Ivnbh
# ciuIAwAzymIMIlJ7jZm1GFuOqUDkpQ2DZ6BloNgA5CjE1BImmZjEKI5dNsJd+jZ2
# CcpIRPlG7GYD2GlaR8Aw/lfkgIQB22P0eR+hm8Qa2vbqpIZQZV63BBPyZnahBllb
# wocwG9/tVlhoe0HD2Iv1nU9PC+K8s84i5MdYNIEnrsTC3AlJDaGCEuUwghLhBgor
# BgEEAYI3AwMBMYIS0TCCEs0GCSqGSIb3DQEHAqCCEr4wghK6AgEDMQ8wDQYJYIZI
# AWUDBAIBBQAwggFRBgsqhkiG9w0BCRABBKCCAUAEggE8MIIBOAIBAQYKKwYBBAGE
# WQoDATAxMA0GCWCGSAFlAwQCAQUABCDV4w1fw0ZxcYyjngmpg8RKkuUEKE9srNko
# UiQpktfOpwIGXin1JLlvGBMyMDIwMDIxMTE5MzgwOC4zNTFaMASAAgH0oIHQpIHN
# MIHKMQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMH
# UmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSUwIwYDVQQL
# ExxNaWNyb3NvZnQgQW1lcmljYSBPcGVyYXRpb25zMSYwJAYDVQQLEx1UaGFsZXMg
# VFNTIEVTTjozQkJELUUzMzgtRTlBMTElMCMGA1UEAxMcTWljcm9zb2Z0IFRpbWUt
# U3RhbXAgU2VydmljZaCCDjwwggTxMIID2aADAgECAhMzAAABHcLCCK4+uq5IAAAA
# AAEdMA0GCSqGSIb3DQEBCwUAMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNo
# aW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29y
# cG9yYXRpb24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEw
# MB4XDTE5MTExMzIxNDAzOVoXDTIxMDIxMTIxNDAzOVowgcoxCzAJBgNVBAYTAlVT
# MRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQK
# ExVNaWNyb3NvZnQgQ29ycG9yYXRpb24xJTAjBgNVBAsTHE1pY3Jvc29mdCBBbWVy
# aWNhIE9wZXJhdGlvbnMxJjAkBgNVBAsTHVRoYWxlcyBUU1MgRVNOOjNCQkQtRTMz
# OC1FOUExMSUwIwYDVQQDExxNaWNyb3NvZnQgVGltZS1TdGFtcCBTZXJ2aWNlMIIB
# IjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAnIUB+qF+9ZgKHPlQgCkog4Jx
# E7nDBGiccHUtx3G4Dmf7Sq1xPxbYhj47XjszH4ko6Hpd9FogVJLRMdXOs/+At1zC
# Gh1jY+/yEeg/We+4torptCryEChrH5hfWuijIINh1jOESX852lVrR/U+SryLRdrB
# ygw3Hjv3O4VAGiMy6lQjTBNq+la+5GHyJ7uTRx9KpIDiBmVvQVYJraXqVmaErl3B
# phfx9THN9jfyr/LF1f4WJRN004AswPLObTaL7KKYcw/V9AHLLDbCdKkamwO7v7K9
# yWbUGs4z7Y38NcBr4dVDCd5XJq4GncJZZUcNtSs49VzRLvjWKfIszyO6r6D85QID
# AQABo4IBGzCCARcwHQYDVR0OBBYEFBdeWhRg3sjyCL4rQ2oIz8Ctm8KpMB8GA1Ud
# IwQYMBaAFNVjOlyKMZDzQ3t8RhvFM2hahW1VMFYGA1UdHwRPME0wS6BJoEeGRWh0
# dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kvY3JsL3Byb2R1Y3RzL01pY1RpbVN0
# YVBDQV8yMDEwLTA3LTAxLmNybDBaBggrBgEFBQcBAQROMEwwSgYIKwYBBQUHMAKG
# Pmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9wa2kvY2VydHMvTWljVGltU3RhUENB
# XzIwMTAtMDctMDEuY3J0MAwGA1UdEwEB/wQCMAAwEwYDVR0lBAwwCgYIKwYBBQUH
# AwgwDQYJKoZIhvcNAQELBQADggEBAGCMkZ2Q8AzLOODQFevh+mz54xvfUV8OEXin
# Y9EkjuSHcAhRIzC5Dx38/ZXuVd4QAYWuvPOhDMb90tvcn6VB1uamwrSwp/AsnYmf
# HsjSnBR7iphJsOrMlquFP7Vjh4+ObwBadetdlSN//m2dJ4PNgHTeo54ypbsP8E37
# arRuiHP7TTWOsbalUfmWXKvKDBJyn2I96L7NoluPC9u8hrVg/ReobiUkSLiGyttz
# NL6IUTns+bZ51ky1C6NXEY/LZmnUy03FTnM4H7t2c2tMxbhQW1H5BGdIDButZya5
# BZ14pcl4feBYev/SMBRsg6aGB/CdwQFKf1oW3gX60Mk1VhvMSTAwggZxMIIEWaAD
# AgECAgphCYEqAAAAAAACMA0GCSqGSIb3DQEBCwUAMIGIMQswCQYDVQQGEwJVUzET
# MBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UEBxMHUmVkbW9uZDEeMBwGA1UEChMV
# TWljcm9zb2Z0IENvcnBvcmF0aW9uMTIwMAYDVQQDEylNaWNyb3NvZnQgUm9vdCBD
# ZXJ0aWZpY2F0ZSBBdXRob3JpdHkgMjAxMDAeFw0xMDA3MDEyMTM2NTVaFw0yNTA3
# MDEyMTQ2NTVaMHwxCzAJBgNVBAYTAlVTMRMwEQYDVQQIEwpXYXNoaW5ndG9uMRAw
# DgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNyb3NvZnQgQ29ycG9yYXRpb24x
# JjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1wIFBDQSAyMDEwMIIBIjANBgkq
# hkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAqR0NvHcRijog7PwTl/X6f2mUa3RUENWl
# CgCChfvtfGhLLF/Fw+Vhwna3PmYrW/AVUycEMR9BGxqVHc4JE458YTBZsTBED/Fg
# iIRUQwzXTbg4CLNC3ZOs1nMwVyaCo0UN0Or1R4HNvyRgMlhgRvJYR4YyhB50YWeR
# X4FUsc+TTJLBxKZd0WETbijGGvmGgLvfYfxGwScdJGcSchohiq9LZIlQYrFd/Xcf
# PfBXday9ikJNQFHRD5wGPmd/9WbAA5ZEfu/QS/1u5ZrKsajyeioKMfDaTgaRtogI
# Neh4HLDpmc085y9Euqf03GS9pAHBIAmTeM38vMDJRF1eFpwBBU8iTQIDAQABo4IB
# 5jCCAeIwEAYJKwYBBAGCNxUBBAMCAQAwHQYDVR0OBBYEFNVjOlyKMZDzQ3t8RhvF
# M2hahW1VMBkGCSsGAQQBgjcUAgQMHgoAUwB1AGIAQwBBMAsGA1UdDwQEAwIBhjAP
# BgNVHRMBAf8EBTADAQH/MB8GA1UdIwQYMBaAFNX2VsuP6KJcYmjRPZSQW9fOmhjE
# MFYGA1UdHwRPME0wS6BJoEeGRWh0dHA6Ly9jcmwubWljcm9zb2Z0LmNvbS9wa2kv
# Y3JsL3Byb2R1Y3RzL01pY1Jvb0NlckF1dF8yMDEwLTA2LTIzLmNybDBaBggrBgEF
# BQcBAQROMEwwSgYIKwYBBQUHMAKGPmh0dHA6Ly93d3cubWljcm9zb2Z0LmNvbS9w
# a2kvY2VydHMvTWljUm9vQ2VyQXV0XzIwMTAtMDYtMjMuY3J0MIGgBgNVHSABAf8E
# gZUwgZIwgY8GCSsGAQQBgjcuAzCBgTA9BggrBgEFBQcCARYxaHR0cDovL3d3dy5t
# aWNyb3NvZnQuY29tL1BLSS9kb2NzL0NQUy9kZWZhdWx0Lmh0bTBABggrBgEFBQcC
# AjA0HjIgHQBMAGUAZwBhAGwAXwBQAG8AbABpAGMAeQBfAFMAdABhAHQAZQBtAGUA
# bgB0AC4gHTANBgkqhkiG9w0BAQsFAAOCAgEAB+aIUQ3ixuCYP4FxAz2do6Ehb7Pr
# psz1Mb7PBeKp/vpXbRkws8LFZslq3/Xn8Hi9x6ieJeP5vO1rVFcIK1GCRBL7uVOM
# zPRgEop2zEBAQZvcXBf/XPleFzWYJFZLdO9CEMivv3/Gf/I3fVo/HPKZeUqRUgCv
# OA8X9S95gWXZqbVr5MfO9sp6AG9LMEQkIjzP7QOllo9ZKby2/QThcJ8ySif9Va8v
# /rbljjO7Yl+a21dA6fHOmWaQjP9qYn/dxUoLkSbiOewZSnFjnXshbcOco6I8+n99
# lmqQeKZt0uGc+R38ONiU9MalCpaGpL2eGq4EQoO4tYCbIjggtSXlZOz39L9+Y1kl
# D3ouOVd2onGqBooPiRa6YacRy5rYDkeagMXQzafQ732D8OE7cQnfXXSYIghh2rBQ
# Hm+98eEA3+cxB6STOvdlR3jo+KhIq/fecn5ha293qYHLpwmsObvsxsvYgrRyzR30
# uIUBHoD7G4kqVDmyW9rIDVWZeodzOwjmmC3qjeAzLhIp9cAvVCch98isTtoouLGp
# 25ayp0Kiyc8ZQU3ghvkqmqMRZjDTu3QyS99je/WZii8bxyGvWbWu3EQ8l1Bx16HS
# xVXjad5XwdHeMMD9zOZN+w2/XU/pnR4ZOC+8z1gFLu8NoFA12u8JJxzVs341Hgi6
# 2jbb01+P3nSISRKhggLOMIICNwIBATCB+KGB0KSBzTCByjELMAkGA1UEBhMCVVMx
# EzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQxHjAcBgNVBAoT
# FU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjElMCMGA1UECxMcTWljcm9zb2Z0IEFtZXJp
# Y2EgT3BlcmF0aW9uczEmMCQGA1UECxMdVGhhbGVzIFRTUyBFU046M0JCRC1FMzM4
# LUU5QTExJTAjBgNVBAMTHE1pY3Jvc29mdCBUaW1lLVN0YW1wIFNlcnZpY2WiIwoB
# ATAHBgUrDgMCGgMVAEQjn0R34rNHbg1CEPwsfWkeOCdCoIGDMIGApH4wfDELMAkG
# A1UEBhMCVVMxEzARBgNVBAgTCldhc2hpbmd0b24xEDAOBgNVBAcTB1JlZG1vbmQx
# HjAcBgNVBAoTFU1pY3Jvc29mdCBDb3Jwb3JhdGlvbjEmMCQGA1UEAxMdTWljcm9z
# b2Z0IFRpbWUtU3RhbXAgUENBIDIwMTAwDQYJKoZIhvcNAQEFBQACBQDh7X/WMCIY
# DzIwMjAwMjEyMDMzMjM4WhgPMjAyMDAyMTMwMzMyMzhaMHcwPQYKKwYBBAGEWQoE
# ATEvMC0wCgIFAOHtf9YCAQAwCgIBAAICGakCAf8wBwIBAAICEbIwCgIFAOHu0VYC
# AQAwNgYKKwYBBAGEWQoEAjEoMCYwDAYKKwYBBAGEWQoDAqAKMAgCAQACAwehIKEK
# MAgCAQACAwGGoDANBgkqhkiG9w0BAQUFAAOBgQB6XYmpXzuCuteL/rDs5cNRFvgq
# QlPdgd5+/kr0rfpqFXBiS0uWnufltczGk0NKNo2R8DwgGc8R/wW5H3CLhxrf7tgZ
# abDCXBYFzfSUPTwiKBP9EdViLTVrRG8VFBz/NW81xJZ1TpzMdMSTUxXAt/Ono8E2
# G2KbMlUmYOyq6ssgdDGCAw0wggMJAgEBMIGTMHwxCzAJBgNVBAYTAlVTMRMwEQYD
# VQQIEwpXYXNoaW5ndG9uMRAwDgYDVQQHEwdSZWRtb25kMR4wHAYDVQQKExVNaWNy
# b3NvZnQgQ29ycG9yYXRpb24xJjAkBgNVBAMTHU1pY3Jvc29mdCBUaW1lLVN0YW1w
# IFBDQSAyMDEwAhMzAAABHcLCCK4+uq5IAAAAAAEdMA0GCWCGSAFlAwQCAQUAoIIB
# SjAaBgkqhkiG9w0BCQMxDQYLKoZIhvcNAQkQAQQwLwYJKoZIhvcNAQkEMSIEILUC
# NXKl/YlyVfES+gN1+8nAYRzrfmlKh6H7dQ8DIuDtMIH6BgsqhkiG9w0BCRACLzGB
# 6jCB5zCB5DCBvQQgAr4Mw0Kfd3yHC65s6QntiApCRe1xG1Je3XJG6WXvmpMwgZgw
# gYCkfjB8MQswCQYDVQQGEwJVUzETMBEGA1UECBMKV2FzaGluZ3RvbjEQMA4GA1UE
# BxMHUmVkbW9uZDEeMBwGA1UEChMVTWljcm9zb2Z0IENvcnBvcmF0aW9uMSYwJAYD
# VQQDEx1NaWNyb3NvZnQgVGltZS1TdGFtcCBQQ0EgMjAxMAITMwAAAR3CwgiuPrqu
# SAAAAAABHTAiBCAFMqgDqqAtxrnQVE6L2luJBmrcNX+9s4uG8l2uA3wqKzANBgkq
# hkiG9w0BAQsFAASCAQAXtxamjeAHGlwUi4QIqORGyfoIKKnfnqwfbTNYDXO2pXCS
# UNgXF206NMkAc884mArjjzx6aJjFS6N9a1KOcxiPwK+3TnCBFHaeejGltrw5iSFb
# x7iwyblbL8j4tBl8oD9ZzmdiNLJqhJWtrAchsw2TMgjYu3iyQbr9y+THBxtcXI20
# dW3mHMPoHFSDT78vrJcAgvoY+Y5WMkOJwLwhcp0uaLPiN7tI1Fh3YBV6LZ/HYHRl
# qkKbpdwz0tuMm75Jt+UhQYjPmQ5pIQWJDQ42fRVn/T7u/A+BL64zGAUQuxdWOP+Y
# ugdowe4KGPFribJHCITEr5SnFnHdamGigpxdyYJb
# SIG # End signature block
