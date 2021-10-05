# PowerShell Governance

## Terms

* [**PowerShell Committee**](#powershell-committee): A committee of project owners who are responsible for design decisions,
  approving [RFCs][RFC-repo], and approving new maintainers/committee members
* [**Repository maintainer**](#repository-maintainers): An individual responsible for merging pull requests (PRs) into `master` when all requirements are met (code review, tests, docs, and RFC approval as applicable).
  Repository Maintainers are the only people with write permissions for the `master` branch.
* [**Working Groups (WGs)**](#working-groups-(wgs)) are collections of contributors responsible for
  providing expertise on a specific area of PowerShell in order to help establish consensus within
  the community and Committee.
* **Corporation**: The Corporation owns the PowerShell repository and, under extreme circumstances,
  reserves the right to dissolve or reform the PowerShell Committee, the Project Leads, and the Corporate Maintainer.
  The Corporation for PowerShell is Microsoft.
* **Corporate Maintainer**: The Corporate Maintainer is an entity, person or set of persons,
  with the ability to veto decisions made by the PowerShell Committee or any other collaborators on the PowerShell project.
  This veto power will be used with restraint since it is intended that the community drive the project.
  The Corporate Maintainer is determined by the Corporation both initially and in continuation.
  The initial Corporate Maintainer for PowerShell is Jeffrey Snover ([jpsnover](https://github.com/jpsnover)).
* [**RFC process**][RFC-repo]: The "request-for-comments" (RFC) process whereby design decisions get made.

## PowerShell Committee

The PowerShell Committee and its members (aka Committee Members) are the primary caretakers of the PowerShell experience, including the PowerShell language, design, and project.

### Current Committee Members

* Bruce Payette ([BrucePay](https://github.com/BrucePay))
* Jim Truher ([JamesWTruher](https://github.com/JamesWTruher))
* Paul Higinbotham ([paulhigin](https://github.com/paulhigin))
* Rob Holt ([rjmholt](https://github.com/rjmholt))
* Steve Lee ([SteveL-MSFT](https://github.com/SteveL-MSFT))

### Committee Member Responsibilities

Committee Members are responsible for reviewing and approving [PowerShell RFCs][RFC-repo] proposing new features or design changes.

#### Changes that require an [RFC][RFC-repo]

The following types of decisions require a written RFC and ample time for the community to respond with their feedback before a contributor begins work on the issue:

* new features or capabilities in PowerShell (e.g. PowerShell classes, PSRP over SSH, etc.)
* anything that might require a breaking change, as defined in our [Breaking Changes Contract][breaking-changes]
* new modules, cmdlets, or parameters that ship in the core PowerShell modules (e.g. `Microsoft.PowerShell.*`, `PackageManagement`, `PSReadLine`)
* the addition of new PowerShell Committee Members or Repository Maintainers
* any changes to the process of maintaining the PowerShell repository (including the responsibilities of Committee Members, Repository Maintainers, and Area Experts)

#### Changes that don't require an RFC

In some cases, a new feature or behavior may be deemed small enough to forgo the RFC process
(e.g. changing the default PSReadline `EditMode` to `Emacs` on Mac/Linux).
In these cases, [issues marked as `1 - Planning`][issue-process] require only a simple majority of Committee Members to sign off.
After that, a Repository Maintainer should relabel the issue as `2 - Ready` so that a contributor can begin working on it.

If any Committee Members feels like this behavior is large enough to warrant an RFC, they can add the label `RFC-required` and the issue owner is expected to follow the RFC process.

#### Committee Member DOs and DON'Ts

As a PowerShell Committee Member:

1. **DO** reply to issues and pull requests with design opinions
  (this could include offering support for good work or exciting new features)
1. **DO** encourage healthy discussion about the direction of PowerShell
1. **DO** raise "red flags" on PRs that haven't followed the proper RFC process when applicable
1. **DO** contribute to documentation and best practices
1. **DO** maintain a presence in the PowerShell community outside of GitHub (Twitter, blogs, StackOverflow, Reddit, Hacker News, etc.)
1. **DO** heavily incorporate community feedback into the weight of your decisions
1. **DO** be polite and respectful to a wide variety of opinions and perspectives
1. **DO** make sure contributors are following the [contributor guidelines](../../.github/CONTRIBUTING.md)

1. **DON'T** constantly raise "red flags" for unimportant or minor problems to the point that the progress of the project is being slowed
1. **DON'T** offer up your opinions as the absolute opinion of the PowerShell Committee.
  Members are encouraged to share their opinions, but they should be presented as such.

### PowerShell Committee Membership

The initial PowerShell Committee consists of Microsoft employees.
It is expected that over time, PowerShell experts in the community will be made Committee Members.
Membership is heavily dependent on the level of contribution and expertise: individuals who contribute in meaningful ways to the project will be recognized accordingly.

At any point in time, a Committee Member can nominate a strong community member to join the Committee.
Nominations should be submitted in the form of [RFCs][RFC-repo] detailing why that individual is qualified and how they will contribute.
After the RFC has been discussed, a unanimous vote will be required for the new Committee Member to be confirmed.

## Repository Maintainers

Repository Maintainers are trusted stewards of the PowerShell community/repository responsible for maintaining consistency and quality of PowerShell code.
One of their primary responsibilities is merging pull requests after all requirements have been fulfilled.

For more information on Repository Maintainers--their responsibilities, who they are, and how one becomes a Maintainer--see the [README for Repository Maintainers][maintainers].

## Working Groups (WGs)

[Working Groups (WGs)][wg] are collections of contributors with knowledge of specific components or
technologies in the PowerShell domain.
They are responsible for issue triage/acceptance, code reviews, and providing their expertise to
others in issues, PRs, and RFC discussions,
as well as to the Committee when said expertise is helpful in broader discussions.

They have [write access](https://docs.github.com/en/free-pro-team@latest/github/setting-up-and-managing-organizations-and-teams/repository-permission-levels-for-an-organization) to the PowerShell repository which gives them the power to:

1. `git push` to all branches *except* `master`.
1. Merge pull requests to all branches *except* `master` (though this should not be common given that [`master`is the only long-living branch](../git/README.md#understand-branches)).
1. Assign labels, milestones, and people to [issues](https://guides.github.com/features/issues/).

### Working Group Responsibilities

If you are a member of a Working Group, you are expected to be actively involved in any development, design, or contributions in the focus area of the WG.
More information on the responsibilities of Working Groups can be found [here][wg],
while current WG definitions and membership can be found [here][wg-definitions].

If you are a Working Group member:

1. **DO** triage and contribute to discussions in issues and PRs that have `WG-*` labels assigned
1. **DO** regularly communicate with other members of your WG to coordinate decisions about
   whether issues should move forward
1. **DO** make decisions on whether or not issues should proceed forward with implementations or RFCs
1. **DO** assign the [correct labels][issue-process] to issues
1. **DO** assign yourself to issues and PRs labeled with your area of expertise only when you are actively
   working on or implementing them
1. **DO** code reviews for PRs where you're assigned or in your WG
   (while reviewing PRs, leave your comment even if everything looks good - a simple "Looks good to me" or "LGTM" will suffice, so that we know someone has already taken a look at it).
1. **DO** ensure that contributors are following the [contributor guidelines](../../.github/CONTRIBUTING.md)
   and [Code of Conduct](https://github.com/PowerShell/PowerShell/blob/master/CODE_OF_CONDUCT.md)
1. **DO** ensure that contributions [include Pester tests][pester] for all new/changed functionality
1. **DO** ensure that contributions [include documentation][docs-contributing] for all new-/changed functionality
1. **DO** encourage contributions to refer to issues in their pull request description (e.g. `Resolves issue #123`)
1. **DO** encourage contributions to have meaningful titles for all PRs,
   editing their title if necessary to ensure that changelogs convey useful and accurate information
1. **DO** verify that all contributions are following the [Coding Guidelines](../dev-process/coding-guidelines.md)

1. **DON'T** create new features, new designs, or change behaviors without following the [RFC][RFC-repo] or approval process

## Issue Management Process

See our [Issue Management Process][issue-process]

## Pull Request Process

See our [Pull Request Process][pull-request-process]

[RFC-repo]: https://github.com/PowerShell/PowerShell-RFC
[pester]: ../testing-guidelines/WritingPesterTests.md
[breaking-changes]: ../dev-process/breaking-change-contract.md
[issue-process]: ../maintainers/issue-management.md
[pull-request-process]: ../../.github/CONTRIBUTING.md#lifecycle-of-a-pull-request
[docs-contributing]: https://github.com/PowerShell/PowerShell-Docs/blob/staging/CONTRIBUTING.md
[maintainers]: ../maintainers/README.md
[wg]: ./working-group.md
[wg-defintions]: ./working-group-definitions.md
