New-Module PesterInterop {
    if ((Get-Module Pester).Version -lt '5.1') {
        function BeforeDiscovery {
            . $args[0]
        }
    }
} | Import-Module

Describe 'ClassHelp' {
    BeforeDiscovery {
        Add-Type -AssemblyName System.Xml.Linq

        # Slightly hacky way to make help info available across pester scopes / It blocks.
        $testCases = @(
            @{ ClassName = 'ClassOne'; HelpInfo = @{ Value = $null } }
            @{ ClassName = 'ClassTwo'; HelpInfo = @{ Value = $null } }
        )
    }

    BeforeAll {
        $modulePath = Join-Path $PSScriptRoot -ChildPath 'PSClassHelpProvider'
        $helpFilePath = Join-Path $PSScriptRoot -ChildPath 'PSClassHelpProvider\module\en-US\module-help.xml'

        $psModulePath = $env:PSModulePath
        $env:PSModulePath = $modulePath + [System.IO.Path]::PathSeparator + $modulePath
    }

    AfterAll {
        $env:PSModulePath = $psModulePath
    }

    It 'Should find and load the test module' {
        { Import-Module module -ErrorAction Stop } | Should -Not -Throw
    }

    It 'Should include a maml help document' {
        $helpFilePath | Should -Exist
    }

    It 'Should have a valid help file according to the MAML schema' {
        $xDocument = [System.Xml.Linq.XDocument]::Load(
            $helpFilePath,
            'SetLineInfo'
        )

        $xmlSchemaSet = [System.Xml.Schema.XmlSchemaSet]::new()
        $xmlSchemaSet.XmlResolver = [System.Xml.XmlUrlResolver]::new()
        $null = $xmlSchemaSet.Add(
            'http://schemas.microsoft.com/maml/2004/10',
            (Join-Path $PSHome -ChildPath 'Schemas\PSMaml\Maml.xsd')
        )

        $validateErrors = [System.Collections.Generic.List[string]]::new()
        [System.Xml.Schema.Extensions]::Validate(
            $xDocument,
            $xmlSchemaSet,
            {
                param ($sender, $eventArgs)

                if ($eventArgs.Severity -in 'Error', 'Warning') {
                    $message = 'Line {0}, Column {1}, {2}' -f @(
                        $eventArgs.LineNumber
                        $eventArgs.LinePosition
                        $eventArgs.Message
                    )
                    $validateErrors.Add($message)
                }
            }
        )
        $validateErrors | Should -BeNullOrEmpty
    }

    It 'Should get the class "<ClassName>"' -TestCases $testCases {
        param ($ClassName)

        module\Get-ModuleClass -Name $ClassName | Should -Not -BeNullOrEmpty
    }

    Context 'Help content' {
        It 'Should get help for the class "<ClassName">' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $HelpInfo.Value = Get-Help $ClassName -Category Class
            $HelpInfo.Value | Should -Not -BeNullOrEmpty
        }

        It 'Should have a synopsis for the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Class synopsis.' -f $ClassName

            $HelpInfo.Value.introduction.Text | Should -Be $expected
            $HelpInfo.Value.Synopsis | Should -Be $expected
        }

        It 'Should contain a description for each property of the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Property description.' -f $ClassName

            $field = $HelpInfo.Value.members.member | Where-Object type -eq 'field'
            $field | Should -Not -BeNullOrEmpty
            $field.introduction.Text | Should -Be $expected
            $field.fieldData.name | Should -Be 'Property'
            $field.fieldData.type.name | Should -Be 'string'
        }

        It 'Should contain information about a default constructor of the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Constructor description.' -f $ClassName

            $ctor = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'ctor' -and -not $_.Parameters }
            $ctor | Should -Not -BeNullOrEmpty
            $ctor.introduction.Text | Should -Be $expected
        }

        It 'Should contain information about a constructor which requires arguments of the class "<ClassName">' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Constructor with argument description.' -f $ClassName

            $ctor = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'ctor' -and $_.Parameters }
            $ctor | Should -Not -BeNullOrEmpty
            $ctor.introduction.Text | Should -Be $expected
            $ctor.Parameters | Should -HaveCount 1
            $ctor.Parameters.Parameter.Name | Should -Be 'argument'
            $ctor.Parameters.Parameter.type.name | Should -Be 'string'
        }

        It 'Should contain information about a method which returns nothing and requires no arguments in the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Void method description.' -f $ClassName

            $method = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'voidMethod' -and -not $_.Parameters }
            $method | Should -Not -BeNullOrEmpty
            $method.introduction.Text | Should -Be $expected
            $method.Parameters | Should -BeNullOrEmpty
        }

        It 'Should contain information about a method which returns nothing and requires arguments in the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: Void method with argument description.' -f $ClassName

            $method = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'voidMethod' -and $_.Parameters }
            $method | Should -Not -BeNullOrEmpty
            $method.introduction.Text | Should -Be $expected
            $method.Parameters | Should -HaveCount 1
            $method.Parameters.Parameter.Name | Should -Be 'argument'
            $method.Parameters.Parameter.type.name | Should -Be 'string'
        }

        It 'Should contain information about a method which returns a value and requires no arguments in the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: String method description.' -f $ClassName

            $method = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'returnMethod' -and -not $_.Parameters }
            $method | Should -Not -BeNullOrEmpty
            $method.introduction.Text | Should -Be $expected
            $method.Parameters | Should -BeNullOrEmpty
        }

        It 'Should contain information about a method which returns a value and requires arguments in the class "<ClassName>"' -TestCases $testCases {
            param ($ClassName, $HelpInfo)

            $expected = '{0}: String method with argument description.' -f $ClassName

            $method = $HelpInfo.Value.members.member | Where-Object { $_.title -eq 'returnMethod' -and $_.Parameters }
            $method | Should -Not -BeNullOrEmpty
            $method.introduction.Text | Should -Be $expected
            $method.Parameters | Should -HaveCount 1
            $method.Parameters.Parameter.Name | Should -Be 'argument'
            $method.Parameters.Parameter.type.name | Should -Be 'string'
        }
    }
}
