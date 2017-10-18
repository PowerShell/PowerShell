# VSTS Release Builds

## Requirements

Docker must be installed to run any of the release builds.

## Running Windows Release Builds

From PowerShell on Windows, run `.\vstsbuild.ps1 -ReleaseTag <tag> -Name <buildName>`.

Windows Build Names:

 * `win7-x64`
     * Builds the Universal Windows x64 Package
 * `win7-x86`
     * Builds the Universal Windows x86 Package
 * `win7-x64-symbols`
     * Builds the Windows x64 Zip with symbols
 * `win7-x86-symbols`
     * Builds the Windows x86 Zip with symbols

## Running Linux Release Builds

From PowerShell on Linux or macOS, run `.\vstsbuild.ps1 -ReleaseTag <tag> -Name <buildName>`.

Linux Build Names:

 * `ubuntu.14.04`
     * Builds the Ubuntu 14.04 Package and AppImage Package
 * `ubuntu.16.04`
     * Builds the Ubuntu 16.04 Package
 * `centos.7`
     * Builds the CentOS 7 Package
