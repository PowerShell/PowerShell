function Get-FileHash
{
    [CmdletBinding(DefaultParameterSetName = "Path", HelpURI = "http://go.microsoft.com/fwlink/?LinkId=517145")]
    param(
        [Parameter(Mandatory, ParameterSetName="Path", Position = 0)]
        [System.String[]]
        $Path,

        [Parameter(Mandatory, ParameterSetName="LiteralPath", ValueFromPipelineByPropertyName = $true)]
        [Alias("PSPath")]
        [System.String[]]
        $LiteralPath,
        
        [Parameter(Mandatory, ParameterSetName="Stream")]
        [System.IO.Stream]
        $InputStream,

        [ValidateSet("SHA1", "SHA256", "SHA384", "SHA512", "MACTripleDES", "MD5", "RIPEMD160")]
        [System.String]
        $Algorithm="SHA256"
    )
    
    begin
    {
        # Construct the strongly-typed crypto object
        
        # First see if it has a FIPS algorithm  
        $hasherType = "System.Security.Cryptography.${Algorithm}CryptoServiceProvider" -as [Type]
        if ($hasherType)
        {
            $hasher = $hasherType::New()
        }
        else
        {
            # Check if the type is supported in the current system
            $algorithmType = "System.Security.Cryptography.${Algorithm}" -as [Type]
            if ($algorithmType)
            {
                if ($Algorithm -eq "MACTripleDES")
                {
                    $hasher = $algorithmType::New()
                }
                else
                {
                    $hasher = $algorithmType::Create()
                }
            }
            else
            {
                $errorId = "AlgorithmTypeNotSupported"
                $errorCategory = [System.Management.Automation.ErrorCategory]::InvalidArgument
                $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::AlgorithmTypeNotSupported -f $Algorithm
                $exception = [System.InvalidOperationException]::New($errorMessage)
                $errorRecord = [System.Management.Automation.ErrorRecord]::New($exception, $errorId, $errorCategory, $null)
                $PSCmdlet.ThrowTerminatingError($errorRecord)
            }
        }

        function GetStreamHash
        {
            param(
                [System.IO.Stream]
                $InputStream,

                [System.String]
                $RelatedPath,

                [System.Security.Cryptography.HashAlgorithm]
                $Hasher)

            # Compute file-hash using the crypto object
            [Byte[]] $computedHash = $Hasher.ComputeHash($InputStream)
            [string] $hash = [BitConverter]::ToString($computedHash) -replace '-',''

            if ($RelatedPath -eq $null)
            {
                $retVal = [PSCustomObject] @{
                    Algorithm = $Algorithm.ToUpperInvariant()
                    Hash = $hash
                }
            }
            else
            {
                $retVal = [PSCustomObject] @{
                    Algorithm = $Algorithm.ToUpperInvariant()
                    Hash = $hash
                    Path = $RelatedPath
                }
            }
            $retVal.psobject.TypeNames.Insert(0, "Microsoft.Powershell.Utility.FileHash")
            $retVal
        }
    }
    
    process
    {
        if($PSCmdlet.ParameterSetName -eq "Stream")
        {
            GetStreamHash -InputStream $InputStream -RelatedPath $null -Hasher $hasher
        }
        else
        {
            $pathsToProcess = @()
            if($PSCmdlet.ParameterSetName  -eq "LiteralPath")
            {
                $pathsToProcess += Resolve-Path -LiteralPath $LiteralPath | Foreach-Object ProviderPath
            }
            if($PSCmdlet.ParameterSetName -eq "Path")
            {
                $pathsToProcess += Resolve-Path $Path | Foreach-Object ProviderPath
            }

            foreach($filePath in $pathsToProcess)
            {
                if(Test-Path -LiteralPath $filePath -PathType Container)
                {
                    continue
                }

                try
                {
                    # Read the file specified in $FilePath as a Byte array
                    [system.io.stream]$stream = [system.io.file]::OpenRead($filePath)
                    GetStreamHash -InputStream $stream  -RelatedPath $filePath -Hasher $hasher
                }
                catch [Exception]
                {
                    $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::FileReadError -f $FilePath, $_
                    Write-Error -Message $errorMessage -Category ReadError -ErrorId "FileReadError" -TargetObject $FilePath
                    return
                }
                finally
                {
                    if($stream)
                    {
                        $stream.Dispose()
                    }
                }                            
            }
        }
    }
}

<# This cmdlet is used to create a new temporary file in $env:temp #>
function New-TemporaryFile
{
    [CmdletBinding(
        HelpURI='http://go.microsoft.com/fwlink/?LinkId=526726',
        SupportsShouldProcess=$true)]
    [OutputType([System.IO.FileInfo])]
    Param()

    Begin
    {
        try
        {
            if($PSCmdlet.ShouldProcess($env:TEMP))
            {
                $tempFilePath = [System.IO.Path]::GetTempFileName()            
            }
        }
        catch
        {
            $errorRecord = [System.Management.Automation.ErrorRecord]::new($_.Exception,"NewTemporaryFileWriteError", "WriteError", $env:TEMP)
            Write-Error -ErrorRecord $errorRecord
            return
        } 

        if($tempFilePath)
        {
            Get-Item $tempFilePath
        }
    }    
}

<# This cmdlet is used to generate a new guid #>
function New-Guid
{
    [CmdletBinding(HelpURI='http://go.microsoft.com/fwlink/?LinkId=526920')]
	[OutputType([System.Guid])]
    Param()
    
    Begin
    {
        [Guid]::NewGuid()
    }    
}

<############################################################################################ 
# Format-Hex cmdlet helps in displaying the Hexadecimal equivalent of the input data.
############################################################################################>
function Format-Hex
{
    [CmdletBinding(
        DefaultParameterSetName="Path", 
        HelpUri="http://go.microsoft.com/fwlink/?LinkId=526919")]
    [Alias("fhx")]
    [OutputType("Microsoft.PowerShell.Commands.ByteCollection")]
    param 
    (
        [Parameter (Mandatory=$true, Position=0, ParameterSetName="Path")]
        [ValidateNotNullOrEmpty()]
        [string[]] $Path,

        [Parameter (Mandatory=$true, ParameterSetName="LiteralPath")]
        [ValidateNotNullOrEmpty()]
        [Alias("PSPath")]
        [string[]] $LiteralPath,

        [Parameter(Mandatory=$true, ParameterSetName="ByInputObject", ValueFromPipeline=$true)]
        [Object] $InputObject,

        [Parameter (ParameterSetName="ByInputObject")]
        [ValidateSet("Ascii", "UTF32", "UTF7", "UTF8", "BigEndianUnicode", "Unicode")]
        [string] $Encoding = "Ascii",

        [Parameter(ParameterSetName="ByInputObject")]
        [switch]$Raw
    )

    begin
    {
        $bufferSize = 16
        $inputStreamArray = [System.Collections.ArrayList]::New()
        <############################################################################################ 
        # The ConvertToHexadecimalHelper is a helper method used to fetch unicode bytes from the 
        # input data and display the hexadecimial representaion of the of the input data in bytes.
        ############################################################################################>
        function ConvertToHexadecimalHelper
        {
            param 
            (
                [Byte[]] $inputBytes,
                [string] $path,
                [Uint32] $offset
            )

            # This section is used to display the hexadecimal 
            # representaion of the of the input data in bytes.
            if($inputBytes -ne $null)
            {
                $byteCollectionObject =  [Microsoft.PowerShell.Commands.ByteCollection]::new($offset, $inputBytes, $path)
                Write-Output -InputObject $byteCollectionObject
            }
        }

        <############################################################################################ 
        # The ProcessFileContent is a helper method used to fetch file contents in blocks and  
        # process it to support displaying hexadecimal formating of the fetched content.
        ############################################################################################>
        function ProcessFileContent
        {
            param 
            (
                [string] $filePath,
                [boolean] $isLiteralPath
            )

            if($isLiteralPath)
            {
                $resolvedPaths = Resolve-Path -LiteralPath $filePath
            }
            else
            {
                $resolvedPaths = Resolve-Path -Path $filePath
            }

            # If Path resolution has failed then a corresponding non-terminating error is 
            # written to the pipeline. We continue processing any remaining files.
            if($resolvedPaths -eq $null)
            {
                return
            }
                
            if($resolvedPaths.Count -gt 1)
            {
                # write a non-terminating error message indicating that path specified is resolving to multiple file system paths.
                $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::FormatHexResolvePathError -f $filePath
                Write-Error -Message $errorMessage -Category ([System.Management.Automation.ErrorCategory]::InvalidData) -ErrorId "FormatHexResolvePathError"
            }

            $targetFilePath = $resolvedPaths.ProviderPath


            if($targetFilePath -ne $null)
            {
                $buffer = [byte[]]::new($bufferSize)

                try
                {
                    try
                    {
                        $currentFileStream = [System.IO.File]::Open($targetFilePath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
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

                        $errorRecord = [System.Management.Automation.ErrorRecord]::new($exception,"FormatHexFileAccessError", ([System.Management.Automation.ErrorCategory]::ReadError), $targetFilePath)
                        $PSCmdlet.WriteError($errorRecord)
                    }

                    if($null -ne $currentFileStream)
                    {
                        $srcStream = [System.IO.BinaryReader]::new($currentFileStream) 
                        $displayHeader = $true
                        $offset = 0
                        $blockCounter = 0                   
                        while($numberOfBytesRead = $srcStream.Read($buffer, 0, $bufferSize))
                        {
                            # send only the bytes that have been read
                            # if we send the whole buffer, we'll have extraneous bytes
                            # at the end of an incomplete group of 16 bytes
                            if ( $numberOfBytesRead -eq $bufferSize ) 
                            {
                                # under some circumstances if we don't copy the buffer
                                # and the results are stored to a variable, the results are not
                                # correct and one object replicated in all the output objects
                                ConvertToHexadecimalHelper ($buffer.Clone()) $targetFilePath $offset
                            }
                            else
                            {
                                # handle the case of the partial (and probably last) buffer
                                $bytesReadBuffer = [byte[]]::New($numberOfBytesRead)
                                [Array]::Copy($buffer,0, $bytesReadBuffer,0,$numberOfBytesRead)
                                ConvertToHexadecimalHelper $bytesReadBuffer $targetFilePath $offset
                            }
                            $displayHeader = $false
                            $blockCounter++;

                            # Updating the offset value.
                            $offset = $blockCounter*0x10
                        }                    
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
                }
            }
        }
    }

    process 
    {
        switch($PSCmdlet.ParameterSetName)
        {
            "Path"
            {
                ProcessFileContent $Path $false
            }
            "LiteralPath" 
            { 
                ProcessFileContent $LiteralPath $true
            }
            "ByInputObject" 
            { 
                # If it's an actual byte array, then we directly use it for hexadecimal formatting.
                if($InputObject -is [Byte[]])
                {
                    ConvertToHexadecimalHelper $InputObject $null
                    return
                }
                # if it's a single byte, we'll assume streaming
                elseif($InputObject -is [byte])
                {
                    $null = $inputStreamArray.Add($InputObject)
                }
                # If the input data is of string type then directly get bytes out of it.
                elseif($InputObject -is [string])
                {
                    # The ValidateSet arribute on the Encoding paramter makes sure that only
                    # valid values (supported on all paltforms where Format-Hex is avaliable)
                    # are allowed through user input.
                    $inputBytes = [Text.Encoding]::$Encoding.GetBytes($InputObject)
                    ConvertToHexadecimalHelper $inputBytes $null
                    return
                }
                elseif($InputObject -is [System.IO.FileSystemInfo])
                {
                    # If file path is provided as an input, use the file contents to show the hexadecimal format.
                    $filePath = ([System.IO.FileSystemInfo]$InputObject).FullName
                    ProcessFileContent $filePath $false
                    return
                }
                elseif($InputObject -is [int64])
                {
                    $inputBytes = [BitConverter]::GetBytes($InputObject)
                    $null = $inputStreamArray.AddRange($inputBytes)
                }
                elseif($InputObject -is [int64[]])
                {
                    foreach($i64 in $InputObject)
                    {
                        $inputBytes = [BitConverter]::GetBytes($i64)
                        $null = $inputStreamArray.AddRange($inputBytes)
                    }
                }
                elseif($InputObject -is [int])
                {
                    # If we get what appears as ints, it may not be what the user really wants.
                    # for example, if the user types a small set of numbers just to get their 
                    # character representations, as follows:
                    #
                    # 170..180 | format-hex
                    #           Path:
                    #           00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
                    #00000000   AA AB AC AD AE AF B0 B1 B2 B3 B4                 ª«¬­®¯°±²³´
                    #
                    # any integer padding is likely to be more confusing than this
                    # fairly compact representation.
                    #
                    # However, some might like to see the results with the raw data,
                    # -Raw exists to provide that behavior:
                    # PS# 170..180 | format-hex -Raw
                    # 
                    #            Path:
                    # 
                    #            00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F
                    # 
                    # 00000000   AA 00 00 00 AB 00 00 00 AC 00 00 00 AD 00 00 00  ª...«...¬...­...
                    # 00000010   AE 00 00 00 AF 00 00 00 B0 00 00 00 B1 00 00 00  ®...¯...°...±...
                    # 00000020   B2 00 00 00 B3 00 00 00 B4 00 00 00              ²...³...´...
                    #
                    # this provides a representation of the piped numbers which includes all
                    # of the bytes which are in an int32
                    if ( $Raw )
                    {
                        $inputBytes = [BitConverter]::GetBytes($InputObject)
                        $null = $inputStreamArray.AddRange($inputBytes)
                    }
                    else
                    {
                        # first determine whether we can represent this as a byte
                        $possibleByte = $InputObject -as [byte]
                        # first determine whether we can represent this as a int16
                        $possibleInt16 = $InputObject -as [int16]
                        if ( $possibleByte -ne $null ) 
                        {
                            $null = $inputStreamArray.Add($possibleByte)
                        }
                        elseif ( $possibleint16 -ne $null )
                        {
                            $inputBytes = [BitConverter]::GetBytes($possibleInt16)
                            $null = $inputStreamArray.AddRange($inputBytes)
                        }
                        else
                        {
                            # now int
                            $inputBytes = [BitConverter]::GetBytes($InputObject)
                            $null = $inputStreamArray.AddRange($inputBytes)
                        }
                    }
                }
                else
                {
                    # Otherwise, write a non-terminating error message indicating that input object type is not supported.
                    $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::FormatHexTypeNotSupported -f $InputObject.GetType()
                    Write-Error -Message $errorMessage -Category ([System.Management.Automation.ErrorCategory]::ParserError) -ErrorId "FormatHexFailureTypeNotSupported"
                }
                # Handle streaming case here
                # during this process we may not have enough characters to create a ByteCollection
                # if we do, create as many ByteCollections as necessary, each being 16 bytes in length
                if ( $inputStreamArray.Count -ge $bufferSize )
                {
                    $rowCount = [math]::Floor($inputStreamArray.Count/$bufferSize)
                    $arrayLength = $bufferSize * $rowCount
                    for($i = 0; $i -lt $rowCount; $i++) 
                    {
                        $rowOffset = $i * $bufferSize
                        ConvertToHexadecimalHelper -inputBytes $inputStreamArray.GetRange($rowOffset, $bufferSize) -path ' ' -offset $offset
                        $offset += $bufferSize
                    }
                    # We use RemoveRange because of the pathological case of having
                    # streamed combination of bytes, int16, int32, int64 which are greater
                    # than 16 bytes. Consider the case:
                    # $i = [int16]::MaxValue + 3
                    # $i64=[int64]::MaxValue -5
                    # .{ $i;$i;$i;$i64 } | format-hex
                    # which create an arraylist 20 bytes 
                    # we need to remove only the bytes from the array which we emitted
                    $inputStreamArray.RemoveRange(0,$arrayLength)
                }
            }
        }
    }
    end
    {
        # now manage any left over bytes in the $inputStreamArray
        if ( $PSCmdlet.ParameterSetName -eq "ByInputObject" )
        {
            ConvertToHexadecimalHelper $inputStreamArray $null -path ' ' -offset $offset
        }
    }
   
}

## Imports a PowerShell Data File - a PowerShell hashtable defined in
## a file (such as a Module manifest, session configuration file)
function Import-PowerShellDataFile
{
    [CmdletBinding(DefaultParameterSetName = "ByPath", HelpUri = "http://go.microsoft.com/fwlink/?LinkID=623621")]
    [OutputType("System.Collections.Hashtable")]
    param(
        [Parameter(ParameterSetName = "ByPath", Position = 0)]
        [String[]] $Path,
        
        [Parameter(ParameterSetName = "ByLiteralPath", ValueFromPipelineByPropertyName = $true)]
        [Alias("PSPath")]
        [String[]] $LiteralPath
    )
    
    begin
    {
        function ThrowInvalidDataFile
        {
            param($resolvedPath, $extraError)
            
            $errorId = "CouldNotParseAsPowerShellDataFile$extraError"
            $errorCategory = [System.Management.Automation.ErrorCategory]::InvalidData
            $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::CouldNotParseAsPowerShellDataFile -f $resolvedPath
            $exception = [System.InvalidOperationException]::New($errorMessage)
            $errorRecord = [System.Management.Automation.ErrorRecord]::New($exception, $errorId, $errorCategory, $null)
            $PSCmdlet.WriteError($errorRecord)   
        }
    }
 
    process
    {
        foreach($resolvedPath in (Resolve-Path @PSBoundParameters))
        {
            $parseErrors = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseFile(($resolvedPath.ProviderPath), [ref] $null, [ref] $parseErrors)
            if ($parseErrors.Length -gt 0)
            {
                ThrowInvalidDataFile $resolvedPath
            }
            else
            {
                $data = $ast.Find( { $args[0] -is [System.Management.Automation.Language.HashtableAst] }, $false )
                if($data)
                {
                    $data.SafeGetValue()
                }
                else
                {
                    ThrowInvalidDataFile $resolvedPath "NoHashtableRoot"
                }
            }
        }
    }
}

## Converts a SDDL string into an object-based representation of a security
## descriptor
function ConvertFrom-SddlString
{
    [CmdletBinding(HelpUri = "http://go.microsoft.com/fwlink/?LinkId=623636")]
    param(
        ## The string representing the security descriptor in SDDL syntax
        [Parameter(Mandatory = $true, Position = 0, ValueFromPipeline = $true)]
        [String] $Sddl,
        
        ## The type of rights that this SDDL string represents, if any.
        [Parameter()]
        [ValidateSet(
            "FileSystemRights", "RegistryRights", "ActiveDirectoryRights",
            "MutexRights", "SemaphoreRights", "CryptoKeyRights",
            "EventWaitHandleRights")]
        $Type
    )

    Begin
    {
        # On CoreCLR CryptoKeyRights and ActiveDirectoryRights are not supported.
        if ($PSEdition -eq "Core" -and ($Type -eq "CryptoKeyRights" -or $Type -eq "ActiveDirectoryRights"))
        {
            $errorId = "TypeNotSupported"
            $errorCategory = [System.Management.Automation.ErrorCategory]::InvalidArgument
            $errorMessage = [Microsoft.PowerShell.Commands.UtilityResources]::TypeNotSupported -f $Type
            $exception = [System.ArgumentException]::New($errorMessage)
            $errorRecord = [System.Management.Automation.ErrorRecord]::New($exception, $errorId, $errorCategory, $null)
            $PSCmdlet.ThrowTerminatingError($errorRecord)
        }

        ## Translates a SID into a NT Account
        function ConvertTo-NtAccount
        {
            param($Sid)

            if($Sid)
            {
                $securityIdentifier = [System.Security.Principal.SecurityIdentifier] $Sid
        
                try
                {
                    $ntAccount = $securityIdentifier.Translate([System.Security.Principal.NTAccount]).ToString()
                }
                catch{}

                $ntAccount
            }
        }

        ## Gets the access rights that apply to an access mask, preferring right types
        ## of 'Type' if specified.
        function Get-AccessRights
        {
            param($AccessMask, $Type)

            if ($PSEdition -eq "Core")
            {
                ## All the types of access rights understood by .NET Core
                $rightTypes = [Ordered] @{
                    "FileSystemRights" = [System.Security.AccessControl.FileSystemRights]
                    "RegistryRights" = [System.Security.AccessControl.RegistryRights]
                    "MutexRights" = [System.Security.AccessControl.MutexRights]
                    "SemaphoreRights" = [System.Security.AccessControl.SemaphoreRights]
                    "EventWaitHandleRights" = [System.Security.AccessControl.EventWaitHandleRights]
                }
            }
            else
            {
                ## All the types of access rights understood by .NET
                $rightTypes = [Ordered] @{
                    "FileSystemRights" = [System.Security.AccessControl.FileSystemRights]
                    "RegistryRights" = [System.Security.AccessControl.RegistryRights]
                    "ActiveDirectoryRights" = [System.DirectoryServices.ActiveDirectoryRights]
                    "MutexRights" = [System.Security.AccessControl.MutexRights]
                    "SemaphoreRights" = [System.Security.AccessControl.SemaphoreRights]
                    "CryptoKeyRights" = [System.Security.AccessControl.CryptoKeyRights]
                    "EventWaitHandleRights" = [System.Security.AccessControl.EventWaitHandleRights]
                }
            }
            $typesToExamine = $rightTypes.Values
        
            ## If they know the access mask represents a certain type, prefer its names
            ## (i.e.: CreateLink for the registry over CreateDirectories for the filesystem)
            if($Type)
            {
                $typesToExamine = @($rightTypes[$Type]) + $typesToExamine
            }
            
       
            ## Stores the access types we've found that apply
            $foundAccess = @()
        
            ## Store the access types we've already seen, so that we don't report access
            ## flags that are essentially duplicate. Many of the access values in the different
            ## enumerations have the same value but with different names.
            $foundValues = @{}

            ## Go through the entries in the different right types, and see if they apply to the
            ## provided access mask. If they do, then add that to the result.   
            foreach($rightType in $typesToExamine)
            {
                foreach($accessFlag in [Enum]::GetNames($rightType))
                {
                    $longKeyValue = [long] $rightType::$accessFlag
                    if(-not $foundValues.ContainsKey($longKeyValue))
                    {
                        $foundValues[$longKeyValue] = $true
                        if(($AccessMask -band $longKeyValue) -eq ($longKeyValue))
                        {
                            $foundAccess += $accessFlag
                        }
                    }
                }
            }

            $foundAccess | Sort-Object
        }

        ## Converts an ACE into a string representation
        function ConvertTo-AceString
        {
            param(
                [Parameter(ValueFromPipeline)]
                $Ace,
                $Type
            )

            process
            {
                foreach($aceEntry in $Ace)
                {
                    $AceString = (ConvertTo-NtAccount $aceEntry.SecurityIdentifier) + ": " + $aceEntry.AceQualifier
                    if($aceEntry.AceFlags -ne "None")
                    {
                        $AceString += " " + $aceEntry.AceFlags
                    }

                    if($aceEntry.AccessMask)
                    {
                        $foundAccess = Get-AccessRights $aceEntry.AccessMask $Type

                        if($foundAccess)
                        {
                            $AceString += " ({0})" -f ($foundAccess -join ", ")
                        }
                    }

                    $AceString
                }
            }
        }
    }

    Process
    {
        $rawSecurityDescriptor = [Security.AccessControl.CommonSecurityDescriptor]::new($false,$false,$Sddl)

        $owner = ConvertTo-NtAccount $rawSecurityDescriptor.Owner
        $group = ConvertTo-NtAccount $rawSecurityDescriptor.Group
        $discretionaryAcl = ConvertTo-AceString $rawSecurityDescriptor.DiscretionaryAcl $Type
        $systemAcl = ConvertTo-AceString $rawSecurityDescriptor.SystemAcl $Type

        [PSCustomObject] @{
            Owner = $owner
            Group = $group
            DiscretionaryAcl = @($discretionaryAcl)
            SystemAcl = @($systemAcl)
            RawDescriptor = $rawSecurityDescriptor
        }
    }
}