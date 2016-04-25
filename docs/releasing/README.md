Preparing
=========

Open PowerShell releases use [Semantic Versioning][semver]. Until we hit 1.0,
each sprint results in a bump to the minor version number, and interim bugfix
releases bump the patch number.

When a particular commit is chosen as a release, we create an
[annotated tag][tag] that names the release and list the major changes since the
previous release. An annotated tag has a message (like a commit), and is *not*
the same as a lightweight tag. Create one with `git tag -a vX.Y.Z`. Our
convention is to prepend the `v` to the semantic version. The summary (first
line) of the annotated tag message should be the full release title, e.g.
'v0.3.0 alpha release of Open PowerShell'.

When the annotated tag is finalized, push it with `git push --tags`. GitHub will
see the tag and present it as an option when creating a new [release][]. Start
the release, use the annotated tag's summary as the title, and save the release
as a draft while you upload the binary packages.

[semver]: http://semver.org/
[tag]: https://git-scm.com/book/en/v2/Git-Basics-Tagging
[release]: https://help.github.com/articles/creating-releases/

Building Packages
=================

Linux / OS X
------------

The `PowerShellGitHubDev` module contains a `Start-PSPackage` function to build
Linux packages. It requires that `Start-PSBuild -Publish` has been run. The
output *must* be published so that it includes the runtime. This function will
automatically deduce the correct version from the most recent annotated tag
(using `git describe`), and if not specified, will build a package for the
current platform.

At this time, Linux packages must be built on Linux, and OS X packages on OS X;
however, an RPM can be created on Ubuntu. This requires installing the `rpm`
package, building with `-Runtime centos.7.1-x64`, and packaging with `-Type rpm`.

The `Start-PSBuild` function relies on the [Effing Package Management][fpm]
project, which makes building packages for any (non-Windows) platform a breeze.
Follow their readme to install FPM.

To modify any property of the packages, edit the `Start-PSPackage` function.
Please also refer to the function for details on the package properties (such as
the description, maintainer, vendor, URL, license, category, dependencies, and
file layout).

[fpm]: https://github.com/jordansissel/fpm

Windows
-------

We do not yet create Windows installers. However, Open PowerShell is
self-contained. Thus a ZIP archive of the resulting `Start-PSBuild -Publish`
output will contain all the necessary dependencies and the `powershell.exe`
executable.
