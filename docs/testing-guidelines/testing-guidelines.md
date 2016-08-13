
# Testing Guidelines

Testing is a critical and required part of the PowerShell project.

The Microsoft PowerShell team created nearly 100,000 tests over the last 12 years which we run as part of the release process for Windows PowerShell.
Having all of those tests available for the initial release of PowerShell was not feasible, and we have targeted those tests which
we believe will provide us the ability to catch regressions in the areas which have had the largest changes for PowerShell. 
It is our intent to continue to release more and more of our tests until we have the coverage we need.

For creating new tests, please review the 
[documents](https://github.com/PowerShell/PowerShell/tree/master/docs/testing-guidelines) on how to
create tests for PowerShell.

## CI System

We use [AppVeyor](http://www.appveyor.com/) as a continuous integration (CI) system for Windows 
and [Travis CI](http://www.travis-ci.com) for non-Windows platforms.

### AppVeyor

In the `README.md` at the top of the repo, you can see AppVeyor badge.
It indicates the last build status of `master` branch.
Hopefully, it's green:

![AppVeyor-Badge-Green.png](Images/AppVeyor-Badge-Green.png)

This badge is **clickable**; you can open corresponding build page with logs, artifacts, and tests results.
From there you can easily navigate to the build history.

### Travis CI

Travis CI works similarly to AppVeyor. 
For Travis CI there will be multiple badges.
The badges indicate the last build status of `master` branch for different platforms.
Hopefully, it's green:

![Travis-CI-Badge-Green.png](Images/Travis-CI-Badge-Green.png)

This badge is **clickable**; you can open corresponding build page with logs, artifacts, and tests results.
From there you can easily navigate to the build history.

### Getting CI Results

CI System builds (AppVeyor and Travis CI) and runs tests on every pull request and provides quick feedback about it.

![AppVeyor-Github](Images/AppVeyor-Github.png)

These green check boxes and red crosses are **clickable** as well. 
They will bring you to the corresponding page with details. 

## Test Frameworks
### Pester
Our script-based test framework is [Pester](https://github.com/Pester/Pester). 
This is the framework which we are using internally at Microsoft for new script-based tests, 
and a large number of the tests which are part of the PowerShell project have been migrated from that test base. 
Pester tests can be used to test most of PowerShell behavior (even some API operations can easily be tested in Pester).

Substantial changes were required, to get Pester executing on non-Windows systems. 
These changes are not yet in the official Pester code base. 
Some features of Pester may not be available or may have incorrect behavior. 
Please make sure to create issues in [PowerShell/PowerShell](https://github.com/PowerShell/PowerShell/issues) (not Pester) for anything that you find.

### xUnit
For those tests which are not easily run via Pester, we have decided to use [xUnit](https://xunit.github.io/) as the test framework. 
Currently, we have a minuscule number of tests which are run by using xUnit.

## Running tests outside of CI
When working on new features or fixes, it is natural to want to run those tests locally before making a PR. 
Two helper functions are part of the build.psm1 module to help with that:
* `Start-PSPester` will execute all Pester tests which are run by the CI system
* `Start-PSxUnit` will execute the available xUnit tests run by the CI system
Our CI system runs these as well; there should be no difference between running these on your dev system, versus in CI.

When running tests in this way, be sure that you have started PowerShell with `-noprofile` as some tests will fail if the
environment is not the default or has any customization.

For example, to run all the Pester tests for CI (assuming you are at the root of the PowerShell repo):
```
Import-Module ./build.psm1
Start-PSPester
```
If you wish to run specific tests, that is possible as well:
```
Start-PSPester -Directory test/powershell/engine/Api
```
Or a specific Pester test file:
```
Start-PSPester -Directory test/powershell/engine/Api -Test XmlAdapter.Tests.Api
```

### What happens after your PR?
When your PR has successfully passed the CI test gates, your changes will be used to create PowerShell binaries which can be run
in Microsoft's internal test frameworks. 
The tests that you created for your change and the library of historical tests will be run to determine if any regressions are present. 
If these tests find regressions, you'll be notified that your PR is not ready, and provided with enough information to investigate why the failure happened.



## Test Layout
We have taken a functional approach to the layout of our Pester tests. 
You should place new tests in their appropriate location. 
If you are making a fix to a cmdlet in a module, the test belongs in the module directory.
If you are unsure, you can make it part of your PR, or create an issue. 
The current layout of tests is:
* test/powershell/engine
* test/powershell/engine/Api
* test/powershell/engine/Basic
* test/powershell/engine/ETS
* test/powershell/engine/Help
* test/powershell/engine/Logging
* test/powershell/engine/Module
* test/powershell/engine/ParameterBinding
* test/powershell/engine/Runspace
* test/powershell/engine/Logging/MessageAnalyzer
* test/powershell/Host
* test/powershell/Host/ConsoleHost
* test/powershell/Host/TabCompletion
* test/powershell/Language
* test/powershell/Modules
* test/powershell/Provider
* test/powershell/Scripting
* test/powershell/Scripting/Debugging
* test/powershell/Scripting/NativeExecution
* test/powershell/SDK
* test/powershell/Security
* test/powershell/Language/Classes
* test/powershell/Language/Interop
* test/powershell/Language/Operators
* test/powershell/Language/Parser
* test/powershell/Language/Interop/DotNet
* test/powershell/Modules/Microsoft.PowerShell.Archive
* test/powershell/Modules/Microsoft.PowerShell.Core
* test/powershell/Modules/Microsoft.PowerShell.Diagnostics
* test/powershell/Modules/Microsoft.PowerShell.Management
* test/powershell/Modules/Microsoft.PowerShell.Security
* test/powershell/Modules/Microsoft.PowerShell.Utility
* test/powershell/Modules/PSReadLine

