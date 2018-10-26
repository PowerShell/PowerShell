$CaseFoldingTxt = './CaseFolding.txt'

function Load-CaseFoldingTxt
{
    Invoke-WebRequest -Uri http://www.unicode.org/Public/UCD/latest/ucd/CaseFolding.txt -OutFile $CaseFoldingTxt
}

function ConvertFromUtf32
{
    param([int32] $utf32)

    $utf32 = $utf32 - 0x10000
    $address0 = [int]($utf32 / 0x400) + 0xd800
    $address1 = $utf32 % 0x400 + 0xdc00
    [System.String]::Format('0x{0:X4}{1:X4}', $address0, $address1)
}

function Start-Gen
{
    $CaseFoldingTxtHeader = Get-Content -LiteralPath $CaseFoldingTxt -TotalCount 6 | ForEach-Object { "// $_" }

    $lines = Get-Content -LiteralPath $CaseFoldingTxt | Where-Object { !$_.StartsWith("#") -and $_ -ne "" }

    $SimpleCaseFoldingTableBMPane1 = [string[]]::new(65536)
    $SimpleCaseFoldingTableBMPane2 = [string[]]::new(65536)
    for ($i = 0; $i -lt 65536; $i++)
    {
        $SimpleCaseFoldingTableBMPane1[$i] = "(char)"+$i
        $SimpleCaseFoldingTableBMPane2[$i] = "(char)"+$i
    }

    $lines | ForEach-Object {
        $blocks = $_ -split "; "
        if ($blocks[1] -eq "C" -or $blocks[1] -eq "S") {
            try {
                $line = [convert]::ToInt32($blocks[0], 16)
                $value = [convert]::ToInt32($blocks[2], 16)
            } catch {
                Write-Host $blocks[0]
                Write-Host $blocks[2]
            }
            if ($line -le 65535) {
                $SimpleCaseFoldingTableBMPane1[$line] = "(char)"+$value
            } else {
                $SimpleCaseFoldingTableBMPane2[$line - 65536] = "(char)"+($value - 65536)
            }
            #Write-Host "$blocks[0] ($SimpleCaseFoldingTablePane01In) - $blocks[2] ($(ConvertFromUtf32 ("0x"+$blocks[2])))"
            #Sleep 5
        }
    }

    $fileDate = [System.Datetime]::Now.ToString("yyyyMMddmmss")

    $template = @"
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The file automatically generated at $fileDate
// Source file is loaded from file http://www.unicode.org/Public/UCD/latest/ucd/CaseFolding.txt

$CaseFoldingTxtHeader

using System.Collections.Generic;

namespace System.Management.Automation.Unicode
{
    /// <summary>
    /// Simple case folding methods.
    /// </summary>
    internal static partial class SimpleCaseFolding
    {
        /// <summary>
        /// Lookup a char in the 's_simpleCaseFoldingTableBMPane1' table. Get a index. Use the index to lookup target char in 's_simpleCaseFoldingTableInOut'
        /// </summary>
        private static readonly char[] s_simpleCaseFoldingTableBMPane1 = new char[]
        {
            $($SimpleCaseFoldingTableBMPane1 -join ",`n            ")
        };

        private static readonly char[] s_simpleCaseFoldingTableBMPane2 = new char[]
        {
            $($SimpleCaseFoldingTableBMPane2 -join ",`n            ")
        };

    }
}
"@

    Set-Content -LiteralPath ".\CaseFolding-$fileDate.gen.cs" -Value $template
}
