$menu = 'Open Windows PowerShell Here as Administrator'
$command = "$PSHOME\powershell.exe -NoExit -NoProfile -Command ""Set-Location '%V'"""
 
'directory', 'directory\background', 'drive' | ForEach-Object {
    New-Item -Path "Registry::HKEY_CLASSES_ROOT\$_\shell" -Name runas\command -Force |
    Set-ItemProperty -Name '(default)' -Value $command -PassThru |
    Set-ItemProperty -Path {$_.PSParentPath} -Name '(default)' -Value $menu -PassThru |
    Set-ItemProperty -Name HasLUAShield -Value ''
}