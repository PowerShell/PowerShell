return
$classModulePath = Join-Path $pwd "ClassHelpMod"
$destinationPath = "$pshome\modules"

## Add the test resource to modulepath
Copy-Item -Recurse -Path $classModulePath $destinationPath -Force -ErrorAction SilentlyContinue

$classHelp = Get-Help "ClassHelpClass"
$classHelpWithWildCard = Get-Help "ClassHelpClas*"
$classHelpWithCategory = Get-Help -category Class
$classHelpWithIncompleteName = Get-Help "ClassHelpCl"

Describe "Class MAML help tests" -Tags "DRT" {
    
    It "is found" {

         $classHelp -ne $null | Should Be $true
    }

    It "is found with wildcard name" {

        $classHelpWithWildCard -ne $null | Should Be $true
    }

    It "is found with incomplete name" {
        $classHelpWithIncompleteName | Should Not BeNullOrEmpty
    }

    It "is found with category Class" {

        $helpContent = $classHelpWithCategory | ? Name -eq 'ClassHelpClass' 
        $helpContent -ne $null | Should Be $true
    }
    
    
    It "has fields" {
        $classHelp.members.member | Should Not Be $null
    }

    It "has methods" {
        $foundMethod = $false
        $methodCount = 0

        foreach($member in $classHelp.members.member)
        {
            if($member.type -eq "method")
            {
                $foundMethod = $true
                $methodCount++
            } 
        }
        
        $foundMethod | Should Be $true
        $methodCount | Should Be 2
    }

	<#
    It "has a synopsis" {

       # $classHelp.synopsis | Should Be "This is synopsis."
    }

	#>

    It "has a name" {

        $classHelp.name | Should Be "ClassHelpClass"        
    }

    It "has examples" {

        $classHelp.examples.example.code.length | Should Be 78
    }

    It "has related links" {

        $classHelp.relatedlinks.navigationLink.linkText | Should Be 'Online version:'
        $classHelp.relatedlinks.navigationLink.uri | Should Be 'http://go.microsoft.com/fwlink/?LinkID=138337'
    }
}

## Remove the test resource from modulepath
Remove-Item (Join-Path $destinationPath "ClassHelpMod") -Force -Recurse -ErrorAction SilentlyContinue
