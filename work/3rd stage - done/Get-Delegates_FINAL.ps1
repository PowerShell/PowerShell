<#
Find delegates of a mail account, checked against Active Directory.
+ service/user accounts, SMBs, DLs
+ provide additional account info: alternative mail addresses, display names, account/SMB name

+ delegation level flags from MS Exchange (FA, Send As, Send on behalf)
+ resolve delegated DLs into real users
+ provide contact data of delegated users

+ Switch: Output into clipboard

last build: 19.11.18 - NOT Final Build
UI221223
#>
function Get-Delegates
{
    [CmdletBinding()]
    [Alias()]
    [OutputType([string])]
    Param
    (
        # Name Mailbox
        [Parameter (Mandatory=$true,
        ValueFromPipelineByPropertyName=$true,
        Position=1)]
        [Parameter]
        ${SRV_man}
        )

    Begin
    {
    ${SRV_man} = Read-Host -Prompt "Please Enter SRV-Name"
    Write-Host `n"Checking Service Account $SRV_SAM"`n
    }
    
    Process
    {
    $SRV = Get-ADUser -Identity $SRV_man -Properties *
    $SRV_SAM = Get-ADUser $SRV.DisplayName
    
    $PD = $SRV.publicDelegates
    
    $Names = Get-ADUser -Identity $PD -Properties Name
    }
    
    End
    {
    Write-Host "Following users are delegated for" $SRV_SAM":"`n
    Format-Table -InputObject $Names -AutoSize -HideTableHeaders
    }
}
