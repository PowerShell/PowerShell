Function IsWindows
{
    $pingCommand = Get-Command -CommandType Application ping
    if ($pingCommand.Definition.IndexOf("\\") -ne -1)
    {
        return 1;
    }
    return 0;
}

Function GetTempDir
{
    if (IsWindows)
    {
        return $env:TEMP
    }
    else
    {
        return "/tmp"
    }
}

