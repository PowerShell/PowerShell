Describe "Unimplemented Management Cmdlet Tests" -Tags "CI" {

    $Commands = @(
        "Get-Service",
        "Stop-Service",
        "Start-Service",
        "Suspend-Service",
        "Resume-Service",
        "Restart-Service",
        "Set-Service",
        "New-Service",

        "Restart-Computer",
        "Stop-Computer",
        "Rename-Computer",

        "Get-ComputerInfo",

        "Test-Connection",

        "Get-TimeZone",
        "Set-TimeZone"
    )

    foreach ($Command in $Commands) {
        It "$Command should only be available on Windows" {
            [bool](Get-Command $Command -ErrorAction SilentlyContinue) | Should Be $IsWindows
        }
    }
}
