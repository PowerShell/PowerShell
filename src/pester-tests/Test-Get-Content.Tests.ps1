Describe "Test-Get-Content" {
    It "Should throw an error on a directory  " {
        # also tests that -erroraction SilentlyContinue will work.

        Get-Content . -ErrorAction SilentlyContinue | Should Throw
        cat . -ErrorAction SilentlyContinue         | Should Throw
        gc . -ErrorAction SilentlyContinue          | Should Throw
        type . -ErrorAction SilentlyContinue        | Should Throw

    }

    It "Should deliver an array object when listing a file" {
        (Get-Content -Path ./Test-Get-Content.Tests.ps1).GetType().BaseType.Name | Should Be "Array"
        (Get-Content -Path ./Test-Get-Content.Tests.ps1)[0]                      |Should be "Describe `"Test-Get-Content`" `{"

        (gc -Path ./Test-Get-Content.Tests.ps1).GetType().BaseType.Name | Should Be "Array"
        (gc -Path ./Test-Get-Content.Tests.ps1)[0]                      |Should be "Describe `"Test-Get-Content`" `{"

        (type -Path ./Test-Get-Content.Tests.ps1).GetType().BaseType.Name | Should Be "Array"
        (type -Path ./Test-Get-Content.Tests.ps1)[0]                      |Should be "Describe `"Test-Get-Content`" `{"

        (cat -Path ./Test-Get-Content.Tests.ps1).GetType().BaseType.Name | Should Be "Array"
        (cat -Path ./Test-Get-Content.Tests.ps1)[0]                      |Should be "Describe `"Test-Get-Content`" `{"

    }
}
