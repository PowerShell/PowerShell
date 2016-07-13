##
## Copyright (c) Microsoft Corporation, 2015
##
## Test to verify required module processing with visible cmdlets set.
##

# Path for test module files.
if (($MyInvocation.InvocationName -ne $null) -and ($MyInvocation.InvocationName -ne "") -and (Test-Path $MyInvocation.InvocationName))
{
    $path = Split-Path $MyInvocation.InvocationName
}
else
{
    # Try using current path.
    $path = $pwd
}

# Pre-existing module files with *required modules* used for test.
$testModuleName = "MyRequiredModules"
$testModulePSSC = "RequiredModules.pssc"

#
# This bug appeared after changing sequence of processing required modules on a remote session.
# The bug occurred because Import-Module was removed before processing required modules, which needs that cmdlet.
# Fix is to not remove Import-Module for cmdlet visibility but instead make it private so it can still be 
# used internally.
#
Describe "Tests successfully creating a restricted endpoint with required modules" -Tags "InnerLoop","P1" {

    It "Creates a required modules endpoint and a new session on that endpoint." {
        try
        {
            # Copy test module to $PSHome\Modules path.
            $destPath = Join-Path $PSHOME\Modules $testModuleName
            new-item -type directory $destPath
            copy-item -Path (Join-Path -Path $path -ChildPath ($testModuleName + '.psd1')) -Destination $destPath
            copy-item -Path (Join-Path -Path $path -ChildPath ($testModuleName + '.psm1')) -Destination $destPath

            # Create restricted endpoint that imports the test module, using PSSC file.
            Register-PSSessionConfiguration -Name $testModuleName -Path (Join-Path -Path $path -ChildPath $testModulePSSC) -Force

            # Create a session on the restricted endpoint.
            $session = New-PSSession -ConfigurationName $testModuleName

            # Session should be created without error.
            $session | Should Not Be $null
            ($session.State -eq [System.Management.Automation.Runspaces.RunspaceState]::Opened) | Should Be $true

            # Restricted session should not allow public access to Import-Module.
            $result = Invoke-Command -Session $session -ScriptBlock { Get-Command Import-Module } 2>&1
            $result.Count | Should Be 1
            ($result[0] -is [System.Management.Automation.ErrorRecord]) | Should Be $true

            # Restricted session should have the required module "MyRequiredModules" loaded.
            $result = Invoke-Command -Session $session -ScriptBlock { Get-MyModule -Title "Hello" }
            $result.Count | Should Be 1
            ($result -match "Hello") | Should Be $true
        }
        finally
        {
            ##
            ## Clean up
            ##

            if ($session -ne $null)
            {
                $session | Remove-PSSession
            }

            $sc = Get-PSSessionConfiguration -Name $testModuleName 2>$null
            if ($sc -ne $null)
            {
                $sc | Unregister-PSSessionConfiguration -force
            }

            if (Test-Path $destPath)
            {
                Remove-Item -Path $destPath -Recurse -Force
            }
        }
    }
}
