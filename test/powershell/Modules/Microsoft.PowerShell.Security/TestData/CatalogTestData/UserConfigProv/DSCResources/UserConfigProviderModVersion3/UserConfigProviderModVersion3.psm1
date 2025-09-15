# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# The Get-TargetResource cmdlet is used to fetch the desired state of the DSC managed node through a powershell script.
# This cmdlet executes the user supplied script (i.e., the script is responsible for validating the desired state of the
# DSC managed node). The result of the script execution is in the form of a hashtable containing all the information
# gathered from the GetScript execution.
function Get-TargetResource {
    [CmdletBinding()]
    param(
       [Parameter(Mandatory = $true)]
       [ValidateNotNullOrEmpty()]
       [string]
       $Text
    )

    $result = @{
        Text = "Hello from Get!"
    }

    $result
}

# The Set-TargetResource cmdlet is used to Set the desired state of the DSC managed node through a powershell script.
# The method executes the user supplied script (i.e., the script is responsible for validating the desired state of the
# DSC managed node). If the DSC managed node requires a restart either during or after the execution of the SetScript,
# the SetScript notifies the PS Infrastructure by setting the variable $DSCMachineStatus.IsRestartRequired to $true.
function Set-TargetResource {
    [CmdletBinding()]
    param(
       [Parameter(Mandatory = $true)]
       [ValidateNotNullOrEmpty()]
       [string]
       $Text
    )

    $path = "$env:SystemDrive\dscTestPath\hello3.txt"
    New-Item -Path $path -Type File -Force
    Add-Content -Path $path -Value $text
}

# The Test-TargetResource cmdlet is used to validate the desired state of the DSC managed node through a powershell script.
# The method executes the user supplied script (i.e., the script is responsible for validating the desired state of the
# DSC managed node). The result of the script execution should be true if the DSC managed machine is in the desired state
# or else false should be returned.
function Test-TargetResource {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Text
    )
    $false
}
