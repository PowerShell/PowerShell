$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$sut = (Split-Path -Leaf $MyInvocation.MyCommand.Path).Replace(".Tests.", ".")
. "$here\$sut"

Describe "Test-Get-Content" {
<#
    Dependencies:
    1. mkdir
#>
    It "Should throw an error on a directory  " {
        # also tests that -erroraction SilentlyContinue will work.

        Get-Content $HOME -ErrorAction SilentlyContinue| Should Throw
        cat $HOME -ErrorAction SilentlyContinue| Should Throw
        gc $HOME -ErrorAction SilentlyContinue| Should Throw
        

    }

    It "Should deliver an array object when listing a file" {
        (Get-Content -Path .\Test-Get-Content.Tests.ps1).GetType().BaseType.Name | Should Be "Array"
        (Get-Content -Path .\Test-Get-Content.Tests.ps1)[0] |Should be "`$here = Split-Path -Parent `$MyInvocation.MyCommand.Path"
    }

    It "Should support pipelines" {
        
    }
}
