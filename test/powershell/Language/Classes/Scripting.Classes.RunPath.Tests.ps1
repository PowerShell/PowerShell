Describe "Script with a class definition run path" -Tags "CI" {

    BeforeAll {
        if ( $IsWindows ) {
            $TestFileName = "My,'=~Test.ps1"
        } else {
            $TestFileName = "My,'`"=~Test.ps1"
        }
    }

    $TestCases = @(
        @{ FileName = 'MyTest.ps1'  ; Name = 'typical path' }
        # We had a bug with some symbols in script file names.
        # We fixed the bug but leave the test to exclude regression.
        @{ FileName = $TestFileName; Name = 'path with unhandled assemblyname characters' }
    )

    It "Script with a class definition can run from a <Name>" -TestCases $TestCases {
        param( $FileName )

        $FilePath = Join-Path -Path $TestDrive -ChildPath $FileName

@'
class MyClass { static [string]$MyProperty = 'Some value' }
[MyClass]::MyProperty
'@ | Out-File -FilePath $FilePath

        ( . $FilePath ) | Should Match 'Some value'
    }
}
