Describe "Set/New-Service cmdlet tests" -Tags "Feature", "RequireAdminOnWindows" {
    BeforeAll {
        $originalDefaultParameterValues = $PSDefaultParameterValues.Clone()
        if ( -not $IsWindows ) {
            $PSDefaultParameterValues["it:skip"] = $true
        }
    }
    AfterAll {
        $global:PSDefaultParameterValues = $originalDefaultParameterValues
    }

    It "SetServiceCommand can be used as API for '<parameter>' with '<value>'" -TestCases @(
        @{parameter = "ComputerName"; value = "foo"},
        @{parameter = "Name"        ; value = "bar"},
        @{parameter = "DisplayName" ; value = "hello"},
        @{parameter = "Description" ; value = "hello world"},
        @{parameter = "StartupType" ; value = "Automatic"},
        @{parameter = "StartupType" ; value = "Boot"},
        @{parameter = "StartupType" ; value = "Disabled"},
        @{parameter = "StartupType" ; value = "Manual"},
        @{parameter = "StartupType" ; value = "System"},
        @{parameter = "Status"      ; value = "Running"},
        @{parameter = "Status"      ; value = "Stopped"},
        @{parameter = "Status"      ; value = "Paused"},
        @{parameter = "InputObject" ; value = (Get-Service | Select-Object -First 1)},
        # cmdlet inherits this property, but it's not exposed as parameter so it should be $null
        @{parameter = "Include"     ; value = "foo", "bar" ; expectedNull = $true},
        # cmdlet inherits this property, but it's not exposed as parameter so it should be $null
        @{parameter = "Exclude"     ; value = "foo", "bar" ; expectedNull = $true}
    ) {
        param($parameter, $value, $expectedNull)

        $setServiceCommand = [Microsoft.PowerShell.Commands.SetServiceCommand]::new()
        $setServiceCommand.$parameter = $value
        if ($expectedNull -eq $true) {
            $setServiceCommand.$parameter | Should BeNullOrEmpty
        }
        else {
            $setServiceCommand.$parameter | Should Be $value
        }
    }

    It "Set-Service parameter validation for invalid values: <script>" -TestCases @(
        @{
            script  = {Set-Service foo -StartupType bar};
            errorid = "CannotConvertArgumentNoMessage,Microsoft.PowerShell.Commands.SetServiceCommand"
        }
    ) {
        param($script, $errorid)
        { & $script } | ShouldBeErrorId $errorid
    }

    It "Set-Service can change '<parameter>' to '<value>'" -TestCases @(
        @{parameter = "Description"; value = "hello"},
        @{parameter = "DisplayName"; value = "test spooler"},
        @{parameter = "StartupType"; value = "Disabled"},
        @{parameter = "Status"     ; value = "running"     ; expected = "OK"}
    ) {
        param($parameter, $value, $expected)
        $currentService = Get-CimInstance -ClassName Win32_Service -Filter "Name='spooler'"
        try {
            $setServiceCommand = [Microsoft.PowerShell.Commands.SetServiceCommand]::new()
            $setServiceCommand.Name = "Spooler"
            $setServiceCommand.$parameter = $value
            $setServiceCommand.Invoke()
            $updatedService = Get-CimInstance -ClassName Win32_Service -Filter "Name='spooler'"
            if ($expected -eq $null) {
                $expected = $value
            }
            if ($parameter -eq "StartupType") {
                $updatedService.StartMode | Should Be $expected
            }
            else {
                $updatedService.$parameter | Should Be $expected
            }
        }
        finally {
            if ($parameter -eq "StartupType") {
                $setServiceCommand.StartupType = $currentService.StartMode
            }
            else {
                $setServiceCommand.$parameter = $currentService.$parameter
            }
            $setServiceCommand.Invoke()
            $updatedService = Get-CimInstance -ClassName Win32_Service -Filter "Name='spooler'"
            $updatedService.$parameter | Should Be $currentService.$parameter
        }
    }

    It "NewServiceCommand can be used as API for '<parameter>' with '<value>'" -TestCases @(
        @{parameter = "Name"           ; value = "bar"},
        @{parameter = "BinaryPathName" ; value = "hello"},
        @{parameter = "DisplayName"    ; value = "hello world"},
        @{parameter = "Description"    ; value = "this is a test"},
        @{parameter = "StartupType"    ; value = "Automatic"},
        @{parameter = "StartupType"    ; value = "Boot"},
        @{parameter = "StartupType"    ; value = "Disabled"},
        @{parameter = "StartupType"    ; value = "Manual"},
        @{parameter = "StartupType"    ; value = "System"},
        @{parameter = "Credential"     ; value = (
                [System.Management.Automation.PSCredential]::new("username", 
                    (ConvertTo-SecureString "PlainTextPassword" -AsPlainText -Force)))
        }
        @{parameter = "DependsOn"      ; value = "foo", "bar"}
    ) {
        param($parameter, $value)

        $newServiceCommand = [Microsoft.PowerShell.Commands.NewServiceCommand]::new()
        $newServiceCommand.$parameter = $value
        $newServiceCommand.$parameter | Should Be $value
    }

    It "New-Service can create a new service called '<name>'" -TestCases @(
        @{name = "testautomatic"; startupType = "Automatic"; description = "foo" ; displayname = "one"},
        @{name = "testmanual"   ; startupType = "Manual"   ; description = "bar" ; displayname = "two"},
        @{name = "testdisabled" ; startupType = "Disabled" ; description = $null ; displayname = $null}
    ) {
        param($name, $startupType, $description, $displayname)
        try {
            $parameters = @{
                Name           = $name;
                BinaryPathName = "$PSHOME\powershell.exe";
                StartupType    = $startupType;
            }
            if ($description) {
                $parameters += @{description = $description}
            }
            if ($displayname) {
                $parameters += @{displayname = $displayname}
            }
            $service = New-Service @parameters
            $service | Should Not BeNullOrEmpty
            $service = Get-CimInstance Win32_Service -Filter "name='$name'"
            $service | Should Not BeNullOrEmpty
            $service.Name | Should Be $name
            $service.Description | Should Be $description
            $expectedStartup = $(
                switch ($startupType) {
                    "Automatic" {"Auto"}
                    "Manual" {"Manual"}
                    "Disabled" {"Disabled"}
                    default { throw "Unsupported StartupType in TestCases" }
                }
            )
            $service.StartMode | Should Be $expectedStartup
            if ($displayname -eq $null) {
                $service.DisplayName | Should Be $name
            }
            else {
                $service.DisplayName | Should Be $displayname
            }
        }
        finally {
            $service = Get-CimInstance Win32_Service -Filter "name='$name'"
            if ($service -ne $null) {
                $service | Remove-CimInstance
            }
        }
    }

    It "New-Service with bad parameters will fail for '<name>' where '<parameter>' = '<value>'" -TestCases @(
        @{name = 'credtest'    ; parameter = "Credential" ; value = (
            [System.Management.Automation.PSCredential]::new("username", 
            (ConvertTo-SecureString "PlainTextPassword" -AsPlainText -Force)))
        },
        @{name = 'badstarttype'; parameter = "StartupType"; value = "System"},
        @{name = 'winmgmt'     ; parameter = "DisplayName"; value = "foo"}
    ) {
        param($name, $parameter, $value)
        $parameters = @{$parameter = $value; Name = $name; Binary = "$PSHOME\powershell.exe"; ErrorAction = "Stop"}
        { New-Service @parameters } | ShouldBeErrorId "CouldNotNewService,Microsoft.PowerShell.Commands.NewServiceCommand"
    }
}
