# docker run magrathea with a non-interactive tty
monad-run()
{
    monad-docker-run "--tty" $*
}

# docker run magrathea with interactive tty
monad-it()
{
    monad-docker-run "--interactive --tty" $*
}

monad-attach()
{
    monad-docker-run "--attach STDOUT --attach STDERR" $*
}

# runs ephemeral andschwa/magrathea docker container with local
# directory mounted and workdir set to /opt
monad-docker-run()
{
    local CONSOLE=$1
    shift 1
    docker run --rm \
	   --volume $(pwd)/:/opt \
	   --workdir /opt \
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
    # default docker-machine VM does not change
    if [[ $OSTYPE == darwin* ]]; then
	CUID=1000
	CUSER=docker
	CGID=50
	CGROUP=staff
    fi
    echo \
	groupadd -o -f -g $CGID $CGROUP '&&' \
	useradd -u $CUID -g $CGID -d /opt $CUSER '&&' \
	sudo --set-home -u $CUSER -g $CGROUP --
}
