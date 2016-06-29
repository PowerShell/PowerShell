function CreateDirStructure
{
    param(
        [string] $Dir,
        [int] $Width,
        [int] $Depth
    )

   1..$Width | % {
      $newDirPath = Join-Path $Dir ("Level"+$Depth+$_)
      $newFilePath = $newDirPath + ".txt"
      mkdir -Path $newDirPath -Force | Out-Null
      Get-Date | Out-File $newFilePath
      if ($Depth -gt 0) { CreateDirStructure $newDirPath $Width ($Depth - 1) }
   }
}

#Pester.psm1 does not currently implement BeforeAll / AfterAll for suite setup/cleanup.
$testDir = "$env:TEMP\GetChildItemDepthTests"
CreateDirStructure $testDir 2 2

<#  Above produces this structure:
$env:Temp\GetChildItemDepthTests\Level21
$env:Temp\GetChildItemDepthTests\Level21.txt
$env:Temp\GetChildItemDepthTests\Level21\Level11
$env:Temp\GetChildItemDepthTests\Level21\Level11.txt
$env:Temp\GetChildItemDepthTests\Level21\Level11\Level01
$env:Temp\GetChildItemDepthTests\Level21\Level11\Level01.txt
$env:Temp\GetChildItemDepthTests\Level21\Level11\Level02
$env:Temp\GetChildItemDepthTests\Level21\Level11\Level02.txt
$env:Temp\GetChildItemDepthTests\Level21\Level12
$env:Temp\GetChildItemDepthTests\Level21\Level12.txt
$env:Temp\GetChildItemDepthTests\Level21\Level12\Level01
$env:Temp\GetChildItemDepthTests\Level21\Level12\Level01.txt
$env:Temp\GetChildItemDepthTests\Level21\Level12\Level02
$env:Temp\GetChildItemDepthTests\Level21\Level12\Level02.txt
$env:Temp\GetChildItemDepthTests\Level22
$env:Temp\GetChildItemDepthTests\Level22.txt
$env:Temp\GetChildItemDepthTests\Level22\Level11
$env:Temp\GetChildItemDepthTests\Level22\Level11.txt
$env:Temp\GetChildItemDepthTests\Level22\Level11\Level01
$env:Temp\GetChildItemDepthTests\Level22\Level11\Level01.txt
$env:Temp\GetChildItemDepthTests\Level22\Level11\Level02
$env:Temp\GetChildItemDepthTests\Level22\Level11\Level02.txt
$env:Temp\GetChildItemDepthTests\Level22\Level12
$env:Temp\GetChildItemDepthTests\Level22\Level12.txt
$env:Temp\GetChildItemDepthTests\Level22\Level12\Level01
$env:Temp\GetChildItemDepthTests\Level22\Level12\Level01.txt
$env:Temp\GetChildItemDepthTests\Level22\Level12\Level02
$env:Temp\GetChildItemDepthTests\Level22\Level12\Level02.txt
#>

Describe "Tests for Depth parameter of Get-ChildItem" -Tags "P1", "RI" {
    
    It "Depth 0 for items" {
         (Get-ChildItem $testDir -Recurse -Depth 0).Count -eq (Get-ChildItem $testDir).Count | Should Be $true
    }
    It "Depth 0 for names" {
         (Get-ChildItem $testDir -Recurse -Depth 0 -Name).Count -eq (Get-ChildItem $testDir -Name).Count | Should Be $true
    }
    It "FileSystemProvider limited recursion for items" {
	 (Get-ChildItem $testDir -Recurse -Depth 1).Count | Should Be 12
    }
    It "FileSystemProvider limited recursion for names" {
	 (Get-ChildItem $testDir -Recurse -Depth 1 -Name).Count | Should Be 12
    }
    It "RegistryProvider limited recursion for items" {
	 $topLevelOnlyCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles").Count
         $depth1RecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Recurse -Depth 1).Count
         $fullRecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Recurse).Count
         ($depth1RecursionCount -gt $topLevelOnlyCount) -and ($depth1RecursionCount -lt $fullRecursionCount) | Should Be $true
    }
    It "RegistryProvider limited recursion for names" {
	 $topLevelOnlyCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Name).Count
         $depth1RecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Recurse -Depth 1 -Name).Count
         $fullRecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Recurse -Name).Count
         ($depth1RecursionCount -gt $topLevelOnlyCount) -and ($depth1RecursionCount -lt $fullRecursionCount) | Should Be $true
    }
    It "Implicit recursion for FileSystemProvider" {
	 (Get-ChildItem $testDir -Depth 1).Count -eq (Get-ChildItem -Recurse $testDir -Depth 1).Count | Should Be $true
    }
    It "Implicit recursion for RegistryProvider" {
	 $explicitRecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Recurse -Depth 0 -Name).Count
	 $implicitRecursionCount = (Get-ChildItem "hklm:\SYSTEM\CurrentControlSet\Hardware Profiles" -Depth 0 -Name).Count
	 $implicitRecursionCount | Should Be $explicitRecursionCount
    }
}

del $testDir -Recurse -Force