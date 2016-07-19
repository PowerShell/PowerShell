Contribute to PowerShell
=================================

We welcome and appreciate contributions from the community. There are many ways to become involved with PowerShell, including filing issues, joining in design conversations,
writing and improving documentation, contributing to the code. Please read the rest of this document to ensure a smooth contribution process.

New to Git?
----
- Make sure you have a [GitHub account](https://github.com/signup/free)
- Learning Git:
    * GitHub Help:  [Good Resources for Learning Git and GitHub][good-git-resources]
    * [Git Basics](../docs/git/basics.md): install and getting started.
- [GitHub Flow Guide](https://guides.github.com/introduction/flow/): step-by-step instructions of GitHub flow.

Quick Start Check-list
----
- Review the [Contribution License Agreement][CLA] requriement.
- Get familiar with the [PowerShell repository](../docs/git/powershell-repository-101.md)

Contributing to Issues
----

- Review the [Issue Label Descriptions](../docs/dev-process/issue-label-descriptions.md)
- Check if the issue you are going to file already exists in our [GitHub issues][open-issues]
- If you can't find your issue already, [open a new issue](https://github.com/PowerShell/PowerShell/issues/new), making sure to follow the directions in the [issue template](./ISSUE_TEMPLATE.md) as best you can.  
- If the issue is marked as [`Help Wanted`][help-wanted-issue], the PowerShell [maintainers][maintainers] are looking for help with the issue.

Contributing to Documentation
----
### Contributing to documentation related to the PowerShell the product

Please see the [Contributor Guide in `PowerShell/PowerShell-Docs`](https://github.com/PowerShell/PowerShell-Docs/blob/staging/CONTRIBUTING.md).

### Contributing to documentation related to contributing or maintaining the PowerShell Project

- When appropriate in writting markdown docs, use [semantic linefeeds](http://rhodesmill.org/brandon/2012/one-sentence-per-line/).
  In most cases, it means "once sentence per line".
- Otherwise, these issues should be treated like any other issue in this repo.  See [Contribuing to Code](#contributing-to-code).

Contributing to Code
----

### Building and testing
#### Building PowerShell
Please see [Building PowerShell](../README.md#building-powershell)
#### Testing PowerShell
Please see PowerShell [Testing Guidelines - Running Tests Outside of CI][running-tests-outside-of-ci] on how to test you build locally.

### Finding or creating an issue

1. Follow the instructions in [Contributing to Issues][contribute-issues] to find or open an issue.
2. Mention in the issue that you are working on the issue and ask `@powershell/powershell` for an assignment.

### Forks and Pull Requests

GitHub fosters collaboration through the notion of [pull requests][using-prs].
On GitHub, anyone can [fork][fork-a-repo] an existing repository into their own branch where they can make private changes to the original repository. 
To contribute these changes back into the original repository, a user simply creates a pull request in order to "request" that the changes be taken "upstream".

Additional references:
* GitHub's guide on [forking project](https://guides.github.com/activities/forking/)
* GitHub's guide on [Contributing to Open Source](https://guides.github.com/activities/contributing-to-open-source/#pull-request)
* GitHub's guide on [Understanding the GitHub Flow](https://guides.github.com/introduction/flow/) 


### Lifecycle of a pull request

#### Pull request submission
**Always create a pull request to the `master` branch of this repository**. 
For more information, learn about our [branch structure][branch-structure].

![Github-PR-dev.png](Images/Github-PR-dev.png)

* If your contribution in a way that changes the user or developer experience, you are expected to document those changes.  See [Contributing to documentation related to the PowerShell the product](#contributing-to-documentation-related-to-the-powershell-the-product) 

* Add a meaningful title of the PR describing what change you want to check in. Don't simply put: "Fixes issue #5". A better example is: "Added Ensure parameter to New-Item CmdLet. Fixes #5". 

* When you create a pull request, fill out the pull request template including a summary of what's included in your changes. 
If the changes are related to an existing GitHub issue, please reference the issue in pull request title or description (e.g. ```Closes #11```). See [this][closing-via-message] for more details.

* Include an update to the [change log](../CHANGELOG.MD) file in your pull request to reflect changes for future versions changelog. Put them in `Unreleased` section (create one if doesn't exist). This would simplify the release process for [maintainers][maintainers]. Example:
    ```
    ## Versions
    
    ### Unreleased
    
    -  Added support for `-FriendlyName` in `Update-Item`.
    ```
    Please use past tense when describing your changes: 
    
      * Instead of "Adding support for Windows Server 2012 R2", write "Added support for Windows Server 2012 R2".
    
      * Instead of "Fix for server connection issue", write "Fixed server connection issue".
    
    Also, if change is related to specific resource, please prefix the description with the resource name:
    
      * Instead of "New parameter 'ConnectionCredential' in New-SqlConnection", write "New-SqlConnection: added parameter 'ConnectionCredential'"

#### Pull request - Automatic checks
    
* If this is your first contribution to PowerShell, you may be asked to sign a [Contribution Licensing Agreement][CLA] (CLA) before your changes will be accepted.
* Make sure you follow the [Common Engineering Practices](#common-engineering-practices) and [testing guidelines](../docs/testing-guidelines/testing-guidelines.md)
* After submitting your pull request, our [CI system (Travis-CI & Appveyor)][ci-system] will run a suite of tests and automatically update the status of the pull request.

#### Pull request - Code review

* After a successful test pass, the area [maintainers][maintainers] will do a code review, commenting on any changes that might need to be made. If you are not designated as an area's [maintainer][maintainers], feel free to review others' Pull Requests as well.  Additional feedback is always welcome (leave your comments even if everything looks good - simple "Looks good to me" or "LGTM" will suffice so that we know someone has already taken a look at it)! 
* Once the code review is done, all merge conflicts are resolved, and the CI system build status is passing, a [maintainer][maintainers] will merge your changes.

Making Breaking Changes
----

When you make code changes, please pay attention to these that can affect the [Public Contract](../docs/dev-process/breaking-change-contract.md),
for example, PowerShell parameter, API or protocols changes. Before making changes to the code, first review the [breaking changes contract](../docs/dev-process/breaking-change-contract.md)
and follow the guidelines to keep PowerShell backward compatible.

Making Design Changes
----
To add new features such as CmdLets or making design changes, please follow the [PowerShell Request for Comments (RFC)](https://github.com/PowerShell/PowerShell-RFC) process.

Common Engineering Practices
----
Other than the guidelines for ([coding](../docs/coding-guidelines/coding-guidelines.md), 
the [RFC process](https://github.com/PowerShell/PowerShell-RFC) for design, [documentation](#contributing-to-documentation)
and [testing](../docs/testing-guidelines/testing-guidelines.md)) discussed above, we encourage contributors to follow these common engineering practices:

- Format commit messages based on [Tim Pope's guidelines]("http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html"):

```
Summarize change in 50 characters or less

Provide more detail after the first line. Leave one blank line below the
summary and wrap all lines at 72 characters or less.

If the change fixes an issue, leave another blank line after the final
paragraph and indicate which issue the change fixes in the specific format below.

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

To speed up the acceptance of any contribution to any PowerShell repositories, you could [sign a Microsoft Contribution Licensing Agreement (CLA)](https://cla.microsoft.com/) ahead of time. 
If you've already contributed to PowerShell repositories in the past, congratulations! You've already completed this step.  This a one-time requirement for the PowerShell project. 
Signing the CLA process is simple and can be done in less than a minute.  You don't have to do this up-front. You can simply clone, fork, and submit your pull request as usual.
When your pull request is created, it is classified by a CLA bot. 
If the change is trivial, it's classified as `cla-required`. 
Once you sign a CLA, all your existing and future pull requests will be labeled as `cla-signed`.

[testing-guidelines]: ../docs/testing-guidelines/testing-guidelines.md
[running-tests-outside-of-ci]: ../docs/testing-guidelines/testing-guidelines.md#running-tests-outside-of-ci
[issue-triage]: ../docs/dev-process/issue-management-process.md
[governance]: ../docs/community/governance.md
[using-prs]: https://help.github.com/articles/using-pull-requests/
[fork-a-repo]: https://help.github.com/articles/fork-a-repo/
[branch-structure]: tbd
[closing-via-message]: https://help.github.com/articles/closing-issues-via-commit-messages/
[CLA]: #contributor-license-agreement-cla
[ci-system]: ../docs/testing-guidelines/testing-guidelines.md#ci-system
[good-git-resources]: https://help.github.com/articles/good-resources-for-learning-git-and-github/
[contribute-issues]: #contributing-to-issues
[open-issue]: https://github.com/PowerShell/PowerShell/issues
[maintainers]: ../docs/maintainers/maintainers.md
[help-wanted-issue]: https://github.com/PowerShell/PowerShell/issues?q=is%3Aopen+is%3Aissue+label%3A%22help+wanted%22
