$LogonSrv = Read-Host -Prompt "SRV?" | Out-String
$LoginUser = Read-Host -Prompt "USR?" | Out-String
$LoginPwd = Read-Host -Prompt "PWD?" -AsSecureString
$SecPwd = ConvertTo-SecureString $LoginPwd -AsPlainText -Force
$Ready = New-Object -TypeName pscredential $LoginUser, $SecPwd
try {
    PING.EXE $LogonSrv -c 2 -i 255
}
catch {
    $LogonSrv.
}
$Worker = Get-WmiObject -ClassName "Win32_Service" -ComputerName $LogonSrv -Credential $Ready -Filter "name -like 'MSMQ*'" -EnableAllPrivileges
$Worker.getType()