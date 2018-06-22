# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

# Take a hashtable and return two copies,
# one with the key/value pair, the other without
function PermuteHashtableOnProperty
{
    param([hashtable[]]$Hashtable, [string]$Key, [object]$Value)

    foreach ($ht in $Hashtable)
    {
        $ht.Clone()
    }

    foreach ($ht in $Hashtable)
    {
        $ht2 = $ht.Clone()
        $ht2.$Key = $Value
        $ht2
    }
}

# Take a base hashtable and produce all possible
# combinations of that hashtable with the keys
# and values in the key/value hashtable
function PermuteHashtable
{
    param([hashtable]$InitialTable, [hashtable]$KeyValues)

    $l = $InitialTable
    foreach ($key in $KeyValues.Keys)
    {
        $l = PermuteHashtableOnProperty -Hashtable $l -Key $key -Value $KeyValues[$key]
    }

    $l
}

$guid = [guid]::NewGuid()

$modSpecRequired = @{
    ModuleName = "ModSpecRequired"
    RequiredVersion = "3.0.2"
}

$requiredOptionalConstraints = @{ Guid = $guid }

$modSpecRange = @{
    ModuleName = "ModSpecRequired"
    ModuleVersion = "3.0.1"
}

$rangeOptionalConstraints = @{ MaximumVersion = "3.2.0"; Guid = $guid }

Describe "ModuleSpecification objects and logic" -Tag "CI" {

    BeforeAll {
        $testCases = [System.Collections.Generic.List[hashtable]]::new()

        foreach ($case in (PermuteHashtable -InitialTable $modSpecRequired -KeyValues $requiredOptionalConstraints))
        {
            $testCases.Add(@{
                ModuleSpecification = $case
                Keys = ($case.Keys -join ",")
            })
        }

        foreach ($case in (PermuteHashtable -InitialTable $modSpecRange -KeyValues $rangeOptionalConstraints))
        {
            $testCases.Add(@{
                ModuleSpecification = $case
                Keys = ($case.Keys -join ",")
            })
        }

        $testCases = $testCases.ToArray()
    }

    Context "ModuleSpecification construction and string parsing" {

        BeforeAll {
            $differentFieldCases = @(
                @{
                    TestName = "Guid"
                    ModSpec1 = @{ Guid = [guid]::NewGuid(); ModuleName = "TestModule"; ModuleVersion = "1.0" }
                    ModSpec2 = @{ Guid = [guid]::NewGuid(); ModuleName = "TestModule"; ModuleVersion = "1.0" }
                },
                @{
                    TestName = "RequiredVersion"
                    ModSpec1 = @{ ModuleName = "Module"; RequiredVersion = "3.0" }
                    ModSpec2 = @{ ModuleName = "Module"; RequiredVersion = "3.1" }
                },
                @{
                    TestName = "Version/MaxVersion-present"
                    ModSpec1 = @{ ModuleName = "ThirdModule"; ModuleVersion = "2.1" }
                    ModSpec2 = @{ ModuleName = "ThirdModule"; MaximumVersion = "2.1" }
                },
                @{
                    TestName = "RequiredVersion/Version-present"
                    ModSpec1 = @{ ModuleName = "FourthModule"; RequiredVersion = "3.0" }
                    ModSpec2 = @{ ModuleName = "FourthModule"; ModuleVersion = "3.0" }
                }
            )
        }

        It "Can be created from a name" {
            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new("NamedModule")
            $ms | Should -Not -BeNull
            $ms.Name | Should -BeExactly "NamedModule"
        }

        It "Can be created from Hashtable with keys: <Keys>" -TestCases $testCases {
            param([hashtable]$ModuleSpecification, [string]$Keys)
            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)

            $ms.Name | Should -BeExactly $ModuleSpecification.ModuleName

            if ($ModuleSpecification.Guid)
            {
                $ms.Guid | Should -Be $ModuleSpecification.Guid
            }

            if ($ModuleSpecification.ModuleVersion)
            {
                $ms.Version | Should -Be $ModuleSpecification.ModuleVersion
            }

            if ($ModuleSpecification.RequiredVersion)
            {
                $ms.RequiredVersion | Should -Be $ModuleSpecification.RequiredVersion
            }

            if ($ModuleSpecification.MaximumVersion)
            {
                $ms.MaximumVersion | Should -Be $ModuleSpecification.MaximumVersion
            }
        }

        It "Can be reconstructed from self.ToString() with keys: <Keys>" -TestCases $testCases {
            param([hashtable]$ModuleSpecification, [string]$Keys)

            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)

            [Microsoft.PowerShell.Commands.ModuleSpecification]$clone = $null
            [Microsoft.PowerShell.Commands.ModuleSpecification]::TryParse(($ms.ToString()), [ref]$clone) | Should -BeTrue

            $clone.Name | Should -Be $ModuleSpecification.ModuleName

            if ($ModuleSpecification.RequiredVersion)
            {
                $clone.RequiredVersion | Should -Be $ModuleSpecification.RequiredVersion
            }

            if ($ModuleSpecification.Version)
            {
                $clone.Version | Should -Be $ModuleSpecification.Version
            }

            if ($ModuleSpecification.MaximumVersion)
            {
                $clone.MaximumVersion | Should -Be $ModuleSpecification.MaximumVersion
            }

            if ($ModuleSpecification.Guid)
            {
                $clone.Guid | Should -Be $ModuleSpecification.Guid
            }
        }
    }

    Context "ModuleSpecification comparison" {

        BeforeAll {
            $modSpecAsm = [Microsoft.PowerShell.Commands.ModuleSpecification].Assembly
            $modSpecComparerType = $modSpecAsm.GetType("Microsoft.PowerShell.Commands.ModuleSpecificationComparer")
            $comparer = [System.Activator]::CreateInstance($modSpecComparerType)
        }

        It "Module specifications with same fields <Keys> are equal" -TestCases $testCases {
            param([hashtable]$ModuleSpecification, [string]$Keys)

            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)
            $ms2 = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)

            $comparer.Equals($ms, $ms2) | Should -BeTrue
        }

        It "Module specifications with same fields <Keys> have the same hash code" -TestCases $testCases {
            param([hashtable]$ModuleSpecification, [string]$Keys)

            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)
            $ms2 = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification)

            $comparer.GetHashCode($ms) | Should -Be $comparer.GetHashCode($ms2)
        }

        It "Module specifications with different <TestName> fields are not equal" -TestCases $differentFieldCases {
            param($TestName, $ModSpec1, $ModSpec2)
            $ms1 = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModSpec1)
            $ms2 = [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModSpec2)

            $comparer.Equals($ms1, $ms2) | Should -BeFalse
        }

        It "Compares two null module specifications as equal" {
            $comparer.Equals($null, $null) | Should -BeTrue
        }

        It "Compares a null module specification with another as unequal" {
            $ms = [Microsoft.PowerShell.Commands.ModuleSpecification]::new(@{
                MOduleName = "NonNullModule"
                Guid = [guid]::NewGuid()
                RequiredVersion = "3.2.1"
            })

            $comparer.Equals($ms, $null) | Should -BeFalse
        }

        It "Succeeds to get a hash code from a null module specification" {
            $comparer.GetHashCode($null) | Should -Not -BeNull
        }
    }

    Context "Invalid ModuleSpecification initialization" {
        BeforeAll {
            $testCases = @(
                @{
                    TestName = "Version+RequiredVersion"
                    ModuleSpecification = @{ Name = "BadVersionModule"; ModuleVersion = "3.1"; RequiredVersion = "3.1" }
                },
                @{
                    TestName = "NoName"
                    ModuleSpecification = @{ ModuleVersion = "0.2" }
                },
                @{
                    TestName = "BadField"
                    ModuleSpecification = @{ Name = "StrangeFieldModule"; RequiredVersion = "7.4"; Duck = "1.2" }
                },
                @{
                    TestName = "BadType"
                    ModuleSpecification = @{ Name = "BadTypeModule"; RequiredVersion = "Hello!" }
                }
            )
        }

        It "Cannot create from a null argument" {
            { [Microsoft.PowerShell.Commands.ModuleSpecification]::new($null) } | Should -Throw
        }

        It "Cannot create from invalid module hashtables: <TestName>" -TestCases $testCases {
            param([string]$TestName, [hashtable]$ModuleSpecification)

            { [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification) } | Should -Throw
        }
    }
}
