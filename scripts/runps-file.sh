#!/bin/bash

SCRIPTDIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

"$SCRIPTDIR/runps-simple.sh" "& $@"
