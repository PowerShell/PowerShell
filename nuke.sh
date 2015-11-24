#!/usr/bin/env bash

rm -rf approot
for dir in $(find . -maxdepth 3 -name project.json | xargs dirname)
do
    (
	cd $dir
	rm -rf bin
    )
done
