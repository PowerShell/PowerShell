#Pester Testing Test Guide

## Who this is for

Cmdlet behavior is validated using the Pester testing framework. The purpose of this document is to create a single standard to maximize unit test coverage while minimizing confusion on expectations. What follows is a working document intended to guide those writing Pester unit tests for PSL.   

Unit testing is done not only to validate that the block of code works as expected, but also to assist the developer to know precisely where in the code to look; in some cases, seeing the source code may inspire better unit tests. In many cases, a unit test *is* the only documented specification.  Fortunately, the MSDN is a great source of information about Cmdlets. 

Test suites need to be created and many cmdlets added and unit-tested. The following list is to be used to guide the thought process of the developer in writing a suite in minimal time, while enhancing quality.
 
Test suites should proceed as functional and system tests of the cmdlets, and the code treated as a black box for the purpose of test suite design.


### Use of Mocks
It is often necessary for the code to interact with the system or other components.  When possible, use Mock objects to facilitate this in order to minimize external dependencies.  Note: creating a Mock in PSL causes PowerShell to look at the Mock, never actually hitting any C# code. Cmdlets cannot be tested using Mocks.  

### Aliases
 Each cmdlet with an alias must be tested with all of its aliases at least once to verify the code path calls the original function.
 
## Testing Standards
### Readability
Every effort should be made to maximize readability of code.  Code is written for the developer in the future to debug- not for the developer writing the code. 

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

So the second section of code should instead be used. The same style should be followed for assignments of variables on consecutive lines:

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

2) Use readable and meaningful variable name when assigning variables. 

3) Do not make large functions. Tests should be simple: define -> manipulate -> assert

4) Do not use tabs.  Tabs are rendered differently depending upon the machine.  This greatly affects readability.

5) Remove the first 3 auto-generated lines of each .Tests.ps1 file. This is created automatically by Pester and is unnecessary.  Each .Test.ps1 file should begin with a Describe block. 

6) Discard the auto-generated function file that is generated in tandem with the .Tests.ps1 file 

7) Name the test file "Test-<cmdlet name > when you create a new test fixture.

8) Each test describes a behavior- use the word "Should" at the beginning of each test description- so it reads "It 'Should..."
	
### Basic Unit Tests

The following table should suffice to inspire in the developer sufficient content to create a suite of tests.

test # | test name | entry criteria/setup | exit criteria/assertion
-------|-----------|----------------------|------------------------
01 | Should be able to be called | without params (if applicable) | no throw
02 | Should be able to be called | minimal required params | no throw, expected output
03 | Should be able to use the X alias | minimal required params | no throw, expected output
04 | Should return the proper data type | required params | no throw, proper data type
05 | Should be able to accept piped input | piped input | expected output
06 | Should be able to call using the X parameter | use X parameter | no throw, expected output
07 | Should be able to call using the Y parameter | use Y parameter | no throw, expected output
08 | Should be able to call using the Z parameter | use Z parameter | no throw, expected output
09 | Should throw under condition X | create condition X | Throw error x
10 | Should throw under condition Y | create condition Y | Throw error y
11 | Should throw under condition Z | create condition Z | Throw error z

These are the **basic** unit tests required to verify the functionality of any Cmdlet.  If the above questions cannot be answered for each Cmdlet, then they cannot be verified to work.

Look at the existing suites of pester tests located within `monad-linux/src/pester-test/` and use that as inspiration.


##Running Pester Tests
Pester tests may be run from outside of PowerShell via the command line.  Build PowerShell and Pester using (assuming you're in the build folder) `./build.sh make ../src/pester-test/<filename>` or `./build.sh make pester-tests`
















