Describe 'Tests for indexers' -Tags "CI" {
    It 'Indexer in dictionary' { 
        
        $hashtable = @{ "Hello"="There" }    
        $hashtable["Hello"] | Should Be "There"
    }

    It 'Accessing a Indexed property of a dictionary that does not exist should return $NULL' {
        $hashtable = @{ "Hello"="There" }
        $hashtable["Hello There"] | Should Be $null
        }

    It 'Wmi object implements an indexer' {
    
        # wmi object only exist on windows
        if(-not $IsWindows)
        {
            return
        }
        $service = Get-WmiObject -List -Amended Win32_Service
    
        $service.Properties["DisplayName"].Name | Should Be 'DisplayName'
    }

    It 'Accessing a Indexed property of a wmi object that does not exist should return $NULL' {
        # wmi object only exist on windows
        if(-not $IsWindows)
        {
            return
        }    
        
        $service = Get-WmiObject -List -Amended Win32_Service
        $service.Properties["Hello There"] | Should Be $null
    }
}

