try {
    if ( ! $IsWindows ) {
        $global:PSDefaultParameterValues['it:pending'] = $true
        $script:SkipTests = $true
    }
    Describe "New-CimSession" {
        BeforeAll {
            $sessions = @()
        }
        AfterEach {
            try {
                $sessions | remove-cimsession
            }
            finally {
                $sessions = @()
            }
        }
        It "A cim session can be created" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessions += $session 
            $session.Name | Should be $sessionName
            $session.InstanceId  | should BeOfType "System.Guid"
        }
        It "A Cim session can be retrieved" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessions += $session 
            (get-cimsession -Name $sessionName).InstanceId | should be $session.InstanceId
            (get-cimsession -Id $session.Id).InstanceId | should be $session.InstanceId
            (get-cimsession -InstanceId $session.InstanceId).InstanceId | should be $session.InstanceId
        }
        It "A cim session can be removed" {
            $sessionName = [guid]::NewGuid()
            $session = New-CimSession -ComputerName . -name $sessionName
            $sessiosn += $session
            $session.Name | Should be $sessionName
            $session | Remove-CimSession
            Get-CimSession $session.Id -ErrorAction SilentlyContinue | should BeNullOrEmpty
        }
    }
}
finally {
    $global:PSDefaultParameterValues.remove("it:pending")
}
