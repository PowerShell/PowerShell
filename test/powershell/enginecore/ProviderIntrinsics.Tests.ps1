Describe "ProviderIntrinsics Tests" -tags "CI" {
    BeforeAll {
        setup -d TestDir
    }
    It 'If a childitem exists, HasChild method returns $true' {
        $ExecutionContext.InvokeProvider.ChildItem.HasChild("$TESTDRIVE") | Should be $true
    }
    It 'If a childitem does not exist, HasChild method returns $false' {
        $ExecutionContext.InvokeProvider.ChildItem.HasChild("$TESTDRIVE/TestDir") | Should be $false
    }
    It 'If the path does not exist, HasChild throws an exception' {
        try {
            $ExecutionContext.InvokeProvider.ChildItem.HasChild("TESTDRIVE/ThisDirectoryDoesNotExist")
            throw "Execution OK"
        }
        catch {
            $_.fullyqualifiederrorid | should be "ItemNotFoundException"
        }
    }
} 

