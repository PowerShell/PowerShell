Describe 'Misc Test' -Tags "CI" {

    Context 'Where' {
        class C1 {
        [int[]] $Wheels = @(1,2,3);
        [string] Foo() {
            return (1..10).Where({ $PSItem -in $this.Wheels; }) -join ';'
        }

        [string] Bar()
        {
             return (1..10 | Where  { $PSItem -in $this.Wheels; }) -join ';'
        }
        }
        It 'Invoke Where' {
                [C1]::new().Foo() | should be "1;2;3"
        }
        It 'Pipe to where' {
                [C1]::new().Bar() | should be "1;2;3"
        }
    }

    Context 'ForEach' {
        class C1 {
        [int[]] $Wheels = @(1,2,3);
        [string] Foo() {
            $ret=""
            Foreach($PSItem in $this.Wheels) { $ret +="$PSItem;"}
            return $ret
        }

        [string] Bar()
        {
            $ret = ""
            $this.Wheels | foreach { $ret += "$_;" }
            return $ret
        }
        }
        It 'Invoke Foreach' {
                [C1]::new().Foo() | should be "1;2;3;"
        }
        It 'Pipe to Foreach' {
                [C1]::new().Bar() | should be "1;2;3;"
        }
    }
}
