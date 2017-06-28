Describe "Script with a class definition run path" {

    It "Script with a class definition can run from a path without a comma" -Tags "CI" {

        $FilePath = '.\MyTest.ps1'

        try {
            'class MyClass { [string]$MyProperty }; $True' | Out-File -FilePath $FilePath
            $Success = . $FilePath
        }

        catch {
            $Success = $False
        }

        finally {
            Remove-Item -Path $FilePath
        }

        $Success | Should Be $True
    }

    It "Script with a class definition can run from a path with a comma" -Tags "CI" {

        $FilePath = '.\My,Test.ps1'

        try {
            'class MyClass { [string]$MyProperty }; $True' | Out-File -FilePath $FilePath
            $Success = . $FilePath
        }

        catch {
            $Success = $False
        }

        finally {
            Remove-Item -Path $FilePath
        }

        $Success | Should Be $True
    }
}
