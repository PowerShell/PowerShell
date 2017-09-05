$thisTestFolder = Split-Path $MyInvocation.MyCommand.Path -Parent
$wixProductFile = Join-Path $thisTestFolder "..\..\..\assets\Product.wxs"

Describe "Windows Installer" -Tags "Scenario" {

    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( ! $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    $universalCRuntimeDownloadLink = 'https://www.microsoft.com/download/details.aspx?id=50410'
    $visualStudioCPlusPlusRedistributablesDownloadLink = 'https://www.microsoft.com/download/details.aspx?id=48145'

    $downloadLinks = @(
        @{ downloadLink = $universalCRuntimeDownloadLink; Name = 'Universal C Runtime' }
        @{ downloadLink = $visualStudioCPlusPlusRedistributablesDownloadLink; Name = 'Visual Studio C++ 2015 Redistributables' }
    )
        
    It "WiX (Windows Installer XML) file contains download link '<downloadLink>' for '<Name>'" -TestCases $downloadLinks -Test {
        Param ([string]$downloadLink)
        (Get-Content $wixProductFile -Raw).Contains($downloadLink) | Should Be $true
    }

    It "Download link '<downloadLink>' for '<Name>' is reachable" -TestCases $downloadLinks -Test {
        Param ([string]$downloadLink)
        # Because an outdated link 'https://www.microsoft.com/download/details.aspx?id=504100000' would still return a 200 reponse (due to a redirection to an error page), it only checks that it returns something
        (Invoke-WebRequest $universalCRuntimeDownloadLink -UseBasicParsing) | Should Not Be $null
    }
	
}
