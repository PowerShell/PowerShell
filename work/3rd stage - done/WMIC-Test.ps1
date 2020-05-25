$s = [wmisearcher]'Select * from Win_32_Process where handlecount > 10000'
$s.Get() | Sort-Object handlecount | Format-Table handlecount,Name -AutoSize