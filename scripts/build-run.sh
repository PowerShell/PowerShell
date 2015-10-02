#!/usr/bin/env sh

CUID=$(id -u)
CUSER=$(id -un)
CGID=$(id -g)
CGROUP=$(id -gn)
DIR=/opt/monad-linux
VOLUME=$(dirname $(pwd))/:$DIR

# creates new user in container matching the local user so that
# artifacts will be owned by the local user; set IMPERSONATE to false
# to disable and run as root, defaults to true
if [[ ! $IMPERSONATE ]]; then IMPERSONATE=true; fi
impersonate()
{
    if ! $IMPERSONATE; then return; fi
    echo \
	groupadd -g $CGID $CGROUP '&&' \
	useradd -u $CUID -g $CGID -d $DIR $CUSER '&&' \
	sudo --set-home -u $CUSER -g $CGROUP
}

docker run --rm \
       --volume $VOLUME \
       --workdir $DIR/scripts \
       $DOCKERFLAGS \
       andschwa/magrathea:latest \
       bash -c "$(impersonate) $*"
