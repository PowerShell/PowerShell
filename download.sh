#!/usr/bin/env bash

[[ -n $GITHUB_TOKEN ]] || { echo >&2 "GITHUB_TOKEN variable is undefined, please provide token"; exit 1; }

# Authorizes with read-only access to GitHub API
# Retrieves URL of v0.3.0 release asset
# Downloads asset to powershell.deb
curl -s -i \
     -H "Authorization: token $GITHUB_TOKEN " \
     -H 'Accept: application/octet-stream' \
     'https://api.github.com/repos/PowerShell/PowerShell/releases/assets/1536045' |
    grep location |
    sed 's/location: //g' |
    wget -i - -O powershell.deb
