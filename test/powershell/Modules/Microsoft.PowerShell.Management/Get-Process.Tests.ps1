Describe "Get-Process for admin" -Tags @('CI', 'RequireAdminOnWindows') {
    It "Should support -IncludeUserName" {
        (Get-Process powershell -IncludeUserName | Select-Object -First 1).UserName | Should Match $env:USERNAME
    }
}

Describe "Get-Process" -Tags "CI" {
    # These tests are no good, please replace!
    It "Should return a type of Object[] for Get-Process cmdlet" -Pending:$IsOSX {
        (Get-Process).GetType().BaseType | Should Be 'array'
        (Get-Process).GetType().Name | Should Be Object[]
    }

    It "Should have not empty Name flags set for Get-Process object" -Pending:$IsOSX {
        Get-Process | foreach-object { $_.Name | Should Not BeNullOrEmpty }
    }
}

Describe "Get-Process Formatting" -Tags "Feature" {
    It "Should not have Handle in table format header" {
        $types = "System.Diagnostics.Process","System.Diagnostics.Process#IncludeUserName"

        foreach ($type in $types)
        {
            $formatData = Get-FormatData -TypeName $type -PowerShellVersion $PSVersionTable.PSVersion
            $tableControls = $formatData.FormatViewDefinition | Where-Object {$_.Control -is "System.Management.Automation.TableControl"}
            foreach ($tableControl in $tableControls)
            {
                $tableControl.Control.Headers.Label -match "Handle*" | Should BeNullOrEmpty
                # verify that rows without headers isn't the handlecount (as PowerShell will create a header that matches the property name)
                $tableControl.Control.Rows.Columns.DisplayEntry.Value -eq "HandleCount" | Should BeNullOrEmpty
            }
        }
    }
}
