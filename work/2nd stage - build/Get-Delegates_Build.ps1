<#
Get delegates of given Account - excluding SMB

last build: 19.11.2018
by UI221223
#>

# Input processing
$Who = Read-Host -Prompt "--> Please note: SMBs are currently not supported <--`nPlease enter Accountname, GUID or mailaddress"
Write-Host "...processing your request..."
$SRV = Get-ADObject -Filter "sAMAccountname -like '$Who*' -or mail -like '$Who*' -or userprincipalname -like '$Who*'" -Properties *

# check for VLs in publicDelegates and do something about them
$SRV_SAM = $SRV.sAMAccountName
$SRV_mail = $SRV.mail
$PD = @()
$PD += $SRV.publicDelegates
$PD += $SRV.msExchDelegateListBL
$PD += $SRV.msExchDelegateListLink
$OC = foreach ($entry in $PD) {
    Get-ADObject -Identity "$entry" -Properties ObjectClass | `
        Select-Object -ExpandProperty ObjectClass
} # end foreach
Write-Host `n"Checking Service Account $SRV_SAM"
# processing delegates and assembling stuff
$Clip_VL = Foreach ($entry in $PD) {
    if ($OC -notlike "user") {
        Get-ADObject -Identity "$entry" -Properties * | `
            Select-Object -ExpandProperty Member | `
            Get-ADObject -Properties DisplayName, sAMAccountName, mail
    } #end if
} #end foreach
$Clip_Ppl = Foreach ($entry in $PD) {
    if ($OC -like "user") {
        Get-ADObject -Identity "$entry" -Properties DisplayName, sAMAccountName, mail
    } #end if
} #end foreach
if ($Clip_Ppl.length -gt 0) {
    $Clip = $Clip_Ppl
} # end if
elseif ($Clip_VL.Length -gt 0) {
    $Clip = $Clip_VL
} # end elseif
# format everything nicely and put it on Screen + in Clipboard
$Table = $Clip | Select-Object DisplayName, sAMAccountName, mail | Sort-Object -Property DisplayName -Unique | Out-String
if ($Table.Length -gt 0) {
    Write-Host `n"Following users are delegated for" $SRV_mail":"
    $Table
    Write-Host "Table is copied to clipboard.`nPress return to close this window."
    Read-Host
    Set-Clipboard $Table
}
elseif ($Table.Length -like 0) {
    Read-Host -Prompt "Nothing to copy to clipboard.`nPress return to close this window."
}
<# 
C/P
#>