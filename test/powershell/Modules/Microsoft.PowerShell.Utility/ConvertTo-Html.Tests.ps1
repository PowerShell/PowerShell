# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "ConvertTo-Html Tests" -Tags "CI" {

    BeforeAll {
        $customObject = [pscustomobject]@{"Name" = "John Doe"; "Age" = 42; "Friends" = ("Jack", "Jill")}
        $CustomParameters_2 = @{
            Uri             = 'https://microsoft.com/powershell'
        }
        $CustomParameters_3 = @{
            String          = 'https://microsoft.com/powershell can be installed on different https://www.linux.org/ distributions. Most https://www.linux.org/ platforms and distributions have a major release each year, and provide a package manager that is used to install https://microsoft.com/powershell.'
        }
        $newLine = "`r`n"
    }

    function normalizeLineEnds([string]$text)
    {
        $text -replace "`r`n?|`n", "`r`n"
    }

    It "Test ConvertTo-Html with no parameters" {
        $returnObject = $customObject | ConvertTo-Html
        ,$returnObject | Should -BeOfType System.Object[]
        $returnString = $returnObject -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/><col/><col/></colgroup>
<tr><th>Name</th><th>Age</th><th>Friends</th></tr>
<tr><td>John Doe</td><td>42</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html Fragment parameter" {
        $returnString = ($customObject | ConvertTo-Html -Fragment) -join $newLine
        $expectedValue = normalizeLineEnds @"
<table>
<colgroup><col/><col/><col/></colgroup>
<tr><th>Name</th><th>Age</th><th>Friends</th></tr>
<tr><td>John Doe</td><td>42</td><td>System.Object[]</td></tr>
</table>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html as List" {
        $returnString = ($customObject | ConvertTo-Html -As List) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<tr><td>Name:</td><td>John Doe</td></tr>
<tr><td>Age:</td><td>42</td></tr>
<tr><td>Friends:</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html specified properties" {
        $returnString = ($customObject | ConvertTo-Html -Property Name, Friends -As List) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<tr><td>Name:</td><td>John Doe</td></tr>
<tr><td>Friends:</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html using page parameters" {
        $returnString = ($customObject | ConvertTo-Html -Title "Custom Object" -Body "Body Text" -CssUri "page.css" -As List) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>Custom Object</title>
<link rel="stylesheet" type="text/css" href="page.css" />
</head><body>
Body Text
<table>
<tr><td>Name:</td><td>John Doe</td></tr>
<tr><td>Age:</td><td>42</td></tr>
<tr><td>Friends:</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html pre and post" {
        $returnString = ($customObject | ConvertTo-Html -PreContent "Before the object" -PostContent "After the object") -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
Before the object
<table>
<colgroup><col/><col/><col/></colgroup>
<tr><th>Name</th><th>Age</th><th>Friends</th></tr>
<tr><td>John Doe</td><td>42</td><td>System.Object[]</td></tr>
</table>
After the object
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-HTML meta"{
        $returnString = ($customObject | ConvertTo-Html -Meta @{"author"="John Doe"}) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta name="author" content="John Doe">
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/><col/><col/></colgroup>
<tr><th>Name</th><th>Age</th><th>Friends</th></tr>
<tr><td>John Doe</td><td>42</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-HTML meta with invalid properties should throw warning" {
        $parms = @{"authors"="John Doe";"keywords"="PowerShell,PSv6"}
        # make this a string, rather than an array of string so match will behave
        [string]$observedProperties = $customObject | ConvertTo-Html -Meta $parms 3>&1
        $observedProperties | Should -Match $parms["authors"]
    }

    It "Test ConvertTo-HTML charset"{
        $returnString = ($customObject | ConvertTo-Html -Charset "utf-8") -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<meta charset="UTF-8">
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/><col/><col/></colgroup>
<tr><th>Name</th><th>Age</th><th>Friends</th></tr>
<tr><td>John Doe</td><td>42</td><td>System.Object[]</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html URI Auto Create HyperLink #1" {
        $returnString = ([uri]"https://bing.com/" | convertto-html -Property absoluteuri,authority,host,idnhost -hyperlink) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/><col/><col/><col/></colgroup>
<tr><th>AbsoluteUri</th><th>Authority</th><th>Host</th><th>IdnHost</th></tr>
<tr><td><a href="https://bing.com/">https://bing.com/</a></td><td>bing.com</td><td>bing.com</td><td>bing.com</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html URI Auto Create HyperLink #2" {
        $returnString = ($CustomParameters_2 | ConvertTo-Html -hyperlink) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/></colgroup>
<tr><th>Uri</th></tr>
<tr><td><a href="https://microsoft.com/powershell">https://microsoft.com/powershell</a></td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html URI Auto Create HyperLink #3 - embedded" {
        $returnString = ($CustomParameters_3 | ConvertTo-Html -hyperlink) -join $newLine

        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/></colgroup>
<tr><th>String</th></tr>
<tr><td><a href="https://microsoft.com/powershell">https://microsoft.com/powershell</a> can be installed on different <a href="https://www.linux.org/">https://www.linux.org/</a> distributions. Most <a href="https://www.linux.org/">https://www.linux.org/</a> platforms and distributions have a major release each year, and provide a package manager that is used to install <a href="https://microsoft.com/powershell">https://microsoft.com/powershell</a>.</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }


    It "Test ConvertTo-Html URI Auto Create HyperLink #4" {
        $returnString = ([uri]"https://bing.com/" | convertto-html -Property absoluteuri,authority,host,idnhost) -join $newLine
        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/><col/><col/><col/></colgroup>
<tr><th>AbsoluteUri</th><th>Authority</th><th>Host</th><th>IdnHost</th></tr>
<tr><td>https://bing.com/</td><td>bing.com</td><td>bing.com</td><td>bing.com</td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-Html URI Auto Create HyperLink #5 - edge cases" {
        $CustomParameters_4 = @{
            String          = 'Special url with parenthesis http://example.com/test(1).html, and complex url https://www.amazon.com/Amazon-Gift-Card-Print-Logo/dp/B07P76HM3B/ref=sr_1_3?crid=3JKK1WLD85QJV&keywords=gift+card&qid=1668718715&sprefix=gitftcard%2Caps%2C54&sr=8-3, username-password http://userid:password@example.com:8080/'
        }
        $returnString = ($CustomParameters_4 | ConvertTo-Html -hyperlink) -join $newLine

        $expectedValue = normalizeLineEnds @"
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head>
<title>HTML TABLE</title>
</head><body>
<table>
<colgroup><col/></colgroup>
<tr><th>String</th></tr>
<tr><td>Special url with parenthesis <a href="http://example.com/test(1).html">http://example.com/test(1).html</a>, and complex url <a href="https://www.amazon.com/amazon-gift-card-print-logo/dp/b07p76hm3b/ref=sr_1_3?crid=3jkk1wld85qjv&amp;keywords=gift+card&amp;qid=1668718715&amp;sprefix=gitftcard%2caps%2c54&amp;sr=8-3">https://www.amazon.com/Amazon-Gift-Card-Print-Logo/dp/B07P76HM3B/ref=sr_1_3?crid=3JKK1WLD85QJV&amp;keywords=gift+card&amp;qid=1668718715&amp;sprefix=gitftcard%2Caps%2C54&amp;sr=8-3</a>, username-password <a href="http://userid:password@example.com:8080/">http://userid:password@example.com:8080/</a></td></tr>
</table>
</body></html>
"@
        $returnString | Should -Be $expectedValue
    }

    It "Test ConvertTo-HTML transitional"{
        $returnString = $customObject | ConvertTo-Html -Transitional | Select-Object -First 1
        $returnString | Should -Be '<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN"  "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">'
    }

    It "Test ConvertTo-HTML supports scriptblock-based calculated properties: by hashtable" {
        $returnString = ($customObject | ConvertTo-Html @{ l = 'NewAge'; e = { $_.Age + 1 } }) -join $newLine
        $returnString | Should -Match '\b43\b'
    }

    It "Test ConvertTo-HTML supports scriptblock-based calculated properties: directly" {
        $returnString = ($customObject | ConvertTo-Html { $_.Age + 1 }) -join $newLine
        $returnString | Should -Match '\b43\b'
    }

    It "Test ConvertTo-HTML calculated property supports 'name' key as alias of 'label'" {
        $returnString = ($customObject | ConvertTo-Html @{ name = 'AgeRenamed'; e = 'Age'}) -join $newLine
        $returnString | Should -Match 'AgeRenamed'
    }

    It "Test ConvertTo-HTML calculated property supports integer 'width' entry" {
        $returnString = ($customObject | ConvertTo-Html @{ e = 'Age'; width = 10 }) -join $newLine
        $returnString | Should -Match '\swidth\s*=\s*(["''])10\1'
    }

    It "Test ConvertTo-HTML calculated property supports string 'width' entry" {
        $returnString = ($customObject | ConvertTo-Html @{ e = 'Age'; width = '10' }) -join $newLine
        $returnString | Should -Match '\swidth\s*=\s*(["''])10\1'
    }
}
