#!/usr/bin/env bash

#clean OMI if needed
if [$1 == "clean" ]; then
    pushd src/omi/Unix
    make clean
    popd
fi

# Build OMI
cd src/omi/Unix
./configure --dev
make -j
cd ../../..

# Build the OMI Provider
cd src/omi-provider
cmake .
make -j
cd ../..
