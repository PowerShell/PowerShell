Describe "Write-Debug DRT Unit Tests" -Tags DRT{
    function unittest-writedebugline
    {
        Write-Host ""
        Write-Host "foo"
        Write-Host "hello" -ForegroundColor Black -BackgroundColor Cyan
        Write-Debug "this is a test"
    }

    It "Write-Debug Test" {
        $o = "this is a test"
        { Write-Debug $o } | Should Not Throw
    }

    It "Write-Debug Test2" {
        { $debugpreference='continue'; Write-Debug 'hello' } | Should Not Throw
        $debugpreference ='SilentlyContinue'
    }

    It "Write-Debug Test3" {
        { unittest-writedebugline } | Should Not Throw
    }
}