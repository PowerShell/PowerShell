# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe "Verify PowerShell Runs" {
    BeforeAll{
        $options = (Get-PSOptions)
        $path = Split-Path -Path $options.Output
        Write-Verbose "Path: '$path'" -Verbose
        $rootPath = Split-Path -Path $path
        $mount = 'C:\powershell'
        $container = 'mcr.microsoft.com/powershell:nanoserver-1803'
    }

    It "Verify Version " {
        $version = docker run --rm -v "${rootPath}:${mount}" ${container} "${mount}\publish\pwsh" -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
        $version | Should -Match '^7\.'
    }
}
