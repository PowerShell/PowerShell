<#
Change PowerShell execution policy to unrestricted
Show change before to after on screen

Last Build: 23.01.2019
by: UI221223
#>
$before = Get-ExecutionPolicy -List | Out-String
Set-ExecutionPolicy Unrestricted -Force
Set-ExecutionPolicy Unrestricted -Scope Process -Force
$after = Get-ExecutionPolicy -List | Out-String
Write-Host "That was before:"`n$before`n"now it is:"`n$after
<#
c/p
#>