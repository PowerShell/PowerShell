./bin/Microsoft.PowerShell.Linux.Host -c "Invoke-Pester src/pester-tests/$1 -OutputFile pester-tests.xml -OutputFormat NUnitXml"
# XML files are not executable
chmod -x pester-tests.xml
