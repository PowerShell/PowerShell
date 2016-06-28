try {
    $enlistmentRoot = git rev-parse --show-toplevel

    $docLocation = [io.path]::Combine($enlistmentRoot, "docs","cmdlet-example")
    Push-Location $docLocation
    ./SendGreeting.Tests.ps1
} finally {
    Pop-Location
}
