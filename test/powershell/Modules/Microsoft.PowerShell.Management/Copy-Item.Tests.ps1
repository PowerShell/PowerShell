# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Validate Copy-Item locally" -Tags "CI" {
    It "Copy-Item has non-terminating error if destination is in use" -Skip:(!$IsWindows) {
        Copy-Item -Path $env:windir\system32\cmd.exe -Destination TestDrive:\
        $cmd = Start-Process -FilePath TestDrive:\cmd.exe -PassThru
        try {
            { Copy-Item -Path $env:windir\system32\cmd.exe -Destination TestDrive:\ -ErrorAction SilentlyContinue } | Should -Not -Throw
        }
        finally {
            $cmd | Stop-Process
        }
    }
}

# This is a Pester test suite to validate Copy-Item remotely using a remote session.

# If PS Remoting is not available, do not run the suite.
function ShouldRun
{
    if ( $IsCoreCLR ) { return $false }
    $result = Invoke-Command -ComputerName . -ScriptBlock {1} -ErrorAction SilentlyContinue
    return ($result -eq 1)
}

if (-not (ShouldRun))
{
    Write-Host "PS Remoting is not available, skipping tests..." -ForegroundColor Cyan
    return
}

Describe "Validate Copy-Item Remotely" -Tags "CI" {

    # Validate a copy item operation.
    # $filePath is the source file path
    #
    function ValidateCopyItemOperation
    {
        param ([string]$filePath, [string]$destination)

        if (-not $destination)
        {
            $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
        }
        else
        {
            $fileName = Split-Path $filePath -Leaf
            $copiedFilePath = Join-Path $destination $fileName
        }

        $copiedFilePath | Should -Exist

        # Validate file attributes
        $originalFile = Get-Item $filePath -Force
        $newFile = Get-Item $copiedFilePath -Force

        # Validate file Length
        $newFile.Length | Should -Be $originalFile.Length

        # Validate LastWriteTime
        $newFile.LastWriteTime | Should -Be $originalFile.LastWriteTime
        $newFile.LastWriteTimeUtc | Should -Be $originalFile.LastWriteTimeUtc

        # Validate Attributes
        $newFile.Attributes.value__ | Should -Be $originalFile.Attributes.value__
    }

    # Validate a copy item operation.
    # $filePath is the source file path
    #
    function ValidateCopyItemOperationForAlternateDataStream
    {
        param ([string]$filePath, $streamName, $expectedStreamContent)

        $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
        $copiedFilePath | Should -Exist
        (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $filePath).Length

        # Validate the stream
        $actualStreamContent = Get-Content -Path $copiedFilePath -Stream $streamName -ErrorAction SilentlyContinue
        $actualStreamContent | Should -Match $expectedStreamContent
    }

    BeforeAll {
        $s = New-PSSession -ComputerName . -ErrorAction SilentlyContinue
        if (-not $s)
        {
            throw "Failed to create PSSession for remote copy operations."
        }

        $destinationFolderName = "DestinationDirectory"
        $sourceFolderName = "SourceDirectory"
        $testDirectory = Join-Path -Path "TestDrive:" -ChildPath "copyItemRemotely"
        $destinationDirectory = Join-Path -Path $testDirectory -ChildPath $destinationFolderName
        $sourceDirectory = Join-Path -Path $testDirectory -ChildPath $sourceFolderName

        # Creates one txt file
        #
        function CreateTestFile
        {
            param ([switch]$setReadOnlyAttribute = $false, [switch]$emptyFile = $false)

            # Create the test directory.
            New-Item -Path $sourceDirectory -Force -ItemType Directory | Out-Null

            # Create the file.
            $filePath = Join-Path -Path $sourceDirectory -ChildPath "testfileone.txt"
            if (-not $emptyFile)
            {
                "File test content" | Out-File -FilePath $filePath -Force
            }
            else
            {
                "" | Out-File -FilePath $filePath -Force
            }

            if (-not (Test-Path $filePath))
            {
                throw "Failed to create test file $filePath."
            }

            if ($setReadOnlyAttribute)
            {
                Set-ItemProperty -Path $filePath -Name IsReadOnly -Value $true -Force
            }

            return (Get-Item $filePath).FullName
        }

        # Create a set of directories and files with the following structure:
        # .\copyItemRemotely\SourceDirectory\A\a.txt
        # .\copyItemRemotely\SourceDirectory\A\a2.txt
        # .\copyItemRemotely\SourceDirectory\rootFile.txt
        # .\copyItemRemotely\SourceDirectory\B\b.txt
        # .\copyItemRemotely\SourceDirectory\C\D\d.txt
        #
        function CreateTestDirectory
        {
            param ([switch]$setReadOnlyAttribute = $false)

            $directoriesToCreate = @()
            $directoriesToCreate += "A"
            $directoriesToCreate += "B"
            $directoriesToCreate += "C\D"

            $filesToCreate = @()
            $filesToCreate += "rootFile.txt"
            $filesToCreate += "A\a.txt"
            $filesToCreate += "A\a2.txt"
            $filesToCreate += "B\b.txt"
            $filesToCreate += "C\D\d.txt"

            # Create the directories.
            foreach ($directory in $directoriesToCreate)
            {
                $directoryPath = Join-Path -Path $sourceDirectory -ChildPath $directory
                New-Item -Path $directoryPath -Force -ItemType Directory | Out-Null
            }

            $result = @{
                SourceDirectory = (Get-Item $sourceDirectory).FullName
                Files = @()
            }

            # Create the files.
            foreach ($file in $filesToCreate)
            {
                $filePath = Join-Path -Path $sourceDirectory -ChildPath $file
                $file + "`r`n File test content" | Out-File -FilePath $filePath -Force

                if (-not (Test-Path -Path $filePath))
                {
                    throw "Failed to create test file $filePath."
                }

                if ($setReadOnlyAttribute)
                {
                    Set-ItemProperty -Path $filePath -Name IsReadOnly -Value $true -Force
                }

                $result.Files += (Get-Item -Path $filePath).FullName
            }

            return $result
        }

        function GenerateTestAssembly
        {
            $assemblyPath = Join-Path -Path $env:TEMP -ChildPath TestModule
            $outputPath = Join-Path -Path $assemblyPath -ChildPath TestModule.dll

            if (-not (Test-Path -Path $assemblyPath))
            {
                New-Item -Path $assemblyPath -Force -ItemType Directory | Out-Null
            }

            if (-not (Test-Path -Path $outputPath))
            {
                $code = @"
                namespace TestModule
                {
                    using System;
                    using System.Management.Automation;

                    [Cmdlet(VerbsCommon.Get, "TestModule")]
                    public class TestSameCmdlets : PSCmdlet
                    {
                        protected override void ProcessRecord()
                        {
                            WriteObject("TestModule");
                        }
                    }
                }
"@
                Add-Type -TypeDefinition $code -OutputAssembly $outputPath
            }

            $result = @{
                ModuleName = "TestModule"
                Path = (Get-Item $outputPath).FullName
            }

            return $result
        }

        function GetDestinationFolderPath
        {
            return (Get-Item -Path $destinationDirectory).FullName
        }
    }

    AfterAll {
        Remove-PSSession -Name $s.Name -ErrorAction SilentlyContinue
    }

    BeforeEach {
        <# Ensure we start with an empty test directory. Here is the file structure

        $destinationFolderName = "DestinationDirectory"
        $sourceFolderName = "SourceDirectory"
        $testDirectory = Join-Path "TestDrive:" "copyItemRemotely"
        $destinationDirectory = Join-Path $testDirectory $destinationFolderName
        $sourceDirectory = Join-Path $testDirectory $sourceFolderName
        #>

        if (Test-Path -Path $testDirectory)
        {
            Remove-Item -Path $testDirectory -Force -ErrorAction SilentlyContinue -Recurse
        }

        # Create testDirectory, and destinationDirectory
        New-Item -Path $testDirectory -ItemType Directory -Force | Out-Null
        New-Item -Path $destinationDirectory -ItemType Directory -Force | Out-Null
    }

    Context "Validate Copy-Item Locally." {
        It "Copy-Item -Path $filePath -Destination $destinationFolderPath" {

            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $filePath -Destination $destinationFolderPath
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy-Item -Path $($testObject.SourceDirectory)  -Destination $destinationFolderPath -Recurse" {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $testObject.SourceDirectory -Destination $destinationFolderPath -Recurse
            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\SourceDirectory")
                $copiedFilePath | Should -Exist
            }
        }
    }

    Context "Validate Copy-Item to remote session." {

        It "Copy one file to remote session." {
            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $filePath -ToSession $s -Destination $destinationFolderPath
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy one read only file to remote session." {

            $filePath = CreateTestFile -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $filePath -ToSession $s -Destination $destinationFolderPath -Force
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy-Item works for a read only file when '-Force' is not used." {

            $filePath = CreateTestFile -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $filePath -ToSession $s -Destination $destinationFolderPath -Verbose
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy one folder to session Recursively" {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $testObject.SourceDirectory -ToSession $s -Destination $destinationFolderPath -Recurse

            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\SourceDirectory")
                $copiedFilePath | Should -Exist
		(Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy folder with read only files to remote session recursively." {
            $testObject = CreateTestDirectory -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $testObject.SourceDirectory -ToSession $s -Destination $destinationFolderPath -Recurse -Force

            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\SourceDirectory")
		$copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy one file to remote session fails when the remote directory does not exist." {

            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            $destinationFolderPath = Join-Path $destinationFolderPath "A\B\C\D\E"
            $expectedFullyQualifiedErrorId = 'RemotePathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand'

            { Copy-Item -Path $filePath -ToSession $s -Destination $destinationFolderPath -ErrorAction Stop } |
                Should -Throw -ErrorId $expectedFullyQualifiedErrorId
        }

        It "Copy folder to remote session recursively works even if the target directory does not exist." {
            $testObject = CreateTestDirectory -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            $destinationFolderPath = Join-Path $destinationFolderPath "FolderThatDoesNotExist"
            Copy-Item -Path $testObject.SourceDirectory -ToSession $s -Destination $destinationFolderPath -Recurse -Force

            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\FoderThatDoesNotExist")
                $copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy one empty file to remote session." {

            $filePath = CreateTestFile -emptyFile
            $destinationFolderPath = GetDestinationFolderPath
            $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
            $copiedFilePath | Should -Not -Exist
            Copy-Item -Path $filePath  -ToSession $s -Destination $destinationFolderPath
            $copiedFilePath | Should -Exist
            (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $filePath).Length
        }

        It "Copy-Item to session supports alternate data streams." {

            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            $streamContent = "This content is hidden"
            $streamName = "Hidden"
            Set-Content -Path $filePath -Value $streamContent -Stream $streamName
            Copy-Item -Path $filePath -ToSession $s -Destination $destinationFolderPath -Verbose
            ValidateCopyItemOperationForAlternateDataStream -filePath $filePath -streamName $streamName -expectedStreamContent $streamContent
        }
    }

    Context "Validate Copy-Item from remote session." {

        It "Copy one file from remote session." {

            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
            $copiedFilePath | Should -Not -Exist
            Copy-Item -Path $filePath  -FromSession $s -Destination $destinationFolderPath
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy one empty file from remote session." {

            $filePath = CreateTestFile -emptyFile
            $destinationFolderPath = GetDestinationFolderPath
            $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
            $copiedFilePath | Should -Not -Exist
            Copy-Item -Path $filePath  -FromSession $s -Destination $destinationFolderPath
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy folder from remote session recursively." {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            $files = @(Get-ChildItem $destinationFolderPath -Recurse -Force)
            Copy-Item -Path $testObject.SourceDirectory -FromSession $s -Destination $destinationFolderPath -Recurse

            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\SourceDirectory")
                $copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy one file from remote session fails when the target directory does not exist." {
            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            $destinationFolderPath = Join-Path $destinationFolderPath "A\B\C\D\E"
            $expectedFullyQualifiedErrorId = 'CopyItemRemotelyIOError,Microsoft.PowerShell.Commands.CopyItemCommand'

            { Copy-Item -Path $filePath -FromSession $s -Destination $destinationFolderPath -ErrorAction Stop } |
                Should -Throw -ErrorId $expectedFullyQualifiedErrorId
        }

        It "Copy folder from remote session recursively works even if the target directory does not exist." {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            $destinationFolderPath = Join-Path $destinationFolderPath "FoderThatDoesNotExist"
            $files = @(Get-ChildItem $destinationFolderPath -Recurse -Force)
            Copy-Item -Path $testObject.SourceDirectory -FromSession $s -Destination $destinationFolderPath -Recurse

            foreach ($file in $testObject.Files)
            {
                $copiedFilePath = ([string]$file).Replace("SourceDirectory", "DestinationDirectory\FoderThatDoesNotExist")
                $copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy a read only file from a remote session." {

            $filePath = CreateTestFile -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            $copiedFilePath = ([string]$filePath).Replace("SourceDirectory", "DestinationDirectory")
            Copy-Item -Path $filePath  -FromSession $s -Destination $destinationFolderPath -Force
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy-Item for a read only file works with no '-force' parameter." {

            $filePath = CreateTestFile -setReadOnlyAttribute
            $destinationFolderPath = GetDestinationFolderPath
            Copy-Item -Path $filePath -FromSession $s -Destination $destinationFolderPath
            ValidateCopyItemOperation -filePath $filePath
        }

        It "Copy-Item -FromSession works even when trying to copy an assembly that is currently being used by another process." {

            $testAssembly = GenerateTestAssembly
            $destinationFolderPath = GetDestinationFolderPath
            Import-Module $testAssembly.Path -Force
            try
            {
                Copy-Item -Path $testAssembly.Path -FromSession $s -Destination $destinationFolderPath
                ValidateCopyItemOperation -filePath $testAssembly.Path
            }
            finally
            {
                Remove-Module $testAssembly.ModuleName -Force -ErrorAction SilentlyContinue
            }
        }

        It "Copy-Item from session supports alternate data streams." {

            $filePath = CreateTestFile
            $destinationFolderPath = GetDestinationFolderPath
            $streamContent = "This content is hidden"
            $streamName = "Hidden"
            Set-Content -Path $filePath -Value $streamContent -Stream $streamName
            Copy-Item -Path $filePath -FromSession $s -Destination $destinationFolderPath
            ValidateCopyItemOperationForAlternateDataStream -filePath $filePath -streamName $streamName -expectedStreamContent $streamContent
        }

        It "Copy file to the same directory fails." {
            $filePath = CreateTestFile
            { Copy-Item -Path $filePath -Destination $sourceDirectory -FromSession $s -ErrorAction Stop } | Should -Throw -ErrorId "System.IO.IOException,WriteException"
        }

        It "Copy directory with a -Destination parameter given as a file path fails." {
            $filePath = CreateTestFile
            $folderToCopy = GetDestinationFolderPath
            { Copy-Item -Path $folderToCopy -Destination $filePath -FromSession $s -ErrorAction Stop } | Should -Throw -ErrorId "CopyError,Microsoft.PowerShell.Commands.CopyItemCommand"
        }

        It "Copy-Item parameters -FromSession and -ToSession are mutually exclusive." {
            try
            {
                $s1 = New-PSSession -ComputerName . -ErrorAction SilentlyContinue
                $s1 | Should -Not -BeNullOrEmpty
                $filePath = CreateTestFile
                $destinationFolderPath = GetDestinationFolderPath
                { Copy-Item -Path $filePath -Destination $destinationFolderPath -FromSession $s -ToSession $s1 -ErrorAction Stop } | Should -Throw -ErrorId "InvalidInput,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
            finally
            {
                Remove-PSSession -Session $s1 -ErrorAction SilentlyContinue
            }
        }
    }

    Context "Validate Copy-Item Remotely using wildcards" {

        It "Copy-Item from session using wildcards." {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            $sourcePathWithWildcards = "$($testObject.SourceDirectory)\A\*.txt"
            Copy-Item -Path $sourcePathWithWildcards -FromSession $s -Destination $destinationFolderPath -Force

            $sourceFiles = @(Get-Item $sourcePathWithWildcards)
            foreach ($file in $sourceFiles)
            {
                $copiedFilePath = Join-Path $destinationFolderPath (Split-Path $file -Leaf)
                $copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }

        It "Copy-Item to session using wildcards." {

            $testObject = CreateTestDirectory
            $destinationFolderPath = GetDestinationFolderPath
            $sourcePathWithWildcards = "$($testObject.SourceDirectory)\A\*.txt"
            Copy-Item -Path $sourcePathWithWildcards -ToSession $s -Destination $destinationFolderPath -Force

            $sourceFiles = @(Get-Item $sourcePathWithWildcards)
            foreach ($file in $sourceFiles)
            {
                $copiedFilePath = Join-Path $destinationFolderPath (Split-Path $file -Leaf)
                $copiedFilePath | Should -Exist
                (Get-Item $copiedFilePath).Length | Should -Be (Get-Item $file).Length
            }
        }
    }

    Context "Validate FullyQualifiedErrorIds for remote source and destination paths." {

        BeforeAll {
            # Create test file.
            $testFilePath = Join-Path "TestDrive:" "testfile.txt"
            if (Test-Path $testFilePath)
            {
                Remove-Item $testFilePath -Force -ErrorAction SilentlyContinue
            }
            "File test content" | Out-File $testFilePath -Force
        }

        function Test-CopyItemError
        {
            param ($path, $destination, $expectedFullyQualifiedErrorId, $fromSession = $false)

            if ($fromSession)
            {
                It "Copy-Item FromSession -Path '$path' throws $expectedFullyQualifiedErrorId" {
                    { Copy-Item -Path $path -FromSession $s -Destination $destination -ErrorAction Stop } |
                        Should -Throw -ErrorId $expectedFullyQualifiedErrorId
                }
            }
            else
            {
                It "Copy-Item ToSession -Destination '$path' throws $expectedFullyQualifiedErrorId" {
                    { Copy-Item -Path $path -ToSession $s -Destination $destination -ErrorAction Stop } |
                        Should -Throw -ErrorId $expectedFullyQualifiedErrorId
                }
            }
        }

        $invalidSourcePathtestCases = @(
            @{
                Path = "HKLM:\SOFTWARE"
                Destination = $env:SystemDrive
                ExpectedFullyQualifiedErrorId = "NamedParameterNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
            @{
                Path = ".\Source"
                Destination = $env:SystemDrive
                ExpectedFullyQualifiedErrorId = "RemotePathIsNotAbsolute,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
            @{
                Path = $env:SystemDrive + "\X\Y\Z"
                Destination = $env:SystemDrive + "\A\B\C"
                ExpectedFullyQualifiedErrorId = "RemotePathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
            @{
                Path = $null
                Destination = $env:SystemDrive
                ExpectedFullyQualifiedErrorId = "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
            @{
                Path = ''
                Destination = $env:SystemDrive
                ExpectedFullyQualifiedErrorId = "ParameterArgumentValidationErrorEmptyStringNotAllowed,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
            @{
                Path = "$env:SystemDrive\nonexistentdir\*"
                Destination = "$env:SystemDrive\psTest"
                ExpectedFullyQualifiedErrorId = "RemotePathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"
                FromSession = $true
            }
        )

        foreach ($testCase in $invalidSourcePathtestCases) {
           Test-CopyItemError @testCase
        }

        $invalidDestinationPathtestCases = @(
            @{
                Path = $testFilePath
                Destination = ".\Source"
                ExpectedFullyQualifiedErrorId = "RemotePathIsNotAbsolute,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
            @{
                Path = $testFilePath
                Destination = $env:SystemDrive + "\X\A\B\C"
                ExpectedFullyQualifiedErrorId = "RemotePathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
            @{
                Path = $testFilePath
                Destination = $null
                ExpectedFullyQualifiedErrorId = "CopyItemRemoteDestinationIsNullOrEmpty,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
            @{
                Path = $testFilePath
                Destination = ""
                ExpectedFullyQualifiedErrorId = "CopyItemRemoteDestinationIsNullOrEmpty,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
            @{
                Path = "$env:SystemDrive\nonexistentdir\*"
                Destination = "$env:SystemDrive\psTest"
                ExpectedFullyQualifiedErrorId = "PathNotFound,Microsoft.PowerShell.Commands.CopyItemCommand"
            }
        )

        foreach ($testCase in $invalidDestinationPathtestCases) {
           Test-CopyItemError @testCase
        }
    }
}

Describe "Validate Copy-Item error for target sessions not in FullLanguageMode." -Tags "Feature" {

    BeforeAll {

        $testDirectory = "TestDrive:\"

        # Create the test file and directories.
        $source = "$testDirectory\Source"
        $destination = "$testDirectory\Destination"

        New-Item $source -ItemType Directory -Force | Out-Null
        New-Item $destination -ItemType Directory -Force | Out-Null

        $testFilePath = Join-Path $source "testfile.txt"
        "File test content" | Out-File $testFilePath -Force

        # Keep track of the sessions.
        $testSessions = @{}

        # Keep track of the session names to be unregistered.
        $sessionToUnregister = @()

        $languageModes = @("ConstrainedLanguage", "NoLanguage", "RestrictedLanguage")
        $id = (Get-Random).ToString()

        foreach ($languageMode in $languageModes)
        {
            $sessionName = $languageMode + "_" + $id
            $sessionToUnregister += $sessionName
            $configFilePath = Join-Path $testDirectory "test.pssc"

            # Create the session.
            Write-Host "Creating pssession with '$languageMode' ..."
            New-PSSessionConfigurationFile -Path $configFilePath -SessionType Default -LanguageMode $languageMode
            Register-PSSessionConfiguration -Name $sessionName -Path $configFilePath -Force | Out-Null
            $testSession = New-PSSession -ConfigurationName $sessionName

            # Validate that the session is opened.
            $testSession.State | Should -Be "Opened"

            # Add the new session to the list.
            $testSessions[$languageMode] = $testSession

            # Remove the pssc file.
            Remove-Item $configFilePath -Force -ErrorAction SilentlyContinue
        }
    }

    AfterAll {

        $testSessions.Values | Remove-PSSession -ErrorAction SilentlyContinue

        $sessionToUnregister | ForEach-Object {
            Unregister-PSSessionConfiguration -Name $_ -Force -ErrorAction SilentlyContinue
        }
    }

    foreach ($languageMode in $testSessions.Keys)
    {
        $session = $testSessions[$languageMode]

        It "Copy-Item throws 'SessionIsNotInFullLanguageMode' error for a session in '$languageMode'" {

            # FromSession
            { Copy-Item -Path $testFilePath -FromSession $session -Destination $destination -Force -Verbose -ErrorAction Stop } |
                Should -Throw -ErrorId "SessionIsNotInFullLanguageMode,Microsoft.PowerShell.Commands.CopyItemCommand"

            # ToSession
            { Copy-Item -Path $testFilePath -ToSession $session -Destination $destination -Force -Verbose -ErrorAction Stop } |
                Should -Throw -ErrorId "SessionIsNotInFullLanguageMode,Microsoft.PowerShell.Commands.CopyItemCommand"
        }
    }
}

Describe "Copy-Item can use Recurse and Exclude together" -Tags "Feature" {

    Context "Local and Remote Tests" {

        BeforeAll {
            $s = New-PSSession -ComputerName . -ErrorAction SilentlyContinue
            if (-not $s)
            {
                throw "Failed to create PSSession for remote copy operations."
            }

            $null = New-Item -ItemType Directory -Path "TestDrive:\Parent\Sub"
            $null = New-Item -Path "TestDrive:\Parent\p1.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\p2.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\s4.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\Sub\s1.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\Sub\s2.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\Sub\s3.txt" -Value "test"
            $null = New-Item -Path "TestDrive:\Parent\Sub\p3.txt" -Value "testcl"
        }

        It "can exclude files at sub directory" {
            Copy-Item -Path TestDrive:\Parent\* -Recurse -Exclude s*.txt -Destination TestDrive:\Temp -Force
            $copiedFiles = Get-ChildItem -Recurse -Path TestDrive:\Temp
            $copiedFiles.Count | Should -Be 3
        }

        It "can exclude files at sub directory to a session" {
            Copy-Item -Path TestDrive:\Parent\* -Recurse -Exclude s*.txt -Destination $TestDrive\Temp2 -Force -ToSession $s
            $copiedFiles = Get-ChildItem -Recurse -Path TestDrive:\Temp
            $copiedFiles.Count | Should -Be 3
        }

        It "can exclude files at sub directory from a session" {
            Copy-Item -Path $TestDrive\Parent\* -Recurse -Exclude s*.txt -Destination TestDrive:\Temp3 -FromSession $s
            $copiedFiles = Get-ChildItem -Recurse -Path TestDrive:\Temp2
            $copiedFiles.Count | Should -Be 3
        }

        AfterAll {
            Remove-PSSession -Session $s -ErrorAction SilentlyContinue
        }
    }
}

Describe "Copy-Item remotely bug fixes" -Tags "Feature" {

    BeforeAll {
        $s = New-PSSession -ComputerName . -ErrorAction SilentlyContinue
        if (-not $s)
        {
            throw "Failed to create PSSession for remote copy operations."
        }

        $originalContent = "test file 1 - Source"
        $newContent =  "This is some new content"

        $null = New-Item -ItemType Directory -Path "TestDrive:\Source"
        $null = New-Item -ItemType Directory -Path "TestDrive:\Destination"
    }

    AfterAll {
        Remove-PSSession -Session $s -ErrorAction SilentlyContinue
    }

    BeforeEach {

        # Create the same file in the source and destination
        Set-Content -Path "TestDrive:\Source\testFile1.txt" -Value $originalContent -Force
        Set-Content -Path "TestDrive:\Destination\testFile1.txt" -Value $originalContent -Force
    }

    Context "Copy-Item remotely overwrites a destination file if it exists." {

        BeforeEach {

            # Overwrite the source file
            Set-Content -Path "TestDrive:\Source\testFile1.txt" -Value $newContent
        }

        It "Copy item -tosession overwrites the content of an existing file." {

            # Copy file to session
            Copy-Item -Path "TestDrive:\Source\testFile1.txt" -Destination "$TestDrive\Destination\testFile1.txt" -ToSession $s

            # Validate the file was overwritten
            $fileContent = Get-Content "TestDrive:\Destination\testFile1.txt" -ErrorAction SilentlyContinue -Raw
            $fileContent | Should -Match $newContent
        }

        It "Copy item -fromsession overwrites the content of an existing file." {

            # Copy file to session
            Copy-Item -Path "$TestDrive\Source\testFile1.txt" -Destination "TestDrive:\Destination\testFile1.txt" -FromSession $s

            # Validate the file was overwritten
            $fileContent = Get-Content "TestDrive:\Destination\testFile1.txt" -ErrorAction SilentlyContinue -Raw
            $fileContent | Should -Match $newContent
        }
    }

    Context "Copy-Item remotely creates a destination file if it does not exist." {

        BeforeEach {

            if (Test-Path "TestDrive:\AnotherDestination")
            {
                Remove-Item "TestDrive:\AnotherDestination" -Force -Recurse -ErrorAction SilentlyContinue
            }
            $null = New-Item -ItemType Directory -Path "TestDrive:\AnotherDestination"

            # Ensure the file does not exist
            "TestDrive:\AnotherDestination\FileThatDoesNotExist.txt" | Should -Not -Exist
        }

        It "Copy-Item -tosession creates the file if it does not exist on the remote destination." {

            # Copy file to session
            Copy-Item -Path "TestDrive:\Source\testFile1.txt" -Destination "$TestDrive\AnotherDestination\FileThatDoesNotExist.txt" -ToSession $s

            # Verify that the file was created
            "TestDrive:\AnotherDestination\FileThatDoesNotExist.txt" | Should -Exist
        }

        It "Copy-Item -fromsession creates the file if it does not exist on the local machine." {

            # Copy file from session
            Copy-Item -Path "$TestDrive\Source\testFile1.txt" -Destination "TestDrive:\AnotherDestination\FileThatDoesNotExist.txt" -FromSession $s

            # Verify that the file was created
            "TestDrive:\AnotherDestination\FileThatDoesNotExist.txt" | Should -Exist
        }
    }
}
