#!/usr/bin/env sh

# --rm: always run ephemerally
# --volume: path must be absolute, so resolve it
# --workdir: start location for Make
# $DOCKERFLAGS: additional flags
# magrathea: contains all dependencies
# bash: use $* over $@ so that multi-word parameters aren't split up
docker run --rm \
       --volume $(dirname $(pwd))/:/opt/monad-linux \
       --workdir /opt/monad-linux/scripts \
       $DOCKERFLAGS \
       andschwa/magrathea:latest \
       bash -c "$*"
