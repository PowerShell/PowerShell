$BIN = "${pwd}/bin"

mkdir $BIN/Modules -ErrorAction SilentlyContinue

# Deploy PowerShell modules
cd $BIN/Modules
robocopy ../../test/Pester Pester /s /e
robocopy ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility Microsoft.PowerShell.Utility /s /e
cp ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility/CoreClr/* Microsoft.PowerShell.Utility
robocopy ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Security Microsoft.PowerShell.Security /s /e
robocopy ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Management Microsoft.PowerShell.Management /s /e
robocopy ../../src/monad/monad/miscfiles/modules/PSDiagnostics PSDiagnostics /s /e
cd ../..

# Publish PowerShell
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN
# Copy files that dotnet-publish does not currently deploy
cd ../..
