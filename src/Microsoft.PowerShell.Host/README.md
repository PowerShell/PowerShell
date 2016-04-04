# Core PowerShell Host

This host is based off the infamous `host06` example. It relies only on managed
code, and is cross-platform. It needs a fair bit of work, but it loads
profiles, has tab-completion, and can be used to debug PowerShell scripts.

## Executable

The `bin/powershell[.exe]` executable for Core PowerShell is built by this
project, as it is the top dependency of the graph, and has `emitEntryPoint:
true`, meaning a native executable is produced automatically by CLI (no need to
own a separate native host). It is also the project that deploys the `ps1xml`
types and formatting files, as well as the default profile and the included
PowerShell modules.

Note that many of these should probably live with System.Management.Automation,
but we're waiting on a bug fix from CLI to move them.

## update-content.sh

This script is used to update our current tree's files with those that live in
`src/monad`. We only need to update them when new changes are merged from
Source Depot, and the build scripts are much simpler without this logic. With
the files living here, the `content` key in the `project.json` handles the
deployment for us in a cross-platform manner.
