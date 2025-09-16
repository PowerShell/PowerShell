#!/usr/bin/env bash
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This script is a thin wrapper around the official install-powershell.sh script.
# It's purpose is to be a predictable entry point for CI.
# The official script's name may change, but this script's name will not.

set -e

# Create a temporary file to store the downloaded script
# and ensure it's cleaned up when the script exits.
temp_script_path=$(mktemp)
trap 'rm -f "$temp_script_path"' EXIT

# Download the script from the stable URL to the temporary file.
# The -L flag is important to follow redirects from aka.ms.
curl -sL "https://aka.ms/install-powershell.sh" -o "$temp_script_path"

# Execute the downloaded script with all arguments passed to this wrapper.
bash "$temp_script_path" "$@"
