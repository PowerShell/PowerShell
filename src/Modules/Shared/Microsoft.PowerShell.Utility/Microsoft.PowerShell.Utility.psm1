
## Converts a SDDL string into an object-based representation of a security
## descriptor
function ConvertFrom-SddlString
{
    [CmdletBinding(HelpUri = "https://go.microsoft.com/fwlink/?LinkId=623636")]
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
