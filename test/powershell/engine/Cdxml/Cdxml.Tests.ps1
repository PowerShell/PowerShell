# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Cdxml cmdlets are supported" -Tag CI,RequireAdminOnWindows {
    BeforeDiscovery {
        $skipCdxml = ! $IsWindows -or $null -eq (Get-Command -ErrorAction SilentlyContinue Mofcomp.exe)
    }

    BeforeAll {
        $script:CimClassName = "PSCore_CimTest1"
        $script:CimNamespace = "root/default"
        $script:moduleDir = Join-Path -Path $PSScriptRoot -ChildPath assets -AdditionalChildPath CimTest
        $script:deleteMof = Join-Path -Path $moduleDir -ChildPath DeleteCimTest.mof
        $script:createMof = Join-Path -Path $moduleDir -ChildPath CreateCimTest.mof

        $CimCmdletArgs = @{
            Namespace = ${script:CimNamespace}
            ClassName = ${script:CimClassName}
            ErrorAction = "SilentlyContinue"
        }

        function Test-CimTestClass {
            $null -eq (Get-CimClass @CimCmdletArgs)
        }

        function Test-CimTestInstance {
            $null -eq (Get-CimInstance @CimCmdletArgs)
        }

        $skipNotWindows = ! $IsWindows
        if ( $skipNotWindows ) {
            return
        }

        # if MofComp does not exist, we shouldn't bother moving forward
        if ( (Get-Command -ErrorAction SilentlyContinue Mofcomp.exe) -eq $null ) {
            return
        }

        # start from a clean slate, remove the instances and the
        # classes if they exist
        if ( Test-CimTestClass ) {
            if ( Test-CimTestInstance ) {
                Get-CimInstance @CimCmdletArgs | Remove-CimInstance
            }
            # if there's a failure with mofcomp then we will have trouble
            # executing the tests. Keep track of the exit code
            $result = MofComp.exe $deleteMof
            $script:MofCompReturnCode = $LASTEXITCODE
            if ( $script:MofCompReturnCode -ne 0 ) {
                return
            }
        }

        # create the class and instances
        # and track the exitcode for the compilation of the mof file
        # if there's a problem, there's no reason to keep going
        $testMof = Get-Content -Path ${script:createmof} -Raw
        $currentTimeZone = [System.TimeZoneInfo]::Local

        # this date is simply the same one used in the test mof
        # the minutes must be padded to 3 digits proceeded by a '+' or '-'
        # when east of UTC 0 we must add a '+'
        $offsetMinutes = ($currentTimeZone.GetUtcOffset([datetime]::new(2008, 01, 01, 0, 0, 0))).TotalMinutes
        $UTCOffset = "{0:+000;-000}" -f $offsetMinutes
        $testMof = $testMof.Replace("<UTCOffSet>", $UTCOffset)
        Set-Content -Path $testDrive\testmof.mof -Value $testMof
        $result = MofComp.exe $testDrive\testmof.mof
        $script:MofCompReturnCode = $LASTEXITCODE
        if ( $script:MofCompReturnCode -ne 0 ) {
            return
        }

        # now load the cdxml module
        if ( Get-Module CimTest ) {
            Remove-Module -Force CimTest
        }
        Import-Module -Force ${script:ModuleDir}
    }

    AfterAll {
        if ( $skipNotWindows ) {
            return
        }
        if ( Get-Module CimTest ) {
            Remove-Module CimTest -Force
        }
        $null = MofComp.exe $deleteMof
        if ( $LASTEXITCODE -ne 0 ) {
            Write-Warning "Could not remove PSCore_CimTest class"
        }
    }

    BeforeEach {
        If ( $script:MofCompReturnCode -ne 0 ) {
            Set-ItResult -Skipped -Because "MofComp.exe failed with exit code $($script:MofCompReturnCode)"
        }
    }

    Context "Module level tests" {
        It "The CimTest module should have been loaded" -Skip:$skipCdxml {
            $result = Get-Module CimTest
            $result.ModuleBase | Should -Be ${script:ModuleDir}
        }

        It "The CimTest module should have the proper cmdlets" -Skip:$skipCdxml {
            $result = Get-Command -Module CimTest
            $result.Count | Should -Be 4
            ($result.Name | Sort-Object ) -join "," | Should -Be "Get-CimTest,New-CimTest,Remove-CimTest,Set-CimTest"
        }
    }

    Context "Get-CimTest cmdlet" {
        It "The Get-CimTest cmdlet should return 4 objects" -Skip:$skipCdxml {
            $result = Get-CimTest
            $result.Count | Should -Be 4
            ($result.id |Sort-Object) -join ","  | Should -Be "1,2,3,4"
        }

        It "The Get-CimTest cmdlet should retrieve an object via id" -Skip:$skipCdxml {
            $result = Get-CimTest -id 1
            @($result).Count | Should -Be 1
            $result.field1 | Should -Be "instance 1"
        }

        It "The Get-CimTest cmdlet should retrieve an object by piped id" -Skip:$skipCdxml {
            $result = 1,2,4 | ForEach-Object { [pscustomobject]@{ id = $_ } } | Get-CimTest
            @($result).Count | Should -Be 3
            ( $result.id | Sort-Object ) -join "," | Should -Be "1,2,4"
        }

        It "The Get-CimTest cmdlet should retrieve an object by datetime" -Skip:$skipCdxml {
            $result = Get-CimTest -DateTime ([datetime]::new(2008,01,01,0,0,0))
            @($result).Count | Should -Be 1
            $result.field1 | Should -Be "instance 1"
        }

        It "The Get-CimTest cmdlet should return the proper error if the instance does not exist" -Skip:$skipCdxml {
            { Get-CimTest -ErrorAction Stop -id "ThisIdDoesNotExist" } | Should -Throw -ErrorId "CmdletizationQuery_NotFound_Id,Get-CimTest"
        }

        It "The Get-CimTest cmdlet should work as a job" -Skip:$skipCdxml {
            try {
                $job = Get-CimTest -AsJob
                $result = $null
                # wait up to 10 seconds, then the test will fail
                # we need to wait long enough, but not too long
                # the time can be adjusted
                $null = Wait-Job -Job $job -Timeout 10
                $result = $job | Receive-Job
                $result.Count | Should -Be 4
                ( $result.id | Sort-Object ) -join "," | Should -Be "1,2,3,4"
            }
            finally {
                if ( $job ) {
                    $job | Remove-Job -Force
                }
            }
        }

        It "Should be possible to invoke a method on an object returned by Get-CimTest" -Skip:$skipCdxml {
            $result = Get-CimTest | Select-Object -First 1
            $result.GetCimSessionInstanceId() | Should -BeOfType guid
        }
    }

    Context "Remove-CimTest cmdlet" {
        BeforeEach {
            Get-CimTest | Remove-CimTest
            1..4 | ForEach-Object { New-CimInstance -Namespace root/default -class PSCore_Test1 -Property @{
                id = "$_"
                field1 = "field $_"
                field2 = 10 * $_
                }
            }
        }

        It "The Remote-CimTest cmdlet should remove objects by id" -Skip:$skipCdxml {
            Remove-CimTest -id 1
            $result = Get-CimTest
            $result.Count | Should -Be 3
            ($result.id |Sort-Object) -join ","  | Should -Be "2,3,4"
        }

        It "The Remove-CimTest cmdlet should remove piped objects" -Skip:$skipCdxml {
            Get-CimTest -id 2 | Remove-CimTest
            $result  = Get-CimTest
            @($result).Count | Should -Be 3
            ($result.id |Sort-Object) -join ","  | Should -Be "1,3,4"
        }

        It "The Remove-CimTest cmdlet should work as a job" -Skip:$skipCdxml {
            try {
                $job = Get-CimTest -id 3 | Remove-CimTest -asjob
                $result = $null
                # wait up to 10 seconds, then the test will fail
                # we need to wait long enough, but not too long
                # the time can be adjusted
                $null = Wait-Job -Job $job -Timeout 10
                $result  = Get-CimTest
                @($result).Count | Should -Be 3
                ($result.id |Sort-Object) -join ","  | Should -Be "1,2,4"
            }
            finally {
                if ( $job ) {
                    $job | Remove-Job -Force
                }
            }
        }
    }

    Context "New-CimTest operations" {
        It "Should create a new instance" -Skip:$skipCdxml {
            $instanceArgs = @{
                id = "telephone"
                field1 = "television"
                field2 = 0
            }
            New-CimTest @instanceArgs
            $result = Get-CimInstance -Namespace root/default -class PSCore_Test1 | Where-Object {$_.id -eq "telephone"}
            $result.field2 | Should -Be 0
            $result.field1 | Should -Be $instanceArgs.field1
        }

        It "Should return the proper error if called with an improper value" -Skip:$skipCdxml {
            $instanceArgs = @{
                Id = "error validation"
                field1 = "a string"
                field2 = "a bad string" # this needs to be an int
            }
            { New-CimTest @instanceArgs } | Should -Throw -ErrorId "ParameterArgumentTransformationError,New-CimTest"
            # just make sure that it wasn't added
            Get-CimTest -id $instanceArgs.Id -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }

        It "Should support -whatif" -Skip:$skipCdxml {
            $instanceArgs = @{
                Id = "1000"
                field1 = "a string"
                field2 = 111
                Whatif = $true
            }
            New-CimTest @instanceArgs
            Get-CimTest -id $instanceArgs.Id -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }

    Context "Set-CimTest operations" {
        It "Should set properties on an instance" -Skip:$skipCdxml {
            $instanceArgs = @{
                id = "updateTest1"
                field1 = "updatevalue"
                field2 = 100
            }
            $newValues = @{
                id = "updateTest1"
                field2 = 22
                field1 = "newvalue"
            }
            New-CimTest @instanceArgs
            $result = Get-CimTest -id $instanceArgs.id
            $result.field2 | Should -Be $instanceArgs.field2
            $result.field1 | Should -Be $instanceArgs.field1
            Set-CimTest @newValues
            $result = Get-CimTest -id $newValues.id
            $result.field1 | Should -Be $newValues.field1
            $result.field2 | Should -Be $newValues.field2
        }

        It "Should set properties on an instance via pipeline" -Skip:$skipCdxml {
            $instanceArgs = @{
                id = "updateTest2"
                field1 = "updatevalue"
                field2 = 100
            }
            New-CimTest @instanceArgs
            $result = Get-CimTest -id $instanceArgs.id
            $result.field2 | Should -Be $instanceArgs.field2
            $result.field1 | Should -Be $instanceArgs.field1
            $result.field1 = "yet another value"
            $result.field2 = 33
            $result | Set-CimTest
            $result = Get-CimTest -id $instanceArgs.id
            $result.field1 | Should -Be "yet another value"
            $result.field2 | Should -Be 33
        }
    }

}

