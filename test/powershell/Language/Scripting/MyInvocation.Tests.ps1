# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Testing of MyInvocation' -Tags "CI" {
    It 'MyInvocation works in Function' {

        function myfunc
        {
            $MyInvocation.Line.IndexOf("myfunc") | Should -BeGreaterThan -1
        }

        { . myfunc } | Should -Not -Throw
        {& myfunc } | Should -Not -Throw
    }

    It 'MyInvocation works in Filter' {

        filter myfilter
        {
            $MyInvocation.Line.IndexOf("myfilter") | Should -BeGreaterThan -1
        }

        {. myfilter } | Should -Not -Throw
        { & myfilter } | Should -Not -Throw
    }

    Context 'MyInvocation works with multi-line invocations' {
        It 'MyInvocation.Statement works in & Script block' {
            $a = & {
                $MyInvocation.Statement
            }
            $a.IndexOf('& {
                $MyInvocation.Statement
            }') |Should -BeGreaterThan -1
        }
        It 'MyInvocation.Statement works in dot sourced Script block' {
            $a = . {
                $MyInvocation.Statement
            }
            $a.IndexOf('. {
                $MyInvocation.Statement
            }') |Should -BeGreaterThan -1
        }
    }

    Context 'MyInvocation works in Script block' {

        It 'MyInvocation works in dot sourced Script block' {
            $a = . {$MyInvocation.Line}
            $a.IndexOf('$a = . {$MyInvocation.Line}') | Should -BeGreaterThan -1
        }
        It 'MyInvocation works in & Script block2' {
            $a = & {$MyInvocation.Line}
            $a.IndexOf('$a = & {$MyInvocation.Line}') | Should -BeGreaterThan -1
        }

        It 'MyInvocation works when run Script file' {
            $a = & {$MyInvocation.ScriptName}
            $a.ToLower().IndexOf("myinvocation.tests.ps1") | Should -BeGreaterThan -1
        }

        It 'MyInvocation works when dot source Script file' {
            $a = . {$MyInvocation.ScriptName}
            $a.ToLower().IndexOf("myinvocation.tests.ps1") | Should -BeGreaterThan -1
        }
    }
}
