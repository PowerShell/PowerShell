# Build login
${LogonTo} = Read-Host -Prompt "Bitte Server eingeben"
${LogonAs} = Read-Host -Prompt "Bitte Nutzernamen eingeben"
${Pwd} = Read-Host -Prompt "Bitte Passwort eingeben" -AsSecureString
${Logon} = (New-Object -TypeName pscredential -ArgumentList $LogonAs, $Pwd)

Get-WmiObject -Class WIN32_Service -Property * -Filter * -ComputerName $LogonTo -Credential $Logon -Authentication Default -EnableAllPrivileges