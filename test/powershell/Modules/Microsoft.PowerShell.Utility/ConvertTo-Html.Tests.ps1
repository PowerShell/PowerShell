Describe "ConvertTo-Html Tests" -Tags "CI" {

    BeforeAll {
        $customObject = [pscustomobject]@{"Name" = "John Doe"; "Age" = 42; "Friends" = ("Jack", "Jill")}
        $newLine = "`r`n"
    }

    function normalizeLineEnds([string]$text)
    {
        $text -replace "`r`n?|`n", "`r`n"
    }

    It "Test ConvertTo-Html with no parameters" {
        $returnObject = $customObject | ConvertTo-Html
        $returnObject -is [System.Array] | Should Be $true
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
        $returnString | Should Be $expectedValue
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
        $returnString | Should Be $expectedValue
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
        $returnString | Should Be $expectedValue
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
        $returnString | Should Be $expectedValue
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
        $returnString | Should Be $expectedValue
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
        $returnString | Should Be $expectedValue
    }
}

