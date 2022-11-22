# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "Format-Table" -Tags "CI" {
    BeforeAll {
        if ($null -ne $PSStyle) {
            $outputRendering = $PSStyle.OutputRendering
            $PSStyle.OutputRendering = 'plaintext'
        }
    }

    AfterAll {
        if ($null -ne $PSStyle) {
            $PSStyle.OutputRendering = $outputRendering
        }
    }

        It "Should call format table on piped input without error" {
                { Get-Date | Format-Table } | Should -Not -Throw
        }

        It "Should return a format object data type" {
                $val = Get-Date | Format-Table | Get-Member
                $val2 = Get-Date | Format-Table | Get-Member

                $val.TypeName | Should -Match "Microsoft.Powershell.Commands.Internal.Format"
                $val2.TypeName | Should -Match "Microsoft.Powershell.Commands.Internal.Format"
        }

        It "Should be able to be called with optional parameters" {
                $v1 = Get-Date | Format-Table *
                $v2 = Get-Date | Format-Table -Property Hour
                $v3 = Get-Date | Format-Table -GroupBy Hour
        }

        It "Format-Table with not existing table with force should throw PipelineStoppedException"{
                $obj = New-Object -TypeName PSObject
                $e = { $obj | Format-Table -View bar -Force -ErrorAction Stop } |
                    Should -Throw -ErrorId "FormatViewNotFound,Microsoft.PowerShell.Commands.FormatTableCommand" -PassThru
                $e.CategoryInfo | Should -Match "PipelineStoppedException"
        }

        It "Format-Table with array should work" {
                $al = (0..255)
                $info = @{}
                $info.array = $al
                $result = $info | Format-Table | Out-String
                $result | Should -Match "array\s+{0, 1, 2, 3`u{2026}}" # ellipsis
        }

        It "Format-Table with Negative Count should work" {
                $FormatEnumerationLimit = -1
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{1, 2}"
        }

        # Pending on issue#888
        It "Format-Table with Zero Count should work" -Pending {
                $FormatEnumerationLimit = 0
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{...}"
        }

        It "Format-Table with Less Count should work" {
                $FormatEnumerationLimit = 1
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{1...}"
        }

        It "Format-Table with More Count should work" {
                $FormatEnumerationLimit = 10
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{1, 2}"
        }

        It "Format-Table with Equal Count should work" {
                $FormatEnumerationLimit = 2
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{1, 2}"
        }

        # Pending on issue#888
        It "Format-Table with Bogus Count should throw Exception" -Pending {
                $FormatEnumerationLimit = "abc"
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result|Out-String
                $resultStr | Should -Match "test\s+{1, 2}"
        }

        # Pending on issue#888
        It "Format-Table with Var Deleted should throw Exception" -Pending {
                $FormatEnumerationLimit = 2
                Remove-Variable FormatEnumerationLimit
                $result = Format-Table -InputObject @{'test'= 1, 2}
                $resultStr = $result | Out-String
                $resultStr | Should -Match "test\s+{1, 2}"
        }

        It "Format-Table with new line should work" {
                $info = @{}
                $info.name = "1\n2"
                $result = $info | Format-Table | Out-String
                $result | Should -Match "name\s+1.+2"
        }

        It "Format-Table with ExposeBug920454 should work" {
                $IP1 = [System.Net.IPAddress]::Parse("1.1.1.1")
                $IP2 = [System.Net.IPAddress]::Parse("4fde:0000:0000:0002:0022:f376:255.59.171.63")
                $IPs = New-Object System.Collections.ArrayList
                $IPs.Add($IP1)
                $IPs.Add($IP2)
                $info = @{}
                $info.test = $IPs
                $result = $info | Format-Table | Out-String
                $result | Should -Match "test\s+{1.1.1.1, 4fde::2:22:f376:ff3b:ab3f}"
        }

        It "Format-Table with Autosize should work" {
                $IP1 = [PSCustomObject]@{'name'='Bob';'size'=1234;'booleanValue'=$true;}
                $IP2 = [PSCustomObject]@{'name'='Jim';'size'=5678;'booleanValue'=$false;}
                $IPs = New-Object System.Collections.ArrayList
                $IPs.Add($IP1)
                $IPs.Add($IP2)
                $result = $IPs | Format-Table -AutoSize | Out-String
                $result | Should -Match "name size booleanValue"
                $result | Should -Match "---- ---- ------------"
                $result | Should -Match "Bob\s+1234\s+True"
                $result | Should -Match "Jim\s+5678\s+False"
        }

        It "Format-Table with '<testName>' should return `$null" -TestCases @(
            @{ testName = "empty array"; testObject = @{}   },
            @{ testName = "null"       ; testObject = $null }
        ) {
            param ($testObject)
                $result = $testObject | Format-Table -Property "foo","bar" | Out-String
                $result | Should -BeNullOrEmpty
        }

        It "Format-Table with '<testName>' string and -Force should output table with requested properties" -TestCases @(
            @{ testName = "single line"; testString = "single line string" },
            @{ testName = "multi line" ; testString = "line1`nline2"       },
            @{ testName = "array"      ; testString = "line1","line2"      }
        ) {
            param ($testString)
            $result = $testString | Format-Table -Property "fox","bar" -Force | Out-String
            $result.Replace(" ","").Replace([Environment]::NewLine,"") | Should -BeExactly "foxbar------"
        }

        It "Format-Table with complex object for End-To-End should work" {
                Add-Type -TypeDefinition "public enum MyDayOfWeek{Sun,Mon,Tue,Wed,Thu,Fri,Sat}"
                $eto = New-Object MyDayOfWeek
                $info = @{}
                $info.intArray = 1,2,3,4
                $info.arrayList = "string1","string2"
                $info.enumerable = [MyDayOfWeek]$eto
                $info.enumerableTestObject = $eto
                $result = $info|Format-Table|Out-String
                $result | Should -Match "intArray\s+{1, 2, 3, 4}"
                $result | Should -Match "arrayList\s+{string1, string2}"
                $result | Should -Match "enumerable\s+Sun"
                $result | Should -Match "enumerableTestObject\s+Sun"
        }

        It "Format-Table with Expand Enumerable should work" {
                $obj1 = "x 0","y 0"
                $obj2 = "x 1","y 1"
                $objs = New-Object System.Collections.ArrayList
                $objs.Add($obj1)
                $objs.Add($obj2)
                $mo = [PSCustomObject]@{name = "this is name";sub = $objs}
                $result1 = $mo|Format-Table -Expand CoreOnly|Out-String
                $result1 | Should -Match "name\s+sub"
                $result1 | Should -Match "this is name"

                $result2 = $mo|Format-Table -Expand EnumOnly|Out-String
                $result2 | Should -Match "name\s+sub"
                $result2 | Should -Match "this is name\s+{x 0 y 0, x 1 y 1}"

                $result3 = $mo|Format-Table -Expand Both|Out-String
                $result3 | Should -Match "name\s+sub"
                $result3 | Should -Match "this is name\s+{x 0 y 0, x 1 y 1}"
        }

        It "Format-Table should not add trailing whitespace to the header" {
            $out = "hello" | Format-Table -Property foo -Force | Out-String
            $out.Replace([System.Environment]::NewLine, "") | Should -BeExactly "foo---"
        }

        It "Format-Table should not add trailing whitespace to rows" {
            $out = [pscustomobject]@{a=1;b=2} | Format-Table -HideTableHeaders | Out-String
            $out.Replace([System.Environment]::NewLine, "") | Should -BeExactly "1 2"
        }

        It "Format-Table should have correct alignment" {
            $ps1xml = @"
<Configuration>
    <ViewDefinitions>
        <View>
            <Name>Test.Format</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>Left</Label>
                        <Alignment>left</Alignment>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Label>Center</Label>
                        <Alignment>center</Alignment>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Label>Right</Label>
                        <Alignment>right</Alignment>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>Left</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Center</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Right</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
    </ViewDefinitions>
</Configuration>
"@

            $ps1xmlPath = Join-Path -Path $TestDrive -ChildPath "alignment.format.ps1xml"
            Set-Content -Path $ps1xmlPath -Value $ps1xml
            # run in own runspace so not affect global sessionstate
            $ps = [powershell]::Create()
            $ps.AddScript( {
                param($ps1xmlPath)
                Update-FormatData -AppendPath $ps1xmlPath
                $a = [PSCustomObject]@{Left=1;Center=2;Right=3}
                $a.PSObject.TypeNames.Insert(0,"Test.Format")
                $a | Out-String
            } ).AddArgument($ps1xmlPath) | Out-Null
            $output = $ps.Invoke()

            $expectedTable = @"

Left Center Right
---- ------ -----
1      2        3



"@
            $output.Replace("`n","").Replace("`r","") | Should -BeExactly $expectedTable.Replace("`n","").Replace("`r","")
        }

        It "Format-Table should not have trailing whitespace if there is truncation: <view>" -TestCases @(
            # `u{2B758} is a double-byte Japanese character
            # `u{2026} is the ellipsis
            @{view="Test.Format.Left"  ; object=[pscustomobject]@{Left="123`u{2B758}"}    ; expected="Left----123`u{2026}"      },
            @{view="Test.Format.Center"; object=[pscustomobject]@{Center="12345`u{2B758}"}; expected="Center------12345`u{2026}"}
        ) {
            param($view, $object, $expected)

            $ps1xml = @"
<Configuration>
    <ViewDefinitions>
        <View>
            <Name>Test.Format.Left</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>Left</Label>
                        <Alignment>left</Alignment>
                        <Width>4</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>Left</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
        <View>
            <Name>Test.Format.Center</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>Center</Label>
                        <Alignment>center</Alignment>
                        <Width>6</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>Center</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
    </ViewDefinitions>
</Configuration>
"@

            $ps1xmlPath = Join-Path -Path $TestDrive -ChildPath "truncation.format.ps1xml"
            Set-Content -Path $ps1xmlPath -Value $ps1xml
            # run in own runspace so not affect global sessionstate
            $ps = [powershell]::Create()
            $ps.AddScript( {
                param($ps1xmlPath,$view,$object)
                Update-FormatData -AppendPath $ps1xmlPath
                $object.PSObject.TypeNames.Insert(0,"Test.Format")
                $object | Format-Table -View $view | Out-String
            } ).AddArgument($ps1xmlPath).AddArgument($view).AddArgument($object) | Out-Null
            $output = $ps.Invoke()
            $output.Replace("`n","").Replace("`r","") | Should -BeExactly $expected
        }

        It "Format-Table should correctly render headers that span multiple rows: <variation>" -TestCases @(
            @{ view = "Default"; widths = 7,7,7; variation = "2 row, 1 row, 1 row"; expectedTable = @"

LongLon Header2 Header3
gHeader
------- ------- -------
1       2       3


"@ },
            @{ view = "Default"; widths = 4,7,4; variation = "4 row, 1 row, 2 row"; expectedTable = @"

Long Header2 Head
Long         er3
Head
er
---- ------- ----
1    2       3


"@ },
            @{ view = "Default"; widths = 4,4,7; variation = "4 row, 2 row, 1 row"; expectedTable = @"

Long Head Header3
Long er2
Head
er
---- ---- -------
1    2    3


"@ },
            @{ view = "Default"; widths = 14,7,3; variation = "1 row, 1 row, 3 row"; expectedTable = @"

LongLongHeader Header2 Hea
                       der
                       3
-------------- ------- ---
1              2       3


"@ },
            @{ view = "One"; widths = 4,1,1; variation = "1 column"; expectedTable = @"

Long
Long
Head
er
----
1


"@ }
        ) {
            param($view, $widths, $expectedTable)
            $ps1xml = @"
<Configuration>
    <ViewDefinitions>
        <View>
            <Name>Default</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>LongLongHeader</Label>
                        <Width>{0}</Width>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Label>Header2</Label>
                        <Width>{1}</Width>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Label>Header3</Label>
                        <Width>{2}</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>First</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Second</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Third</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
        <View>
            <Name>One</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>LongLongHeader</Label>
                        <Width>{0}</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>First</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
    </ViewDefinitions>
</Configuration>
"@
            $ps1xml = $ps1xml.Replace("{0}", $widths[0]).Replace("{1}", $widths[1]).Replace("{2}", $widths[2])
            $ps1xmlPath = Join-Path -Path $TestDrive -ChildPath "span.format.ps1xml"
            Set-Content -Path $ps1xmlPath -Value $ps1xml
            # run in own runspace so not affect global sessionstate
            $ps = [powershell]::Create()
            $ps.AddScript( {
                param($ps1xmlPath, $view)
                Update-FormatData -AppendPath $ps1xmlPath
                $a = [PSCustomObject]@{First=1;Second=2;Third=3}
                $a.PSObject.TypeNames.Insert(0,"Test.Format")
                $a | Format-Table -View $view | Out-String
            } ).AddArgument($ps1xmlPath).AddArgument($view) | Out-Null
            $output = $ps.Invoke()
            foreach ($e in $ps.Streams.Error)
            {
                Write-Verbose $e.ToString() -Verbose
            }
            $ps.HadErrors | Should -BeFalse
            $output.Replace("`r","").Replace(" ",".").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Format-Table should correctly render rows: <variation>" -TestCases @(
            @{ view = "Default"; widths = 4,7,5; variation = "narrow values"; values = [PSCustomObject]@{First=1;Second=2;Third=3}; wrap = $false; expectedTable = @"

Long*Header2*Heade
Long**********r3
Head
er
----*-------*-----
1**********2***3


"@ },
            @{ view = "Default"; widths = 4,7,5; variation = "narrow values with wrap"; values = [PSCustomObject]@{First=1;Second=2;Third=3}; wrap = $true; expectedTable = @"

Long*Header2*Heade
Long**********r3
Head
er
----*-------*-----
1**********2***3


"@ },
            @{ view = "Default"; widths = 4,7,5; variation = "wide values"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $false; expectedTable = @"

Long*Header2*Heade
Long**********r3
Head
er
----*-------*-----
123`u{2026}*`u{2026}345678*1234`u{2026}


"@ },
            @{ view = "One"; widths = 3,1,1; variation = "wide values, 1 column"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $false; expectedTable = @"

Lon
gLo
ngH
ead
er
---
12`u{2026}


"@ },
            @{ view = "Default"; widths = 4,8,6; variation = "wide values with wrap, 1st column"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $true; expectedTable = @"

Long**Header2*Header
Long************3
Head
er
----**-------*------
1234*12345678*123456
5


"@ },
            @{ view = "Default"; widths = 5,7,6; variation = "wide values with wrap, 2nd column"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $true; expectedTable = @"

LongL*Header2*Header
ongHe***********3
ader
-----*-------*------
12345*1234567*123456
************8


"@ },
            @{ view = "Default"; widths = 5,8,5; variation = "wide values with wrap, 3rd column"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $true; expectedTable = @"

LongL**Header2*Heade
ongHe***********r3
ader
-----**-------*-----
12345*12345678*12345
*****************6


"@ },
            @{ view = "Default"; widths = 4,7,5; variation = "wide values with wrap, all 3 columns"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $true; expectedTable = @"

Long*Header2*Heade
Long**********r3
Head
er
----*-------*-----
1234*1234567*12345
5**********8***6


"@ },
            @{ view = "One"; widths = 3,1,1; variation = "wide values with wrap, with 1 column"; values = [PSCustomObject]@{First="12345";Second="12345678";Third="123456"}; wrap = $true; expectedTable = @"

Lon
gLo
ngH
ead
er
---
123
45


"@ }
        ) {
            param($view, $widths, $values, $wrap, $expectedTable)
            $ps1xml = @"
<Configuration>
    <ViewDefinitions>
        <View>
            <Name>Default</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Alignment>left</Alignment>
                        <Label>LongLongHeader</Label>
                        <Width>{0}</Width>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Alignment>right</Alignment>
                        <Label>Header2</Label>
                        <Width>{1}</Width>
                    </TableColumnHeader>
                    <TableColumnHeader>
                        <Alignment>center</Alignment>
                        <Label>Header3</Label>
                        <Width>{2}</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>First</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Second</PropertyName>
                            </TableColumnItem>
                            <TableColumnItem>
                                <PropertyName>Third</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
        <View>
            <Name>One</Name>
            <ViewSelectedBy>
                <TypeName>Test.Format</TypeName>
            </ViewSelectedBy>
            <TableControl>
                <TableHeaders>
                    <TableColumnHeader>
                        <Label>LongLongHeader</Label>
                        <Width>{0}</Width>
                    </TableColumnHeader>
                </TableHeaders>
                <TableRowEntries>
                    <TableRowEntry>
                        <TableColumnItems>
                            <TableColumnItem>
                                <PropertyName>First</PropertyName>
                            </TableColumnItem>
                        </TableColumnItems>
                    </TableRowEntry>
                </TableRowEntries>
            </TableControl>
        </View>
    </ViewDefinitions>
</Configuration>
"@
            $ps1xml = $ps1xml.Replace("{0}", $widths[0]).Replace("{1}", $widths[1]).Replace("{2}", $widths[2])
            $ps1xmlPath = Join-Path -Path $TestDrive -ChildPath "render.format.ps1xml"
            Set-Content -Path $ps1xmlPath -Value $ps1xml
            # run in own runspace so not affect global sessionstate
            $ps = [powershell]::Create()
            $ps.AddScript( {
                param($ps1xmlPath, $view, $values, $wrap)
                Update-FormatData -AppendPath $ps1xmlPath
                $values.PSObject.TypeNames.Insert(0,"Test.Format")
                $values | Format-Table -View $view -Wrap:$wrap | Out-String
            } ).AddArgument($ps1xmlPath).AddArgument($view).AddArgument($values).AddArgument($wrap) | Out-Null
            $output = $ps.Invoke()
            foreach ($e in $ps.Streams.Error)
            {
                Write-Verbose $e.ToString() -Verbose
            }
            $ps.HadErrors | Should -BeFalse
            $output.Replace("`r","").Replace(" ","*").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Should render header/row correctly where values are wider than header w/ implicit autosize: <variation>" -TestCases @(
            @{ variation = "first column"; obj = [pscustomobject]@{abc="1234";bcd="123"},[pscustomobject]@{abc="1";bcd="1234"}; expectedTable = @"

abc  bcd
---  ---
1234 123
1    1234


"@ },
            @{ variation = "both columns"; obj = [pscustomobject]@{abc="1234";bcd="1234"},[pscustomobject]@{abc="1";bcd="1"}; expectedTable = @"

abc  bcd
---  ---
1234 1234
1    1


"@ },
            @{ variation = "second column"; obj = [pscustomobject]@{abc="123";bcd="1234"},[pscustomobject]@{abc="1";bcd="123"}; expectedTable = @"

abc bcd
--- ---
123 1234
1   123


"@ }
        ) {
            param($obj, $expectedTable)
            $output = $obj | Format-Table | Out-String
            $output.Replace("`r","").Replace(" ",".").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Should render header correctly where header is shorter than column width with justification: <variation>" -TestCases @(
            @{ variation = "left/right"; obj = [PSCustomObject]@{a="abc";b=123}; expectedTable = @"

a     b
-     -
abc 123


"@ },
            @{ variation = "left/left"; obj = [PSCustomObject]@{a="abc";b="abc"}; expectedTable = @"

a   b
-   -
abc abc


"@ },
            @{ variation = "right/left"; obj = [PSCustomObject]@{a=123;b="abc"}; expectedTable = @"

  a b
  - -
123 abc


"@ },
            @{ variation = "right/right"; obj = [PSCustomObject]@{a=123;b=123}; expectedTable = @"

  a   b
  -   -
123 123


"@ }
        ) {
            param($obj, $expectedTable)
            $output = $obj | Format-Table | Out-String
            $output.Replace("`r","").Replace(" ",".").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Should render rows correctly when wrapped: <variation>" -TestCases @(
            @{ variation = "right"; obj = [pscustomobject] @{A=1;B=2;Name="This`nIs some random`nmultiline content"}; expectedTable = @"

A B Name
- - ----
1 2 This
    Is some random
    multiline content


"@ },
            @{ variation = "left"; obj = [pscustomobject] @{Name="This`nIs some random`nmultiline content";A=1;B=2}; expectedTable = @"

Name                                  A B
----                                  - -
This                                  1 2
Is some random
multiline content


"@ },
            @{ variation = "middle"; obj = [pscustomobject] @{A=1;Name="This`nIs some random`nmultiline content";B=2}; expectedTable = @"

A Name                                  B
- ----                                  -
1 This                                  2
  Is some random
  multiline content


"@ }
        ) {
            param($obj, $expectedTable)
            $output = $obj | Format-Table -Wrap | Out-String
            $output.Replace("`r","").Replace(" ",".").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Should not return null when the Console width is equal to 0" {
            [system.management.automation.internal.internaltesthooks]::SetTestHook('SetConsoleWidthToZero', $true)
            try
            {
                # Fill the console window with the string, so that it reaches its max width.
                # Check if the max width is equal to default value (120), to test test hook set.
                $testObject = @{ test = '1' * 200}
                Format-Table -InputObject $testObject | Out-String -Stream | ForEach-Object{$_.length} | Sort-Object -Bottom 1 | Should -Be 120
            }
            finally {
                [system.management.automation.internal.internaltesthooks]::SetTestHook('SetConsoleWidthToZero', $false)
            }
        }

        It "-RepeatHeader should output the header at every screen full" -Skip:([Console]::WindowHeight -eq 0) {
            $numHeaders = 4
            $numObjects = [Console]::WindowHeight * $numHeaders
            $out = 1..$numObjects | ForEach-Object { @{foo=$_} } | Format-Table -RepeatHeader | Out-String
            $lines = $out.Split([System.Environment]::NewLine)
            ($lines | Select-String "Name\s*Value").Count | Should -Be ($numHeaders + 1)
        }

        It "Should be formatted correctly if width is declared and using center alignment" {
            $expectedTable = @"

   one
   ---
    1


"@
            $output = [pscustomobject] @{ one = 1 } | Format-Table @{ l='one'; e='one'; width=10; alignment='center' } | Out-String
            $output.Replace("`r","").Replace(" ",".").Replace("`n","^") | Should -BeExactly $expectedTable.Replace("`r","").Replace(" ",".").Replace("`n","^")
        }

        It "Should be formatted correctly with double byte wide chars" {
            $obj = [pscustomobject]@{
                "哇" = "62";
                "dbda" = "KM";
                "消息" = "千"
            }

            $expected = @"

哇 dbda 消息
-- ---- ----
62 KM   千


"@

            $actual = $obj | Format-Table | Out-String
            ($actual.Replace("`r`n", "`n")) | Should -BeExactly ($expected.Replace("`r`n", "`n"))
        }

        It 'Table should format floats, doubles, and decimals with number of decimals from current culture' {
            $o = [PSCustomObject]@{
                double = [double]1234.56789
                float = [float]9876.54321
                decimal = [decimal]4567.123456789
            }

            $table = $o | Format-Table | Out-String

            $line = foreach ($line in $table.split([System.Environment]::NewLine)) { if ($line -match '^1234') { $line } }
            $line | Should -Not -BeNullOrEmpty
            $expectedDecimals = (Get-Culture).NumberFormat.NumberDecimalDigits

            foreach ($num in $line.split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)) {
                $numDecimals = $num.length - $num.indexOf((Get-Culture).NumberFormat.NumberDecimalSeparator) - 1
                $numDecimals | Should -Be $expectedDecimals -Because $num
            }
        }
    }

Describe 'Table color tests' -Tag 'CI' {
    BeforeAll {
        $originalRendering = $PSStyle.OutputRendering
        $PSStyle.OutputRendering = 'Ansi'
    }

    AfterAll {
        $PSStyle.OutputRendering = $originalRendering
    }

    It 'Table header should use TableHeader' {
        ([pscustomobject]@{foo = 1} | Format-Table | Out-String).Trim() | Should -BeExactly @"
$($PSStyle.Formatting.TableHeader)foo$($PSStyle.Reset)
$($PSStyle.Formatting.TableHeader)---$($PSStyle.Reset)
  1
"@
    }
}
