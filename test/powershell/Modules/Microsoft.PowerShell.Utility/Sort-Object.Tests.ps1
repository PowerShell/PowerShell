# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Sort-Object" -Tags "CI" {

    It "should be able to sort object in ascending with using Property switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length } | Should -Not -Throw

        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length | Select-Object -Last 1).Length

        $firstLen -lt $lastLen | Should -BeTrue

    }

    It "should be able to sort object in descending with using Descending switch" {
        { Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending } | Should -Not -Throw

        $firstLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -First 1).Length
        $lastLen = (Get-ChildItem -Path $PSScriptRoot -Include *.ps1 -Recurse | Sort-Object -Property Length -Descending | Select-Object -Last 1).Length

        $firstLen -gt $lastLen | Should -BeTrue
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

		$results[0].FirstName | Should -BeExactly "Minus"
		$results[0].LastName | Should -BeExactly "Two"
		$results[0].YearsInMS | Should -Be -2

		$results[1].FirstName | Should -BeExactly "Eight"
		$results[1].YearsInMS | Should -BeNullOrEmpty

		$results[2].FirstName | Should -BeExactly "One"
		$results[2].LastName | Should -BeExactly "One"
		$results[2].YearsInMS | Should -Be 1

		$results[3].FirstName | Should -BeExactly "Eight"
		$results[3].LastName | Should -BeExactly "Eight"
		$results[3].YearsInMS | Should -Be 8
	}

	It "Sort-Object with Non Conflicting Order Entry Keys should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
		$employees = @($employee1,$employee2,$employee3)
		$ht = @{"e"="YearsInMS"; "descending"=$false; "ascending"=$true}
		$results = $employees | Sort-Object -Property $ht -Descending

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[1]
	}

	It "Sort-Object with Conflicting Order Entry Keys should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
		$employees = @($employee1,$employee2,$employee3)
		$ht = @{"e"="YearsInMS"; "descending"=$false; "ascending"=$false}
		$results = $employees | Sort-Object -Property $ht -Descending

		$results[0] | Should -Be $employees[1]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[2]
	}

	It "Sort-Object with One Order Entry Key should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
		$employees = @($employee1,$employee2,$employee3)
		$ht = @{"e"="YearsInMS"; "descending"=$false}
		$results = $employees | Sort-Object -Property $ht -Descending

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[1]
	}

	It "Sort-Object with HistoryInfo object should work"{
		Add-Type -TypeDefinition "public enum PipelineState{NotStarted,Running,Stopping,Stopped,Completed,Failed,Disconnected}"

		$historyInfo1 = [pscustomobject]@{"PipelineId"=1; "Cmdline"="cmd3"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}
		$historyInfo2 = [pscustomobject]@{"PipelineId"=2; "Cmdline"="cmd1"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}
		$historyInfo3 = [pscustomobject]@{"PipelineId"=3; "Cmdline"="cmd2"; "Status"=[PipelineState]::Completed; "StartTime" = [DateTime]::Now;"EndTime" = [DateTime]::Now.AddSeconds(5.0);}

		$historyInfos = @($historyInfo1,$historyInfo2,$historyInfo3)

		$results = $historyInfos | Sort-Object

		$results[0] | Should -Be $historyInfos[0]
		$results[1] | Should -Be $historyInfos[1]
		$results[2] | Should -Be $historyInfos[2]
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
		$results.Count | Should -Be 5
		$results[2] | Should -Be $a
		$results[3] | Should -Be $b
		$results[4] | Should -Be $d
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
		$results.Count | Should -Be 5
		$results[0] | Should -Be $n
		$results[1] | Should -Be $a
		$results[2] | Should -Be $b
		$results[3] | Should -Be $d
		$results[4] | Should -Be 'b'
	}

	It "Sort-Object with Non Case-Sensitive Unique should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
		$employees = @($employee1,$employee2,$employee3)
		$results = $employees | Sort-Object -Property "LastName" -Descending -Unique

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[1]
		$results[2] | Should -BeNullOrEmpty
	}

	It "Sort-Object with Case-Sensitive Unique should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
		$employees = @($employee1,$employee2,$employee3)
		$results = $employees | Sort-Object -Property "LastName","FirstName" -Descending -Unique -CaseSensitive

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[1]
	}

	It "Sort-Object with Two Order Entry Keys should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=2}
		$employees = @($employee1,$employee2,$employee3)
		$ht1 = @{"expression"="LastName"; "ascending"=$false}
		$ht2 = @{"expression"="FirstName"; "ascending"=$true}
		$results = $employees | Sort-Object -Property @($ht1,$ht2) -Descending

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[1]
		$results[2] | Should -Be $employees[0]
	}

	It "Sort-Object with -Descending:$false and Two Order Entry Keys should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
		$employees = @($employee1,$employee2,$employee3)
		$results = $employees | Sort-Object -Property "LastName","FirstName" -Descending:$false

		$results[0] | Should -Be $employees[1]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[2]
	}

	It "Sort-Object with -Descending and Two Order Entry Keys should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
		$employees = @($employee1,$employee2,$employee3)
		$results = $employees | Sort-Object -Property "LastName","FirstName" -Descending

		$results[0] | Should -Be $employees[2]
		$results[1] | Should -Be $employees[0]
		$results[2] | Should -Be $employees[1]
	}

	It "Sort-Object with Two Order Entry Keys with asc=true should work"{
		$employee1 = [pscustomobject]@{"FirstName"="john"; "LastName"="smith"; "YearsInMS"=5}
		$employee2 = [pscustomobject]@{"FirstName"="joesph"; "LastName"="smith"; "YearsInMS"=15}
		$employee3 = [pscustomobject]@{"FirstName"="john"; "LastName"="smyth"; "YearsInMS"=12}
		$employees = @($employee1,$employee2,$employee3)
		$ht1 = @{"e"="FirstName"; "asc"=$true}
		$ht2 = @{"e"="LastName";}
		$results = $employees | Sort-Object -Property @($ht1,$ht2) -Descending

		$results[0] | Should -Be $employees[1]
		$results[1] | Should -Be $employees[2]
		$results[2] | Should -Be $employees[0]
	}

	It "Sort-Object with Descending No Property should work"{
		$employee1 = 1
		$employee2 = 2
		$employee3 = 3
		$employees = @($employee1,$employee2,$employee3)
		$results = $employees | Sort-Object -Descending

		$results[0] | Should -Be 3
		$results[1] | Should -Be 2
		$results[2] | Should -Be 1
	}
}

Describe 'Sort-Object Stable Unit Tests' -Tags 'CI' {

	Context 'Modulo stable sort' {

		$unsortedData = 1..20

		It 'Return each value in an ordered set, sorted by the value modulo 3, with items having the same result appearing in the same order' {
			$results = $unsortedData | Sort-Object {$_ % 3} -Stable

			$results[0]  | Should -Be 3
			$results[1]  | Should -Be 6
			$results[2]  | Should -Be 9
			$results[3]  | Should -Be 12
			$results[4]  | Should -Be 15
			$results[5]  | Should -Be 18
			$results[6]  | Should -Be 1
			$results[7]  | Should -Be 4
			$results[8]  | Should -Be 7
			$results[9]  | Should -Be 10
			$results[10] | Should -Be 13
			$results[11] | Should -Be 16
			$results[12] | Should -Be 19
			$results[13] | Should -Be 2
			$results[14] | Should -Be 5
			$results[15] | Should -Be 8
			$results[16] | Should -Be 11
			$results[17] | Should -Be 14
			$results[18] | Should -Be 17
			$results[19] | Should -Be 20
		}

		It 'Return each value in an ordered set, sorted by the value modulo 3 (descending), with items having the same result appearing in the same order' {
			$results = $unsortedData | Sort-Object {$_ % 3} -Stable -Descending

			$results[0]  | Should -Be 2
			$results[1]  | Should -Be 5
			$results[2]  | Should -Be 8
			$results[3]  | Should -Be 11
			$results[4]  | Should -Be 14
			$results[5]  | Should -Be 17
			$results[6]  | Should -Be 20
			$results[7]  | Should -Be 1
			$results[8]  | Should -Be 4
			$results[9]  | Should -Be 7
			$results[10] | Should -Be 10
			$results[11] | Should -Be 13
			$results[12] | Should -Be 16
			$results[13] | Should -Be 19
			$results[14] | Should -Be 3
			$results[15] | Should -Be 6
			$results[16] | Should -Be 9
			$results[17] | Should -Be 12
			$results[18] | Should -Be 15
			$results[19] | Should -Be 18
		}

		It 'Return each value in an ordered set, sorted by the value modulo 3, discarding duplicates' {
			$results = $unsortedData | Sort-Object {$_ % 3} -Stable -Unique

			$results[0]  | Should -Be 3
			$results[1]  | Should -Be 1
			$results[2]  | Should -Be 2
		}

		It 'Return each value in an ordered set, sorted by the value modulo 3 (descending), discarding duplicates' {
			$results = $unsortedData | Sort-Object {$_ % 3} -Stable -Unique -Descending

			$results[0]  | Should -Be 2
			$results[1]  | Should -Be 1
			$results[2]  | Should -Be 3
		}
	}
}

Describe 'Sort-Object Top and Bottom Unit Tests' -Tags 'CI' {

	# Helper function to compare two sort entries
	function Compare-SortEntry
	{
		param($nSortEntry, $fullSortEntry)
		if ($nSortEntry -is [System.Array]) {
			# Arrays are compared using reference equality to ensure that the original array was
			# moved to the correct position in both sorts; value equality doesn't verify this
			[object]::ReferenceEquals($nSortEntry, $fullSortEntry) | Should -BeTrue
		} else {
			$nSortEntry | Should -Be $fullSortEntry
		}
	}

	# Helper function that compares a full sort with an n-sort
	function Test-SortObject {
		param([array]$unsortedData, [hashtable]$baseSortParameters, [string]$nSortType, [int]$nValue)
		$nSortParameters = @{
			$nSortType = $nValue
		}
		# Sort the data
		$fullSortResults = $unsortedData | Sort-Object @baseSortParameters
		$nSortResults = $unsortedData | Sort-Object @baseSortParameters @nSortParameters
		# Verify the counts when not doing a -Unique sort
		if (-not $baseSortParameters.ContainsKey('Unique')) {
			$nSortResults.Count | Should -Be $(if ($nSortParameters[$nSortType] -gt $unsortedData.Length) {$unsortedData.Length} else {$nSortParameters[$nSortType]})
			$fullSortResults.Count | Should -Be $unsortedData.Length
		}
		# Compare the n-sort result entries with their corresponding full sort result entries
		if ($nSortType -eq 'Top') {
			$range = 0..$($nSortResults.Count - 1)
		} else {
			$range = -$nSortResults.Count..-1
		}
		foreach ($i in $range) {
			Compare-SortEntry $nSortResults[$i] $fullSortResults[$i]
		}
	}

	# Test cases when only the n-sort type needs to be changed
	$topBottom = @(
		@{nSortType='Top'   }
		@{nSortType='Bottom'}
	)

	# Test cases when the n-sort type and the order type need to be changed
	$topBottomAscendingDescending = @(
		@{nSortType='Top';    orderType='ascending' }
		@{nSortType='Top';    orderType='descending'}
		@{nSortType='Bottom'; orderType='ascending' }
		@{nSortType='Bottom'; orderType='descending'}
	)

	Context 'Integer n-sort' {

		$unsortedData = 973474993,271612178,-1258909473,659770354,1829227828,-1709391247,-10835210,-1477737798,1125017828,813732193

		It 'Return the <nSortType> N sorted in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Descending = $orderType -eq 'descending'}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return all sorted in <ordertype> order when -<nSortType> is too large' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Descending = $orderType -eq 'descending'}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count + 10)
		}

	}

	Context 'Heterogeneous n-sort' {

		$unsortedData = @(
			'x'
			Get-Alias where
			'c'
			([pscustomobject]@{}) | Add-Member -Name ToString -Force -MemberType ScriptMethod -Value {$null} -PassThru # Use this syntax to get $null from a default sort (uses ToString)
			42
			Get-Alias foreach
			,@('a','b','c') # Use this syntax to pass an array with a few values to sort-object (also useful when testing sorts with unexpected data)
			[pscustomobject]@{Name='NotAnAlias'}
			[pscustomobject]@{Name=$null;Definition='Custom'}
			'z'
			,@($null) # Use this syntax to pass an array with a single $null value to sort-object (also useful when testing sorts with unexpected data)
		)

		It 'Return the <nSortType> N sorted in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Descending = $orderType -eq 'descending'}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property = 'Name'}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

	}

	Context 'Homogeneous n-sort' {

		$unsortedData = @(
			[pscustomobject]@{PSTypeName='Employee';FirstName='Dwayne';LastName='Smith' ;YearsInMS=8    }
			[pscustomobject]@{PSTypeName='Employee';FirstName='Lucy'  ;                 ;YearsInMS=$null}
			[pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Jones' ;YearsInMS=-2   }
			[pscustomobject]@{PSTypeName='Employee';FirstName='Sylvie';LastName='Landry';YearsInMS=1    }
			[pscustomobject]@{PSTypeName='Employee';FirstName='Jack'  ;LastName='Frank' ;YearsInMS=5    }
			[pscustomobject]@{PSTypeName='Employee';FirstName='John'  ;LastName='smith' ;YearsInMS=6    }
			[pscustomobject]@{PSTypeName='Employee';FirstName='Joseph';LastName='Smith' ;YearsInMS=15   }
			[pscustomobject]@{PSTypeName='Employee';FirstName='John'  ;LastName='Smyth' ;YearsInMS=12   }
		)

		It 'Return the <nSortType> N sorted by property in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property = 'YearsInMS'}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property in <orderType> order (unique, case-insensitive)' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property='LastName';Unique=$true}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property in <orderType> order (unique, case-sensitive)' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property='LastName';CaseSensitive=$true;Unique=$true}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property with an order entry key' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false}}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property with multiple non-conflicting order entry keys' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$true}}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by property with multiple conflicting order entry keys' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Descending=$true;Property=@{Expression='YearsInMS'; Descending=$false; Ascending=$false}}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by multiple properties with different order entry keys' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Property=@{Expression='LastName';Ascending=$false},@{Expression='FirstName';Ascending=$true}}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by two properties in descending order' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Property='LastName','FirstName';Descending=$true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by two properties with -Descending:$false' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Property='LastName','FirstName';Descending=$false}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N sorted by two properties with mixed sort order' -TestCases $topBottom {
			param([string]$nSortType)
			$baseSortParameters = @{Property=@{Expression='FirstName';Ascending=$true},@{Expression='LastName'};Descending=$true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

	}

	Context 'N-sort of objects that do not define ToString' {

		Add-Type -TypeDefinition 'public enum PipelineState{NotStarted,Running,Stopping,Stopped,Completed,Failed,Disconnected}'
		$unsortedData = @(
			[pscustomobject]@{PipelineId=1;Cmdline='cmd3';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
			[pscustomobject]@{PipelineId=2;Cmdline='cmd1';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
			[pscustomobject]@{PipelineId=3;Cmdline='cmd2';Status=[PipelineState]::Completed;StartTime=[DateTime]::Now;EndTime=[DateTime]::Now.AddSeconds(5.0)}
		)

		It 'Return the <nSortType> N sorted in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Descending = $orderType -eq 'descending'}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

	}

	Context 'N-sort of objects with some null property values' {

		$item0 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
		$item1 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
		$item1.TypeName = 'DeeType'
		$item2 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
		$item2.TypeName = 'B-Type'
		$item3 = New-Object -TypeName Microsoft.PowerShell.Commands.NewObjectCommand
		$item3.TypeName = 'A-Type'
		$unsortedData = @($item0,$item1,$item2,'b',$item3)

		It 'Return the <nSortType> N objects by property in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property='TypeName'}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

		It 'Return the <nSortType> N by script property in <orderType> order' -TestCases $topBottomAscendingDescending {
			param([string]$nSortType, [string]$orderType)
			$baseSortParameters = @{Property={$_.TypeName}}
			if ($orderType -eq 'Descending') {$baseSortParameters['Descending'] = $true}
			Test-SortObject $unsortedData $baseSortParameters $nSortType ($unsortedData.Count - 1)
		}

	}

}
