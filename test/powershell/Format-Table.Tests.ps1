Describe "Format-Table" {
    It "Should call format table on piped input without error" {
	{ Get-Process | Format-Table } | Should Not Throw

	{ Get-Process | ft } | Should Not Throw
    }

    It "Should return a format object data type" {
	$val = (Get-Process | Format-Table | gm )

	$val2 = (Get-Process | Format-Table | gm )

	$val.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"

	$val2.TypeName | Should Match "Microsoft.Powershell.Commands.Internal.Format"
    }

    It "Should be able to be called with optional parameters" {
	$v1 = (Get-Process | Format-Table *)
	$v2 = (Get-Process | Format-Table -Property ProcessName)
	$v3 = (Get-Process | Format-Table -GroupBy ProcessName)
	$v4 = (Get-Process | Format-Table -View StartTime)

	$v12 = (Get-Process | ft *)
	$v22 = (Get-Process | ft -Property ProcessName)
	$v32 = (Get-Process | ft -GroupBy ProcessName)
	$v42 = (Get-Process | ft -View StartTime)

	{ $v1 } | Should Not Throw
	{ $v2 } | Should Not Throw
	{ $v3 } | Should Not Throw
	{ $v4 } | Should Not Throw

	{ $v12 } | Should Not Throw
	{ $v22 } | Should Not Throw
	{ $v32 } | Should Not Throw
	{ $v42 } | Should Not Throw
    }
}


Describe "Format-Table DRT Unit Tests" -Tags DRT{
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
	
	It "Format-Table with arrayList should work"{
		$al = (0..255)
		$info = @{}
		$info.arrayList = $al
		$result = $info|Format-Table|Out-String
		$result | Should Match "arrayList                      {0, 1, 2, 3...}"
	}
	
	It "Format-Table with Negative Count should work"{
		$FormatEnumerationLimit = -1
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1, 2}"
	}
	
	It "Format-Table with Zero Count should work -pending on issue#888" -pending{
		$FormatEnumerationLimit = 0
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {...}"
	}
	
	It "Format-Table with Less Count should work"{
		$FormatEnumerationLimit = 1
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1...}"
	}
	
	It "Format-Table with More Count should work"{
		$FormatEnumerationLimit = 10
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1, 2}"
	}
	
	It "Format-Table with Equal Count should work"{
		$FormatEnumerationLimit = 2
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1, 2}"
	}
	
	It "Format-Table with Bogus Count should throw Exception -pending on issue#888" -pending{
		$FormatEnumerationLimit = "abc"
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1, 2}"
	}
	
	It "Format-Table with Var Deleted should throw Exception -pending on issue#888" -pending{
		$FormatEnumerationLimit = 2
		Remove-Variable FormatEnumerationLimit
		$result = Format-Table -inputobject @{'test'= 1, 2}
		$resultStr = $result|Out-String
		$resultStr | Should Match "test                           {1, 2}"
	}
	
	It "Format-Table with ExposeBug927826 should work"{
		$info = @{}
		$info.name = "1\n2"
		$result = $info|Format-Table|Out-String
		$result | Should Match "name                           1"
		$result | Should Match "2"
	}
	
	It "Format-Table with ExposeBug920454 should work"{
		$IP1 = [System.Net.IPAddress]::Parse("1.1.1.1")
		$IP2 = [System.Net.IPAddress]::Parse("4fde:0000:0000:0002:0022:f376:255.59.171.63")
		$IPs = New-Object System.Collections.ArrayList
		$IPs.Add($IP1)
		$IPs.Add($IP2)
		$info = @{}
		$info.test = $IPs
		$result = $info|Format-Table|Out-String
		$result | Should Match "test                           {1.1.1.1, 4fde::2:22:f376:ff3b:ab3f}"
	}
	
	It "Format-Table with Autosize should work"{
		$p1 = @{'name'='Bob';'size'=1234;'booleanValue'=$true;}
		$p2 = @{'name'='Jim';'size'=5678;'booleanValue'=$false;}
		$IP1 = New-Object –TypeName PSObject –Property $p1
		$IP2 = New-Object –TypeName PSObject –Property $p2
		$IPs = New-Object System.Collections.ArrayList
		$IPs.Add($IP1)
		$IPs.Add($IP2)
		$result = $IPs|Format-Table -Autosize|Out-String
		if($result.Contains("size name booleanValue"))
		{
			$result | Should Match "---- ---- ------------"
			$result | Should Match "1234 Bob          True"
			$result | Should Match "5678 Jim         False"
		}
		
		if($result.Contains("booleanValue size name"))
		{
			$result | Should Match "------------ ---- ----"
			$result | Should Match "True 1234 Bob"
			$result | Should Match "False 5678 Jim"
		}
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
	
	It "Format-Table with single line string for End-To-End should work -pending with issue #900" -pending{
		$p = "single line string"
		$result = $p|Format-Table -Property "foo","bar"|Out-String
		$result | Should BeNullOrEmpty
	}
	
	It "Format-Table with multiple line string for End-To-End should work -pending with issue #900" -pending{
		$p = "Line1\nLine2"
		$result = $p|Format-Table -Property "foo","bar"|Out-String
		$result | Should BeNullOrEmpty
	}
	
	It "Format-Table with string sequence for End-To-End should work -pending with issue #900" -pending{
		$p = "Line1","Line2"
		$result = $p|Format-Table -Property "foo","bar"|Out-String
		$result | Should BeNullOrEmpty
	}
	
	It "Format-Table with string sequence for End-To-End should work -pending with issue #900" -pending{
		$p = "Line1","Line2"
		$result = $p|Format-Table -Property "foo","bar"|Out-String
		$result | Should BeNullOrEmpty
	}
	
	It "Format-Table with complex object for End-To-End should work"{
		Add-Type -TypeDefinition "public enum MyDayOfWeek{Sun,Mon,Tue,Wed,Thr,Fri,Sat}"
		$eto = New-Object MyDayOfWeek
		$info = @{}
		$info.intArray = 1,2,3,4
		$info.arrayList = "string1","string2"
		$info.enumerable = [MyDayOfWeek]$eto
		$info.enumerableTestObject = $eto
		$result = $info|Format-Table|Out-String
		$result | Should Match "intArray                       {1, 2, 3, 4}"
		$result | Should Match "arrayList                      {string1, string2}"
		$result | Should Match "enumerable                     Sun"
		$result | Should Match "enumerableTestObject           Sun"
	}
	
	It "Format-Table with Expand Enumerable should work"{
		$obj1 = "x 0","y 0"
		$obj2 = "x 1","y 1"
		$objs = New-Object System.Collections.ArrayList
		$objs.Add($obj1)
		$objs.Add($obj2)
		$mo = @{}
		$mo.name = "this is name"
		$mo.sub = $objs
		$result1 = $mo|Format-Table -Expand CoreOnly|Out-String
		if(!($result1.Contains("{name, sub}") -or $result1.Contains("{sub, name}")))
		{
			Throw "should contains name and sub for Expand CoreOnly, Acutal:"+$result1
		}
		$result1 | Should Match "this is name"
		
		$result2 = $mo|Format-Table -Expand EnumOnly|Out-String
		$result2 | Should Match "name                           this is name"
		$result2 | Should Match "sub                            {x 0 y 0, x 1 y 1}"
		
		$result3 = $mo|Format-Table -Expand Both|Out-String
		$result3 | Should Match "The following object supports IEnumerable:"
		if(!($result3.Contains("{name, sub}") -or $result3.Contains("{sub, name}")))
		{
			Throw "should contains name and sub for Expand Both, Acutal:"+$result3
		}
		$result3 | Should Match "this is name"
		$result3 | Should Match "The IEnumerable contains the following 2 objects:"
		$result3 | Should Match "name                           this is name"
		$result3 | Should Match "sub                            {x 0 y 0, x 1 y 1}"
	}
}
