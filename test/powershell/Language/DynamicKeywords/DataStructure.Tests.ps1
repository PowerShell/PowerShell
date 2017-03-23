Import-Module $PSScriptRoot/DynamicKeywordTestSupport.psm1

Describe "Keyword loading into the global keyword namespace" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "loads a basic dynamic keyword in" {
        $kw = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "BasicDsl" -ScriptBlock {
            [scriptblock]::Create("using module BasicDsl").Invoke()
            [System.Management.Automation.Language.DynamicKeyword]::GetKeyword("BasicKeyword")
        }

        $kw.Keyword | Should Be "BasicKeyword"
    }
}

Describe "Adding keyword attributes to the DynamicKeyword data structure" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $attrTestCases = @(
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "NameMode"; expected = "NoName" },
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "BodyMode"; expected = "Command" },
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "UseMode"; expected = "OptionalMany" },
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "ResourceName"; expected = $null },
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "DirectCall"; expected = $false },
            @{ module = "BasicDsl"; keyword = "BasicKeyword"; attr = "MetaStatement"; expected = $false },

            @{ module = "BodyModeDsl"; keyword = "CommandBodyKeyword"; attr = "BodyMode"; expected = "Command" },
            @{ module = "BodyModeDsl"; keyword = "HashtableBodyKeyword"; attr = "BodyMode"; expected = "Hashtable" },
            @{ module = "BodyModeDsl"; keyword = "ScriptBlockBodyKeyword"; attr = "BodyMode"; expected = "ScriptBlock" }
        )

        $useModeTestCases = @(
            @{ useMode = "Required" },
            @{ useMode = "RequiredMany" },
            @{ useMode = "Optional" },
            @{ useMode = "OptionalMany" }
        )

        $attrModules = ($attrTestCases | ForEach-Object { $_.module }) | Select-Object -Unique
        $attrArgs = Convert-TestCasesToSerialized -TestCases $attrTestCases -Keys "module","keyword","attr","expected"
        $attrResults = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName $attrModules -Arguments $attrArgs -ScriptBlock {
            $testInput = $args[0].Split(";")
            $results = @{}
            foreach ($test in $testInput)
            {
                $testParams = $test.Split(",")
                $module = $testParams[0]
                $keyword = $testParams[1]
                $attr = $testParams[2]

                [scriptblock]::Create("using module $module").Invoke()
                $kw = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword($keyword)

                $key = $keyword,$attr -join "+"
                $value = $kw.$attr

                switch ($attr)
                {
                    "NameMode" { $value = ([System.Management.Automation.Language.DynamicKeywordNameMode]$value).ToString() }
                    "BodyMode" { $value = ([System.Management.Automation.Language.DynamicKeywordBodyMode]$value).ToString() }
                    "UseMode"  { $value = ([System.Management.Automation.Language.DynamicKeywordUseMode]$value).ToString() }
                }
                
                $results += @{ $key = $value }
            }

            $results
        }

        $useModeArgs = Convert-TestCasesToSerialized -TestCases $useModeTestCases -Keys "useMode"
        $useModeResults = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "UseModeDsl" -Arguments $useModeArgs -ScriptBlock {
            $testInput = $args[0].Split(";")
            $results = @{}

            [scriptblock]::Create("using module UseModeDsl").Invoke()

            foreach ($test in $testInput)
            {
                $kwName = "$($test)UseKeyword"
                $value = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword("UseModeDsl").InnerKeywords.$kwName
                $results += @{ $test = $value }
            }

            $results
        }
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "<keyword> has attribute <attr> with value <expected>" -TestCases $attrTestCases {
        param($module, $keyword, $attr, $expected)

        $key = $keyword,$attr -join "+"
        $attrResults.$key | Should Be $expected
    }

    It "<useMode>UseKeyword should have use mode <useMode>" -TestCases $useModeTestCases {
        param($useMode)

        $useModeResults.$useMode.Keyword | Should Be "$($useMode)UseKeyword"
    }
}

Describe "Adding Properties to keywords" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $testCases = @(
           @{ property = "StringProperty"; type = "string"; values = $null },
           @{ property = "IntProperty"; type = "int"; values = $null },
           @{ property = "OuterProperty"; type = "OuterType"; values = @("OuterOne", "OuterTwo") },
           @{ property = "InnerProperty"; type = "InnerType"; values = @("InnerOne", "InnerTwo") }
           @{ property = "MandatoryProperty"; type = "string"; values = $null }
        )

        $properties = ($testCases | ForEach-Object { $_.property }) -join ","
        $command = {
            $properties = $args[0].Split(",")
            $results = @{}

            [scriptblock]::Create("using module PropertyDsl").Invoke()

            $kw = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword("PropertyKeyword")

            foreach ($property in $properties)
            {
                $results += @{ $property = $kw.Properties.$property }
            }

            $results
        }

        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "PropertyDsl" -ScriptBlock $command -Arguments $properties
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "contains property <property>" -TestCases $testCases {
        param($property, $type, $values)

        $results.$property.Name | Should Be $property
    }

    It "has property <property> with type <type>" -TestCases $testCases {
        param($property, $type, $values)

        $results.$property.TypeConstraint | Should Be $type
    }

    It "has property <property> with possible values <values>" -TestCases $testCases {
        param($property, $type, $values)
        $results.$property.Values -join "," | Should Be ($values -join ",")
    }

    It "correctly sets the mandatory property" {
        $results.MandatoryProperty.Mandatory | Should Be $true
    }
}

Describe "Adding Parameters to keywords" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH = New-PathEntry -ModulePath $TestDrive -PathString $env:PSMODULEPATH

        $testCases = @(
           @{ parameter = "StringParameter"; type = "string"; values = $null },
           @{ parameter = "IntParameter"; type = "int"; values = $null },
           @{ parameter = "OuterParameter"; type = "OuterType"; values = @("OuterOne", "OuterTwo") },
           @{ parameter = "InnerParameter"; type = "InnerType"; values = @("InnerOne", "InnerTwo") }
           @{ parameter = "Switch"; type = "System.Management.Automation.SwitchParameter"; values = $null },
           @{ parameter = "MandatoryParameter"; type = "string"; values = $null }
        )

        $parameters = ($testCases | ForEach-Object { $_.parameter }) -join ","
        $command = {
            $parameters = $args[0].Split(",")
            $results = @{}

            [scriptblock]::Create("using module ParameterDsl").Invoke()

            $kw = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword("ParameterKeyword")

            foreach ($parameter in $parameters)
            {
                $results += @{ $parameter = $kw.Parameters.$parameter }
            }

            $results
        }

        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "ParameterDsl" -ScriptBlock $command -Arguments $parameters
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "adds the parameter <parameter>" -TestCases $testCases {
        param($parameter, $type, $values)
        $results.$parameter.Name | Should Be $parameter
    }

    It "assigns the type <type> to the parameter <parameter>" -TestCases $testCases {
        param($parameter, $type, $values)
        $results.$parameter.TypeConstraint | Should Be $type
    }

    It "assigns the values <values> to the parameter <parameter>" -TestCases $testCases {
        param($parameter, $type, $values)
        $results.$parameter.Values -join "," | Should Be ($values -join ",")
    }

    It "recognizes the switch parameter" {
        $results.Switch.Switch | Should Be $true
    }
}

Describe "Adding inner keywords to keywords" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSMODULEPATH
        $env:PSMODULEPATH += [System.IO.Path]::PathSeparator + $TestDrive

        $testCases = @(
            @{ keyword = "NestedKeyword1"; path = @("NestedKeyword") },
            @{ keyword = "NestedKeyword2"; path = @("NestedKeyword") },
            @{ keyword = "NestedKeyword1_1"; path = @("NestedKeyword","NestedKeyword1") },
            @{ keyword = "NestedKeyword1_2"; path = @("NestedKeyword","NestedKeyword1") },
            @{ keyword = "NestedKeyword2_1"; path = @("NestedKeyword","NestedKeyword2") },
            @{ keyword = "NestedKeyword2_2"; path = @("NestedKeyword","NestedKeyword2") },
            @{ keyword = "NestedKeyword1_1_1"; path = @("NestedKeyword","NestedKeyword1","NestedKeyword1_1") },
            @{ keyword = "NestedKeyword2_2_1"; path = @("NestedKeyword","NestedKeyword2","NestedKeyword2_2") },
            @{ keyword = "NestedKeyword2_2_1_1"; path = @("NestedKeyword","NestedKeyword2","NestedKeyword2_2","NestedKeyword2_2_1") }
        )

        $keywords = ($testCases | ForEach-Object { $_.keyword }) -join ","
        $paths = ($testCases | ForEach-Object { $_.path -join "," }) -join ";"
        $testInput = $keywords + "^" + $paths
        $command = {
            $testInput = $args[0].Split("^")
            $keywords = $testInput[0].Split(",")
            $paths = $testInput[1].Split(";")

            [scriptblock]::Create("using module NestedDsl").Invoke()

            $results = @{}

            for ($i = 0; $i -lt $keywords.Count; $i++)
            {
                $keyword = $keywords[$i]
                $path = $paths[$i].Split(",")
                $innerKw = [System.Management.Automation.Language.DynamicKeyword]::GetKeyword($path[0])
                $tail = ($path | Select-Object -Skip 1)
                foreach ($next in $tail)
                {
                    $innerKw = $innerKw.InnerKeywords.$next
                    if ($innerKw -eq $null)
                    {
                        Write-Error "$keyword : $next had a null entry"
                    }
                }
                $results += @{ $keyword = $innerKw.InnerKeywords.$keyword }
            }

            $results
        }

        $results = Get-ScriptBlockResultInNewProcess -TestDrive $TestDrive -ModuleName "NestedDsl" -ScriptBlock $command -Arguments $testInput
    }

    AfterAll {
        $env:PSMODULEPATH = $savedModPath
    }

    It "defines an inner keyword <keyword> within the keyword path <path>" -TestCases $testCases {
        param($keyword, $path)

        $results.$keyword.Keyword | Should Be $keyword
    }
}
