# PowerShell Core Releasing Process

## Release Steps

When creating a release milestone, you should send meeting invites to maintainers to book the release day and the previous day.
This is to make sure they have enough time to work on the release.

The following release procedure should be started on the previous day of the target release day.
This is to make sure we have enough buffer time to publish the release on the target day.

Before starting the following release procedure, open an issue and list all those steps as to-do tasks.
Check the task when you finish one.
This is to help track the release preparation work.

> Note: Step 2, 3 and 4 can be done in parallel.

1. Create a branch named `release-<Release Tag>` in our private repository.
   All release related changes should happen in this branch.
1. Prepare packages
   - [Build release packages](#building-packages).
   - Sign the MSI packages and DEB/RPM packages.
   - Install and verify the packages.
1. Update documentation, scripts and Dockerfiles
   - Summarize the change log for the release. It should be reviewed by PM(s) to make it more user-friendly.
   - Update [CHANGELOG.md](../../CHANGELOG.md) with the finalized change log draft.
   - Update other documents and scripts to use the new package names and links.
1. Verify the release Dockerfiles.
1. [Create NuGet packages](#nuget-packages) and publish them to [powershell-core feed][ps-core-feed].
1. [Create the release tag](#release-tag) and push the tag to `PowerShell/PowerShell` repository.
1. Create the draft and publish the release in Github.
1. Merge the `release-<Release Tag>` branch to `master` in `powershell/powershell` and delete the `release-<Release Tag>` branch.
1. Publish Linux packages to Microsoft YUM/APT repositories.
1. Trigger the release docker builds for Linux and Windows container images.
   - Linux: push a branch named `docker` to `powershell/powershell` repository to trigger the build at [powershell docker hub](https://hub.docker.com/r/microsoft/powershell/builds/).
     Delete the `docker` branch once the builds succeed.
   - Windows: queue a new build in `PowerShell Windows Docker Build` on VSTS.
1. Verify the generated docker container images.
1. [Update the homebrew formula](#homebrew) for the macOS package.
   This task usually will be taken care of by the community,
   so we can wait for one day or two and see if the homebrew formula has already been updated,
   and only do the update if it hasn't.

## Building Packages

> Note: Linux and Windows packages are taken care of by our release build pipeline in VSTS,
while the macOS package needs to be built separately on a macOS.

The release build should be started based on the `release` branch.
The release Git tag won't be created until all release preparation tasks are done,
so the to-be-used release tag should be passed to the release build as an argument.

> When creating the packages, please ensure that the file path does not contain user names.
That is, clone to `/PowerShell` on Unix, and `C:\PowerShell` for Windows.
The debug symbols include the absolute path to the sources when built,
so it should appear `/PowerShell/src/powershell/System.Management.Automation`,
not `/home/username/src/PowerShell/...`.

### Packaging Overview

The `tools/packaging` module contains a `Start-PSPackage` function to build packages.
It **requires** that PowerShell Core has been built via `Start-PSBuild` from the `build.psm1` module.

#### Windows

The `Start-PSPackage` function delegates to `New-MSIPackage` which creates a Windows Installer Package of PowerShell.
The packages *must* be published in release mode,
so make sure `-Configuration Release` is specified when running `Start-PSBuild`.

It uses the Windows Installer XML Toolset (WiX) to generate a MSI package,
which copies the output of the published PowerShell files to a version-specific folder in Program Files,
and installs a shortcut in the Start Menu.
It can be uninstalled through `Programs and Features`.

Note that PowerShell is always self-contained, thus using it does not require installing it.
The output of `Start-PSBuild` includes a `powershell.exe` executable which can simply be launched.

#### Linux / macOS

The `Start-PSPackage` function delegates to `New-UnixPackage`.
It relies on the [Effing Package Management][fpm] project,
which makes building packages for any (non-Windows) platform a breeze.
Similarly, the PowerShell man-page is generated from the Markdown-like file
[`assets/pwsh.1.ronn`][man] using [Ronn][].
The function `Start-PSBootstrap -Package` will install both these tools.

To modify any property of the packages, edit the `New-UnixPackage` function.
Please also refer to the function for details on the package properties
(such as the description, maintainer, vendor, URL,
license, category, dependencies, and file layout).

> Note that the only configuration on Linux and macOS is `Linux`,
> which is release (i.e. not debug) configuration.

To support side-by-side Unix packages, we use the following design:

We will maintain a `powershell` package
which owns the `/usr/bin/pwsh` symlink,
is the latest version, and is upgradeable.
This is the only package named `powershell`
and similarly is the only package owning any symlinks,
executables, or man-pages named `powershell`.
Until we have a package repository,
this package will contain actual PowerShell bits
(i.e. it is not a meta-package).
These bits are installed to `/opt/microsoft/powershell/6.0.0-alpha.8/`,
where the version will change with each update
(and is the pre-release version).
On macOS, the prefix is `/usr/local`,
instead of `/opt/microsoft` because it is derived from BSD.

> When we have access to package repositories where dependencies can be properly resolved,
> this `powershell` package can become a meta-package which auto-installs the latest package,
> and so only owns the symlink.

For explicitly versioned packages, say for PowerShell 6.0,
we will maintain separate packages named in the form `powershell6.0`,
which owns the binary `powershell6.0`, the symlink `powershell6.0`,
the man-page `powershell6.0`,
and is installed to `/opt/microsoft/powershell/6.0/`.
Specifically this package owns nothing named `powershell`,
as only the `powershell` package owns those files.
This package is upgradeable, but should only be updated with hot-fixes.
This is a necessary consequence of Unix package managers,
as files among packages *cannot* conflict.
From a user-experience perspective,
if the user requires a specific version of PowerShell,
they should not be required to use an absolute path,
and instead should be given a binary with the version in the name.
This pattern is followed by many other languages
(Python being the most obvious example).
This same pattern can be followed for versions 6.1, 7.0, etc.,
and can be used for patch version (e.g. 6.0.1).
Use `Start-PSPackage -Name powershell6.0` to generate
the versioned `powershell6.0` package.
Without `-Name` specified, the primary `powershell`
package will instead be created.

[fpm]: https://github.com/jordansissel/fpm
[man]: ../../assets/pwsh.1.ronn
[ronn]: https://github.com/rtomayko/ronn

### Build and Packaging Examples

On macOS or a supported Linux distro, run the following commands:

```powershell
# Install dependencies
Start-PSBootstrap -Package

# Build for v6.0.0-beta.1 release
Start-PSBuild -Clean -Crossgen -PSModuleRestore -ReleaseTag v6.0.0-beta.1

# Create package for v6.0.0-beta.1 release
Start-PSPackage -ReleaseTag v6.0.0-beta.1
```

On Windows, the `-Runtime` parameter should be specified for `Start-PSBuild` to indicate what version of OS the build is targeting.

```powershell
# Install dependencies
Start-PSBootstrap -Package

# Build for v6.0.0-beta.1 release targeting Windows universal package, set -Runtime to win7-x64
Start-PSBuild -Clean -CrossGen -PSModuleRestore -Runtime win7-x64 -Configuration Release -ReleaseTag v6.0.0-beta.1
```

```powershell
# Create packages for v6.0.0-beta.1 release targeting Windows universal package.
# 'win7-x64' / 'win7-x86' should be used for -WindowsRuntime.
Start-PSPackage -Type msi -ReleaseTag v6.0.0-beta.1 -WindowsRuntime 'win7-x64'
Start-PSPackage -Type zip -ReleaseTag v6.0.0-beta.1 -WindowsRuntime 'win7-x64'
```

## NuGet Packages

The NuGet packages for hosting PowerShell for Windows and non-Windows are being built in our release build pipeline.
The assemblies from the individual Windows and Linux builds are consumed and packed into NuGet packages.
These are then released to [powershell-core feed][ps-core-feed].

[ps-core-feed]: https://powershell.myget.org/gallery/powershell-core

## Release Tag

PowerShell releases use [Semantic Versioning][semver].
Until we hit 6.0, each sprint results in a bump to the build number,
so `v6.0.0-alpha.7` goes to `v6.0.0-alpha.8`.

When a particular commit is chosen as a release,
we create an [annotated tag][tag] that names the release.
An annotated tag has a message (like a commit),
and is *not* the same as a lightweight tag.
Create one with `git tag -a v6.0.0-alpha.7 -m <message-here>`,
and use the release change logs as the message.
Our convention is to prepend the `v` to the semantic version.
The summary (first line) of the annotated tag message should be the full release title,
e.g. 'v6.0.0-alpha.7 release of PowerShellCore'.

When the annotated tag is finalized, push it with `git push --tags`.
GitHub will see the tag and present it as an option when creating a new [release][].
Start the release, use the annotated tag's summary as the title,
and save the release as a draft while you upload the binary packages.

[semver]: https://semver.org/
[tag]: https://git-scm.com/book/en/v2/Git-Basics-Tagging
[release]: https://help.github.com/articles/creating-releases/

## Homebrew

After the release, update homebrew formula.
You need macOS to do it.

There are 2 homebrew formulas: main and preview.

### Main

Update it on stable releases.

1. Make sure that you have [homebrew cask](https://caskroom.github.io/).
1. `brew update`
1. `cd /usr/local/Homebrew/Library/Taps/caskroom/homebrew-cask/Casks`
1. Edit `./powershell.rb`, reference [file history](https://github.com/vors/homebrew-cask/commits/master/Casks/powershell.rb) for the guidelines:
    1. Update `version`
    1. Update `sha256` to the checksum of produced `.pkg` (note lower-case string for the consistent style)
    1. Update `checkpoint` value. To do that run `brew cask _appcast_checkpoint --calculate 'https://github.com/PowerShell/PowerShell/releases.atom'`
1. `brew cask style --fix ./powershell.rb`, make sure there are no errors
1. `brew cask audit --download ./powershell.rb`, make sure there are no errors
1. `brew cask upgrade powershell`, make sure that powershell was updates successfully
1. Commit your changes, send a PR to [homebrew-cask](https://github.com/caskroom/homebrew-cask)

### Preview

Update it on preview releases.

1. Add [homebrew cask versions](https://github.com/Homebrew/homebrew-cask-versions): `brew tap homebrew/cask-versions`
1. `brew update`
1. `cd /usr/local/Homebrew/Library/Taps/homebrew/homebrew-cask-versions/Casks`
1. Edit `./powershell-preview.rb`:
    1. Update `version`
    1. Update `sha256` to the checksum of produced `.pkg` (note lower-case string for the consistent style)
    1. Update `checkpoint` value. To do that run `brew cask _appcast_checkpoint --calculate 'https://github.com/PowerShell/PowerShell/releases.atom'`
1. `brew cask style --fix ./powershell-preview.rb`, make sure there are no errors
1. `brew cask audit --download ./powershell-preview.rb`, make sure there are no errors
1. `brew cask upgrade powershell-preview`, make sure that powershell was updates successfully
1. Commit your changes, send a PR to [homebrew-cask-versions](https://github.com/Homebrew/homebrew-cask-versions)
