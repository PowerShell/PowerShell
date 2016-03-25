#!/usr/bin/env bash

test -x bin/powershell || { echo >&2 "No bin/powershell, please run './build.sh'"; exit 1; }

./bin/powershell --noprofile -c "Import-Module Microsoft.PowerShell.Platform; Invoke-Pester test/powershell/$1 -OutputFile pester-tests.xml -OutputFormat NUnitXml -EnableExit"
failed_tests=$?

# XML files are not executable
chmod -x pester-tests.xml

# Return number of failed tests as exit code (more than 0 will be an error)
exit $failed_tests
