# See here for image contents: https://github.com/microsoft/vscode-dev-containers/tree/v0.241.1/containers/dotnet/.devcontainer/base.Dockerfile

#Required parameter. Specify a .NET version e.g. 6.0.100, 7.0.100-preview.6
ARG VERSION

#"Jammy" is Ubuntu 22.04 LTS. Update when a new devcontainer OS is desired
#NOTE: .NET has made Debian the default container type for .NET but PowerShell build tooling still only recognizes Ubuntu.
FROM mcr.microsoft.com/dotnet/sdk:${VERSION}-jammy

COPY library-scripts/*.sh /tmp/library-scripts/
#NOTE: common-debian will run apt update which is why we dont run it again
RUN \
export DEBIAN_FRONTEND=noninteractive \
&& /tmp/library-scripts/common-debian.sh \
&& rm -rf /tmp/library-scripts \
&& apt install -y --no-install-recommends \
nodejs \
yarn

USER vscode
SHELL ["pwsh","-c"]
ENTRYPOINT [ "pwsh" ]

