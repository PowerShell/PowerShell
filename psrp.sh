export CORE_ROOT=$(pwd)/bin
lldb-3.6 -o "plugin load ./bin/libsosplugin.so" -- ./src/omi/Unix/output/bin/omiserver --ignoreAuthentication
