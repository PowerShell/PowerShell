# PowerShell Governance

## Terms

* [**PowerShell Committee**](#powershell-committee): A committee of project owners who are responsible for design decisions, approving [RFCs][RFC-repo], and approving new maintainers/committee members 
* [**Repository maintainer**](#repository-maintainers): An individual responsible for merging pull requests (PRs) into `master` when all requirements are met (code review, tests, docs, and RFC approval as applicable).
Repository Maintainers are the only people with write permissions into `master`.
* [**Area experts**](#area-experts): People who are experts for specific components (e.g. PSReadline, the parser) or technologies (e.g. security, performance). 
Area experts are responsible for code reviews, issue triage, and providing their expertise to others. 
* **Corporate Maintainer**: The Corporate Maintainer is an entity, person or set of persons, with the ability to veto decisions made by the PowerShell Committee or any other collaborators on the PowerShell project.
This veto power will be used with restraint since it is intended that the community drive the project. 
Under extreme circumstances the Corporate Maintainer also reserves the right to dissolve or reform the PowerShell Committee. 
The Corporate Maintainer for PowerShell is Microsoft.
* [**RFC process**][RFC-repo]: The "review-for-comment" (RFC) process whereby design decisions get made. 

## PowerShell Committee

The PowerShell Committee and its members (aka Committee Members) are the primary caretakers of the PowerShell experience, including the PowerShell language, design, and project.

### Current Committee Members

* Bruce Payette ([BrucePay](https://github.com/BrucePay))
* Jason Shirk ([lzybkr](https://github.com/lzybkr))
* Steve Lee ([SteveL-MSFT](https://github.com/SteveL-MSFT))
* Hemant Mahawar ([HemantMahawar](https://github.com/HemantMahawar))
* Joey Aiello ([joeyaiello](https://github.com/joeyaiello))

### Committee Member Responsibilities

Committee Members are responsible for reviewing and approving [PowerShell RFCs][RFC-repo] proposing new features or design changes. 

#### Changes that require an [RFC][RFC-repo]

The following types of decisions require a written RFC and ample time for the community to respond with their feedback before a contributor begins work on the issue:

* new features or capabilities in PowerShell (e.g. PowerShell classes, PSRP over SSH, etc.)
* anything that might require a breaking changes as defined in our [Breaking Changes Contract][breaking-changes]
* new modules, cmdlets, or parameters that ship in the core PowerShell modules (e.g. `Microsoft.PowerShell.*`, `PackageManagement`, `PSReadline`)
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

Repository Maintainers are trusted stewards of the PowerShell repository responsible for maintaining consistency and quality of PowerShell code. 
One of their primary responsibilities is merging pull requests after all requirements have been fulfilled. 

Repository Maintainers have [write access](https://help.github.com/articles/repository-permission-levels-for-an-organization/) to the PowerShell repository which gives them the power to:

1. Merge pull requests to all branches *including* `master`.
1. `git push` to all branches *including* `master`.
1. Correctly assigning labels, milestones, and contributors to [issues](https://guides.github.com/features/issues/)

### Repository Maintainer Responsibilities

Repository Maintainers enable rapid contributions while maintaining a high level of quality in PowerShell by ensuring that all development processes are being followed correctly. 

If you are a Repository Maintainer:

1. **DO** add [the correct labels](../dev-process/issue-label-descriptions.md) to issues and pull requests 
1. **DO** make sure that [any change requiring approval from the PowerShell Committee](#changes-that-require-an-rfc) has gone through the proper [RFC][RFC-repo] or approval process
1. **DO** make sure the correct [Area Experts](#area-experts) are assigned to relevant pull requests and issues.
This includes adding extra reviewers when it makes sense
(e.g. a pull request that adds remoting capabilities might require a security expert)
1. **DO** validate that code reviews have been performed before merging a pull request
1. **DO** validate that applicable tests and documentation have been written before merging a pull request
1. **DO** make sure contributors are following the [contributor guidelines](../../.github/CONTRIBUTING.md)
1. **DO** ask people to resend a pull request, if it [doesn't target `master`](../../.github/CONTRIBUTING.md#lifecycle-of-a-pull-request).
1. **DO** wait for the [CI system][ci-system] build to pass for pull requests.
1. **DO** encourage contributors to refer to issues in their pull request description per the [issue template](../../.github/ISSUE_TEMPLATE) (e.g. `Resolves issue #123`) 
1. **DO** encourage contributors to create meaningful titles for all PRs.
Edit the title if necessary to provide clarity on the problem
1. **DO** verify that all contributors are following the [Coding Guidlines](../dev-process/coding-guidelines.md)
1. **DO** ensure that each contributor has signed a valid Contributor License Agreement (CLA)
1. **DO** verify compliance with any third party code license terms (e.g., requiring attribution, etc.) if the contribution contains third party code

1. **DON'T** merge pull requests with a failed CI build into `master`
1. **DON'T** merge pull requests without the label `cla-signed` or `cla-not-required` from the Microsoft CLA bot
1. **DON'T** merge pull requests that do not [include all meaningful changes](../../.github/CONTRIBUTING.md#lifecycle-of-a-pull-request) under the **Unreleased** section in the repository's `CHANGELOG.md`
1. **DON'T** merge your own pull requests.
If a Repository Maintainer opens a pull request, another Maintainer must merge it 

### Becoming a Repository Maintainer

(TODO: paste from CM) Repository Maintainers currently consist entirely of Microsoft employees, it's expected that trusted, regular contributors to the PowerShell repository will become maintainers themselves.
Eligibility is heavily dependent on the level of contribution and expertise: individuals who contribute in meaningful ways to the project will be recognized accordingly. 

At any point in time, a Repository Maintainers can nominate a strong community member to become a Repository Maintainer. 
Nominations should be submitted in the form of [RFCs][RFC-repo] detailing why that individual is qualified and how they will contribute.
After the RFC has been discussed, a unanimous vote by the PowerShell Committee will be required for the new Repository Maintainer to be confirmed. 

## Area Experts

Area Experts are people with knowledge of specific components or technologies in the PowerShell domain. They are responsible for code reviews, issue triage, and providing their expertise to others. 

They have [write access](https://help.github.com/articles/permission-levels-for-an-organization-repository/) to the PowerShell repository which gives them the power to:

1. `git push` to all branches *except* `master`.
1. Merge pull requests to all branches *except* `master` (though this should not be common given that [`master`is the only long-living branch](../git/powershell-repository-101.md#understand-branches)).
1. Assign labels, milestones, and people to [issues](https://guides.github.com/features/issues/).

### Area Expert Responsibilities

If you are an Area Expert, you are expected to be actively involved in any development, design, or contributions in your area of expertise. 

If you are an Area Expert:

1. **DO** assign the [correct labels][issue-process]
1. **DO** assign yourself to issues labeled with your area of expertise
1. **DO** [code reviews][TODO] for issues where you're assigned or in your areas of expertise
1. **DO** reply to new issues and pull requests that are related to your area of expertise 
(while reviewing PRs, leave your comment even if everything looks good - a simple "Looks good to me" or "LGTM" will suffice, so that we know someone has already taken a look at it).
1. **DO** make sure contributors are following the [contributor guidelines](../../.github/CONTRIBUTING.md).
1. **DO** ask people to resend a pull request, if it [doesn't target `master`](../../.github/CONTRIBUTING.md#lifecycle-of-a-pull-request).
1. **DO** ensure that contributors [write Pester tests][pester] for all new/changed functionality
1. **DO** ensure that contributors [write documentation][docs-contributing] for all new-/changed functionality
1. **DO** encourage contributors to refer to issues in their pull request description per the [issue template](../../.github/ISSUE_TEMPLATE) (e.g. `Resolves issue #123`)
1. **DO** encourage contributors to create meaningful titles for all PRs. Edit title if necessary.
1. **DO** verify that all contributors are following the [Coding Guidelines](../dev-process/coding-guidelines.md).

1. **DON'T** create new features, new designs, or change behaviors without following the [RFC][RFC-repo] or approval process

## Issue Management Process

See our [Issue Management Process][issue-process]

## Pull Request Process 

See our [Pull Request Process][pull-request-process]

[RFC-repo]: https://github.com/PowerShell/PowerShell-RFC
[pester]: ../testing-guidelines/WritingPesterTests.md 
[ci-system]: ../testing-guidelines/testing-guidelines.md#ci-system
[breaking-changes]: ../dev-process/breaking-change-contract.md
[issue-process]: ../dev-process/issue-label-descriptions.md
[pull-request-process]: ../dev-process/pull-request-process.md
[docs-contributing]: https://github.com/PowerShell/PowerShell-Docs/blob/staging/CONTRIBUTING.md