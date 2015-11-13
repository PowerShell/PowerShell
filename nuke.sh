#!/usr/bin/env bash

rm -rf bin
for dir in $(find . -maxdepth 2 -name project.json | parallel dirname)
do
    pushd $dir
    rm -rf bin
    popd
done
