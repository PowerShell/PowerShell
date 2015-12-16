export CORE_ROOT=$(pwd)/bin TEMP=/tmp
lldb-3.6 -o "plugin load ./bin/libsosplugin.so" -- ./powershell $@
