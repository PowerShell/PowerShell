# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [string]$RepoRoot = (Join-Path -Path $PSScriptRoot -ChildPath '../..'),
    [string]$Platform
)

$resolvedPath = (Resolve-Path $RepoRoot).ProviderPath
$platformParam = @()
if ($Platform) {
    $platformParam = @("--platform", $Platform)
}

docker run $platformParam -e RUN_LOCAL=true --env-file "$PSScriptRoot/config/super-linter.env" -v "${resolvedPath}:/tmp/lint"  ghcr.io/super-linter/super-linter:latest
