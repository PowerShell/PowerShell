# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
<############################################################################################
 # File: Pester.AutomountedDrives.Tests.ps1
 # Pester.AutomountedDrives.Tests suite contains Tests that are
 # used for validating automounted PowerShell drives.
 ############################################################################################>
$script:TestSourceRoot = $PSScriptRoot
Describe "Test suite for validating automounted PowerShell drives" -Tags @('Feature', 'Slow', 'RequireAdminOnWindows') {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"

        $AutomountVHDDriveScriptPath = Join-Path $script:TestSourceRoot 'AutomountVHDDrive.ps1'
        $vhdPath = Join-Path $TestDrive 'TestAutomountVHD.vhd'

        $AutomountSubstDriveScriptPath = Join-Path $script:TestSourceRoot 'AutomountSubstDrive.ps1'
        $substDir = Join-Path (Join-Path $TestDrive 'TestAutomountSubstDrive') 'TestDriveRoot'
        New-Item $substDir -ItemType Directory -Force | Out-Null

        $SubstNotFound = $false
        try { subst.exe } catch { $SubstNotFound = $true }

        $VHDToolsNotFound = $false
        try
        {
            $tmpVhdPath = Join-Path $TestDrive 'TestVHD.vhd'
            New-VHD -Path $tmpVhdPath -SizeBytes 5mb -Dynamic -ErrorAction Stop
            Remove-Item $tmpVhdPath
            $VHDToolsNotFound = (Get-Module Hyper-V).PrivateData.ImplicitRemoting -eq $true
            Remove-Module Hyper-V
        }
        catch
        { $VHDToolsNotFound = $true }
    }

    Context "Validating automounting FileSystem drives" {

        It "Test automounting using subst.exe" -Skip:$SubstNotFound {
           & $powershell -noprofile -command "& '$AutomountSubstDriveScriptPath' -FullPath '$substDir'" | Should -BeExactly "Drive found"
        }

        It "Test automounting using New-VHD/Mount-VHD" -Skip:$VHDToolsNotFound {
            & $powershell -noprofile -command "& '$AutomountVHDDriveScriptPath' -VHDPath '$vhdPath'" | Should -BeExactly "Drive found"
        }
    }

    Context "Validating automounting FileSystem drives from modules" {

        It "Test automounting using subst.exe" -Skip:$SubstNotFound {
           & $powershell -noprofile -command "& '$AutomountSubstDriveScriptPath' -useModule -FullPath '$substDir'" | Should -BeExactly "Drive found"
        }

        It "Test automounting using New-VHD/Mount-VHD" -Skip:$VHDToolsNotFound {
            $vhdPath = Join-Path $TestDrive 'TestAutomountVHD.vhd'
            & $powershell -noprofile -command "& '$AutomountVHDDriveScriptPath' -useModule -VHDPath '$vhdPath'" | Should -BeExactly "Drive found"
        }
    }
}
