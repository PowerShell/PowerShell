Describe "Get-Process for admin" -Tags @('CI', 'RequireAdminOnWindows') {
    It "Should support -IncludeUserName" {
        (Get-Process powershell -IncludeUserName | Select-Object -First 1).UserName | Should Match $env:USERNAME
    }
}

Describe "Get-Process" -Tags "CI" {
    # These tests are no good, please replace!
    BeforeAll {
        $ps = Get-Process
    }
    It "Should return a type of Object[] for Get-Process cmdlet" -Pending:$IsOSX {
        ,$ps | Should BeOfType "System.Object[]"
    }

    It "Should have not empty Name flags set for Get-Process object" -Pending:$IsOSX {
        $ps | foreach-object { $_.Name | Should Not BeNullOrEmpty }
    }
}

Describe "Get-Process Formatting" -Tags "Feature" {
    It "Should not have Handle in table format header" {
        $types = "System.Diagnostics.Process","System.Diagnostics.Process#IncludeUserName"

        foreach ($type in $types) {
            $formatData = Get-FormatData -TypeName $type -PowerShellVersion $PSVersionTable.PSVersion
            $tableControls = $formatData.FormatViewDefinition | Where-Object {$_.Control -is "System.Management.Automation.TableControl"}
            foreach ($tableControl in $tableControls) {
                $tableControl.Control.Headers.Label -match "Handle*" | Should BeNullOrEmpty
                # verify that rows without headers isn't the handlecount (as PowerShell will create a header that matches the property name)
                $tableControl.Control.Rows.Columns.DisplayEntry.Value -eq "HandleCount" | Should BeNullOrEmpty
            }
        }
    }
}

Describe "Process Parent property" -Tags "CI" {
    It "Has Parent process property" {
        $powershellexe = (get-process -id $PID).mainmodule.filename
        & $powershellexe -noprofile -command '(Get-Process -Id $pid).Parent' | Should Not be $null
    }

    It "Has valid parent process ID property" {
        $powershellexe = (get-process -id $PID).mainmodule.filename
        & $powershellexe -noprofile -command '(Get-Process -Id $pid).Parent.Id' | Should Be $pid
    }
}

