try {
    Push-Location $PSScriptRoot/../../docs/cmdlet-example
    ./SendGreeting.Tests.ps1
} finally {
    Pop-Location
}
