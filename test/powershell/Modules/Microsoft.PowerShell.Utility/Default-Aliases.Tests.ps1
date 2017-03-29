Describe "Windows aliases do not conflict with Linux commands" -Tags "CI" {
    BeforeAll {
        $removeAliasList = @("ac","compare","cpp","diff","sleep","sort","start","cat","cp","ls","man","mount","mv","ps","rm","rmdir")
        $keepAliasList = @{cd="Set-Location"},@{dir="Get-ChildItem"},@{echo="Write-output"},@{fc="format-custom"},@{kill="stop-process"},@{clear="clear-host"}
    }

    foreach ($alias in $removeAliasList) {
        It "Should not have certain aliases on Linux" -Skip:$IsWindows {
            Test-Path Alias:$alias | Should Be $false
        }
    }

    foreach ($alias in $keepAliasList) {
        It "Should have aliases that are Bash built-ins on Linux" {
            (Get-Alias $alias.Keys).Definition | Should Be $alias.Values
        }
    }

    It "Should have more as a function" {
        Test-Path Function:more | Should Be $true
    }
}


Describe "Verify approved aliases list" -Tags "CI" {
    BeforeAll {
        $FullCLR = !$isCoreCLR
        $CoreWindows = $isCoreCLR -and $IsWindows
        $CoreUnix = $isCoreCLR -and !$IsWindows
        $FullCLR -or $CoreWindows -or $CoreUnix
        $aliasFullList = @{
            "% -> ForEach-Object"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "? -> Where-Object"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ac -> Add-Content"                       =             $FullCLR -or $CoreWindows
            "asnp -> Add-PSSnapin"                    =             $FullCLR
            "cat -> Get-Content"                      =             $FullCLR -or $CoreWindows
            "cd -> Set-Location"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "CFS -> ConvertFrom-String"               =             $FullCLR
            "chdir -> Set-Location"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "clc -> Clear-Content"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "clear -> Clear-Host"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "clhy -> Clear-History"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "cli -> Clear-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "clp -> Clear-ItemProperty"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "cls -> Clear-Host"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "clv -> Clear-Variable"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "cnsn -> Connect-PSSession"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "compare -> Compare-Object"               =             $FullCLR -or $CoreWindows
            "copy -> Copy-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "cp -> Copy-Item"                         =             $FullCLR -or $CoreWindows
            "cpi -> Copy-Item"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "cpp -> Copy-ItemProperty"                =             $FullCLR -or $CoreWindows
            "curl -> Invoke-WebRequest"               =             $FullCLR
            "cvpa -> Convert-Path"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "dbp -> Disable-PSBreakpoint"             =             $FullCLR -or $CoreWindows -or $CoreUnix
            "del -> Remove-Item"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "diff -> Compare-Object"                  =             $FullCLR -or $CoreWindows
            "dir -> Get-ChildItem"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "dnsn -> Disconnect-PSSession"            =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ebp -> Enable-PSBreakpoint"              =             $FullCLR -or $CoreWindows -or $CoreUnix
            "echo -> Write-Output"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "epal -> Export-Alias"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "epcsv -> Export-Csv"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "epsn -> Export-PSSession"                =             $FullCLR
            "erase -> Remove-Item"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "etsn -> Enter-PSSession"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "exsn -> Exit-PSSession"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "fc -> Format-Custom"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "fhx -> Format-Hex"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "fl -> Format-List"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "foreach -> ForEach-Object"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ft -> Format-Table"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "fw -> Format-Wide"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gal -> Get-Alias"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gbp -> Get-PSBreakpoint"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gc -> Get-Content"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gcb -> Get-Clipboard"                    =             $FullCLR
            "gci -> Get-ChildItem"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gcm -> Get-Command"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gcs -> Get-PSCallStack"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gdr -> Get-PSDrive"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ghy -> Get-History"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gi -> Get-Item"                          =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gin -> Get-ComputerInfo"                 =                          $CoreWindows -or $CoreUnix
            "gjb -> Get-Job"                          =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gl -> Get-Location"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gm -> Get-Member"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gmo -> Get-Module"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gp -> Get-ItemProperty"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gps -> Get-Process"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gpv -> Get-ItemPropertyValue"            =             $FullCLR -or $CoreWindows -or $CoreUnix
            "group -> Group-Object"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gsn -> Get-PSSession"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gsnp -> Get-PSSnapin"                    =             $FullCLR
            "gsv -> Get-Service"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gtz -> Get-TimeZone"                     =                          $CoreWindows
            "gu -> Get-Unique"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gv -> Get-Variable"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "gwmi -> Get-WmiObject"                   =             $FullCLR
            "h -> Get-History"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "history -> Get-History"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "icm -> Invoke-Command"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "iex -> Invoke-Expression"                =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ihy -> Invoke-History"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ii -> Invoke-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ipal -> Import-Alias"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ipcsv -> Import-Csv"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ipmo -> Import-Module"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ipsn -> Import-PSSession"                =             $FullCLR
            "irm -> Invoke-RestMethod"                =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ise -> powershell_ise.exe"               =             $FullCLR
            "iwmi -> Invoke-WmiMethod"                =             $FullCLR
            "iwr -> Invoke-WebRequest"                =             $FullCLR -or $CoreWindows -or $CoreUnix
            "kill -> Stop-Process"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "lp -> Out-Printer"                       =             $FullCLR
            "ls -> Get-ChildItem"                     =             $FullCLR -or $CoreWindows
            "man -> help"                             =             $FullCLR -or $CoreWindows
            "md -> mkdir"                             =             $FullCLR -or $CoreWindows -or $CoreUnix
            "measure -> Measure-Object"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "mi -> Move-Item"                         =             $FullCLR -or $CoreWindows -or $CoreUnix
            "mount -> New-PSDrive"                    =             $FullCLR -or $CoreWindows
            "move -> Move-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "mp -> Move-ItemProperty"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "mv -> Move-Item"                         =             $FullCLR -or $CoreWindows
            "nal -> New-Alias"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ndr -> New-PSDrive"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ni -> New-Item"                          =             $FullCLR -or $CoreWindows -or $CoreUnix
            "nmo -> New-Module"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "npssc -> New-PSSessionConfigurationFile" =             $FullCLR
            "nsn -> New-PSSession"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "nv -> New-Variable"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "nwsn -> New-PSWorkflowSession"           =             $FullCLR
            "ogv -> Out-GridView"                     =             $FullCLR
            "oh -> Out-Host"                          =             $FullCLR -or $CoreWindows -or $CoreUnix
            "popd -> Pop-Location"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ps -> Get-Process"                       =             $FullCLR -or $CoreWindows
            "pushd -> Push-Location"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "pwd -> Get-Location"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "r -> Invoke-History"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rbp -> Remove-PSBreakpoint"              =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rcjb -> Receive-Job"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rcsn -> Receive-PSSession"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rd -> Remove-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rdr -> Remove-PSDrive"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ren -> Rename-Item"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "ri -> Remove-Item"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rjb -> Remove-Job"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rm -> Remove-Item"                       =             $FullCLR -or $CoreWindows
            "rmdir -> Remove-Item"                    =             $FullCLR -or $CoreWindows
            "rmo -> Remove-Module"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rni -> Rename-Item"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rnp -> Rename-ItemProperty"              =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rp -> Remove-ItemProperty"               =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rsn -> Remove-PSSession"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rsnp -> Remove-PSSnapin"                 =             $FullCLR
            "rujb -> Resume-Job"                      =             $FullCLR
            "rv -> Remove-Variable"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rvpa -> Resolve-Path"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "rwmi -> Remove-WmiObject"                =             $FullCLR
            "sajb -> Start-Job"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sal -> Set-Alias"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "saps -> Start-Process"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sasv -> Start-Service"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sbp -> Set-PSBreakpoint"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sc -> Set-Content"                       =             $FullCLR -or $CoreWindows -or $CoreUnix
            "scb -> Set-Clipboard"                    =             $FullCLR
            "select -> Select-Object"                 =             $FullCLR -or $CoreWindows -or $CoreUnix
            "set -> Set-Variable"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "shcm -> Show-Command"                    =             $FullCLR
            "si -> Set-Item"                          =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sl -> Set-Location"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sleep -> Start-Sleep"                    =             $FullCLR -or $CoreWindows
            "sls -> Select-String"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "sort -> Sort-Object"                     =             $FullCLR -or $CoreWindows
            "sp -> Set-ItemProperty"                  =             $FullCLR -or $CoreWindows -or $CoreUnix
            "spjb -> Stop-Job"                        =             $FullCLR -or $CoreWindows -or $CoreUnix
            "spps -> Stop-Process"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "spsv -> Stop-Service"                    =             $FullCLR -or $CoreWindows -or $CoreUnix
            "start -> Start-Process"                  =             $FullCLR -or $CoreWindows
            "stz -> Set-TimeZone"                     =                          $CoreWindows
            "sujb -> Suspend-Job"                     =             $FullCLR
            "sv -> Set-Variable"                      =             $FullCLR -or $CoreWindows -or $CoreUnix
            "swmi -> Set-WmiInstance"                 =             $FullCLR
            "tee -> Tee-Object"                       =             $FullCLR -or $CoreWindows
            "trcm -> Trace-Command"                   =             $FullCLR
            "type -> Get-Content"                     =             $FullCLR -or $CoreWindows -or $CoreUnix
            "wget -> Invoke-WebRequest"               =             $FullCLR
            "where -> Where-Object"                   =             $FullCLR -or $CoreWindows -or $CoreUnix
            "wjb -> Wait-Job"                         =             $FullCLR -or $CoreWindows -or $CoreUnix
            "write -> Write-Output"                   =             $FullCLR -or $CoreWindows
        }

        $aliaslist = @($aliasFullList.Keys | ForEach-Object { if ($aliasFullList[$_]) { $_ } })
    }


    It "All approved aliases present (no aliases removed, no new aliases added)" {
        # We control only default engine aliases (Source -eq "") and aliases from following default loaded modules
        $moduleList = @("Microsoft.PowerShell.Utility", "Microsoft.PowerShell.Management")
        $currentAliasList = Get-Alias | Where-Object { $_.Source -eq "" -or $moduleList -contains $_.Source } | Select-Object -ExpandProperty DisplayName

        Compare-Object -ReferenceObject $currentAliasList -DifferenceObject $aliaslist | Should Be $null
    }
}
