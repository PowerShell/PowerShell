if($psculture -eq 'en-US')
{
    ConvertFrom-StringData @'
        string1=string1 for en-US in if
        string2=string2 for en-US in if
'@
}