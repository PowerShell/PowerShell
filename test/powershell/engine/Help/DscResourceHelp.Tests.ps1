
## Disabling tests due to Bug # 3141590.

<#
$fakeModulePath = Join-Path $pwd "NewDscResource"
$destinationPath = "$pshome\modules"

## Add the test resource to modulepath
Copy-Item -Recurse -Path $fakeModulePath $destinationPath -Force -ErrorAction SilentlyContinue

Import-Module NewDscResource -Force

$resourceHelp = Get-Help "FakeDscResource"
$resourceHelpWithWildCard = Get-Help "FakeDscResour*"
$resourceHelpWithCategory = Get-Help -category DscResource

Describe "Dsc Resource MAML help tests" -Tags "DRT" {
    
    It "is found" -Skip {

         $resourceHelp -ne $null | Should Be $true
    }

    It "is found with wildcard name" -Skip {

        $resourceHelpWithWildCard -ne $null | Should Be $true
    }

    It "is found with category DscResource" -Skip {

        $fakeDscResource = $resourceHelpWithCategory | ? Name -eq 'FakeDscResource' 
        $fakeDscResource -ne $null | Should Be $true
    }    
    
    It "has properties"  -Skip {

        $resourceHelp.properties.parameter[0].name | Should Be "ID"
		$resourceHelp.properties.parameter[0].parameterValue | Should Be "string"

        $resourceHelp.properties.parameter[1].name | Should Be "Name"
		$resourceHelp.properties.parameter[1].parameterValue | Should Be "string"
    }

    It "has a synopsis" -Skip {

        $resourceHelp.synopsis | Should Be "The synopsis for FakeDscResource."
    }

    It "has a name" -Skip {

        $resourceHelp.name | Should Be "FakeDscResource"        
    }

    It "has examples" -Skip {

        $resourceHelp.examples.example.code.length | Should Be 324
    }

    It "has related links" -Skip {

        $resourceHelp.relatedlinks.navigationLink.linkText | Should Be 'Online version:'
        $resourceHelp.relatedlinks.navigationLink.uri | Should Be 'http://go.microsoft.com/fwlink/?LinkID=138337'
    }

    # Blocked by Bug # 2313498 (Threshold)

    #It "can be update help content using Update-Help" {
        
    #    Update-Help NewDscResource -SourcePath $pwd -Force

    #    $helpContent = Get-Help FakeDscResource
    #    $helpContent.synopsis | Should Be "Updated synopsis for FakeDscResource."
    #}
}

## Remove the test resource from modulepath
Remove-Item (Join-Path $destinationPath "NewDscResource") -Force -Recurse -ErrorAction SilentlyContinue
#>
