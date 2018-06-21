function PermuteOnProperty
{
    param([hashtable[]]$Object, [string]$Key, [object]$Value)

    foreach ($obj in $Object)
    {
        $obj.Clone()
    }

    foreach ($obj in $Object)
    {
        $o2 = $obj.Clone()
        $o2.$Key = $Value
        $o2
    }
}

function PermuteObject
{
    param([hashtable]$InitialObject, [hashtable]$KeyValues)

    $l = $InitialObject
    foreach ($key in $KeyValues.Keys)
    {
        $l = PermuteOnProperty -Object $l -Key $key -Value $KeyValues[$key]
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

$modSpecInvalid = @{
    ModuleName = "ModSpecInvalid"
    Guid = $guid
    ModuleVersion = "1.2.3"
    RequiredVersion = "1.2.4"
}

Describe "Valid ModuleSpecification objects" {
    BeforeAll {
        $testCases = [System.Collections.Generic.List[hashtable]]::new()

        foreach ($case in (PermuteObject -InitialObject $modSpecRequired -KeyValues $requiredOptionalConstraints))
        {
            $testCases.Add(@{
                ModuleSpecification = $case
                Keys = ($case.Keys -join ",")
            })
        }

        foreach ($case in (PermuteObject -InitialObject $modSpecRange -KeyValues $rangeOptionalConstraints))
        {
            $testCases.Add(@{
                ModuleSpecification = $case
                Keys = ($case.Keys -join ",")
            })
        }

        $testCases = $testCases.ToArray()
    }

    It "Can be created from Hashtable with keys: <Keys>" -TestCases $testCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification]$ModuleSpecification, [string]$Keys)
        [Microsoft.PowerShell.Commands.ModuleSpecification]::new($ModuleSpecification) | Should -Not -BeNull
    }

    It "Can be reconstructed from self.ToString() with keys: <Keys>" -TestCases $testCases {
        param([Microsoft.PowerShell.Commands.ModuleSpecification]$ModuleSpecification, [string]$Keys)

        [Microsoft.PowerShell.Commands.ModuleSpecification]$clone = $null
        [Microsoft.PowerShell.Commands.ModuleSpecification]::TryParse(($ModuleSpecification.ToString()), [ref]$clone) | Should -BeTrue

        $clone.Name | Should -Be $ModuleSpecification.Name

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