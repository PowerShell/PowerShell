Describe "Get-ChildItem" -Tags "CI" {

    Context 'FileSystem provider' {

        BeforeAll {
            # Create Test data
            $item_a = "a3fe710a-31af-4834-bc29-d0b584589838"
            $item_B = "B1B691A9-B7B1-4584-AED7-5259511BEEC4"
            $item_c = "c283d143-2116-4809-bf11-4f7d61613f92"
            $item_D = "D39B4FD9-3E1D-4DD5-8718-22FE2C934CE3"
            $item_E = "EE150FEB-0F21-4AFF-8066-AF59E925810C"
            $item_F = ".F81D8514-8862-4227-B041-0529B1656A43" 
            $null = New-Item -Path $TestDrive -Name $item_a -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_B -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_c -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_D -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_E -ItemType "Directory" -Force
            $null = New-Item -Path $TestDrive -Name $item_F -ItemType "File" -Force | %{$_.Attributes = "hidden"}
        }

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
            $files = Get-ChildItem -Path $TestDrive
            $files[0].Name     | Should Be $item_E
            $files[1].Name     | Should Be $item_a
            $files[2].Name     | Should Be $item_B
            $files[3].Name     | Should Be $item_c
            $files[4].Name     | Should Be $item_D
        }

        It "Should list hidden files as well when 'Force' parameter is used" {
            $files = Get-ChildItem -path $TestDrive -Force
            $files | Should not be $null
            $files.Count | Should be 6
            $files.Name.Contains($item_F) | Should Be $true
        }

        It "Should list only hidden files when 'Hidden' parameter is used" {
            $files = Get-ChildItem -path $TestDrive -Hidden
            $files | Should not be $null
            $files.Count | Should be 1
            $files[0].Name | Should Be $item_F
        }
        It "Should give .sys file if the fullpath is specified with hidden and force parameter" -Skip:(!$IsWindows){
            $file = Get-ChildItem -path "$env:SystemDrive\\pagefile.sys" -Hidden
            $file | Should not be $null
            $file.Count | Should be 1
            $file.Name | Should be "pagefile.sys"
        }
        # Test is pending on Unix platforms because of a behavior change in the latest .NET Core.
        # Tracked by https://github.com/dotnet/corefx/issues/20456
        It "Should continue enumerating a directory when a contained item is deleted" -Pending:(!$IsWindows) {
            $Error.Clear()
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("GciEnumerationActionDelete", $true)
            $result = Get-ChildItem -Path $TestDrive -ErrorAction SilentlyContinue
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("GciEnumerationActionDelete", $false)
            if ($IsWindows)
            {
                $Error.Count | Should BeExactly 0
                $result.Count | Should BeExactly 5
            }
            else
            {
                $Error.Count | Should BeExactly 1
                $Error[0].FullyQualifiedErrorId | Should BeExactly "DirIOError,Microsoft.PowerShell.Commands.GetChildItemCommand"
                $Error[0].Exception | Should BeOfType System.Io.FileNotFoundException
                $result.Count | Should BeExactly 4
            }
        }
        # Test is pending on Unix platforms because of a behavior change in the latest .NET Core.
        # Tracked by https://github.com/dotnet/corefx/issues/20456
        It "Should continue enumerating a directory when a contained item is renamed" -Pending:(!$IsWindows) {
            $Error.Clear()
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("GciEnumerationActionRename", $true)
            $result = Get-ChildItem -Path $TestDrive -ErrorAction SilentlyContinue
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("GciEnumerationActionRename", $false)
            if ($IsWindows)
            {
                $Error.Count | Should BeExactly 0
                $result.Count | Should BeExactly 4
            }
            else
            {
                $Error.Count | Should BeExactly 1
                $Error[0].FullyQualifiedErrorId | Should BeExactly "DirIOError,Microsoft.PowerShell.Commands.GetChildItemCommand"
                $Error[0].Exception | Should BeOfType System.Io.FileNotFoundException
                $result.Count | Should BeExactly 3
            }
        }
    }

    Context 'Env: Provider' {

        It 'can handle mixed case in Env variables' {
            try
            {
                $env:__FOOBAR = 'foo'
                $env:__foobar = 'bar'

                $foobar = Get-Childitem env: | ? {$_.Name -eq '__foobar'}
                $count = if ($IsWindows) { 1 } else { 2 }
                ($foobar | measure).Count | Should Be $count
            }
            catch
            {
                Get-ChildItem env: | ? {$_.Name -eq '__foobar'} | Remove-Item -ErrorAction SilentlyContinue
            }
        }
    }
}
