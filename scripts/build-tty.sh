#!/usr/bin/env sh

# Runs with a pseudo tty so that interactive shells can be opened
DOCKERFLAGS="--interactive --tty" ./build-run.sh "$*"
