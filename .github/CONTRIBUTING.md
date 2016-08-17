Contributing to PowerShell
========================

We welcome and appreciate contributions from the community.
There are many ways to become involved with PowerShell:
including filing issues,
joining in design conversations,
writing and improving documentation,
and contributing to the code.
Please read the rest of this document to ensure a smooth contribution process.

New to Git?
-----------

* Make sure you have a [GitHub account](https://github.com/signup/free).
* Learning Git:
    * GitHub Help: [Good Resources for Learning Git and GitHub][good-git-resources].
    * [Git Basics](../docs/git/basics.md):
      install and getting started.
* [GitHub Flow Guide](https://guides.github.com/introduction/flow/):
  step-by-step instructions of GitHub flow.

Quick Start Checklist
---------------------

* Review the [Contribution License Agreement][CLA] requirement.
* Get familiar with the [PowerShell repository](../docs/git).

Contributing to Issues
----------------------

* Review [Issue Management][issue-management].
* Check if the issue you are going to file already exists in our [GitHub issues][open-issue].
* If you can't find your issue already,
  [open a new issue](https://github.com/PowerShell/PowerShell/issues/new),
  making sure to follow the directions as best you can.
* If the issue is marked as [`0 - Backlog`][help-wanted-issue],
  the PowerShell maintainers are looking for help with the issue.

Contributing to Documentation
-----------------------------

### Contributing to documentation related to PowerShell

Please see the [Contributor Guide in `PowerShell/PowerShell-Docs`](https://github.com/PowerShell/PowerShell-Docs/blob/staging/CONTRIBUTING.md).

### Contributing to documentation related to maintaining or contributing to the PowerShell project

* When writing Markdown documentation, use [semantic linefeeds][].
  In most cases, it means "once clause / idea per line".
* Otherwise, these issues should be treated like any other issue in this repo.

Contributing to Code
--------------------

### Building and testing
#### Building PowerShell
Please see [Building PowerShell](../README.md#building-the-repository).
#### Testing PowerShell
Please see PowerShell [Testing Guidelines - Running Tests Outside of CI][running-tests-outside-of-ci] on how to test you build locally.

### Finding or creating an issue

1. Follow the instructions in [Contributing to Issues][contribute-issues] to find or open an issue.
2. Mention in the issue that you are working on the issue and ask `@powershell/powershell` for an assignment.

### Forks and Pull Requests

GitHub fosters collaboration through the notion of [pull requests][using-prs].
On GitHub, anyone can [fork][fork-a-repo] an existing repository
into their own user account, where they can make private changes to their fork.
To contribute these changes back into the original repository,
a user simply creates a pull request in order to "request" that the changes be taken "upstream".

Additional references:
* GitHub's guide on [forking](https://guides.github.com/activities/forking/)
* GitHub's guide on [Contributing to Open Source](https://guides.github.com/activities/contributing-to-open-source/#pull-request)
* GitHub's guide on [Understanding the GitHub Flow](https://guides.github.com/introduction/flow/)


### Lifecycle of a pull request

#### Before submitting

* To avoid merge conflicts, make sure your branch is rebased on the `master` branch of this repository.
* Many code changes will require new tests,
  so make sure you've added a new test if existing tests do not effectively test the code changed.
* Clean up your commit history.
  Each commit should be a **single complete** change.
  This discipline is important when reviewing the changes as well as when using `git bisect` and `git revert`.


#### Pull request submission

**Always create a pull request to the `master` branch of this repository**.

![Github-PR-dev.png](Images/Github-PR-dev.png)

* If you're contributing in a way that changes the user or developer experience, you are expected to document those changes.
See [Contributing to documentation related to PowerShell](#contributing-to-documentation-related-to-powershell).

* Add a meaningful title of the PR describing what change you want to check in.
  Don't simply put: "Fixes issue #5".
  A better example is: "Add Ensure parameter to New-Item cmdlet", with "Fixes #5" in the PR's body.

* When you create a pull request,
  including a summary of what's included in your changes and
  if the changes are related to an existing GitHub issue,
  please reference the issue in pull request description (e.g. ```Closes #11```).
  See [this][closing-via-message] for more details.

* If the change warrants a note in the [changelog](../CHANGELOG.MD)
  either update the changelog in your pull request or
  add a comment in the PR description saying that the change may warrant a note in the changelog.
  New changes always go into the **Unreleased** section.
  Keeping the changelog up-to-date simplifies the release process for maintainers.
  An example:
    ```
    Unreleased
    ----------

    * `Update-Item` now supports `-FriendlyName`.
    ```
    Please use the present tense and imperative mood when describing your changes:

      * Instead of "Adding support for Windows Server 2012 R2", write "Add support for Windows Server 2012 R2".

      * Instead of "Fixed for server connection issue", write "Fix server connection issue".

    This form is akin to giving commands to the code base,
    and is recommended by the Git SCM developers.
    It is also used in the [Git commit messages](#common-engineering-practices).

    Also, if change is related to a specific resource, please prefix the description with the resource name:

      * Instead of "New,parameter 'ConnectionCredential' in New-SqlConnection",
        write "New-SqlConnection: added parameter 'ConnectionCredential'".

#### Pull Request - Automatic Checks

* If this is your first contribution to PowerShell,
  you may be asked to sign a [Contribution Licensing Agreement][CLA] (CLA)
  before your changes will be accepted.

* Make sure you follow the [Common Engineering Practices](#common-engineering-practices)
  and [testing guidelines](../docs/testing-guidelines/testing-guidelines.md).

* After submitting your pull request,
  our [CI system (Travis CI and AppVeyor)][ci-system]
  will run a suite of tests and automatically update the status of the pull request.

#### Pull Request - Code Review

* After a successful test pass,
  the area maintainers will do a code review,
  commenting on any changes that might need to be made.

* Additional feedback is always welcome!
  Even if you are not designated as an area's maintainer,
  feel free to review others' pull requests anyway.
  Leave your comments even if everything looks good;
  a simple "Looks good to me" or "LGTM" will suffice.
  This way we know someone has already taken a look at it!

* Once the code review is done,
  all merge conflicts are resolved,
  and the CI system build status is passing,
  a maintainer will merge your changes.

* For more information on the the PowerShell maintainers' process,
  see the [documentation](../docs/maintainers).

Making Breaking Changes
-----------------------

When you make code changes,
please pay attention to these that can affect the [Public Contract](../docs/dev-process/breaking-change-contract.md).
For example, changing PowerShell parameters, APIs, or protocols break the public contract.
Before making changes to the code,
first review the [breaking changes contract](../docs/dev-process/breaking-change-contract.md)
and follow the guidelines to keep PowerShell backward compatible.

Making Design Changes
---------------------

To add new features such as cmdlets or making design changes,
please follow the [PowerShell Request for Comments (RFC)](https://github.com/PowerShell/PowerShell-RFC) process.

Common Engineering Practices
----------------------------

Other than the guidelines for ([coding](../docs/dev-process/coding-guidelines.md),
the [RFC process](https://github.com/PowerShell/PowerShell-RFC) for design,
[documentation](#contributing-to-documentation) and [testing](../docs/testing-guidelines/testing-guidelines.md)) discussed above,
we encourage contributors to follow these common engineering practices:

* Format commit messages following these guidelines:

```
Summarize change in 50 characters or less

Similar to email, this is the body of the commit message,
and the above is the subject.
Always leave a single blank line between the subject and the body
so that `git log` and `git rebase` work nicely.

The subject of the commit should use the present tense and
imperative mood, like issuing a command:

> Makes abcd do wxyz

The body should be a useful message explaining
why the changes were made.

If significant alternative solutions were available,
explain why they were discarded.

Keep in mind that the person most likely to refer to your commit message
is you in the future, so be detailed!

As Git commit messages are most frequently viewed in the terminal,
you should wrap all lines around 72 characters.

Using semantic line feeds (breaks that separate ideas)
is also appropriate, as is using Markdown syntax.
```

* These are based on Tim Pope's [guidelines](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html),
  Git SCM [submitting patches](https://git.kernel.org/cgit/git/git.git/tree/Documentation/SubmittingPatches),
  Brandon Rhodes' [semantic linefeeds][],
  and John Gruber's [Markdown syntax](https://daringfireball.net/projects/markdown/syntax).

* Don't commit code that you didn't write.
  If you find code that you think is a good fit to add to PowerShell,
  file an issue and start a discussion before proceeding.

* Create and/or update tests when making code changes.

* Run tests and ensure they are passing before pull request.

* All pull requests **must** pass CI systems before they can be approved.

* Avoid making big pull requests.
  Before you invest a large amount of time,
  file an issue and start a discussion with the community.

Contributor License Agreement (CLA)
-----------------------------------

To speed up the acceptance of any contribution to any PowerShell repositories,
you could [sign a Microsoft Contribution Licensing Agreement (CLA)](https://cla.microsoft.com/) ahead of time.
If you've already contributed to PowerShell repositories in the past, congratulations!
You've already completed this step.
This a one-time requirement for the PowerShell project.
Signing the CLA process is simple and can be done in less than a minute.
You don't have to do this up-front.
You can simply clone, fork, and submit your pull request as usual.
When your pull request is created, it is classified by a CLA bot.
If the change is trivial, it's classified as `cla-required`.
Once you sign a CLA, all your existing and future pull requests will be labeled as `cla-signed`.

[testing-guidelines]: ../docs/testing-guidelines/testing-guidelines.md
[running-tests-outside-of-ci]: ../docs/testing-guidelines/testing-guidelines.md#running-tests-outside-of-ci
[issue-management]: ../docs/maintainers/issue-management.md
[governance]: ../docs/community/governance.md
[using-prs]: https://help.github.com/articles/using-pull-requests/
[fork-a-repo]: https://help.github.com/articles/fork-a-repo/
[closing-via-message]: https://help.github.com/articles/closing-issues-via-commit-messages/
[CLA]: #contributor-license-agreement-cla
[ci-system]: ../docs/testing-guidelines/testing-guidelines.md#ci-system
[good-git-resources]: https://help.github.com/articles/good-resources-for-learning-git-and-github/
[contribute-issues]: #contributing-to-issues
[open-issue]: https://github.com/PowerShell/PowerShell/issues
[help-wanted-issue]: https://github.com/PowerShell/PowerShell/issues?q=is%3Aopen+is%3Aissue+label%3A%220%20-%20Backlog%22
[semantic linefeeds]: http://rhodesmill.org/brandon/2012/one-sentence-per-line/
