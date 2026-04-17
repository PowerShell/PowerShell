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

    # Build test cases for -ForEach (Pester 5 doesn't capture foreach loop variables in It blocks)
    $testCases = $resourceFiles | ForEach-Object {
        @{ ClassName = ($_.Name -replace "\.resx$"); FilePath = $_.FullName }
    }

    # Store in script scope so BeforeAll (which runs in an isolated scope) can access it
    $script:_TestResourceAssemblyName = $AssemblyName

    Describe "Resources strings in $AssemblyName (was -ResGen used with Start-PSBuild)" -Tag Feature {

        BeforeAll {
            $bindingFlags = [reflection.bindingflags]"NonPublic,Static"
            $ASSEMBLY = [appdomain]::CurrentDomain.GetAssemblies()|
                Where-Object { $_.GetName().Name -eq $script:_TestResourceAssemblyName }
            $repoBase = (Resolve-Path (Join-Path $PSScriptRoot ../../../..)).Path
            $asmBase = Join-Path $repoBase "src/$script:_TestResourceAssemblyName"
            $resourceDir = Join-Path $asmBase resources

            function NormalizeLineEnd
            {
                param (
                    [string] $string
                )

                $string -replace "`r`n", "`n"
            }
        }

        AfterAll {
            Remove-Variable -Name '_TestResourceAssemblyName' -Scope Script -ErrorAction SilentlyContinue
        }

        It "'<ClassName>' should be an available type and the strings should be correct" -Skip:(!$IsWindows) -ForEach $testCases {
            # get the type from the assembly
            $resourceType = $ASSEMBLY.GetType($ClassName, $false, $true)
            $resourceType | Should -Not -BeNullOrEmpty

            # check all the resource strings
            $xmlData = [xml](Get-Content $FilePath)
            foreach ( $inResource in $xmlData.root.data ) {
                $resourceStringToCheck = $resourceType.GetProperty($inResource.name,$bindingFlags).GetValue(0)
                NormalizeLineEnd($resourceStringToCheck) | Should -Be (NormalizeLineEnd($inresource.value))
            }
        }
    }
}
