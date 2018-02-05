Describe "PowerShell Standard reference assembly can create a usable module" -tag Scenario {
    It "The reference assembly can be compiled" {
        try {
            push-location ../Reference
            # don't separate the following 2 lines
            dotnet build
            $? | Should Be $true
            "${PWD}/bin/Debug/netstandard2.0/System.Management.Automation.dll" | Should Exist
        }
        finally {
            pop-location
        }
    }
    It "The demo module can be compiled using the reference assembly" {
        # don't separate the following 2 lines
        dotnet build
        $? | should be $true
        "${PWD}/bin/Debug/netstandard2.0/Demo.Cmdlet.dll" | Should Exist
    }
    It "The demo module can be loaded and executed" {
        $result = pwsh -c "import-module ${PWD}/bin/Debug/netstandard2.0/Demo.Cmdlet.dll; get-thing"
        $result | should match "Success!"
    }
}
