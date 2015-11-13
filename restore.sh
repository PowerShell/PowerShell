#!/usr/bin/env bash

for dir in $(find . -maxdepth 2 -name project.json | parallel dirname)
do
    echo $dir
    pushd $dir
    dnu restore
    popd
done
