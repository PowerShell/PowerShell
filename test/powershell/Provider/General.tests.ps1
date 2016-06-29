Describe "FileSystem Provider Tests" -tags 'InnerLoop', 'DRT' {
    Context "FileSystem Regression Test: Win7 bug #213606" {
        BeforeAll {
            $testBaseDirectory = "TESTDRIVE:\providerScriptTests"
            if ( test-path $testBaseDirectory )
            {
                remove-item -recurse -force $testBaseDirectory
            }
            new-item -itemtype Directory $testBaseDirectory | out-null
            $wildcardTestCases = @('[hello]', 'z]', '[a-l].txt')
            $testDir = "${testBaseDirectory}\213606"
            new-item -itemType Directory -Path "${testDir}" | out-null
        }
        AfterAll {
            Remove-Item -Recurse -Force ${testBaseDirectory}
        }
        # loop the wildcardtestcases
        foreach ( $case in $wildcardTestCases )
        {
            It "directory named $case" {
                try
                {
                    push-location "${testDir}"
                    New-Item -Path . -Name $case -itemtype Directory | out-null
                    push-location -literalpath $case 
                    123 | out-file testfile
                    Get-ChildItem | out-null
                    throw "OK"
                }
                catch
                {
                        $_.FullyQualifiedErrorId | Should be "OK"
                }
                finally
                {
                    pop-location # for -literalpath $case
                    pop-location # for ${testDir}
                }
            }
        }
    }

    Context "FileSystem Regression Test: Win7 bug #220240" {
        BeforeAll {
            $deviceNames =  'CON', 'PRN', 'AUX', 'NUL', 'COM1', 'COM2', 
                'COM3', 'COM4', 'COM5', 'COM6', 'COM7', 'COM8', 'COM9', 
                'LPT1', 'LPT2', 'LPT3', 'LPT4', 'LPT5', 'LPT6', 'LPT7', 
                'LPT8', 'LPT9'
            $testFile = "TESTDRIVE:\file.txt"
            $testValue = "83294hasfisadfh983rh"
            $expectedError = "MoveError,Microsoft.PowerShell.Commands.MoveItemCommand"
        }

        if ( test-path $testFile )
        {
            remove-item -force $testFile
        }
        Set-Content -Path $testFile -Value $testValue

        Foreach($deviceName in $deviceNames)
        {
            try
            {
                Move-Item -Path $testFile -Destination $deviceName -EA Stop
                Throw "OK"
            }
            catch
            {
                It $deviceName {
                    $_.FullyQualifiedErrorId | Should be $expectedError
                }
            }
        }
    }

    # P1
    Context "FileSystem Regression Test: Win7 bug 300987" {
        BeforeAll {
            $testFile = "TESTDRIVE:\Bug300987.txt"
            if (Test-Path $testFile) { Remove-Item $testFile -Force -Recurse}
            New-Item -Path $testFile -ItemType file -Value "hello" | out-null
            $d = Get-ChildItem $testFile
            $expectedPath = (get-psdrive TESTDRIVE).root
        }

        It "Directory name should not be null." {
            $d.DirectoryName | Should Not BeNullOrEmpty
            }
        It "DIR displayed full provider directory name, which is not expected." {
            $d.DirectoryName | Should Not Match "::"
            }
        It "Reports correct Path" {
            $d.DirectoryName | Should be $expectedPath
            }
    }

    Context "FileSystem Regression Test: Win7 bug 401207 (Get-PsDrive should return used and free space)" {
        BeforeAll {
            $BugtestDrive = ($env:SystemDrive).Substring(0,1)
            $BugtestDrive_NA = "Variable"
            # Positive test:
            $used = (Get-PSDrive -Name $BugtestDrive).Used
            $free = (Get-PSDrive -Name $BugtestDrive).Free
            #negative test:
            $used_NA = (Get-PSDrive -Name $BugtestDrive_NA).Used
            $free_NA = (Get-PSDrive -Name $BugtestDrive_NA).Free
        }
        It "The Used property is available from (Get-PSDrive -Name $BugtestDrive)" {
            $used -gt 0 | should be $true
        }
        It "The Free property not available from (Get-PSDrive -Name $BugtestDrive)" {
            $free -gt 0 | should be $true
        }
        It "The free property is NULL from (Get-PSDrive -Name $BugtestDrive_NA)" {
            $free_NA | should benullorempty
        }
        It "The Free property is NULL from (Get-PSDrive -Name $BugtestDrive_NA)" {
            $used_NA | should benullorempty
        }
    }

    Context "FileSystem Regression Test: Win7 bug 411596" {
        BeforeAll {
            # Set up
            $signature = @"
                [DllImport("kernel32.dll")]
                public static extern uint GetShortPathName(
                    string lpszLongPath, 
                    System.Text.StringBuilder lpszShortPath, 
                    uint cchBuffer);
"@
            $GetShortPathName = "Kernel32Functions.Kernel32GetShortPathName" -as "type"
            if ( ! $GetShortPathName )
            {
                $GetShortPathName = Add-Type -MemberDefinition $signature -Name "Kernel32GetShortPathName" -Namespace "Kernel32Functions" -PassThru
            }

            $testDirectory = "TestDrive:\12345678901234567890"
            if (Test-Path $testDirectory) 
            { 
                Remove-Item $testDirectory -Force -Recurse
            }
            New-Item -Path $testDirectory -ItemType directory | out-null
            $expectedPath = Resolve-Path $testDirectory
        }

        It "ShortPathName is valid" {
            $length = $GetShortPathName::GetShortPathName($expectedPath.ProviderPath, $null, 0)
            $shortPathStringBuilder = new-object System.Text.StringBuilder($length);
            $GetShortPathName::GetShortPathName($expectedPath.ProviderPath, $shortPathStringBuilder, $length) |out-null
            $shortPath = $shortPathStringBuilder.ToString()
            $actualPath = push-location $shortPath -PassThru
            $actualPath.ProviderPath | Should be ($expectedPath.ProviderPath)
            pop-location
        }
    }

    Context "bug #516430 (Get-ChildItem regression problem: filter does not work properly)" {
        BeforeAll {
            $testPath = "TESTDRIVE:\Bug516430"
            $item1 = "testFile1"
            $item2 = "testFile2"
            $testValue = "blah";
            if (Test-Path $testPath) 
            { 
                Remove-Item $testPath -Force -Recurse
            }
            New-Item -Path $testPath -ItemType directory | out-null
            push-location $TestPath
            New-Item -Path $item1 -ItemType file -Value $testValue | out-null
            New-Item -Path $item2 -ItemType file -Value $testValue | out-null
            $actualItems = Get-ChildItem -Path $item1,$item2 -Filter $item2 -Include $item1,$item2
            pop-Location 
        }
        It "Should have the right count" {
            $actualItems.Count | should be 1
        }
        It "Should have the right name" {
            $actualItems.Name | should be $item2
        }
    }

    Context "bug #613656 (Get-Item does not work properly with folders that contain square brackets)" {
        BeforeAll {
            # $initialLocation = Get-Location
            $testFile = "[Bug613656]"
            $testDirectory = "TestDrive:\${testFile}"
            $expectedPath = join-path (get-psdrive TESTDRIVE).Root ${testFile}
            New-Item -Path $testDirectory -ItemType directory -Force | Out-Null
            Push-Location -LiteralPath $testDirectory
            $result = Get-Item .
            Pop-Location
            }
        It "Should retrieve name of directory" {
            $result | Should not BeNullOrEmpty
            }
        It "Should have only a single result" {
            $result.Length | Should be 1
        }
        It "Should have the right path" {
            $result.FullName |should be $expectedPath
            }
        }

    Context "bug #659712 (PowerShell: dir -exclude not working properly)" {
        BeforeAll {
            $testDirectory = "TESTDRIVE:\659712"
            $items = "abc123456xyz", "abc123456789", "xyz123456abc", "lmn123456xyz"
            if (Test-Path $testDirectory) { Remove-Item $testDirectory -Force -Recurse}
            New-Item -Path $testDirectory -ItemType directory | Out-Null
            push-Location $testDirectory
            New-Item -Path $items -ItemType File -Value "blah" | Out-Null
        }
        AfterAll {
            pop-location
        }
        It "Get-ChildItem . -Include *6x* should return no files" {
            Get-ChildItem . -Include *6x* | Should BeNullOrEmpty
        }
        It "Get-ChildItem -Include *6x* should return no files" {
            Get-ChildItem -Include *6x* | Should BeNullOrEmpty
        }
        It "Get-ChildItem * -Include *6x* should return 2 files" {
            $result = Get-ChildItem * -Include *6x* 
            $result.Count | Should Be 2
        }
        It "Get-ChildItem -Exclude *6x* should return 2 files and have correct names" {
            $result = Get-ChildItem -Exclude *6x*
            $result.Count | should be 2
            $expected = $items[1,2] -join ","
            ($result.Name -join ",") | Should Be $expected
        }
        It "Get-ChildItem . -Exclude *6x* should return 2 files and have correct names" {
            $result = Get-ChildItem . -Exclude *6x*
            $result.Count | should be 2
            $expected = $items[1,2] -join ","
            ($result.Name -join ",") | Should Be $expected
        }
        It "Get-ChildItem * -Exclude *6x* should return 2 files and have correct names" {
            $result = Get-ChildItem * -Exclude *6x*
            $result.Count | Should be 2
            $expected = $items[1,2] -join ","
            ($result.Name -join ",") | Should Be $expected
        }
        It "Get-ChildItem *123* -Include abc* -Exclude *xyz should return one file with correct name" {
            $result = Get-ChildItem *123* -Include abc* -Exclude *xyz
            $result.Count |Should Be 1
            $result.Name | Should be $items[1]
        }
        It "Check for bug 669542" {
            $result = Get-ChildItem abc* -Include *xyz
            $result.Count | Should be 1
            $result.Name | Should be "abc123456xyz"
        } 
    }
    
    Context "SeekRegression117443" {
        BeforeAll {
            $testDir = "TESTDRIVE:\Bug117443"
            $testFile = "testFile"
            $testValue = "12345`n12345`n12345`n12345"
            New-Item -itemtype directory -path ${testDir}| out-null
            New-Item -itemType File -path "${testDir}\${testFile}" -Value $testValue | out-null
            $testFileProviderPath = (get-item "${testDir}\${testFile}").FullName
            $reader = $executionContext.InvokeProvider.Content.GetReader($testFileProviderPath).Item(0)
        }
        AfterAll {
            $reader.close()
        }
        It "reader should return the proper count of lines" {
            $reader.Seek(0, "Begin")
            $text1 = $reader.Read(2)
            $text1.Count | Should be 2
        }
        It "The lines the reader returned should be correct" {
            $reader.Seek(0, "Begin")
            $text1 = $reader.Read(2)
            $expectedText1 = "12345"
            $text1[0] | Should be $expectedText1
            $text1[1] | Should be $expectedText1
        }
        It "After seek to last two characters, reader should have 1 line" {
            $reader.Seek(-2, "End")
            $text2 = $reader.Read(2)
            $text2[0].Length | Should be 2
        }
        It "The lines the reader returned should be correct" {
            $reader.Seek(-2, "End")
            $text2 = $reader.Read(2)
            $expectedText2 = "45"
            $text2[0] | Should be $expectedText2
        }
    }
    Context "SetContentRegression213662" {
        It "File Content should be the same" {
            $testFile = "Bug213662.txt"
            $testDir = "TESTDRIVE:\Bug213662"
            $testValue = "asfsadfsfsdafsadfa13246347";
            new-item -itemtype directory -path "${testDir}" | out-null
            push-location "${testDir}"
            Set-Content -Path $testFile -Value $testValue
            $testFile | Should ContainExactly $testValue
            pop-location
        }
    }
    Context "SetContentRegression435601" {
        It "File Content should be the same" {
            $testFile = "TESTDRIVE:\Bug435601.txt"
            $testValue = "asfsadfsfsdafsadfa13246347";
            Set-Content -LiteralPath $testFile -Value $testValue
            $testFile | Should ContainExactly $testValue
        }
    }
    Context "Set-Location fails to set correct location on ContainerCmdletProviders that define a root path" {
        BeforeAll {
            $ProviderTestDriveName = "Bug422954Drive"
            $ProviderTestDrive = "${ProviderTestDriveName}:"
            $t = @'
                using System;
                using System.Collections.Generic;
                using System.Collections.ObjectModel;
                using System.Management.Automation;
                using System.Management.Automation.Provider;
                namespace Microsoft.Provider.Test
                {
                    [CmdletProvider("Bug422954TestProvider", ProviderCapabilities.None)]
                    public class Bug422954TestProvider : ContainerCmdletProvider
                    {
                        public const string DriveName = "Bug422954Drive";
                        public const string ProviderName = "Bug422954TestProvider";
                        public Bug422954TestProvider() { }
                        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
                        {
                            PSDriveInfo drive = new PSDriveInfo(DriveName, this.ProviderInfo, "", "The provider to test Bug422954", null);
                            Collection<PSDriveInfo> drives = new Collection<PSDriveInfo>();
                            drives.Add(drive);
                            return drives;
                        }
                        protected override void GetChildItems(string path, bool recurse)
                        {
                            if (path.Contains(DriveName))
                            {
                                string message = "Path being passed to GetChildItems() is " + path;
                                ErrorRecord error = new ErrorRecord(new Exception(message), "Bug422954TestProviderError", ErrorCategory.NotSpecified, null);
                                ThrowTerminatingError(error);
                            }
                        }
                        protected override bool IsValidPath(string path) { return true; }
                        protected override bool ItemExists(string path) { return true; }
                    }
                }
'@

            if ( ! (get-psdrive Bug422954Drive -ea SilentlyContinue))
            {
                $ProviderType = Add-Type -TypeDefinition $t -passthru
                import-module -Assembly $ProviderType.assembly
            }
            $currentlocation = get-location
        }
        AfterAll {
            Set-Location $currentLocation
        }
        It "'$ProviderTestDrive' should be present" {
            (Get-PsDrive $ProviderTestDriveName).Name | Should be $ProviderTestDriveName
        }
        It "Setting location to $ProviderTestDrive is possible" {
            { Set-Location ${ProviderTestDrive} } | Should not throw
            (Get-Location).Drive.Name | Should be ${ProviderTestDriveName}
        }
        It "Getting ChildItems from ${ProviderTestDrive} does not throw" {
            { Set-Location ${ProviderTestDrive} } | Should not throw
            { Get-ChildItem } | Should Not Throw
        }

    }

}

Describe "Function provider tests" -tags 'InnerLoop', 'DRT' {
    Context "bug #489815 (REGRESSION: dir -include x,y needs -recurse in CTP3)" {
        BeforeAll {
            Function Bug489815TestFunctionA01 {}
            Function Bug489815TestFunctionA02 {}
            Function Bug489815TestFunctionzzz {}
            $results = Get-ChildItem -Include *489815TestFunction*,*zzz function:*
        }
        It "Should have the right count" {
            $results.length | should be 3
        }
    }
}

Describe "Registry Provider Tests" -tags 'InnerLoop', 'Registry', 'DRT' {
    Context "bug #659712 PowerShell: dir -exclude working in Registry" {
        BeforeAll {
            $testDirectory = "659712"
            $items = "abc123456xyz", "abc123456789", "xyz123456abc", "lmn123456xyz"

            $Registry = "HKCU:\Software"
            push-location $Registry
        
            if (Test-Path $testDirectory) 
            { 
                Remove-Item $testDirectory -Force -Recurse
            }
            New-Item -Path $testDirectory -ItemType directory | Out-Null
            push-Location $testDirectory
            New-Item -Path $items -ItemType string -Value "blah" | Out-Null
        }
        AfterAll {
            pop-location # back from $testDirectory
            if ( (get-location).Path -eq $Registry )
            {
                Remove-Item -force -Recurse $testDirectory
            }
            pop-location # back from $Registry
        }
        It "Get-ChildItem . -Include *6x* should return no files" {
            Get-ChildItem . -Include *6x* | Should BeNullOrEmpty
        }
        It "Get-ChildItem -Include *6x* should return no files" {
            Get-ChildItem -Include *6x* | Should BeNullOrEmpty
        }
        #
        # deviation from original test which looks like it fails now
        #
        It "Get-ChildItem -Exclude *6x* should return 2 files and have correct names" {
            $result = Get-ChildItem -Exclude *6x*
            $result.Count | should be 2
            $expected = $items[1,2] -join ","
            ($result.PSChildName -join ",") | Should Be $expected
        }
        It "Get-ChildItem . -Exclude *6x* should return 2 files and have correct names" {
            $result = Get-ChildItem -Exclude *6x*
            $result.Count | should be 2
            $expected = $items[1,2] -join ","
            ($result.PSChildName -join ",") | Should Be $expected
        }
    }

    Context "RegistryRegression133180" {
        $registryPath = "HKLM:\system\currentcontrolset\control\print\monitors"
        $skip = $false
        if ( ! (test-path $registryPath ) )
        {
            $skip = $true
        }

        It "Should be able to handle provider paths containing a slash character" {
            push-location $registryPath
            { get-ChildItem } | Should Not Throw
            pop-location
        }
    }
    Context "RegistryRegression177299" {
        BeforeAll {
            $registryPath = "HKCU:\"
            $skip = $false
            if ( ! (test-path $registryPath ) )
            {
                $skip = $true
            }
        }

        BeforeEach {
            push-location $registryPath
        }
        It "Should be able to remove registry items in a transaction" -skip:$skip {
            {
                $testItem = "Regression177299"
                New-Item $testItem
                Start-Transaction
                Remove-Item $testItem -UseTransaction
                Get-Item $testItem | out-null
                Complete-Transaction
            } | Should Not Throw
        }
        AfterEach {
            pop-location
        }
    }
    Context "RegistryRegression227542" {
        BeforeAll {
            $fileName = join-path (get-item TESTDRIVE:).FullName 'TempHive.reg'
            $testPath = '\SOFTWARE\Microsoft\PowerShell\1'; # Under HKLM
            $skip = $false
            if (-not (Test-Path "HKLM:$testPath"))
            {
                $skip = $true
            }
        }
        BeforeEach {
            $exportSuccess = Reg.exe SAVE HKLM${testPath} $fileName
        }
        AfterEach {
            Remove-Item -Path $fileName
        }

        It "The registry provider does not leak handles" -skip:${skip} {
            $newPath = "\TempHive";
            $loadSuccess = Reg.exe LOAD "HKLM$newPath" $fileName
            push-Location HKLM:$newPath
            Pop-Location
            $unloadSuccess = Reg.exe UNLOAD HKLM$newPath
        }
    }
    Context "RegistryRegression390395A" {

        $childPath = "HKLM:\SYSTEM\CurrentControlSet\Control";
        $skip = $false
        if (-not (Test-Path $childPath))
        {
            $skip = $true
        }
        BeforeEach {
            push-Location $childPath

            $childName = Split-Path $childPath -Leaf -Resolve
            $parentPath = Split-Path $childPath -Parent -Resolve
            $testPath = '..\' + $childName;
            $actualPath = Set-Location $testPath -PassThru;
        }
        AfterEach {
            Pop-Location
        }
        It "Registry provider can result path like '../../something/..'" {
            $actualPath.Path | Should Be $childPath
        }
    }
    Context "RegistryRegression421614" {
        $registryPath = "HKLM:\software\Microsoft\Windows NT\CurrentVersion"
        $skip = $false
        if ( ! (test-path $registryPath ) )
        {
            $skip = $true
        }

        # if the path doesn't exist, you better skip it
        It "LiteralPath and Path with same path should be the same in Registry" -skip:${skip} {
            $expected = Get-ItemProperty -Path $registryPath
            $actual = Get-ItemProperty -LiteralPath $registryPath
            $actual.PSPath | Should Be $expected.PSPath
        }
    }
    Context "RegistryRegression436298" {
        BeforeEach {
            $driveName = "RR436298"
            $drive = New-PsDrive -Root HKEY_CLASSES_ROOT -Name $driveName -PSProvider Registry
        }
        AfterEach {
            Remove-PsDrive $drive
        }
        It "Get-ChildItem performance does not slow down to much with an alias for HKEY_CLASSES_ROOT" {
            $originalTimeSpan = Measure-Command { Get-ChildItem Registry::HKEY_CLASSES_ROOT }
            $testedTimespan = Measure-Command { Get-Childitem "${driveName}:" }
            $ratio = $testedTimeSpan.Ticks/$originalTimeSpan.Ticks
            $ratio -lt 10 | Should Be $True
        }
    }
}
