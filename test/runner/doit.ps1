dotnet publish -o runner
if ( $? ) {
    try {
        push-location runner
        ./PSxUnitRunner -assem ./powershell-tests.dll
    }
    finally {
        pop-location
    }
}
