# Repository Maintainers

Repository Maintainers are trusted stewards of the PowerShell repository responsible for maintaining consistency and quality of PowerShell code.
One of their primary responsibilities is merging pull requests after all requirements have been fulfilled.

They have [write access](https://docs.github.com/en/free-pro-team@latest/github/setting-up-and-managing-organizations-and-teams/repository-permission-levels-for-an-organization) to the PowerShell repositories which gives them the power to:

1. `git push` to the official PowerShell repository
1. Merge [pull requests](https://docs.github.com/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests)
1. Assign labels, milestones, and people to [issues](https://guides.github.com/features/issues/) and [pull requests](https://docs.github.com/pull-requests/collaborating-with-pull-requests/proposing-changes-to-your-work-with-pull-requests/about-pull-requests)

## Table of Contents

- [Current Repository Maintainers](#current-repository-maintainers)
- [Repository Maintainer Responsibilities](#repository-maintainer-responsibilities)
- [Issue Management Process](#issue-management-process)
- [Pull Request Workflow](#pull-request-workflow)
- [LTS Servicing](#lts-servicing)
- [Becoming a Repository Maintainer](#becoming-a-repository-maintainer)

## Current Repository Maintainers

<!-- please keep in alphabetical order -->

- Aditya Patwardhan ([adityapatwardhan](https://github.com/adityapatwardhan))
- Andrew Menagarishvili ([anmenaga](https://github.com/anmenaga))
- Dongbo Wang ([daxian-dbw](https://github.com/daxian-dbw))
- Ilya Sazonov ([iSazonov](https://github.com/iSazonov))
- Robert Holt ([rjmholt](https://github.com/rjmholt))
- Travis Plunk ([TravisEz13](https://github.com/TravisEz13))

## Former Repository Maintainers

<!-- please keep in alphabetical order -->

- Andy Jordan ([andyleejordan](https://github.com/andyleejordan))
- Jason Shirk ([lzybkr](https://github.com/lzybkr))
- Mike Richmond ([mirichmo](https://github.com/mirichmo))
- Sergei Vorobev ([vors](https://github.com/vors))

## Repository Maintainer Responsibilities

Repository Maintainers enable rapid contributions while maintaining a high level of quality in PowerShell by ensuring that all development processes are being followed correctly.

If you are a Repository Maintainer, you:

1. **MUST** abide by the [Code of Conduct](../../CODE_OF_CONDUCT.md) and report suspected violations to the [PowerShell Committee][ps-committee]
1. **MUST** ensure that each contributor has signed a valid Microsoft Contributor License Agreement (CLA)
1. **MUST** verify compliance with any third party code license terms (e.g., requiring attribution, etc.) if the contribution contains third party code.
1. **MUST** make sure that [any change requiring approval from the PowerShell Committee](../community/governance.md#changes-that-require-an-rfc) has gone through the proper [RFC][RFC-repo] or approval process
1. **MUST** validate that code reviews have been conducted before merging a pull request when no code is written
1. **MUST** validate that tests and documentation have been written before merging a pull request that contains new functionality
1. **SHOULD** add [the correct labels][issue-management] to issues and pull requests
1. **SHOULD** make sure the correct [Area Experts](../community/governance.md#area-experts) are assigned to relevant pull requests and issues.
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
1. **SHOULD** encourage contributors to write meaningful, descriptive git commits
1. **SHOULD NOT** merge pull requests with a failed CI build
  (unless, for instance, the pull request is being submitted to fix broken CI)
1. **SHOULD NOT** merge pull requests without the status check passing from the Microsoft CLA bot
  (unless the CLA bot is broken, and CLA signing can be confirmed through other means)
1. **SHOULD NOT** merge pull requests too quickly after they're submitted.
  Even if the pull request meets all the requirements, people should have time to give their input
  (unless the pull request is particularly urgent for some reason)
1. **SHOULD NOT** merge your own pull requests.
  If a Repository Maintainer opens a pull request, another Maintainer should merge it unless there are extreme, short-term circumstances requiring a merge or another Maintainer has given explicit sign-off without merging

## Issue Management Process

Please see [Issue Management][issue-management]

## Pull Request Workflow

Please see [Contributing][CONTRIBUTING]

## Maintainer Best Practices

Please see [Best Practices][best-practice]

## LTS Servicing

For information about servicing Long-Term Servicing (LTS) releases and backporting fixes to LTS branches:

- [LTS Servicing Criteria](../release/LTS-servicing-criteria.md)

This document provides criteria for evaluating which fixes are appropriate for LTS releases, the backport request process, and decision-making guidelines.

## Becoming a Repository Maintainer

Repository Maintainers currently consist mostly of Microsoft employees.
It is expected that over time, regular trusted contributors to the PowerShell repository will be made Repository Maintainers.
Eligibility is heavily dependent on the level of contribution and expertise: individuals who contribute consistently in meaningful ways to the project will be recognized accordingly.

At any point in time, the existing Repository Maintainers can unanimously nominate a strong community member to become a Repository Maintainer.
Nominations are brought to the PowerShell Committee to understand the reasons and justification.
A simple majority of the PowerShell Committee is required to veto the nomination.
When a nominee has been approved, a PR will be submitted by a current Maintainer to update this document to add the nominee's name to
the [Current Repository Maintainers](#current-repository-maintainers) with justification as the description of the PR to serve as the public announcement.

[RFC-repo]: https://github.com/PowerShell/PowerShell-RFC
[ci-system]: ../testing-guidelines/testing-guidelines.md#ci-system
[issue-management]: issue-management.md
[CONTRIBUTING]: ../../.github/CONTRIBUTING.md
[best-practice]: best-practice.md
[ps-committee]: ../community/governance.md#powershell-committee
