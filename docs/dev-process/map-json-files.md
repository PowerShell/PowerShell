# Mapping

PowerShell/PowerShell utilizes `dotnet cli` project model.
Source code for a library (executable) is located under `src/<library-name>`.
I.e. System.Management.Automation.dll sources are located under `src/System.Management.Automation`

In the windows source tree, this files located differently.
That's why we use `map.json` files in `src/<library-name>`.
This file is a simple json hashtable that describes mapping between files in source depot and GitHub.

* Keys are relative file paths from `psl-monad` (that has the same layout as admin sd enlistment).
* Values are file paths, relative to the corresponding `map.json` folder.

#### Example

There is an entry 

```
"monad/src/engine/COM/ComMethod.cs":  "engine/COM/ComMethod.cs",
```

in `.\src\System.Management.Automation\map.json`.
It tells us that file `ComMethod.cs` located in `monad/src/engine/COM/ComMethod.cs` in psl-monad (and sd enlistment).
And in PowerShell/PowerShell it mapped into `src/System.Management.Automation/engine/COM/ComMethod.cs`

### build.psm1

Our dev module contains a number of functions to work that can be used to work with this mapping file.

* `Copy-MappedFiles` -- copies files from psl-monad into PowerShell/PowerShell. Used for "sd -> github" integration.
* `Send-GitDiffToSd` -- applies patch from git to **admin** enlistment with respect to all `map.json` files. 
  It supports `-WhatIf` switch.

```
> Send-GitDiffToSd -diffArg1 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be~1 -diffArg2 32b90c048aa0c5bc8e67f96a98ea01c728c4a5be -AdminRoot d:\e\ps_dev\admin 
> cd d:\e\ps_dev\admin
> sd online ...
> # move files to new change list (i.e. with sdb)
> sd submit -c <n>
 
```

## Updating `map.json`

If you are bringing new (that are not yet included) files from source-depot, you need to update `map.json` in the corresponding folder to include them.

This way, we can keep track of changes and have ability to integrate changes back to Source Depot.

Use this approach for any files from source-depot (including test files) 

## Creating `map.json`

If you are creating new folder for that, create `map.json` with all mapped files in it.

* Make a separate commit with update/creation for `map.json`.
  Separate commit will help to manage this change.

```
> mkdir .\src\My.New.Module
> notepad .\src\My.New.Module\map.json
# add mappings into the file
```

* Find current baseline SD change-list in tags:

```
> git tag
SD-692351
SD-693793
SD/688741
SD/692351
SD/693793
SD/695331
SD/700586
SD/704605
SD/706766
SD/709766  <--- the last changelist
v0.1.0
v0.2.0
v0.3.0
v0.4.0
```

* Find corresponding commit in psl-monad and check it out.

```
> Push-Location ..\psl-monad
> git checkout 85e2ecd
> Pop-Location
```

* Use `Copy-MappedFiles` function to copy files on disk.

```
> Copy-MappedFiles -PslMonadRoot ..\psl-monad -Path .\src\My.New.Module
```

* Make a separate commit with mapped files.
  Use `--author="PowerShell Team <PowerShellTeam@hotmail.com>"` switch to indicate that it's a collective work.

```
git commit --author="PowerShell Team <PowerShellTeam@hotmail.com>"
```
