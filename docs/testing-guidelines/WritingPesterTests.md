### Writing Pester Tests
Note that this document does not replace the documents found in the [Pester](https://github.com/pester/pester "Pester") project. This is just
some quick tips and suggestions for creating Pester tests for this project. The Pester community is vibrant and active, if you have questions
about Pester or creating tests, the [Pester Wiki](https://github.com/pester/pester/wiki) has a lot of great information.

When creating tests, keep the following in mind:
* Tests should not be overly complicated and test too many things
	* boil down your tests to their essence, test only what you need
* Tests should be as simple as they can
* Tests should generally not rely on any other test
	
Examples:
Here's the simplest of tests

```
Describe "A variable can be assigned and retrieved" {
    It "Create a variable and make sure it's value is correct" {
       $a = 1
       $a | Should be 1
   }
} 
```

If you need to do type checking, that can be done as well

```
Describe "One is really one" {
    It "Compare 1 to 1" {
       $a = 1
       $a | Should be 1
    }
    It "1 is really an int" {
       $i = 1
       $i.GetType() | Should Be "int"
    }
} 
```
alternatively, you could do the following:
```
Describe "One is really one" {
    It "Compare 1 to 1" {
       $a = 1
       $a | Should be 1
    }
    It "1 is really an int" {
       $i = 1
       $i.GetType() | Should Be ([System.Int32])
    }
} 
```

If you are checking for proper errors, do that in a `try/catch`, and then check `FullyQualifiedErrorId`. Checking against `FullyQualifiedErrorId` is recommended because it does not change based on culture as an error message might.
```
...
it "Get-Item on a nonexisting file should have error PathNotFound" {
    try
    {
        get-item "ThisFileCannotPossiblyExist" -ErrorAction Stop
        throw "No Exception!"
    }
    catch
    {
        $_.FullyQualifiedErrorId | should be "PathNotFound,Microsoft.PowerShell.Commands.GetItemCommand"
    }
}
```

Note that if get-item were to succeed, a different FullyQualifiedErrorId would be thrown and the test will fail because the FQErrorId is wrong. This is the suggested path because Pester wants to check the error message, which will likely not work here because of localized builds, but the FullyQualifiedErrorId is constant regardless of the locale.

### Describe/Context/It
For creation of PowerShell tests, the Describe block is the level of granularity suggested and one of three tags should be used: "CI", "Feature", or "Scenario". If the tag is not provided, tests in that describe block will be run any time tests are executed.

#### Describe
Creates a logical group of tests. All Mocks and TestDrive contents defined within a Describe block are scoped to that Describe; they will no longer be present when the Describe block exits. A `Describe block may contain any number of Context and It blocks.

#### Context
Provides logical grouping of It blocks within a single Describe block. Any Mocks defined inside a Context are removed at the end of the Context scope, as are any files or folders added to the TestDrive during the Context block's execution. Any BeforeEach or AfterEach blocks defined inside a Context also only apply to tests within that Context .

#### It
The  It  block is intended to be used inside of a  Describe  or  Context  Block. If you are familiar with the AAA pattern (Arrange-Act-Assert), the body of the  It  block is the appropriate location for an assert. The convention is to assert a single expectation for each  It  block. The code inside of the  It  block should throw a terminating error if the expectation of the test is not met and thus cause the test to fail. The name of the  It  block should expressively state the expectation of the test.

### Selected Features

#### Test Drive
A PSDrive is available for file activity during a tests and this drive is limited to the scope of a single Describe block. The contents of the drive are cleared when a context block is exited.
A test may need to work with file operations and validate certain types of file activities. It is usually desirable not to perform file activity tests that will produce side effects outside of an individual test. Pester creates a PSDrive inside the user's temporary drive that is accessible via a names PSDrive TestDrive:. Pester will remove this drive after the test completes. You may use this drive to isolate the file operations of your test to a temporary store.

The following example illustrates the feature:

```
function Add-Footer($path, $footer) {
    Add-Content $path -Value $footer
}

Describe "Add-Footer" {
    $testPath="TestDrive:\test.txt"
    Set-Content $testPath -value "my test text."
    Add-Footer $testPath "-Footer"
    $result = Get-Content $testPath

    It "adds a footer" {
        (-join $result) | Should Be("my test text.-Footer")
    }
}
```

When this test completes, the contents of the TestDrive PSDrive will be removed.

#### Parameter Generation
```

$testCases = @(
    @{ a = 0; b = 1; ExpectedResult = 1 }
    @{ a = 1; b = 0; ExpectedResult = 1 }
    @{ a = 1; b = 1; ExpectedResult = 0 }
    @{ a = 0; b = 0; ExpectedResult = 0 }
    )

Describe "A test" {
	It "<a> -xor <b> should be <expectedresult>" -testcase $testcases {
        param ($a, $b, $ExpectedResult)
		$a -xor $b | Should be $ExpectedResult
	}
}
```

You can also construct loops and pass values as parameters, including the expected value, but Pester does this for you.

#### Mocking
Mocks the behavior of an existing command with an alternate implementation. This creates new behavior for any existing command within the scope of a Describe or Context block. The function allows you to specify a script block that will become the command's new behavior.
The following example illustrates simple use:
```
Context "Get-Random is not random" {
        Mock Get-Random { return 3 }
        It "Get-Random returns 3" {
            Get-Random | Should be 3
        }
    }
```
More information may be found on the [wiki](https://github.com/pester/Pester/wiki/Mock)
### Free Code in a Describe block
Code execution in Pester can be very subtle and can cause issues when executing test code. The execution of code which lays outside of the usual code blocks may not happen as you expect. Consider the following:
```
Describe it {
    Write-Host -For DarkRed "Before Context"
    Context "subsection" {
        Write-Host -for DarkRed "Before BeforeAll"
        BeforeAll { write-host -for Blue "In Context BeforeAll" }
        Write-Host -for DarkRed "After BeforeAll"

        Write-Host -for DarkRed "Before AfterAll"
        AfterAll { Write-Host -for Blue "In Context AfterAll" }
        Write-Host -for DarkRed "After AfterAll"

        BeforeEach { Write-Host -for Blue "In BeforeEach" }
        AfterEach { Write-Host -for Blue "In AfterEach" }

        Write-Host -for DarkRed "Before It"
        It "should not be a surprise" {
            1 | should be 1
        }
        Write-Host -for DarkRed "After It"
    }
    Write-Host -for DarkRed "After Context"
    Write-Host -for DarkGreen "Before Describe BeforeAll" 
    BeforeAll { Write-Host -for DarkGreen "In Describe BeforeAll" }
    AfterAll { Write-Host -for DarkGreen "In Describe AfterAll" }
} 
```

Now, when run, you can see the execution schedule

```
PS# invoke-pester c:\temp\pester.demo.tests.ps1
Describing it
In Describe BeforeAll
Before Context
   Context subsection
In Context BeforeAll
Before BeforeAll
After BeforeAll
Before AfterAll
After AfterAll
Before It
In BeforeEach
    [+] should not be a surprise 79ms
In AfterEach
After It
In Context AfterAll
After Context
Before Describe BeforeAll
In Describe AfterAll
Tests completed in 79ms
Passed: 1 Failed: 0 Skipped: 0 Pending: 0 
```

The DESCRIBE BeforeAll block is executed before any other code even though it was at the bottom of the Describe block, so if state is set elsewhere in the describe BLOCK, that state will not be visible (as the code will not yet been run). Notice, too, that the BEFOREALL block in Context is executed before any other code in that block.
Generally, you should have code reside in one of the code block elements of `[Before|After][All|Each]`, especially if those block rely on state set by free code elsewhere in the block.



