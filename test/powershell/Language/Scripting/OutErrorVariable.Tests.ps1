function Validate-Result {
    param($out, $outStr, $count, $v)

    It "$outStr.count" { $out.count | Should Be $count }
    for($i=0; $i -lt $count; $i++)
    {
        It "$outStr[$i]" { $out[$i] | Should Be $v[$i] } 
    }                
}

Describe "Tests OutVariable only" -Tags "CI" {
    BeforeAll {

        function get-foo1
        {
            [CmdletBinding()]
            param()

            "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.writeobject("foo")
        }

        function get-bar 
        {
            [CmdletBinding()]
            param()

            "bar"
            get-foo1 -outVariable global:a
        }
    }

    Context 'Updating OutVariable Case 1: pipe string' {        

        get-foo1 -outVariable a > $null

        $a.count | Should Be 1
        $a[0] | Should Be "foo"
    }


    Context 'Updating OutVariable Case 2: $pscmdlet.writeobject' {

        get-foo2 -outVariable a > $null

        $a.count | Should Be 1
        $a[0] | Should Be "foo"
    }


    Context 'Appending OutVariable Case 1: pipe string' {        

        $a = 'a','b'
        get-foo1 -outVariable +a > $null

        Validate-Result $a '$a' 3 @("a", "b", "foo")
    }


    Context 'Appending OutVariable Case 2: $pscmdlet.writeobject' {        

        $a = 'a','b'
        get-foo2 -outVariable +a > $null

        Validate-Result $a '$a' 3 @("a", "b", "foo")
    }    

    Context 'Nested OutVariable' {

        get-bar -outVariable b > $null

        Validate-Result $global:a '$global:foo' 1 @("foo")
        
        Validate-Result $b '$b' 2 @("bar", "foo")
    }
}

Describe "Test ErrorVariable only" -Tags "CI" {
    BeforeAll {    
        function get-foo1
        {
            [CmdletBinding()]
            param()

            write-error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.WriteError($global:foo[0])
        }

        function get-bar 
        {
            [CmdletBinding()]
            param()

            write-error "bar"
            get-foo1 -errorVariable global:a
        }
    }
    Context 'Updating ErrorVariable Case 1: write-error' {
        get-foo1 -errorVariable a 2> $null

        Validate-Result $a '$a' 1 @("foo")
    }

    Context 'Updating ErrorVariable Case 2: $pscmdlet.WriteError' {

        write-error "foo" -errorVariable global:foo 2> $null
        get-foo2 -errorVariable a 2> $null

        Validate-Result $global:foo '$global:foo' 1 @("foo")
    }

    Context 'Appending ErrorVariable Case 1' {

        $a = 'a','b'
        get-foo1 -errorVariable +a 2> $null

        Validate-Result $a '$a' 3 @("a", "b", "foo")
    }

    Context 'Appending ErrorVariable Case 1' {

        write-error "foo" -errorVariable global:foo 2> $null
        $a = 'a','b'
        get-foo2 -errorVariable +a 2> $null
                
        Validate-Result $a '$a' 3 @("a", "b", "foo")
    }

    Context 'Nested ErrorVariable' {

        get-bar -errorVariable b 2> $null

        It '$global:a.count' { $global:a.count | Should Be 1 }
        It '$global:a[0]' {$global:a[0].ToString() | Should Be "foo" }

        It '$b.count' { $b.count | Should Be 2 }
        It '$b[0]' { $b[0].ToString() -eq "bar" }
        It '$b[1]' { $b[1].ToString() -eq "foo" }
    }

    Context 'Nested ErrorVariable with redirection' {

        get-bar -errorVariable b 2>&1 > $null
        
        Validate-Result $global:a '$global:a' 1 @("foo")
        Validate-Result $b '$b' 2 @("bar", "foo")
    }

}

Describe "Update both OutVariable and ErrorVariable" -Tags "CI" {
    BeforeAll {       

        function get-foo
        {
          [CmdletBinding()]
          param()
  
          write-output "foo-output"
          write-error  "foo-error"
        }

        function get-foo1
        {
            [CmdletBinding()]
            param()

            write-error "foo"
        }

        function get-foo2
        {
            [CmdletBinding()]
            param()

            $pscmdlet.WriteError($global:foo[0])
        }

        function get-bar 
        {
            [CmdletBinding()]
            param()

            write-error "bar"
            get-foo1 -errorVariable global:a
        }

        function get-foo3
        {
            [CmdletBinding()]
            param()

            "foo-output-0"
            write-output "foo-output-1"
            write-error "foo-error"
        }

        function get-bar2
        {
            [CmdletBinding()]
            param()

            "bar-output-0"
            write-output "bar-output-1"
            write-error "bar-error"
            get-foo3 -OutVariable global:foo_out -errorVariable global:foo_err 
        }
    }
    
    Context 'Update OutVariable and ErrorVariable' {        

        get-foo3 -OutVariable out -errorVariable err 2> $null > $null        

        Validate-Result $out '$out' 2 @("foo-output-0", "foo-output-1")
        Validate-Result $err '$err' 1 @("foo-error")
    }

    Context 'Update OutVariable and ErrorVariable' {        

        get-bar2 -OutVariable global:bar_out -errorVariable global:bar_err  2> $null > $null

        Validate-Result $foo_out '$foo_out' 2 @("foo-output-0", "foo-output-1")
        Validate-Result $foo_err '$foo_err' 1 @("foo-error")

        Validate-Result $bar_out '$bar_out' 4 @("bar-output-0", "bar-output-1", "foo-output-0", "foo-output-1")
        Validate-Result $bar_err '$bar_err' 2 @("bar-error", "foo-error")
    }

    Context 'Verify that exceptions are added to the ErrorVariable' {

        function get-foo4
        {
            [CmdletBinding()]
            param()

            write-error "foo-error"
    
            try
            {
                throw "foo-exception"
            }
            catch
            {}
        }

        get-foo4 -errorVariable err 2> $null 

        Validate-Result $err '$err' 2 @("foo-error", "foo-exception")        
    }

    Context 'Error variable in multi-command pipeline' {

        

        function get-foo5
        {
          [CmdletBinding()]
          param([Parameter(ValueFromPipeline = $true)][string] $foo)
  
          process
          {
            write-output $foo
            write-error  $foo
          }
        }

        (get-foo5 "foo-message" -ev foo_err1 -ov foo_out1 | get-foo5 -ev foo_err2 -ov foo_out2 | get-foo5 -ev foo_err3 -ov foo_out3) 2>&1 > $null
        
        Validate-Result $foo_out1 '$foo_out1' 1 @("foo-message")
        Validate-Result $foo_out2 '$foo_out2' 1 @("foo-message")
        Validate-Result $foo_out3 '$foo_out3' 1 @("foo-message")
        Validate-Result $foo_err1 '$foo_err1' 1 @("foo-message")
        Validate-Result $foo_err2 '$foo_err2' 1 @("foo-message")
        Validate-Result $foo_err3 '$foo_err3' 1 @("foo-message")
    }

    Context 'Error variable in multi-command pipeline (with native cmdlet)' {

        (get-foo -ev foo_err | get-item -ev get_item_err ) 2>&1 > $null

        Validate-Result $foo_err '$foo_err' 1 @("foo-error")

        It '$get_item_err.count' { $get_item_err.count | Should Be 1 }
        It '$get_item_err[0].exception' { $get_item_err[0].exception.GetType() | Should Be 'System.Management.Automation.ItemNotFoundException' }
    }

    Context 'Multi-command pipeline with nested commands' {       

        function get-bar3 
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  "bar-error"
            write-output "bar-output"
            get-foo
        }

        (get-foo -errorVariable foo_err | get-bar3 -errorVariable bar_err) 2>&1 > $null        

        Validate-Result $foo_err '$foo_err' 1 @("foo-error")

        Validate-Result $bar_err '$bar_err' 2 @("bar-error", "foo-error")
}

    Context 'multi-command pipeline with nested commands' { 

        function get-foo6
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  "foo-error"
            write-output $i
        }

        function get-bar4
        {
            [CmdletBinding()]
            param([Parameter(ValueFromPipeline = $true)][string] $i)

            write-error  "bar-error"
            get-foo6 "foo-output" -errorVariable global:foo_err1 | get-foo6 -errorVariable global:foo_err2 
        }

        get-bar4 -errorVariable global:bar_err 2>&1 > $null

        Validate-Result $global:foo_err1 '$global:foo_err1' 1 @("foo-error")
        Validate-Result $global:foo_err2 '$global:foo_err2' 1 @("foo-error")

        Validate-Result $global:bar_err '$global:bar_err' 2 @("bar-error", "foo-error")        
    }

    Context 'Nested output variables' {
        BeforeAll {
            

            function get-foo7
            {
                [CmdletBinding()]
                param([Parameter(ValueFromPipeline = $true)][string] $output)

                $output
                write-error "foo-error"
            }

            function get-bar5
            {
                [CmdletBinding()]
                param()

                "bar-output"
                write-error  "bar-error"
                get-foo7 "foo-output" -ev global:foo_err1 -ov global:foo_out1 | get-foo7 -ev global:foo_err2 -ov global:foo_out2
                get-foo7 "foo-output" -ev global:foo_err3 -ov global:foo_out3 | get-foo7 -ev global:foo_err4 -ov global:foo_out4
            }
        }

        get-bar5 -ev global:bar_err -ov global:bar_out 2>&1 > $null

        Validate-Result $global:foo_out1 '$global:foo_out1' 1 @("foo-output")
        Validate-Result $global:foo_err1 '$global:foo_err1' 1 @("foo-error")

        Validate-Result $global:foo_out2 '$global:foo_out2' 1 @("foo-output")
        Validate-Result $global:foo_err2 '$global:foo_err2' 1 @("foo-error")

        Validate-Result $global:foo_out3 '$global:foo_out3' 1 @("foo-output")
        Validate-Result $global:foo_err3 '$global:foo_err3' 1 @("foo-error")

        Validate-Result $global:foo_out4 '$global:foo_out4' 1 @("foo-output")
        Validate-Result $global:foo_err4 '$global:foo_err4' 1 @("foo-error")
                
        Validate-Result $global:bar_out '$global:bar_out' 3 @("bar-output", "foo-output", "foo-output")
        Validate-Result $global:bar_err '$global:bar_err' 3 @("bar-error", "foo-error", "foo-error")        
    }
}