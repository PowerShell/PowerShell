Describe 'ConvertFrom-Json' -tags "CI" {
    It 'can convert a single-line object' {
        ('{"a" : "1"}' | ConvertFrom-Json).a | Should Be 1
    }

    It 'can convert one string-per-object' {
        $json = @('{"a" : "1"}', '{"a" : "x"}') | ConvertFrom-Json
        $json.Count | Should Be 2
        $json[1].a | Should Be 'x'
    }

    It 'can convert multi-line object' {
        $json = @('{"a" :', '"x"}') | ConvertFrom-Json
        $json.a | Should Be 'x'
    }

    It 'can silently continue single-line error' {
        $Error.Clear()

        $json = @('{"a" :}') | ConvertFrom-Json -ErrorAction SilentlyContinue 

        $json | Should BeNullOrEmpty       

        # The error should have been written to the error stream.
        $Error.Count | Should Be 1
        $err = $Error[0]

        # Verify the error record
        $err.CategoryInfo.Category | Should Be 'InvalidData'
        $err.TargetObject | Should Be '{"a" :}'
        $err.FullyQualifiedErrorId | Should Be 'ConvertFromJson,Microsoft.PowerShell.Commands.ConvertFromJsonCommand'
    }

    It 'can silently report single-line error' {
        $err = $null
        $json = @('{"a" :}') | ConvertFrom-Json -ErrorAction SilentlyContinue -ErrorVariable err 

        $json | Should BeNullOrEmpty

        # Verify the error record
        $err | Should Not BeNullOrEmpty
        $err.CategoryInfo.Category | Should Be 'InvalidData'
        $err.TargetObject | Should Be '{"a" :}'
        $err.FullyQualifiedErrorId | Should Be 'ConvertFromJson,Microsoft.PowerShell.Commands.ConvertFromJsonCommand'
    }

    It 'can silently continue multi-line error' {
        $Error.Clear()

        $json = @('{"a" : "1"}', '{"a" :}', '{"a" : "x"}') | ConvertFrom-Json -ErrorAction SilentlyContinue        

        # verify expected objects
        $json.Count | Should Be 2
        # The second array element should have  been skipped, makeing a:x the second returned object.
        $json[1].a | Should Be 'x'

        # The error should have been written to the error stream.
        $Error.Count | Should Be 1
        $err = $Error[0]

        # Verify the error record
        $err.CategoryInfo.Category | Should Be 'InvalidData'
        $err.TargetObject | Should Be '{"a" :}'
        $err.FullyQualifiedErrorId | Should Be 'ConvertFromJson,Microsoft.PowerShell.Commands.ConvertFromJsonCommand'
    }

    It 'can silently report multi-line error' {
        $err = $null

        $json = @('{"a" : "1"}', '{"a" :}', '{"a" : "x"}') | ConvertFrom-Json -ErrorAction SilentlyContinue -ErrorVariable err       

        # verify expected objects
        $json.Count | Should Be 2
        # The second array element should have  been skipped, makeing a:x the second returned object.
        $json[1].a | Should Be 'x'

        # Verify the error record
        $err | Should Not BeNullOrEmpty
        $err.CategoryInfo.Category | Should Be 'InvalidData'
        $err.TargetObject | Should Be '{"a" :}'
        $err.FullyQualifiedErrorId | Should Be 'ConvertFromJson,Microsoft.PowerShell.Commands.ConvertFromJsonCommand'
    }

    It 'can silently ignore single-line error' {
        $err = $null
        $Error.Clear()

        $json = @('{"a" :}') | ConvertFrom-Json -ErrorAction Ignore -ErrorVariable err       

        $json | Should BeNullOrEmpty

        # Expecting Ignore to suppress both ErrorVariable and $Error
        $err | Should BeNullOrEmpty
        $Error.Count | Should Be 0
    }

    It 'can silently ignore multi-line error' {
        $err = $null
        $Error.Clear()

        $json = @('{"a" : "1"}', '{"a" :}', '{"a" : "x"}') | ConvertFrom-Json -ErrorAction Ignore -ErrorVariable err        

        # verify expected objects
        $json.Count | Should Be 2
        # The second array element should have  been skipped, makeing a:x the second returned object.
        $json[1].a | Should Be 'x'

        # Expecting Ignore to suppress both ErrorVariable and $Error
        $err | Should BeNullOrEmpty
        $Error.Count | Should Be 0
    }
}
