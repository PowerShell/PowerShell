# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe "Verify PowerShell Runs" {
    BeforeAll{
        $options = (Get-PSOptions)
        $path = split-path -path $options.Output
        Write-Verbose "Path: '$path'" -Verbose
        $rootPath = split-Path -path $path
        $mount = 'C:\powershell'
        $container = 'mcr.microsoft.com/powershell:nanoserver-1803'
    }

    it "Verify Version " {
        $version = docker run --rm -v "${rootPath}:${mount}" ${container} "${mount}\publish\pwsh" -NoLogo -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
        $version | Should -match '^7\.'
    }
}
