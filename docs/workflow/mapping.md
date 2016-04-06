# Mapping

PowerShell/PowerShell utilizes `dotnet cli` project model.
Source code for a library (executable) is located under `src/<library-name>`.
I.e. System.Management.Automation.dll sources are located under `src/System.Management.Automation`

In the windows source tree, this files located differently.
That's why we have `mapping.json` in the root of PowerShell/PowerShell repo.
This file is a simple json hashtable that describes mapping between files in source depot and GitHub.

Keys are relative file paths from `src\monad` submodule (that has the same layout as admin sd enlistment).
Values are relative file paths in PowerShell/PowerShell GitHub project.

**Note**: this "src\monad" prefix appears in keys for historical reasons.
We used to have a submodule at this path.
If you replace this `src\monad` with path to the **admin** enlistment, you will get the mapping to source depot.

### PowerShellGitHubDev.psm1

Our dev module contains a number of functions to work that can be used to work with this mapping file.

* `Copy-SubmoduleFiles` -- is used for "sd -> github" integration
* `New-MappingFile` -- was used to create the first version on mapping.json
* `Send-GitDiffToSd` -- the most interesting function for us: 
it applies patch from git to **admin** enslistment with respect to `mapping.json`.
It supports `-WhatIf` switch.

```
> Send-GitDiffToSd -diffArg1 45555786714d656bd31cbce67dbccb89c433b9cb -diffArg2 45555786714d656bd31cbce67dbccb89c433b9cb~1 -pathToAdmin d:\e\ps_dev\admin 
> cd d:\e\ps_dev\admin
> sd online ...
> # move files to new change list (i.e. with sdb)
> sd submit -c <n>
 
```

 
