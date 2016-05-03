if (!$IsLinux -And !$IsOSX) {
    #check to see whether we're running as admin in Windows...
    $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $windowsIdentity
    if ($windowsPrincipal.IsInRole("Administrators") -eq $true) {
        $NonWinAdmin=$false
    } else {$NonWinAdmin=$true}
}


Describe "Get-EventLog cmdlet tests" {
    BeforeAll {
        Remove-EventLog -LogName TestLog -ea Ignore
        Remove-EventLog -LogName MissingTestLog -ea Ignore
        New-EventLog -LogName TestLog -Source TestSource -ea Ignore
    }
    BeforeEach {
        Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea Ignore
    }
    AfterEach { 
        Clear-EventLog -LogName TestLog -ea Ignore
    }
    AfterAll { 
        Remove-EventLog -LogName TestLog -ea Ignore
    }
    It "should return an array of strings when called with -AsString parameter" -Skip:($IsLinux -Or $IsOSX) {
        {$result=Get-EventLog -AsString -ea stop}  | Should Not Throw
        {$result.GetType()|% { $_.IsArray }}       | Should Be $true
        {$result.Contains("System")}               | Should Be $true
        {$result.Contains("TestLog")}              | Should Be $true
        ($result.Count -ge 4)                      | Should Be $true
    }
    It "should return a list of strings when called with -List parameter" -Skip:($IsLinux -Or $IsOSX) {
        {$result=Get-EventLog -List -ea stop}      | Should Not Throw
        {$result.GetType()|% { $_.IsArray }}       | Should Be $true
        {$logs=$result|Select -ExpandProperty Log} | Should Not Throw
        {$logs.Contains("System")}                 | Should Be $true
        {$logs.Contains("TestLog")}                | Should Be $true
        ($logs.Count -ge 4)                        | Should Be $true
    }
    It "should be able to Get-EventLog -LogName Application -Newest 100" -Skip:($IsLinux -Or $IsOSX){
        {$result=get-eventlog -LogName Application -Newest 100 -ea stop} | Should Not Throw
        ($result)                                  | Should Not BeNullOrEmpty
        ($result.Length -le 100)                   | Should Be $true
        ($result[0].GetType().Name)                | Should Be EventLog
    }
    It "should throw 'ParameterBindingException' when called with both -List and -AsString" -Skip:($IsLinux -Or $IsOSX){
        {Get-EventLog  -LogName MissingTestLog -ea stop} | Should Throw
    }
    It "should be able to Get-EventLog -LogName * with multiple matches" -Skip:($IsLinux -Or $IsOSX){
        {$result=get-eventlog -LogName *  -ea stop}| Should Not Throw
        ($result)                                  | Should Not BeNullOrEmpty
        {$result.Contains("System")}               | Should Be $true
        {$result.Contains("TestLog")}              | Should Be $true
        ($result.count -ge 4)                      | Should Be $true
    }
    It "should throw 'The Log name 'MissingTestLog' does not exist' when asked to get a log that does not exist" -Skip:($IsLinux -Or $IsOSX){
        {Get-EventLog  -LogName MissingTestLog -ea stop} | Should Throw 'does not exist'
    }
} 
