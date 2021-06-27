# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Windows Installer" -Tags "Scenario" {

    BeforeAll {
        $skipTest = -not $IsWindows
        $preRequisitesLink =  'https://aka.ms/pscore6-prereq'
        $linkCheckTestCases = @(
            @{ Name = "Universal C Runtime"; Url = $preRequisitesLink }
            @{ Name = "WMF 4.0"; Url = "https://www.microsoft.com/download/details.aspx?id=40855" }
            @{ Name = "WMF 5.0"; Url = "https://www.microsoft.com/download/details.aspx?id=50395" }
            @{ Name = "WMF 5.1"; Url = "https://www.microsoft.com/download/details.aspx?id=54616" }
        )
    }

    It "WiX (Windows Installer XML) file contains pre-requisites link $preRequisitesLink" -Skip:$skipTest {
        $wixProductFile = Join-Path -Path $PSScriptRoot -ChildPath "..\..\..\assets\wix\Product.wxs"
        (Get-Content $wixProductFile -Raw).Contains($preRequisitesLink) | Should -BeTrue
    }

    ## Running 'Invoke-WebRequest' with WMF download URLs has been failing intermittently,
    ## because sometimes the URLs lead to a 'this download is no longer available' page.
    ## We use a retry logic here. Retry for 5 times with 1 second interval.
    # It "Pre-Requisistes link for '<Name>' is reachable: <url>" -TestCases $linkCheckTestCases -Skip:$skipTest {
    It "Pre-Requisistes link for '<Name>' is reachable: <url>" -TestCases $linkCheckTestCases -Pending {
        param ($Url)

        foreach ($i in 1..5) {
            try {
                $result = Invoke-WebRequest $Url -UseBasicParsing
                break;
            } catch {
                Start-Sleep -Seconds 1
            }
        }

        $result | Should -Not -Be $null
    }
}
