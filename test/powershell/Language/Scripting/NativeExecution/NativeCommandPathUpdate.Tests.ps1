# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

## The "Path Update" feature is only available on Windows.
## Skip the test suite on Unix platforms.
if (-not $IsWindows) {
    return;
}

function GetEnvPathLiteralValue {
    param(
        [System.EnvironmentVariableTarget] $Target
    )

    if ($Target -eq 'User') {
        $regKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment')
    } elseif ($Target -eq 'Machine') {
        $regKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey('SYSTEM\CurrentControlSet\Control\Session Manager\Environment')
    } else {
        return [PSCustomObject]@{ Kind = $null; Value = $env:Path }
    }

    try {
        $kind = $regKey.GetValueKind('Path')
        $value = $regKey.GetValue('Path', $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)

        return [PSCustomObject]@{
            Kind = $kind
            Value = $value
        }
    }
    finally {
        ${regKey}?.Dispose()
    }
}

function RestoreEnvPath {
    param(
        [System.EnvironmentVariableTarget] $Target,
        [Microsoft.Win32.RegistryValueKind] $ValueKind,
        [string] $LiteralValue
    )

    ## Open the registry key with 'write' access.
    if ($Target -eq 'User') {
        $regKey = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Environment', $true)
    } elseif ($Target -eq 'Machine') {
        $regKey = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey('SYSTEM\CurrentControlSet\Control\Session Manager\Environment', $true)
    } else {
        ## Ignore value kind when restoring the in-proc env Path.
        $env:Path = $LiteralValue
        return
    }

    try {
        $regKey.SetValue('Path', $LiteralValue, $ValueKind)
    } finally {
        ${regKey}?.Dispose()
    }
}

function UpdatePackageManager {
    param(
        [Parameter(ParameterSetName = 'Add')]
        [switch] $Add,

        [Parameter(ParameterSetName = 'Remove')]
        [switch] $Remove,

        [string] $Name
    )

    $regKeyPath = 'HKLM:\Software\Microsoft\Command Processor\KnownPackageManagers'
    $keyExists = Test-Path -Path $regKeyPath
    if (-not $keyExists) {
        Write-Host -ForegroundColor Cyan "The registry key 'KnownPackageManagers' doesn't exist."
    }

    $subKeyPath = "$regKeyPath\$Name"
    if ($Add) {
        $null = New-Item $subKeyPath -Force -ErrorAction Stop
    }
    elseif ($Remove -and $keyExists) {
        Remove-Item $subKeyPath -Recurse -Force -ErrorAction Stop
    }
}

Describe "Path update for package managers" -tags @('CI', 'RequireAdminOnWindows') {

    It "Path update is off for an executable that is not registered" {
        try {
            $oldUserPath = GetEnvPathLiteralValue -Target 'User'
            $oldSysPath = GetEnvPathLiteralValue -Target 'Machine'
            $oldProcPath = $env:Path

            testexe -updateuserandsystempath

            $newUserPath = GetEnvPathLiteralValue -Target 'User'
            $newUserPath.Kind | Should -Be $oldUserPath.Kind -Because "Value kind should not be changed"
            $newUserPath.Value | Should -BeLike "$($oldUserPath.Value)*X:\not-exist-user-path" -Because "'testexe -updateuserandsystempath' should append 'X:\not-exist-user-path' to the User Path."

            $newSysPath = GetEnvPathLiteralValue -Target 'Machine'
            $newSysPath.Kind | Should -Be $oldSysPath.Kind -Because "Value kind should not be changed"
            $newSysPath.Value | Should -BeLike "X:\not-exist-sys-path*$($oldSysPath.Value)" -Because "'testexe -updateuserandsystempath' should prepend 'X:\not-exist-sys-path' to the System Path."

            $newProcPath = $env:Path
            $newProcPath | Should -Be $oldProcPath -Because "'testexe -updateuserpath' doesn't change the Process Path and the executable 'testexe' is not in the package manager list."
        }
        finally {
            if ($oldUserPath -ne $null) {
                RestoreEnvPath -Target 'User' -ValueKind $oldUserPath.Kind -LiteralValue $oldUserPath.Value
            }

            if ($oldSysPath -ne $null) {
                RestoreEnvPath -Target 'Machine' -ValueKind $oldSysPath.Kind -LiteralValue $oldSysPath.Value
            }
        }
    }

    ## Add the executable name without extension to the list of package managers.
    Context "Add 'testexe' to the list and test 'Path Update'" {
        BeforeAll {
            UpdatePackageManager -Add -Name 'testexe'
        }

        AfterAll {
            UpdatePackageManager -Remove -Name 'testexe'
        }

        It "Test when only User Path is changed" {
            try {
                $oldUserPath = GetEnvPathLiteralValue -Target 'User'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-user-path' will be appended to User Path.
                    testexe -updateuserpath

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path item added to 'User Path' should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path item added to 'User Path' should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-user-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-user-path'
                }
            }
            finally {
                if ($oldUserPath -ne $null) {
                    RestoreEnvPath -Target 'User' -ValueKind $oldUserPath.Kind -LiteralValue $oldUserPath.Value
                }
            }
        }

        It "Test when only System Path is changed" {
            try {
                $oldSysPath = GetEnvPathLiteralValue -Target 'Machine'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-sys-path' will be prepended to System Path.
                    testexe -updatesystempath > $null

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path item added to 'System Path' should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path item added to 'System Path' should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-sys-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-sys-path'
                }
            }
            finally {
                if ($oldSysPath -ne $null) {
                    RestoreEnvPath -Target 'Machine' -ValueKind $oldSysPath.Kind -LiteralValue $oldSysPath.Value
                }
            }
        }

        It "Test when both User and System Paths are changed" {
            try {
                $oldUserPath = GetEnvPathLiteralValue -Target 'User'
                $oldSysPath = GetEnvPathLiteralValue -Target 'Machine'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-user-path' will be appended to User Path.
                    ## New item 'X:\not-exist-sys-path' will be prepended to System Path.
                    $null = testexe -updateuserandsystempath

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path items should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path items should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-user-path;X:\not-exist-sys-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-user-path;X:\not-exist-sys-path'
                }
            }
            finally {
                if ($oldUserPath -ne $null) {
                    RestoreEnvPath -Target 'User' -ValueKind $oldUserPath.Kind -LiteralValue $oldUserPath.Value
                }

                if ($oldSysPath -ne $null) {
                    RestoreEnvPath -Target 'Machine' -ValueKind $oldSysPath.Kind -LiteralValue $oldSysPath.Value
                }
            }
        }

        It "Test when neither User nor System Path is changed" {
            $oldProcPath, $newProcPath = pwsh -noprofile -c {
                $oldPath = $env:Path

                ## Print help message and exit.
                testexe -h > $null

                $newPath = $env:Path
                $oldPath, $newPath
            }

            $newProcPath | Should -Be $oldProcPath -Because "'testexe -h' doesn't change the env Path."
        }
    }

    ## Add the executable name with extension to the list of package managers.
    Context "Add 'testexe.exe' to the list and test 'Path Update'" {
        BeforeAll {
            UpdatePackageManager -Add -Name 'testexe.exe'
        }

        AfterAll {
            UpdatePackageManager -Remove -Name 'testexe.exe'
        }

        It "Test when only User Path is changed" {
            try {
                $oldUserPath = GetEnvPathLiteralValue -Target 'User'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-user-path' will be appended to User Path.
                    testexe -updateuserpath

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path item added to 'User Path' should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path item added to 'User Path' should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-user-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-user-path'
                }
            }
            finally {
                if ($oldUserPath -ne $null) {
                    RestoreEnvPath -Target 'User' -ValueKind $oldUserPath.Kind -LiteralValue $oldUserPath.Value
                }
            }
        }

        It "Test when only System Path is changed" {
            try {
                $oldSysPath = GetEnvPathLiteralValue -Target 'Machine'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-sys-path' will be prepended to System Path.
                    testexe -updatesystempath > $null

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path item added to 'System Path' should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path item added to 'System Path' should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-sys-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-sys-path'
                }
            }
            finally {
                if ($oldSysPath -ne $null) {
                    RestoreEnvPath -Target 'Machine' -ValueKind $oldSysPath.Kind -LiteralValue $oldSysPath.Value
                }
            }
        }

        It "Test when both User and System Paths are changed" {
            try {
                $oldUserPath = GetEnvPathLiteralValue -Target 'User'
                $oldSysPath = GetEnvPathLiteralValue -Target 'Machine'

                $oldProcPath, $newProcPath = pwsh -noprofile -c {
                    $oldPath = $env:Path

                    ## New item 'X:\not-exist-user-path' will be appended to User Path.
                    ## New item 'X:\not-exist-sys-path' will be prepended to System Path.
                    $null = testexe -updateuserandsystempath

                    $newPath = $env:Path
                    $oldPath, $newPath
                }

                $newProcPath.Length | Should -BeGreaterThan $oldProcPath.Length -Because "Path should be updated. The new path items should be appended to 'Process Path'."
                $newProcPath.IndexOf($oldProcPath) | Should -Be 0 -Because "Path should be updated. The new path items should be appended to 'Process Path'."

                $newItem = $newProcPath.SubString($oldProcPath.Length)
                if ($oldProcPath.EndsWith(';')) {
                    $newItem | Should -Be 'X:\not-exist-user-path;X:\not-exist-sys-path'
                }
                else {
                    $newItem | Should -Be ';X:\not-exist-user-path;X:\not-exist-sys-path'
                }
            }
            finally {
                if ($oldUserPath -ne $null) {
                    RestoreEnvPath -Target 'User' -ValueKind $oldUserPath.Kind -LiteralValue $oldUserPath.Value
                }

                if ($oldSysPath -ne $null) {
                    RestoreEnvPath -Target 'Machine' -ValueKind $oldSysPath.Kind -LiteralValue $oldSysPath.Value
                }
            }
        }

        It "Test when neither User nor System Path is changed" {
            $oldProcPath, $newProcPath = pwsh -noprofile -c {
                $oldPath = $env:Path

                ## Print help message and exit.
                testexe -h > $null

                $newPath = $env:Path
                $oldPath, $newPath
            }

            $newProcPath | Should -Be $oldProcPath -Because "'testexe -h' doesn't change the env Path."
        }
    }
}
