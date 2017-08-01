# Default profile for all users for PowerShell host

if ($IsWindows)
{
	# Add Windows PowerShell PSModulePath to make it easier to discover potentially compatible PowerShell modules
	# If a Windows PowerShell module works or not, please provide feedback at https://github.com/PowerShell/PowerShell/issues/4062

	Write-Warning "Appended Windows PowerShell PSModulePath"
	$env:psmodulepath += ";${env:userprofile}\Documents\WindowsPowerShell\Modules;${env:programfiles}\WindowsPowerShell\Modules;${env:windir}\system32\WindowsPowerShell\v1.0\Modules\"
}
