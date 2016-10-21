Describe "Sort-Object" -Tags "CI" {

    It "should be able to sort object in ascending with using Property switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length } | Should Not Throw
        
        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -Last 1).Length

        $firstLen -lt $lastLen | Should be $true

    }

    It "should be able to sort object in descending with using Descending switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending } | Should Not Throw
        
        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -Last 1).Length

        $firstLen -gt $lastLen | Should be $true
    }
}

Describe "Sort-Object DRT Unit Tests" -Tags "CI" {
    It "Sort-Object with object array should work"{
        $employee1 = [pscustomobject]@{"FirstName"="Eight"; "LastName"="Eight"; "YearsInMS"=8}
        $employee2 = [pscustomobject]@{"FirstName"="Eight"; "YearsInMS"=$null}
        $employee3 = [pscustomobject]@{"FirstName"="Minus"; "LastName"="Two"; "YearsInMS"=-2}
        $employee4 = [pscustomobject]@{"FirstName"="One"; "LastName"="One"; "YearsInMS"=1}
        $employees = @($employee1,$employee2,$employee3,$employee4)
        $results = $employees | Sort-Object -Property YearsInMS
        
        $results[0].FirstName | Should Be "Minus"
        $results[0].LastName | Should Be "Two"
        $results[0].YearsInMS | Should Be -2
        
        $results[1].FirstName | Should Be "Eight"
        $results[1].YearsInMS | Should BeNullOrEmpty
        
        $results[2].FirstName | Should Be "One"
        $results[2].LastName | Should Be "One"
        $results[2].YearsInMS | Should Be 1
        
        $results[3].FirstName | Should Be "Eight"
        $results[3].LastName | Should Be "Eight"
        $results[3].YearsInMS | Should Be 8
    }

    It "Sort-Object with Non Conflicting Order Entry Keys should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
        $employees = @($employee1,$employee2,$employee3)
        $ht = @{"e"="YearsInMS"; "descending"=$false; "ascending"=$true}
        $results = $employees | Sort-Object -Property $ht -Descending
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[1]
    }

    It "Sort-Object with Conflicting Order Entry Keys should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
        $employees = @($employee1,$employee2,$employee3)
        $ht = @{"e"="YearsInMS"; "descending"=$false; "ascending"=$false}
        $results = $employees | Sort-Object -Property $ht -Descending
        
        $results[0] | Should Be $employees[1]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[2]
    }

    It "Sort-Object with One Order Entry Key should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
        $employees = @($employee1,$employee2,$employee3)
        $ht = @{"e"="YearsInMS"; "descending"=$false}
        $results = $employees | Sort-Object -Property $ht -Descending
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[1]
    }

    It "Sort-Object with HistoryInfo object should work"{
        Add-Type -TypeDefinition "public enum PipelineState{NotStarted,Running,Stopping,Stopped,Completed,Failed,Disconnected}"
        
        $historyInfo1 = [pscustomobject]@{"PipelineId"=1; "Cmdline"="cmd3"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}
        $historyInfo2 = [pscustomobject]@{"PipelineId"=2; "Cmdline"="cmd1"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}
        $historyInfo3 = [pscustomobject]@{"PipelineId"=3; "Cmdline"="cmd2"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}
        
        $historyInfos = @($historyInfo1,$historyInfo2,$historyInfo3)
        
        $results = $historyInfos | Sort-Object
        
        $results[0] | Should Be $historyInfos[0]
        $results[1] | Should Be $historyInfos[1]
        $results[2] | Should Be $historyInfos[2]
    }

    It "Sort-Object with Non Existing And Null Script Property should work"{
        $n = new-object microsoft.powershell.commands.newobjectcommand
        $d = new-object microsoft.powershell.commands.newobjectcommand
        $d.TypeName = 'Deetype'
        $b = new-object microsoft.powershell.commands.newobjectcommand
        $b.TypeName = 'btype'
        $a = new-object microsoft.powershell.commands.newobjectcommand
        $a.TypeName = 'atype'
        $results = $n, $d, $b, 'b', $a | Sort-Object -proper {$_.TypeName}
        $results.Count | Should Be 5
        $results[2] | Should Be $a
        $results[3] | Should Be $b
        $results[4] | Should Be $d
        #results[0] and [1] can be any order
    }

    It "Sort-Object with Non Existing And Null Property should work"{
        $n = new-object microsoft.powershell.commands.newobjectcommand
        $n.TypeName = $null
        $d = new-object microsoft.powershell.commands.newobjectcommand
        $d.TypeName = 'Deetype'
        $b = new-object microsoft.powershell.commands.newobjectcommand
        $b.TypeName = 'btype'
        $a = new-object microsoft.powershell.commands.newobjectcommand
        $a.TypeName = 'atype'
        $results = $n, $d, $b, 'b', $a | Sort-Object -prop TypeName
        $results.Count | Should Be 5
        $results[0] | Should Be $n
        $results[1] | Should Be $a
        $results[2] | Should Be $b
        $results[3] | Should Be $d
        $results[4] | Should Be 'b'
    }

    It "Sort-Object with Non Case-Sensitive Unique should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
        $employees = @($employee1,$employee2,$employee3)
        $results = $employees | Sort-Object -Property "LastName" -Descending -Unique
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[1]
        $results[2] | Should BeNullOrEmpty
    }

    It "Sort-Object with Case-Sensitive Unique should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
        $employees = @($employee1,$employee2,$employee3)
        $results = $employees | Sort-Object -Property "LastName","FirstName" -Descending -Unique -CaseSensitive
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[1]
    }

    It "Sort-Object with Two Order Entry Keys should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
        $employees = @($employee1,$employee2,$employee3)
        $ht1 = @{"expression"="LastName"; "ascending"=$false}
        $ht2 = @{"expression"="FirstName"; "ascending"=$true}
        $results = $employees | Sort-Object -Property @($ht1,$ht2) -Descending
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[1]
        $results[2] | Should Be $employees[0]
    }

    It "Sort-Object with -Descending:$false and Two Order Entry Keys should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
        $employees = @($employee1,$employee2,$employee3)
        $results = $employees | Sort-Object -Property "LastName","FirstName" -Descending:$false
        
        $results[0] | Should Be $employees[1]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[2]
    }

    It "Sort-Object with -Descending and Two Order Entry Keys should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
        $employees = @($employee1,$employee2,$employee3)
        $results = $employees | Sort-Object -Property "LastName","FirstName" -Descending
        
        $results[0] | Should Be $employees[2]
        $results[1] | Should Be $employees[0]
        $results[2] | Should Be $employees[1]
    }

    It "Sort-Object with Two Order Entry Keys with asc=true should work"{
        $employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
        $employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
        $employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
        $employees = @($employee1,$employee2,$employee3)
        $ht1 = @{"e"="FirstName"; "asc"=$true}
        $ht2 = @{"e"="LastName";}
        $results = $employees | Sort-Object -Property @($ht1,$ht2) -Descending
        
        $results[0] | Should Be $employees[1]
        $results[1] | Should Be $employees[2]
        $results[2] | Should Be $employees[0]
    }

    It "Sort-Object with Descending No Property should work"{
        $employee1 = 1
        $employee2 = 2
        $employee3 = 3
        $employees = @($employee1,$employee2,$employee3)
        $results = $employees | Sort-Object -Descending
        
        $results[0] | Should Be 3
        $results[1] | Should Be 2
        $results[2] | Should Be 1
    }
}

Describe 'Sort-Object Top and Bottom Unit Tests' -Tags 'CI' {

    It 'Return the top N integers sorted in ascending order' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N integers sorted in descending order' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N integers sorted in ascending order' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the bottom N integers sorted in descending order' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects from a collection of different types of objects, sorted in ascending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 6
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            if ($topNSortResults[$i] -is [System.Array]) {
                $topNSortResults[$i].Equals($fullSortResults[$i]) | Should Be $true
            } else {
                $topNSortResults[$i] | Should Be $fullSortResults[$i]
            }
        }
    }

    It 'Return the top N objects from a collection of different types of objects, sorted in descending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 6
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            if ($topNSortResults[$i] -is [System.Array]) {
                $topNSortResults[$i].Equals($fullSortResults[$i]) | Should Be $true
            } else {
                $topNSortResults[$i] | Should Be $fullSortResults[$i]
            }
        }
    }

    It 'Return the bottom N objects from a collection of different types of objects, sorted in ascending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 6
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            if ($bottomNSortResults[$i] -is [System.Array]) {
                $bottomNSortResults[$i].Equals($fullSortResults[$i + $offset]) | Should Be $true
            } else {
                $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
            }
        }
    }

    It 'Return the bottom N objects from a collection of different types of objects, sorted in descending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 6
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            if ($bottomNSortResults[$i] -is [System.Array]) {
                $bottomNSortResults[$i].Equals($fullSortResults[$i + $offset]) | Should Be $true
            } else {
                $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
            }
        }
    }

    It 'Return the top N objects from a collection of different types of objects, sorted by property in ascending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Property='Name'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 6
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            if ($topNSortResults[$i] -is [System.Array]) {
                $topNSortResults[$i].Equals($fullSortResults[$i]) | Should Be $true
            } else {
                $topNSortResults[$i] | Should Be $fullSortResults[$i]
            }
        }
    }

    It 'Return the top N objects from a collection of different types of objects, sorted by property in descending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Property='Name';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 6
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            if ($topNSortResults[$i] -is [System.Array]) {
                $topNSortResults[$i].Equals($fullSortResults[$i]) | Should Be $true
            } else {
                $topNSortResults[$i] | Should Be $fullSortResults[$i]
            }
        }
    }

    It 'Return the bottom N objects from a collection of different types of objects, sorted by property in ascending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Property='Name'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 6
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            if ($bottomNSortResults[$i] -is [System.Array]) {
                $bottomNSortResults[$i].Equals($fullSortResults[$i + $offset]) | Should Be $true
            } else {
                $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
            }
        }
    }

    It 'Return the bottom N objects from a collection of different types of objects, sorted by property in descending order' {
        $unsortedData = @(
            ($item0 = 'x')
            ($item1 = Get-Service wuauserv)
            ($item2 = 'c')
            ($item3 = ([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru) # Use this syntax to get $null from a default sort (uses ToString)
            ($item4 = 42)
            ($item5 = Get-Service bits)
            ,($item6 =@('a','b','c')) # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
            ($item7 = [pscustomobject]@{Name='NotAService'})
            ($item8 = [pscustomobject]@{Name=$null;Status='Custom'})
            ($item9 = 'z')
            ,($item10 = ,$null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
        )
        $baseSortParameters = @{Property='Name';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 6
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            if ($bottomNSortResults[$i] -is [System.Array]) {
                $bottomNSortResults[$i].Equals($fullSortResults[$i + $offset]) | Should Be $true
            } else {
                $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
            }
        }
    }

    It 'Return the top N objects from a collection of objects of the same type, sorted by property in ascending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{PSTypeName='Employee';FirstName='Dwayne';LastName='Smith' ;YearsInMS=8    }
            $item1 = [pscustomobject]@{PSTypeName='Employee';FirstName='Lucy'  ;                 ;YearsInMS=$null}
            $item2 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Jones' ;YearsInMS=-2   }
            $item3 = [pscustomobject]@{PSTypeName='Employee';FirstName='Sylvie';LastName='Landry';YearsInMS=1    }
            $item4 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Frank' ;YearsInMS=5    }
        )
        $baseSortParameters = @{Property='YearsInMS'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N objects from a collection of objects of the same type, sorted by property in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{PSTypeName='Employee';FirstName='Dwayne';LastName='Smith' ;YearsInMS=8    }
            $item1 = [pscustomobject]@{PSTypeName='Employee';FirstName='Lucy'  ;                 ;YearsInMS=$null}
            $item2 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Jones' ;YearsInMS=-2   }
            $item3 = [pscustomobject]@{PSTypeName='Employee';FirstName='Sylvie';LastName='Landry';YearsInMS=1    }
            $item4 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Frank' ;YearsInMS=5    }
        )
        $baseSortParameters = @{Property='YearsInMS';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects from a collection of objects of the same type, sorted by property in ascending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{PSTypeName='Employee';FirstName='Dwayne';LastName='Smith' ;YearsInMS=8    }
            $item1 = [pscustomobject]@{PSTypeName='Employee';FirstName='Lucy'  ;                 ;YearsInMS=$null}
            $item2 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Jones' ;YearsInMS=-2   }
            $item3 = [pscustomobject]@{PSTypeName='Employee';FirstName='Sylvie';LastName='Landry';YearsInMS=1    }
            $item4 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Frank' ;YearsInMS=5    }
        )
        $baseSortParameters = @{Property='YearsInMS'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the bottom N objects from a collection of objects of the same type, sorted by property in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{PSTypeName='Employee';FirstName='Dwayne';LastName='Smith' ;YearsInMS=8    }
            $item1 = [pscustomobject]@{PSTypeName='Employee';FirstName='Lucy'  ;                 ;YearsInMS=$null}
            $item2 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Jones' ;YearsInMS=-2   }
            $item3 = [pscustomobject]@{PSTypeName='Employee';FirstName='Sylvie';LastName='Landry';YearsInMS=1    }
            $item4 = [pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Frank' ;YearsInMS=5    }
        )
        $baseSortParameters = @{Property='YearsInMS';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return all integers from random integers sorted in ascending order when -Top is too large' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 20
        $topNSortResults.Count | Should Be $fullSortResults.Count
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return all integers from random integers sorted in descending order when -Top is too large' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 20
        $topNSortResults.Count | Should Be $fullSortResults.Count
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return all integers from random integers sorted in ascending order when -Bottom is too large' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 20
        $bottomNSortResults.Count | Should Be $fullSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return all integers from random integers sorted in descending order when -Bottom is too large' {
        $unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193
        $baseSortParameters = @{Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 20
        $bottomNSortResults.Count | Should Be $fullSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N objects when non-conflicting order entry keys are used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$true}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects when non-conflicting order entry keys are used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$true}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N objects when conflicting order entry keys are used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$false}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects when conflicting order entry keys are used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$false}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N objects when one order entry key is used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects when one order entry key is used' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=2 }
        )
        $baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the top N objects from collection of custom objects that do not define ToString' {
        Add-Type -TypeDefinition 'public enum PipelineState{NotStarted,Running,Stopping,Stopped,Completed,Failed,Disconnected}'
        $unsortedData = @(
            $item0 = [pscustomobject]@{PipelineId=1;Cmdline='cmd3';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
            $item1 = [pscustomobject]@{PipelineId=2;Cmdline='cmd1';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
            $item2 = [pscustomobject]@{PipelineId=3;Cmdline='cmd2';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
        )
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects from collection of custom objects that do not define ToString' {
        Add-Type -TypeDefinition 'public enum PipelineState{NotStarted,Running,Stopping,Stopped,Completed,Failed,Disconnected}'
        $unsortedData = @(
            $item0 = [pscustomobject]@{PipelineId=1;Cmdline='cmd3';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
            $item1 = [pscustomobject]@{PipelineId=2;Cmdline='cmd1';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
            $item2 = [pscustomobject]@{PipelineId=3;Cmdline='cmd2';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
        )
        $baseSortParameters = @{}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects from collection of objects with existing and null script properties' {
        $item0 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1.TypeName = 'DeeType'
        $item2 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item2.TypeName = 'B-Type'
        $item3 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item3.TypeName = 'A-Type'
        $unsortedData = @($item0,$item1,$item2,'b',$item3)
        $baseSortParameters = @{Property={$_.TypeName}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects from collection of objects with existing and null script properties' {
        $item0 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1.TypeName = 'DeeType'
        $item2 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item2.TypeName = 'B-Type'
        $item3 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item3.TypeName = 'A-Type'
        $unsortedData = @($item0,$item1,$item2,'b',$item3)
        $baseSortParameters = @{Property={$_.TypeName}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects from collection of objects with existing and null properties' {
        $item0 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1.TypeName = 'DeeType'
        $item2 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item2.TypeName = 'B-Type'
        $item3 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item3.TypeName = 'A-Type'
        $unsortedData = @($item0,$item1,$item2,'b',$item3)
        $baseSortParameters = @{Property='TypeName'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects from collection of objects with existing and null properties' {
        $item0 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item1.TypeName = 'DeeType'
        $item2 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item2.TypeName = 'B-Type'
        $item3 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
        $item3.TypeName = 'A-Type'
        $unsortedData = @($item0,$item1,$item2,'b',$item3)
        $baseSortParameters = @{Property='TypeName'}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N unique objects sorted by property name using a case-insensitive comparison in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName';Descending=$true;Unique=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N unique objects sorted by property name using a case-insensitive comparison in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName';Descending=$true;Unique=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N unique objects sorted by property name using a case-sensitive comparison in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName';CaseSensitive=$true;Descending=$true;Unique=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 3
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N unique objects sorted by property name using a case-sensitive comparison in descending order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName';CaseSensitive=$true;Descending=$true;Unique=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 3
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects sorted using two order entry keys' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property=@{Expression='LastName';Ascending=$false},@{Expression='FirstName';Ascending=$true}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects sorted using two order entry keys' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property=@{Expression='LastName';Ascending=$false},@{Expression='FirstName';Ascending=$true}}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects sorted using two properties with -Descending:$false' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName','FirstName';Descending=$false}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects sorted using two properties with -Descending:$false' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName','FirstName';Descending=$false}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects sorted in descending order using two properties' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName','FirstName';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects sorted in descending order using two properties' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property='LastName','FirstName';Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

    It 'Return the top N objects using two properties with mixed sort order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property=@{Expression='FirstName';Ascending=$true},@{Expression='LastName'};Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $topNSortResults = $unsortedData | Sort-Object @baseSortParameters -Top 2
        for ($i = 0; $i -lt $topNSortResults.Count; $i++) {
            $topNSortResults[$i] | Should Be $fullSortResults[$i]
        }
    }

    It 'Return the bottom N objects using two properties with mixed sort order' {
        $unsortedData = @(
            $item0 = [pscustomobject]@{FirstName='John'  ;LastName='Smith';YearsInMS=5 }
            $item1 = [pscustomobject]@{FirstName='Joseph';LastName='Smith';YearsInMS=15}
            $item2 = [pscustomobject]@{FirstName='John'  ;LastName='Smyth';YearsInMS=12}
            $item3 = [pscustomobject]@{FirstName='Johnny';LastName='smith';YearsInMS=8 }
        )
        $baseSortParameters = @{Property=@{Expression='FirstName';Ascending=$true},@{Expression='LastName'};Descending=$true}
        $fullSortResults = $unsortedData | Sort-Object @baseSortParameters
        $bottomNSortResults = $unsortedData | Sort-Object @baseSortParameters -Bottom 2
        $offset = $fullSortResults.Count - $bottomNSortResults.Count
        for ($i = 0; $i -lt $bottomNSortResults.Count; $i++) {
            $bottomNSortResults[$i] | Should Be $fullSortResults[$i + $offset]
        }
    }

}