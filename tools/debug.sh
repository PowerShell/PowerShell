#!/usr/bin/env bash

hash lldb-3.6 2>/dev/null || { echo >&2 "No lldb-3.6, please run 'sudo apt-get install lldb-3.6'"; exit 1; }
test -x debug/powershell || { echo >&2 "No debug/powershell, please run 'Start-PSBuild -Publish -Output debug'"; exit 1; }
test -x debug/libsosplugin.so || { echo >&2 "No debug/libsosplugin.so, please run 'Start-PSBuild -Publish -Output debug'"; exit 1; }

cat << EOF

Launching LLDB with SOS plugin...
Type 'run' or 'r' to start PowerShell.
Press Ctrl-C to interrupt PowerShell and run LLDB commands.
Type 'exit' when interrupted to leave LLDB.

Visit https://git.io/v2Jhh for CoreCLR debugging help.

Most useful commands are 'clrstack', 'clrthreads', and 'pe'.

EOF

pushd debug
lldb-3.6 -o "plugin load libsosplugin.so" -- ./pwsh $@
popd
