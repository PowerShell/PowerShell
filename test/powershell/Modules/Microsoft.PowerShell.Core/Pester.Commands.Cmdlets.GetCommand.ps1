##
## Copyright (c) Microsoft Corporation, 2015
##

Describe "Tests Get-Command with relative paths and wildcards" {

    BeforeAll {
        # Create temporary EXE command files
        $null = New-Item -ItemType File -Path (Join-Path $env:Temp WildCardCommandA.exe) -ErrorAction Ignore
        $null = New-Item -ItemType File -Path (Join-Path $env:Temp WildCardCommand[B].exe) -ErrorAction Ignore
    }

    It "Test wildcard with drive relative directory path" {
        $pathName = Join-Path $env:Temp "WildCardCommandA*"
        $pathName = $pathName.Substring(2, ($pathName.Length - 2))
        $result = Get-Command -Name $pathName
        $result | Should Not Be $null
        $result.Name | Should Be WildCardCommandA.exe
    }

    It "Test wildcard with relative directory path" {
        pushd $env:Temp
        $result = Get-Command -Name .\WildCardCommandA*
        popd
        $result | Should Not Be $null
        $result | Should Be WildCardCommandA.exe
    }
    
    It "Test with PowerShell wildcard and reative path" {
        pushd $env:Temp

        # This should use the wildcard to find WildCardCommandA.exe
        $result = Get-Command -Name .\WildCardCommand[A].exe
        $result | Should Not Be $null
        $result | Should Be WildCardCommandA.exe

        # This should find the file WildCardCommand[B].exe
        $result = Get-Command -Name .\WildCardCommand[B].exe
        $result | Should Not Be $null
        $result | Should Be WildCardCommand[B].exe

        popd
    }

    AfterAll {
        # Remove temporary files
        Remove-Item -Path (Join-Path $env:Temp WildCardCommand*) -Force -ErrorAction Ignore
    }
}
