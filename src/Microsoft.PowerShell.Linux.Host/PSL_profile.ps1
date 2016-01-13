(get-psprovider 'FileSystem').Home = $env:HOME

function prompt
{
    "PSL " + $(get-location) + "> "
}

& {
    for ($i = 0; $i -lt 26; $i++)
    {
        $funcname = ([System.Char]($i+65)) + ':'
        $str = "function global:$funcname { set-location $funcname } "
        invoke-expression $str
    }
}

function banner 
{
   "`nPowerShell for Linux interactive console"
   "========================================"
   "- Type 'get-help' for help"
   "- Type 'exit' to exit`n"
}

banner
