Describe "Format-Table" -Tags "CI" {
		It "Should call format table on piped input without error" {
				{ Get-Date | Format-Table } | Should Not Throw

				{ Get-Date | ft } | Should Not Throw
		}

		It "Should return a format object data type" {
				$val = (Get-Date | Format-Table | gm )

				$val2 = (Get-Date | Format-Table | gm )

				$val.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"

				$val2.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"
		}

		It "Should be able to be called with optional parameters" {
				$v1 = (Get-Date | Format-Table *)
				$v2 = (Get-Date | Format-Table -Property Hour)
				$v3 = (Get-Date | Format-Table -GroupBy Hour)

				$v12 = (Get-Date | ft *)
				$v22 = (Get-Date | ft -Property Hour)
				$v32 = (Get-Date | ft -GroupBy Hour)

		}
}


Describe "Format-Table DRT Unit Tests" -Tags "CI" {
		It "Format-Table with not existing table with force should throw PipelineStoppedException"{
				$obj = New-Object -typename PSObject
				try
				{
						$obj | Format-Table -view bar -force -EA Stop
						Throw "Execution OK"
				}
				catch
				{
						$_.CategoryInfo | Should Match "PipelineStoppedException"
						$_.FullyQualifiedErrorId | Should be "FormatViewNotFound,Microsoft.PowerShell.Commands.FormatTableCommand"
				}
		}

		It "Format-Table with array should work" {
				$al = (0..255)
				$info = @{}
				$info.array = $al
				$result = $info|Format-Table|Out-String
				$result | Should Match "array\s+{0, 1, 2, 3...}"
		}

		It "Format-Table with Negative Count should work" {
				$FormatEnumerationLimit = -1
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1, 2}"
		}

		# Pending on issue#888
		It "Format-Table with Zero Count should work" -Pending {
				$FormatEnumerationLimit = 0
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{...}"
		}

		It "Format-Table with Less Count should work" {
				$FormatEnumerationLimit = 1
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1...}"
		}

		It "Format-Table with More Count should work" {
				$FormatEnumerationLimit = 10
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1, 2}"
		}

		It "Format-Table with Equal Count should work" {
				$FormatEnumerationLimit = 2
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1, 2}"
		}

		# Pending on issue#888
		It "Format-Table with Bogus Count should throw Exception" -Pending {
				$FormatEnumerationLimit = "abc"
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1, 2}"
		}

		# Pending on issue#888
		It "Format-Table with Var Deleted should throw Exception" -Pending {
				$FormatEnumerationLimit = 2
				Remove-Variable FormatEnumerationLimit
				$result = Format-Table -inputobject @{'test'= 1, 2}
				$resultStr = $result|Out-String
				$resultStr | Should Match "test\s+{1, 2}"
		}

		It "Format-Table with new line should work" {
				$info = @{}
				$info.name = "1\n2"
				$result = $info|Format-Table|Out-String
				$result | Should Match "name\s+1.+2"
		}

		It "Format-Table with ExposeBug920454 should work" {
				$IP1 = [System.Net.IPAddress]::Parse("1.1.1.1")
				$IP2 = [System.Net.IPAddress]::Parse("4fde:0000:0000:0002:0022:f376:255.59.171.63")
				$IPs = New-Object System.Collections.ArrayList
				$IPs.Add($IP1)
				$IPs.Add($IP2)
				$info = @{}
				$info.test = $IPs
				$result = $info|Format-Table|Out-String
				$result | Should Match "test\s+{1.1.1.1, 4fde::2:22:f376:ff3b:ab3f}"
		}

		It "Format-Table with Autosize should work" {
				$IP1 = [PSCustomObject]@{'name'='Bob';'size'=1234;'booleanValue'=$true;}
				$IP2 = [PSCustomObject]@{'name'='Jim';'size'=5678;'booleanValue'=$false;}
				$IPs = New-Object System.Collections.ArrayList
				$IPs.Add($IP1)
				$IPs.Add($IP2)
				$result = $IPs|Format-Table -Autosize|Out-String
				$result | Should Match "name size booleanValue"
				$result | Should Match "---- ---- ------------"
				$result | Should Match "Bob\s+1234\s+True"
				$result | Should Match "Jim\s+5678\s+False"
		}

		It "Format-Table with No Objects for End-To-End should work"{
				$p = @{}
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
		}

		It "Format-Table with Null Objects for End-To-End should work"{
				$p = $null
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
		}

		#pending on issue#900
		It "Format-Table with single line string for End-To-End should work" -pending{
				$p = "single line string"
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
		}

		#pending on issue#900
		It "Format-Table with multiple line string for End-To-End should work" -pending{
				$p = "Line1\nLine2"
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
		}

		#pending on issue#900
		It "Format-Table with string sequence for End-To-End should work" -pending{
				$p = "Line1","Line2"
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
		}

		#pending on issue#900
		It "Format-Table with string sequence for End-To-End should work" -pending{
				$p = "Line1","Line2"
				$result = $p|Format-Table -Property "foo","bar"|Out-String
				$result | Should BeNullOrEmpty
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
				$result | Should Match "intArray\s+{1, 2, 3, 4}"
				$result | Should Match "arrayList\s+{string1, string2}"
				$result | Should Match "enumerable\s+Sun"
				$result | Should Match "enumerableTestObject\s+Sun"
		}

		It "Format-Table with Expand Enumerable should work" {
				$obj1 = "x 0","y 0"
				$obj2 = "x 1","y 1"
				$objs = New-Object System.Collections.ArrayList
				$objs.Add($obj1)
				$objs.Add($obj2)
				$mo = [PSCustomObject]@{name = "this is name";sub = $objs}
				$result1 = $mo|Format-Table -Expand CoreOnly|Out-String
				$result1 | Should Match "name\s+sub"
				$result1 | Should Match "this is name"

				$result2 = $mo|Format-Table -Expand EnumOnly|Out-String
				$result2 | Should Match "name\s+sub"
				$result2 | Should Match "this is name\s+{x 0 y 0, x 1 y 1}"

				$result3 = $mo|Format-Table -Expand Both|Out-String
				$result3 | Should Match "name\s+sub"
				$result3 | Should Match "this is name\s+{x 0 y 0, x 1 y 1}"
		}
}
