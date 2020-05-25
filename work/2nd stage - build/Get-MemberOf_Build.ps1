<#
ToDo:
Adding ManagedBy to DL results > combining objects

Ask for clipboard like 
$Output = Write-Host -Prompt "Copy output to clipboard? > Y/N" `n "Press [1] for FNC, [2] for FS, [3] for VL/DL & [4] for everything."

If ($Output -is "Y") {
    If ($Output -is Y1 -or Y2 -or Y3 -or Y4)
        $ClipThis = 
    }
}
#>

### Base ###
$Worker = Read-Host -Prompt "Please enter user"
$MmbrOf = Get-ADObject -Filter "samaccountname -like '$Worker*' -or userprincipalname -like '$Worker*' -or mail -like '$Worker*' -or name -like '$Worker*'" -Properties MemberOf
$DisNames = $MmbrOf | Select-Object -ExpandProperty MemberOf

### Function Groups ###
Write-Host `n"processing Function Groups..."
    $FNCs = $DisNames | select-string -Pattern "FNC" -AllMatches
        $Clear_FNCs = Foreach ($FNC in $FNCs) {
            Get-ADGroup -Identity "$FNC" -Properties *
        }
Write-Host `n"User $Worker is member of following Function Groups:"
    $Clear_FNCs | Sort-Object | Format-Table -Property CN, Description -AutoSize -Wrap

### File Shares ###
Write-Host "processing File Shares..."
    $FSs = $DisNames | select-string -Pattern "ACC" -AllMatches
        $Clear_FSs = Foreach ($FS in $FSs) {
            Get-ADGroup -Identity "$FS" -Properties *
        }
Write-Host `n"User $Worker has access (RWXD/RX) to following file shares:"
    $Clear_FSs | Sort-Object | Format-Table -Property CN, Description -AutoSize -Wrap

### Distribution List ### !!! UNDER CONSTRUCTION !!! ###
    Write-Host "processing Distribution Lists..."
    $VLs = $DisNames | select-string -Pattern "Verteilerlisten" -AllMatches
        $Clear_VLs = foreach ($VL in $VLs) {
            Get-ADObject "$VL" -Properties *
        }
Write-Host `n"User $Worker is member of the following distribution lists:"
    $Clear_VLs | Sort-Object | Format-Table -Property DisplayName, mail -AutoSize -Wrap

Write-Host "Done.`nPress Return to close this window."
Read-Host