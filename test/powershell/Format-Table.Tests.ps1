Describe "Format-Table" {
    It "Should call format table on piped input without error" {
        { Get-Process | Format-Table } | Should Not Throw

        { Get-Process | ft } | Should Not Throw
    }

    It "Should return a format object data type" {
        $val = (Get-Process | Format-Table | gm )

        $val2 = (Get-Process | Format-Table | gm )

        $val.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"

        $val2.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"
    }

    It "Should be able to be called with optional parameters" {
        $v1 = (Get-Process | Format-Table *)
        $v2 = (Get-Process | Format-Table -Property ProcessName)
        $v3 = (Get-Process | Format-Table -GroupBy ProcessName)
        $v4 = (Get-Process | Format-Table -View StartTime)

        $v12 = (Get-Process | ft *)
        $v22 = (Get-Process | ft -Property ProcessName)
        $v32 = (Get-Process | ft -GroupBy ProcessName)
        $v42 = (Get-Process | ft -View StartTime)

        { $v1 } | Should Not Throw
        { $v2 } | Should Not Throw
        { $v3 } | Should Not Throw
        { $v4 } | Should Not Throw

        { $v12 } | Should Not Throw
        { $v22 } | Should Not Throw
        { $v32 } | Should Not Throw
        { $v42 } | Should Not Throw
    }
}
