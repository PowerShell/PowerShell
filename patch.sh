#!/usr/bin/env bash

dotnet restore
echo Patching System.Console.dll
curl -fLo \
     ~/.dnx/packages/System.Console/4.0.0-rc2-23616/ref/dotnet5.4/System.Console.dll \
     'https://github.com/andschwa/corefx/raw/cc1ad3b33318c44d6c615d449b2db7b1715e8e60/System.Console.dll'
