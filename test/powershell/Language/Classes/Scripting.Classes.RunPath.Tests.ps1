Describe "Script with a class definition run path" -Tags "CI" {

    if ( $IsLinux -or $IsOSX )
    {
        $UnhandledName = "My,'`"=~Test.ps1"
    }
    else
    {
        $UnhandledName = "My,'=~Test.ps1"
    }

    $TestCases = @(
        @{ FileName = 'MyTest.ps1'  ; Name = 'typical path' }
        @{ FileName = $UnhandledName; Name = 'path with unhandled assemblyname characters' }
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
