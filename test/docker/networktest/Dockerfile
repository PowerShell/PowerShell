# escape=`
FROM mcr.microsoft.com/windows/servercore:ltsc2022

SHELL ["pwsh.exe","-command"]

# the source msi should change on a daily basis
# the destination should not change
ADD PSCore.msi /PSCore.msi

# the msipath (PSCore.msi) below will need to change based on the daily package
# set up for basic auth
# install-powershellremoting will restart winrm service
RUN new-LocalUser -Name testuser -password (ConvertTo-SecureString 11aa!!AA -asplaintext -force); `
    add-localgroupmember -group administrators -member testuser; `
    set-item wsman:/localhost/service/auth/basic $true; `
    set-item WSMan:/localhost/client/trustedhosts "*" -force; `
    set-item wsman:/localhost/service/AllowUnencrypted $true; `
    set-item WSMan:/localhost/client/AllowUnencrypted $true; `
    Start-Process -FilePath msiexec.exe -ArgumentList '-qn', `
    '-i c:\PSCore.msi','-log c:\PSCore-install.log','-norestart' -wait ; `
    $psexec = get-item -path ${ENV:ProgramFiles}/powershell/*/pwsh.exe; `
    $corehome = $psexec.directory.fullname; `
    & $psexec Install-PowerShellRemoting.ps1; `
    remove-item -force c:\PSCore.msi
