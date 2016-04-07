Commit Dance
============

**Update:** commit dance became much simpler after
[removing psl-monad submodule](https://github.com/PowerShell/PowerShell/issues/656).
Thus, this really only applies to `src/windows-build` and
`src/Modules/Pester`. If you need to touch their content, this doc
provides the overview of the process. Remember that it's written
against `src/monad` submodule, which doesn't exist anymore.

Also see the [submodules documentation](./submodules.md).

Sometimes, you need to do the work in a submodule (i.e. you added a new string in `.resx` file).
The submodule has a relationship to the SuperProject  (PowerShell), but in order to be sure that CI is
notified about changes in a submodule, you need be sure that this is reflected as a pull request in the SuperProject.

**Scenario**: Jim fixes issue in `src/monad/monad/src/engine/DataStoreAdapter.cs`

the following takes place in the submodule:
* Make the code change
* Commit the code change in a feature branch. Branch name in form `<alias>/<moniker>`, i.e `jim/colon-drive`.
* Push the branch (in the submodule project): `git push origin jim/colon-drive`
* Pull request in the submodule to notify people via email about new code review. 

Then, in the SuperProject (PowerShell) 
* `git status` should show a change of submodule **as a whole** (not individual files). _In my case_ it is `src/monad`.
* `git diff` should show something like

```diff
index 1ba8dd4..ed5d202 160000
--- a/src/monad
+++ b/src/monad
@@ -1 +1 @@
-Subproject commit 1ba8dd4f721f2cd4363f64609515ec1600378c30
+Subproject commit ed5d2022617f317a78a8a57e2c257c8e29cddbd5
```

* Commit that change into a branch with the same name, i.e. `jim/colon-drive`
* Push the branch (in the SuperProject): `git push origin jim/colon-drive`
* Pull request in the superproject to kick-in CI build and notify people. Reference submodule pull-request in the PR message to make navigation between two simpler.

### Commands log

Here is a transcript of command Jim used:
```
cd $HOME/PowerShell/src/monad/monad/src/engine
vi DataStoreAdapter.cs
git checkout -b jim/colon-drive
git commit -a
git push origin jim/colon-drive
```
then I went to the web interface and did the pull request *in the submodule*. After that, back to the commandline:
```
cd $HOME/PowerShell
git checkout -b jim/colon-drive
git commit -a
git push origin jim/colon-drive
```
and back to the web interface for the pull request *in the superproject*.

