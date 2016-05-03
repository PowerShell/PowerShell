if (!$IsLinux -And !$IsOSX) {
    #check to see whether we're running as admin in Windows...
    $windowsIdentity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $windowsPrincipal = new-object 'Security.Principal.WindowsPrincipal' $windowsIdentity
    if ($windowsPrincipal.IsInRole("Administrators") -eq $true) {
        $NonWinAdmin=$false
    } else {$NonWinAdmin=$true}
}


Describe "New-EventLog cmdlet tests" {
    BeforeEach {
        Remove-EventLog -LogName TestLog -ea Ignore
        New-EventLog -LogName TestLog -Source TestSource -ea Ignore
        Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea ignore
    }
    It "should be able to Remove-EventLog -LogName <string> -ComputerName <string>" -Skip:($NonWinAdmin -or $IsLinux -Or $IsOSX) {
        {Remove-EventLog -LogName TestLog -ComputerName $env:COMPUTERNAME -ea stop}   | Should Not Throw
        {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Throw
        {Get-EventLog -LogName TestLog -ea stop}                                      | Should Throw
    }
    It "should be able to Remove-EventLog -Source <string> -ComputerName <string>" -Skip:($true -Or $NonWinAdmin -Or $IsLinux -Or $IsOSX) {
        {Remove-EventLog -Source TestSource -ComputerName $env:COMPUTERNAME -ea stop}   | Should Not Throw
        {Write-EventLog -LogName TestLog -Source TestSource -Message "Test" -EventID 1 -ea stop} | Should Throw
        {Get-EventLog -LogName TestLog -ea stop}                                      | Should Throw
    }
} 
