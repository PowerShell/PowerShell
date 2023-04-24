# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Verify aliases and cmdlets" -Tags "CI" {
    BeforeAll {
        function ConvertTo-Hashtable {
            [CmdletBinding()]
            param ([Parameter(ValueFromPipeline=$true)][psobject]$o)
            PROCESS {
                $pNames = $o.psobject.properties.name
                $ht = @{}
                foreach($pName in $pNames) {
                    $ht[$pName] = $o.$pName
                }
                $ht
            }
        }

        $FullCLR = !$IsCoreCLR
        $CoreWindows = $IsCoreCLR -and $IsWindows
        $CoreUnix = $IsCoreCLR -and !$IsWindows
        $isPreview = $PSVersionTable.GitCommitId.Contains("preview")
        if ($IsWindows) {
            $configPath = Join-Path -Path $env:USERPROFILE -ChildPath 'Documents' -AdditionalChildPath 'PowerShell'
        }
        else {
            $configPath = Join-Path -Path $env:HOME -ChildPath '.config' -AdditionalChildPath 'powershell'
        }

        if (Test-Path "$configPath/powershell.config.json") {
            Move-Item -Path "$configPath/powershell.config.json"  -Destination "$configPath/powershell.config.json.backup"
        }

        $AllScope = '[System.Management.Automation.ScopedItemOptions]::AllScope'
        $ReadOnly = '[System.Management.Automation.ScopedItemOptions]::ReadOnly'
        $None     = '[System.Management.Automation.ScopedItemOptions]::None'

        $commandString = @"
"CommandType",  "Name",                             "Definition",                       "Present",                                      "ReadOnlyOption",       "AllScopeOption",       "ConfirmImpact"
"Alias",        "%",                                "ForEach-Object",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "AllScope",             ""
"Alias",        "?",                                "Where-Object",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "AllScope",             ""
"Alias",        "ac",                               "Add-Content",                      $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "asnp",                             "Add-PSSnapIn",                     $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "cat",                              "Get-Content",                      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "cd",                               "Set-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "CFS",                              "ConvertFrom-String",               $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "chdir",                            "Set-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "clc",                              "Clear-Content",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "clear",                            "Clear-Host",                       $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "clhy",                             "Clear-History",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "cli",                              "Clear-Item",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "clp",                              "Clear-ItemProperty",               $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "cls",                              "Clear-Host",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "clv",                              "Clear-Variable",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "cnsn",                             "Connect-PSSession",                $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "compare",                          "Compare-Object",                   $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "copy",                             "Copy-Item",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "cp",                               "Copy-Item",                        $($FullCLR -or $CoreWindows              ),     "",                     "AllScope",             ""
"Alias",        "cpi",                              "Copy-Item",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "cpp",                              "Copy-ItemProperty",                $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "curl",                             "Invoke-WebRequest",                $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "cvpa",                             "Convert-Path",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "dbp",                              "Disable-PSBreakpoint",             $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "del",                              "Remove-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "diff",                             "Compare-Object",                   $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "dir",                              "Get-ChildItem",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "dnsn",                             "Disconnect-PSSession",             $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "ebp",                              "Enable-PSBreakpoint",              $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "echo",                             "Write-Output",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "epal",                             "Export-Alias",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "epcsv",                            "Export-Csv",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "epsn",                             "Export-PSSession",                 $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "erase",                            "Remove-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "etsn",                             "Enter-PSSession",                  $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "exsn",                             "Exit-PSSession",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "fc",                               "Format-Custom",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "fhx",                              "Format-Hex",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "fl",                               "Format-List",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "foreach",                          "ForEach-Object",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "AllScope",             ""
"Alias",        "ft",                               "Format-Table",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "fw",                               "Format-Wide",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gal",                              "Get-Alias",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gbp",                              "Get-PSBreakpoint",                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gc",                               "Get-Content",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gcai",                             "Get-CimAssociatedInstance",        $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "gcb",                              "Get-Clipboard",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "gci",                              "Get-ChildItem",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gcim",                             "Get-CimInstance",                  $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "gcls",                             "Get-CimClass",                     $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "gcm",                              "Get-Command",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gcms",                             "Get-CimSession",                   $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "gcs",                              "Get-PSCallStack",                  $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gdr",                              "Get-PSDrive",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gerr",                             "Get-Error",                        $(             $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ghy",                              "Get-History",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gi",                               "Get-Item",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gin",                              "Get-ComputerInfo",                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "gjb",                              "Get-Job",                          $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "gl",                               "Get-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gm",                               "Get-Member",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gmo",                              "Get-Module",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gp",                               "Get-ItemProperty",                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gps",                              "Get-Process",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gpv",                              "Get-ItemPropertyValue",            $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "group",                            "Group-Object",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gsn",                              "Get-PSSession",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "gsnp",                             "Get-PSSnapIn",                     $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "gsv",                              "Get-Service",                      $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "gtz",                              "Get-TimeZone",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "gu",                               "Get-Unique",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gv",                               "Get-Variable",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "gwmi",                             "Get-WmiObject",                    $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "h",                                "Get-History",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "history",                          "Get-History",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "icim",                             "Invoke-CimMethod",                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "icm",                              "Invoke-Command",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "iex",                              "Invoke-Expression",                $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ihy",                              "Invoke-History",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ii",                               "Invoke-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ipal",                             "Import-Alias",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ipcsv",                            "Import-Csv",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ipmo",                             "Import-Module",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ipsn",                             "Import-PSSession",                 $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "irm",                              "Invoke-RestMethod",                $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ise",                              "powershell_ise.exe",               $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "iwmi",                             "Invoke-WMIMethod",                 $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "iwr",                              "Invoke-WebRequest",                $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "kill",                             "Stop-Process",                     $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "lp",                               "Out-Printer",                      $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "ls",                               "Get-ChildItem",                    $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "man",                              "help",                             $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "md",                               "mkdir",                            $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "measure",                          "Measure-Object",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "mi",                               "Move-Item",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "mount",                            "New-PSDrive",                      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "move",                             "Move-Item",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "mp",                               "Move-ItemProperty",                $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "mv",                               "Move-Item",                        $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "nal",                              "New-Alias",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ncim",                             "New-CimInstance",                  $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "ncms",                             "New-CimSession",                   $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "ncso",                             "New-CimSessionOption",             $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "ndr",                              "New-PSDrive",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ni",                               "New-Item",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "nmo",                              "New-Module",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "npssc",                            "New-PSSessionConfigurationFile",   $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "nsn",                              "New-PSSession",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "nv",                               "New-Variable",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "nwsn",                             "New-PSWorkflowSession",            $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "ogv",                              "Out-GridView",                     $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "oh",                               "Out-Host",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "popd",                             "Pop-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "ps",                               "Get-Process",                      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "pushd",                            "Push-Location",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "AllScope",             ""
"Alias",        "pwd",                              "Get-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "r",                                "Invoke-History",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "rbp",                              "Remove-PSBreakpoint",              $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rcie",                             "Register-CimIndicationEvent",      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "rcim",                             "Remove-CimInstance",               $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "rcjb",                             "Receive-Job",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "rcms",                             "Remove-CimSession",                $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "rcsn",                             "Receive-PSSession",                $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rd",                               "Remove-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "rdr",                              "Remove-PSDrive",                   $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "ren",                              "Rename-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "ri",                               "Remove-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rjb",                              "Remove-Job",                       $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "rm",                               "Remove-Item",                      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "rmdir",                            "Remove-Item",                      $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "rmo",                              "Remove-Module",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rni",                              "Rename-Item",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rnp",                              "Rename-ItemProperty",              $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rp",                               "Remove-ItemProperty",              $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rsn",                              "Remove-PSSession",                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "rsnp",                             "Remove-PSSnapin",                  $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "rujb",                             "Resume-Job",                       $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "rv",                               "Remove-Variable",                  $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rvpa",                             "Resolve-Path",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "rwmi",                             "Remove-WMIObject",                 $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "sajb",                             "Start-Job",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "sal",                              "Set-Alias",                        $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "saps",                             "Start-Process",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "sasv",                             "Start-Service",                    $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "sbp",                              "Set-PSBreakpoint",                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "sc",                               "Set-Content",                      $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "scb",                              "Set-Clipboard",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "scim",                             "Set-CimInstance",                  $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "select",                           "Select-Object",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "AllScope",             ""
"Alias",        "set",                              "Set-Variable",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "shcm",                             "Show-Command",                     $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "si",                               "Set-Item",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "sl",                               "Set-Location",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "sleep",                            "Start-Sleep",                      $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "sls",                              "Select-String",                    $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "sort",                             "Sort-Object",                      $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "sp",                               "Set-ItemProperty",                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "spjb",                             "Stop-Job",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "spps",                             "Stop-Process",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "spsv",                             "Stop-Service",                     $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "start",                            "Start-Process",                    $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "stz",                              "Set-TimeZone",                     $($FullCLR -or $CoreWindows              ),     "",                     "",                     ""
"Alias",        "sujb",                             "Suspend-Job",                      $($FullCLR                               ),     "",                     "",                     ""
"Alias",        "sv",                               "Set-Variable",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "",                     ""
"Alias",        "swmi",                             "Set-WMIInstance",                  $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "tee",                              "Tee-Object",                       $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Alias",        "trcm",                             "Trace-Command",                    $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "type",                             "Get-Content",                      $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "wget",                             "Invoke-WebRequest",                $($FullCLR                               ),     "ReadOnly",             "",                     ""
"Alias",        "where",                            "Where-Object",                     $($FullCLR -or $CoreWindows -or $CoreUnix),     "ReadOnly",             "AllScope",             ""
"Alias",        "wjb",                              "Wait-Job",                         $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     ""
"Alias",        "write",                            "Write-Output",                     $($FullCLR -or $CoreWindows              ),     "ReadOnly",             "",                     ""
"Cmdlet",       "Add-Computer",                     "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Add-Content",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Add-History",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Add-Member",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Add-PSSnapin",                     "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Add-Type",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Checkpoint-Computer",              "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Clear-Content",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Clear-EventLog",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Clear-History",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Clear-Item",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Clear-ItemProperty",               "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Clear-RecycleBin",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "High"
"Cmdlet",       "Clear-Variable",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Compare-Object",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Complete-Transaction",             "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Connect-PSSession",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Connect-WSMan",                    "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-Csv",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-Json",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-Markdown",             "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-SddlString",           "",                                 $(             $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-SecureString",         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertFrom-String",               "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "ConvertFrom-StringData",           "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Convert-Path",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Convert-String",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "ConvertTo-Csv",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertTo-Html",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertTo-Json",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertTo-SecureString",           "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ConvertTo-Xml",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Copy-Item",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Copy-ItemProperty",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Debug-Job",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Debug-Process",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Debug-Runspace",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Disable-ComputerRestore",          "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Disable-ExperimentalFeature",      "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Disable-PSBreakpoint",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Disable-PSRemoting",               "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Disable-PSSessionConfiguration",   "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Low"
"Cmdlet",       "Disable-RunspaceDebug",            "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Disable-WSManCredSSP",             "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Disconnect-PSSession",             "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Disconnect-WSMan",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Enable-ComputerRestore",           "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Enable-ExperimentalFeature",       "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Enable-PSBreakpoint",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Enable-PSRemoting",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Enable-PSSessionConfiguration",    "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Enable-RunspaceDebug",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Enable-WSManCredSSP",              "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Enter-PSHostProcess",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Enter-PSSession",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Exit-PSHostProcess",               "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Exit-PSSession",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Export-Alias",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Export-Clixml",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Export-Console",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Export-Counter",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Export-Csv",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Export-FormatData",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Export-ModuleMember",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Export-PSSession",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "ForEach-Object",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "Format-Custom",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Format-Default",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Format-Hex",                       "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Format-List",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Format-Table",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Format-Wide",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Acl",                          "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-Alias",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-AuthenticodeSignature",        "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-ChildItem",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-CimAssociatedInstance",        "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-CimClass",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-CimInstance",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-CimSession",                   "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-Clipboard",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-CmsMessage",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Command",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ComputerInfo",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-ComputerRestorePoint",         "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-Content",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ControlPanelItem",             "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-Counter",                      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-Credential",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Culture",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Date",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Error",                        "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Event",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-EventLog",                     "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-EventSubscriber",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ExecutionPolicy",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ExperimentalFeature",          "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-FileHash",                     "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-FormatData",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Help",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-History",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Host",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-HotFix",                       "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-Item",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ItemProperty",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-ItemPropertyValue",            "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Job",                          "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Location",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-MarkdownOption",               "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Member",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Module",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PfxCertificate",               "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Process",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSBreakpoint",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSCallStack",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSDrive",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSHostProcessInfo",            "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSProvider",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSSession",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-PSSessionCapability",          "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-PSSessionConfiguration",       "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-PSSnapin",                     "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-Random",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Runspace",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-RunspaceDebug",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Service",                      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-TimeZone",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-TraceSource",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Transaction",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-TypeData",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Uptime",                       "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-UICulture",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Unique",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Variable",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-Verb",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Get-WinEvent",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-WmiObject",                    "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Get-WSManCredSSP",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Get-WSManInstance",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Group-Object",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-Alias",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Import-Clixml",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-Counter",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Import-Csv",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-LocalizedData",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-Module",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-PowerShellDataFile",        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Import-PSSession",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Invoke-CimMethod",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Invoke-Command",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Invoke-Expression",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Invoke-History",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Invoke-Item",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Invoke-RestMethod",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Invoke-WebRequest",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Invoke-WmiMethod",                 "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Invoke-WSManAction",               "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Join-Path",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Join-String",                      "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Limit-EventLog",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Measure-Command",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Measure-Object",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Move-Item",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Move-ItemProperty",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "New-Alias",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "New-CimInstance",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "New-CimSession",                   "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "New-CimSessionOption",             "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "New-Event",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-EventLog",                     "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "New-FileCatalog",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "New-GUID",                         "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-Item",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "New-ItemProperty",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "New-Module",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-ModuleManifest",               "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "New-Object",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-PSDrive",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "New-PSRoleCapabilityFile",         "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-PSSession",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-PSSessionConfigurationFile",   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-PSSessionOption",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-PSTransportOption",            "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-Service",                      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "New-TemporaryFile",                "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "New-TimeSpan",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "New-Variable",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "New-WebServiceProxy",              "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "New-WinEvent",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "New-WSManInstance",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "New-WSManSessionOption",           "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Out-Default",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Out-File",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Out-GridView",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Out-Host",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Out-LineOutput",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Out-Null",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Out-Printer",                      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Out-String",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Pop-Location",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Protect-CmsMessage",               "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Push-Location",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Read-Host",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Receive-Job",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Receive-PSSession",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Low"
"Cmdlet",       "Register-ArgumentCompleter",       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Register-CimIndicationEvent",      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Register-EngineEvent",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Register-ObjectEvent",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Register-PSSessionConfiguration",  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Register-WmiEvent",                "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Remove-Alias",                     "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Remove-CimInstance",               "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-CimSession",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-Computer",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Remove-Event",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-EventLog",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Remove-Item",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-ItemProperty",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-Job",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-Module",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-PSBreakpoint",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-PSDrive",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-PSSession",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-PSSnapin",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Remove-Service",                   "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-TypeData",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-Variable",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Remove-WmiObject",                 "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Remove-WSManInstance",             "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Rename-Computer",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Rename-Item",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Rename-ItemProperty",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Reset-ComputerMachinePassword",    "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Resolve-Path",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Restart-Computer",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Restart-Service",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Restore-Computer",                 "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Resume-Job",                       "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Resume-Service",                   "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Save-Help",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Select-Object",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Select-String",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Select-Xml",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Send-MailMessage",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-Acl",                          "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Alias",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-AuthenticodeSignature",        "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-CimInstance",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Clipboard",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Content",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Date",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-ExecutionPolicy",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Item",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-ItemProperty",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Location",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-MarkdownOption",               "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-PSBreakpoint",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-PSDebug",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-PSSessionConfiguration",       "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-Service",                      "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-StrictMode",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-TimeZone",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Set-TraceSource",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Set-Variable",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Set-WmiInstance",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Set-WSManInstance",                "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Set-WSManQuickConfig",             "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Show-Command",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Show-ControlPanelItem",            "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Show-EventLog",                    "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Show-Markdown",                    "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Sort-Object",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Split-Path",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Start-Job",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Start-Process",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Start-Service",                    "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Start-Sleep",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Start-Transaction",                "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Start-Transcript",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Stop-Computer",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Stop-Job",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Stop-Process",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Stop-Service",                     "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Stop-Transcript",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Suspend-Job",                      "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Suspend-Service",                  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Switch-Process",                   "",                                 $(                              $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Tee-Object",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Test-Connection",                  "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Test-ComputerSecureChannel",       "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Test-FileCatalog",                 "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Medium"
"Cmdlet",       "Test-Json",                        "",                                 $(             $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Test-ModuleManifest",              "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Test-Path",                        "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Test-PSSessionConfigurationFile",  "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Test-WSMan",                       "",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "None"
"Cmdlet",       "Trace-Command",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Unblock-File",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Undo-Transaction",                 "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Unprotect-CmsMessage",             "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Unregister-Event",                 "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Unregister-PSSessionConfiguration","",                                 $($FullCLR -or $CoreWindows              ),     "",                     "",                     "Low"
"Cmdlet",       "Update-FormatData",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "Update-Help",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Medium"
"Cmdlet",       "Update-List",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Update-TypeData",                  "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "Low"
"Cmdlet",       "Use-Transaction",                  "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Wait-Debugger",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Wait-Event",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Wait-Job",                         "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Wait-Process",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Where-Object",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Debug",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Error",                      "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-EventLog",                   "",                                 $($FullCLR                               ),     "",                     "",                     ""
"Cmdlet",       "Write-Host",                       "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Information",                "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Output",                     "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Progress",                   "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Verbose",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"Cmdlet",       "Write-Warning",                    "",                                 $($FullCLR -or $CoreWindows -or $CoreUnix),     "",                     "",                     "None"
"@

            # We control only default engine aliases (Source -eq "") and aliases from following default loaded modules "
            # We control only default engine Cmdlets (Source -eq "") and Cmdlets from following default loaded modules
            $moduleList = @(
                    "Microsoft.PowerShell.Utility",
                    "Microsoft.PowerShell.Management",
                    "Microsoft.PowerShell.Security",
                    "Microsoft.PowerShell.Host",
                    "Microsoft.PowerShell.Diagnostics",
                    "Microsoft.WSMan.Management",
                    "Microsoft.PowerShell.Core",
                    "CimCmdlets"
                    )
            $getAliases = {
                param($moduleList)

                if ($moduleList -is [string]) {
                    $moduleList = $moduleList | ConvertFrom-Json
                }

                Import-Module -Name $moduleList -ErrorAction SilentlyContinue
                Get-Alias | Where-Object { $_.Source -eq "" -or $moduleList -contains $_.Source }
            }

            $getCommands = {
                param($moduleList)

                if ($moduleList -is [string]) {
                    $moduleList = $moduleList | ConvertFrom-Json
                }

                Import-Module -Name $moduleList -ErrorAction SilentlyContinue
                Get-Command -CommandType Cmdlet | Where-Object { $moduleList -contains $_.Source }
            }

            # On Preview releases, Experimental Features may add new cmdlets/aliases, so we get cmdlets/aliases with features disabled
            if ($isPreview) {
                $emptyConfigPath = Join-Path -Path $TestDrive -ChildPath "test.config.json"
                Set-Content -Path $emptyConfigPath -Value "" -Force -ErrorAction Stop
                $currentAliasList = & "$PSHOME/pwsh" -NoProfile -OutputFormat XML -SettingsFile $emptyConfigPath -Command $getAliases -args ($moduleList | ConvertTo-Json)
                $currentCmdletList = & "$PSHOME/pwsh" -NoProfile -OutputFormat XML -SettingsFile $emptyConfigPath -Command $getCommands -args ($moduleList | ConvertTo-Json)
            }
            else {
                $currentAliasList = & $getAliases $moduleList
                $currentCmdletList = & $getCommands $moduleList
            }

            $commandList  = $commandString | ConvertFrom-Csv -Delimiter ","
            $commandHashTableList = $commandList.Where({$_.Present -eq "True" -and $_.CommandType -eq "Cmdlet"}) | ConvertTo-Hashtable

            $aliasFullList  = $commandList | Where-Object { $_.Present -eq "True" -and $_.CommandType -eq "Alias"  }

            $AllScopeOption = [System.Management.Automation.ScopedItemOptions]::AllScope
            $ReadOnlyOption = [System.Management.Automation.ScopedItemOptions]::ReadOnly
    }

    AfterAll {
        if (Test-Path "$configPath/powershell.config.json.backup") {
            Move-Item -Path "$configPath/powershell.config.json.backup"  -Destination "$configPath/powershell.config.json"
        }
    }

    It "All approved aliases present (no new aliases added, no aliases removed)" {
        $observedAliases = $currentAliasList.ForEach({"{0}:{1}" -f $_.Name,$_.Definition}) | Sort-Object
        $expectedAliases = $aliasFullList.ForEach({"{0}:{1}" -f $_.Name, $_.Definition}) | Sort-Object
        $observedAliases | Should -Be $expectedAliases
    }

    It "All approved aliases have the correct 'AllScope' option" {
        $expectedAllScopeAliases = $aliasFullList.Where({$_.AllScopeOption -eq "AllScope"}).ForEach({"{0}:{1}" -f $_.Name, $_.Definition}) | Sort-Object
        $observedAllScopeAliases = $currentAliasList.Where({($_.Options -as [System.Management.Automation.ScopedItemOptions]) -band $AllScopeOption}).Foreach({"{0}:{1}" -f $_.Name, $_.Definition}) | Sort-Object
        $observedAllScopeAliases | Should -Be $expectedAllScopeAliases
    }

    It "All approved aliases have the correct 'ReadOnly' option" {
        $expectedReadOnlyAliases = $aliasFullList.Where({$_.ReadOnlyOption -eq "ReadOnly"}).ForEach({"{0}:{1}" -f $_.Name, $_.Definition}) | Sort-Object
        $observedReadOnlyAliases = $currentAliasList.Where({($_.Options -as [System.Management.Automation.ScopedItemOptions]) -band $ReadOnlyOption}).ForEach({"{0}:{1}" -f $_.Name, $_.Definition}) | Sort-Object
        $observedReadOnlyAliases | Should -Be $expectedReadOnlyAliases
    }

    It "All approved Cmdlets present (no new Cmdlets added, no Cmdlets removed)" {
        $observedCmdletNames = $currentCmdletList.ForEach({"{0}" -f $_.Name}) | Sort-Object
        $expectedCmdletNames = $commandHashTableList.ForEach({"{0}" -f $_.Name}) | Sort-Object
        $observedCmdletNames | Should -Be $expectedCmdletNames
    }

    It "'<Name>' Cmdlet should have the correct ConfirmImpact '<ConfirmImpact>'" -TestCases $commandHashtableList {
        param ( $Name, $ConfirmImpact )
        # retrieve again because we may have serialized the commandinfo
        $cmdlet = Get-Command $Name
        $cmdletAttribute = $cmdlet.ImplementingType.GetCustomAttributes($true).Where({$_ -is [System.Management.Automation.CmdletAttribute]})
        $impact = $cmdletAttribute.ConfirmImpact
        $impact | Should -Be $ConfirmImpact
    }
}

Describe "PATHEXT defaults" -Tags 'CI' {
    It "PATHEXT contains .CPL" -Skip:(!$IsWindows) {
        $env:PATHEXT.Split(";") | Should -Contain ".CPL"
    }
}
