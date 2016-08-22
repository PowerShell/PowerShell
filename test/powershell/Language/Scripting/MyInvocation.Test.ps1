Describe 'Testing of MyInvocation' -Tags "CI" {


    It 'Script file' {

        $MyInvocation.Line.ToLower().IndexOf('$null = & $scriptblock @parameters') | Should BeGreaterThan -1
    }

    It 'Function' {

        function myfunc
        {
            $MyInvocation.Line.IndexOf("myfunc") | Should BeGreaterThan -1
        }

        . myfunc
        & myfunc
    }

    It 'Filter' {

        filter myfilter
        {
            $MyInvocation.Line.IndexOf("myfilter") | Should BeGreaterThan -1
        }

        . myfilter
        & myfilter
    }

    Context 'Script block' {

        $a = . {$MyInvocation.Line}

        It 'Script block1' { $a.IndexOf('$a = . {$MyInvocation.Line}') | Should BeGreaterThan -1 }

        $a = & {$MyInvocation.Line}

        It 'Script block2' { $a.IndexOf('$a = & {$MyInvocation.Line}') | Should BeGreaterThan -1 }

        $a = & {$MyInvocation.ScriptName}

        It 'Script block3' { $a.ToLower().IndexOf("myinvocation.test.ps1") | Should BeGreaterThan -1 }

        $a = . {$MyInvocation.ScriptName}

        It 'Script block4' { $a.ToLower().IndexOf("myinvocation.test.ps1") | Should BeGreaterThan -1 }
    }
}
