export CORE_ROOT=$(pwd)/bin TEMP=/tmp
./powershell $@
# Fix tty due to possible bug in .NET
stty echo
