Describe "Format-Table" {
    It "Should call format table on piped input without error" {
  { Get-Date | Format-Table } | Should Not Throw

  { Get-Date | ft } | Should Not Throw
    }

    It "Should return a format object data type" {
  $val = (Get-Date | Format-Table | gm )

  $val2 = (Get-Date | Format-Table | gm )

	$val.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"

	$val2.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"
    }

    It "Should be able to be called with optional parameters" {
  $v1 = (Get-Date | Format-Table *)
  $v2 = (Get-Date | Format-Table -Property Hour)
  $v3 = (Get-Date | Format-Table -GroupBy Hour)

  $v12 = (Get-Date | ft *)
  $v22 = (Get-Date | ft -Property Hour)
  $v32 = (Get-Date | ft -GroupBy Hour)

    }
}
