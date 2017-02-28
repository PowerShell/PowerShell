
Describe "Tests for the Import-PowerShellDataFile cmdlet" -Tags "CI" {

    It "Validates error on a missing path" {
        { Import-PowerShellDataFile -Path /SomeMissingDirectory -ErrorAction Stop } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Validates error on a directory" {
        { Import-PowerShellDataFile ${TESTDRIVE} -ErrorAction Stop } | ShouldBeErrorId "PathNotFound,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Generates a good error on an insecure file" {
        $path = Setup -f insecure.psd1 -content '@{ Foo = Get-Process }' -pass
        { Import-PowerShellDataFile $path -ErrorAction Stop } | ShouldBeErrorId "System.InvalidOperationException,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Generates a good error on a file that isn't a PowerShell Data File (missing the hashtable root)" {
        $path = setup -f NotAPSDataFile -content '"Hello World"' -Pass
        { Import-PowerShellDataFile $path -ErrorAction Stop } | ShouldBeErrorId "CouldNotParseAsPowerShellDataFileNoHashtableRoot,Microsoft.PowerShell.Commands.ImportPowerShellDataFileCommand"
    }

    It "Can parse a PowerShell Data File (detailed tests are in AST.SafeGetValue tests)" {

        $path = Setup -F gooddatafile -content '@{ "Hello" = "World" }' -pass

        $result = Import-PowerShellDataFile $path -ErrorAction Stop
        $result.Hello | Should be "World"
    }

}
