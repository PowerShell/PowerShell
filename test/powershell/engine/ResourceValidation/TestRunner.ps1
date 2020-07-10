# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
function Test-ResourceStrings
{
    param ( $AssemblyName, $ExcludeList )

    # determine the needed resource directory. If these tests are moved
    # this logic will need to change
    $repoBase = (Resolve-Path (Join-Path $PSScriptRoot ../../../..)).Path
    $asmBase = Join-Path $repoBase "src/$AssemblyName"
    $resourceDir = Join-Path $asmBase resources
    $resourceFiles = Get-ChildItem $resourceDir -Filter *.resx -ErrorAction stop |
        Where-Object { $excludeList -notcontains $_.Name }

    $bindingFlags = [reflection.bindingflags]"NonPublic,Static"

    # the resource generation is based on the file name of the .resx file
    # and is not in a namespace. We can find all of these classes in the
    # ${AssemblyName} assembly
    $ASSEMBLY = [appdomain]::CurrentDomain.GetAssemblies()|
        Where-Object { $_.FullName -match "$AssemblyName" }

    # Validate that the binary you're running has the proper message
    # based on the resource files. It is possible that -ResGen could be
    # skipped and the messages might become out of sync with what is in
    # source
    #
    # you could argue that we should be generating conditions where the message is delivered
    # and then test for that, but that is a check against an ErrorId while this is ensuring
    # that the contents of the built binary matches the resx content
    #
    # Normally you don't want to use reflection to get at the implementation details
    # but this tests generated code, so changes there will be consistent across
    # all the resource classes we generate
    #
    # This is the reason why this is not a general module for use. There is
    # no other way to run these tests
    Describe "Resources strings in $AssemblyName (was -ResGen used with Start-PSBuild)" -Tag Feature {

        function NormalizeLineEnd
        {
            param (
                [string] $string
            )

            $string -replace "`r`n", "`n"
        }

        foreach ( $resourceFile in $resourceFiles )
        {
            # in the event that the id has a space in it, it is replaced with a '_'
            $classname = $resourcefile.Name -replace ".resx"
            It "'$classname' should be an available type and the strings should be correct" -Skip:(!$IsWindows) {
                # get the type from the assembly
                $resourceType = $ASSEMBLY.GetType($classname, $false, $true)
                # the properties themselves are static internals, so we need
                # to using the appropriate bindingflags
                $resourceType | Should -Not -BeNullOrEmpty

                # check all the resource strings
                $xmlData = [xml](Get-Content $resourceFile.Fullname)
                foreach ( $inResource in $xmlData.root.data ) {
                    $resourceStringToCheck = $resourceType.GetProperty($inResource.name,$bindingFlags).GetValue(0)
                    NormalizeLineEnd($resourceStringToCheck) | Should -Be (NormalizeLineEnd($inresource.value))
                }
            }
        }
    }
}
