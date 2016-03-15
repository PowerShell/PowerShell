# Contributing to Project Magrathea

## Rules

**Do not commit code changes to the master branch!**

**Read the documentation on [submodules][]!**

**Do not commit submodule updates accidentally!**

Don't forget to commit early and often!

Please add `[ci skip]` to commits that should be ignored by the CI systems
(e.g. changes to documentation).

All pull requests **must** pass both CI systems before they will be approved.

Write *good* commit messages. Follow Tim Pope's [guidelines][]:

* The first line *must* be a short, capitalized summary
* The second line *must* be blank
* The rest should be a wrapped, detailed explanation of the what and why
* The tone should be imperative

[guidelines]: http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html

## Installation

#### Windows

Install [Git for Windows][].

During the install process, choose these recommended settings:

* Use Git from the Windows Command Prompt
* Use OpenSSH
* Checkout Windows-style, commit Unix-style line endings
* Use Windows' default console window
* Enable file system caching

#### Linux

Install via the package manager:

```sh
sudo apt-get install git
```

#### Authentication

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

Alternatively, on Windows, you can try the
[Git Credential Manager for Windows][manager].

[manager]: https://github.com/Microsoft/Git-Credential-Manager-for-Windows
[Git for Windows]: https://git-scm.com/download/win
[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/

## New to Git?

If you're new to Git, learn the following commands: `checkout`, `branch`,
`pull`, `push`, `merge`.

Use GitHub's [Hello World][] to learn how to create a feature branch, commit
changes, and issue a pull request.

The user name and email must be set in order to commit changes:

```sh
git config --global user.name "First Last"
git config --global user.email "alias@microsoft.com"
```

[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules
[hello world]: https://guides.github.com/activities/hello-world/
[guides]: https://guides.github.com/activities/hello-world/

#### Microsoft employees

Microsoft employees should follow Microsoft open source [guidelinces][MS-OSS-Hub].

Particularly:

* [Join][MS-OSS-Hub] Microsoft github organization.
* Use your `alias@microsoft.com` for commit messages email. 
It the requirement for contributions made as part of your work at Microsoft.
* Enable [2 factor authentication][].

[MS-OSS-Hub]: https://opensourcehub.microsoft.com/articles/how-to-join-microsoft-github-org-self-service
[2 factor authentication]: https://github.com/blog/1614-two-factor-authentication

## Permissions

If you have difficulty in pushing your changes, there is a high
probability that you actually don't have permissions.

Be sure that you have write access to corresponding repo (remember
that submodules have their own privilege).

You do *not* necessarily need to have write permissions to the main
repositories, as you can also just [fork a repo][].

[fork a repo]: https://help.github.com/articles/fork-a-repo/

## Rebase and Fast-Forward Merge Pull Requests

Because GitHub's "Merge Pull Request" button merges with `--no-ff`, an extra
merge commit will always be created. This can be especially annoying when
trying to commit updates to submodules. Therefore our policy is to merge using
the Git CLI after approval, with a rebase onto master to enable a fast-forward
merge. If you are uncomfortable doing this, please ask @andschwa to merge.

## Submodules

This repository is a superproject with a half-dozen [submodules][]. **DO NOT**
commit updates unless absolutely necessary. When submodules must be updated, a
separate Pull Request must be submitted, reviewed, and merged before updating
the superproject. When committing submodule updates, ensure no other changes
are in the same commit. Submodule bumps may be included in feature branches for
ease of work, but the update must be independently approved before merging into
master.

[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Recommended Git configurations

I highly recommend these configurations to help deal with whitespace, rebasing,
and general use of Git.

> Auto-corrects your command when it's sure (`stats` to `status`)
```sh
git config --global help.autoCorrect -1
```

> Refuses to merge when pulling, and only pushes to branch with same name.
```sh
git config --global pull.ff only
git config --global push.default current
```

> Shows shorter commit hashes and always shows reference names in the log.
```sh
git config --global log.abbrevCommit true
git config --global log.decorate short
```

> Ignores whitespace changes and uses more information when merging.
```sh
git config --global apply.ignoreWhitespace change
git config --global rerere.enabled true
git config --global rerere.autoUpdate true
git config --global am.threeWay true
```
