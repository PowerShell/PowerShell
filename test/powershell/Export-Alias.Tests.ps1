$here = Split-Path -Parent $MyInvocation.MyCommand.Path
. (Join-Path -Path $here -ChildPath Test-Common.ps1)

Describe "Export-Alias" {
    $testAliasDirectory = Join-Path -Path $(GetTempDir) -ChildPath ExportAliasTestDirectory
    $testAliases        = "TestAliases"
    $fulltestpath       = Join-Path -Path $($testAliasDirectory) -ChildPath $($testAliases)

    BeforeEach {
        New-Item -Path $testAliasDirectory -ItemType Directory -Force
    }

    It "Should be able to create a file in the specified location"{
        Export-Alias $fulltestpath

        Test-Path $fulltestpath | Should be $true
    }

    It "Should create a file with the list of aliases that match the expected list" {
        Export-Alias $fulltestpath

        Test-Path $fulltestpath | Should Be $true

        $actual   = Get-Content $fulltestpath | Sort
        $expected = $(Get-Command -CommandType Alias)

        for ( $i=0; $i -lt $expected.Length; $i++)
        {
            # We loop through the expected list and not the other because the output writes some comments to the file.
            $expected[$i] | Should Match $actual[$i].Name
        }

    }

    Remove-Item -Path $testAliasDirectory -Recurse -Force
}
