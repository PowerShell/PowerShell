Contribute to PowerShell
=================================

We welcome and appreciate contributions from the community. There are many ways to become involved with PowerShell, including filing issues, joining in design conversations,
writing and improving documentation, contributing to code. Please read the rest of this document to ensure a smooth contribution process.

New to Git?
----
- Make sure you have a [GitHub account](https://github.com/signup/free)
- [Git Basics](../docs/git/basics.md): install and getting started.
- [Commit process](../docs/git/committing.md): step-by-step instructions how to commit your changes
- [Git for sd users](../docs/git/source-depot.md): a handy reference document for
people familiar with `sd`


Quick Start Check-list
----
- Read [the Community Governance](../docs/community/governance.md)
- Make sure you have signed [PowerShell Contribution License Agreement (CLA)](#contributor-license-agreement) before pull request
- Get familiar with the [PowerShell repository](../docs/git/powershell-repository-101.md)
- Setup your [development environment](../docs/dev-process/setup-dev-environment.md)
- Build the [PowerShell repository](../readme.md)
- [Try it out with the binaries you just built](../docs/dev-process/tryit.md)


Contributing to Issue
----

- Review the [GitHub Issue Management process](../docs/dev-process/issue-management-process.md). It covers the triage process and the definition of Label, Assignee and the guidance like verifying and closing issues
- Check if the issue you are going to file already exists in [GitHub Issue query](https://github.com/PowerShell/PowerShell/issues)
- Submit an issue, assuming it does not exist yet, via [GitHub Issue track](https://github.com/PowerShell/PowerShell/issues) by following the issue template.

Contributing to Documentation
----
- TODO: Don will fill in the details

Contributing to Code
----

- [Pick something to work on](https://github.com/PowerShell/PowerShell/issues)
- If you cannot find an existing issue for your desired work, open a new issue for your work
  - Get agreement from the PowerShell team and the community regarding your proposed change via the [Issue Triage Process](../docs/dev-process/issue-management-process.md).
  - If you will be adding a new cmdlet or other design changes, follow [Making Design Changes guidelines](#making-design-changes)
  - For breaking changes, see [Make Breaking Changes guidelines](#making-breaking-changes)
  - If you would like to be assigned to the issue, please ask @powershell/powershell (TODO) for an assignment
- Create a [personal fork of the repository](https://help.github.com/articles/fork-a-repo/) to start your work
- Follow the [coding guidelines](../docs/coding-guidelines/coding-guidelines.md) and [testing guidelines](../docs/testing-guidelines/testing-guidelines.md)
- Read the [Pull Request (PR) Guidelines](docs/dev-process/pull-request-rules.md) and create a [PR](https://guides.github.com/activities/hello-world/) against the upstream repository
- Perform a [code review](../docs/dev-process/code-review-guidelines.md) with the PowerShell Committee (TODO) on the pull request.


Making Breaking Changes
----

When you make code changes, please pay attention to these that can affect the [Public Contract](../docs/dev-process/breaking-change-contract.md),
for example, PowerShell parameter, API or protocols changes.  Before starting making changes to the code, first review the [Breaking Changes guidelines](../docs/dev-process/breaking-change-contract.md)
and follow the guidelines to keep PowerShell backward compatible.

Making Design Changes
----
To add new features such as cmdlets or making design changes, please follow the [PowerShell Request for Comments (RFC)](https://github.com/PowerShell/PowerShell-RFC) process.

Common Engineering Practices
----
Other than the guidelines ([coding](../docs/coding-guidelines/coding-guidelines.md), [RFC process](https://github.com/PowerShell/PowerShell-RFC) for design, [documentation]()
and [testing](../docs/testing-guidelines/testing-guidelines.md)) discussed above, following are common engineering practices we would like everyone to follow:
- Do not commit code changes to the master branch! Read GitHub's guides on [Forking Project](https://guides.github.com/activities/forking/) and [Understanding the GitHub Flow](https://guides.github.com/introduction/flow/)
- Format commit messages as follows based on [the Tim Pope's guidelines]("http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html"):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue is fixed in the specific format
below.

Fix #42
```

- Don't commit code that you didn't write. If you find code that you think is a good fit to add to PowerShell, file an issue and start a discussion before proceeding
- Create and/or update tests when making code changes
- Run tests and ensure they are passing before pull request
- All pull requests **must** pass CI systems before they can be approved
- Avoid making a big pull requests. Instead, file an issue and start a discussion with the community before you invest a large amount of time
- Blog and tweet about your contributions frequently!

File Headers
----
The following file header is the used for PowerShell. Please use it for new files. For more information, see [coding guidelines](coding-guidelines/coding-guidelines.md).
```C#
// …  TODO TODO
// Licensed to the PowerShell …. under one or more agreements.
// See the LICENSE file in the project root for more information.
```

Licensing & Copyright
----
You can find [here](../docs/community/legal-licensing.md) for the PowerShell sources license and copyright information.

Contributor License Agreement
----
You must sign a PowerShell Contribution License Agreement (CLA) before your Pull Request will be merged.
This a one-time requirement for the PowerShell project. Signing the CLA process is simple and can be done in less than a minute.
You can read more about [Contribution License Agreements (CLA)](http://en.wikipedia.org/wiki/Contributor_License_Agreement) on wikipedia.

You don't have to do this up-front. You can simply clone, fork, and submit your pull-request as
usual. When your pull-request is created, it is classified by a CLA bot. If the change is trivial
(e.g. you just fixed a typo), then the PR is labelled with cla-not-required. Otherwise it's
 classified as cla-required. Once you signed a CLA, the current and all future pull-requests will be
 labelled as cla-signed.
