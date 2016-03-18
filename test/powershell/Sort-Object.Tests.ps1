Describe "Sort-Object" {

    It "should be able to sort object in ascending with using Property switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length } | Should Not Throw
        
        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -Last 1).Length

        $firstLen -lt $lastLen | Should be $true

    }

    It "should be able to sort object in descending with using Descending switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending } | Should Not Throw
        
        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -Last 1).Length

        $firstLen -gt $lastLen | Should be $true
    }
}

