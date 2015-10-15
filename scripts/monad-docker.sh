# docker run magrathea without tty
monad-run()
{
    monad-docker-run "--attach STDOUT --attach STDERR" $*
}

# docker run magrathea with interactive tty
monad-tty()
{
    monad-docker-run "--interactive --tty" $*
}

# runs ephemeral andschwa/magrathea docker container with local
# directory mounted to /opt and workdir set to /opt/scripts
monad-docker-run()
{
    local CONSOLE=$1
    shift 1
    docker run --rm \
	   --volume $(dirname $(pwd))/:/opt \
	   --workdir /opt/scripts \
	   $CONSOLE \
	   andschwa/magrathea:latest \
	   bash -c "$(monad-impersonate) bash -c '$*'"
}

# creates new user in container matching the local user so that
# artifacts will be owned by the local user; set IMPERSONATE to false
# to disable and run as root, defaults to true
if [[ ! $IMPERSONATE ]]; then IMPERSONATE=true; fi
monad-impersonate()
{
    if ! $IMPERSONATE; then return; fi
    local CUID=$(id -u)
    local CUSER=$(id -un)
    local CGID=$(id -g)
    local CGROUP=$(id -gn)
    echo \
	groupadd -g $CGID $CGROUP '&&' \
	useradd -u $CUID -g $CGID -d /opt $CUSER '&&' \
	sudo --set-home -u $CUSER -g $CGROUP --
}
