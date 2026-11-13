# Working with PowerShell repository

## Get the code for the first time

```sh
git clone https://github.com/PowerShell/PowerShell.git --branch=master
```

## Branches

* Don't commit your changes directly to master.
  It will make the collaborative workflow messy.
* Checkout a new local branch from `master` for every change you want to make (bugfix, feature).
* Use lowercase-with-dashes for naming.
* Follow [Linus' recommendations][Linus] about history.
  * "People can (and probably should) rebase their _private_ trees (their own work). That's a _cleanup_. But never other peoples code. That's a 'destroy history'...
  You must never EVER destroy other peoples history. You must not rebase commits other people did.
  Basically, if it doesn't have your sign-off on it, it's off limits: you can't rebase it, because it's not yours."

### Understand branches

* **master** is the branch with the latest and greatest changes.
  It could be unstable.
* Send your pull requests to **master**.

### Sync your local repository

Use **git rebase** instead of **git merge** and **git pull**, when you're updating your feature-branch.

```sh
# fetch updates all remote branch references in the repository
# --all : tells it to do it for all remotes (handy, when you use your fork)
# -p : tells it to remove obsolete remote branch references (when they are removed from remote)
git fetch --all -p

# rebase on origin/master will rewrite your branch history
git rebase origin/master
```

### More complex scenarios

Covering all possible git scenarios is behind the scope of the current document.
Git has excellent documentation and lots of materials available online.

We are leaving a few links here:

[Linus]:https://web.archive.org/web/20230522041845/https://wincent.com/wiki/git_rebase%3A_you're_doing_it_wrong

## Tags

If you are looking for the source code for a particular release,
you will find it via **tags**.

* `git tag` will show you list of all tags.
* Find the tag that corresponds to the release.
* Use `git checkout <tag-name>` to get this version.

**Note:** [checking out a tag][tag] will move the repository to a [DETACHED HEAD][HEAD] state.

[tag]:https://git-scm.com/book/en/v2/Git-Basics-Tagging#Checking-out-Tags
[HEAD]:https://www.git-tower.com/learn/git/faq/detached-head-when-checkout-commit

If you want to make changes, based on tag's version (i.e. a hotfix),
checkout a new branch from this DETACHED HEAD state.

```sh
git checkout -b vors/hotfix
```

## Recommended Git configurations

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
