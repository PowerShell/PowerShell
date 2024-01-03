<#
    .EXTERNALHELP module-help.xml
#>

class classOne {
    <#
        .DESCRIPTION
            ClassOne: class description.
    #>

    <#
        ClassOne: Property description.
    #>
    [string] $Property

    <#
        ClassOne: Constructor description.
    #>
    classOne() { }

    <#
        ClassOne: Constructor with argument description.
    #>
    classOne([string] $argument) { }

    <#
        ClassOne: Void method description.
    #>
    [void] voidMethod() { }

    <#
        ClassOne: Void method with argument description.
    #>
    [void] voidMethod([string] $argument) { }

    <#
        ClassOne: String method description.
    #>
    [string] returnMethod() { return '' }

    <#
        ClassOne: String method with argument description.
    #>
    [string] returnMethod([string] $argument) { return '' }
}

class classTwo {
    <#
        .DESCRIPTION
            classTwo: class description.
    #>

    <#
        classTwo: Property description.
    #>
    [string] $Property

    <#
        classTwo: Constructor description.
    #>
    classTwo() { }

    <#
        classTwo: Constructor with argument description.
    #>
    classTwo([string] $argument) { }

    <#
        classTwo: Void method description.
    #>
    [void] voidMethod() { }

    <#
        classTwo: Void method with argument description.
    #>
    [void] voidMethod([string] $argument) { }

    <#
        classTwo: String method description.
    #>
    [string] returnMethod() { return '' }

    <#
        classTwo: String method with argument description.
    #>
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
