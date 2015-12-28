export CORE_ROOT=$(pwd)/bin
./powershell -c "Invoke-Pester src/pester-tests/$1 -OutputFile pester-tests.xml -OutputFormat NUnitXml"
# XML files are not executable
chmod -x pester-tests.xml
# Fix tty due to possible bug in .NET
stty echo || true
