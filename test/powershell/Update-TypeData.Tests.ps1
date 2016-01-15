Describe "Update-TypeData" {

    Context "Validate Update-Type updates correctly" {

        It "Should not throw upon reloading previous formatting file" {
            { Update-TypeData } | Should Not throw
        }

        It "Should validly load formatting data" {
           { Get-TypeData -typename System.Diagnostics.Process | Export-TypeData -Path "outputfile.ps1xml" }
           { Update-TypeData -prependPath "outputfile.ps1xml" | Should Not throw }
           { Remove-Item "outputfile.ps1xml" -ErrorAction SilentlyContinue }
        }
    }
}
