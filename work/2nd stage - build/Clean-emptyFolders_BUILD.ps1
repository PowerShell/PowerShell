$root = Read-Host -Prompt "Enter path to clear"
Invoke-Command -ScriptBlock {
    Get-ChildItem -Path "$root" -Directory -Recurse | Where-Object {!$_.GetFiles("*","AllDirectories")} | Remove-Item -Recurse
} -ComputerName localhost