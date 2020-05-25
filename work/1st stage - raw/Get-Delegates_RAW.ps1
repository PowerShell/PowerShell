$Who = Read-Host -Prompt "Please Enter SRV-Name or mailaddress"
$SRV = Get-ADObject -Filter "sAMAccountName -like '$Who*' -or userprincipalname -like '$Who*' -or mail -like '$Who*'" -Properties *
$SRV_SAM = $SRV.SamAccountName
$SRV_mail = $SRV.UserPrincipalName
$PD = @()
$PD += $SRV.publicDelegates
$PD += $SRV.msExchDelegateListBL
$PD += $SRV.msExchDelegateListLink
$OC = foreach ($entry in $PD) {
    Get-ADObject -Identity "$entry" -Properties * | Select-Object -ExpandProperty ObjectClass
} # end foreach
Write-Host `n"Checking Service Account $SRV_SAM"`n"Following users are delegated for" $SRV_mail":"`n
$Clip_VL = Foreach ($entry in $PD) {
    if ($OC -notlike "user") {
        Get-ADObject -Identity "$entry" -Properties * | `
            Select-Object -ExpandProperty Member | `
            Get-ADUser -Properties DisplayName, sAMAccountName, mail
    } #end if
} #end foreach
$Clip_Ppl = Foreach ($entry in $PD) {
    if ($OC -like "user") {
        Get-ADUser -Identity "$entry" -Properties DisplayName, sAMAccountName, mail
    } #end if
} #end foreach
if ($Clip_Ppl.length -gt 0) {
    $Clip = $Clip_Ppl
} # end if
elseif ($Clip_VL.Length -gt 0) {
    $Clip = $Clip_VL
} # end elseif
$Table = $Clip | Select-Object DisplayName, sAMAccountName, mail | Sort-Object -Property DisplayName -Unique | Out-String
Set-Clipboard $Table
$Table
Read-Host -Prompt "copied to clipboard, press return to close"