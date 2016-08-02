<############################################################################################ 
 # File: Pester.Commands.Cmdlets.ArchiveTests.ps1
 # Commands.Cmdlets.ArchiveTests suite contains Tests that are
 # used for validating Microsoft.PowerShell.Archive module.
 ############################################################################################>
$script:TestSourceRoot = $PSScriptRoot
Describe "Test suite for Microsoft.PowerShell.Archive module" -Tags "CI" {

    AfterAll {
        $global:ProgressPreference = $_progressPreference
        $env:PSMODULEPATH = $_modulePath 
    }
    BeforeAll {
        # remove the archive module forcefully, to be sure we get the correct version
        if ( Get-Module Microsoft.PowerShell.Archive. ) {
            Remove-Module Microsoft.PowerShell.Archive -force
        }
        # Version comparisons should use a System.Version rather than SemanticVersion
        $PSVersion = $PSVersionTable.PSVersion -as [Version]
        # Write-Progress not supported yet on Core
        $_progressPreference = $ProgressPreference
        # we need to be sure that we get the correct archive module
        $_modulePath = $env:PSMODULEPATH
        $powershellexe = (get-process -pid $PID).MainModule.FileName
        $env:PSMODULEPATH = join-path ([io.path]::GetDirectoryName($powershellexe)) Modules
        if ( $IsCoreCLR ) { $global:ProgressPreference = "SilentlyContinue" }
        
        Setup -d SourceDir
        Setup -d SourceDir/ChildDir-1
        Setup -d SourceDir/ChildDir-2
        Setup -d SourceDir/ChildEmptyDir
        
        $content = "Some Data"
        $Files = ( [io.path]::Combine("SourceDir","Sample-1.txt")), ([io.path]::Combine("SourceDir","Sample-2.txt")),
            ([io.path]::Combine("SourceDir","ChildDir-1","Sample-3.txt")), ([io.path]::Combine("SourceDir","ChildDir-1","Sample-4.txt")),
            ([io.path]::Combine("SourceDir","ChildDir-2","Sample-5.txt")), ([io.path]::Combine("SourceDir","ChildDir-2","Sample-6.txt"))

        foreach($file in $files ) {
            Setup -f $file -content $content
        }

        Setup -f Sample.unzip -content "Some Text"
        Setup -f Sample.cab -content "Some Text"

        $preCreatedArchivePath = Join-Path $script:TestSourceRoot "SamplePreCreatedArchive.archive"
        Copy-Item $preCreatedArchivePath $TestDrive/SamplePreCreatedArchive.zip -Force
    }

    function Add-CompressionAssemblies {
        Add-Type -AssemblyName System.IO.Compression
        if (($psedition -eq "Core") -or $IsCoreCLR )
        {
            Add-Type -AssemblyName System.IO.Compression.ZipFile
        }
        else
        {
            Add-Type -AssemblyName System.IO.Compression.FileSystem
        }
    }

    function CompressArchivePathParameterSetValidator {
        param 
        (
            [string[]] $path,
            [string] $destinationPath,
            [string] $compressionLevel = "Optimal"
        )

        try
        {
            Compress-Archive -Path $path -DestinationPath $destinationPath -CompressionLevel $compressionLevel
            trow "ValidateNotNullOrEmpty attribute is missing on one of parameters belonging to Path parameterset."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationError,Compress-Archive"
        }
    }

    function CompressArchiveLiteralPathParameterSetValidator {
        param 
        (
            [string[]] $literalPath,
            [string] $destinationPath,
            [string] $compressionLevel = "Optimal"
        )

        try
        {
            Compress-Archive -LiteralPath $literalPath -DestinationPath $destinationPath -CompressionLevel $compressionLevel
            throw "ValidateNotNullOrEmpty attribute is missing on one of parameters belonging to LiteralPath parameterset."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "ParameterArgumentValidationError,Compress-Archive"
        }
    }

    
    function CompressArchiveInValidPathValidator {
        param 
        (
            [string[]] $path,
            [string] $destinationPath,
            [string] $invalidPath,
            [string] $expectedFullyQualifiedErrorId
        )
        
        try
        {   
            Compress-Archive -Path $path -DestinationPath $destinationPath           
            throw "Failed to validate that an invalid Path $invalidPath was supplied as input to Compress-Archive cmdlet."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be $expectedFullyQualifiedErrorId
        }
    }

    function CompressArchiveInValidArchiveFileExtensionValidator {
        param 
        (
            [string[]] $path,
            [string] $destinationPath,
            [string] $invalidArchiveFileExtension
        )

        try
        {
            Compress-Archive -Path $path -DestinationPath $destinationPath             
            throw "Failed to validate that an invalid archive file format $invalidArchiveFileExtension was supplied as input to Compress-Archive cmdlet."
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be "NotSupportedArchiveFileExtension,Compress-Archive"
        }
    }

    function Validate-ArchiveEntryCount {
        param 
        (
            [string] $path,
            [int] $expectedEntryCount
        )

        Add-CompressionAssemblies
        try
        {
            $archiveFileStreamArgs = @($path, [System.IO.FileMode]::Open)
            $archiveFileStream = New-Object -TypeName System.IO.FileStream -ArgumentList $archiveFileStreamArgs
    
            $zipArchiveArgs = @($archiveFileStream, [System.IO.Compression.ZipArchiveMode]::Read, $false)
            $zipArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $zipArchiveArgs
    
            $actualEntryCount = $zipArchive.Entries.Count
            $actualEntryCount | Should Be $expectedEntryCount
        }
        finally
        {
            if ($null -ne $zipArchive) { $zipArchive.Dispose()}
            if ($null -ne $archiveFileStream) { $archiveFileStream.Dispose() }
        }
    }
    
    function ArchiveFileEntryContentValidator {
        param 
        (
            [string] $path,
            [string] $entryFileName,
            [string] $expectedEntryFileContent
        )
        
        Add-CompressionAssemblies
        try
        {
            $destFile = "$TestDrive/ExpandedFile"+([System.Guid]::NewGuid().ToString())+".txt"
    
            $archiveFileStreamArgs = @($path, [System.IO.FileMode]::Open)
            $archiveFileStream = New-Object -TypeName System.IO.FileStream -ArgumentList $archiveFileStreamArgs
    
            $zipArchiveArgs = @($archiveFileStream, [System.IO.Compression.ZipArchiveMode]::Read, $false)
            $zipArchive = New-Object -TypeName System.IO.Compression.ZipArchive -ArgumentList $zipArchiveArgs
    
            $entryToBeUpdated = $zipArchive.Entries | ? {$_.FullName -eq $entryFileName}
            
            if($entryToBeUpdated -ne $null)
            {
                $srcStream = $entryToBeUpdated.Open()
                $destStream = New-Object "System.IO.FileStream" -ArgumentList( $destFile, [System.IO.FileMode]::Create )
                $srcStream.CopyTo( $destStream )
                $destStream.Dispose()
                $srcStream.Dispose()
                Get-Content $destFile | Should Be $expectedEntryFileContent
            }
            else
            {
                throw "Failed to find the file $entryFileName in the archive file $path"
            }
        }
        finally
        {
            if ($zipArchive)
            {
                $zipArchive.Dispose()
            }
            if ($archiveFileStream)
            {
                $archiveFileStream.Dispose()
            }
        }
    }

    function ExpandArchiveInvalidParameterValidator {
        param 
        (
            [boolean] $isLiteralPathParameterSet,
            [string[]] $path,
            [string] $destinationPath,
            [string] $expectedFullyQualifiedErrorId
        )

        try
        {
            if($isLiteralPathParameterSet)
            {
                Expand-Archive -LiteralPath $literalPath -DestinationPath $destinationPath
            }
            else
            { 
                Expand-Archive -Path $path -DestinationPath $destinationPath
            }

            throw "Expand-Archive did NOT throw expected error"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should Be $expectedFullyQualifiedErrorId
        }
    }

    Context "Compress-Archive - Parameter validation test cases" {
        
        It "Validate errors from Compress-Archive with NULL & EMPTY values for Path, LiteralPath, DestinationPath, CompressionLevel parameters" {
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "$TestDrive/SampleSingleFile.zip"

            CompressArchivePathParameterSetValidator $null $destinationPath
            CompressArchivePathParameterSetValidator $sourcePath $null
            CompressArchivePathParameterSetValidator $null $null

            CompressArchivePathParameterSetValidator "" $destinationPath
            CompressArchivePathParameterSetValidator $sourcePath ""
            CompressArchivePathParameterSetValidator "" ""

            CompressArchivePathParameterSetValidator $null $null "NoCompression"

            CompressArchiveLiteralPathParameterSetValidator $null $destinationPath
            CompressArchiveLiteralPathParameterSetValidator $sourcePath $null
            CompressArchiveLiteralPathParameterSetValidator $null $null

            CompressArchiveLiteralPathParameterSetValidator "" $destinationPath
            CompressArchiveLiteralPathParameterSetValidator $sourcePath ""
            CompressArchiveLiteralPathParameterSetValidator "" ""

            CompressArchiveLiteralPathParameterSetValidator $null $null "NoCompression"

            CompressArchiveLiteralPathParameterSetValidator $sourcePath $destinationPath $null
            CompressArchiveLiteralPathParameterSetValidator $sourcePath $destinationPath ""
        }
        
        It "Validate errors from Compress-Archive when invalid path (non-existing path / non-filesystem path) is supplied for Path or LiteralPath parameters" {
            CompressArchiveInValidPathValidator "$TestDrive/InvalidPath" $TestDrive "$TestDrive/InvalidPath" "ArchiveCmdletPathNotFound,Compress-Archive"
            if ( ! $IsCoreCLR ) {
                CompressArchiveInValidPathValidator "HKLM:/SOFTWARE" $TestDrive "HKLM:/SOFTWARE" "PathNotFound,Compress-Archive"
            }
            CompressArchiveInValidPathValidator "$TestDrive" "$TestDrive/NonExistingDirectory/sample.zip" "$TestDrive/NonExistingDirectory/sample.zip" "ArchiveCmdletPathNotFound,Compress-Archive"

            $path = @("$TestDrive", "$TestDrive/InvalidPath")
            CompressArchiveInValidPathValidator $path $TestDrive "$TestDrive/InvalidPath" "ArchiveCmdletPathNotFound,Compress-Archive"

            if ( ! $IsCoreCLR ) {
                $path = @("$TestDrive", "HKLM:/SOFTWARE")
                CompressArchiveInValidPathValidator $path $TestDrive "HKLM:/SOFTWARE" "PathNotFound,Compress-Archive"
            }

            $invalidUnZipFileFormat = "$TestDrive/Sample.unzip"
            CompressArchiveInValidArchiveFileExtensionValidator $TestDrive "$invalidUnZipFileFormat" ".unzip"
            
            $invalidcabZipFileFormat = "$TestDrive/Sample.cab"
            CompressArchiveInValidArchiveFileExtensionValidator $TestDrive "$invalidcabZipFileFormat" ".cab"
        }

        It "Validate error from Compress-Archive when archive file already exists and -Update parameter is not specified" {
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "$TestDrive/ValidateErrorWhenUpdateNotSpecified.zip"

            try
            {
                "Some Data" > $destinationPath
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                throw "Failed to validate that an archive file format $destinationPath already exists and -Update switch parameter is not specified while running Compress-Archive command."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "ArchiveFileExists,Compress-Archive"
            }
        }

        It "Validate error from Compress-Archive when duplicate paths are supplied as input to Path parameter" {
            $sourcePath = @(
                "$TestDrive/SourceDir/Sample-1.txt", 
                "$TestDrive/SourceDir/Sample-1.txt")
            $destinationPath = "$TestDrive/DuplicatePaths.zip"

            try
            {
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath         
                throw "Failed to detect that duplicate Path $sourcePath is supplied as input to Path parameter."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "DuplicatePathFound,Compress-Archive"
            }
        }

        It "Validate error from Compress-Archive when duplicate paths are supplied as input to LiteralPath parameter" {
            $sourcePath = @(
                "$TestDrive/SourceDir/Sample-1.txt", 
                "$TestDrive/SourceDir/Sample-1.txt")
            $destinationPath = "$TestDrive/DuplicatePaths.zip"

            try
            {
                Compress-Archive -LiteralPath $sourcePath -DestinationPath $destinationPath
                throw "Failed to detect that duplicate Path $sourcePath is supplied as input to LiteralPath parameter."
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "DuplicatePathFound,Compress-Archive"
            }
        }

        It "Validate that relative path can be specified as Path parameter of Compress-Archive cmdlet" {
            $sourcePath = "./SourceDir"
            $destinationPath = "RelativePathForPathParameter.zip"
            try {
                push-location $TESTDRIVE
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                Test-Path $destinationPath | Should Be $true
            }
            finally {
                Pop-Location
            }
        }
        It "Validate that relative path can be specified as LiteralPath parameter of Compress-Archive cmdlet" {
            $sourcePath = "./SourceDir"
            $destinationPath = "RelativePathForLiteralPathParameter.zip"
            try {
                push-location $TESTDRIVE
                Compress-Archive -LiteralPath $sourcePath -DestinationPath $destinationPath
                Test-Path $destinationPath | Should Be $true
            }
            finally {
                Pop-Location
            }
        }
        It "Validate that relative path can be specified as DestinationPath parameter of Compress-Archive cmdlet" {
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "./RelativePathForDestinationPathParameter.zip"
            try {
                push-location $TESTDRIVE
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                Test-Path $destinationPath | Should Be $true
            }
            finally {
                Pop-Location
            }
        }
    }

    Context "Compress-Archive - functional test cases" {
        It "Validate that a single file can be compressed using Compress-Archive cmdlet" {
            $sourcePath = [io.path]::Combine("$TestDrive","SourceDir","ChildDir-1","Sample-3.txt")
            $destinationPath = [io.path]::Combine("$TestDrive","SampleSingleFile.zip")
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
            $destinationPath | Should Exist
        }
        # This test requires a fix in PS5 to support reading paths with square bracket
        It "Validate that Compress-Archive cmdlet can accept LiteralPath parameter with Special Characters" -skip:($PSVersion -lt "5.0") {
            $sourcePath = "$TestDrive/SourceDir/ChildDir-1/Sample[]File.txt"
            "Some Random Content" | Out-File -LiteralPath $sourcePath
            $destinationPath = "$TestDrive/SampleSingleFileWithSpecialCharacters.zip"
            try
            {
                Compress-Archive -LiteralPath $sourcePath -DestinationPath $destinationPath
                $destinationPath | Should Exist
            }
            finally
            {
                get-item -literalPath $sourcePath | remove-Item -force
                # Remove-Item -LiteralPath $sourcePath -Force
            }
        }
        It "Validate that Compress-Archive cmdlet errors out when DestinationPath resolves to multiple locations" {

            New-Item $TestDrive/SampleDir/Child-1 -Type Directory -Force | Out-Null
            New-Item $TestDrive/SampleDir/Child-2 -Type Directory -Force | Out-Null
            New-Item $TestDrive/SampleDir/Test.txt -Type File -Force | Out-Null

            $destinationPath = "$TestDrive/SampleDir/Child-*/SampleChidArchive.zip"
            $sourcePath = "$TestDrive/SampleDir/Test.txt"
            try
            {
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                throw "Failed to detect that destination $destinationPath can resolve to multiple paths"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "InvalidArchiveFilePath,Compress-Archive"
            }
            finally
            {
                Remove-Item -LiteralPath $TestDrive/SampleDir -Force -Recurse
            }
        }
        It "Validate that Compress-Archive cmdlet works when DestinationPath has wild card pattern and resolves to a single valid path" {

            New-Item $TestDrive/SampleDir/Child-1 -Type Directory -Force | Out-Null
            New-Item $TestDrive/SampleDir/Test.txt -Type File -Force | Out-Null

            $destinationPath = "$TestDrive/SampleDir/Child-*/SampleChidArchive.zip"
            $sourcePath = "$TestDrive/SampleDir/Test.txt"
            try
            {
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                $destinationPath | Should Exist
            }
            finally
            {
                Remove-Item -LiteralPath $TestDrive/SampleDir -Force -Recurse
            }
        }
        # This test requires a fix in PS5 to support reading paths with square bracket
        It "Validate that Compress-Archive cmdlet can accept LiteralPath parameter for a directory with Special Characters in the directory name" -skip:($PSVersion -lt "5.0") {
            $sourcePath = "$TestDrive/Source[]Dir/ChildDir[]-1"
            New-Item $sourcePath -Type Directory | Out-Null
            "Some Random Content" | Out-File -LiteralPath "$sourcePath/Sample[]File.txt"
            $destinationPath = "$TestDrive/SampleDirWithSpecialCharacters.zip"
            try
            {
                Compress-Archive -LiteralPath $sourcePath -DestinationPath $destinationPath
                $destinationPath | Should Exist
            }
            finally
            {
                get-item -LiteralPath $sourcePath | Remove-Item -Force -Recurse
                # Remove-Item -LiteralPath $sourcePath -Force -Recurse
            }
        }
        It "Validate that Compress-Archive cmdlet can accept DestinationPath parameter with Special Characters" {
            $sourcePath = "$TestDrive/SourceDir/ChildDir-1/Sample-3.txt"
            $destinationPath = "$TestDrive/Sample[]SingleFile.zip"
            try
            {
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	    Test-Path -LiteralPath $destinationPath | Should Be $true
            }
            finally
            {
                Get-Item -LiteralPath $destinationPath | Remove-Item -Force
                # Remove-Item -LiteralPath $destinationPath -Force
            }
        }
        It "Validate that Source Path can be at SystemDrive location" -skip:($IsCoreCLR) {
            $sourcePath = "$env:SystemDrive/SourceDir"
            $destinationPath = "$TestDrive/SampleFromSystemDrive.zip"
            New-Item $sourcePath -Type Directory | Out-Null
            "Some Data" | Out-File -FilePath $sourcePath/SampleSourceFileForArchive.txt
            try
            {
                Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
                Test-Path $destinationPath | Should Be $true
            }
            finally
            {
                remove-item "$sourcePath" -Force -Recurse -ErrorAction SilentlyContinue
            }
        }
        It "Validate that multiple files can be compressed using Compress-Archive cmdlet" {
            $sourcePath = @(
                "$TestDrive/SourceDir/ChildDir-1/Sample-3.txt", 
                "$TestDrive/SourceDir/ChildDir-1/Sample-4.txt", 
                "$TestDrive/SourceDir/ChildDir-2/Sample-5.txt",
                "$TestDrive/SourceDir/ChildDir-2/Sample-6.txt")
            $destinationPath = "$TestDrive/SampleMultipleFiles.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }
        It "Validate that multiple files and directories can be compressed using Compress-Archive cmdlet" {
            $sourcePath = @(
                "$TestDrive/SourceDir/Sample-1.txt", 
                "$TestDrive/SourceDir/Sample-2.txt", 
                "$TestDrive/SourceDir/ChildDir-1", 
                "$TestDrive/SourceDir/ChildDir-2")
            $destinationPath = "$TestDrive/SampleMultipleFilesAndDirs.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }
        It "Validate that a single directory can be compressed using Compress-Archive cmdlet" {
            $sourcePath = @("$TestDrive/SourceDir/ChildDir-1")
            $destinationPath = "$TestDrive/SampleSingleDir.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }
        It "Validate that a single directory with multiple files and subdirectories can be compressed using Compress-Archive cmdlet" {
            $sourcePath = @("$TestDrive/SourceDir")
            $destinationPath = "$TestDrive/SampleSubTree.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }
        It "Validate that a single directory & multiple files can be compressed using Compress-Archive cmdlet" {
            $sourcePath = @(
                "$TestDrive/SourceDir/ChildDir-1", 
                "$TestDrive/SourceDir/Sample-1.txt", 
                "$TestDrive/SourceDir/Sample-2.txt")
            $destinationPath = "$TestDrive/SampleMultipleFilesAndSingleDir.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }

        It "Validate that if .zip extension is not supplied as input to DestinationPath parameter, then .zip extension is appended" {
            $sourcePath = @("$TestDrive/SourceDir")
            $destinationPath = "$TestDrive/SampleNoExtension.zip"
            $destinationWithoutExtensionPath = "$TestDrive/SampleNoExtension"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationWithoutExtensionPath
        	Test-Path $destinationPath | Should Be $true
        }

        It "Validate that -Update parameter makes Compress-Archive to not throw an error if archive file already exists" {
            $sourcePath = @("$TestDrive/SourceDir")
            $destinationPath = "$TestDrive/SampleUpdateTest.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -Update
        	Test-Path $destinationPath | Should Be $true
        }
        It "Validate -Update parameter by adding a new file to an existing archive file" {
            $sourcePath = @("$TestDrive/SourceDir/ChildDir-1")
            $destinationPath = "$TestDrive/SampleUpdateAdd1File.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
            New-Item $TestDrive/SourceDir/ChildDir-1/Sample-AddedNewFile.txt -Type File | Out-Null
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -Update
            Test-Path $destinationPath | Should Be $true
            Validate-ArchiveEntryCount -path $destinationPath -expectedEntryCount 3
        }

        It "Validate that all CompressionLevel values can be used with Compress-Archive cmdlet" {
            $sourcePath = "$TestDrive/SourceDir/Sample-1.txt"
            
            $destinationPath = "$TestDrive/FastestCompressionLevel.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -CompressionLevel Fastest
            Test-Path $destinationPath | Should Be $true

            $destinationPath = "$TestDrive/OptimalCompressionLevel.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -CompressionLevel Optimal
            Test-Path $destinationPath | Should Be $true

            $destinationPath = "$TestDrive/NoCompressionCompressionLevel.zip"
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -CompressionLevel NoCompression
            Test-Path $destinationPath | Should Be $true
        }

        It "Validate that -Update parameter is modifying a file that already exists in the archive file" {
            $filePath = "$TestDrive/SourceDir/ChildDir-1/Sample-3.txt"

            $initialContent = "Initial Content"
            $modifiedContent = "Modified Content"
    
            $initialContent | Set-Content $filePath
    
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "$TestDrive/UpdatingModifiedFile.zip"
                    
            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
            Test-Path $destinationPath | Should Be $True

            $modifiedContent | Set-Content $filePath

            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath -Update
            Test-Path $destinationPath | Should Be $True
    
            ArchiveFileEntryContentValidator "$destinationPath" ([io.path]::Combine("SourceDir","ChildDir-1","Sample-3.txt")) $modifiedContent
        }
        
        It "Validate Compress-Archive cmdlet in pipleline scenario" {
            $destinationPath = "$TestDrive/CompressArchiveFromPipeline.zip"

            # Piping a single file path to Compress-Archive
            dir -Path $TestDrive/SourceDir/Sample-1.txt | Compress-Archive -DestinationPath $destinationPath
            Test-Path $destinationPath | Should Be $True

            # Piping a string directory path to Compress-Archive
            "$TestDrive/SourceDir/ChildDir-2" | Compress-Archive -DestinationPath $destinationPath -Update
            Test-Path $destinationPath | Should Be $True

            # Piping the output of Get-ChildItem to Compress-Archive
            dir "$TestDrive/SourceDir" -Recurse | Compress-Archive -DestinationPath $destinationPath -Update
            Test-Path $destinationPath | Should Be $True
        }

        It "Validate that Compress-Archive works on ReadOnly files" {
            $sourcePath = "$TestDrive/ReadOnlyFile.txt"
            $destinationPath = "$TestDrive/TestForReadOnlyFile.zip"

            "Some Content" | Out-File -FilePath $sourcePath
            $createdItem = Get-Item $sourcePath
            $createdItem.Attributes = 'ReadOnly'

            Compress-Archive -Path $sourcePath -DestinationPath $destinationPath
        	Test-Path $destinationPath | Should Be $true
        }

        It "Validate that Compress-Archive generates Verbose messages" {
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "$TestDrive/Compress-Archive generates VerboseMessages.zip"
            
            try
            {   
                $ps=[PowerShell]::Create()
                $ps.Streams.Error.Clear()
                $ps.Streams.Verbose.Clear()
                $script = "Import-Module Microsoft.PowerShell.Archive; Compress-Archive -Path $sourcePath -DestinationPath `"$destinationPath`" -CompressionLevel Fastest -Verbose"
                $ps.AddScript($script)
                $ps.Invoke()

                $ps.Streams.Verbose.Count -gt 0 | Should Be $True
                $ps.Streams.Error.Count | Should Be 0
            }
            finally
            {
                $ps.Dispose()
            }
        }
    }

    Context "Expand-Archive - Parameter validation test cases" {
        It "Validate non existing archive -Path trows expected error message" {
            $sourcePath = "$TestDrive/SourceDir"
            $destinationPath = "$TestDrive/ExpandedArchive"
            try
            {   
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath
        		throw "Expand-Archive succeeded for non existing archive path"
            }
            catch
            {
        		$_.FullyQualifiedErrorId | Should Be "PathNotFound,Expand-Archive"
            }
        }

        It "Validate errors from Expand-Archive with NULL & EMPTY values for Path, LiteralPath, DestinationPath parameters" {
            ExpandArchiveInvalidParameterValidator $false $null "$TestDrive/SourceDir" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $false $null $null "ParameterArgumentValidationError,Expand-Archive"

            ExpandArchiveInvalidParameterValidator $false "$TestDrive/SourceDir" $null "ParameterArgumentTransformationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $false "" "$TestDrive/SourceDir" "ParameterArgumentTransformationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $false "$TestDrive/SourceDir" "" "ParameterArgumentTransformationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $false "" "" "ParameterArgumentTransformationError,Expand-Archive"

            ExpandArchiveInvalidParameterValidator $true $null "$TestDrive/SourceDir" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true $null $null "ParameterArgumentValidationError,Expand-Archive"

            ExpandArchiveInvalidParameterValidator $true "$TestDrive/SourceDir" $null "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "" "$TestDrive/SourceDir" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "$TestDrive/SourceDir" "" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "" "" "ParameterArgumentValidationError,Expand-Archive"

            ExpandArchiveInvalidParameterValidator $true $null "$TestDrive/SourceDir" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true $null $null "ParameterArgumentValidationError,Expand-Archive"

            ExpandArchiveInvalidParameterValidator $true "$TestDrive/SourceDir" $null "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "" "$TestDrive/SourceDir" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "$TestDrive/SourceDir" "" "ParameterArgumentValidationError,Expand-Archive"
            ExpandArchiveInvalidParameterValidator $true "" "" "ParameterArgumentValidationError,Expand-Archive"
        }

        It "Validate errors from Expand-Archive when invalid path (non-existing path / non-filesystem path) is supplied for Path or LiteralPath parameters" {
            try { Expand-Archive -Path "$TestDrive/NonExistingArchive" -DestinationPath "$TestDrive/SourceDir"; throw "Expand-Archive did NOT throw expected error" }
            catch { $_.FullyQualifiedErrorId | Should Be "ArchiveCmdletPathNotFound,Expand-Archive" }

            if ( ! $IsCoreCLR ) {
                try { Expand-Archive -Path "HKLM:/SOFTWARE" -DestinationPath "$TestDrive/SourceDir"; throw "Expand-Archive did NOT throw expected error" }
                catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Expand-Archive" }
            }

            try { Expand-Archive -LiteralPath "$TestDrive/NonExistingArchive" -DestinationPath "$TestDrive/SourceDir"; throw "Expand-Archive did NOT throw expected error" }
            catch { $_.FullyQualifiedErrorId | Should Be "ArchiveCmdletPathNotFound,Expand-Archive" }

            if ( ! $IsCoreCLR ) {
                try { Expand-Archive -LiteralPath "HKLM:/SOFTWARE" -DestinationPath "$TestDrive/SourceDir"; throw "Expand-Archive did NOT throw expected error" }
                catch { $_.FullyQualifiedErrorId | Should Be "PathNotFound,Expand-Archive" }
            }
        }

        It "Validate error from Expand-Archive when invalid path (non-existing path / non-filesystem path) is supplied for DestinationPath parameter" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            # $destinationPath = "HKLM:/SOFTWARE"
            $destinationPath = "Variable:/"

            try { Expand-Archive -Path $sourcePath -DestinationPath $destinationPath; throw "Expand-Archive did NOT throw expected error" }
            catch { $_.FullyQualifiedErrorId | Should Be "InvalidDirectoryPath,Expand-Archive" }
        }
    }

    Context "Expand-Archive - functional test cases" {
        It "Validate basic Expand-Archive scenario" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $content = "Some Data"
            $destinationPath = "$TestDrive/DestDirForBasicExpand"
            $files = @("Sample-1.txt", "Sample-2.txt")

            # The files in "$TestDrive/SamplePreCreatedArchive.zip" are precreated.
            $fileCreationTimeStamp = Get-Date -Year 2014 -Month 6 -Day 13 -Hour 15 -Minute 50 -Second 20 -Millisecond 0

            Expand-Archive -Path $sourcePath -DestinationPath $destinationPath
            foreach($currentFile in $files)
            {
                $expandedFile = Join-Path $destinationPath -ChildPath $currentFile
                Test-Path $expandedFile | Should Be $True

                # We are validating to make sure that time stamps are preserved in the 
                # compressed archive are reflected back when the file is expanded. 
                (dir $expandedFile).LastWriteTime.CompareTo($fileCreationTimeStamp) | Should Be 0
                
                Get-Content $expandedFile | Should Be $content
            }
        }
        It "Validate that Expand-Archive cmdlet errors out when DestinationPath resolves to multiple locations" {
            $testbasename = "TargetDir"
            setup -d "$testbasename"
            setup -d "$testbasename/Child-1"
            setup -d "$testbasename/Child-2"

            $destinationPath = [io.path]::Combine("$TestDrive","$testbasename","Child-*")
            $sourcePath = join-path "$TestDrive" "SamplePreCreatedArchive.zip"
            try
            {
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath
                throw "Failed to detect that destination $destinationPath can resolve to multiple paths"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "InvalidDestinationPath,Expand-Archive"
            }
            finally
            {
                Remove-Item -LiteralPath "$TestDrive/$testbasename" -Force -Recurse
            }
        }
        It "Validate that Expand-Archive cmdlet works when DestinationPath resolves has wild card pattern and resolves to a single valid path" {
            $testbasename = "TargetDir"
            setup -d "$testbasename"
            setup -d "$testbasename/Child-1"

            $destinationPath = [io.path]::Combine("$TestDrive","$testbasename","Child-*")
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            try
            {
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath
                $expandedFiles = Get-ChildItem $destinationPath -Recurse
                $expandedFiles.Length | Should BeGreaterThan 1       
            }
            finally
            {
                Remove-Item -LiteralPath "$TestDrive/$testbasename" -Force -Recurse
            }
        }
        It "Validate Expand-Archive scenario where DestinationPath has Special Characters" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $content = "Some Data"
            $destinationPath = "$TestDrive/DestDir[]Expand"
            $files = @("Sample-1.txt", "Sample-2.txt")

            # The files in "$TestDrive/SamplePreCreatedArchive.zip" are precreated.
            $fileCreationTimeStamp = Get-Date -Year 2014 -Month 6 -Day 13 -Hour 15 -Minute 50 -Second 20 -Millisecond 0

            Expand-Archive -Path $sourcePath -DestinationPath $destinationPath
            foreach($currentFile in $files)
            {
                $expandedFile = Join-Path $destinationPath -ChildPath $currentFile
                Test-Path -LiteralPath $expandedFile | Should Be $True

                # We are validating to make sure that time stamps are preserved in the 
                # compressed archive are reflected back when the file is expanded. 
                (dir -LiteralPath $expandedFile).LastWriteTime.CompareTo($fileCreationTimeStamp) | Should Be 0
                
                Get-Content -LiteralPath $expandedFile | Should Be $content
            }
        }
        It "Invoke Expand-Archive with relative path in Path parameter and -Force parameter" {
            $sourcePath = "./SamplePreCreatedArchive.zip"
            $destinationPath = "$TestDrive/SomeOtherNonExistingDir/Path"
            try
            {
                Push-Location $TestDrive
                
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath -Force
                $expandedFiles = Get-ChildItem $destinationPath -Recurse
                $expandedFiles.Length | Should Be 2
            }
            finally
            {
                Pop-Location
            }
        }

        It "Invoke Expand-Archive with relative path in LiteralPath parameter and -Force parameter" {
            $sourcePath = "./SamplePreCreatedArchive.zip"
            $destinationPath = "$TestDrive/SomeOtherNonExistingDir/LiteralPath"
            try
            {
                Push-Location $TestDrive
                
                Expand-Archive -LiteralPath $sourcePath -DestinationPath $destinationPath -Force
                $expandedFiles = Get-ChildItem $destinationPath -Recurse
                $expandedFiles.Length | Should Be 2
            }
            finally
            {
                Pop-Location
            }
        }

        It "Invoke Expand-Archive with non-existing relative directory in DestinationPath parameter and -Force parameter" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $destinationPath = "./SomeOtherNonExistingDir/DestinationPath"
            try
            {
                Push-Location $TestDrive
                
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath -Force
                $expandedFiles = Get-ChildItem $destinationPath -Recurse
                $expandedFiles.Length | Should Be 2
            }
            finally
            {
                Pop-Location
            }
        }

        It "Invoke Expand-Archive with unsupported archive format" {
            $sourcePath = "$TestDrive/Sample.cab"
            $destinationPath = "$TestDrive/UnsupportedArchiveFormatDir"
            try
            {
                Expand-Archive -Path $sourcePath -DestinationPath $destinationPath -Force
                throw "Failed to detect unsupported archive format at $sourcePath"
            }
            catch
            {
                $_.FullyQualifiedErrorId | Should Be "NotSupportedArchiveFileExtension,Expand-Archive"
            }
        }

        It "Invoke Expand-Archive with archive file containing multiple files, directories with subdirectories and empty directories" {
            $sourcePath = "$TestDrive/SourceDir"
            $archivePath = "$TestDrive/FileAndDirTreeForExpand.zip"
            $destinationPath = "$TestDrive/FileAndDirTree"
            $sourceList = dir $sourcePath -Name

            Add-CompressionAssemblies
            [System.IO.Compression.ZipFile]::CreateFromDirectory($sourcePath, $archivePath)

            Expand-Archive -Path $archivePath -DestinationPath $destinationPath
            $extractedList = dir $destinationPath -Name

            Compare-Object -ReferenceObject $extractedList -DifferenceObject $sourceList -PassThru | Should Be $null
        }

        It "Validate Expand-Archive cmdlet in pipleline scenario" {
            $sourcePath = "$TestDrive/SamplePreCreated*.zip"
            $destinationPath = "$TestDrive/PipeToExpandArchive"

            $content = "Some Data"
            $files = @("Sample-1.txt", "Sample-2.txt")

            dir $sourcePath | Expand-Archive -DestinationPath $destinationPath

            foreach($currentFile in $files)
            {
                $expandedFile = Join-Path $destinationPath -ChildPath $currentFile
                Test-Path $expandedFile | Should Be $True
                Get-Content $expandedFile | Should Be $content
            }
        }

        It "Validate that Expand-Archive generates Verbose messages" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $destinationPath = "$TestDrive/VerboseMessagesInExpandArchive"
            
            try
            {   
                $ps=[PowerShell]::Create()
                $ps.Streams.Error.Clear()
                $ps.Streams.Verbose.Clear()
                $script = "Import-Module Microsoft.PowerShell.Archive; Expand-Archive -Path $sourcePath -DestinationPath $destinationPath -Verbose"
                $ps.AddScript($script)
                $ps.Invoke()

                $ps.Streams.Verbose.Count -gt 0 | Should Be $True
                $ps.Streams.Error.Count | Should Be 0
            }
            finally
            {
                $ps.Dispose()
            }
        }

        It "Validate that without -Force parameter Expand-Archive generates non-terminating errors without overwriting existing files" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $destinationPath = "$TestDrive/NoForceParameterExpandArchive"
            
            try
            {   
                $ps=[PowerShell]::Create()
                $ps.Streams.Error.Clear()
                $ps.Streams.Verbose.Clear()
                $script = "Import-Module Microsoft.PowerShell.Archive; Expand-Archive -Path $sourcePath -DestinationPath $destinationPath; Expand-Archive -Path $sourcePath -DestinationPath $destinationPath"
                $ps.AddScript($script)
                $ps.Invoke()

                $ps.Streams.Error.Count -gt 0 | Should Be $True
            }
            finally
            {
                $ps.Dispose()
            }
        }

        It "Validate that without DestinationPath parameter Expand-Archive cmdlet succeeds in expanding the archive" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $archivePath = "$TestDrive/NoDestinationPathParameter.zip"
            $destinationPath = "$TestDrive/NoDestinationPathParameter"
            copy-item $sourcePath $archivePath -Force
            
            try
            {
                Push-Location $TestDrive
                
                Expand-Archive -Path $archivePath
                (dir $destinationPath).Count | Should Be 2
            }
            finally
            {
                Pop-Location
            }
        }

        It "Validate that without DestinationPath parameter Expand-Archive cmdlet succeeds in expanding the archive when destination directory exists" {
            $sourcePath = "$TestDrive/SamplePreCreatedArchive.zip"
            $archivePath = "$TestDrive/NoDestinationPathParameterDirExists.zip"
            $destinationPath = "$TestDrive/NoDestinationPathParameterDirExists"
            copy-item $sourcePath $archivePath -Force
            New-Item -Path $destinationPath -ItemType Directory | Out-Null
            
            try
            {
                Push-Location $TestDrive
                
                Expand-Archive -Path $archivePath
                (dir $destinationPath).Count | Should Be 2
            }
            finally
            {
                Pop-Location
            }
        }
    }
}
