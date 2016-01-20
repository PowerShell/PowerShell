# Contributing to Project Magrathea

## New to Git?

**DO NOT COMMIT TO THE MASTER BRANCH**

**Do not commit submodule updates accidentally**

Use GitHub's [Hello World][] to learn how to create a feature branch, commit
changes, and issue a pull request.

[hello world]: https://guides.github.com/activities/hello-world/

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
