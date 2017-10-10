Describe "Windows Installer" -Tags "Scenario" {

    $preRequisitesLink =  'https://aka.ms/pscore6-prereq'
        
    It "WiX (Windows Installer XML) file contains pre-requisites link $preRequisitesLink" {
        $wixProductFile = Join-Path -Path $PSScriptRoot -ChildPath "..\..\..\assets\Product.wxs"
        (Get-Content $wixProductFile -Raw).Contains($preRequisitesLink) | Should Be $true
    }

    It "Pre-Requisistes link $preRequisitesLink is reachable" -TestCases $downloadLinks -Test {
        # Because an outdated link 'https://www.microsoft.com/download/details.aspx?id=504100000' would still return a 200 reponse (due to a redirection to an error page), it only checks that it returns something
        (Invoke-WebRequest $preRequisitesLink -UseBasicParsing) | Should Not Be $null
    }
}
