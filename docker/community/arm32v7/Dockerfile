FROM arm32v7/ubuntu:bionic

RUN \
  apt-get update \
  && apt-get install --no-install-recommends ca-certificates libunwind8 libssl1.0 libicu60 wget --yes \
  && wget https://github.com/PowerShell/PowerShell/releases/download/v6.1.0/powershell-6.1.0-linux-arm32.tar.gz \
  && mkdir ~/powershell \
  && tar -xvf ./powershell-6.1.0-linux-arm32.tar.gz -C ~/powershell \
  && ln -s /root/powershell/pwsh /usr/bin/pwsh \
  && apt-get clean \
  && rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["pwsh"]
