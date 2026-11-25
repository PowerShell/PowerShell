# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'PSScopedProperties mutable remoting settings' -Tag 'CI' {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        $PSDefaultParameterValues['It:Skip'] = -not [ExperimentalFeature]::IsEnabled('PSScopedProperties')
    }

    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It 'keeps PSSessionOption concrete and isolates property writes by scope' {
        & {
            $parentOptions = $PS.AtScope('Local').Remoting.SessionOptions
            $parentOptions | Should -BeOfType ([System.Management.Automation.Remoting.PSSessionOption])
            $parentOptions.OperationTimeout = [timespan]::FromSeconds(41)
            $PSSessionOption.OperationTimeout | Should -Be ([timespan]::FromSeconds(41))
            $PSSessionOption.OperationTimeout = [timespan]::FromSeconds(39)
            $parentOptions.OperationTimeout | Should -Be ([timespan]::FromSeconds(39))
            $parentOptions.OperationTimeout = [timespan]::FromSeconds(41)
            & {
                $childOptions = $PS.AtScope('Local').Remoting.SessionOptions
                $childOptions.OperationTimeout = [timespan]::FromSeconds(17)
                $childOptions.OperationTimeout | Should -Be ([timespan]::FromSeconds(17))
                $parentOptions.OperationTimeout | Should -Be ([timespan]::FromSeconds(41))

                { $childOptions.ProxyAuthentication = [System.Management.Automation.Runspaces.AuthenticationMechanism]::Credssp } | Should -Throw
                $childOptions.ProxyAuthentication | Should -Be ([System.Management.Automation.Runspaces.AuthenticationMechanism]::Negotiate)
                $childOptions.ProxyAuthentication = [System.Management.Automation.Runspaces.AuthenticationMechanism]::Basic
                { $childOptions.ProxyAuthentication = [System.Management.Automation.Runspaces.AuthenticationMechanism]::Credssp } | Should -Throw
                $childOptions.ProxyAuthentication | Should -Be ([System.Management.Automation.Runspaces.AuthenticationMechanism]::Basic)

                $PS.Reset('Remoting.SessionOptions')
                $PS.Remoting.SessionOptions.OperationTimeout | Should -Be ([timespan]::FromSeconds(41))
            }
        }

        $standalone = [System.Management.Automation.Remoting.PSSessionOption]::new()
        $standalone.OperationTimeout = [timespan]::FromSeconds(23)
        $standalone.OperationTimeout | Should -Be ([timespan]::FromSeconds(23))
    }

    It 'uses live ApplicationArguments views with shallow copy-on-write and dictionary interfaces' {
        & {
            $parentOptions = $PS.AtScope('Local').Remoting.SessionOptions
            $dictionary = [System.Management.Automation.PSPrimitiveDictionary]::new()
            $dictionary.Add('marker', 'parent')
            $sharedList = [System.Collections.ArrayList]::new()
            [void]$sharedList.Add('before')
            $dictionary.Add('items', [object]$sharedList)
            $parentOptions.ApplicationArguments = $dictionary

            $capturedOptions = $PS.Remoting.SessionOptions
            & {
                $capturedArguments = $capturedOptions.ApplicationArguments
                $capturedOptions.ApplicationArguments = $capturedArguments
                $capturedArguments['self-assignment'] = 'safe'
                $capturedOptions.ApplicationArguments['self-assignment'] | Should -BeExactly 'safe'

                $foreignArguments = $parentOptions.ApplicationArguments
                $capturedOptions.ApplicationArguments = $foreignArguments
                $capturedOptions.ApplicationArguments['foreign-copy'] = 'child'
                $parentOptions.ApplicationArguments.ContainsKey('foreign-copy') | Should -BeFalse

                $capturedArguments = $capturedOptions.ApplicationArguments
                $capturedArguments['marker'] = 'child'
                $childValues = [System.Collections.IDictionary]$capturedArguments
                $childValues['interface'] = 'visible'
                $capturedArguments['marker'] | Should -BeExactly 'child'
                $capturedArguments.ContainsKey('interface') | Should -BeTrue
                $capturedArguments.Keys | Should -Contain 'interface'
                $capturedArguments.Values | Should -Contain 'visible'
                $capturedArguments.Count | Should -Be 4

                $enumerator = ([System.Collections.IEnumerable]$capturedArguments).GetEnumerator()
                $enumeratedCount = 0
                while ($enumerator.MoveNext()) { $enumeratedCount++ }
                $enumeratedCount | Should -Be 4
                $copy = [object[]]::new($capturedArguments.Count)
                ([System.Collections.ICollection]$capturedArguments).CopyTo($copy, 0)
                $copy | Should -Not -BeNullOrEmpty

                $capturedArguments['items'].Add('after')
                $capturedArguments['items'].Count | Should -Be 2

                $capturedOptions.ApplicationArguments = $null
                { $capturedArguments['after-null'] = 'unavailable' } | Should -Throw
                $capturedOptions.ApplicationArguments | Should -BeNullOrEmpty
            }

            $parentOptions.ApplicationArguments['marker'] | Should -BeExactly 'parent'
            $parentOptions.ApplicationArguments.ContainsKey('interface') | Should -BeFalse
            $parentOptions.ApplicationArguments['items'].Count | Should -Be 2 -Because 'nested mutable values keep normal reference semantics'
        }
    }

    It 'preserves explicit null SessionOptions and rejects a captured view after nulling it' {
        & {
            $options = $PS.AtScope('Local').Remoting.SessionOptions
            $options.OperationTimeout = [timespan]::FromSeconds(29)
            $captured = $PS.Remoting.SessionOptions
            $PSSessionOption = $null

            $PSSessionOption | Should -BeNullOrEmpty
            $PS.Remoting.SessionOptions | Should -BeNullOrEmpty
            { $captured.OperationTimeout } | Should -Throw

            $PS.Reset('Remoting.SessionOptions')
            $PS.Remoting.SessionOptions | Should -Not -BeNullOrEmpty
        }
    }

    It 'copies ordinary Hashtable and primitive dictionary overrides without changing the parent' {
        & {
            $parentView = $PS.AtScope('Local')
            $globalData = [hashtable]::new([StringComparer]::OrdinalIgnoreCase)
            $globalData['origin'] = 'global'
            $parentView.Remoting.ApplicationPrivateData = $globalData

            & {
                $scopeData = $PS.AtScope('Local').Remoting.ApplicationPrivateData
                $scopeData | Should -BeOfType ([hashtable])
                $scopeData['local'] = 'child'
                $scopeData['local'] | Should -BeExactly 'child'
                $scopeData.ContainsKey('origin') | Should -BeTrue
                $PS.Reset('Remoting.ApplicationPrivateData')
                $PS.Remoting.ApplicationPrivateData.ContainsKey('local') | Should -BeFalse
            }
            $parentView.Remoting.ApplicationPrivateData.ContainsKey('local') | Should -BeFalse

            $wrappedSource = @{ marker = 'wrapped' }
            $wrappedSourceObject = [System.Management.Automation.PSObject]::AsPSObject($wrappedSource)
            $parentView.Remoting.ApplicationPrivateData = $wrappedSourceObject
            & {
                $wrappedView = $PS.AtScope('Local').Remoting.ApplicationPrivateData
                $wrappedView['local'] = 'child'
                $wrappedView['local'] | Should -BeExactly 'child'
            }
            $wrappedSource.ContainsKey('local') | Should -BeFalse
            $parentView.Remoting.ApplicationPrivateData.ContainsKey('local') | Should -BeFalse

            $primitive = [System.Management.Automation.PSPrimitiveDictionary]::new()
            $primitive.Add('first', 1)
            $parentView.Remoting.ApplicationPrivateData = $primitive
            & {
                $captured = $PS.AtScope('Local').Remoting.ApplicationPrivateData
                $captured | Should -BeOfType ([System.Management.Automation.PSPrimitiveDictionary])
                $captured['second'] = 2
                $captured.Count | Should -Be 2
                $captured.ContainsValue(2) | Should -BeTrue
                $roundTrip = [System.Management.Automation.PSSerializer]::Deserialize([System.Management.Automation.PSSerializer]::Serialize($captured))
                $roundTrip['second'] | Should -Be 2
            }
            $parentView.Remoting.ApplicationPrivateData.ContainsKey('second') | Should -BeFalse
        }
    }
}
