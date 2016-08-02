Describe "Tests for the Import-PowerShellDataFile cmdlet" -Tags "Feature" {

    It "Validates error on a missing path" {

        $foundError = ""
        try
        {
            Import-PowerShellDataFile -Path /SomeMissingDirectory -ErrorAction Stop
        }
        catch
        {
            $foundError = $_.FullyQualifiedErrorId
        }
        
        $foundError | Should be "PathNotFound,Microsoft.PowerShell.Commands.ResolvePathCommand"
    }

    It "Validates error on a directory" {

        $foundError = ""
        try
        {
            Import-PowerShellDataFile ${TESTDRIVE} -ErrorAction Stop
        }
        catch
        {
            $foundError = $_.FullyQualifiedErrorId
        }
        
        $foundError | Should be "CouldNotParseAsPowerShellDataFile,Import-PowerShellDataFile"
    }

    It "Generates a good error on an insecure file" {

        $path = New-TemporaryFile
        Set-Content $path '@{ Foo = Get-Process }'
        
        $foundError = ""
        try
        {
            Import-PowerShellDataFile $path -ErrorAction Stop
        }
        catch
        {
            $foundError = $_.FullyQualifiedErrorId
        }
        finally
        {
            Remove-Item $path
        }
        
        $foundError | Should be "InvalidOperationException,Import-PowerShellDataFile"
    }

    It "Generates a good error on a file that isn't a PowerShell Data File (missing the hashtable root)" {

        $path = New-TemporaryFile
        Set-Content $path '"Hello World"'
        
        $foundError = ""
        try
        {
            Import-PowerShellDataFile $path -ErrorAction Stop
        }
        catch
        {
            $foundError = $_.FullyQualifiedErrorId
        }
        finally
        {
            Remove-Item $path
        }
        
        $foundError | Should be "CouldNotParseAsPowerShellDataFileNoHashtableRoot,Import-PowerShellDataFile"
    }

    It "Can parse a PowerShell Data File (detailed tests are in AST.SafeGetValue tests)" {

        $path = New-TemporaryFile
        Set-Content $path '@{ "Hello" = "World" }'
        
        $result = Import-PowerShellDataFile $path -ErrorAction Stop
        $result.Hello | Should be "World"
    }
    
}
