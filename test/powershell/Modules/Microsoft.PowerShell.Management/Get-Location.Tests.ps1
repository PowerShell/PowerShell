# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.
Describe "Get-Location" -Tags "CI" {
    $currentDirectory=[System.IO.Directory]::GetCurrentDirectory()

    BeforeEach {
        Push-Location $currentDirectory
    }

    AfterEach {
	    Pop-location
    }

    It "Should list the output of the current working directory" {
	    (Get-Location).Path | Should -BeExactly $currentDirectory
    }

    It "Should do exactly the same thing as its alias" {
	    (pwd).Path | Should -BeExactly (Get-Location).Path
    }

    It "Should throw an exception when missing an argument for parameter 'PSProvider'" {
        { Get-Location -PSProvider } | Should -Throw "Missing an argument for parameter 'PSProvider'."
    }

    It "Should throw an exception when missing an argument for parameter 'PSDrive'" {
        { Get-Location -PSDrive } | Should -Throw "Missing an argument for parameter 'PSDrive'."
    }

    It "Should throw an exception when missing an argument for parameter 'StackName'" {
        { Get-Location -StackName } | Should -Throw "Missing an argument for parameter 'StackName'."
    }

    It "Should return a PathInfo object when no parameters are given" {
        Get-Location | Should -BeOfType System.Management.Automation.PathInfo
    }

    It "Should return a PathInfo object given a valid argument for parameter 'PSDrive'" {
        $tempPSDriveName = "GeLocationTempPSDrive"

        $tempPSDrive = New-PSDrive -Name $tempPSDriveName -PSProvider "FileSystem" -Root $currentDirectory

        Get-Location -PSDrive $tempPSDriveName | Should -BeOfType System.Management.Automation.PathInfo

        $tempPSDrive | Remove-PSDrive
    }

    It "Should return a PathInfo object given a valid argument for parameter 'PSProvider'" {
        Get-Location -PSProvider alias | Should -BeOfType System.Management.Automation.PathInfo
    }

    It "Should return a PathInfoStack object for parameter 'Stack'" {
        Get-Location -Stack | Should -BeOfType System.Management.Automation.PathInfoStack
    }

    It "Should return a PathInfoStack with the correct values for parameter 'Stack'" {
        $stackAsArray = (Get-Location -Stack).ToArray()

        $stackAsArray.Length | Should -BeExactly 1

        $stackAsArray[0] | Should -BeExactly $currentDirectory
    }

    It "Should return a PathInfoStack with the correct values for the argument for parameter 'StackName'" {
        $tempDirectory = Join-Path -Path $currentDirectory -ChildPath "getLocationTempDir"

        New-Item -Path ($tempDirectory) -ItemType "directory"

        Set-Location -Path $tempDirectory

        Push-Location $currentDirectory -StackName "Stack1"

        $stackAsArray = (Get-Location -StackName "Stack1").ToArray()

        $stackAsArray.Length | Should -BeExactly 1

        $stackAsArray[0].Path | Should -BeExactly $tempDirectory

        Remove-Item -Path $tempDirectory
    }
}
