# Godmodules
# $null
# $true
# $false
[PSObject[]]$Clip,
[String[]]$VL_Clip = @()
[String[]]$mB_Clip = @()
# $ResultHash = @{}

# Submodules sourcing data
$VL_list = @()
$mB_list = @()
$VL_list += Get-ADUser ui221223 -Properties * | Select-Object -ExpandProperty MemberOf
$counter = 0

# Run VLs from Submodule
foreach ($VL in $VL_list) {
    $clips = New-Object PSObject
    $handover_mBs = New-Object PSObject
    $VLs = Get-ADObject "$VL" -Properties *
    if ($VLs.mailNickname -like "VL*") {
        $counter++
        $clips | Add-Member -MemberType NoteProperty -Name "DL_DisplayName" -Value $VLs.DisplayName
        $clips | Add-Member -MemberType NoteProperty -Name "DL_mail" -Value $VLs.mail
        $clips | Add-Member -MemberType NoteProperty -Name "Manager" -Value ""
        $clips | Add-Member -MemberType NoteProperty -Name "Manager_mail" -Value ""
        $clips | Add-Member -MemberType NoteProperty -Name "Manager_phone" -Value ""
        $handover_mBs | Add-Member -MemberType NoteProperty -Name "Manager" -Value $VLs.managedBy
        $VL_Clip += $clips
        $mB_list += $handover_mBs
    } # end if
} # end foreach
# $Clip

# Run Manager from Submodule
$Managerlist = $mB_list | Select-Object -ExpandProperty Manager
foreach ($mB in $Managerlist) {
    $clips = New-Object PSObject
    $manager = Get-AdObject "$mB" -Properties *
    if (!$manager.Deleted -like $true) {
        $counter++
        $clips | Add-Member -MemberType NoteProperty -Name "DL_DisplayName" -Value ""
        $clips | Add-Member -MemberType NoteProperty -Name "DL_mail" -Value ""
        $clips | Add-Member -MemberType NoteProperty -Name "Manager" -Value $manager.CN
        $clips | Add-Member -MemberType NoteProperty -Name "Manager_mail" -Value $manager.mail
        $clips | Add-Member -MemberType NoteProperty -Name "Manager_phone" -Value $manager.mobile
        $mB_Clip += $clips
    } # end if
} # end foreach
$Clip += $VL_Clip
$Clip += $mB_Clip
$Clip | Format-Table -AutoSize -Wrap

<#
#distinguished name
$DNs = New-Object Array

[System.Management.Automation.PSMemberViewTypes]
$PDs = (Get-ADUser "$DNs" -Properties * | Where-Object {} | Sort-Object SamAccountName)

#listview with distinguished name
Write-Host "##### Listview #####"
Write-Host " "
foreach ($DNs in $PDs)
    {
    $ident = $delegates.DistinguishedName
    $DisplayName = $delegates.DisplayName
    $perm = @(Get-ADObject -Identity "$ident" -Properties *  | Select-Object publicDelegates -ExpandProperty publicDelegates | Out-String)
    $perm = $perm
    Write-Host "Get user delegation for:" "$DisplayName"
    Write-Host " "
    Write-Host $perm
    }

#tableoverview
Write-Host "##### Tableview #####"

foreach ($delegates in $pd)
    {
    $ident = $delegates.DistinguishedName
    $DisplayName = $delegates.DisplayName
    $perm = @(Get-ADObject -Identity "$ident" -Properties * | Select-Object SamAccountName,Mail,@{n='Delegations';e={$_.publicDelegates-replace '^CN=|,.*$'}})
    $perm | ft -AutoSize -Wrap
    }
#>