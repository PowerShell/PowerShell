# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Command privilege management for powershell 
# This script safeguards PowerShell environments by managing command privileges and preventing unauthorized activity. Here's how it works:

# Administrative Privileges Check:
# The script verifies administrative privileges, ensuring it executes authorized actions securely.

# Conditional Execution:
# Based on provided arguments and administrative status, it selectively applies configurations to administer PowerShell sessions effectively.

# Registry Value Management:
# It controls access to the command prompt via registry values, limiting unauthorized usage within PowerShell.

# Secure Credential Handling:
# The script prompts users for credentials securely, limiting authentication attempts and handling errors to prevent unauthorized access.

# Structured Execution Flow:
# Continuously prompting for credentials until successful authentication,
# it ensures authorized access and protects against unauthorized activity within PowerShell environments.

# Check if the script is running with administrative privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Check if the script is running in non-administration mode and the choice is to apply only for admin PowerShell
if (-not $isAdmin -and $args[0] -eq "1") {
    Write-Host "This script is configured to apply only for admin PowerShell sessions."
    Exit
}

# Define variables
$registryKey = "HKCU:\Software\MyApplication"
$registryValueName = "DisableCMD"

# Function to set registry value
function Set-RegistryValue {
    param (
        [string]$key,
        [string]$valueName,
        [int]$valueData
    )

    try {
        # Check if the registry key exists, if not, create it
        if (-not (Test-Path -Path $key)) {
            New-Item -Path $key -Force | Out-Null
        }

        Set-ItemProperty -Path $key -Name $valueName -Value $valueData -ErrorAction Stop
        Write-Host "Registry value set successfully."
    } catch {
        Write-Host "Error setting registry value: $_"
    }
}

# Function to close the PowerShell window
function Close-PowerShellWindow {
    $host.SetShouldExit(0)
}

# Function to prompt for credentials
function Prompt-ForCredentials {
    param (
        [int]$attempt
    )

    # Get the directory where the script is located using $PSScriptRoot
    $scriptDirectory = $PSScriptRoot

    # Construct the path to the credentials file relative to the script's location
    $credentialsFile = Join-Path -Path $scriptDirectory -ChildPath "credentials.xml"

    # Load credentials from XML file
    try {
        $xml = [xml](Get-Content $credentialsFile)
        $storedUsername = $xml.credentials.username
        $storedPassword = $xml.credentials.password

        # Prompt user for credentials
        if ($attempt -lt 3) {
            $usernameInput = Read-Host "Username for Windows:"
            $passwordInput = Read-Host -Prompt "Passowrd for windows:" -AsSecureString
            $credential = New-Object System.Management.Automation.PSCredential ($usernameInput, $passwordInput)
        } else {
            $credential = Get-Credential -Message "Credintial for windows"
        }

        # Check if the user canceled the prompt
        if ($credential -eq $null) {
            Write-Host "Credential prompt canceled. PowerShell remains usable. if not close click CTRL+C to close "
            Close-PowerShellWindow
        }

        # Validate user credentials
        elseif (($credential.UserName -eq $storedUsername) -and ($credential.GetNetworkCredential().Password -eq ($storedPassword))) {
            Write-Host "Authentication successful. Access granted."
            return $true
        } else {
            Write-Host "Authentication failed. Access denied."
            return $false
        }
    } catch {
       Write-Host " 
            Sorry , [Windows] Require password !!
       "
    }
}

# Enable command prompt and PowerShell with user authentication
try {
    # If the choice is to apply for the whole PowerShell or admin PowerShell
    if ($args[0] -eq "0" -or $args[0] -eq "1") {
        # Set registry value to enable command prompt
        Set-RegistryValue -Key $registryKey -ValueName $registryValueName -ValueData 0
    }

    $authenticated = $false
    $attempts = 0

    while (-not $authenticated) {
        $authenticated = Prompt-ForCredentials -attempt $attempts

        # Increment attempt count if authentication fails
        if (-not $authenticated) {
            $attempts++
            if ($attempts -ge 3) {
                Write-Host "Maximum attempts reached. Opening popup window for credential entry."
                $authenticated = Prompt-ForCredentials -attempt $attempts
            }
            if ($attempts -ge 3) {
                Write-Host "Maximum attempts reached. Exiting script."
                Close-PowerShellWindow
            }
        }
    }

    # Additional operations can be performed here if needed after successful authentication

} catch {
    Write-Host "Error occurred: $_"
}
