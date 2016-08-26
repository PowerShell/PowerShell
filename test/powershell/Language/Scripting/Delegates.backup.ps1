#  <Test>
#    <summary>Test for conversion b/w script block and delegate</summary>
#  </Test>

# This try/catch allows us to define the types on demand, not ignore any errors, and rerun the script.

    Class Argument
    {
        [string] $ArgName = 'T'
        [string] $ArgType = 't'
        [boolean] $IsRealType = $true
    }

    Class DelegateTypeDecoration
    {
        [string]$accessModifier='public'
        [string]$delegateName='Function'            
        [Argument[]] $arguments = $null
        [string]$returnTypeName = "void"
    }

    function Get-DelegateTypeDecoration
    {
        param(
            [Parameter(ValueFromPipeline)]
            [hashtable[]]$Data)

        process
        {
            Write-Output ([DelegateTypeDecoration[]]$Data)
        }
    }
    

    function Get-DelegateDefinition   
    {
        [CmdletBinding()]
        param(
        [Parameter(ValueFromPipeline)]
        [DelegateTypeDecoration[]]$Decorations,
        [string]
        $Description = '')
        
        process
        {
            Foreach($decoration in $Decorations)
            {
                $ret = $decoration.accessModifier + " delegate " + $decoration.returnTypeName + " " + $decoration.delegateName;
                $hasVirtualType = (($decoration.arguments | where { ($_ -ne $null) -and (-not $_.IsRealType) }) -ne $null)
                $typeString =''
                $argString = '('

                if (($decoration.arguments -ne $null) -and ($decoration.arguments.Count -gt 0))
                {
                    $firstVirtualType = $true                
                    if ($hasVirtualType)  { $typeString = '<' }
                    for($i = 0; $i -lt $decoration.arguments.Count; $i++)
                    {
                        if (-not $decoration.arguments[$i].IsRealType)
                        {                    
                            if (-not $firstVirtualType) { $typeString += ', '}
                            $typeString += $decoration.arguments[$i].ArgType                        
                        }
                        if ( $i -ne 0) {$argString += ', '}
                        $argString += $decoration.arguments[$i].ArgType + ' ' + $decoration.arguments[$i].ArgName
                    }
                    if ($hasVirtualType)  { $typeString += '>' }
                }
                $argString += ')'
            
                $ret +=  $typeString + $argString + ';'
                $ret
            }
            #add-type -TypeDefinition $ret
        }
    }

    function Get-Argument
    {
        param(
            [Parameter(ValueFromPipeline)]
            [hashtable[]]$Data)

        process
        {
            Write-Output ([Argument[]]$Data)
        }
    }



function lineno
{
    $myInvocation.ScriptLineNumber
}

#@{},@{arguments = @{ArgName='T1'; ArgType='T';IsRealType=$false }, @{ArgName='Int2'; ArgType='int'} | Get-Argument;DelegateName='Add'; returnTypeName='int' } | Get-DelegateTypeDecoration | Add-DelegateDefinition

Describe "For conversion b/w script block and delegate" -Tags "CI" {
    It 'Conversion of "public void Function();"' {
            #add type first
            $delegateDefinition = (@{} | Get-DelegateTypeDecoration | Get-DelegateDefinition)
            Add-Type -TypeDefinition $delegateDefinition
            ([Function]{ $script:gl=lineno; $args.Length | Should Be 0 }).Invoke()
        }


    $arguments = @()  
    $parameters = @()
    For($i = 1; $i -lt 2; $i++)
    {
        For($j = 1; $j -le $i; $j++)
        {
            $arguments += @{ArgName="t$j"; ArgType="T$j";IsRealType=$false}
            $parameters+=$j              
        }
        $delegateDefinition = @{arguments = $arguments | Get-Argument; DelegateName='Function'; ReturnTypeName='void'} | Get-DelegateTypeDecoration | Get-DelegateDefinition;
        It $delegateDefinition {
            #add type first
            Add-Type -TypeDefinition $delegateDefinition
            $t = [Function[int]]
            ({ $script:gl=lineno; $args.Length | Should Be $i } -as $t).DynamicInvoke($parameters)
        }
    }
}


<#
    add-type -TypeDefinition @"
    public delegate void Function();
    public delegate void Function<T>(T t);
    public delegate void Function<T1,T2,T3,T4,T5,T6,T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);


    public delegate R Function<R, T>(T t);
    public delegate int FunctionRV();
    public delegate string FunctionRR();
    public delegate R FunctionR<R>();

    public delegate int Add2(int i, int j);
    public delegate int Add3(int i, int j, int k);
    public delegate int Add4(int i, int j, int k, int l);
    public delegate int Add5(int i, int j, int k, int l, int m);
    public delegate int Add6(int i, int j, int k, int l, int m, int n);
    public delegate int Add7(int i, int j, int k, int l, int m, int n, int o);


    public delegate T Add2<T>(T i, T j);
    public delegate T Add3<T>(T i, T j, T k);
    public delegate T Add4<T>(T i, T j, T k, T l);
    public delegate T Add5<T>(T i, T j, T k, T l, T m);
    public delegate T Add6<T>(T i, T j, T k, T l, T m, T n);
    public delegate T Add7<T>(T i, T j, T k, T l, T m, T n, T o);
"@
#>
<#([Function]{ $script:gl=lineno; Assert ($args.Length -eq 0) "#1" }).Invoke()

Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
([Function[Hashtable]]{ $script:gl=lineno; Assert ($args[0].a -eq 'foo') "#2" }).Invoke(@{a='foo'})
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Function[string,Hashtable]]{ $script:gl=lineno; $args[0].a }).Invoke(@{a='bar'}) -eq 'bar') "#3"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Function[int,Hashtable]]{ $script:gl=lineno; $args[0].a }).Invoke(@{a=711}) -eq 711) "#4"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([FunctionRV]{ $script:gl=lineno; 42 }).Invoke() -eq 42) "#5"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([FunctionRR]{ $script:gl=lineno; '0042' }).Invoke() -eq '0042') "#6"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([FunctionR[int]]{ $script:gl=lineno; 11 }).Invoke() -eq 11) "#7"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([FunctionR[string]]{ $script:gl=lineno; '0011' }).Invoke() -eq '0011') "#8"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"

Assert (([Add2]{ $script:gl=lineno; $args[0] + $args[1] }).Invoke(1,2) -eq 3) "#9"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add3]{ $script:gl=lineno; $args[0] + $args[1] + $args[2] }).Invoke(1,2,3) -eq 6) "#10"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add4]{ $script:gl=lineno; $args[0] + $args[1] + $args[2] + $args[3] }).Invoke(1,2,3,4) -eq 10) "#11"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add5]{ $script:gl=lineno; $args[0] + $args[1] + $args[2] + $args[3] + $args[4] }).Invoke(1,2,3,4,5) -eq 15) "#12"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add6]{ $script:gl=lineno; $args[0] + $args[1] + $args[2] + $args[3] + $args[4] + $args[5] }).Invoke(1,2,3,4,5,6) -eq 21) "#13"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add7]{ $script:gl=lineno; $args[0] + $args[1] + $args[2] + $args[3] + $args[4] + $args[5] + $args[6] }).Invoke(1,2,3,4,5,6,7) -eq 28) "#14"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"

Assert (([Add2]{ param($i,$j) $script:gl=lineno; $i + $j }).Invoke(1,2) -eq 3) "#15"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add3]{ param($i,$j,$k) $script:gl=lineno; $i + $j + $k }).Invoke(1,2,3) -eq 6) "#16"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add4]{ param($i,$j,$k,$l) $script:gl=lineno; $i + $j + $k + $l }).Invoke(1,2,3,4) -eq 10) "#17"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add5]{ param($i,$j,$k,$l,$m) $script:gl=lineno; $i + $j + $k + $l + $m }).Invoke(1,2,3,4,5) -eq 15) "#18"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add6]{ param($i,$j,$k,$l,$m,$n) $script:gl=lineno; $i + $j + $k + $l + $m + $n }).Invoke(1,2,3,4,5,6) -eq 21) "#19"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add7]{ param($i,$j,$k,$l,$m,$n,$o) $script:gl=lineno; $i + $j + $k + $l + $m + $n + $o }).Invoke(1,2,3,4,5,6,7) -eq 28) "#20"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"

Assert (([Add2]{ param($i) $script:gl=lineno; $i + $args[0] }).Invoke(1,2) -eq 3) "#21"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add3]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] }).Invoke(1,2,3) -eq 6) "#22"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add4]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] }).Invoke(1,2,3,4) -eq 10) "#23"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add5]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] }).Invoke(1,2,3,4,5) -eq 15) "#24"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add6]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] }).Invoke(1,2,3,4,5,6) -eq 21) "#25"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add7]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] + $args[5] }).Invoke(1,2,3,4,5,6,7) -eq 28) "#26"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"

Assert (([Add2[int]]{ param($i) $script:gl=lineno; $i + $args[0] }).Invoke(1,2) -eq 3) "#27"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add3[int]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] }).Invoke(1,2,3) -eq 6) "#28"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add4[int]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] }).Invoke(1,2,3,4) -eq 10) "#29"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add5[int]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] }).Invoke(1,2,3,4,5) -eq 15) "#30"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add6[int]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] }).Invoke(1,2,3,4,5,6) -eq 21) "#31"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add7[int]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] + $args[5] }).Invoke(1,2,3,4,5,6,7) -eq 28) "#32"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"

Assert (([Add2[string]]{ param($i) $script:gl=lineno; $i + $args[0] }).Invoke('ab','cd') -eq 'abcd') "#33"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add3[string]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] }).Invoke('ab','cd','ef') -eq 'abcdef') "#34"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add4[string]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] }).Invoke('ab','cd','ef','gh') -eq 'abcdefgh') "#35"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add5[string]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] }).Invoke('ab','cd','ef','gh','ij') -eq 'abcdefghij') "#36"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add6[string]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] }).Invoke('ab','cd','ef','gh','ij','kl') -eq 'abcdefghijkl') "#37"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
Assert (([Add7[string]]{ param($i) $script:gl=lineno; $i + $args[0] + $args[1] + $args[2] + $args[3] + $args[4] + $args[5] }).Invoke('ab','cd','ef','gh','ij','kl','mn') -eq 'abcdefghijklmn') "#38"
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
([Function[int,int,int,int,int,int,int]]{ param($i,$j,$k,$l,$m,$n,$o) $script:gl=lineno; Assert (($i + $j + $k + $l + $m + $n + $o) -eq 28) "#39" }).Invoke(1,2,3,4,5,6,7)
Assert (($gl + 1) -eq (lineno)) "$gl scope failed"
#>