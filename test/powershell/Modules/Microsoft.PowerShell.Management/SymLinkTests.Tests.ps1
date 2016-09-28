function Clean-State {
    
    if (Test-Path $FullyQualifiedLink)
    {
	Remove-Item $FullyQualifiedLink -Force
    }
    
    if (Test-Path $FullyQualifiedChildFolder)
    {
        Remove-Item $FullyQualifiedChildFolder -Force
    }

    if (Test-Path $FullyQualifiedFolder)
    {
        Remove-Item $FullyQualifiedFolder -Force
    }

    if (Test-Path $tmpDirectory)
    {
        Remove-Item $tmpDirectory -Force
    }
}

Describe "New-Item" -Tags "CI" {

$tmpDirectory                 = $TestDrive
$ChildFolder                  = "childFolder"
$Folder                       = "newDirectory"
$Symlink                      = "Symlink"    
$FullyQualifiedFolder         = Join-Path -Path $tmpDirectory -ChildPath $Folder
$FullyQualifiedChildFolder    = Join-Path -Path $FullyQualifiedFolder -ChildPath $ChildFolder
$FullyQualifiedLink           = Join-Path -Path $tmpDirectory -ChildPath $SymLink


BeforeEach {
    Clean-State
}

    It "Should be able to detect a recursive symlink" {

	New-Item -ItemType directory -Name $parentfolder -Path $tmpDirectory
	New-Item -ItemType directory -Name $childfolder -Path $FullyQualifiedChildFolder
	New-Item -ItemType SymbolicLink -Target $tmpDirectory -Name $symlink -Path $FullyQualifiedLink
	
	Test-Path $FullyQualifiedLink                       | Should Be $true
	Test-Path $FullyQualifiedChildFolder                | Should Be $true
	Test-Path $FullyQualifiedFolder                     | Should Be $true

	$recurseInfo = Get-ChildItem $FullyQualifiedLink
	$recurseInfo.Target                                 | Should Match ([regex]::Escape($tmpDirectory))
	$recurseInfo.LinkType                               | Should Be "SymbolicLink"

	#detect recursive symlink
	$recurse = Get-ChildItem -r $tmpDirectory
	$recurseString = "" 
	foreach ($row in $recurse)
	{
	    $recurseString += $row[0] 
	    $recurseString += " " 
	}
	
	$recurseString | Should Be "newDirectory testlink childFolder "
		
	# Remove the link explicitly to avoid broken symlink issue
	Remove-Item $FullyQualifiedLink -Force

    }
}
