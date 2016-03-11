Working notes - 
#Commit Dance
Most of the time, you're going to be working in a submodule (say src/monad/monad/engine).
The submodule has a relationship to the SuperProject  (PowerShell), but in order to be sure that everyone is
notified about changes in a submodule, you need be sure that this is reflected as a pull request in the SuperProject.
Here's the process I followed to make a change to `src/monad/monad/src/engine/DataStoreAdapter.cs`

the following takes place in the submodule:
*  Make the code change
* Commit the code change
* Get the committed change into a branch
* Push the branch 
* Pull request in the submodule for notification purposes

Then, in the SuperProject (PowerShell) (git status will show a change _in my case it was `src/monad`_)

* Commit that change
* Get the committed change into a branch
* Push the branch
* Pull request in the superproject for notification purposes

the I used commands were:
```
cd $HOME/PowerShell/src/monad/monad/src/engine
vi DataStoreAdapter.cs
git commit -a
git checkout -b colon-drive
git push origin colon-drive
```
then I went to the web interface and did the pull request *in the submodule*. After that, back to the commandline:
```
cd $HOME/PowerShell
git checkout -b colon-drive
git commit -a
git push origin colon-drive
```
and back to the web interface for the pull request *in the superproject*.
