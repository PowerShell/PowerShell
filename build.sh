#!/usr/bin/env bash

# Test for build dependencies
hash cmake 2>/dev/null || { echo >&2 "No cmake, please run 'sudo apt-get install cmake'"; exit 1; }
hash g++ 2>/dev/null || { echo >&2 "No g++, please run 'sudo apt-get install g++'"; exit 1; }
hash dotnet 2>/dev/null || { echo >&2 "No dotnet, please visit https://dotnet.github.io/getting-started/"; exit 1; }

# Test for lock file
test -r src/Microsoft.PowerShell.Linux.Host/project.lock.json || { echo >&2 "Please run 'dotnet restore' to download .NET Core packages"; exit 2; }

# Ensure output directory is made
BIN="$(pwd)/bin"
mkdir -p "$BIN"

# Build native library and deploy to bin
pushd src/libpsl-native
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
ctest -V
test -r src/libpsl-native.* || { echo >&2 "Compilation of libpsl-native failed"; exit 3; }
cp src/libpsl-native.* "$BIN"
popd

# Publish PowerShell to bin, with LINUX defined through a configuration
dotnet publish --output "$BIN" --configuration Linux src/Microsoft.PowerShell.Linux.Host

# Fix permissions for packaging
chmod -R go=u "$BIN"
