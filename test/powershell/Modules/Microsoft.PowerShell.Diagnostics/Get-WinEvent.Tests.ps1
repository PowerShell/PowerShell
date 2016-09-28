Describe 'Get-WinEvent' -Tags "CI" {
    BeforeAll {
        if ( ! $IsWindows )
        {
            $origDefaults = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues['it:skip'] = $true
        }
    }
    AfterAll {
        if ( ! $IsWindows ){
            $global:PSDefaultParameterValues = $origDefaults
        }
    }
    Context "Get-WinEvent ListProvider parameter" {
        It 'Get-WinEvent can list the providers' {
            $result = Get-WinEvent -listprovider * -erroraction ignore
            $result | should not BeNullOrEmpty
        }
        It 'Get-WinEvent can get a provider by name' {
            $providers = Get-WinEvent -listprovider * -erroraction ignore
            $result = Get-WinEvent -listprovider ($providers[0].name)
            $result | should not BeNullOrEmpty
        }

    }
    Context "Get-WinEvent can retrieve events" {
        # for this set of tests we need to have a provider which has multiple events
        BeforeAll {
            $foundEvents = $false
            $providers = Get-WinEvent -listprovider * -erroraction ignore
            foreach($provider in $providers) {
                $events = Get-WinEvent -provider $provider.name -erroraction ignore
                if ( $events.Count -gt 2 ) {
                    $providerForTests = $provider
                    $foundEvents = $true
                    break
                }
            }
        }
        It 'Get-WinEvent can get events from a provider' {
            # we sample the first 20 results, as this could be very large
            $results = Get-WinEvent -provider $providerForTests.Name -max 20
            foreach($event in $results ) {
                $event.providername | should be $providerForTests.name
            }
        }
        It 'Get-WinEvent can get events via logname' {
            $results = get-winevent -logname $providerForTests.LogLinks.LogName -MaxEvents 10 
            $results | should not BeNullOrEmpty
        }
        It 'Get-WinEvent can use the simplest of filters' {
            $filter = @{ ProviderName = $providerForTests.Name }
            $testEvents = Get-WinEvent -filterhashtable $filter
            $testEvents.Count | should be $events.Count
        }
        It 'Get-WinEvent can use a filter which includes two items' {
            $filter = @{ ProviderName = $providerForTests.Name; Id = $events[0].Id}
            $results = Get-WinEvent -filterHashtable $filter
            $results | Should not BeNullOrEmpty
        }
    }
    # Get-WinEvent works only on windows
    It 'can query a System log' {
        Get-WinEvent -LogName System -MaxEvents 1 | Should Not BeNullOrEmpty
    }
}
