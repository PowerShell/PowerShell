# Issue Management

## Security Vulnerabilities

If you believe that there is a security vulnerability in PowerShell,
first follow the [vulnerability issue reporting policy](../../.github/SECURITY.md) before submitting an issue.

## Long-living issue labels

Issue labels for PowerShell/PowerShell can be found [here](https://github.com/powershell/powershell/labels).

### Issue and PR Labels

Issues are opened for many different reasons.
We use the following labels for issue classifications:

* `Issue-Announcement`: the issue is for discussing an [Announcement](https://github.com/PowerShell/Announcements)
* `Issue-Bug`: the issue is reporting a bug
* `Issue-Code Cleanup`: the issue is for cleaning up the code with no impact on functionality
* `Issue-Discussion`: the issue may not have a clear classification yet.
  The issue may generate an [RFC][ln-rfc] or may be reclassified as a bug or enhancement.
* `Issue-Enhancement`: the issue is more of a feature request than a bug.
* `Issue-Meta`: an issue used to track multiple issues.
* `Issue-Question`: ideally support can be provided via other mechanisms,
  but sometimes folks do open an issue to get a question answered and we will use this label for such issues.

[ln-rfc]: https://github.com/PowerShell/PowerShell-RFC

When an issue is resolved, the following labels are used to describe the resolution:

* `Resolution-Answered`: the issue was an `Issue-Question` and was answered.
* `Resolution-By Design`: the issue is not considered a bug, the behavior is working as designed.
* `Resolution-Duplicate`: the issue is a duplicate - there must be a comment linking to another issue.
* `Resolution-External`: the issue cannot be address by this repo.  Should be addressed externally.
* `Resolution-Fixed`: the issue has been fixed and should be referenced from a PR.
* `Resolution-Won't Fix`: the issue may be considered a bug or enhancement but won't be fixed.
  If there is an inadequate explanation as to why the issue was closed,
  anyone should feel free to reopen the issue.

### Feature areas

These labels describe what feature area of PowerShell that an issue affects.
Those labels denoted by `WG-*` are owned by a Working Group (WG) defined
[here](../community/working-group-definitions.md):

* `Area-Maintainers-Build`: build issues
* `Area-Cmdlets-Core`: cmdlets in the Microsoft.PowerShell.Core module
* `Area-Cmdlets-Utility`: cmdlets in the Microsoft.PowerShell.Utility module
* `Area-Cmdlets-Management`: cmdlets in the Microsoft.PowerShell.Management module
* `Area-Documentation`: PowerShell *repo* documentation issues, general PowerShell doc issues go [here](https://github.com/PowerShell/PowerShell-Docs/issues)
* `Area-DSC`: DSC related issues
* `Area-PowerShellGet`: PowerShellGet related issues
* `Area-SideBySide`: side by side support
* `WG-DevEx-Portability`: anything related to authoring cross-platform or cross-architecture
  modules, cmdlets, and scripts
* `WG-DevEx-SDK`: anything related to hosting PowerShell as a runtime, PowerShell's APIs,
   PowerShell Standard, or the development of modules and cmdlets
* `WG-Engine`: core PowerShell engine, interpreter, and runtime
* `WG-Engine-Performance`: core PowerShell engine, interpreter, and runtime performance
* `WG-Engine-Providers`: built-in PowerShell providers such as FileSystem, Certificates,
   Registry, etc. (or anything returned by `Get-PSProvider`)
* `WG-Interactive-Console`: the console experience
* `WG-Interactive-Debugging`: debugging PowerShell script
* `WG-Interactive-HelpSystem`: anything related to the help infrastructure and formatting of help
* `WG-Interactive-IntelliSense`: tab completion
* `WG-Interactive-PSReadline`: PSReadline related issues
* `WG-Language`: parser, language semantics
* `WG-Quality-Test`: issues in a test or in test infrastructure
* `WG-Remoting`: PSRP issues with any transport layer
* `WG-Security`: security related areas such as [JEA](https://github.com/powershell/JEA)

### Operating Systems

These are for issues that are specific to certain Operating Systems:

* `OS-Linux`
* `OS-macOS`
* `OS-Windows`
* `OS-WSL`: Windows Subsystem for Linux

### Process Tags

The following labels are used on PRs:

* `Review - Needed` : The PR is being reviewed.  Please see [Pull Request - Code Review](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---code-review)
* `Review - Waiting on Author` : The PR was reviewed by the team and requires changes or comments from the author before being accepted.
* `Review - Abandoned` : The PR was not updated for a significant number of days (the exact number could vary over time).
  Maintainers should look into such PRs and re-evaluate them.
* `Review - Committee` : The PR/Issue needs a review from [powershell-committee](../community/governance.md#powershell-committee)

### Miscellaneous labels

* `Blocked` : An issue cannot be addressed due to external factors,
  but should not be closed because those external factors are temporary.
* `BVT/DRT` : An issue affecting or exposed by tests that have not been open sourced.
* `Changelog Needed` : The PR requires an addition to the changelog,
  and should be removed when it has been added.
* `Committee-Reviewed` : The PR/Issue has been reviewed by the [powershell-committee](../community/governance.md#powershell-committee)
* `Compliance` : Issues with the compliance label are required to be fixed either in the long term or short term for
  Microsoft to continue to sign and release packages from the project as official Microsoft packages.
  The time frame in which it needs to be fixed should be identified in the issue.
* `Documentation Needed` : The PR has changes that require a documentation change or new documentation added to [PowerShell-Docs](https://github.com/powershell/powershell-docs)
* `First-Time-Issue` : An issue that is identified as being easy and a good candidate for first time contributors
* `Hackathon` or `Hacktoberfest` : An issue that would be a good candidate for hackathons such as `Hacktoberfest` or `Hackillinois`
* `Porting` : An issue that affects a feature not yet ported to other platforms.
* `Up-for-Grabs` : We've acknowledged the issue but have no immediate plans to address it.
  If you're looking for a way to contribute, these issues can be a good place to start.
* `Usability` : This label is used to help us filter issues that might be higher priority
  because they more directly affect the usability of a particular feature or area.
* `Waiting - DotNetCore` : An issue waiting on a fix/change in DotNetCore.
