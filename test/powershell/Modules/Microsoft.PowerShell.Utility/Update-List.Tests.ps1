Describe "Update-List tests" -Tags CI {
    BeforeEach {
        $list = [System.Collections.Generic.List[int]]::new()
        $list.Add(1)
        $list.Add(2)
        $list.Add(3)
        $obj = [PSCustomObject]@{list = $list}
    }

    It "Can use -Add" {
        $newobj = $obj | Update-List -Property list -Add 4,5
        [string]::Join(",", $newobj.List) | Should Be "1,2,3,4,5"
    }

    It "Can use -Remove" {
        $newobj = $obj | Update-List -Property list -Remove 2
        [string]::Join(",", $newObj.List) | Should Be "1,3"
    }

    It "Can use both -Add and -Remove" {
        $newobj = $obj | Update-List -Property list -Add 6,7 -Remove 1,3
        [string]::Join(",", $newobj.List) | Should Be "2,6,7"
    }

    It "Can use -Replace" {
        $newobj = $obj | Update-List -Property list -Replace 4,5,6
        [string]::Join(",", $newobj.List) | Should Be "4,5,6"
    }
}
