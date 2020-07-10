# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#
# Wrap Python script in such a way to make it easy to
# consume from PowerShell
#
# The variable $PSScriptRoot points to the directory
# from which the script was executed. This allows
# picking up the Python script from the same directory
#

& $PSScriptRoot/class1.py | ConvertFrom-Json

