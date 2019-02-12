# Overview
While we have done a fairly good job providing adequate coverage for the highest priority areas of PowerShell Core, we still have lots of gaps when we compare what is available in the OSS project vs what we have available as part of the Windows release test infrastructure.
This is to be expected as that infrastructure was built up over more than a decade.
It is not clear, however, how many of the current tests actually provide value, and how much duplication we have.
In any event, there is still a large difference in what we have in PowerShell Core and our older tests.

# Data Visibility
In both PowerShell Core or our proprietary tests we can't actually determine how and what cmdlets and engine functions (language/scripting/debugging/remoting) are being tested because we do not gather any usage telemetry on what our tests are doing during test execution.
We have no way via automation to determine how thoroughly we are testing our code except by inspecting the actual tests or looking at code coverage data (which isn't gathered often and only available on full PowerShell).
Further, this doesn't provide us data with regard to the test quality, only that we've covered a specific block of code.
The areas of greatest risk are those where we have no data at all, by collecting coverage data we can illuminate those code paths that are not being used and fill those gaps with new tests.

## Telemetry and Logging
While we have some telemetry feeds on Windows, we have _no_ telemetry capabilities on non-Windows platforms.
We should enable telemetry on PowerShell Core on all platforms and review the usage data as soon as possible.
This will provide us much needed visibility in how PowerShell Core is being used and can help us identify areas we should track more closely.
We already have infrastructure in place to allow us see how PowerShell Core is being used, by collecting telemetry from PowerShell Core, we can improve our confidence as we drive to production quality.

### Logging
The code which on Windows create ETW logging has been completely stubbed out on Linux/macOS.
We should take advantage of the native logging mechanisms on Linux/macOS and implement a logger similar to the ETW logger on Windows using Syslog (or equivalent).
We could use this data during test runs to identify test gaps.
Simply by capturing the cmdlets and their parameters which are invoked during test would illuminate the gaps we have in our current tests, and allow us to easily fill them.
It is not sufficient to support only one platform because we have many tests which determine at runtime whether or not it should run based on OS, so data from Windows will not be the same as that from Linux or MacOS.
We can also determine engine coverage gaps by measuring test of operators and other language elements.
Without that data, we are simply shooting in the dark.

## Code coverage
Even in our lab (STEX) environment we run tests to get code coverage data irregularly, and  PowerShell Core has _no_ tools to gather code coverage data.
We need to investigate possible solutions for code coverage on PowerShell Core.
There are a small number of solutions available:
* [OpenCover](https://github.com/OpenCover/opencover)
    * OpenCover is currently used by corefx to produce code coverage data, also visualization is available via [CoverAlls](https://coveralls.io/github/OpenCover/opencover).
    We should investigate `OpenCover` and determine it's feasibility for us.
    Unfortunately I haven't been able to find a solution for Linux
* DotCover
    * I have contacted `JetBrains` to see if they have any solutions which may be used with .NET Core.

If we can get code coverage on PowerShell Core on Windows, we would at least be able to have _some_ data to illuminate our test gaps.

Running code coverage more often on full PowerShell is something that we should consider, if it will help us close existing test coverage gaps, but the issue is _not_ test coverage for full PowerShell, but rather PowerShell Core where we have only a small percentage of tests in comparison.

## Daily Test Runs
We currently run only those tests which are tagged `CI` excluding the tag `SLOW` as part of our continuous integration systems.
This means roughly 1/3rd of our github tests are not being run on any regular schedule.
In order to provide us with higher confidence in our code, we should be running *ALL* of our tests on a regular basis.
However, running the tests is only the first step, we need an easy way to be notified of test failures, and to track progress of those runs over time.
Tracking this over time affords us the ability to see how our test count increases, implying an improvement in coverage.
It also provides us mechanism whereby we can see trends in instability.

### Pending and Skipped Tests
We currently have approximately 300 tests which are marked either as `skipped` or `pending`.
`Pending` tests represent those tests which should be run but are not currently being executed, usually because the underlying functionality is not present.
Over time, the number of `Pending` tests should drive to zero, and we should be tracking the list of tests which are marked `pending` and track those to be sure that they do not spend too long in this state.
`Skipped` tests due to platform applicability are certainly valid, and it is important that we track how many tests are skipped and for what reason.
A test which is skipped for all platforms should be considered carefully as to whether it should exist at all.
In either case, `pending` or `skipped` tests should be tracked over time so we can determine whether we are improving our ability to measure quality.

In the best case, the _total_ number of tests is the same across all platforms.
The count of Skipped/Pending tests would naturally be different as not all tests will be applicable to every platform, but if we can have consistency of test count across platforms, it will be easier to compare test results on one platform vs another.

## Remoting Considerations
Given that one of the targeted scenarios is to manage heterogeneous environments remotely, we need a test environment which encompasses the available platforms and protocols.
Our current test infrastructure does not have comprehensive support for remote testing except for loopback operations.
We need a matrix of protocols and platforms to ensure that we have adequate coverage to ensure quality.

In addition to loopback tests using both WSMan and SSH protocols, we should have the following _minimum_ matrix of client/server connections for both WSMan and SSH (where available) protocols.
* Windows Client->Nano Server
* Windows Client->Linux Server
* Linux Client -> Windows Server
* macOS Client -> Nano Client
* PowerShell Core Client -> Full PowerShell Server
* Full PowerShell Client -> PowerShell Core Server
* Downlevel Full PowerShell Client -> PowerShell Core Server

### Challenges
* On Windows, we have cmdlets to enable and disable remote access, those cmdlets do not exist on non-Windows platforms (they rely on configuration stored in the registry), nor do we support configuration for the SSH protocol.
We need to be sure that we can easily enable remoting for the non-Windows platforms and support both WSMan _and_ SSH configurations.
* Our current multi-machine tests do not test the connection code, they simply execute test code remotely and retrieve results and assume a good connection.
The infrastructure used for these tests is STEX which is not an open environment.
We will need to create automation to create and configure the test systems in the test matrix and then invoke tests on them.
It is not clear that our current CI systems can accommodate our needs here as neither AppVeyor or Travis can supply us with all of the OS images needed.
We may need to create our own heterogeneous environment in Azure, or look to other teams (MS Build Lab/Jenkins) for assistance.

We need to investigate whether there are solutions available, and if not, design/implement an environment to meet our needs.

# Reporting
Currently, we report against the simplest of KPI:
* is the CI build error free (which is part of the PR/Merge process - and reported on our landing page)

We are also collecting data for a daily build, but not yet report on the following KPI
* is the daily build error free (we are running this on AppVeyor, we still need to do this for Travis-CI)

There are a number of KPIs which we could report on:
* Code KPIs
    * What is the coverage (% blocks covered) of our `CI` tests
    * What is the coverage of all tests
    * How long is the CI system taking
        * build time
        * test time
    * What is the percentage of pull requests pass `CI` tests
    * What is the trend for `pending` tests
* Process KPIs
    * What is the time delta between a posted PR (with no test errors) and merge
    * How many revisions are needed before a PR is accepted
    * How many PRs are rejected
* Product KPIs
    * What is the usage of PowerShell Core on the various platforms
    * What are the common operations performed by PowerShell Core (cmdlet usage)
    * How many remote connections are created in a session

## Reporting Priorities
1. As a baseline, we should report code coverage on current Full PowerShell from the latest Windows release
2. We should report on code coverage as soon as possible.
This is the tool that will enable us to at least determine where we have _no_ data and illuminate test gaps.
3. We should track our CI execution time, which will allow us to keep an eye on our code submission workflow and how much our developers are waiting for builds

### Public Dashboard
Because we collect the test results as a build artifact, it is possible to collect and collate this data and provide a number of dash-board reports.
Since we are an OSS project, it makes sense that our reports should be also public.
A public dashboard provides evidence that we are not collecting PII, increasing trust in the project and shows that we are using data to drive product decisions.
We could easily create a web presence in Azure which would enable us to provide reporting on the current and historical state of our tests.
Now that we are running all of our tests on a nightly basis, we should be communicating the results of those test runs.
PowerBI could be used to create the visualizations, which would reduce the time and effort.

In order to achieve this we need to:
* Designate an Azure instance to run services and populate Azure tables:
    * Create tools to retrieve and transform git data
    * Create tools to retrieve and transform the test data
* Create PowerBI queries to visualize our KPIs

# Release Criteria
We must start defining the release criteria for a production ready release of PowerShell Core, as an initial proposal:
* No open issues for the release
* 80% code coverage of high use cmdlets (cmdlets used by 70% of users, as captured via telemetry)
* 90% code coverage of language elements (coverage error code paths may not be 100%)
* 60% code Coverage on Windows via Github tests
* 100% of our minimum remoting matrix tested
* Acceptance by 50% PowerShell MVPs (via Survey)
* Acceptance by Partners (via Survey)

A couple of assumptions have been made for this list:
1) we actually know the high use cmdlets - this implies we have telemetry to determine our cmdlet use
2) we have designated our Partners

# Microsoft Release Process
If PowerShell Core is targeted to be released as part of Windows Server or Nano, we will have additional requirements which must be met with regard to acceptance by the Windows org of the PowerShell Core package.
This represents additional work which needs planning and allocated resources.

## Replace STEX tests with PowerShell Core tests
Currently we have two distinct set of test artifacts, those used in PowerShell Core and our traditional lab (STEX) based tests.
We are creating new tests for PowerShell Core using the existing tests as guidance, but this is creating duplication between the two sets so we need to find ways to invoke our current PowerShell Core tests as part of our STEX based runs.
When we do this, we can start to delete our existing tests and reduce the maintenance burden due to duplicated tests.

**Proposal**

* Create a way to invoke our github based tests in lab runs

## Historical Tests
The PowerShell team created roughly 90,000 tests since the beginning of the project starting in 2003.
Initially tests were created in a C# framework which was used internally at Microsoft.
After that, a number of script based frameworks were created to run script based tests.
We have decided not to release those for a number of reasons:
* All the frameworks have a core assumption of running in the Microsoft Lab environment and rely on internal Microsoft tools
* All the frameworks rely on specific logging file formats used by internal Microsoft reporting tools
* Some of the frameworks have limitations which we can avoid by recasting/recreating them as `Pester` or `xUnit` tests

While, these reasons are not insurmountable, releasing these tests with their custom frameworks does not help us with our desire to promulgate open solutions.
Further, by creating/releasing `xUnit` and `Pester` tests we can take the opportunity to review existing tests and improve them, or eliminate them if they are poor, or duplicate tests.
It is obvious that the review of more than 90,000 tests is an enormous undertaking, and will take some time, and we are committed to creating a set of tests which can provide confidence that PowerShell Core has the same quality as earlier releases of PowerShell.

## Challenges ##
While there is a test gap between what is available on Github vs what is available via our internal proprietary tests, it may be possible to use our existing tests (and frameworks) against PowerShell Core.
This is an attractive potential because we have very high confidence of our current tests.
Moreover, we could also collect code coverage data during a test pass.
It would be expected to have a number of test failures because different functionality between PowerShell Core and Full PowerShell but collecting this data would enable us to enumerate those difference.
Unfortunately, the current logging mechanism used in our in-lab tests are not available in PowerShell Core, and a new logging mechanism would need to be created

**Proposal**

* Investigate the effort to run our historical tests in a PowerShell Core environment

# Action Plan
This document represents a number of initiatives, all of which will take resources.
Below is my suggestion for prioritization to reduce risk and improve confidence in our overall quality:

1. Implement telemetry feeds for PowerShell Core
2. Implement data collection feeds from our CI systems
3. Create a public dashboard to visualize our KPIs
    * including data from 1, 2 above
4. Implement Code Coverage on PowerShell Core
5. Design/Implement remoting test infrastructure
6. Replace in-lab tests with PowerShell Core tests
7. Investigate feasibility of running current in-lab tests on PowerShell Core

These are [tracked](https://github.com/PowerShell/PowerShell/issues?utf8=%E2%9C%93&q=is%3Aissue%20%23testability%20) as issues
