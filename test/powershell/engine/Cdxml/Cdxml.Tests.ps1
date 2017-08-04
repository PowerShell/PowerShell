$script:CimClassName = "PSCore_CimTest"
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
        Remove-Module CimTest -Force

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
        $result.Count | Should Be 2
        ($result.Name | sort-object ) -join "," | Should Be "Get-CimTest,Remove-CimTest"
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
    }

    Context "Remove-CimTest cmdlet" {
        It "The Remote-CimTest cmdlet should remove objects by id" @ItSkipOrPending {
            Remove-CimTest -id 1
            $result = Get-CimTest
            $result.Count | should be 3
            ($result.id |sort-object) -join ","  | should be "2,3,4"
        }
        It "The Remove-CimTest cmdlet should remove piped objects" @ItSkipOrPending {
            Get-CimTest -id 2 | Remove-CimTest
            $result  = Get-CimTest 
            @($result).Count | should be 2
            ($result.id |sort-object) -join ","  | should be "3,4"
        }
    }

}
