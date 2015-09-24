#!/usr/bin/env sh

echo "$*"
# Runs by non-interactively, just attaches output
DOCKERFLAGS="--attach STDOUT --attach STDERR" ./build-run.sh "$*"
