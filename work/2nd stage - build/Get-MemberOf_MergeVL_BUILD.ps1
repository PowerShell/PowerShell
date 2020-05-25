<#
Add VLinfos: DL DisplayName, Mail
Add MBinfos: Manager, Mail, Phone

Array?
PSObject?
PSCustomObject?
#>

# Basic work & Variables
$Who = "UI221223" <# needs to be User input, but lazy... #>

$MmbrOf = Get-ADObject -Filter "sAMAccountName -like '$Who*' -or UserPrincipalName -like '$Who*' -or mail -like '$who*' -or CN -like '$Who*'" -Properties memberOf
$DisNames = $MmbrOf | Select-Object -ExpandProperty memberOf | Sort-Object -Unique
$ClearNames = $DisNames | Get-ADObject -Properties *

# get VLs + Infos
$VLinfos = Foreach ($VL in ($ClearNames.DistinguishedName -match "Verteilerlisten*")) {
    Get-ADObject -Filter "DistinguishedName -like '$VL'" -Properties *
}

# get mBs + Infos
$MBinfos = Foreach ($MB in $VLInfos.ManagedBy) {
    Get-ADUser -Filter "DistinguishedName -like '$MB'" -Properties *
}

return $VLinfos
return $MBinfos

<#
#distinguished name of the ou
$distname = Get-ADUser SRV_RWEST_LNGOPS2018 -Properties publicDelegates
$pd = (Get-ADObject -Filter * -Properties * -SearchBase "$distname" | Where-Object {$_.publicDelegates -ne $null} | Sort-Object SamAccountName )

#listview with distinguished name
Write-Host "##### Listview #####"
Write-Host " "
foreach ($delegates in $pd){
$ident = $delegates.DistinguishedName
$DisplayName = $delegates.DisplayName
$perm = @(Get-ADObject -Identity "$ident" -Properties *  | Select-Object publicDelegates -ExpandProperty publicDelegates | Out-String)
$perm = $perm
Write-Host "Get user delegation for"
Write-Host "User:" $DisplayName
Write-Host $perm
}

#tableoverview
Write-Host "##### Tableview #####"

foreach ($delegates in $pd){

$ident = $delegates.DistinguishedName
$DisplayName = $delegates.DisplayName
$perm = @(Get-ADObject -Identity "$ident" -Properties * | Select-Object SamAccountName,Mail,@{n='Delegations';e={$_.publicDelegates-replace '^CN=|,.*$'}})
$perm | ft -AutoSize -Wrap
}
pause
#>