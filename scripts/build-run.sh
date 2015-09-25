#!/usr/bin/env sh

CUID=$(id -u)
CUSER=$(id -un)
CGID=$(id -g)
CGROUP=$(id -gn)
DIR=/opt/monad-linux
VOLUME=$(dirname $(pwd))/:$DIR

# creates new user in container matching the local user so that
# artifacts will be owned by the local user (instead of root)
impersonate()
{
    echo \
	groupadd -g $CGID $CGROUP '&&' \
	useradd -u $CUID -g $CGID -d $DIR $CUSER '&&' \
	sudo -u $CUSER -g $CGROUP
}

docker run --rm \
       --volume $VOLUME \
       --workdir $DIR/scripts \
       $DOCKERFLAGS \
       andschwa/magrathea:latest \
       bash -c "$(impersonate) $*"
