# get a random string of characters a-z and A-Z
function Get-RandomString
{
    param ( [int]$length = 8 )
    $chars = .{ ([int][char]'a')..([int][char]'z');([int][char]'A')..([int][char]'Z') }
    ([char[]]($chars | get-random -count $length)) -join ""
}

# get a random string which is not the name of an existing provider
function Get-NonExistantProviderName
{
   param ( [int]$length = 8 )
   do {
       $providerName = get-randomstring -length $length
   } until ( (get-psprovider -PSProvider $providername -erroraction silentlycontinue) -eq $null )
   $providerName
}

# get a random string which is not the name of an existing drive
function Get-NonExistantDriveName
{
    param ( [int]$length = 8 )
    do {
        $driveName = Get-RandomString -length $length
    } until ( (get-psdrive $driveName -erroraction silentlycontinue) -eq $null )
    $drivename
}

# get a random string which is not the name of an existing function
function Get-NonExistantFunctionName
{
    param ( [int]$length = 8 )
    do {
        $functionName = Get-RandomString -length $length
    } until ( (test-path function:$functionName) -eq $false )
    $functionName
}

Describe "Clear-Content cmdlet tests" -Tags "CI" {
  BeforeAll {
    $file1 = "file1.txt"
    $file2 = "file2.txt"
    $file3 = "file3.txt"
    $content1 = "This is content"
    $content2 = "This is content for alternate stream tests"
    Setup -File "$file1"
    Setup -File "$file2" -Content $content1
    Setup -File "$file3" -Content $content2
    $streamContent = "content for alternate stream"
    $streamName = "altStream1"
  }

  Context "Clear-Content should actually clear content" {
    It "should clear-Content of testdrive:\$file1" {
      set-content -path testdrive:\$file1 -value "ExpectedContent" -passthru | Should be "ExpectedContent"
      clear-content -Path testdrive:\$file1
    }

    It "shouldn't get any content from testdrive:\$file1" {
      $result = get-content -path testdrive:\$file1
      $result | Should BeExactly $null
    }

    # we could suppress the WhatIf output here if we use the testhost, but it's not necessary
    It "The filesystem provider supports should process" -skip:(!$IsWindows) {
      clear-content TESTDRIVE:\$file2 -WhatIf
      "TESTDRIVE:\$file2" | should contain "This is content"
    }

    It "The filesystem provider should support ShouldProcess (reference ProviderSupportsShouldProcess member)" {
      $cci = ((get-command clear-content).ImplementingType)::new()
      $cci.SupportsShouldProcess | should be $true
    }

    It "Alternate streams should be cleared with clear-content" -skip:(!$IsWindows) {
      # make sure that the content is correct
      # this is here rather than BeforeAll because only windows can write to an alternate stream
      set-content -path "TESTDRIVE:/$file3" -stream $streamName -value $streamContent
      get-content -path "TESTDRIVE:/$file3" | Should be $content2
      get-content -Path "TESTDRIVE:/$file3" -stream $streamName | should be $streamContent
      clear-content -PATH "TESTDRIVE:/$file3" -stream $streamName
      get-content -Path "TESTDRIVE:/$file3" | should be $content2
      get-content -Path "TESTDRIVE:/$file3" -stream $streamName | should BeNullOrEmpty
    }

    It "the '-Stream' dynamic parameter is visible to get-command in the filesystem" {
      try {
        push-location TESTDRIVE:
        (get-command clear-content -stream foo).parameters.keys -eq "stream" | should be "stream"
      }
      finally {
        pop-location
      }
    }

    It "the '-stream' dynamic parameter should not be visible to get-command in the function provider" {
      try {
        push-location function:
        get-command clear-content -stream $streamName
        throw "ExpectedExceptionNotDelivered"
      }
      catch {
        $_.FullyQualifiedErrorId | should be "NamedParameterNotFound,Microsoft.PowerShell.Commands.GetCommandCommand"
      }
      finally {
        pop-location
      }
    }
  }

  Context "Proper errors should be delivered when bad locations are specified" {
    It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$null" {
      try {
        clear-content -path $null -ea stop
        throw "expected exception was not delivered"
      }
      catch {
        $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ClearContentCommand"
      }
    }
    #[BugId(BugDatabase.WindowsOutOfBandReleases, 903880)]
    It "should throw `"Cannot bind argument to parameter 'Path'`" when -Path is `$()" {
      try {
        clear-content -path $() -ea stop
        throw "expected exception was not delivered"
      }
      catch {
        $_.FullyQualifiedErrorId | should be "ParameterArgumentValidationErrorNullNotAllowed,Microsoft.PowerShell.Commands.ClearContentCommand"
      }
    }
    #[DRT][BugId(BugDatabase.WindowsOutOfBandReleases, 906022)]
    It "should throw 'PSNotSupportedException' when you clear-content to an unsupported provider" {
      $functionName = Get-NonExistantFunctionName
      $null = new-item function:$functionName -Value { 1 }
      try {
        clear-content -path function:$functionName -ea Stop
        throw "Expected exception was not thrown"
      }
      catch {
        $_.FullyQualifiedErrorId | should be "NotSupported,Microsoft.PowerShell.Commands.ClearContentCommand"
      }
    }
    It "should throw FileNotFound error when referencing a non-existant file" {
      try {
        $badFile = "TESTDRIVE:/badfilename.txt"
        clear-content -path $badFile -ea Stop
        throw "ExpectedExceptionNotDelivered"
      }
      catch {
        $_.FullyQualifiedErrorId | should be "PathNotFound,Microsoft.PowerShell.Commands.ClearContentCommand"
      }
    }
    It "should throw DriveNotFound error when referencing a non-existant drive" {
       try {
         $badDrive = "{0}:/file.txt" -f (Get-NonExistantDriveName)
         clear-content -path $badDrive -ea Stop
         thow "ExpectedExceptionNotDelivered"
       }
       catch {
         $_.FullyQualifiedErrorId | Should be "DriveNotFound,Microsoft.PowerShell.Commands.ClearContentCommand"
       }
    }
    # we'll use a provider qualified path to produce this error
    It "should throw ProviderNotFound error when referencing a non-existant provider" {
       try {
         $badProviderPath = "{0}::C:/file.txt" -f (Get-NonExistantProviderName)
         clear-content -path $badProviderPath -ea Stop
         thow "ExpectedExceptionNotDelivered"
       }
       catch {
         $_.FullyQualifiedErrorId | Should be "ProviderNotFound,Microsoft.PowerShell.Commands.ClearContentCommand"
       }
    }
  }
}
