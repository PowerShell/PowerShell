# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Describe 'ConvertFrom-Markdown tests' -Tags 'CI' {

    BeforeAll {
        $esc = [char]0x1b

        $hostSupportsVT100 = $Host.UI.SupportsVirtualTerminal

        function GetExpectedString
        {
            [CmdletBinding()]
            param(
            [ValidateSet(
                "Header1", "Header2", "Header3", "Header4", "Header5", "Header6",
                "Code", "CodeBlock",
                "Link", "Image",
                "Bold", "Italics")]
            [Parameter()]
            [string] $ElementType,

            [string] $Text,

            [string] $CodeFormatString,

            [string] $CodeText,

            [bool] $VT100Support
            )

            # Force VT100Support to be false if the host does not support it.
            # This makes the expected string to be correct.
            $VT100Support = $VT100Support -and $hostSupportsVT100

            switch($elementType)
            {
                "Header1" { if($VT100Support) {"$esc[7m$text$esc[0m`n`n" } else {"$text`n`n"} }
                "Header2" { if($VT100Support) {"$esc[4;93m$text$esc[0m`n`n" } else {"$text`n`n"} }
                "Header3" { if($VT100Support) {"$esc[4;94m$text$esc[0m`n`n" } else {"$text`n`n"} }
                "Header4" { if($VT100Support) {"$esc[4;95m$text$esc[0m`n`n" } else {"$text`n`n"} }
                "Header5" { if($VT100Support) {"$esc[4;96m$text$esc[0m`n`n" } else {"$text`n`n"} }
                "Header6" { if($VT100Support) {"$esc[4;97m$text$esc[0m`n`n" } else {"$text`n`n"} }

                "Code" {
                    if($VT100Support) {
                        if($IsMacOS)
                        {
                            ($CodeFormatString -f "$esc[107;95m$CodeText$esc[0m") + "`n`n"
                        }
                        else
                        {
                            ($CodeFormatString -f "$esc[48;2;155;155;155;38;2;30;30;30m$CodeText$esc[0m") + "`n`n"
                        }
                    }
                    else {
                        $CodeFormatString -f "$CodeText" + "`n`n"
                    }
                }
                "CodeBlock" {
                    $expectedString = @()
                    $CodeText -split "`n" | ForEach-Object {
                        if($VT100Support) {
                            if($IsMacOS) {
                                $expectedString += "$esc[107;95m$_$esc[500@$esc[0m"
                            }
                            else {
                                $expectedString += "$esc[48;2;155;155;155;38;2;30;30;30m$_$esc[500@$esc[0m"
                            }

                        }
                        else {
                            $expectedString += $_
                        }
                    }
                    $returnString = $expectedString -join "`n"
                    "$returnString`n`n"
                }

                "Link" { if($VT100Support) {"$esc[4;38;5;117m`"$text`"$esc[0m`n" } else { "`"$text`"`n" } }
                "Image" { if($VT100Support) { "$esc[33m[$text]$esc[0m`n" } else { "[$text]`n" } }
                "Bold" { if($VT100Support) { "$esc[1m$text$esc[0m`n" } else { "$text`n" } }
                "Italics" { if($VT100Support) { "$esc[36m$text$esc[0m`n" } else { "$text`n" } }
            }
        }

        function GetExpectedHTML
        {
            [CmdletBinding()]
            param(
            [ValidateSet(
                "Header1", "Header2", "Header3", "Header4", "Header5", "Header6",
                "Code", "CodeBlock",
                "Link", "Image",
                "Bold", "Italics")]
            [Parameter()]
            [string] $ElementType,

            [string] $Text,

            [string] $Url,

            [string] $CodeFormatString,

            [string] $CodeText
            )

            $id = $Text.Replace(" ","-").ToLowerInvariant()

            switch($elementType)
            {
                "Header1" { "<h1 id=`"$id`">$text</h1>`n" }
                "Header2" { "<h2 id=`"$id`">$text</h2>`n" }
                "Header3" { "<h3 id=`"$id`">$text</h3>`n" }
                "Header4" { "<h4 id=`"$id`">$text</h4>`n" }
                "Header5" { "<h5 id=`"$id`">$text</h5>`n" }
                "Header6" { "<h6 id=`"$id`">$text</h6>`n" }

                "Code" { "<p>" + ($CodeFormatString -f "<code>$CodeText</code>") + "</p>`n" }
                "CodeBlock" { "<pre><code>$CodeText`n</code></pre>`n" }

                "Link" { "<p><a href=`"$Url`">$text</a></p>`n" }
                "Image" { "<p><img src=`"$url`" alt=`"$text`" /></p>`n" }
                "Bold" { "<p><strong>$text</strong></p>`n" }
                "Italics" { "<p><em>$text</em></p>`n" }
            }
        }
    }

    Context 'Basic tests' {
        BeforeAll {
            $mdFile = New-Item -Path $TestDrive/input.md -Value "Some **test string** to write in a file" -Force
            $mdLiteralPath = New-Item -Path $TestDrive/LiteralPath.md -Value "Some **test string** to write in a file" -Force
            $expectedStringFromFile = if ($hostSupportsVT100) {
                "Some $esc[1mtest string$esc[0m to write in a file`n`n"
            } else {
                "Some test string to write in a file`n`n"
            }

            $codeBlock = @'
```
bool function()
{
}
```
'@

            $codeBlockText = @"
bool function()`n{`n}
"@

                $TestCases = @(
                    @{ element = 'Header1'; InputMD = '# Header 1'; Text = 'Header 1'; VT100 = $true }
                    @{ element = 'Header2'; InputMD = '## Header 2'; Text = 'Header 2'; VT100 = $true }
                    @{ element = 'Header3'; InputMD = '### Header 3'; Text = 'Header 3'; VT100 = $true }
                    @{ element = 'Header4'; InputMD = '#### Header 4'; Text = 'Header 4'; VT100 = $true }
                    @{ element = 'Header5'; InputMD = '##### Header 5'; Text = 'Header 5'; VT100 = $true }
                    @{ element = 'Header6'; InputMD = '###### Header 6'; Text = 'Header 6'; VT100 = $true }
                    @{ element = 'Code'; InputMD = 'This is a `code` sample'; CodeFormatString = 'This is a {0} sample'; CodeText = 'code'; VT100 = $true}
                    @{ element = 'CodeBlock'; InputMD = $codeBlock; CodeText = $codeBlockText; VT100 = $true }
                    @{ element = 'Link'; InputMD = '[GitHub](https://www.github.com)'; Text = 'GitHub'; Url = 'https://www.github.com'; VT100 = $true}
                    @{ element = 'Image'; InputMD = '![alt-text](https://bing.com/ps.svg)'; Text = 'alt-text'; Url = 'https://bing.com/ps.svg'; VT100 = $true}
                    @{ element = 'Bold'; InputMD = '**bold text**'; Text = 'bold text'; VT100 = $true}
                    @{ element = 'Italics'; InputMD = '*italics text*'; Text = 'italics text'; VT100 = $true }

                    @{ element = 'Header1'; InputMD = '# Header 1'; Text = 'Header 1'; VT100 = $false }
                    @{ element = 'Header2'; InputMD = '## Header 2'; Text = 'Header 2'; VT100 = $false }
                    @{ element = 'Header3'; InputMD = '### Header 3'; Text = 'Header 3'; VT100 = $false }
                    @{ element = 'Header4'; InputMD = '#### Header 4'; Text = 'Header 4'; VT100 = $false }
                    @{ element = 'Header5'; InputMD = '##### Header 5'; Text = 'Header 5'; VT100 = $false }
                    @{ element = 'Header6'; InputMD = '###### Header 6'; Text = 'Header 6'; VT100 = $false }
                    @{ element = 'Code'; InputMD = 'This is a `code` sample'; CodeFormatString = 'This is a {0} sample'; CodeText = 'code'; VT100 = $false}
                    @{ element = 'CodeBlock'; InputMD = $codeBlock; CodeText = $codeBlockText ; VT100 = $false}
                    @{ element = 'Link'; InputMD = '[GitHub](https://www.github.com)'; Text = 'GitHub'; Url = 'https://www.github.com'; VT100 = $false}
                    @{ element = 'Image'; InputMD = '![alt-text](https://bing.com/ps.svg)'; Text = 'alt-text'; Url = 'https://bing.com/ps.svg'; VT100 = $false}
                    @{ element = 'Bold'; InputMD = '**bold text**'; Text = 'bold text' ; VT100 = $false}
                    @{ element = 'Italics'; InputMD = '*italics text*'; Text = 'italics text' ; VT100 = $false}
                )
        }

        It 'Can convert element : <element> to vt100 using pipeline input - VT100 : <VT100>' -TestCases $TestCases {
            param($element, $inputMD, $text, $codeFormatString, $codeText, $VT100)

            try
            {
                if(-not $VT100)
                {
                    $options = Get-MarkdownOption
                    $options.EnableVT100Encoding = $false
                    $options | Set-MarkdownOption
                }

                $output = $inputMD | ConvertFrom-Markdown -AsVT100EncodedString
            }
            finally
            {
                $options = Get-MarkdownOption
                $options.EnableVT100Encoding = $true
                $options | Set-MarkdownOption
            }

            if($element -like 'Header?' -or
               $element -eq 'Link' -or
               $element -eq 'Image' -or
               $element -eq 'Bold' -or
               $element -eq 'Italics')
            {
                $expectedString = GetExpectedString -ElementType $element -Text $text -VT100Support $VT100
            }
            elseif($element -eq 'Code')
            {
                $expectedString = GetExpectedString -ElementType $element -CodeFormatString $codeFormatString -CodeText $codeText -VT100Support $VT100
            }
            elseif($element -eq 'CodeBlock')
            {
                $expectedString = GetExpectedString -ElementType $element -CodeText $codeText -VT100Support $VT100
            }

            $output.VT100EncodedString | Should -BeExactly $expectedString
        }

        It 'Can convert element : <element> to HTML using pipeline input' -TestCases $TestCases {
            param($element, $inputMD, $text, $codeFormatString, $codeText, $url)

            $output = $inputMD | ConvertFrom-Markdown

            if($element -like 'Header?' -or
               $element -eq 'Bold' -or
               $element -eq 'Italics')
            {
                $expectedString = GetExpectedHTML -ElementType $element -Text $text
            }
            elseif($element -eq 'Code')
            {
                $expectedString = GetExpectedHTML -ElementType $element -CodeFormatString $codeFormatString -CodeText $codeText
            }
            elseif($element -eq 'CodeBlock')
            {
                $expectedString = GetExpectedHTML -ElementType $element -CodeText $codeText
            }
            elseif ($element -eq 'Link')
            {
                $expectedString = GetExpectedHTML -ElementType $element -Text $text -Url $url
            }
            elseif ($element -eq 'Image')
            {
                $expectedString = GetExpectedHTML -ElementType $element -Text $text -Url $url
            }

            $output.Html | Should -BeExactly $expectedString
        }

        It 'Can convert input from a file path to vt100 encoded string' {
            $output = ConvertFrom-Markdown -Path $mdFile.FullName -AsVT100EncodedString
            $output.VT100EncodedString | Should -BeExactly $expectedStringFromFile
        }

        It 'Can convert input from a fileinfo object to vt100 encoded string' {
            $ouputFromFileInfo = $mdFile | ConvertFrom-Markdown -AsVT100EncodedString
            $ouputFromFileInfo.VT100EncodedString | Should -BeExactly $expectedStringFromFile
        }

        It 'Can convert input from a literal path to vt100 encoded string' {
            $output = ConvertFrom-Markdown -Path $mdLiteralPath -AsVT100EncodedString
            $output.VT100EncodedString | Should -BeExactly $expectedStringFromFile
        }

        It 'Can accept Path as positional parameter' {
            $output = ConvertFrom-Markdown $mdFile.FullName -AsVT100EncodedString
            $output.VT100EncodedString | Should -BeExactly $expectedStringFromFile
        }
    }

    Context "ConvertFrom-Markdown error cases" {
        It "Gets an error if path is not FileSystem provider path" {
            { ConvertFrom-Markdown -Path Env:\PSModulePath -ErrorAction Stop } | Should -Throw -ErrorId 'OnlyFileSystemPathsSupported,Microsoft.PowerShell.Commands.ConvertFromMarkdownCommand'
        }

        It "Gets an error if path does not exist" {
            { ConvertFrom-Markdown -Path DoestnotExist -ErrorAction Stop } | Should -Throw -ErrorId 'FileNotFound,Microsoft.PowerShell.Commands.ConvertFromMarkdownCommand'
        }

        It "Gets an error if input object type is not correct" {
            { ConvertFrom-Markdown -InputObject 1 -ErrorAction Stop } | Should -Throw -ErrorId 'InvalidInputObject,Microsoft.PowerShell.Commands.ConvertFromMarkdownCommand'
        }

        It "Gets an error if input file does not exist" {
            { [System.IO.FileInfo]::new("IDoNoExist") | ConvertFrom-Markdown -ErrorAction Stop } | Should -Throw -ErrorId 'FileNotFound,Microsoft.PowerShell.Commands.ConvertFromMarkdownCommand'
        }
    }

    Context "ConvertFrom-Markdown empty or null content tests" {
        BeforeAll {
            $codeBlock = @'
```CSharp
```
'@

            $testCases = @(
                @{Type = "CodeBlock"; Markdown = "$codeBlock"; ExpectedOutput = ''}
                @{Type = "Header1"; Markdown = "# "; ExpectedOutput = ''}
                @{Type = "Header2"; Markdown = "## "; ExpectedOutput = ''}
                @{Type = "Header3"; Markdown = "### "; ExpectedOutput = ''}
                @{Type = "Header4"; Markdown = "#### "; ExpectedOutput = ''}
                @{Type = "Header5"; Markdown = "##### "; ExpectedOutput = ''}
                @{Type = "Header6"; Markdown = "###### "; ExpectedOutput = ''}
                @{Type = "Image"; Markdown = "'![]()'"; ExpectedOutput = if ($hostSupportsVT100) {"'$esc[33m[Image]$esc[0m'"} else {"'[Image]'"}}
                @{Type = "Link"; Markdown = "'[]()'"; ExpectedOutput = if ($hostSupportsVT100) {"'$esc[4;38;5;117m`"`"$esc[0m'"} else {"'`"`"'"}}
            )
        }

        It "No error if thrown when empty content is provided for mardown element : <Type>" -TestCases $testCases {
            param($Type, $Markdown, $ExpectedOutput)

            $resultObj = ConvertFrom-Markdown -InputObject $Markdown -AsVT100EncodedString
            $resultObj.VT100EncodedString.Trim() | Should -BeExactly $ExpectedOutput
        }
    }

    Context "Get/Set-MarkdownOption tests" {

        BeforeAll {
            $esc = [char]0x1b
        }

        BeforeEach {
            $originalOptions = Get-MarkdownOption
        }

        AfterEach {
            Set-MarkdownOption -InputObject $originalOptions
        }

        It "Verify default values for MarkdownOptions" {
            $options = Get-MarkdownOption

            $options.AsEscapeSequence("Header1") | Should -BeExactly "$esc[7m[7m$esc[0m"
            $options.AsEscapeSequence("Header2") | Should -BeExactly "$esc[4;93m[4;93m$esc[0m"
            $options.AsEscapeSequence("Header3") | Should -BeExactly "$esc[4;94m[4;94m$esc[0m"
            $options.AsEscapeSequence("Header4") | Should -BeExactly "$esc[4;95m[4;95m$esc[0m"
            $options.AsEscapeSequence("Header5") | Should -BeExactly "$esc[4;96m[4;96m$esc[0m"
            $options.AsEscapeSequence("Header6") | Should -BeExactly "$esc[4;97m[4;97m$esc[0m"

            if($IsMacOS)
            {
                $options.AsEscapeSequence("Code") | Should -BeExactly "$esc[107;95m[107;95m$esc[0m"
            }
            else
            {
                $options.AsEscapeSequence("Code") | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            }

            $options.AsEscapeSequence("Link") | Should -BeExactly "$esc[4;38;5;117m[4;38;5;117m$esc[0m"
            $options.AsEscapeSequence("Image") | Should -BeExactly "$esc[33m[33m$esc[0m"
            $options.AsEscapeSequence("EmphasisBold") | Should -BeExactly "$esc[1m[1m$esc[0m"
            $options.AsEscapeSequence("EmphasisItalics") | Should -BeExactly "$esc[36m[36m$esc[0m"
        }

        It "Verify Set-MarkdownOption can get options" {
            Set-MarkdownOption `
                -Header1Color "[4;1m" `
                -Header2Color "[93m" `
                -Header3Color "[94m" `
                -Header4Color "[95m" `
                -Header5Color "[96m" `
                -Header6Color "[97m" `
                -ImageAltTextForegroundColor "[34m" `
                -LinkForegroundColor "[4;38;5;88m" `
                -ItalicsForegroundColor "[35m" `
                -BoldForegroundColor "[32m"

            $newOptions = Get-MarkdownOption

            $newOptions.AsEscapeSequence("Header1") | Should -BeExactly "$esc[4;1m[4;1m$esc[0m"
            $newOptions.AsEscapeSequence("Header2") | Should -BeExactly "$esc[93m[93m$esc[0m"
            $newOptions.AsEscapeSequence("Header3") | Should -BeExactly "$esc[94m[94m$esc[0m"
            $newOptions.AsEscapeSequence("Header4") | Should -BeExactly "$esc[95m[95m$esc[0m"
            $newOptions.AsEscapeSequence("Header5") | Should -BeExactly "$esc[96m[96m$esc[0m"
            $newOptions.AsEscapeSequence("Header6") | Should -BeExactly "$esc[97m[97m$esc[0m"

            if($IsMacOS)
            {
                $newOptions.AsEscapeSequence("Code") | Should -BeExactly "$esc[107;95m[107;95m$esc[0m"
            }
            else
            {
                $newOptions.AsEscapeSequence("Code") | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            }

            $newOptions.AsEscapeSequence("Link") | Should -BeExactly "$esc[4;38;5;88m[4;38;5;88m$esc[0m"
            $newOptions.AsEscapeSequence("Image") | Should -BeExactly "$esc[34m[34m$esc[0m"
            $newOptions.AsEscapeSequence("EmphasisBold") | Should -BeExactly "$esc[32m[32m$esc[0m"
            $newOptions.AsEscapeSequence("EmphasisItalics") | Should -BeExactly "$esc[35m[35m$esc[0m"
        }

        It "Verify defaults for light theme" {
            Set-MarkdownOption -Theme Light
            $options = Get-MarkdownOption

            $options.AsEscapeSequence("Header1") | Should -BeExactly "$esc[7m[7m$esc[0m"
            $options.AsEscapeSequence("Header2") | Should -BeExactly "$esc[4;33m[4;33m$esc[0m"
            $options.AsEscapeSequence("Header3") | Should -BeExactly "$esc[4;34m[4;34m$esc[0m"
            $options.AsEscapeSequence("Header4") | Should -BeExactly "$esc[4;35m[4;35m$esc[0m"
            $options.AsEscapeSequence("Header5") | Should -BeExactly "$esc[4;36m[4;36m$esc[0m"
            $options.AsEscapeSequence("Header6") | Should -BeExactly "$esc[4;30m[4;30m$esc[0m"

            if($IsMacOS)
            {
                $options.AsEscapeSequence("Code") | Should -BeExactly "$esc[107;95m[107;95m$esc[0m"
            }
            else
            {
                $options.AsEscapeSequence("Code") | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            }

            $options.AsEscapeSequence("Link") | Should -BeExactly "$esc[4;38;5;117m[4;38;5;117m$esc[0m"
            $options.AsEscapeSequence("Image") | Should -BeExactly "$esc[33m[33m$esc[0m"
            $options.AsEscapeSequence("EmphasisBold") | Should -BeExactly "$esc[1m[1m$esc[0m"
            $options.AsEscapeSequence("EmphasisItalics") | Should -BeExactly "$esc[36m[36m$esc[0m"
        }

        It "Options should be string without escape sequences" {
            $options = Get-MarkdownOption

            $options.Header1 | Should -BeExactly "[7m"
            $options.Header2 | Should -BeExactly "[4;93m"
            $options.Header3 | Should -BeExactly "[4;94m"
            $options.Header4 | Should -BeExactly "[4;95m"
            $options.Header5 | Should -BeExactly "[4;96m"
            $options.Header6 | Should -BeExactly "[4;97m"

            if($IsMacOS)
            {
                $options.Code | Should -BeExactly "[107;95m"
            }
            else
            {
                $options.Code | Should -BeExactly "[48;2;155;155;155;38;2;30;30;30m"
            }


            $options.Link | Should -BeExactly "[4;38;5;117m"
            $options.Image | Should -BeExactly "[33m"
            $options.EmphasisBold | Should -BeExactly "[1m"
            $options.EmphasisItalics | Should -BeExactly "[36m"
        }
    }

    Context "Show-Markdown tests" {
        BeforeEach {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("ShowMarkdownOutputBypass", $true)
        }

        AfterEach {
            [System.Management.Automation.Internal.InternalTestHooks]::SetTestHook("ShowMarkdownOutputBypass", $false)
        }

        It "Can show VT100 converted from markdown" {
            $text = "Bold"
            $mdText = "**$text**"
            $expectedString = GetExpectedString -ElementType 'Bold' -Text $text -VT100Support $true

            $result = $mdText | ConvertFrom-Markdown -AsVT100EncodedString | Show-Markdown
            $result | Should -BeExactly $expectedString
        }

        It "Can show HTML converted from markdown" {
            $text = "Bold"
            $mdText = "**$text**"
            $expectedString = GetExpectedHTML -ElementType 'Bold' -Text $text

            $result = $mdText | ConvertFrom-Markdown | Show-Markdown -UseBrowser
            $result | Should -BeExactly $expectedString
        }

        It "Markdown files work with cmdlet: <pathParam>" -TestCases @(
            @{ pathParam = "Path" }
            @{ pathParam = "LiteralPath" }
        ) {
            param($pathParam)

            $text = "Header"
            $mdText = "# $text"
            $expectedString = GetExpectedString -ElementType Header1 -Text $text -VT100Support $true
            $mdFile = Join-Path $TestDrive "test.md"
            Set-Content -Path $mdFile -Value $mdText

            $params = @{ $pathParam = $mdFile }
            $result = Show-Markdown @params
            $result | Should -BeExactly $expectedString
        }

        It "Can show markdown piped directly to cmdlet" {
            $text = "Header"
            $mdText = "# $text"
            $expectedString = GetExpectedString -ElementType Header1 -Text $text -VT100Support $true

            $result = $mdText | Show-Markdown
            $result | Should -BeExactly $expectedString
        }

        It "Can show markdown piped directly to cmdlet as HTML" {
            $text = "Header"
            $mdText = "# $text"
            $expectedString = GetExpectedHTML -ElementType Header1 -Text $text

            $result = $mdText | Show-Markdown -UseBrowser
            $result | Should -BeExactly $expectedString
        }

        It "Gets an error if the input object is missing the <propertyname> property." -TestCases @(
            @{ propertyname = 'Html' }
            @{ propertyname = 'VT100EncodedString' }
        ) {
            param($propertyname)

            $markdownInfo = [Microsoft.PowerShell.MarkdownRender.MarkdownInfo]::new()

            if($propertyname -eq 'VT100EncodedString')
            {
                { Show-Markdown -InputObject $markdownInfo -ErrorAction Stop } | Should -Throw -ErrorId 'VT100EncodedStringIsNullOrEmpty,Microsoft.PowerShell.Commands.ShowMarkdownCommand'
            }
            else
            {
                { Show-Markdown -UseBrowser -InputObject $markdownInfo -ErrorAction Stop } | Should -Throw -ErrorId 'HtmlIsNullOrEmpty,Microsoft.PowerShell.Commands.ShowMarkdownCommand'
            }
        }

        It "Can show markdown piped directly to cmdlet as array of strings" {
            $testMarkDownPath = Join-Path -Path $TestDrive -ChildPath 'test.md'
            Set-Content -Path $testMarkDownPath -Value "# Header`n`ntext"
            $fileLinesArray = Get-Content -Path $testMarkDownPath
            $result = $fileLinesArray | Show-Markdown
            $expectedString = (ConvertFrom-Markdown -Path $testMarkDownPath -AsVT100EncodedString).VT100EncodedString
            $result | Should -BeExactly $expectedString
        }
    }

    Context "Hosted PowerShell scenario" {

        It 'ConvertFrom-Markdown gets expected output when run in hosted powershell' {

            try {
                $pool = [runspacefactory]::CreateRunspacePool(1, 2, $Host)
                $pool.Open()

                $ps = [powershell]::Create()
                $ps.RunspacePool = $pool
                $ps.AddScript({
                        $output = '# test' | ConvertFrom-Markdown
                        $output.Html.trim()
                    })

                $output = $ps.Invoke()

                $output | Should -BeExactly '<h1 id="test">test</h1>'
            } finally {
                $ps.Dispose()
            }
        }

        It 'Get-MarkdownOption gets default values when run in hosted powershell' {

            try {
                $ps = [powershell]::Create()
                $ps.AddScript( {
                    Get-MarkdownOption -ErrorAction Stop
                })

                $options = $ps.Invoke()

                $options | Should -Not -BeNullOrEmpty
                $options.Header1 | Should -BeExactly "[7m"
                $options.Header2 | Should -BeExactly "[4;93m"
                $options.Header3 | Should -BeExactly "[4;94m"
                $options.Header4 | Should -BeExactly "[4;95m"
                $options.Header5 | Should -BeExactly "[4;96m"
                $options.Header6 | Should -BeExactly "[4;97m"

                if ($IsMacOS) {
                    $options.Code | Should -BeExactly "[107;95m"
                } else {
                    $options.Code | Should -BeExactly "[48;2;155;155;155;38;2;30;30;30m"
                }

                $options.Link | Should -BeExactly "[4;38;5;117m"
                $options.Image | Should -BeExactly "[33m"
                $options.EmphasisBold | Should -BeExactly "[1m"
                $options.EmphasisItalics | Should -BeExactly "[36m"
            }
            finally {
                $ps.Dispose()
            }
        }

        It 'Set-MarkdownOption sets values when run in hosted powershell' {

            try {
                $ps = [powershell]::Create()
                $ps.AddScript( {
                    Set-MarkdownOption -Header1Color '[93m' -ErrorAction Stop -PassThru
                })

                $options = $ps.Invoke()

                $options | Should -Not -BeNullOrEmpty
                $options.Header1 | Should -BeExactly "[93m"
                $options.Header2 | Should -BeExactly "[4;93m"
                $options.Header3 | Should -BeExactly "[4;94m"
                $options.Header4 | Should -BeExactly "[4;95m"
                $options.Header5 | Should -BeExactly "[4;96m"
                $options.Header6 | Should -BeExactly "[4;97m"

                if ($IsMacOS) {
                    $options.Code | Should -BeExactly "[107;95m"
                } else {
                    $options.Code | Should -BeExactly "[48;2;155;155;155;38;2;30;30;30m"
                }

                $options.Link | Should -BeExactly "[4;38;5;117m"
                $options.Image | Should -BeExactly "[33m"
                $options.EmphasisBold | Should -BeExactly "[1m"
                $options.EmphasisItalics | Should -BeExactly "[36m"
            }
            finally {
                $ps.Dispose()
            }
        }
    }
}
