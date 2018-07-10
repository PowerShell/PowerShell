# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

Describe 'ConvertFrom-Markdown tests' -Tags 'CI' {

    BeforeAll {
        $esc = [char]0x1b

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

            [string] $CodeText
            )

            switch($elementType)
            {
                "Header1" { "$esc[7m$text$esc[0m`n`n" }
                "Header2" { "$esc[4;93m$text$esc[0m`n`n" }
                "Header3" { "$esc[4;94m$text$esc[0m`n`n" }
                "Header4" { "$esc[4;95m$text$esc[0m`n`n" }
                "Header5" { "$esc[4;96m$text$esc[0m`n`n" }
                "Header6" { "$esc[4;97m$text$esc[0m`n`n" }

                "Code" { ($CodeFormatString -f "$esc[48;2;155;155;155;38;2;30;30;30m$CodeText$esc[0m") + "`n`n" }
                "CodeBlock" {
                    $expectedString = @()
                    $CodeText -split "`n" | ForEach-Object { $expectedString += "$esc[48;2;155;155;155;38;2;30;30;30m$_$esc[500@$esc[0m" }
                    $returnString = $expectedString -join "`n"
                    "$returnString`n`n"
                }

                "Link" { "$esc[4;38;5;117m`"$text`"$esc[0m`n" }
                "Image" { "$esc[33m[$text]$esc[0m`n" }
                "Bold" { "$esc[1m$text$esc[0m`n" }
                "Italics" { "$esc[36m$text$esc[0m`n" }
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
            $esc = [char]0x1b
            $mdFile = New-Item -Path $TestDrive/input.md -Value "Some **test string** to write in a file" -Force
            $mdLiteralPath = New-Item -Path $TestDrive/LiteralPath.md -Value "Some **test string** to write in a file" -Force
            $expectedStringFromFile = "Some $esc[1mtest string$esc[0m to write in a file`n`n"

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
                    @{ element = 'Header1'; InputMD = '# Header 1'; Text = 'Header 1' }
                    @{ element = 'Header2'; InputMD = '## Header 2'; Text = 'Header 2' }
                    @{ element = 'Header3'; InputMD = '### Header 3'; Text = 'Header 3' }
                    @{ element = 'Header4'; InputMD = '#### Header 4'; Text = 'Header 4' }
                    @{ element = 'Header5'; InputMD = '##### Header 5'; Text = 'Header 5' }
                    @{ element = 'Header6'; InputMD = '###### Header 6'; Text = 'Header 6' }
                    @{ element = 'Code'; InputMD = 'This is a `code` sample'; CodeFormatString = 'This is a {0} sample'; CodeText = 'code'}
                    @{ element = 'CodeBlock'; InputMD = $codeBlock; CodeText = $codeBlockText }
                    @{ element = 'Link'; InputMD = '[GitHub](https://www.github.com)'; Text = 'GitHub'; Url = 'https://www.github.com'}
                    @{ element = 'Image'; InputMD = '![alt-text](https://bing.com/ps.svg)'; Text = 'alt-text'; Url = 'https://bing.com/ps.svg'}
                    @{ element = 'Bold'; InputMD = '**bold text**'; Text = 'bold text' }
                    @{ element = 'Italics'; InputMD = '*italics text*'; Text = 'italics text' }
                )
        }


        It 'Can convert element : <element> to vt100 using pipeline input' -TestCases $TestCases {
            param($element, $inputMD, $text, $codeFormatString, $codeText)

            $output = $inputMD | ConvertFrom-Markdown -AsVT100EncodedString

            if($element -like 'Header?' -or
               $element -eq 'Link' -or
               $element -eq 'Image' -or
               $element -eq 'Bold' -or
               $element -eq 'Italics')
            {
                $expectedString = GetExpectedString -ElementType $element -Text $text
            }
            elseif($element -eq 'Code')
            {
                $expectedString = GetExpectedString -ElementType $element -CodeFormatString $codeFormatString -CodeText $codeText
            }
            elseif($element -eq 'CodeBlock')
            {
                $expectedString = GetExpectedString -ElementType $element -CodeText $codeText
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

            $options.Header1 | Should -BeExactly "$esc[7m[7m$esc[0m"
            $options.Header2 | Should -BeExactly "$esc[4;93m[4;93m$esc[0m"
            $options.Header3 | Should -BeExactly "$esc[4;94m[4;94m$esc[0m"
            $options.Header4 | Should -BeExactly "$esc[4;95m[4;95m$esc[0m"
            $options.Header5 | Should -BeExactly "$esc[4;96m[4;96m$esc[0m"
            $options.Header6 | Should -BeExactly "$esc[4;97m[4;97m$esc[0m"
            $options.Code | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            $options.Link | Should -BeExactly "$esc[4;38;5;117m[4;38;5;117m$esc[0m"
            $options.Image | Should -BeExactly "$esc[33m[33m$esc[0m"
            $options.EmphasisBold | Should -BeExactly "$esc[1m[1m$esc[0m"
            $options.EmphasisItalics | Should -BeExactly "$esc[36m[36m$esc[0m"
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

            $options.Header1 | Should -BeExactly "$esc[4;1m[4;1m$esc[0m"
            $options.Header2 | Should -BeExactly "$esc[93m[93m$esc[0m"
            $options.Header3 | Should -BeExactly "$esc[94m[94m$esc[0m"
            $options.Header4 | Should -BeExactly "$esc[95m[95m$esc[0m"
            $options.Header5 | Should -BeExactly "$esc[96m[96m$esc[0m"
            $options.Header6 | Should -BeExactly "$esc[97m[97m$esc[0m"
            #$options.Code | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            $options.Link | Should -BeExactly "$esc[4;38;5;88m[4;38;5;88m$esc[0m"
            $options.Image | Should -BeExactly "$esc[34m[34m$esc[0m"
            $options.EmphasisBold | Should -BeExactly "$esc[32m[32m$esc[0m"
            $options.EmphasisItalics | Should -BeExactly "$esc[35m[35m$esc[0m"
        }

        It "Verify defaults for light theme" {
            $options = Get-MarkdownOption

            $options.Header1 | Should -BeExactly "$esc[7m[7m$esc[0m"
            $options.Header2 | Should -BeExactly "$esc[4;33m[4;33m$esc[0m"
            $options.Header3 | Should -BeExactly "$esc[4;34m[4;34m$esc[0m"
            $options.Header4 | Should -BeExactly "$esc[4;35m[4;35m$esc[0m"
            $options.Header5 | Should -BeExactly "$esc[4;36m[4;36m$esc[0m"
            $options.Header6 | Should -BeExactly "$esc[4;30m[4;30m$esc[0m"
            $options.Code | Should -BeExactly "$esc[48;2;155;155;155;38;2;30;30;30m[48;2;155;155;155;38;2;30;30;30m$esc[0m"
            $options.Link | Should -BeExactly "$esc[4;38;5;117m[4;38;5;117m$esc[0m"
            $options.Image | Should -BeExactly "$esc[33m[33m$esc[0m"
            $options.EmphasisBold | Should -BeExactly "$esc[1m[1m$esc[0m"
            $options.EmphasisItalics | Should -BeExactly "$esc[36m[36m$esc[0m"
        }
    }

}
