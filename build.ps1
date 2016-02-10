$BIN = "${pwd}/bin"

mkdir $BIN/Modules -ErrorAction SilentlyContinue

# Deploy PowerShell modules
cd $BIN/Modules
robocopy ../../test/Pester Pester /s /e
cd ../..

# Publish PowerShell
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN
# Copy files that dotnet-publish does not currently deploy
cd ../..
