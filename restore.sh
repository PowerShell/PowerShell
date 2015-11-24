#!/usr/bin/env bash

source ~/.dnx/dnvm/dnvm.sh
dnvm use 1.0.0-rc2-16177

for dir in $(find . -maxdepth 3 -name project.json | xargs dirname)
do
    (
    cd $dir
    dnu restore
    )
done
