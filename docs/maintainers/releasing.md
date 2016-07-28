Preparing
=========

PowerShell releases use [Semantic Versioning][semver]. 
Until we hit 6.0, each sprint results in a bump to the build number,
so `v6.0.0-alpha.7` goes to `v6.0.0-alpha.8`.

When a particular commit is chosen as a release,
we create an [annotated tag][tag] that names the release,
and list the major changes since the previous release. 
An annotated tag has a message (like a commit),
and is *not* the same as a lightweight tag.
Create one with `git tag -a v6.0.0-alpha.7`.
Our convention is to prepend the `v` to the semantic version. 
The summary (first line) of the annotated tag message should be the full release title, 
e.g. 'v6.0.0-alpha.7 release of PowerShell'.

When the annotated tag is finalized, push it with `git push --tags`. 
GitHub will see the tag and present it as an option when creating a new [release][]. 
Start the release, use the annotated tag's summary as the title, 
and save the release as a draft while you upload the binary packages.

Just as important as creating the release is updating the links on our readme,
and the package names in the installation instructions.
The AppVeyor build number should also be incremented.

While it is not a big concern for developer previews,
the official releases should be created on dedicated machines
such that the debug symbols do not contain personal machine paths.

[semver]: http://semver.org/
[tag]: https://git-scm.com/book/en/v2/Git-Basics-Tagging
[release]: https://help.github.com/articles/creating-releases/

Building Packages
=================

Linux / OS X
------------

The `build.psm1` module contains a `Start-PSPackage` function to build Linux packages. 
It requires that `Start-PSBuild -Publish` has been run. 
The output *must* be published so that it includes the runtime. 
This function will automatically deduce the correct version from the most recent annotated tag (using `git describe`), 
and if not specified, will build a package for the current platform.

At this time, each package must be made on the corresponding platform.
The `Start-PSBuild` function relies on the [Effing Package Management][fpm] project,
which makes building packages for any (non-Windows) platform a breeze.
Follow their readme to install FPM.

To modify any property of the packages, edit the `Start-PSPackage` function.
Please also refer to the function for details on the package properties (such as the description, 
maintainer, vendor, URL, license, category, dependencies, and file layout).

[fpm]: https://github.com/jordansissel/fpm

Windows
-------

The `Start-PSBuild` function delegates to `New-MSIPackage` which creates a Windows Installer Package of PowerShell. 
It uses the Windows Installer XML Toolset (WiX) to generate a `PowerShell_<version>.msi`, 
which installs a self-contained copy of the current version (commit) of PowerShell. 
It copies the output of the published PowerShell application to a version-specific folder in Program Files, 
and installs a shortcut in the Start Menu. 
It can be uninstalled through Programs and Features.

Note that PowerShell is always self-contained, thus using it does not require installing it. 
The output of `Start-PSBuild -Publish` includes a `powershell.exe` executable which can simply be launched.
