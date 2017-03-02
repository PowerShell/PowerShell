Describe 'Exceptions flow for classes' -Tags "CI" {

    $canaryHashtable = @{}

    $iss = [initialsessionstate]::CreateDefault()
    $iss.Variables.Add([System.Management.Automation.Runspaces.SessionStateVariableEntry]::new('canaryHashtable', $canaryHashtable, $null))
    $iss.Commands.Add([System.Management.Automation.Runspaces.SessionStateFunctionEntry]::new('Get-Canary', '$canaryHashtable'))
    $ps = [powershell]::Create($iss)

    BeforeEach {
        $canaryHashtable.Clear()
        $ps.Commands.Clear()
    }

    Context 'All calls are inside classes' {

        It 'does not execute statements after instance method with exception' {

            # Put try-catch outside to avoid try-catch logic altering analysis
            try {

                $ps.AddScript( @'
class C
{
    [void] m1()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] = 42
        $this.ImThrow()
        $canaryHashtable['canary'] = 100
    }

    [void] ImThrow()
    {
        throw 'I told you'
    }
}
[C]::new().m1()
'@).Invoke()

            } catch {}

            $canaryHashtable['canary'] | Should Be 42
        }

        It 'does not execute statements after static method with exception' {

            # Put try-catch outside to avoid try-catch logic altering analysis
            try {

                $ps.AddScript( @'
class C
{
    static [void] s1()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] = 43
        [C]::ImThrow()
        $canaryHashtable['canary'] = 100
    }

    static [void] ImThrow()
    {
        1 / 0
    }
}
[C]::s1()
'@).Invoke()

            } catch {}

            $canaryHashtable['canary'] | Should Be 43
        }

        It 'does not execute statements after instance method with exception and deep stack' {

            # Put try-catch outside to avoid try-catch logic altering analysis
            try {

                $ps.AddScript( @'
class C
{
    [void] m1()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] = 1
        $this.m2()
        $canaryHashtable['canary'] = -6101
    }

    [void] m2()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] += 10
        $this.m3()
        $canaryHashtable['canary'] = -6102
    }

    [void] m3()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] += 100
        $this.m4()
        $canaryHashtable['canary'] = -6103
    }

    [void] m4()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] += 1000
        $this.ImThrow()
        $canaryHashtable['canary'] = -6104
    }

    [void] ImThrow()
    {
        $canaryHashtable = Get-Canary
        $canaryHashtable['canary'] += 10000

        1 / 0
    }
}
[C]::new().m1()
'@).Invoke()

            } catch {}

            $canaryHashtable['canary'] | Should Be 11111
        }
    }

    Context 'Class method call PS function' {

        $body = @'
class C
{
    [void] m1()
    {
        m2
    }

    static [void] s1()
    {
        s2
    }
}


function m2()
{
    $canary = Get-Canary
    $canary['canaryM'] = 45
    ImThrow
    $canary['canaryM'] = 100
}

function s2()
{
    $canary = Get-Canary
    $canary['canaryS'] = 46
    CallImThrow
    $canary['canaryS'] = 100
}

function CallImThrow()
{
    ImThrow
}

function ImThrow()
{
    1 / 0
}

'@

        It 'does not execute statements after function with exception called from instance method' {

            # Put try-catch outside to avoid try-catch logic altering analysis
            try {

                $ps.AddScript($body).Invoke()
                $ps.AddScript('$c = [C]::new(); $c.m1()').Invoke()

            } catch {}

            $canaryHashtable['canaryM'] | Should Be 45
        }

        It 'does not execute statements after function with exception called from static method' {

            # Put try-catch outside to avoid try-catch logic altering analysis
            try {

                $ps.AddScript($body).Invoke()
                $ps.AddScript('[C]::s1()').Invoke()

            } catch {}

            $canaryHashtable['canaryS'] | Should Be 46
        }

    }

    Context "No class is involved" {
        It "functions calls continue execution by default" {

            try {

                $ps.AddScript( @'

$canaryHashtable = Get-Canary
function foo() { 1 / 0; $canaryHashtable['canary'] += 10 }
$canaryHashtable['canary'] = 1
foo
$canaryHashtable['canary'] += 100

'@).Invoke()

            } catch {}

            $canaryHashtable['canary'] | Should Be 111
        }
    }
}

Describe "Exception error position" -Tags "CI" {
    class MSFT_3090412
    {
        static f1() { [MSFT_3090412]::bar = 42 }
        static f2() { throw "an error in f2" }
        static f3() { "".Substring(0, 10) }
        static f4() { dir nosuchfile -ea Stop }
    }

    It "Setting a property that doesn't exist" {
        try {
            [MSFT_3090412]::f1()
            throw "f1 should have thrown"
        } catch {
            $_.InvocationInfo.Line | Should Match ([regex]::Escape('[MSFT_3090412]::bar = 42'))
        }
    }

    It "Throwing an exception" {
        try {
            [MSFT_3090412]::f2()
            throw "f2 should have thrown"
        } catch {
            $_.InvocationInfo.Line | Should Match ([regex]::Escape('throw "an error in f2"'))
        }
    }

    It "Calling a .Net method that throws" {
        try {
            [MSFT_3090412]::f3()
            throw "f3 should have thrown"
        } catch {
            $_.InvocationInfo.Line | Should Match ([regex]::Escape('"".Substring(0, 10)'))
        }
    }

    It "Terminating error" {
        try {
            [MSFT_3090412]::f4()
            throw "f4 should have thrown"
        } catch {
            $_.InvocationInfo.Line | Should Match ([regex]::Escape('dir nosuchfile -ea Stop'))
        }
    }
}

Describe "Exception from initializer" -Tags "CI" {
    class MSFT_6397334a
    {
        [int]$a = "zz"
        MSFT_6397334a() {}
    }

    class MSFT_6397334b
    {
        [int]$a = "zz"
    }

    class MSFT_6397334c
    {
        static [int]$a = "zz"
        static MSFT_6397334a() {}
    }

    class MSFT_6397334d
    {
        static [int]$a = "zz"
    }

    It "instance member w/ ctor" {
        try {
            [MSFT_6397334a]::new()
            throw "[MSFT_6397334a]::new() should have thrown"
        }
        catch
        {
            $e = $_
            $e.FullyQualifiedErrorId | Should Be InvalidCastFromStringToInteger
            $e.InvocationInfo.Line | Should Match 'a = "zz"'
        }
    }

    It "instance member w/o ctor" {
        try {
            [MSFT_6397334b]::new()
            throw "[MSFT_6397334b]::new() should have thrown"
        }
        catch
        {
            $e = $_
            $e.FullyQualifiedErrorId | Should Be InvalidCastFromStringToInteger
            $e.InvocationInfo.Line | Should Match 'a = "zz"'
        }
    }

    It "static member w/ ctor" {
        try {
            $null = [MSFT_6397334c]::a
            throw "No Exception!"
        }
        catch
        {
            $_.Exception | Should BeOfType System.TypeInitializationException
            $e  = $_.Exception.InnerException.InnerException.ErrorRecord
            $e.FullyQualifiedErrorId | Should Be InvalidCastFromStringToInteger
            $e.InvocationInfo.Line | Should Match 'a = "zz"'
        }
    }

    It "static member w/o ctor" {
        try {
            $null = [MSFT_6397334d]::a
            throw "No Exception!"
        }
        catch
        {
            $_.Exception | Should BeOfType System.TypeInitializationException
            $e  = $_.Exception.InnerException.InnerException.ErrorRecord
            $e.FullyQualifiedErrorId | Should Be InvalidCastFromStringToInteger
            $e.InvocationInfo.Line | Should Match 'a = "zz"'
        }
    }
}
