Describe "Tests for the Import-PowerShellDataFile cmdlet" -Tags "CI" {

    It "Validates error on a missing path" {

        try
        {
            Import-PowerShellDataFile -Path /SomeMissingDirectory -ErrorAction Stop
            Throw "Command did not throw exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
        }
    }

    It "Validates error on a directory" {

        try
        {
            Import-PowerShellDataFile ${TESTDRIVE} -ErrorAction Stop
            Throw "Command did not throw exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "CouldNotParseAsPowerShellDataFile,Import-PowerShellDataFile"
        }
        
    }

    It "Generates a good error on an insecure file" {

        $path = Setup -f insecure.psd1 -content '@{ Foo = Get-Process }' -pass
        try
        {
            Import-PowerShellDataFile $path -ErrorAction Stop
            Throw "Command did not throw exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "InvalidOperationException,Import-PowerShellDataFile"
        }
    }

    It "Generates a good error on a file that isn't a PowerShell Data File (missing the hashtable root)" {

        $path = setup -f NotAPSDataFile -content '"Hello World"' -Pass
        try
        {
            Import-PowerShellDataFile $path -ErrorAction Stop 
            Throw "Command did not throw exception"
        }
        catch
        {
            $_.FullyQualifiedErrorId | Should be "CouldNotParseAsPowerShellDataFileNoHashtableRoot,Import-PowerShellDataFile"
        }
    }

    It "Can parse a PowerShell Data File (detailed tests are in AST.SafeGetValue tests)" {

        $path = Setup -F gooddatafile -content '@{ "Hello" = "World" }' -pass
        
        $result = Import-PowerShellDataFile $path -ErrorAction Stop
        $result.Hello | Should be "World"
    }
    
}
