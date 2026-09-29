# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
<############################################################################################
 # File: Pester.AutomountedDrives.Tests.ps1
 # Pester.AutomountedDrives.Tests suite contains Tests that are
 # used for validating automounted PowerShell drives.
 ############################################################################################>
$script:TestSourceRoot = $PSScriptRoot

BeforeDiscovery {
    $SubstNotFound = $true
    $VHDToolsNotFound = $true
    if ($IsWindows) {
        $SubstNotFound = $false
        try { $null = subst.exe } catch { $SubstNotFound = $true }

        try {
            $tmpVhdPath = Join-Path ([System.IO.Path]::GetTempPath()) "TestProbeVHD_$([guid]::NewGuid()).vhd"
            New-VHD -Path $tmpVhdPath -SizeBytes 5mb -Dynamic -ErrorAction Stop | Out-Null
            Remove-Item $tmpVhdPath -ErrorAction SilentlyContinue
            $VHDToolsNotFound = (Get-Module Hyper-V).PrivateData.ImplicitRemoting -eq $true
            Remove-Module Hyper-V -ErrorAction SilentlyContinue
        }
        catch
        { $VHDToolsNotFound = $true }
    }
}

Describe "Test suite for validating automounted PowerShell drives" -Tags @('Feature', 'Slow', 'RequireAdminOnWindows') -Skip:(-not $IsWindows) {

    BeforeAll {
        $powershell = Join-Path -Path $PSHOME -ChildPath "pwsh"

        $AutomountVHDDriveScriptPath = Join-Path $PSScriptRoot 'AutomountVHDDrive.ps1'
        $vhdPath = Join-Path $TestDrive 'TestAutomountVHD.vhd'

        $AutomountSubstDriveScriptPath = Join-Path $PSScriptRoot 'AutomountSubstDrive.ps1'
        $substDir = Join-Path (Join-Path $TestDrive 'TestAutomountSubstDrive') 'TestDriveRoot'
        New-Item $substDir -ItemType Directory -Force | Out-Null
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
