#!/usr/bin/env bash

# Runs by non-interactively, just attaches output
export DOCKERFLAGS="--attach STDOUT --attach STDERR"
./build-run.sh "$*"
