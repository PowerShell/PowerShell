# Quick git-primer for sd users

There few important conceptual differences with **sd**.
This list tries to bridge the gap

* When you do `git commit` changes are stored in your local clone of the repo. 
To Submit them, you need to do `git push`.
* Forks are just a way to store branches. 
You can push branch to the original repo (if you have permissions) or to the fork. 
There is no principal difference.
* Most of the git complications come from the fact that you need to do integrations by yourself.


The Microsoft Open Source Hub [article](https://opensourcehub.microsoft.com/articles/git-for-sd-users) for other details.

### Rosetta stone


#### Concepts

| Concept | SD termin | Git termin |
|---------|----|----------------|
| Your local copy | enlistment | cloned repo |
| Code changes  | changelist | commit |
| Way to preserve changes locally | dpk | local branch |


#### Commands 

Before running a git command for the first time, try

```sh
git help <command name>
```

To read about flags and the overall meaning.

|Scenarios | SD command | Git commmand |
|---------|----|----------------|
| Get the code at the first place | sd enlist | git clone |
| Sync changes  |  sd sync | git pull |
| Add new files |  sd add  | git add |
| Modify existing files | sd edit | git add |
| Remove existing files | sd delete | git rm |
| Copy/Rename files | sd branch | git mv |
| Commit to local |   | git commit |
| Submit changes | sd submit | git push |
| Revert changes | sd revert | git checkout |
| Undo submitted changes | sd undo | git revert |
| Diff tool | odd | git diff |
| Clean enlistment  |  build nuke | git clean |

