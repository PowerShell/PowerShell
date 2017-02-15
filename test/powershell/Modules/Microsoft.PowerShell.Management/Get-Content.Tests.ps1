Describe "Get-Content" -Tags "CI" {
  $testString = "This is a test content for a file"
  $nl         = [Environment]::NewLine
  $firstline  = "Here's a first line "
  $secondline = " here's a second line"
  $thirdline  = "more text"
  $fourthline = "just to make sure"
  $fifthline  = "there's plenty to work with"
  $testString2 = $firstline + $nl + $secondline + $nl + $thirdline + $nl + $fourthline + $nl + $fifthline
  $testPath   = Join-Path -Path $TestDrive -ChildPath testfile1
  $testPath2  = Join-Path -Path $TestDrive -ChildPath testfile2

  BeforeEach {
    New-Item -Path $testPath -ItemType file -Force -Value $testString
    New-Item -Path $testPath2 -ItemType file -Force -Value $testString2
  }
  AfterEach {
    Remove-Item -Path $testPath -Force
    Remove-Item -Path $testPath2 -Force
  }
  It "Should throw an error on a directory  " {
        try {
            Get-Content . -ErrorAction Stop
            throw "No Exception!"
        }
        catch {
            $_.FullyQualifiedErrorId | should be "GetContentReaderUnauthorizedAccessError,Microsoft.PowerShell.Commands.GetContentCommand"
        }
  }
  It "Should return an Object when listing only a single line and the correct information from a file" {
        $content = (Get-Content -Path $testPath)
        $content | Should Be $testString
        $content.Count | Should Be 1
        $content | Should BeOfType "System.String"
  }
  It "Should deliver an array object when listing a file with multiple lines and the correct information from a file" {
        $content = (Get-Content -Path $testPath2)
        @(Compare-Object $content $testString2.Split($nl) -SyncWindow 0).Length | Should Be 0
        ,$content | Should BeOfType "System.Array"
  }
  It "Should be able to return a specific line from a file" {
	  (Get-Content -Path $testPath2)[1] | Should be $secondline
  }
  It "Should be able to specify the number of lines to get the content of using the TotalCount switch" {
	  $returnArray    = (Get-Content -Path $testPath2 -TotalCount 2)
  	$returnArray[0] | Should Be $firstline
	  $returnArray[1] | Should Be $secondline
  }
  It "Should be able to specify the number of lines to get the content of using the Head switch" {
	  $returnArray    = (Get-Content -Path $testPath2 -Head 2)
    $returnArray[0] | Should Be $firstline
	  $returnArray[1] | Should Be $secondline
  }
  It "Should be able to specify the number of lines to get the content of using the First switch" {
	  $returnArray    = (Get-Content -Path $testPath2 -First 2)
  	$returnArray[0] | Should Be $firstline
	  $returnArray[1] | Should Be $secondline
  }
  It "Should return the last line of a file using the Tail switch" {
	  Get-Content -Path $testPath -Tail 1 | Should Be $testString
  }
  It "Should return the last lines of a file using the Last alias" {
	  Get-Content -Path $testPath2 -Last 1 | Should Be $fifthline
  }
  It "Should be able to get content within a different drive" {
	  pushd env:
	  $expectedoutput = [Environment]::GetEnvironmentVariable("PATH");
    { Get-Content PATH } | Should Not Throw
	  Get-Content PATH     | Should Be $expectedoutput
  	popd
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
  It "should throw 'PSNotSupportedException' when you set-content to an unsupported provider" -Skip:($IsLinux -Or $IsOSX) {
     {get-content -path HKLM:\\software\\microsoft -ea stop} | Should Throw "IContentCmdletProvider interface is not implemented"
  }
  It "should Get-Content with a variety of -Tail and -ReadCount values" {#[DRT]
    set-content -path $testPath "Hello,World","Hello2,World2","Hello3,World3","Hello4,World4"
    $result=get-content -path $testPath -readcount:-1 -tail 5
    $result.Length | Should Be 4
    $expected = "Hello,World","Hello2,World2","Hello3,World3","Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -readcount 0 -tail 3
    $result.Length    | Should Be 3
    $expected = "Hello2,World2","Hello3,World3","Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -readcount 1 -tail 3
    $result.Length    | Should Be 3
    $expected = "Hello2,World2","Hello3,World3","Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -readcount 99999 -tail 3
    $result.Length    | Should Be 3
    $expected = "Hello2,World2","Hello3,World3","Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -readcount 2 -tail 3
    $result.Length    | Should Be 2
    $expected = "Hello2,World2","Hello3,World3"
    $expected = $expected,"Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -readcount 2 -tail 2
    $result.Length    | Should Be 2
    $expected = "Hello3,World3","Hello4,World4"
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -delimiter "," -tail 2
    $result.Length    | Should Be 2
    if ($IsWindows) {$expected = "World3`r`nHello4,","World4`r`n"
    } else {$expected = "World3`nHello4,","World4`n"}
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -delimiter "o" -tail 3
    $result.Length    | Should Be 3
    if ($IsWindows) {$expected = "rld3`r`nHello","4,Wo","rld4"
    } else {$expected = "rld3`nHello","4,Wo","rld4"}
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i].Trim()  | Should BeExactly $expected[$i]}
    $result=get-content -path $testPath -encoding:Byte -tail 10
    $result.Length    | Should Be 10
    if ($IsWindows) {
      $expected = "52","44","87","111","114","108","100","52","13","10"
      for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
    }
  }
  #[BugId(BugDatabase.WindowsOutOfBandReleases, 905829)]
  It "should get-content that matches the input string"{
    set-content $testPath "Hello,llllWorlld","Hello2,llllWorlld2"
    $result=get-content $testPath -delimiter "ll"
    $result.Length    | Should Be 9
    if ($IsWindows) {$expected = "Hell","o,ll","ll","Worll","d`r`nHell","o2,ll","ll","Worll","d2`r`n"
    } else {$expected = "Hell","o,ll","ll","Worll","d`nHell","o2,ll","ll","Worll","d2`n"}
    for ($i = 0; $i -lt $result.Length ; $i++) { $result[$i]  | Should BeExactly $expected[$i]}
  }
}
