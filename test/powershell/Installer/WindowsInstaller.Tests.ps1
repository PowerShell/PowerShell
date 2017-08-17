$thisTestFolder = Split-Path $MyInvocation.MyCommand.Path -Parent
$wixProductFile = Join-Path $thisTestFolder "..\..\..\assets\Product.wxs"

Describe "Windows Installer" -Tags "Scenario" {

    Context "Universal C Runtime Download Link" {
        $universalCRuntimeDownloadLink = 'https://www.microsoft.com/download/details.aspx?id=50410'
        It "Wix file should have download link about Universal C runtime" {
           (Get-Content $wixProductFile -Raw).Contains($universalCRuntimeDownloadLink) | Should Be $true
        }
        It "Should have download link about Universal C runtime that is reachable" {
           (Invoke-WebRequest $universalCRuntimeDownloadLink.Replace("https://",'http://')) | Should Not Be $null
        }
    }

    Context "Visual Studio C++ Redistributables Link" {
        $visualStudioCPlusPlusRedistributablesDownloadLink = 'https://www.microsoft.com/download/details.aspx?id=48145'
        It "WiX file should have documentation about Visual Studio C++ redistributables" {
           (Get-Content $wixProductFile -Raw).Contains($visualStudioCPlusPlusRedistributablesDownloadLink) | Should Be $true
        }
        It "Should have download link about Universal C runtime that is reachable" {
           (Invoke-WebRequest $visualStudioCPlusPlusRedistributablesDownloadLink.Replace("https://",'http://')) | Should Not Be $null
        }
    }

}