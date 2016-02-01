#!/usr/bin/env bash

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
