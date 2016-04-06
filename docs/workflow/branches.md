# Branches

PowerShell has a number of [long-living](https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows#Long-Running-Branches) branches

* **master** -- current development, upstream. It's based on source-depot, but has changes for Linux/OS X. 
It also has changes for the latest version of .NET Core, i.e. rc3.
* **source-depot** -- an exact mirror of dev branch in Source Depot. 
Here code flow in "sd -> github" case. 
It should be treated mostly as **read-only** (unless you are integrating changes "sd->github").
* **server2016** -- branch with the code that we want to ship in Server 2016. We use it for "github -> sd" integration.

### Example bugfix

Create a [feature-branch](https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows#Topic-Branches) from **server2016**.

```
> # switch branch to server2016 to create feature-branch from the right commit
> git checkout server2016
Branch server2016 set up to track remote branch server2016 from origin.
Switched to a new branch 'server2016'

# create feature branch
> git checkout -b vors/encoding
```

**Note** how we use `alias/feature-name` pattern in the example above.

Then we develop the changes in the feature branch. 
We can push it to the [`origin`](https://github.com/PowerShell/PowerShell) to kick-in CI build or share work-in-progress.

```
> git push origin vors/encoding
```

Eventually we merge feature branch back to **server2016** via a Pull Request with a codereview from my peers.

After merging feature branch we should bring changes into **master**, so we have bugfixes in the upstream branch as well.

```
> git checkout master
> git checkout -b vors/master
> # look-up the sha1 for relevant commits (i.e. with gitk --all)
> git cherry-pick <sha1>..<sha2>
> git push origin vors/master
```

**Note** the first commit is not included in the diaposone, so the interval is open on the left side.

Then I can create a Pull Requst from `vors/master` to `master` via GitHub web interface.
