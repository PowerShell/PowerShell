ln -s ../share/powershell/bin/powershell powershell
fpm --force --verbose \
    --name 'powershell' \
    --version '0.1.0' \
    --iteration '1' \
    --maintainer 'Andrew Schwartzmeyer <andschwa@microsoft.com>' \
    --vendor 'Microsoft <mageng@microsoft.com>' \
    --url 'https://github.com/PowerShell/PowerShell' \
    --license 'Unlicensed' \
    --description 'Open PowerShell on .NET Core\nPowerShell is an open-source, cross-platform, scripting language and rich object shell. Built upon .NET Core, it is also a C# REPL.\n' \
    --category 'shells' \
    --depends 'libunwind8' \
    --depends 'libicu52' \
    --deb-build-depends 'dotnet' \
    --deb-build-depends 'cmake' \
    --deb-build-depends 'g++' \
    -t deb \
    -s dir \
    --prefix '/usr/local/share/powershell' \
    -- 'bin/' 'powershell=/usr/local/bin'
rm powershell
