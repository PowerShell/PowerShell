#!/usr/bin/env bash

for dir in $(find . -maxdepth 2 -name project.json | parallel dirname)
do
    pushd $dir
    dnu build
    popd
done
