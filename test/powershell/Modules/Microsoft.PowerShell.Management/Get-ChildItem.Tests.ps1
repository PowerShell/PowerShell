Describe "Get-ChildItem" -Tags "CI" {

    Context 'FileSystem provider' {

        BeforeAll {
            # Create Test data
            $null = New-Item -Path $TestDrive -Name "a" -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name "B" -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name "c" -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name "D" -ItemType "File" -Force
            $null = New-Item -Path $TestDrive -Name "E" -ItemType "Directory" -Force
            $null = New-Item -Path $TestDrive -Name ".F" -ItemType "File" -Force | %{$_.Attributes = "hidden"}
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
            $files[0].Name     | Should Be "E"
            $files[1].Name     | Should Be "a"
            $files[2].Name     | Should Be "B"
            $files[3].Name     | Should Be "c"
            $files[4].Name     | Should Be "D"
        }

        It "Should list hidden files as well when 'Force' parameter is used" {
            $files = Get-ChildItem -path $TestDrive -Force
            $files | Should not be $null
            $files.Count | Should be 6
            $files.Name.Contains(".F")
        }

        It "Should list only hidden files when 'Hidden' parameter is used" {
            $files = Get-ChildItem -path $TestDrive -Hidden
            $files | Should not be $null
            $files.Count | Should be 1
            $files[0].Name | Should Be ".F"
        }
        It "Should give .sys file if the fullpath is specified with hidden and force parameter" -Skip:(!$IsWindows){
            $file = Get-ChildItem -path "$env:SystemDrive\\pagefile.sys" -Hidden
            $file | Should not be $null
            $file.Count | Should be 1
            $file.Name | Should be "pagefile.sys"
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
