Describe "Get-ChildItem" {
    It "Should list the contents of the current folder" {
	(Get-ChildItem .).Name.Length | Should BeGreaterThan 0
    }

    It "Should list the contents of the home directory" {
	pushd $HOME
	(Get-ChildItem .).Name.Length | Should BeGreaterThan 0
	popd
    }

    It "Should have a the proper fields and be populated" {
	$var = Get-Childitem .

	$var.Name.Length   | Should BeGreaterThan 0
	$var.Mode.Length   | Should BeGreaterThan 0
	$var.LastWriteTime | Should BeGreaterThan 0
	$var.Length.Length | Should BeGreaterThan 0

    }

    It "Should list files in sorted order" {
	New-Item -Path $TestDrive -Name "a" -ItemType "File" -Force
	New-Item -Path $TestDrive -Name "B" -ItemType "File" -Force
	New-Item -Path $TestDrive -Name "c" -ItemType "File" -Force
	New-Item -Path $TestDrive -Name "D" -ItemType "File" -Force
	New-Item -Path $TestDrive -Name "E" -ItemType "Directory" -Force

	$files = Get-ChildItem -Path $TestDrive
	$files[0].Name     | Should Be "E"
	$files[1].Name     | Should Be "a"
	$files[2].Name     | Should Be "B"
	$files[3].Name     | Should Be "c"
	$files[4].Name     | Should Be "D"
    }
}
