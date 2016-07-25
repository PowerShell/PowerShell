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
