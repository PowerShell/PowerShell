# Repository Maintainers

Repository Maintainers are trusted stewards of the PowerShell repository responsible for maintaining consistency and quality of PowerShell code.
One of their primary responsibilities is merging pull requests after all requirements have been fulfilled.

They have [write access](https://help.github.com/articles/permission-levels-for-an-organization-repository/) to the PowerShell repositories which gives them the power to:

1. `git push` to the official PowerShell repository
2. Merge pull requests
3. Assign labels, milestones, and people to [issues](https://guides.github.com/features/issues/)

## Table of Contents
- [Current Repository Maintainers](#current-repository-maintainers)
- [Repository Maintainer Responsibilities](#repository-maintainer-responsibilities)
- [Issue Management Process](#issue-management-process)
- [Pull Request Workflow](#pull-management-process)
  - [Abandoned Pull Requests](#abandoned-pull-requests)
- [Becoming a Repository Maintainer](#becoming-a-repository-maintainer)

## Current Repository Maintainers

* Sergei Vorobev ([vors](https://github.com/vors))
* Jason Shirk ([lzybkr](https://github.com/lzybkr))
* Dongbo Wang ([daxian-dbw](https://github.com/daxian-dbw))
* Travis Plunk ([TravisEz123](https://github.com/TravisEz123))
* Mike Richmond ([mirichmo](https://github.com/mirichmo))
* Andy Schwartzmeyer ([andschwa](https://github.com/andschwa))

## Repository Maintainer Responsibilities

Repository Maintainers enable rapid contributions while maintaining a high level of quality in PowerShell by ensuring that all development processes are being followed correctly. 

If you are a Repository Maintainer, you:

1. **MUST** ensure that each contributor has signed a valid Contributor License Agreement (CLA) 
1. **MUST** verify compliance with any third party code license terms (e.g., requiring attribution, etc.) if the contribution contains third party code.
1. **MUST** make sure that [any change requiring approval from the PowerShell Committee](#changes-that-require-an-rfc) has gone through the proper [RFC][RFC-repo] or approval process
1. **MUST** validate that code reviews have been conducted before merging a pull request when no code is written
1. **MUST** validate that tests and documentation have been written before merging a pull request that contains new functionality
1. **SHOULD** add [the correct labels][issue-management] to issues and pull requests
1. **SHOULD** make sure the correct [Area Experts](#area-experts) are assigned to relevant pull requests and issues.
This includes adding extra reviewers when it makes sense
(e.g. a pull request that adds remoting capabilities might require a security expert)
1. **SHOULD** validate that the names and email addresses in the git commits reasonably match identity of the person submitting the pull request 
1. **SHOULD** make sure contributors are following the [contributor guidelines][CONTRIBUTING]
1. **SHOULD** ask people to resend a pull request, if it [doesn't target `master`](../../.github/CONTRIBUTING.md#lifecycle-of-a-pull-request)
1. **SHOULD** wait for the [CI system][ci-system] build to pass for pull requests 
(unless, for instance, the pull request is being submitted to fix broken CI)
1. **SHOULD** encourage contributors to refer to issues in their pull request description (e.g. `Resolves issue #123`).
If a user did not create an issue prior to submitting their pull request, their pull request should not be rejected.
However, they should be reminded to create an issue in the future to frontload any potential problems with the work and to minimize duplication of efforts. 
1. **SHOULD** encourage contributors to create meaningful titles for all PRs.
Edit the title if necessary to provide clarity on the problem
1. **SHOULD** encourage contributes to write meaningful, descriptive git commits
1. **SHOULD NOT** merge pull requests with a failed CI build
(unless, for instance, the pull request is being submitted to fix broken CI)
1. **SHOULD NOT** merge pull requests without the label `cla-signed` or `cla-not-required` from the Microsoft CLA bot
(unless the CLA bot is broken, and CLA signing can be confirmed through other means)
1. **SHOULD NOT** merge pull requests too quickly after they're submitted.
Even if the pull request meets all the requirements, people should have time to give their input 
(unless the pull request is particularly urgent for some reason)
1. **SHOULD NOT** merge your own pull requests.
If a Repository Maintainer opens a pull request, another Maintainer should merge it unless there are extreme, short-term circumstances requiring a merge or another Maintainer has given explicit sign-off without merging

## Issue Management Process

Please see [Issue Management][issue-management]

## Pull Request Workflow

1. A contributor opens a pull request.
1. The contributor ensures that their pull request passes the [CI system][ci-system] build.
  - If the build fails, a maintainer adds the ```waiting for author``` label to the pull request. 
  The contributor can then continue to update the pull request until the build passes.
1. Once the build passes, the maintainer either reviews the pull request immediately or adds the ```need review``` label.
1. A maintainer or trusted contributor reviews the pull request code.
  - If the contributor does not meet the reviewer's standards, the reviewer makes comments. 
  A maintainer then removes the ```need review``` label and adds the ```waiting for author``` label. 
  The contributor must address the comments and repeat from step 2.
  - If the contributor meets the reviewer's standards, the reviewer comments that they are satisfied. 
  A maintainer then removes the ```need review``` label.
1. Once the code review is completed, a maintainer merges the pull request.

### Abandoned Pull Requests

A pull request with the label ```waiting for the author``` for **more than two weeks** without a word from the author is considered abandoned.

In these cases:

1. Ping the author of PR to remind him of pending changes.
  - If the contributor responds, it's no longer an abandoned pull request, proceed as normal.
2. If the contributor does not respond **within a week**:
  - Create a new branch with the changes and open an issue to merge the code into the dev branch. 
  Mention the original pull request ID in the description of the new issue and close the abandoned pull request. 
  - If the changes in an abandoned pull request are no longer needed (e.g. due to refactoring of the code base or a design change), simply close the pull request.

## Becoming a Repository Maintainer

Repository Maintainers currently consist entirely of Microsoft employees
It is expected that over time, regular trusted contributors to the PowerShell repository will be made Repository Maintainers.
Eligibility is heavily dependent on the level of contribution and expertise: individuals who contribute in meaningful ways to the project will be recognized accordingly. 

At any point in time, a Repository Maintainers can nominate a strong community member to become a Repository Maintainer. 
Nominations should be submitted in the form of [RFCs][RFC-repo] detailing why that individual is qualified and how they will contribute.
After the RFC has been discussed, a unanimous vote by the PowerShell Committee will be required for the new Repository Maintainer to be confirmed. 

[RFC-repo]: https://github.com/PowerShell/PowerShell-RFC
[ci-system]: ../testing-guidelines/testing-guidelines.md#ci-system
[issue-management]: issue-management.md
[CONTRIBUTING]: ../../.github/CONTRIBUTING.md