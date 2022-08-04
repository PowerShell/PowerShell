# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "UnixFileSystem additions" -Tag "CI" {
    Context "Basic Validation" {
        BeforeAll {
            $PSDefaultParameterValues.Add('It:Skip', $IsWindows)
        }

        AfterAll {
            $PSDefaultParameterValues.Remove('It:Skip')
        }

        It "Should include a UnixStat property" {
            $i = Get-Item ${TestDrive}
            $i.UnixStat | Should -Not -BeNullOrEmpty
        }

        It "The UnixStat property should be the correct type" {
            $expected = "System.Management.Automation.Platform+Unix+CommonStat"
            $i = (Get-Item /).psobject.properties['UnixStat'].TypeNameOfValue
            $i | Should -Be $expected
        }
    }

    Context "Validation of additional properties on file system objects" {
        BeforeAll {
            $PSDefaultParameterValues.Add('It:Skip', $IsWindows)

            $testDir  = "${TestDrive}/TestDir"
            $testFile = "${testDir}/TestFile"

            $testCase = @{ Mode = '000';  Perm = '----------'; Item = "${testFile}" },
                        @{ Mode = '111';  Perm = '---x--x--x'; Item = "${testFile}" },
                        @{ Mode = '222';  Perm = '--w--w--w-'; Item = "${testFile}" },
                        @{ Mode = '333';  Perm = '--wx-wx-wx'; Item = "${testFile}" },
                        @{ Mode = '444';  Perm = '-r--r--r--'; Item = "${testFile}" },
                        @{ Mode = '555';  Perm = '-r-xr-xr-x'; Item = "${testFile}" },
                        @{ Mode = '666';  Perm = '-rw-rw-rw-'; Item = "${testFile}" },
                        @{ Mode = '777';  Perm = '-rwxrwxrwx'; Item = "${testFile}" },
                        @{ Mode = '4777'; Perm = '-rwsrwxrwx'; Item = "${testFile}" },
                        @{ Mode = '1777'; Perm = 'drwxrwxrwt'; Item = "${testDir}"  }
        }

        AfterAll {
            $PSDefaultParameterValues.Remove('It:Skip')
        }

        BeforeEach {
            $null = New-Item -ItemType Directory -Path "${testDir}"
            $null = New-Item -ItemType File -Path "${testFile}"
        }

        AfterEach {
            Remove-Item -Path "${testFile}" -Force
            Remove-Item -Path "${testDir}"  -Recurse -Force
        }

        It "Should present filemode '<Mode>' string correctly as '<Perm>'" -testCase $testCase {
            param ($Mode, $Perm, $Item )
            chmod "$Mode" "${Item}"
            $i = Get-Item $Item
            $i.UnixMode | Should -Be $Perm
        }

        It "Should retrieve the user name for the file" {
            $i = Get-Item ${testFile}
            $user = (/bin/ls -ld $testFile).split(" ",[System.StringSplitOptions]"RemoveEmptyEntries")[2]
            $i.User | Should -Be $user
        }

        It "Should retrieve the group name for the file" {
            $i = Get-Item ${testFile}
            $group = (/bin/ls -ld $testFile).split(" ",[System.StringSplitOptions]"RemoveEmptyEntries")[3]
            $i.Group | Should -Be $Group
        }
    }

    Context "Other properties of UnixStat object" {
        BeforeAll {
            $PSDefaultParameterValues.Add('It:Skip', $IsWindows)
            if ($IsWindows) {
                return
            }

            $testDir  = "${TestDrive}/TestDir"
            $testFile = "${testDir}/TestFile"

            $null = New-Item -Type Directory -Path $testDir
            $null = New-Item -Type File -Path $testFile
            Set-Content -Path ${testFile} -Value "abc"

            $expectedFileInode,$permission,$expectedFileLinkCount,$user,$group,$expectedFileSize,$unused =
                (/bin/ls -ldi ${testFile}).Split(" ", 7, [System.StringSplitOptions]"RemoveEmptyEntries")
            $file = Get-Item ${testFile}

            $expectedDirInode,$permission,$expectedDirLinkCount,$user,$group,$expectedDirSize,$unused =
                (/bin/ls -ldi ${testDir}).Split(" ", 7, [System.StringSplitOptions]"RemoveEmptyEntries")
            $Dir = Get-Item ${testDir}

            $testCases =
                @{ Expected = $expectedFileInode; Observed = $File.UnixStat.Inode; Title = "FileInode" },
                @{ Expected = $expectedFileLinkCount; Observed = $File.UnixStat.HardlinkCount; Title = "FileHardlinkCount" },
                @{ Expected = $expectedFileSize; Observed = $File.UnixStat.Size; Title = "FileSize" },
                @{ Expected = $expectedDirInode; Observed = $Dir.UnixStat.Inode; Title = "DirInode" },
                @{ Expected = $expectedDirLinkCount; Observed = $Dir.UnixStat.HardlinkCount; Title = "DirHardlinkCount" },
                @{ Expected = $expectedDirSize; Observed = $Dir.UnixStat.Size; Title = "DirSize" }
        }

        AfterAll {
            $PSDefaultParameterValues.Remove('It:Skip')
        }

        It "Should have correct values in UnixStat property for '<Title>'" -TestCases $testCases {
            param ( $Title, $expected, $observed )

            $observed | Should -Be $expected

        }
    }

}
