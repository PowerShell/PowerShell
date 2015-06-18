#!/bin/bash

branch=$(git for-each-ref --format=$'%(objectname) %(refname:short)' refs/heads | awk "/^$(git rev-parse HEAD)/ {print \$2}")
echo $branch

