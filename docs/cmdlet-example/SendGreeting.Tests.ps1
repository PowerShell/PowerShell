Describe "Send-Greeting cmdlet" -Tag 'Slow','CI' {
    It "Should be able build the cmdlet" {
        Remove-Item -Recurse -Force bin -ErrorAction SilentlyContinue
        dotnet restore --verbosity Error | Should BeNullOrEmpty
        dotnet build | ?{ $_ -match "Compiling SendGreeting for .NETStandard,Version=v1.3" } | Should Not BeNullOrEmpty
    }

    It "Should be able to use the module" {
        Import-Module -ErrorAction Stop ./bin/Debug/netstandard1.3/SendGreeting.dll
        Send-Greeting -Name World | Should Be "Hello World!"
        Remove-Module SendGreeting
    }
}
