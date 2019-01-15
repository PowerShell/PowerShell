# Copyright (c) Microsoft Corporation. All rights reserved.
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
    }

	BeforeEach {
		$testfile = Join-Path -Path $TestDrive -ChildPath "clixml-directive.xml"
	}

    AfterEach {
		remove-item $testfile -Force -ErrorAction SilentlyContinue
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
		1,2,3 | Export-Clixml -Path $testfile
		$ps = [PowerShell]::Create()
		$ps.AddCommand("Get-Process")
		$ps.AddCommand("Import-CliXml")
		$ps.AddParameter("Path", $testfile)
		$ps.BeginInvoke()
		$ps.Stop()
		$ps.InvocationStateInfo.State | Should -Be "Stopped"
	}

	It "Export-Clixml using -Depth should work" {
		class Three
		{
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
		Start-Process -FilePath $pshome\pwsh -RedirectStandardOutput $testfile -Args "-noprofile -nologo -outputformat xml -command get-command import-clixml" -Wait
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
}
