function Test-UnblockFile {
    try {
        Get-Content -Path $testfilepath -Stream Zone.Identifier -ErrorAction Stop | Out-Null
    }
    catch {
        if ($_.FullyQualifiedErrorId -eq "GetContentReaderFileNotFoundError,Microsoft.PowerShell.Commands.GetContentCommand") {
            return $true
        }
    }
    
    return $false
}

Describe "Unblock-File" -Tags "CI" {

    BeforeAll {
        if ( ! $IsWindows )
        {
            $origDefaults = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues['it:skip'] = $true

        } else {
            $testfilepath = Join-Path -Path $TestDrive -ChildPath testunblockfile.ttt
        }
    }

    AfterAll {
        if ( ! $IsWindows ){
            $global:PSDefaultParameterValues = $origDefaults
        }
    }

    BeforeEach {
        if ( $IsWindows ){
            Set-Content -Value "[ZoneTransfer]`r`nZoneId=4" -Path $testfilepath -Stream Zone.Identifier
        }
    }

    It "With '-Path': no file exist" {
        try {
            Unblock-File -Path nofileexist.ttt -ErrorAction Stop
            throw "No Exception!"
        } 
        catch {
            $_.FullyQualifiedErrorId | Should Be "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }
    }

    It "With '-LiteralPath': no file exist" {
        try {
            Unblock-File -LiteralPath nofileexist.ttt -ErrorAction Stop
            throw "No Exception!"
        } 
        catch {
            $_.FullyQualifiedErrorId | Should Be "FileNotFound,Microsoft.PowerShell.Commands.UnblockFileCommand"
        }
    }

    It "With '-Path': file exist" {
        Unblock-File -Path $testfilepath
        Test-UnblockFile | Should Be $true
    }

    It "With '-LiteralPath': file exist" {
        Unblock-File -LiteralPath $testfilepath
        Test-UnblockFile | Should Be $true
    }
}
