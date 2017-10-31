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
            $item_G = "5560A62F-74F1-4FAE-9A23-F4EBD90D2676" 
            $null = New-Item -Path $TestDrive -Name $item_a -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_B -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_c -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_D -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name $item_E -ItemType "Directory" -Force
            $null = New-Item -Path $TestDrive -Name $item_F -ItemType "File" -Force | ForEach-Object {$_.Attributes = "hidden"}
            $null = New-Item -Path (Join-Path -Path $TestDrive -ChildPath $item_E) -Name $item_G -ItemType "File" -Force
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
        It "Should find the hidden file if specified with hidden switch" {
            $file = Get-ChildItem -Path (Join-Path $TestDrive $item_F) -Hidden
            $file | Should Not BeNullOrEmpty
            $file.Count | Should be 1
            $file.Name | Should be $item_F
        }

        It "Should list items in current directory only with depth set to 0" {
            (Get-ChildItem -Path $TestDrive -Depth 0).Count | Should Be 5
            (Get-ChildItem -Path $TestDrive -Depth 0 -Include *).Count | Should Be 5
            (Get-ChildItem -Path $TestDrive -Depth 0 -Exclude IntentionallyNonexistent).Count | Should Be 5
        }

        It "Should return items recursively when using 'Include' or 'Exclude' parameters" {
            (Get-ChildItem -Path $TestDrive -Depth 1).Count | Should Be 6
            (Get-ChildItem -Path $TestDrive -Depth 1 -Include $item_G).Count | Should Be 1
            (Get-ChildItem -Path $TestDrive -Depth 1 -Exclude $item_a).Count | Should Be 5
        }
    }

    Context 'Env: Provider' {

        It 'can handle mixed case in Env variables' {
            try
            {
                $env:__FOOBAR = 'foo'
                $env:__foobar = 'bar'

                $foobar = Get-Childitem env: | Where-Object {$_.Name -eq '__foobar'}
                $count = if ($IsWindows) { 1 } else { 2 }
                ($foobar | measure).Count | Should Be $count
            }
            catch
            {
                Get-ChildItem env: | Where-Object {$_.Name -eq '__foobar'} | Remove-Item -ErrorAction SilentlyContinue
            }
        }
    }
}
