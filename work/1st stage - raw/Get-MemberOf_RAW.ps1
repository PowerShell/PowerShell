$Worker = Read-Host -Prompt "enter GUID"
$MmbrOf = Get-AdUser -Identity $Worker -Properties MemberOf
$DisNames = $MmbrOf | Select-Object -ExpandProperty MemberOf
$ClearNames = $DisNames | Get-ADObject -Properties CN, DisplayName, Description, mail
$ClearNames | Sort-Object | Format-Table -Property CN, DisplayName, mail, Description -AutoSize -Wrap