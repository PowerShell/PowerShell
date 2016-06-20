# Branches

PowerShell has a number of [long-living](https://git-scm.com/book/en/v2/Git-Branching-Branching-Workflows#Long-Running-Branches) branches

* **master** -- current development, upstream. It's based on source-depot, but has changes for Linux/OS X. 
It also has changes for the latest version of .NET Core, i.e. rc3.
* **source-depot** -- an exact mirror of dev branch in Source Depot for all [mapped](./mapping.md) files. 
Here code flow in "sd -> github" case. 
It should be treated mostly as **read-only** (unless you are integrating changes "sd->github").

