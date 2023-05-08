#-------------------------------------------------------------------------------------------------------------
# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License. See https://go.microsoft.com/fwlink/?linkid=2090316 for license information.
#-------------------------------------------------------------------------------------------------------------

FROM mcr.microsoft.com/powershell:preview-fedora-30

# Configure apt and install packages
RUN dnf install -y git procps wget findutils \
    && dnf clean all
