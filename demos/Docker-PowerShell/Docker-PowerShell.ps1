# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# This is a short example of the Docker-PowerShell module. The same cmdlets may be used to manage both local & remote machines, including both Windows & Linux hosts
# The only difference between them is the example container image is pulled & run.

# Import the Docker module
#    It's available at https://github.com/Microsoft/Docker-PowerShell
Import-Module Docker

# Pull the 'hello-world' image from Docker Hub
Pull-ContainerImage hello-world # Linux
# Pull-ContainerImage patricklang/hello-world # Windows

# Now run it
Run-ContainerImage hello-world # Linux
# Run-ContainerImage patricklang/hello-world # Windows

# Make some room on the screen
cls

# List all containers that have exited
Get-Container | Where-Object State -EQ "exited"

# That found the right one, so go ahead and remove it
Get-Container | Where-Object State -EQ "exited" | Remove-Container

# Now remove the container image
Remove-ContainerImage hello-world

# And list the container images left on the container host
Get-ContainerImage
