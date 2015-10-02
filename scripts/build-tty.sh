#!/usr/bin/env bash

# Runs with a pseudo tty so that interactive shells can be opened
export DOCKERFLAGS="--interactive --tty"
./build-run.sh "$*"
