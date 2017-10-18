Describe "Windows Installer" -Tags "Scenario" {

    BeforeAll {
        $preRequisitesLink =  'https://aka.ms/pscore6-prereq'
        $linkCheckTestCases = @(
            @{ Name = "Universal C Runtime"; Url = $preRequisitesLink }
            @{ Name = "WMF 4.0"; Url = "https://www.microsoft.com/download/details.aspx?id=40855" }
            @{ Name = "WMF 5.0"; Url = "https://www.microsoft.com/download/details.aspx?id=50395" }
            @{ Name = "WMF 5.1"; Url = "https://www.microsoft.com/download/details.aspx?id=54616" }
        )
    }
        
    It "WiX (Windows Installer XML) file contains pre-requisites link $preRequisitesLink" {
        $wixProductFile = Join-Path -Path $PSScriptRoot -ChildPath "..\..\..\assets\Product.wxs"
        (Get-Content $wixProductFile -Raw).Contains($preRequisitesLink) | Should Be $true
    }

    It "Pre-Requisistes link for '<Name>' is reachable" -TestCases $linkCheckTestCases -Test {
        param ($Url)

        # Because an outdated link 'https://www.microsoft.com/download/details.aspx?id=504100000' would still return a 200 reponse (due to a redirection to an error page), it only checks that it returns something
        (Invoke-WebRequest $Url -UseBasicParsing) | Should Not Be $null
    }
}
