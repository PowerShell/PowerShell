#!/usr/bin/env sh

# Docker requires the volume path to be absolute... so we resolve it ourselves.
docker run --rm --interactive --tty --volume $(dirname $(pwd))/:/opt/monad --workdir /opt/monad/scripts andschwa/magrathea $@
