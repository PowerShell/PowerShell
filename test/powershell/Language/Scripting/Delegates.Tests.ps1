# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe 'Test for conversion b/w script block and delegate' -Tags "CI" {
    BeforeAll {
        function lineno
        {
            $MyInvocation.ScriptLineNumber
        }

        function Generate-ArgumentTypesAndParameters
        {
            param([int] $num, [ref] $parameters, [ref]$argumentTypes, [bool]$hasReturn = $false)

            For($j = 1; $j -le $num; $j++)
            {
                $ran = Get-Random -Minimum 1 -Maximum 3
                $type = [Int32]
                switch ($ran)
                {
                    1 { $type = [Int32]; break}
                    2 { $type = [string]; break}
                    3 { $type = [Hashtable];  break}
                }

                $argumentTypes.Value += $type;

                if((-not $hasReturn) -or ($j -lt $num ) )  {
                    $parameters.Value  += Get-Value($type)
                }
            }
        }

        function Get-Value
        {
            param([type] $t)

            if ($t -eq [Int32]) { 100 }
            elseif($t -eq [string]) { 'abc' }
            elseif ($t -eq [Hashtable])  {@{a = 'foo'} }
        }
    }

    #0 arg, no return
    It 'System.Action' {
        ([System.Action]{ $script:gl=lineno; $args.Length | Should -Be 0 }).Invoke()
        ($gl + 1) | Should -BeExactly (lineno)
    }
    # multiple args, no return
    For($i = 1; $i -le 8; $i++)
    {
        $str = 'System.Action`{0}' -f $i
        Context $str {
            $gt = [object].Assembly.GetType($str)

            $parameters = @()
            $argumentTypes=@()
            Generate-ArgumentTypesAndParameters $i ([ref] $parameters) ([ref] $argumentTypes)

            $ct = $gt.MakeGenericType($argumentTypes)

            $func = { $script:gl=lineno; $args.Length | Should -BeExactly $i } -as $ct
            $func.DynamicInvoke($parameters)
            It '$gl + 2' { ($gl + 2) | Should -BeExactly (lineno) }
        }
    }

    #0 arg with return value
    It 'System.Func[Int32]' {
        ([System.Func[Int32]]{ $script:gl=lineno; $args.Length }).Invoke() | Should -Be 0
        ($gl + 1) | Should -BeExactly (lineno)
    }

    It 'System.Func[string]' {
        ([System.Func[string]]{ $script:gl=lineno; 'hello' }).Invoke() | Should -BeExactly 'hello'
        ($gl + 1) | Should -BeExactly (lineno)
    }

    It 'System.Func[hashtable]' {
        (([System.Func[hashtable]]{ $script:gl=lineno; @{a = 'foo' }}).Invoke()).a | Should -BeExactly 'foo'
        ($gl + 1) | Should -BeExactly (lineno)
    }

    #multiple args, different return type
    For($i = 2; $i -le 9; $i++)
    {
        $str = 'System.Func`{0}' -f $i
        Context $str {
            $gt = [object].Assembly.GetType($str)

            $parameters = @()
            $argumentTypes=@()
            Generate-ArgumentTypesAndParameters $i ([ref] $parameters) ([ref] $argumentTypes) $true
            $v= Get-Value($argumentTypes[$i-1])
            $ct = $gt.MakeGenericType($argumentTypes)
            $func = { $script:gl=lineno; $null = ($args.Length | Should -BeExactly ($i-1)); $v } -as $ct
            $t = $func.DynamicInvoke($parameters)
            It '$gl + 2' { ($gl + 2) | Should -BeExactly (lineno) }
            if ($argumentTypes[$i-1] -eq [Hashtable] )
            {
                It '$t.a' { $t.a | Should -BeExactly $v.a }
            }
            else
            {
                It '$t' { $t | Should -BeExactly $v }
            }
        }
    }
}
