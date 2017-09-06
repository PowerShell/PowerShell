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

    $preRequisitesLink =  'https://github.com/PowerShell/PowerShell/blob/master/docs/installation/windows.md#prerequisites'
        
    It "WiX (Windows Installer XML) file contains pre-requisites link $preRequisitesLink" {
        (Get-Content $wixProductFile -Raw).Contains($preRequisitesLink) | Should Be $true
    }

    It "Pre-Requisistes link $preRequisitesLink is reachable" -TestCases $downloadLinks -Test {
        # Because an outdated link 'https://www.microsoft.com/download/details.aspx?id=504100000' would still return a 200 reponse (due to a redirection to an error page), it only checks that it returns something
        (Invoke-WebRequest $preRequisitesLink -UseBasicParsing) | Should Not Be $null
    }
	
}
