Import-Module $PSScriptRoot/DynamicKeywordTestSupport.psm1

Describe "Keyword loading into the global keyword namespace" -Tags "CI" {
    BeforeAll {
        $savedModPath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive
    }

    AfterAll {
        $env:PSModulePath = $savedModPath
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
        $savedModPath = $env:PSModulePath
        $env:PSModulePath += [System.IO.Path]::PathSeparator + $TestDrive

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
            $testInput = $args[0] -split ";"
            $results = @{}
            foreach ($test in $testInput)
            {
                $testParams = $test -split ","
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
            $testInput = $args[0] -split ";"
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
        $env:PSModulePath = $savedModPath
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
}

Describe "Adding Parameters to keywords" -Tags "CI" {

}

Describe "Adding inner keywords to keywords" -Tags "CI" {

}