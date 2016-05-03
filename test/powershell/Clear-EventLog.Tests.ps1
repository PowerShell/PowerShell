if (!$IsLinux -And !$IsOSX) {
    #check to see whether we're running as admin in Windows...
    $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $windowsIdentity
    if ($windowsPrincipal.IsInRole("Administrators") -eq $true) {
        $NonWinAdmin=$false
    } else {$NonWinAdmin=$true}
}


Describe "Clear-EventLog cmdlet tests" {
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
    It "should be able to Clear-EventLog" -Skip:($NonWinAdmin -or $IsLinux -Or $IsOSX) {
        $result=Get-EventLog -LogName TestLog
        $result.Count   | Should be 1
        $result.Message | Should BeExactly "Test"
        Clear-EventLog -LogName TestLog
        $result=Get-EventLog -LogName TestLog -ea Ignore
        $result.Count   | Should be 0
    }
    It "should throw 'The Log name 'MissingTestLog' does not exist' when asked to clear a log that does not exist" -Skip:($IsLinux -Or $IsOSX){
        {Clear-EventLog  -LogName MissingTestLog -ea stop} | Should Throw 'The Log name "MissingTestLog" does not exist'
    }
} 
