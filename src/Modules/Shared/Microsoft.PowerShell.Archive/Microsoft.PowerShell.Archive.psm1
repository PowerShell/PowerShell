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
    CreateFileAtExpandedPath=Created '{0}'.
    InvalidArchiveFilePathError=The archive file path '{0}' specified as input to the {1} parameter is resolving to multiple file system paths. Provide a unique path to the {2} parameter where the archive file has to be created.
    InvalidExpandedDirPathError=The directory path '{0}' specified as input to the DestinationPath parameter is resolving to multiple file system paths. Provide a unique path to the Destination parameter where the archive file contents have to be expanded.
    FileExistsError=Failed to create file '{0}' while expanding the archive file '{1}' contents as the file '{2}' already exists. Use the -Force parameter if you want to overwrite the existing directory '{3}' contents when expanding the archive file.
    DeleteArchiveFile=The partially created archive file '{0}' is deleted as it is not usable.
    InvalidDestinationPath=The destination path '{0}' does not contain a valid archive file name.
    PreparingToCompressVerboseMessage=Preparing to compress...
    PreparingToExpandVerboseMessage=Preparing to expand...
'@
}

Import-LocalizedData  LocalizedData -filename ArchiveResources

$zipFileExtension = ".zip"

<############################################################################################ 
# The Compress-Archive cmdlet can be used to zip/compress one or more files/directories.
############################################################################################>
function Compress-Archive
{
    [CmdletBinding(
    DefaultParameterSetName="Path", 
    SupportsShouldProcess=$true,
    HelpUri="https://go.microsoft.com/fwlink/?LinkID=393252")]
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
        $Force = $false  
    )

    BEGIN 
    {         
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

        $achiveFileName = [system.IO.Path]::GetFileName($DestinationPath)
        $destinationParentDir = GetResolvedPathHelper $destinationParentDir $false $PSCmdlet
        
        if($destinationParentDir.Count -gt 1)
        {
            $errorMessage = ($LocalizedData.InvalidArchiveFilePathError -f $DestinationPath, "DestinationPath", "DestinationPath")
            ThrowTerminatingErrorHelper "InvalidArchiveFilePath" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $DestinationPath
        }

        IsValidFileSystemPath $destinationParentDir | Out-Null
        $DestinationPath = Join-Path -Path $destinationParentDir -ChildPath $achiveFileName

        # GetExtension API does not validate for the actual existence of the path.
        $extension = [system.IO.Path]::GetExtension($DestinationPath)

        # If user does not specify .Zip extension, we append it.
        If($extension -eq [string]::Empty)
        {
            $DestinationPathWithOutExtension = $DestinationPath
            $DestinationPath = $DestinationPathWithOutExtension + $zipFileExtension   
            $appendArchiveFileExtensionMessage = ($LocalizedData.AppendArchiveFileExtensionMessage -f $DestinationPathWithOutExtension, $DestinationPath)
            Write-Verbose $appendArchiveFileExtensionMessage
        }
        else
        {
            # Invalid file extension is specified for the zip file to be created.
            if($extension -ne $zipFileExtension)
            {
                $errorMessage = ($LocalizedData.InvalidZipFileExtensionError -f $extension, $zipFileExtension)
                ThrowTerminatingErrorHelper "NotSupportedArchiveFileExtension" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $extension
            }
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
    HelpUri="https://go.microsoft.com/fwlink/?LinkID=393253")]    
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
        [switch] $Force
    )

    BEGIN 
    {   
       $isVerbose = $psboundparameters.ContainsKey("Verbose")
       $isConfirm = $psboundparameters.ContainsKey("Confirm")

        $isDestinationPathProvided = $true
        if($DestinationPath -eq [string]::Empty)
        {
            $resolvedDestinationPath = $pwd
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
                        $expandedItems | % { Remove-Item $_ -Force -Recurse }
                    }
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
    
    if ($PSEdition -eq "Desktop")
    {
        Add-Type -AssemblyName System.IO.Compression
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

        # If the directory is present at the drive level the DirectoryInfo.Parent include '\' example: C:\
        # On the other hand if the directory exists at a deper level then DirectoryInfo.Parent 
        # has just the path (without an ending '\'). example C:\source 
        if($sourceDirFullName.Length -eq 3)
        {
            $modifiedSourceDirFullName = $sourceDirFullName
        }
        else
        {
            $modifiedSourceDirFullName = $sourceDirFullName + ([io.path]::DirectorySeparatorChar)
        }
    }
    else
    {
        $sourceDirFullName = $sourceDirPath
        $modifiedSourceDirFullName = $sourceDirFullName + ([io.path]::DirectorySeparatorChar)
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
                $subDirFiles.Add($currentContent.FullName + ([io.path]::DirectorySeparatorChar))
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
                    if($currentArchiveEntry.FullName -eq $relativeFilePath)
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
            # to end with '\' to detect the path as an directory.
            if(!$relativeFilePath.EndsWith(([io.path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase))
            {
                try
                {
                    try
                    {
                        $currentFileStream = [System.IO.File]::Open($currentFilePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
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

                        $currentArchiveEntry = $zipArchive.CreateEntry($relativeFilePath, $compression)

                        # Updating  the File Creation time so that the same timestamp would be retained after expanding the compressed file. 
                        # At this point we are sure that Get-ChildItem would succeed.
                        $currentArchiveEntry.LastWriteTime = (Get-Item -LiteralPath $currentFilePath).LastWriteTime

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
                $currentArchiveEntry = $zipArchive.CreateEntry("$relativeFilePath", $compression)
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

    if([System.IO.File]::Exists($archiveFile))
    {
        $extension = [system.IO.Path]::GetExtension($archiveFile)

        # Invalid file extension is specified for the zip file.
        if($extension -ne $zipFileExtension)
        {
            $errorMessage = ($LocalizedData.InvalidZipFileExtensionError -f $extension, $zipFileExtension)
            ThrowTerminatingErrorHelper "NotSupportedArchiveFileExtension" $errorMessage ([System.Management.Automation.ErrorCategory]::InvalidArgument) $extension
        }
    }
    else
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
        $zipArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $zipArchiveArgs

        if($zipArchive.Entries.Count -eq 0)
        {
            $archiveFileIsEmpty = ($LocalizedData.ArchiveFileIsEmpty -f $archiveFile)
            Write-Verbose $archiveFileIsEmpty
            return
        }

        $currentEntryCount = 0
        $progressBarStatus = ($LocalizedData.ExpandProgressBarText -f $archiveFile)

        # The archive entries can either be empty directories or files.
        foreach($currentArchiveEntry in $zipArchive.Entries)
        {
            $currentArchiveEntryPath = Join-Path -Path $expandedDir -ChildPath $currentArchiveEntry.FullName 
            $extension = [system.IO.Path]::GetExtension($currentArchiveEntryPath)

            # The current archive entry is an empty directory
            # The FullName of the Archive Entry representing a directory would end with a trailing '\'.
            if($extension -eq [string]::Empty -and 
            $currentArchiveEntryPath.EndsWith(([io.path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase))
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
