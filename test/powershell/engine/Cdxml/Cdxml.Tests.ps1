$script:CimClassName = "PSCore_CimTest1"
$script:CimNamespace = "root/default"
$script:moduleDir = join-path $PSScriptRoot assets CimTest
$script:deleteMof = join-path $moduleDir DeleteCimTest.mof
$script:createMof = join-path $moduleDir CreateCimTest.mof

$CimCmdletArgs = @{
    Namespace = ${script:CimNamespace}
    ClassName = ${script:CimClassName}
    ErrorAction = "SilentlyContinue"
    }

$script:ItSkipOrPending = @{}

function Test-CimTestClass {
    (Get-CimClass @CimCmdletArgs) -ne $null
}

function Test-CimTestInstance {
    (Get-CimInstance @CimCmdletArgs) -ne $null
}

Describe "Cdxml cmdlets are supported" -Tag CI,RequireAdminOnWindows {
    BeforeAll {
        $skipNotWindows = ! $IsWindows
        if ( $skipNotWindows ) {
            $script:ItSkipOrPending = @{ Skip = $true }
            return
        }

        # start from a clean slate, remove the instances and the 
        # class if they exist
        if ( Test-CimTestClass ) {
            if ( Test-CimTestInstance ) {
                Get-CimInstance @CimCmdletArgs | Remove-CimInstance
            }
            $result = MofComp.exe $deleteMof
            if ( $LASTEXITCODE -ne 0 ) {
                $script:ItSkipOrPending = @{ Pending = $true }
                return
            }
        }

        # create the class and instances
        $result = MofComp.exe ${script:createMof}

        if ( $LASTEXITCODE -ne 0 ) {
            $script:ItSkipOrPending = @{ Pending = $true }
            return
        }
        # now load the cdxml module
        if ( Get-Module CimTest ) {
            Remove-Module -force CimTest
        }
        Import-Module -force ${script:ModuleDir}
    }
    AfterAll {
        if ( $skipNotWindows ) {
            return
        }
        if ( get-module CimTest ) {
            Remove-Module CimTest -Force
        }

        $result = MofComp.exe $deleteMof
        if ( $LASTEXITCODE -ne 0 ) {
            Write-Warning "Could not remove PSCore_CimTest class"
        }
    }

    It "The CimTest module should have been loaded" @ItSkipOrPending {
        $result = Get-Module CimTest
        $result.ModuleBase | should be ${script:ModuleDir}
    }

    It "The CimTest module should have the proper cmdlets" @ItSkipOrPending {
        $result = Get-Command -Module CimTest
        $result.Count | Should Be 4
        ($result.Name | sort-object ) -join "," | Should Be "Get-CimTest,New-CimTest,Remove-CimTest,Set-CimTest"
    }

    Context "Get-CimTest cmdlet" {
        It "The Get-CimTest cmdlet should return 4 objects" @ItSkipOrPending {
            $result = Get-CimTest
            $result.Count | should be 4
            ($result.id |sort-object) -join ","  | should be "1,2,3,4"
        }
        It "The Get-CimTest cmdlet should retrieve an object via id" @ItSkipOrPending {
            $result = Get-CimTest -id 1
            @($result).Count | should be 1
            $result.field1 | Should be "instance 1"
        }
        It "The Get-CimTest cmdlet should retrieve an object by piped id" @ItSkipOrPending {
            $result = 1,2,4 | foreach-object { [pscustomobject]@{ id = $_ } } | Get-CimTest
            @($result).Count | should be 3
            ( $result.id | sort-object ) -join "," | Should be "1,2,4"
        }
        It "The Get-CimTest cmdlet should work as a job" @ItSkipOrPending {
            try {
                $job = Get-CimTest -AsJob
                $result = $null
                $i = 0
                # wait up to 10 seconds, then the test will fail
                # we need to wait long enough, but not too long
                # the time can be adjusted
                do {
                    if ( $job.State -eq "Completed" ) 
                    { 
                        $result = $job | Receive-Job
                        break 
                    }
                    start-sleep 1
                } while ( $i++ -lt 10 )
                $result.Count | should be 4
                ( $result.id | sort-object ) -join "," | Should be "1,2,3,4"
            }
            finally {
                if ( $job ) {
                    $job | Remove-Job -force
                }
            }
        }
    }

    Context "Remove-CimTest cmdlet" {
        BeforeEach {
            Get-CimTest | Remove-CimTest
            1..4 | %{ New-CimInstance -namespace root/default -class PSCore_Test1 -property @{  
                id = "$_"
                field1 = "field $_"
                field2 = 10 * $_ 
                }
            }
        }
        It "The Remote-CimTest cmdlet should remove objects by id" @ItSkipOrPending {
            Remove-CimTest -id 1
            $result = Get-CimTest
            $result.Count | should be 3
            ($result.id |sort-object) -join ","  | should be "2,3,4"
        }
        It "The Remove-CimTest cmdlet should remove piped objects" @ItSkipOrPending {
            Get-CimTest -id 2 | Remove-CimTest
            $result  = Get-CimTest 
            @($result).Count | should be 3
            ($result.id |sort-object) -join ","  | should be "1,3,4"
        }
        It "The Remove-CimTest cmdlet should work as a job" @ItSkipOrPending {
            try {
                $job = Get-CimTest -id 3 | Remove-CimTest -asjob
                $result = $null
                $i = 0
                # wait up to 10 seconds, then the test will fail
                # we need to wait long enough, but not too long
                # the time can be adjusted
                do {
                    if ( $job.State -eq "Completed" ) 
                    { 
                        break 
                    }
                    start-sleep 1
                } while ( $i++ -lt 10 )
                $result  = Get-CimTest 
                @($result).Count | should be 3
                ($result.id |sort-object) -join ","  | should be "1,2,4"
            }
            finally {
                if ( $job ) {
                    $job | Remove-Job -force
                }
            }
        }
    }

    Context "New-CimTest operations" {
        It "Should create a new instance" @ItSkipOrPending {
            $instanceArgs = @{
                id = "telephone"
                field1 = "television"
                field2 = 0
            }
            New-CimTest @instanceArgs
            $result = Get-CimInstance -namespace root/default -class PSCore_Test1 | ?{$_.id -eq "telephone"}
            $result.field2 | should be 0
            $result.field1 | Should be $instanceArgs.field1
        }
    }

    Context "Set-CimTest operations" {

        It "Should set properties on an instance" @ItSkipOrPending {
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
            $result.field2 | should be $instanceArgs.field2
            $result.field1 | Should be $instanceArgs.field1
            Set-CimTest @newValues
            $result = Get-CimTest -id $newValues.id
            $result.field1 | Should be $newValues.field1
            $result.field2 | should be $newValues.field2
        }

        It "Should set properties on an instance via pipeline" @ItSkipOrPending {
            $instanceArgs = @{
                id = "updateTest2"
                field1 = "updatevalue"
                field2 = 100
            }
            New-CimTest @instanceArgs
            $result = Get-CimTest -id $instanceArgs.id
            $result.field2 | should be $instanceArgs.field2
            $result.field1 | Should be $instanceArgs.field1
            $result.field1 = "yet another value"
            $result.field2 = 33
            $result | Set-CimTest
            $result = Get-CimTest -id $instanceArgs.id
            $result.field1 | Should be "yet another value"
            $result.field2 | should be 33
        }

    }

}
