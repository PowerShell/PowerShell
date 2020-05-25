$com = read-host "Enter Computer name here"
Get-WinEvent  -Computername $com -FilterHashtable @{Logname = 'Security'; ID = 4672} |
    Where-Object {-not(($_.Properties[0].Value -like "S-1-5-18") -or ($_.Properties[0].Value -like "S-1-5-19") -or ($_.Properties[0].Value -like "S-1-5-20"))} |
    Select-Object -first 1 @{N = 'User'; E = {$_.Properties[1].Value}}