#!/usr/bin/env bash

# Build OMI
function build {
    pushd src/omi/Unix
    ./configure --dev
    make -j
    popd
}

#clean OMI if needed
function clean {
    pushd src/omi/Unix
    make clean
    popd
}
while [ "$1" != 0 ]; do
  case $1 in
    -c | --clean)     shift
                      clean
                      build
                      exit
  esac
  shift
  build
  exit
done

