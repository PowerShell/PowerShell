#!/usr/bin/env bash

echo "Installing build dependencies"

curl http://llvm.org/apt/llvm-snapshot.gpg.key | sudo apt-key add -
echo "deb http://llvm.org/apt/trusty/ llvm-toolchain-trusty-3.6 main" | sudo tee /etc/apt/sources.list.d/llvm.list
sudo apt-get update -qq

sudo apt-get install -y wget make g++ cmake \
     libc6 libgcc1 libstdc++6 \
     libcurl3 libgssapi-krb5-2 libicu52 liblldb-3.6 liblttng-ust0 libssl1.0.0 libunwind8 libuuid1 zlib1g clang-3.5

wget -P /tmp https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/Latest/dotnet-host-ubuntu-x64.latest.deb
sudo dpkg -i /tmp/dotnet-host-ubuntu-x64.latest.deb

wget -P /tmp https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/Latest/dotnet-sharedframework-ubuntu-x64.latest.deb
sudo dpkg -i /tmp/dotnet-sharedframework-ubuntu-x64.latest.deb

wget -P /tmp https://dotnetcli.blob.core.windows.net/dotnet/beta/Installers/Latest/dotnet-sdk-ubuntu-x64.latest.deb
sudo dpkg -i /tmp/dotnet-sdk-ubuntu-x64.latest.deb
