$BIN = "${pwd}/bin"

# Publish PowerShell
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN
# Copy files that dotnet-publish does not currently deploy
cd ../..
