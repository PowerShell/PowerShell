#-------------------------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License. See https://go.microsoft.com/fwlink/?linkid=2090316 for license information.
#-------------------------------------------------------------------------------------------------------------

FROM mcr.microsoft.com/powershell:preview-fedora-30@sha256:f405d4d60f8d196532da75038c76c052084ef02121f8e2d3852080ff4a230a5a

# Configure apt and install packages
RUN dnf install -y git procps wget findutils \
    && dnf clean all
