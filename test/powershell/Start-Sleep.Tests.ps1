Describe "Start-Sleep" {

    Context "Validate Start-Sleep works properly" {
        It "Should only sleep for at least 1 second" {
            $result = Measure-Command { Start-Sleep -s 1 }
            $result.TotalSeconds | Should BeGreaterThan 0.25
        }

        It "Should sleep for at least 1 second using the alias" {
            $result = Measure-Command { sleep -s 1 }
            $result.TotalSeconds | Should BeGreaterThan 0.25
        }
    }
}
