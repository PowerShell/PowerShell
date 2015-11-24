#!/usr/bin/env bash

for dir in $(find . -maxdepth 3 -name project.json | xargs dirname)
do
    (
    cd $dir
    dnu build
    )
done
