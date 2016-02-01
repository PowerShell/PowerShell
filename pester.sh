#!/usr/bin/env bash

./bin/powershell --noprofile -c "Invoke-Pester test/powershell/$1 -OutputFile pester-tests.xml -OutputFormat NUnitXml -EnableExit"
failed_tests=$?

# XML files are not executable
chmod -x pester-tests.xml

# Exit with failure if number of failed tests exceeds threshold
[ ! $failed_tests -gt 14 ]
