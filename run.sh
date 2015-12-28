export CORE_ROOT=$(pwd)/bin
./powershell $@
# Fix tty due to possible bug in .NET
stty echo
