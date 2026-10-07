# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
function Test-ResourceStrings
{
    param (
        [Parameter(Mandatory = $true)]
        [string] $AssemblyName,

        [string[]] $ExcludeList = @()
    )

    # determine the needed resource directory. If these tests are moved
    # this logic will need to change
    $repoBase = (Resolve-Path (Join-Path $PSScriptRoot ../../../..)).Path
    $asmBase = Join-Path $repoBase "src/$AssemblyName"
    $resourceDir = Join-Path $asmBase resources
    $resourceFiles = Get-ChildItem $resourceDir -Filter *.resx -ErrorAction stop |
        Where-Object { $excludeList -notcontains $_.Name }

    # Build test cases for -ForEach. Each case carries everything the It needs,
    # because Pester 5 doesn't propagate file/function-scope variables into
    # runtime blocks (BeforeAll/It). Setting $script:foo in this function does
    # NOT make it visible inside BeforeAll either.
    $testCases = $resourceFiles | ForEach-Object {
        @{
            ClassName    = ($_.Name -replace "\.resx$")
            FilePath     = $_.FullName
            AssemblyName = $AssemblyName
        }
    }

    Describe "Resources strings in $AssemblyName (was -ResGen used with Start-PSBuild)" -Tag Feature {

        It "'<ClassName>' should be an available type and the strings should be correct" -Skip:(!$IsWindows) -ForEach $testCases {
            $bindingFlags = [reflection.bindingflags]"NonPublic,Static"
            $ASSEMBLY = [appdomain]::CurrentDomain.GetAssemblies() |
                Where-Object { $_.GetName().Name -eq $AssemblyName }

            # get the type from the assembly
            $resourceType = $ASSEMBLY.GetType($ClassName, $false, $true)
            $resourceType | Should -Not -BeNullOrEmpty

            # check all the resource strings
            $xmlData = [xml](Get-Content $FilePath)
            foreach ( $inResource in $xmlData.root.data ) {
                $resourceStringToCheck = $resourceType.GetProperty($inResource.name,$bindingFlags).GetValue(0)
                ($resourceStringToCheck -replace "`r`n", "`n") | Should -Be ($inresource.value -replace "`r`n", "`n")
            }
        }
    }
}
