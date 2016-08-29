# first check to see which platform we're on. If we're on windows we should be able
# to be sure whether we're running elevated. If we're on Linux, we can use whoami to
# determine whether we're elevated
Describe "Set-Date" -Tag "CI" {
    BeforeAll {
        Import-Module (join-path $psscriptroot "../../Common/Test.Helpers.psm1")
        $IsElevated = Test-IsElevated
    }
    It "Set-Date should be able to set the date in an elevated context" -Skip:(! $IsElevated) {
        { get-date | set-date } | Should not throw
    }
    It "Set-Date should produce an error in a non-elevated context" -Skip:($IsElevated) {
        { get-date |set-date} | should throw
    }
}
