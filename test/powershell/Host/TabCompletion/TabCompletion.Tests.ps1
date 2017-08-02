Describe "TabCompletion" -Tags CI {
    It 'Should complete Command' {
        $res = TabExpansion2 -inputScript 'Get-Com' -cursorColumn 'Get-Com'.Length
        $res.CompletionMatches[0].CompletionText | Should be Get-Command
    }

    It 'Should complete native exe' -Skip:(!$IsWindows) {
        $res = TabExpansion2 -inputScript 'notep' -cursorColumn 'notep'.Length
        $res.CompletionMatches[0].CompletionText | Should be notepad.exe
    }

    It 'Should complete dotnet method' {
        $res = TabExpansion2 -inputScript '(1).ToSt' -cursorColumn '(1).ToSt'.Length
        $res.CompletionMatches[0].CompletionText | Should be 'ToString('
    }

    It 'Should complete Magic foreach' {
        $res = TabExpansion2 -inputScript '(1..10).Fo' -cursorColumn '(1..10).Fo'.Length
        $res.CompletionMatches[0].CompletionText | Should be 'Foreach('
    }

    It "Should complete Magic where" {
        $res = TabExpansion2 -inputScript '(1..10).wh' -cursorColumn '(1..10).wh'.Length
        $res.CompletionMatches[0].CompletionText | Should be 'Where('
    }

    It 'Should complete types' {
        $res = TabExpansion2 -inputScript '[pscu' -cursorColumn '[pscu'.Length
        $res.CompletionMatches[0].CompletionText | Should be 'pscustomobject'
    }

    It 'Should complete namespaces' {
        $res = TabExpansion2 -inputScript 'using namespace Sys' -cursorColumn 'using namespace Sys'.Length
        $res.CompletionMatches[0].CompletionText | Should be 'System'
    }

    It 'Should complete format-table hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Table @{ ' -cursorColumn 'Get-ChildItem | Format-Table @{ '.Length
        $res.CompletionMatches.Count | Should Be 5
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should Be 'Alignment Expression FormatString Label Width'
    }


    It 'Should complete format-* hashtable on GroupBy: <cmd>' -TestCases (
        @{cmd = 'Format-Table'},
        @{cmd = 'Format-List'},
        @{cmd = 'Format-Wide'},
        @{cmd = 'Format-Custom'}
    ) {
        param($cmd)
        $res = TabExpansion2 -inputScript "Get-ChildItem | $cmd -GroupBy @{ " -cursorColumn "Get-ChildItem | $cmd -GroupBy @{ ".Length
        $res.CompletionMatches.Count | Should Be 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should Be 'Expression FormatString Label'
    }

    It 'Should complete format-list hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-List @{ ' -cursorColumn 'Get-ChildItem | Format-List @{ '.Length
        $res.CompletionMatches.Count | Should Be 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should Be 'Expression FormatString Label'
    }

    It 'Should complete format-wide hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Wide @{ ' -cursorColumn 'Get-ChildItem | Format-Wide @{ '.Length
        $res.CompletionMatches.Count | Should Be 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should Be 'Expression FormatString'
    }

    It 'Should complete format-custom  hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Format-Custom @{ ' -cursorColumn 'Get-ChildItem | Format-Custom @{ '.Length
        $res.CompletionMatches.Count | Should Be 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' ' | Should Be 'Depth Expression'
    }

    It 'Should complete Select-Object  hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Select-Object @{ ' -cursorColumn 'Get-ChildItem | Select-Object @{ '.Length
        $res.CompletionMatches.Count | Should Be 2
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' '| Should Be 'Expression Name'
    }

    It 'Should complete Sort-Object  hashtable' {
        $res = TabExpansion2 -inputScript 'Get-ChildItem | Sort-Object @{ ' -cursorColumn 'Get-ChildItem | Sort-Object @{ '.Length
        $res.CompletionMatches.Count | Should Be 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' '| Should Be 'Ascending Descending Expression'
    }

    It 'Should complete New-Object  hashtable' {
        class X {
            $A
            $B
            $C
        }
        $res = TabExpansion2 -inputScript 'New-Object -TypeName X -Property @{ ' -cursorColumn 'New-Object -TypeName X -Property @{ '.Length
        $res.CompletionMatches.Count | Should Be 3
        $res.CompletionMatches.CompletionText -join ' ' | Should Be 'A B C'
    }

    It 'Should complete "Get-Process -Id " with Id and name in tooltip' {
        Set-StrictMode -Version latest
        $cmd = 'Get-Process -Id '
        [System.Management.Automation.CommandCompletion]$res = TabExpansion2 -inputScript $cmd  -cursorColumn $cmd.Length
        $res.CompletionMatches[0].CompletionText -match '^\d+$' | Should be true
        $res.CompletionMatches[0].ListItemText -match '^\d+ -' | Should be true
        $res.CompletionMatches[0].ToolTip -match '^\d+ -' | Should be true
    }

    It 'Should complete keyword' -skip {
        $res = TabExpansion2 -inputScript 'using nam' -cursorColumn 'using nam'.Length
        $res.CompletionMatches[0].CompletionText | Should Be 'namespace'
    }

    It 'Should complete about help topic' {

        $aboutHelpPath = Join-Path $PSHOME (Get-Culture).Name

        ## If help content does not exist, tab completion will not work. So update it first.
        if(-not (Test-Path (Join-Path $aboutHelpPath "about_Splatting.help.txt")))
        {
            Update-Help -Force -ErrorAction SilentlyContinue
        }

        $res = TabExpansion2 -inputScript 'get-help about_spla' -cursorColumn 'get-help about_spla'.Length
        $res.CompletionMatches.Count | Should Be 1
        $res.CompletionMatches[0].CompletionText | Should BeExactly 'about_Splatting'
    }

    Context NativeCommand {
        BeforeAll {
            $nativeCommand = (Get-Command -CommandType Application -TotalCount 1).Name
        }
        It 'Completes native commands with -' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-') {
                    return "-flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand -"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches.CompletionText | Should Be "-flag"
        }

        It 'Completes native commands with --' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '--') {
                    return "--flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand --"
            $res = TabExpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches.CompletionText | Should Be "--flag"
        }

        It 'Completes native commands with --f' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '--f') {
                    return "--flag"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand --f"
            $res = TaBexpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches.CompletionText | Should Be "--flag"
        }

        It 'Completes native commands with -o' {
            Register-ArgumentCompleter -Native -CommandName $nativeCommand -ScriptBlock {
                param($wordToComplete, $ast, $cursorColumn)
                if ($wordToComplete -eq '-o') {
                    return "-option"
                }
                else {
                    return "unexpected wordtocomplete"
                }
            }
            $line = "$nativeCommand -o"
            $res = TaBexpansion2 -inputScript $line -cursorColumn $line.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches.CompletionText | Should Be "-option"
        }
    }

    It 'Should complete "Export-Counter -FileFormat" with available output formats' -Pending {
        $res = TabExpansion2 -inputScript 'Export-Counter -FileFormat ' -cursorColumn 'Export-Counter -FileFormat '.Length
        $res.CompletionMatches.Count | Should Be 3
        $completionText = $res.CompletionMatches.CompletionText | Sort-Object
        $completionText -join ' '| Should Be 'blg csv tsv'
    }

    Context "File name completion" {
        BeforeAll {
            $tempDir = Join-Path -Path $TestDrive -ChildPath "baseDir"
            $oneSubDir = Join-Path -Path $tempDir -ChildPath "oneSubDir"
            $oneSubDirPrime = Join-Path -Path $tempDir -ChildPath "prime"
            $twoSubDir = Join-Path -Path $oneSubDir -ChildPath "twoSubDir"
            $separator = [System.IO.Path]::DirectorySeparatorChar

            New-Item -Path $tempDir -ItemType Directory -Force > $null
            New-Item -Path $oneSubDir -ItemType Directory -Force > $null
            New-Item -Path $oneSubDirPrime -ItemType Directory -Force > $null
            New-Item -Path $twoSubDir -ItemType Directory -Force > $null

            $testCases = @(
                @{ inputStr = "ab"; name = "abc"; localExpected = ".${separator}abc"; oneSubExpected = "..${separator}abc"; twoSubExpected = "..${separator}..${separator}abc" }
                @{ inputStr = "asaasas"; name = "asaasas!popee"; localExpected = ".${separator}asaasas!popee"; oneSubExpected = "..${separator}asaasas!popee"; twoSubExpected = "..${separator}..${separator}asaasas!popee" }
                @{ inputStr = "asaasa"; name = "asaasas!popee"; localExpected = ".${separator}asaasas!popee"; oneSubExpected = "..${separator}asaasas!popee"; twoSubExpected = "..${separator}..${separator}asaasas!popee" }
                @{ inputStr = "bbbbbbbbbb"; name = 'bbbbbbbbbb`'; localExpected = "& '.${separator}bbbbbbbbbb``'"; oneSubExpected = "& '..${separator}bbbbbbbbbb``'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbbbbb``'" }
                @{ inputStr = "bbbbbbbbb"; name = "bbbbbbbbb#"; localExpected = ".${separator}bbbbbbbbb#"; oneSubExpected = "..${separator}bbbbbbbbb#"; twoSubExpected = "..${separator}..${separator}bbbbbbbbb#" }
                @{ inputStr = "bbbbbbbb"; name = "bbbbbbbb{"; localExpected = "& '.${separator}bbbbbbbb{'"; oneSubExpected = "& '..${separator}bbbbbbbb{'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbbb{'" }
                @{ inputStr = "bbbbbbb"; name = "bbbbbbb}"; localExpected = "& '.${separator}bbbbbbb}'"; oneSubExpected = "& '..${separator}bbbbbbb}'"; twoSubExpected = "& '..${separator}..${separator}bbbbbbb}'" }
                @{ inputStr = "bbbbbb"; name = "bbbbbb("; localExpected = "& '.${separator}bbbbbb('"; oneSubExpected = "& '..${separator}bbbbbb('"; twoSubExpected = "& '..${separator}..${separator}bbbbbb('" }
                @{ inputStr = "bbbbb"; name = "bbbbb)"; localExpected = "& '.${separator}bbbbb)'"; oneSubExpected = "& '..${separator}bbbbb)'"; twoSubExpected = "& '..${separator}..${separator}bbbbb)'" }
                @{ inputStr = "bbbb"; name = "bbbb$"; localExpected = "& '.${separator}bbbb$'"; oneSubExpected = "& '..${separator}bbbb$'"; twoSubExpected = "& '..${separator}..${separator}bbbb$'" }
                @{ inputStr = "bbb"; name = "bbb'"; localExpected = "& '.${separator}bbb'''"; oneSubExpected = "& '..${separator}bbb'''"; twoSubExpected = "& '..${separator}..${separator}bbb'''" }
                @{ inputStr = "bb"; name = "bb,"; localExpected = "& '.${separator}bb,'"; oneSubExpected = "& '..${separator}bb,'"; twoSubExpected = "& '..${separator}..${separator}bb,'" }
                @{ inputStr = "b"; name = "b;"; localExpected = "& '.${separator}b;'"; oneSubExpected = "& '..${separator}b;'"; twoSubExpected = "& '..${separator}..${separator}b;'" }
            )

            try {
                Push-Location -Path $tempDir
                foreach ($entry in $testCases) {
                    New-Item -Path $tempDir -Name $entry.name -ItemType File -ErrorAction SilentlyContinue > $null
                }
            } finally {
                Pop-Location
            }
        }

        AfterAll {
            Remove-Item -Path $tempDir -Recurse -Force
        }

        AfterEach {
            Pop-Location
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param ($inputStr, $localExpected)

            Push-Location -Path $tempDir
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $localExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\'" -TestCases $testCases {
            param ($inputStr, $oneSubExpected)

            Push-Location -Path $oneSubDir
            $inputStr = "..\${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $oneSubExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\..\'" -TestCases $testCases {
            param ($inputStr, $twoSubExpected)

            Push-Location -Path $twoSubDir
            $inputStr = "../../${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $twoSubExpected
        }

        It "Input '<inputStr>' should successfully complete with relative path '..\..\..\ba*\'" -TestCases $testCases {
            param ($inputStr, $twoSubExpected)

            Push-Location -Path $twoSubDir
            $inputStr = "..\..\..\ba*\${inputStr}"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $twoSubExpected
        }

        It "Test relative path" {
            Push-Location -Path $oneSubDir
            $beforeTab = "twoSubDir/../../pri"
            $afterTab = "..${separator}prime"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }

        It "Test path with both '\' and '/'" {
            Push-Location -Path $twoSubDir
            $beforeTab = "..\../..\ba*/ab"
            $afterTab = "..${separator}..${separator}abc"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should Be 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }
    }

    Context "Cmdlet name completion" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "get-c*item"; expected = "get-childitem" }
                @{ inputStr = "set-alia?"; expected = "set-alias" }
                @{ inputStr = "s*-alias"; expected = "set-alias" }
                @{ inputStr = "se*-alias"; expected = "set-alias" }
                @{ inputStr = "set-al"; expected = "set-alias" }
                @{ inputStr = "set-a?i"; expected = "set-alias" }
                @{ inputStr = "set-?lias"; expected = "set-alias" }
                @{ inputStr = "get-*ditem"; expected = "get-childitem" }
                @{ inputStr = "Microsoft.PowerShell.Management\get-c*item"; expected = "Microsoft.PowerShell.Management\get-childitem" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-alia?"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\s*-alias"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\se*-alias"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-al"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-a?i"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Utility\set-?lias"; expected = "Microsoft.PowerShell.Utility\set-alias" }
                @{ inputStr = "Microsoft.PowerShell.Management\get-*ditem"; expected = "Microsoft.PowerShell.Management\get-childitem" }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }
    }

    Context "Miscellaneous completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "get-childitem -"; expected = "-Path"; setup = $null }
                @{ inputStr = "get-childitem -Fil"; expected = "-Filter"; setup = $null }
                @{ inputStr = '$arg'; expected = '$args'; setup = $null }
                @{ inputStr = '$args.'; expected = 'Count'; setup = $null }
                @{ inputStr = '$host.UI.Ra'; expected = 'RawUI'; setup = $null }
                @{ inputStr = '$host.UI.WriteD'; expected = 'WriteDebugLine('; setup = $null }
                @{ inputStr = '$MaximumHistoryCount.'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '$A=[datetime]::now;$A.'; expected = 'Date'; setup = $null }
                @{ inputStr = '$x= gps powershell;$x.*pm'; expected = 'NPM'; setup = $null }
                @{ inputStr = 'function write-output {param($abcd) $abcd};Write-Output -a'; expected = '-abcd'; setup = $null }
                @{ inputStr = 'function write-output {param($abcd) $abcd};Microsoft.PowerShell.Utility\Write-Output -'; expected = '-InputObject'; setup = $null }
                @{ inputStr = '[math]::Co'; expected = 'Cos('; setup = $null }
                @{ inputStr = '[math]::PI.GetT'; expected = 'GetType('; setup = $null }
                @{ inputStr = '[math]'; expected = '::E'; setup = $null }
                @{ inputStr = '[math].'; expected = 'Assembly'; setup = $null }
                @{ inputStr = '[math].G'; expected = 'GenericParameterAttributes'; setup = $null }
                @{ inputStr = '[Environment+specialfolder]::App'; expected = 'ApplicationData'; setup = $null }
                @{ inputStr = 'icm {get-pro'; expected = 'Get-Process'; setup = $null }
                @{ inputStr = 'write-ouput (get-pro'; expected = 'Get-Process'; setup = $null }
                @{ inputStr = 'iex "get-pro'; expected = '"Get-Process"'; setup = $null }
                @{ inputStr = '$variab'; expected = '$variableA'; setup = { $variableB = 2; $variableA = 1 } }
                @{ inputStr = 'a -'; expected = '-keys'; setup = { function a {param($keys) $a} } }
                @{ inputStr = 'Get-Content -Li'; expected = '-LiteralPath'; setup = $null }
                @{ inputStr = 'New-Item -W'; expected = '-WhatIf'; setup = $null }
                @{ inputStr = 'Get-Alias gs'; expected = 'gsn'; setup = $null }
                @{ inputStr = 'Get-Alias -Definition cd'; expected = 'cd..'; setup = $null }
                @{ inputStr = 'remove-psdrive fun'; expected = 'Function'; setup = $null }
                @{ inputStr = 'new-psdrive -PSProvider fi'; expected = 'FileSystem'; setup = $null }
                @{ inputStr = 'Get-PSDrive -PSProvider En'; expected = 'Environment'; setup = $null }
                @{ inputStr = 'remove-psdrive fun'; expected = 'Function'; setup = $null }
                @{ inputStr = 'get-psprovider ali'; expected = 'Alias'; setup = $null }
                @{ inputStr = 'Get-PSDrive -PSProvider Variable '; expected = 'Variable'; setup = $null }
                @{ inputStr = 'Get-Command Get-Chil'; expected = 'Get-ChildItem'; setup = $null }
                @{ inputStr = 'Get-Variable psver'; expected = 'PSVersionTable'; setup = $null }
                @{ inputStr = 'Get-Help *child'; expected = 'Get-ChildItem'; setup = $null }
                @{ inputStr = 'Trace-Command e'; expected = 'ETS'; setup = $null }
                @{ inputStr = 'Get-TraceSource e'; expected = 'ETS'; setup = $null }
                @{ inputStr = '[int]:: max'; expected = 'MaxValue'; setup = $null }
                @{ inputStr = '"string". l*'; expected = 'Length'; setup = $null }
                @{ inputStr = '("a" * 5).e'; expected = 'EndsWith('; setup = $null }
                @{ inputStr = '([string][int]1).e'; expected = 'EndsWith('; setup = $null }
                @{ inputStr = '(++$i).c'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '"a".Length.c'; expected = 'CompareTo('; setup = $null }
                @{ inputStr = '@(1, "a").c'; expected = 'Count'; setup = $null }
                @{ inputStr = '{1}.is'; expected = 'IsConfiguration'; setup = $null }
                @{ inputStr = '@{ }.'; expected = 'Count'; setup = $null }
                @{ inputStr = '@{abc=1}.a'; expected = 'Add('; setup = $null }
                @{ inputStr = '$a.f'; expected = "'fo-o'"; setup = { $a = @{'fo-o'='bar'} } }
                @{ inputStr = 'dir | % { $_.Full'; expected = 'FullName'; setup = $null }
                @{ inputStr = '@{a=$(exit)}.Ke'; expected = 'Keys'; setup = $null }
                @{ inputStr = '@{$(exit)=1}.Va'; expected = 'Values'; setup = $null }
                @{ inputStr = 'switch -'; expected = '-CaseSensitive'; setup = $null }
                @{ inputStr = 'gm -t'; expected = '-Type'; setup = $null }
                @{ inputStr = 'foo -aa -aa'; expected = '-aaa'; setup = { function foo {param($a, $aa, $aaa)} } }
                @{ inputStr = 'switch ( gps -'; expected = '-Name'; setup = $null }
                @{ inputStr = 'set-executionpolicy '; expected = 'AllSigned'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -exe: b'; expected = 'Bypass'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -exe:b'; expected = 'Bypass'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy -ExecutionPolicy:'; expected = 'AllSigned'; setup = $null }
                @{ inputStr = 'Set-ExecutionPolicy by -for:'; expected = '$true'; setup = $null }
                @{ inputStr = 'Import-Csv -Encoding '; expected = 'ASCII'; setup = $null }
                @{ inputStr = 'Get-Process | % ModuleM'; expected = 'ModuleMemorySize'; setup = $null }
                @{ inputStr = 'Get-Process | % {$_.MainModule} | % Com'; expected = 'Company'; setup = $null }
                @{ inputStr = 'Get-Process | % MainModule | % Com'; expected = 'Company'; setup = $null }
                @{ inputStr = '$p = Get-Process; $p | % ModuleM'; expected = 'ModuleMemorySize'; setup = $null }
                @{ inputStr = 'gmo Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'rmo Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'gcm -Module Microsoft.PowerShell.U'; expected = 'Microsoft.PowerShell.Utility'; setup = $null }
                @{ inputStr = 'gmo -list PackageM'; expected = 'PackageManagement'; setup = $null }
                @{ inputStr = 'gcm -Module PackageManagement Find-Pac'; expected = 'Find-Package'; setup = $null }
                @{ inputStr = 'ipmo PackageM'; expected = 'PackageManagement'; setup = $null }
                @{ inputStr = 'Get-Process powersh'; expected = 'powershell'; setup = $null }
                @{ inputStr = "function bar { [OutputType('System.IO.FileInfo')][OutputType('System.Diagnostics.Process')]param() }; bar | ? { `$_.ProcessN"; expected = 'ProcessName'; setup = $null }
                @{ inputStr = "function bar { [OutputType('System.IO.FileInfo')][OutputType('System.Diagnostics.Process')]param() }; bar | ? { `$_.LastAc"; expected = 'LastAccessTime'; setup = $null }
                @{ inputStr = "& 'get-comm"; expected = "'Get-Command'"; setup = $null }
                @{ inputStr = 'alias:dir'; expected = Join-Path 'Alias:' 'dir'; setup = $null }
                @{ inputStr = 'gc alias::ipm'; expected = 'Alias::ipmo'; setup = $null }
                @{ inputStr = 'gc enVironment::psmod'; expected = 'enVironment::PSModulePath'; setup = $null }
                ## tab completion safe expression evaluator tests
                @{ inputStr = '@{a=$(exit)}.Ke'; expected = 'Keys'; setup = $null }
                @{ inputStr = '@{$(exit)=1}.Ke'; expected = 'Keys'; setup = $null }
                ## tab completion variable names
                @{ inputStr = '@PSVer'; expected = '@PSVersionTable'; setup = $null }
                @{ inputStr = '$global:max'; expected = '$global:MaximumHistoryCount'; setup = $null }
                @{ inputStr = '$PSMod'; expected = '$PSModuleAutoLoadingPreference'; setup = $null }
                ## tab completion for variable in path
                @{ inputStr = 'cd $pshome\Modu'; expected = Join-Path $PSHOME "Modules"; setup = $null }
                ## tab completion AST-based tests
                @{ inputStr = 'get-date | ForEach-Object { $PSItem.h'; expected = 'Hour'; setup = $null }
                @{ inputStr = '$a=gps;$a[0].h'; expected = 'Handle'; setup = $null }
                @{ inputStr = "`$(1,'a',@{})[-1].k"; expected = 'Keys'; setup = $null }
                @{ inputStr = "`$(1,'a',@{})[1].tri"; expected = 'Trim('; setup = $null }
                ## tab completion for type names
                @{ inputStr = '[ScriptBlockAst'; expected = 'System.Management.Automation.Language.ScriptBlockAst'; setup = $null }
                @{ inputStr = 'New-Object dict'; expected = 'System.Collections.Generic.Dictionary'; setup = $null }
                @{ inputStr = 'New-Object System.Collections.Generic.List[datet'; expected = "'System.Collections.Generic.List[datetime]'"; setup = $null }
                @{ inputStr = '[System.Management.Automation.Runspaces.runspacef'; expected = 'System.Management.Automation.Runspaces.RunspaceFactory'; setup = $null }
                @{ inputStr = '[specialfol'; expected = 'System.Environment+SpecialFolder'; setup = $null }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases {
            param($inputStr, $expected, $setup)

            if ($null -ne $setup) { . $setup }
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }

        It "Tab completion UNC path" -Skip:(!$IsWindows) {
            $homeDrive = $env:HOMEDRIVE.Replace(":", "$")
            $beforeTab = "\\localhost\$homeDrive\wind"
            $afterTab = "& '\\localhost\$homeDrive\Windows'"
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }

        It "Tab completion for registry" -Skip:(!$IsWindows) {
            $beforeTab = 'registry::HKEY_l'
            $afterTab = 'registry::HKEY_LOCAL_MACHINE'
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }

        It "Tab completion for wsman provider" -Skip:(!$IsWindows) {
            $beforeTab = 'wsman::localh'
            $afterTab = 'wsman::localhost'
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }

        It "Tab completion for filesystem provider qualified path" {
            if ($IsWindows) {
                $beforeTab = 'filesystem::{0}\Wind' -f $env:SystemDrive
                $afterTab = 'filesystem::{0}\Windows' -f $env:SystemDrive
            } else {
                $beforeTab = 'filesystem::/us' -f $env:SystemDrive
                $afterTab = 'filesystem::/usr' -f $env:SystemDrive
            }
            $res = TabExpansion2 -inputScript $beforeTab -cursorColumn $beforeTab.Length
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be $afterTab
        }

        It "Tab completion dynamic parameter of a custom function" {
            function Test-DynamicParam {
                [CmdletBinding()]
                PARAM( $DeFirst )

                DYNAMICPARAM {
                    $paramDictionary = [System.Management.Automation.RuntimeDefinedParameterDictionary]::new()
                    $attributeCollection = [System.Collections.ObjectModel.Collection[Attribute]]::new()
                    $attributeCollection.Add([Parameter]::new())
                    $deSecond = [System.Management.Automation.RuntimeDefinedParameter]::new('DeSecond', [System.Array], $attributeCollection)
                    $deThird = [System.Management.Automation.RuntimeDefinedParameter]::new('DeThird', [System.Array], $attributeCollection)
                    $null = $paramDictionary.Add('DeSecond', $deSecond)
                    $null = $paramDictionary.Add('DeThird', $deThird)
                    return $paramDictionary
                }

                PROCESS {
                    Write-Host 'Hello'
                    Write-Host $PSBoundParameters
                }
            }

            $inputStr = "Test-DynamicParam -D"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 3
            $res.CompletionMatches[0].CompletionText | Should Be '-DeFirst'
            $res.CompletionMatches[1].CompletionText | Should Be '-DeSecond'
            $res.CompletionMatches[2].CompletionText | Should Be '-DeThird'
        }

        It "Tab completion dynamic parameter '-CodeSigningCert'" -Skip:(!$IsWindows) {
            try {
                Push-Location cert:\
                $inputStr = "gci -co"
                $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
                $res.CompletionMatches[0].CompletionText | Should Be '-CodeSigningCert'
            } finally {
                Pop-Location
            }
        }

        It "Tab completion for file system takes precedence over functions" {
            try {
                Push-Location $TestDrive
                New-Item -Name myf -ItemType File -Force
                function MyFunction { "Hi there" }

                $inputStr = "myf"
                $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
                $res.CompletionMatches.Count | Should BeExactly 2
                $res.CompletionMatches[0].CompletionText | Should Be (Resolve-Path myf -Relative)
                $res.CompletionMatches[1].CompletionText | Should Be "MyFunction"
            } finally {
                Remove-Item -Path myf -Force
                Pop-Location
            }
        }

        It "Tab completion for validateSet attribute" {
            function foo { param([ValidateSet('cat','dog')]$p) }
            $inputStr = "foo "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 2
            $res.CompletionMatches[0].CompletionText | Should be 'cat'
            $res.CompletionMatches[1].CompletionText | Should be 'dog'
        }

        It "Tab completion for enum type parameter of a custom function" {
            function baz ([consolecolor]$name, [ValidateSet('cat','dog')]$p){}
            $inputStr = "baz -name "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 16
            $res.CompletionMatches[0].CompletionText | Should Be 'Black'

            $inputStr = "baz Black "
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 2
            $res.CompletionMatches[0].CompletionText | Should be 'cat'
            $res.CompletionMatches[1].CompletionText | Should be 'dog'
        }

        It "Tab completion for enum members after comma" {
            $inputStr = "Get-Command -Type Alias,c"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 2
            $res.CompletionMatches[0].CompletionText | Should Be 'Cmdlet'
            $res.CompletionMatches[1].CompletionText | Should Be 'Configuration'
        }

        It "Test [CommandCompletion]::GetNextResult" {
            $inputStr = "Get-Command -Type Alias,c"
            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 2
            $res.GetNextResult($false).CompletionText | Should Be 'Configuration'
            $res.GetNextResult($true).CompletionText | Should Be 'Cmdlet'
            $res.GetNextResult($true).CompletionText | Should Be 'Configuration'
        }
    }

    Context "Folder/File path tab completion with special characters" {
        BeforeAll {
            $separator = [System.IO.Path]::DirectorySeparatorChar
            $testCases = @(
                @{ inputStr = "cd My"; expected = "'.${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Help '.\My ``[Path``]'\"; expected = "'.${separator}My ``[Path``]${separator}test.ps1'" }
                @{ inputStr = "Get-Process >My"; expected = "'.${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Process >'.\My ``[Path``]\'"; expected = "'.${separator}My ``[Path``]${separator}test.ps1'" }
                @{ inputStr = "Get-Process >${TestDrive}\My"; expected = "'${TestDrive}${separator}My ``[Path``]'" }
                @{ inputStr = "Get-Process > '${TestDrive}\My ``[Path``]\'"; expected = "'${TestDrive}${separator}My ``[Path``]${separator}test.ps1'" }
            )

            New-Item -Path "$TestDrive\My [Path]" -ItemType Directory > $null
            New-Item -Path "$TestDrive\My [Path]\test.ps1" -ItemType File > $null

            Push-Location -Path $TestDrive
        }

        AfterAll {
            Pop-Location
        }

        It "Complete special relative path '<inputStr>'" -TestCases $testCases {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }
    }

    Context "Local tab completion with AST" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = '$p = Get-Process; $p | % ProcessN '; bareWord = 'ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'function bar { Get-Ali* }'; bareWord = 'Get-Ali*'; expected = 'Get-Alias' }
                @{ inputStr = 'function baz ([string]$version, [consolecolor]$name){} baz version bl'; bareWord = 'bl'; expected = 'Black' }
            )
        }

        It "Input '<inputStr>' should successfully complete via AST" -TestCases $testCases {
            param($inputStr, $bareWord, $expected)

            $tokens = $null
            $ast = [System.Management.Automation.Language.Parser]::ParseInput($inputStr, [ref] $tokens, [ref]$null)
            $elementAst = $ast.Find(
                { $args[0] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $args[0].Value -eq $bareWord },
                $true
            )

            $res = TabExpansion2 -ast $ast -tokens $tokens -positionOfCursor $elementAst.Extent.EndScriptPosition
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }
    }

    Context "User-overridden TabExpansion implementations" {
        It "Override TabExpansion with function" {
            function TabExpansion ($line, $lastword) {
                "Overridden-TabExpansion-Function"
            }

            $inputStr = '$pid.'
            $res = [System.Management.Automation.CommandCompletion]::CompleteInput($inputStr, $inputst.Length, $null)
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be 'Overridden-TabExpansion-Function'
        }

        It "Override TabExpansion with alias" {
            function OverrideTabExpansion ($line, $lastword) {
                "Overridden-TabExpansion-Alias"
            }
            Set-Alias -Name TabExpansion -Value OverrideTabExpansion

            $inputStr = '$pid.'
            $res = [System.Management.Automation.CommandCompletion]::CompleteInput($inputStr, $inputst.Length, $null)
            $res.CompletionMatches.Count | Should BeExactly 1
            $res.CompletionMatches[0].CompletionText | Should Be "Overridden-TabExpansion-Alias"
        }
    }

    Context "No tab completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = 'function new-' }
                @{ inputStr = 'filter new-' }
                @{ inputStr = '@pid.' }
            )
        }

        It "Input '<inputStr>' should not complete to anything" -TestCases $testCases {
            param($inputStr)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeExactly 0
        }
    }

    context "Tab completion error tests" {
        BeforeAll {
            $ast = {}.Ast;
            $tokens = [System.Management.Automation.Language.Token[]]@()
            $testCases = @(
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::MapStringInputToParsedInput('$pid.', 7)}; expected = "PSArgumentException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput('$pid.', 7, $null, $null)}; expected = "PSArgumentException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput('$pid.', 5, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($null, $null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $null, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $null, $null, $null)}; expected = "PSArgumentNullException" }
                @{ inputStr = {[System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $ast.Extent.EndScriptPosition, $null, $null)}; expected = "PSArgumentNullException" }
            )
        }

        It "Input '<inputStr>' should throw in tab completion" -TestCases $testCases {
            param($inputStr, $expected)
            $inputStr | ShouldBeErrorId $expected
        }
    }

    Context "DSC tab completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = 'Configura'; expected = 'Configuration' }
                @{ inputStr = '$extension = New-Object [System.Collections.Generic.List[string]]; $extension.wh'; expected = "Where(" }
                @{ inputStr = '$extension = New-Object [System.Collections.Generic.List[string]]; $extension.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration foo { node $SelectedNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $SelectedNodes.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration foo { node $AllNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $ConfigurationData.AllNodes.'; expected = 'Where(' }
                @{ inputStr = 'Configuration foo { node $ConfigurationData.AllNodes.fo'; expected = 'ForEach(' }
                @{ inputStr = 'Configuration bar { File foo { Destinat'; expected = 'DestinationPath = ' }
                @{ inputStr = 'Configuration bar { File foo { Content'; expected = 'Contents = ' }
                @{ inputStr = 'Configuration bar { Fil'; expected = 'File' }
                @{ inputStr = 'Configuration bar { Import-Dsc'; expected = 'Import-DscResource' }
                @{ inputStr = 'Configuration bar { Import-DscResource -Modu'; expected = '-ModuleName' }
                @{ inputStr = 'Configuration bar { Import-DscResource -ModuleName blah -Modu'; expected = '-ModuleVersion' }
                @{ inputStr = 'Configuration bar { Scri'; expected = 'Script' }
                @{ inputStr = 'configuration foo { Script ab {Get'; expected = 'GetScript = ' }
                @{ inputStr = 'configuration foo { Script ab { '; expected = 'DependsOn = ' }
            )
        }

        It "Input '<inputStr>' should successfully complete" -TestCases $testCases -Skip:(!$IsWindows) {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }
    }

    Context "CIM cmdlet completion tests" {
        BeforeAll {
            $testCases = @(
                @{ inputStr = "Invoke-CimMethod -ClassName Win32_Process -MethodName Crea"; expected = "Create" }
                @{ inputStr = "Get-CimInstance -ClassName Win32_Process | Invoke-CimMethod -MethodName AttachDeb"; expected = "AttachDebugger" }
                @{ inputStr = 'Get-CimInstance Win32_Process | ?{ $_.ProcessId -eq $Pid } | Get-CimAssociatedInstance -ResultClassName Win32_Co*uterSyst'; expected = "Win32_ComputerSystem" }
                @{ inputStr = "Get-CimInstance -ClassName Win32_Environm"; expected = "Win32_Environment" }
                @{ inputStr = "New-CimInstance -ClassName Win32_Environm"; expected = "Win32_Environment" }
                @{ inputStr = 'New-CimInstance -ClassName Win32_Process | %{ $_.Captio'; expected = "Caption" }
                @{ inputStr = "Invoke-CimMethod -ClassName Win32_Environm"; expected = 'Win32_Environment' }
                @{ inputStr = "Get-CimClass -ClassName Win32_Environm"; expected = 'Win32_Environment' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Process | Invoke-CimMethod -MethodName SetPriorit'; expected = 'SetPriority' }
                @{ inputStr = 'Invoke-CimMethod -Namespace root/StandardCimv2 -ClassName MSFT_NetIPAddress -MethodName Crea'; expected = 'Create' }
                @{ inputStr = '$win32_process = Get-CimInstance -ClassName Win32_Process; $win32_process | Invoke-CimMethod -MethodName AttachDe'; expected = 'AttachDebugger' }
                @{ inputStr = '$win32_process = Get-CimInstance -ClassName Win32_Process; Invoke-CimMethod -InputObject $win32_process -MethodName AttachDe'; expected = 'AttachDebugger' }
                @{ inputStr = 'Get-CimInstance Win32_Process | ?{ $_.ProcessId -eq $Pid } | Get-CimAssociatedInstance -ResultClassName Win32_ComputerS'; expected = 'Win32_ComputerSystem' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Interop -ClassName Win32_PowerSupplyP'; expected = 'Win32_PowerSupplyProfile' }
                @{ inputStr = 'Get-CimInstance __NAMESP'; expected = '__NAMESPACE' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Int'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Int*ro'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimInstance -Namespace root/Interop/'; expected = 'root/Interop/ms_409' }
                @{ inputStr = 'New-CimInstance -Namespace root/Int'; expected = 'root/Interop' }
                @{ inputStr = 'Invoke-CimMethod -Namespace root/Int'; expected = 'root/Interop' }
                @{ inputStr = 'Get-CimClass -Namespace root/Int'; expected = 'root/Interop' }
                @{ inputStr = 'Register-CimIndicationEvent -Namespace root/Int'; expected = 'root/Interop' }
                @{ inputStr = '[Microsoft.Management.Infrastructure.CimClass]$c = $null; $c.CimClassNam'; expected = 'CimClassName' }
                @{ inputStr = '[Microsoft.Management.Infrastructure.CimClass]$c = $null; $c.CimClassName.Substrin'; expected = 'Substring(' }
                @{ inputStr = 'Get-CimInstance -ClassName Win32_Process | %{ $_.ExecutableP'; expected = 'ExecutablePath' }
            )
        }

        It "CIM cmdlet input '<inputStr>' should successfully complete" -TestCases $testCases -Skip:(!$IsWindows) {
            param($inputStr, $expected)

            $res = TabExpansion2 -inputScript $inputStr -cursorColumn $inputStr.Length
            $res.CompletionMatches.Count | Should BeGreaterThan 0
            $res.CompletionMatches[0].CompletionText | Should Be $expected
        }
    }
}

Describe "Tab completion tests with remote Runspace" -Tags Feature {
    BeforeAll {
        if ($IsWindows) {
            $session = New-RemoteSession
            $powershell = [powershell]::Create()
            $powershell.Runspace = $session.Runspace

            $testCases = @(
                @{ inputStr = 'Get-Proc'; expected = 'Get-Process' }
                @{ inputStr = 'Get-Process | % ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'Get-ChildItem alias: | % { $_.Defini'; expected = 'Definition' }
            )

            $testCasesWithAst = @(
                @{ inputStr = '$p = Get-Process; $p | % ProcessN '; bareWord = 'ProcessN'; expected = 'ProcessName' }
                @{ inputStr = 'function bar { Get-Ali* }'; bareWord = 'Get-Ali*'; expected = 'Get-Alias' }
                @{ inputStr = 'function baz ([string]$version, [consolecolor]$name){} baz version bl'; bareWord = 'bl'; expected = 'Black' }
            )
        } else {
            $defaultParameterValues = $PSDefaultParameterValues.Clone()
            $PSDefaultParameterValues["It:Skip"] = $true
        }
    }
    AfterAll {
        if ($IsWindows) {
            Remove-PSSession $session
            $powershell.Dispose()
        } else {
            $Global:PSDefaultParameterValues = $defaultParameterValues
        }
    }

    It "Input '<inputStr>' should successfully complete in remote runspace" -TestCases $testCases {
        param($inputStr, $expected)
        $res = [System.Management.Automation.CommandCompletion]::CompleteInput($inputStr, $inputStr.Length, $null, $powershell)
        $res.CompletionMatches.Count | Should BeGreaterThan 0
        $res.CompletionMatches[0].CompletionText | Should Be $expected
    }

    It "Input '<inputStr>' should successfully complete via AST in remote runspace" -TestCases $testCasesWithAst {
        param($inputStr, $bareWord, $expected)

        $tokens = $null
        $ast = [System.Management.Automation.Language.Parser]::ParseInput($inputStr, [ref] $tokens, [ref]$null)
        $elementAst = $ast.Find(
            { $args[0] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and $args[0].Value -eq $bareWord },
            $true
        )

        $res = [System.Management.Automation.CommandCompletion]::CompleteInput($ast, $tokens, $elementAst.Extent.EndScriptPosition, $null, $powershell)
        $res.CompletionMatches.Count | Should BeGreaterThan 0
        $res.CompletionMatches[0].CompletionText | Should Be $expected
    }
}
