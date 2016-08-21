FROM ubuntu:16.04

RUN apt-get update && \
    apt-get install -y curl libunwind8 libicu55 libcurl3 && \
    curl -LO https://raw.githubusercontent.com/PowerShell/PowerShell/master/tools/download.sh && \
    chmod +x download.sh && \
    ./download.sh && \
    rm powershell_*.deb && \
    rm download.sh && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

ENTRYPOINT ["powershell"]
