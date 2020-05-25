$Worker = Get-ADUser -Filter 'Department -like "GHP*"' -Properties *
    ForEach ($user in $Worker) {
        $ClearUser = Get-AdUser -Identity $user -Properties DisplayName, SamAccountName, mail
        $ClearUser | Format-Table -Property DisplayName, SamAccountName, mail -AutoSize -Wrap
        Write-Host "is member of the following"
        $DisNames = Get-AdUser -Identity $user -Properties MemberOf | Select-Object -ExpandProperty MemberOf
        $ClearNames = $DisNames | Get-ADObject -Properties CN, DisplayName, Description, mail
        $ClearNames | Sort-Object | Format-Table -Property CN, DisplayName, mail, Description -AutoSize -Wrap
    }

    
<#
Export-Csv -Path C:\Temp\export.csv -InputObject $Object

$ghpusrs = get-aduser -Filter 'Department -like "GHP*"' -Properties *
foreach ($user in $ghpgrps) {
    get-aduser -Properties *
}
get-content $ghpdetails
$ghpgrps = get-aduser $ghpusrs -Properties memberof | Select-Object -ExpandProperty memberof
$ghpdetails = get-adobject $ghpgrps -Properties * | Format-Table cn,displayname,description
#>