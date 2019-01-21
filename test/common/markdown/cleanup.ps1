# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Start-Process `
-FilePath "npm" `
-ArgumentList @('install','rimraf','-g','--silent') `
-Wait `
-WorkingDirectory $PSScriptRoot `
-NoNewWindow
Start-Process `
-FilePath "rimraf" `
-ArgumentList @(Join-Path -Path $PSScriptRoot -ChildPath 'node_modules') `
-Wait `
-WorkingDirectory $PSScriptRoot `
-NoNewWindow
