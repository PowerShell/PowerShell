$users = ForEach ($user in (Get-Content '\\C017A7205.group.rwe.com\C$\Users\ui221223\Downloads\executives.1.txt')) {
    Get-AdUser -Filter {mail -like $user} -Properties Name,SamAccountName
}
$users | Select-Object Name,SamAccountName,Mail |  Export-CSV -Path '\\C017A7205.group.rwe.com\C$\Users\ui221223\Downloads\executives.2.csv' -NoTypeInformation