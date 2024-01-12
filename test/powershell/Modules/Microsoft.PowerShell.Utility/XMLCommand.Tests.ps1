# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
Describe "XmlCommand DRT basic functionality Tests" -Tags "CI" {

	BeforeAll {
		if(-not ('IsHiddenTestType' -as "type"))
		{
			Add-Type -TypeDefinition @"
		public class IsHiddenTestType
        {
            public IsHiddenTestType()
            {
                Property1 = 1;
                Property2 = "some string";
            }

            public IsHiddenTestType(int val, string data)
            {
                Property1 = val;
                Property2 = data;
            }

            public int Property1;
            public string Property2;
        }
"@
		}

        class Four
        {
            [int] $num = 4;
        }

        class Three
        {
            [Four] $four = [Four]::New();
            [int] $num = 3;
        }

        class Two
        {
            [Three] $three = [Three]::New();
            [int] $value = 2;
        }

        class One
        {
            [Two] $two = [Two]::New();
            [int] $value = 1;
        }
    }

	BeforeEach {
		$testfile = Join-Path -Path $TestDrive -ChildPath "clixml-directive.xml"
	}

    AfterEach {
		Remove-Item $testfile -Force -ErrorAction SilentlyContinue
    }

 	It "Import with CliXml directive should work" {
        Get-Command export* -Type Cmdlet | Select-Object -First 3 | Export-Clixml -Path $testfile
		$results = Import-Clixml $testfile
		$results.Count | Should -BeExactly 3
        $results[0].PSTypeNames[0] | Should -Be "Deserialized.System.Management.Automation.CmdletInfo"
    }

	It "Import with Rehydration should work" {
		$property1 = 256
		$property2 = "abcdef"
		$isHiddenTestType = [IsHiddenTestType]::New($property1,$property2)
		$isHiddenTestType | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.Property1 | Should -Be $property1
		$results.Property2 | Should -Be $property2
    }

	It "Import Hashtable from exported non-ordered dictionary object should create non-ordered dictionary object" {
		$dict = @{}
		$dict['Larry'] = 'Poik!'
		$dict['Curly'] = 'Nyuk!'
		$dict['Moe'] = 'Wise guy!'
		$dict['larry'] = 'poik!'
		$dict['curly'] = 'nyuk!'
		$dict['moe'] = 'wise guy!'
		$dict.Count | Should -Be 3
		$dict | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "Hashtable"
		$results.Count | Should -Be 3
    }

	It "Import OrderedDictionary from exported ordered dictionary object should create ordered dictionary object" {
		$dict = [ordered]@{}
		$dict['Larry'] = 'Poik!'
		$dict['Curly'] = 'Nyuk!'
		$dict['Moe'] = 'Wise guy!'
		$dict['larry'] = 'poik!'
		$dict['curly'] = 'nyuk!'
		$dict['moe'] = 'wise guy!'
		$dict.Count | Should -Be 3
		$dict | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "OrderedDictionary"
		$results.Count | Should -Be 3
		$results[0] | Should -BeExactly $dict[0]
		$results[1] | Should -BeExactly $dict[1]
		$results[2] | Should -BeExactly $dict[2]
    }

	It "Import Hashtable from exported non-ordered case-sentitive dictionary object" {
		$dict = New-Object hashtable
		$dict['Larry'] = 'Poik!'
		$dict['Curly'] = 'Nyuk!'
		$dict['Moe'] = 'Wise guy!'
		$dict['larry'] = 'poik!'
		$dict['curly'] = 'nyuk!'
		$dict['moe'] = 'wise guy!'
		$dict.Count | Should -Be 6
		$dict | Export-Clixml $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "Hashtable"
		$results.Count | Should -Be 6
	}

	It "Import OrderedDictionary from XML with case-sensitive duplicate keys" {
		$dupKeysXml =
		'<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
				<Obj RefId="0">
					<TN RefId="0">
					<T>System.Collections.Specialized.OrderedDictionary</T>
					<T>System.Object</T>
					</TN>
					<DCT>
					<En>
						<S N="Key">Larry</S>
						<S N="Value">Poik!</S>
					</En>
					<En>
						<S N="Key">Moe</S>
						<S N="Value">Wise guy!</S>
					</En>
					<En>
						<S N="Key">Curly</S>
						<S N="Value">Nyuk!</S>
					</En>
					<En>
						<S N="Key">larry</S>
						<S N="Value">poik!</S>
					</En>
					<En>
						<S N="Key">moe</S>
						<S N="Value">wise guy!</S>
					</En>
					<En>
						<S N="Key">curly</S>
						<S N="Value">nyuk!</S>
					</En>
					</DCT>
				</Obj>
			</Objs>
			'
		$dupKeysXml | Out-File -FilePath $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "OrderedDictionary"
		$results.Count | Should -Be 6
	}

	It "Import Hashtable from XML with duplicate keys" {
		$dupKeysXml =
			'<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
				<Obj RefId="0">
					<TN RefId="0">
					<T>System.Collections.Hashtable</T>
					<T>System.Object</T>
					</TN>
					<DCT>
					<En>
						<S N="Key">Larry</S>
						<S N="Value">Poik!</S>
					</En>
					<En>
						<S N="Key">Moe</S>
						<S N="Value">Wise guy!</S>
					</En>
					<En>
						<S N="Key">Curly</S>
						<S N="Value">Nyuk!</S>
					</En>
					<En>
						<S N="Key">Larry</S>
						<S N="Value">Poik!</S>
					</En>
					<En>
						<S N="Key">Moe</S>
						<S N="Value">Wise guy!</S>
					</En>
					<En>
						<S N="Key">Curly</S>
						<S N="Value">Nyuk!</S>
					</En>
					</DCT>
				</Obj>
			</Objs>
			'
		$dupKeysXml | Out-File -FilePath $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "Hashtable"
		$results.Count | Should -Be 6
	}

	It "Import OrderedDictionary from XML with duplicate keys" {
		$dupKeysXml =
			'<Objs Version="1.1.0.1" xmlns="http://schemas.microsoft.com/powershell/2004/04">
				<Obj RefId="0">
					<TN RefId="0">
					<T>System.Collections.Specialized.OrderedDictionary</T>
					<T>System.Object</T>
					</TN>
					<DCT>
					<En>
						<S N="Key">Larry</S>
						<S N="Value">Poik!</S>
					</En>
					<En>
						<S N="Key">Moe</S>
						<S N="Value">Wise guy!</S>
					</En>
					<En>
						<S N="Key">Curly</S>
						<S N="Value">Nyuk!</S>
					</En>
					<En>
						<S N="Key">Larry</S>
						<S N="Value">Poik!</S>
					</En>
					<En>
						<S N="Key">Moe</S>
						<S N="Value">Wise guy!</S>
					</En>
					<En>
						<S N="Key">Curly</S>
						<S N="Value">Nyuk!</S>
					</En>
					</DCT>
				</Obj>
			</Objs>
			'
		$dupKeysXml | Out-File -FilePath $testfile
		$results = Import-Clixml $testfile
		$results.GetType().Name | Should -BeExactly  "OrderedDictionary"
		$results.Count | Should -Be 6
	}

	It "Export-Clixml StopProcessing should succeed" {
        $ps = [PowerShell]::Create()
        $null = $ps.AddScript("1..10")
        $null = $ps.AddCommand("foreach-object")
        $null = $ps.AddParameter("Process", { $_; Start-Sleep -Seconds 1 })
        $null = $ps.AddCommand("Export-CliXml")
        $null = $ps.AddParameter("Path", $testfile)
        $null = $ps.BeginInvoke()
        Start-Sleep -Seconds 1
        $null = $ps.Stop()
        $ps.InvocationStateInfo.State | Should -Be "Stopped"
        $ps.Dispose()
	}

	It "Import-Clixml StopProcessing should succeed" {
        # create a file to use for import. It should have some complexity
		Get-Process -Id $PID | Export-Clixml -Path $testfile
        # create a large number of files to import
        $script = '$f = ,"' + $testfile + '" * 1000'
		$ps = [PowerShell]::Create()
        $ps.AddScript($script)
		$ps.Invoke()
        $ps.Commands.Clear()
		$ps.AddScript('$null = Import-CliXml -Path $f')
		$ps.BeginInvoke()
		$ps.Stop()
		$ps.InvocationStateInfo.State | Should -Be "Stopped"
	}

	It "Export-Clixml using -Depth should work" {
		$one = [One]::New()
		$one | Export-Clixml -Depth 2 -Path $testfile
		$deserialized_one = Import-Clixml -Path $testfile
		$deserialized_one.Value | Should -Be 1
		$deserialized_one.two.Value | Should -Be 2
		$deserialized_one.two.Three | Should -Not -BeNullOrEmpty
		$deserialized_one.two.three.num | Should -BeNullOrEmpty
	}

	It "Import-Clixml should work with XML serialization from pwsh.exe" {
		# need to create separate process so that current powershell doesn't interpret clixml output
		Start-Process -FilePath $PSHOME\pwsh -RedirectStandardOutput $testfile -Args "-noprofile -nologo -outputformat xml -command get-command import-clixml" -Wait
		$out = Import-Clixml -Path $testfile
		$out.Name | Should -Be "Import-CliXml"
		$out.CommandType.ToString() | Should -Be "Cmdlet"
		$out.Source | Should -Be "Microsoft.PowerShell.Utility"
	}

	It "Import-Clixml -IncludeTotalCount always returns unknown total count" {
		# this cmdlets supports paging, but not this switch
		[PSCustomObject]@{foo=1;bar=@{hello="world"}} | Export-Clixml -Path $testfile
		$out = Import-Clixml -Path $testfile -IncludeTotalCount
		$out[0].ToString() | Should -BeExactly "Unknown total count"
	}

	It "Import-Clixml -First and -Skip work together for simple types" {
		"one","two","three","four" | Export-Clixml -Path $testfile
		$out = Import-Clixml -Path $testfile -First 2 -Skip 1
		$out.Count | Should -Be 2
		$out[0] | Should -BeExactly "two"
		$out[1] | Should -BeExactly "three"
	}

	It "Import-Clixml -First and -Skip work together for collections" {
		@{a=1;b=2;c=3;d=4} | Export-Clixml -Path $testfile
		# order not guaranteed, even with [ordered] so we have to be smart here and compare against the full result
		$out1 = Import-Clixml -Path $testfile	# this results in a hashtable
		$out2 = Import-Clixml -Path $testfile -First 2 -Skip 1	# this results in a dictionary entry
		$out2.Count | Should -Be 2
        ($out2.Name) -join ":" | Should -Be (@($out1.Keys)[1, 2] -join ":")
        ($out2.Value) -join ":" | Should -Be (@($out1.Values)[1, 2] -join ":")
	}

	# these tests just cover aspects that aren't normally exercised being used as a cmdlet
	It "Can read back switch and parameter values using api" {
		Add-Type -AssemblyName "${pshome}/Microsoft.PowerShell.Commands.Utility.dll"

		$cmd = [Microsoft.PowerShell.Commands.ExportClixmlCommand]::new()
		$cmd.LiteralPath = "foo"
		$cmd.LiteralPath | Should -BeExactly "foo"
		$cmd.NoClobber = $true
		$cmd.NoClobber | Should -BeTrue

		$cmd = [Microsoft.PowerShell.Commands.ImportClixmlCommand]::new()
		$cmd.LiteralPath = "bar"
		$cmd.LiteralPath | Should -BeExactly "bar"

		$cmd = [Microsoft.PowerShell.Commands.SelectXmlCommand]::new()
		$cmd.LiteralPath = "foo"
		$cmd.LiteralPath | Should -BeExactly "foo"
		$xml = [xml]"<a/>"
		$cmd.Xml = $xml
		$cmd.Xml | Should -Be $xml
	}

    Context "ConvertTo-CliXml & ConvertFrom-CliXml" {

        It "Getting cmdlet info should work" {
            $content = Get-Command export* -Type Cmdlet | Select-Object -First 3 | ConvertTo-CliXml
            $results = ConvertFrom-CliXml $content
            $results.Count | Should -Be 3
            $results[0].PSTypeNames[0] | Should -BeExactly "Deserialized.System.Management.Automation.CmdletInfo"
            $results[1].PSTypeNames[0] | Should -BeExactly "Deserialized.System.Management.Automation.CmdletInfo"
            $results[2].PSTypeNames[0] | Should -BeExactly "Deserialized.System.Management.Automation.CmdletInfo"
        }

        It "Rehydration should work" {
            $property1 = 256
            $property2 = "abcdef"
            $isHiddenTestType = [IsHiddenTestType]::New($property1,$property2)
            $content = $isHiddenTestType | ConvertTo-CliXml
            $results = ConvertFrom-CliXml $content
            $results.Property1 | Should -Be $property1
            $results.Property2 | Should -BeExactly $property2
        }

        It "ConvertTo-CliXml StopProcessing should succeed" {
            $ps = [PowerShell]::Create()
            $null = $ps.AddScript("1..10")
            $null = $ps.AddCommand("foreach-object")
            $null = $ps.AddParameter("Process", { $_; Start-Sleep -Seconds 1 })
            $null = $ps.AddCommand("ConvertTo-CliXml")

            Wait-UntilTrue { $ps.BeginInvoke() } -IntervalInMilliseconds 1000
            $null = $ps.Stop()
            $ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
            $ps.Dispose()
        }

        It "ConvertFrom-CliXml StopProcessing should succeed" {
            $content = 1,2,3 | ConvertTo-CliXml
            $ps = [PowerShell]::Create()
            $ps.AddCommand("Get-Process")
            $ps.AddCommand("ConvertFrom-CliXml")
            $ps.AddParameter("InputObject", $content)
            $ps.BeginInvoke()
            $ps.Stop()
            $ps.InvocationStateInfo.State | Should -BeExactly "Stopped"
        }

        It "Should serialize integers correctly using ValueFromPipeline" {
            $testObject = 1,2,3
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [array] | Should -BeTrue
            $out.Count | Should -Be 3
            $out[0] | Should -Be 1
            $out[1] | Should -Be 2
            $out[2] | Should -Be 3
        }

        It "Using default depth of 2 should work" {
            $testObject = [One]::New()
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-Clixml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $deserialized_one = ConvertFrom-CliXml -InputObject $content
            $deserialized_one.value | Should -Be 1
            $deserialized_one.two | Should -Not -BeNullOrEmpty
            $deserialized_one.two.value | Should -Be 2
            $deserialized_one.two.three | Should -Not -BeNullOrEmpty
            $deserialized_one.two.three.num | Should -BeNullOrEmpty
        }

        It "Using -Depth 3 should work" {
            $testObject = [One]::New()
            $content = $testObject | ConvertTo-CliXml -Depth 3
            $testObject | Export-CliXml -Path $testfile -Depth 3
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $deserialized_one = ConvertFrom-CliXml -InputObject $content
            $deserialized_one.value | Should -Be 1
            $deserialized_one.two | Should -Not -BeNullOrEmpty
            $deserialized_one.two.value | Should -Be 2
            $deserialized_one.two.three | Should -Not -BeNullOrEmpty
            $deserialized_one.two.three.num | Should -Be 3
            $deserialized_one.two.three.four | Should -Not -BeNullOrEmpty
            $deserialized_one.two.three.four.num | Should -BeNullOrEmpty
        }

        It "Should serialize array correctly using ValueFromPipeline" {
            $testObject = @(1,2,3,4)
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [array] | Should -BeTrue
            $out.Count | Should -Be 4
            $out[0] | Should -Be 1
            $out[1] | Should -Be 2
            $out[2] | Should -Be 3
            $out[3] | Should -Be 4
        }

        It "Should serialize array correctly using -InputObject" {
            $testObject = @(1,2,3,4)
            $content = ConvertTo-CliXml -InputObject $testObject
            Export-CliXml -Path $testfile -InputObject $testObject
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [System.Collections.ArrayList] | Should -BeTrue
            $out.Count | Should -Be 4
            $out[0] | Should -Be 1
            $out[1] | Should -Be 2
            $out[2] | Should -Be 3
            $out[3] | Should -Be 4
        }

        It "Should serialize hashtable correctly" {
            $testObject = [ordered]@{ a = 1; b = 2; c = 3; d = 4 }
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [ordered] | Should -BeTrue
            $out.Count | Should -Be 4
            $out.Keys | Should -BeIn @('a', 'b', 'c', 'd')
            $out.Values | Should -BeIn @(1, 2, 3, 4)
        }

        It "Should serialize PSCustomObject correctly" {
            $testObject = [PSCustomObject]@{ a = 1; b = 2; c = 3; d = 4 }
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [pscustomobject] | Should -BeTrue
            $out.a | Should -Be 1
            $out.b | Should -Be 2
            $out.c | Should -Be 3
            $out.d | Should -Be 4
        }

        It "Should serialize nested PSCustomObject correctly" {
            $testObject = [PSCustomObject]@{ a = 1; b = 2; c = 3; d = [PSCustomObject]@{ e = 4 } }
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [pscustomobject] | Should -BeTrue
            $out.a | Should -Be 1
            $out.b | Should -Be 2
            $out.c | Should -Be 3
            $out.d -is [pscustomobject] | Should -BeTrue
            $out.d.e | Should -Be 4
        }

        It "Should serialize array of PSCustomObjects correctly" {
            $testObject = @(
                [PSCustomObject]@{ Property = 1 }
                [PSCustomObject]@{ Property = 2 }
                [PSCustomObject]@{ Property = 3 }
            )
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [array] | Should -BeTrue
            $out.Count | Should -Be 3
            $out[0].Property | Should -Be 1
            $out[1].Property | Should -Be 2
            $out[2].Property | Should -Be 3
        }

        It "Should serialize array of single PSCustomObject when using ValueFromPipeline" {
            $testObject = @(
                [PSCustomObject]@{ Property = 1 }
            )
            $content = $testObject | ConvertTo-CliXml
            $testObject | Export-CliXml -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [pscustomobject] | Should -BeTrue
            $out.Property | Should -Be 1
        }

        It "Should serialize array of single PSCustomObject when using -InputObject" {
            $testObject = @(
                [PSCustomObject]@{ Property = 1 }
            )
            $content = ConvertTo-CliXml -InputObject $testObject
            Export-CliXml -InputObject $testObject -Path $testfile
            $content | Should -Be (Get-Content -Path $testfile -Raw)
            $out = ConvertFrom-CliXml -InputObject $content
            $out -is [System.Collections.ArrayList] | Should -BeTrue
            $out.Count | Should -Be 1
            $out[0].Property | Should -Be 1
        }
    }
}
