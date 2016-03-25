#!/usr/bin/env bash

#clean OMI if needed
if [$1 == "clean" ]; then
    pushd src/omi/Unix
    make clean
    popd
fi

# Build OMI
pushd src/omi/Unix
./configure --dev
make -j
popd

# Build the OMI Provider
pushd src/omi-provider
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
popd
