Contribute to PowerShell
=================================

We welcome and appreciate contributions from the community. There are many ways to become involved with PowerShell, including filing issues, joining in design conversations,
writing and improving documentation, contributing to code. Please read the rest of this document to ensure a smooth contribution process.

New to Git?
----
- Make sure you have a [GitHub account](https://github.com/signup/free)
- [Git Basics](../docs/git/basics.md): install and getting started.
- [GitHub Flow Guide](https://guides.github.com/introduction/flow/): step-by-step instructions of GitHub flow.


Quick Start Check-list
----
- Read [the Community Governance](../docs/community/governance.md)
- Make sure you have signed a [PowerShell Contribution License Agreement (CLA)](#contributor-license-agreement) before submitting a pull request
- Get familiar with the [PowerShell repository](../docs/git/powershell-repository-101.md)

Contributing to Issues
----

- Review the [GitHub issue management process](../docs/dev-process/issue-management-process.md). It covers the triage process and the definition of labels and assignees as well as a description of how we will verify and close issues
- Check if the issue you are going to file already exists in our [GitHub issues](https://github.com/PowerShell/PowerShell/issues)
- If you can't find your issue already, [open a new issue](https://github.com/PowerShell/PowerShell/issues/new), making sure to follow the directions in the issue template as best you can.  

Contributing to Documentation
----
- First, check the list of [documentation issues](https://github.com/PowerShell/PowerShell-Docs/issues) to make sure your issue doesn't already exist or that someone is already working on it.  
- If you cannot find an existing issue for your desired work, [open a new issue](https://github.com/PowerShell/PowerShell/issues/new) explaining that you'd like to contribute to fix the problem.  
- Follow the guidelines at [Contributing to PowerShell Documentation](https://github.com/PowerShell/PowerShell-Docs/blob/staging/CONTRIBUTING.md).  
- If you contribute to the PowerShell project in a way that changes the user or developer experience, you are expected to document those changes.  

Contributing to Code
----

- Learn how to setup your development environment and build PowerShell for [Linux][build-linux], [Windows Core][build-wc], [Windows Full][build-wf] or
[OS X][build-osx]. 
- Build the [PowerShell repository](https://github.com/PowerShell/PowerShell)
- Ensure you can [locally execute tests][testing-guidelines] with your build. 
- [Pick something to work on](https://github.com/PowerShell/PowerShell/issues)
- If you cannot find an existing issue for your desired work, open a new issue for your work
  - Get agreement from the PowerShell team and the community regarding your proposed change via the [issue triage process][issue-triage].
  - If you're changes require a new cmdlet or other design changes, follow the [design change guidelines](#making-design-changes)
  - Ensure that you've reviewd our [breaking changes guidelines](#making-breaking-changes)
  - If you would like to be assigned to the issue, please ask `@powershell/powershell` for an assignment
- Create a [personal fork of the repository](https://help.github.com/articles/fork-a-repo/) to start your work
- Follow the [coding guidelines](../docs/coding-guidelines/coding-guidelines.md) and [testing guidelines](../docs/testing-guidelines/testing-guidelines.md)
- Read the [Pull Request (PR) Guidelines](../docs/dev-process/pull-request-rules.md) and create a [PR](https://guides.github.com/activities/hello-world/) against the upstream repository
- Perform a [code review](../docs/dev-process/code-review-guidelines.md) with the [PowerShell Committee][governance] on the pull request.

[build-wc]: ../docs/building/windows-core.md
[build-wf]: ../docs/building/windows-full.md
[build-osx]: ../docs/building/osx.md
[build-linux]: ../docs/building/linux.md
[testing-guidelines]: ../docs/testing-guidelines/testing-guidelines.md
[issue-triage]: ../docs/dev-process/issue-management-process.md
[governance]: ../docs/community/governance.md

Making Breaking Changes
----

When you make code changes, please pay attention to these that can affect the [Public Contract](../docs/dev-process/breaking-change-contract.md),
for example, PowerShell parameter, API or protocols changes. Before making changes to the code, first review the [breaking changes contract](../docs/dev-process/breaking-change-contract.md)
and follow the guidelines to keep PowerShell backward compatible.

Making Design Changes
----
To add new features such as cmdlets or making design changes, please follow the [PowerShell Request for Comments (RFC)](https://github.com/PowerShell/PowerShell-RFC) process.

Common Engineering Practices
----
Other than the guidelines for ([coding](../docs/coding-guidelines/coding-guidelines.md), the [RFC process](https://github.com/PowerShell/PowerShell-RFC) for design, [documentation](#contributing-to-documentation)
and [testing](../docs/testing-guidelines/testing-guidelines.md)) discussed above, we encourage contributors to follow these common engineering practices:
- Do not commit code changes to the `master` branch! 
Read GitHub's guides on [forking project](https://guides.github.com/activities/forking/) and [Understanding the GitHub Flow](https://guides.github.com/introduction/flow/)
- Format commit messages based on [Tim Pope's guidelines]("http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html"):

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
- Avoid making big pull requests. Instead, file an issue and start a discussion with the community before you invest a large amount of time
- Blog and tweet about your contributions frequently!

File Headers
----
The following file header is used for all PowerShell code. Please use it for new files. For more information, see [coding guidelines](../docs/coding-guidelines/coding-guidelines.md).
```C#
// …  TODO TODO
// Licensed to the PowerShell …. under one or more agreements.
// See the LICENSE file in the project root for more information.
```

Licensing & Copyright
----
You can find more information about the PowerShell source license and copyright [here](../docs/community/legal-licensing.md).

Contributor License Agreement (CLA)
----
You must sign a PowerShell Contribution License Agreement (CLA) before your pull request will be merged.
This a one-time requirement for the PowerShell project. Signing the CLA process is simple and can be done in less than a minute.
You don't have to do this up-front. You can simply clone, fork, and submit your pull request as usual.
When your pull request is created, it is classified by a CLA bot. 
If the change is trivial, it's classified as `cla-required`. 
Once you sign a CLA, all your existing and future pull requests will be labeled as cla-signed.
