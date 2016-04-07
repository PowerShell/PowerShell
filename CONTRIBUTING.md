Contributing to Project Magrathea
=================================

Rules
-----

**Do not commit code changes to the master branch!**

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

New to Git?
-----------

- [Git Basics](docs/git/basics.md): install and getting started.
- [Git for sd users](docs/git/source-depot.md): a handy reference document for people familiar with `sd`.
- [Commit process](docs/git/committing.md): step-by-step commit guide with all gory details.

Authentication
--------------

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

Alternatively, on Windows, you can try the
[Git Credential Manager for Windows][manager].

[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/
[manager]: https://github.com/Microsoft/Git-Credential-Manager-for-Windows

Microsoft employees
-------------------

Microsoft employees should follow Microsoft open source [guidelinces][MS-OSS-Hub].

Particularly:

* [Join][MS-OSS-Hub] Microsoft GitHub organization.
* Use your `alias@microsoft.com` for commit messages email. 
* Enable [2 factor authentication][].

[MS-OSS-Hub]: https://opensourcehub.microsoft.com/articles/how-to-join-microsoft-github-org-self-service
[2 factor authentication]: https://github.com/blog/1614-two-factor-authentication

[Branches](docs/workflow/branches.md)
--------

* Checkout a new local branch for every change you want to make (bugfix, feature).
* Use `alias/feature-name` pattern.
* Use lowercase-with-dashes for naming.
* Use same branch name in superproject and all [submodules][].

[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

Permissions
-----------

If you have difficulty in pushing your changes, there is a high
probability that you actually don't have permissions.

Be sure that you have write access to corresponding repo (remember
that submodules have their own privilege).

You do *not* necessarily need to have write permissions to the main
repositories, as you can also just [fork a repo][].

[fork a repo]: https://help.github.com/articles/fork-a-repo/

Recommended Git configurations
------------------------------

We highly recommend these configurations to help deal with whitespace,
rebasing, and general use of Git.

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

[Mapping](docs/workflow/mapping.md)
--------

Learn about new files locations in PowerShell/PowerShell.

[Resources](docs/workflow/resources.md)
--------

Learn how to work with string resources in `.resx` files.

