$BIN = "${pwd}/bin"

mkdir $BIN/Modules -ErrorAction SilentlyContinue

# Deploy PowerShell modules
cd $BIN/Modules
robocopy ../../test/Pester Pester /s /e 
robocopy ../../src/monad/monad/miscfiles/modules/Microsoft.PowerShell.Utility Microsoft.PowerShell.Utility /s /e 
cd ../..

# Publish PowerShell
cd src/Microsoft.PowerShell.Linux.Host
dotnet publish --framework dnxcore50 --output $BIN
# Copy files that dotnet-publish does not currently deploy
cp *_profile.ps1 $BIN
cd ../..

# Symlink types and format files to correct names
cd $BIN

cp ../src/monad/monad/miscfiles/types/CoreClr/types.ps1xml .
cp ../src/monad/monad/miscfiles/types/CoreClr/typesv3.ps1xml .

cp ../src/monad/monad/miscfiles/display/Certificate.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/Diagnostics.Format.ps1xml Diagnostics.format.ps1xml
cp ../src/monad/monad/miscfiles/display/DotNetTypes.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/Event.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/FileSystem.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/Help.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/HelpV3.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/PowerShellCore.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/PowerShellTrace.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/Registry.format.ps1xml .
cp ../src/monad/monad/miscfiles/display/WSMan.format.ps1xml .
cd ..
