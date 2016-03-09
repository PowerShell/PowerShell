#!/usr/bin/env bash

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
