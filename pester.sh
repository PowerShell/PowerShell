./bin/powershell -c "Invoke-Pester test/powershell/$1 -OutputFile pester-tests.xml -OutputFormat NUnitXml"
# XML files are not executable
chmod -x pester-tests.xml
