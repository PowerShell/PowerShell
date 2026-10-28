<#
    .EXTERNALHELP module-help.xml
#>

class classOne {
    [string] $Property

    classOne() { }
    classOne([string] $argument) { }

    [void] voidMethod() { }
    [void] voidMethod([string] $argument) { }
    [string] returnMethod() { return '' }
    [string] returnMethod([string] $argument) { return '' }
}

class classTwo {
    [string] $Property

    classTwo() { }
    classTwo([string] $argument) { }

    [void] voidMethod() { }
    [void] voidMethod([string] $argument) { }
    [string] returnMethod() { return '' }
    [string] returnMethod([string] $argument) { return '' }
}

function Get-ModuleClass {
    <#
        .SYNOPSIS
            A self-test utility function.
    #>
    param (
        [Parameter(Mandatory)]
        [string]
        $Name
    )

    return $Name -as [Type]
}
