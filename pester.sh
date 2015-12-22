export CORE_ROOT=$(pwd)/bin
./powershell -c "Invoke-Pester src/pester-tests/$1"
# Fix tty due to possible bug in .NET
stty echo
