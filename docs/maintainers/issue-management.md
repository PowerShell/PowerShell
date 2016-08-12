# Issue Management

## Long-living issue labels
=======
## Issue and PR Labels

Issues are opened for many different reasons.
We use the following labels for issue classifications:

* `Issue-Bug`: the issue is reporting a bug
* `Issue-Discussion`: the issue may not have a clear classification yet.
  The issue may generate a [RFC][ln-rfc] or maybe be reclassified as a bug or enhancement.
* `Issue-Enhancment`: the issue is more of a feature request than a bug.
* `Issue-Meta`: an issue used to track multiple issues
* `Issue-Question`: ideally support can be provided via other mechanisms,
  but sometimes folks to open an issue to get a question answered and we will use this label for such issues.

[ln-rfc]: https://github.com/PowerShell/PowerShell-RFC

When an issue is resolved, the following labels are used to describe the resolution:

* `Resolution-Answered`: the issue was a `Issue-Question` and was answered.
* `Resolution-By Design`: the issue is not considered a bug, the behavior is working as designed
* `Resolution-Duplicate`: the issue is a duplicate - there must be a comment linking to another issue
* `Resolution-Fixed`: the issue is has been fixed and should be referenced from a PR
* `Resolution-Won't Fix`: the issue may be considered a bug or enhancement but won't be fixed.
  If there is an inadequate explanation as to why the issue was closed,
  anyone should feel free to reopen the issue.

### Feature areas

These labels describe what feature area of PowerShell that an issue affects.

* `Area-Build`: build issues
* `Area-Cmdlets`: cmdlets in any module
* `Area-Console`: the console experience
* `Area-Debugging`: debugging PowerShell script
* `Area-Demo`: a demo or sample
* `Area-Documentation`: PowerShell *repo* documentation issues, general PowerShell doc issues go [here](https://github.com/PowerShell/PowerShell-Docs/issues)
* `Area-Engine`: core PowerShell engine, interpreter, runtime
* `Area-HelpSystem`: anything related to the help infrastructure and formatting of help
* `Area-Intellisense`: tab completion
* `Area-Language`: parser, language semantics
* `Area-OMI`: omi
* `Area-PackageManagement`: PackageManagement related issues
* `Area-Performance`: a performance issue
* `Area-Portability`: anything affecting script portability
* `Area-PowerShellGet`: PowerShellGet related issues
* `Area-PSReadline`: PSReadLine related issues
* `Area-SideBySide`: side by side support
* `Area-Test`:issues in a test or in test infrastructure

### Operating systems

These are for issues that are specific to certain operating systems:
* `OS-Linux`
* `OS-OS X`
* `OS-Windows`

### Process Tags

Issues can be in one of the following states:
* `0 - Backlog` : We've acknowledged the issue but have no immediate plans to address it.
  If you're looking for a way to contribute, these issues can be a good place to start.
* `1 - Planning` : The issue requires some design or discussion before coding can begin.
* `2 - Ready` : Any design or discussion is essentially done, coding has not yet begun though.
* `3 - Working` : The assignee(s) are actively working on the issue.
* `4 - In Review` : The issue is being reviewed.
  The assignee(s) are responsible for signing off before the PR will be merged.

The following labels are used on PRs:

* `Review - Needed` : The PR is being reviewed.  Please see [Pull Request - Code Review](../../.github/CONTRIBUTING.md#pull-request-code-review)
* `Review - Waiting on Author` : The PR was reviewed by the team and requires changes or comments from the author before being accepted.

### Random labels

* `Blocked`: an issue cannot be addressed due to external factors,
  but should not be closed because those external factors are temporary.
* `BVT/DRT`: an issue affecting or exposed by tests that have not been open sourced.
* `Porting`: an issue that affects a feature not yet ported to other platforms.
* `Usability`: this label is used to help us filter issues that might be higher priority
  because they more directly affect the usability of a particular feature or area.
* `Changelog Needed`: The PR requires an addition to the changelog,
  and should be removed when it has been added.
