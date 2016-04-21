Pester Testing Test Guide
=========================

Running Pester Tests
--------------------

Go to the top level of the PowerShell repository and run: `Start-PSPester`
inside a self-hosted copy of PowerShell.

You can use `Start-PSPester -Tests SomeTestSuite*` to limit the tests run.

Testing new `powershell` processes
----------------------------------

Any launch of a new `powershell` process must include `-noprofile` so that
modified user and system profiles do not causes tests to fail. You also must
take care to call the development copy of PowerShell, which is *not* the first
one on the path.

Example:

```powershell
    $powershell = Join-Path -Path $PsHome -ChildPath "powershell"
    & $powershell -noprofile -command "ExampleCommand" | Should Be "ExampleOutput"
```

Portability
-----------

Some tests simply must be tied to certain platforms. Use Pester's
`-Skip` directive on an `It` statement to do this. For instance to run
the test only on Windows:

```powershell
It "Should do something on Windows" -Skip:($IsLinux -Or $IsOSX) { ... }
```

Or only on Linux and OS X:

```powershell
It "Should do something on Linux" -Skip:$IsWindows { ... }
```

Pending
-------

When writing a test that should pass, but does not, please do not skip or delete
the test, but use `It "Should Pass" -Pending` to mark the test as pending, and
file an issue on GitHub.

Who this is for
---------------

Cmdlet behavior is validated using the Pester testing framework. The
purpose of this document is to create a single standard to maximize
unit test coverage while minimizing confusion on expectations. What
follows is a working document intended to guide those writing Pester
unit tests for PowerShell.

Unit testing is done not only to validate that the block of code works
as expected, but also to assist the developer to know precisely where
in the code to look; in some cases, seeing the source code may inspire
better unit tests. In many cases, a unit test *is* the only documented
specification. Fortunately, the MSDN is a great source of information
about Cmdlets.

Test suites need to be created and many cmdlets added and unit-tested.
The following list is to be used to guide the thought process of the
developer in writing a suite in minimal time, while enhancing quality.

Test suites should proceed as functional and system tests of the
cmdlets, and the code treated as a black box for the purpose of test
suite design.

Testing Standards
-----------------

### Readability

Every effort should be made to maximize readability of code. Code is
written for the developer in the future to debug- not for the
developer writing the code.

1) When assertions are on consecutive lines, the pipes should line up:

```sh
MyFirstCondition | Should Be 0
MySecondCondition | Should Be 1
```

This is less readable than:

```sh
MyFirstCondition  | Should Be 0
MySecondCondition | Should Be 1
```

So the second section of code should instead be used. The same style
should be followed for assignments of variables on consecutive lines:

```sh
$var1 = <expression 1>
$variable2 = <expression 2>
$var3 = <expression 3>
$typeCollection1 = <expression 4>
$object1 = <expression>
... etc
```

is much less readable than

```sh
$var1            = <expression 1>
$variable2       = <expression 2>
$var3            = <expression 3>
$typeCollection1 = <expression 4>
$object1         = <expression 5>
... etc
```

So all assignment statements must be aligned.

Other style standards are no less important to readability of the code:

- Use readable and meaningful variable name when assigning variables.

- Do not make large functions. Tests should be simple: define ->
  manipulate -> assert

- Do not use tabs. Tabs are rendered differently depending upon the
  machine. This greatly affects readability.

- Remove the first 3 auto-generated lines of each .Tests.ps1 file.
  This is created automatically by Pester and is unnecessary. Each
  .Test.ps1 file should begin with a Describe block.

- Discard the auto-generated function file that is generated in tandem
  with the .Tests.ps1 file

- Name the test file "Test-<cmdlet name > when you create a new test
  fixture.

- Each test describes a behavior- use the word "Should" at the
  beginning of each test description- so it reads "It 'Should..."

