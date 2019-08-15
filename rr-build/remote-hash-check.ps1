[Cmdletbinding()]
param (
    [Parameter(Mandatory)]
    [string]
    $Path,

    [Parameter(Mandatory)]
    [ValidateSet('Feature', 'Master', 'Secure')]
    [string]
    $Type
)

begin {
    Write-Verbose -Message "Starting invocation: $($MyInvocation.InvocationName)"


    function Send-KeyVaultUpdate {
        [CmdletBinding()]
        [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidUsingConvertToSecureStringWithPlainText', '', Justification = "Plaintext is not a secret. Just needs to be a securestring for pushing to AKV.")]
        param (
            [Parameter(Mandatory)]
            [string]
            $Hash,

            [Parameter(Mandatory)]
            [ValidateSet('Feature', 'Master', 'Secure')]
            [string]
            $Type
        )

        begin {
            Write-Verbose -Message "Starting invocation: $($MyInvocation.InvocationName)"
        }

        process {
            $secretValue = ConvertTo-SecureString -String $Hash -AsPlainText -Force
            $secretName = '{0}-{1}' -f $env:BUILD_REPOSITORY_NAME, $Type
            Set-AzureRmKeyVaultSecret -VaultName 'RockyRoad-test-kv' -Name $secretName -SecretValue $secretValue -Expires $null
        }

        end {
            Write-Verbose -Message "Ending invocation: $($MyInvocation.InvocationName)"
        }
    }


    $hashes = [System.Collections.Generic.List[string]]::New()
    $secretMap = @{}
}

process {
    try {
        Write-Verbose -Message "Setting location to $Path which should be a git repo"
        Push-Location -Path $Path
    }
    catch {
        throw "Unable to push location to $Path"
        exit 1
    }

    Write-Verbose -Message 'Getting all of the tracked files'
    $files = git ls-files

    foreach ($file in $files) {
        Write-Verbose -Message "Hashing file: $file"
        $hashes.Add($(Get-FileHash -Path ".\$file").Hash)
    }

    Write-Verbose -Message 'Creating unique hash of all hashes from files'
    # Get-FileHash requires either a file that's written to the FileSystemProvider somewhere or
    # a memory stream
    $hashStream = [System.IO.MemoryStream]::New([Text.Encoding]::UTF8.GetBytes($hashes -join ''))
    $uniqueHash = Get-FileHash -InputStream $hashStream

    Write-Verbose -Message 'Scanning AKV for related hashes'

    $secretNames = switch ($Type) {
        'Feature' {
            @('DevPush')
            break
        }
        'Master' {
            @('DevPush', 'Feature')
            break
        }
        'Secure' {
            @('DevPush', 'Feature', 'Master')
            break
        }
        default {
            throw "This should be an impossible condition. Type = $Type"
            exit 1
        }
    }

    foreach ($secretName in $secretNames) {
        Write-Verbose "Getting secrets for $secretName"
        $keyVaultSecretName = '{0}-{1}' -f $env:BUILD_REPOSITORY_NAME, $secretName
        $rawSecrets = Get-AzureRmKeyVaultSecret -VaultName 'RockyRoad-test-kv' -Name $keyVaultSecretName -IncludeVersions
        $secrets = [System.Collections.Generic.List[string]]::New()

        foreach ($rawSecret in $rawSecrets) {
            Write-Verbose "Getting secret $secretName v. $($rawSecret.Version)"
            $secrets.Add($(Get-AzureRmKeyVaultSecret -VaultName 'RockyRoad-test-kv' -Name $keyVaultSecretName -Version $rawSecret.Version).SecretValueText)
        }

        $secretMap.Add($secretName, $secrets)
    }

    Write-Verbose -Message 'Stepping through each stage for a hash match'
    foreach ($secretName in $secretNames) {
        Write-Verbose -Message "Checking $secretName"
        $stageHash = $null # Re-initialize for each loop
        $stageHash = $secretMap[$secretName].Where({$_ -eq $uniqueHash.Hash})
        if (-not $stageHash) {
            throw "Missing a hash for $secretName"
            exit 1
        }
        else {
            Write-Verbose -Message "Found matching hash for $secretName"
        }
    }

    Write-Verbose "Entering hash into Azure Key Vault (type $Type)"
    Send-KeyVaultUpdate -Hash $uniqueHash.Hash -Type $Type
}

end {
    Write-Verbose -Message "Ending invocation: $($MyInvocation.InvocationName)"
}