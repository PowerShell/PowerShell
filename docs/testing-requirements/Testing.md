# DRAFT

_I have more questions than answers_


#### Current Test Infrastructure
We currently rely heavily on STEX environment for our testing, and we will continue to do so through the Server2016 release. We 
need to use that current infrastructure to continue to test full PowerShell builds; it should be possible to build automation
which takes a full PowerShell build and lay it on an existing lab system, update the appropriate test files and execute
a test pass in the same way that we do with official builds.

The test artifacts which are applicable to full PowerShell are not universally applicable to Core/Nano/OtherPlatform, we will need
to create tooling which allows us to apply a set of test artifacts to a configuration, and then execute tests. Eventually, we need
to have our CI environment test all the flavors of PowerShell we create.
**Question**: Can AppVeyor/Travis service that need?


#### Organization
**Proposal**: Create 3 tiers of testing:

* Checkin 
  * These are run as part of the CI process, and should run quickly. How quickly is an open question. We need to determine
  the right amount of coverage without spending too much time. It may be that we can improve our coverage here through parallelization
  but we have not investigated enough to determine whether it's possible.
* Feature 
  * the tests which look at corner cases, and stand-alone modules (for example, the archive module tests could fall into this
  category)
* Scenario 
  * these are tests which span features, and determine whether the whole product is working correctly. The current P3 tests fall
  largely here

**Actions**: Decide what goes where. My initial thoughts are to migrate our current TTEST unittests into tier 1 (Checkin)

**Current Migration Activity**

We have teams working on migrating tests which are in non-portable frameworks (TTest, Lite1, Lite3, etc) to portable frameworks. 
The first effort is to migrate our TTEST cmdlet unit tests to Pester, we should be taking those migrated tests and get them into  
SD 

##### Layout
We need to have a reasonable layout of our tests, not sure what that looks like yet. We need to make it
easy to find both feature and test code to reduce our maintainance burden.

##### Self Hosting
Self-Hosting remains problematic while are still so early in the development phase, but it is _imperative_
that we dog food as early as possible. This is especially true on the non-Windows platforms where we have made
assumptions about the working environment with regard to a number of issues:
* removal of well known aliases
* case sensitivity of some operations
* coverage
We should be using these non-windows platforms as much as possible to 

=======
# DRAFT

_I have more questions than answers_


#### Current Test Infrastructure
We currently rely heavily on STEX environment for our testing, and we will continue to do so through the Server2016 release. We 
need to use that current infrastructure to continue to test full PowerShell builds; it should be possible to build automation
which takes a full PowerShell build and lay it on an existing lab system, update the appropriate test files and execute
a test pass in the same way that we do with official builds.

The test artifacts which are applicable to full PowerShell are not universally applicable to Core/Nano/OtherPlatform, we will need
to create tooling which allows us to apply a set of test artifacts to a configuration, and then execute tests. Eventually, we need
to have our CI environment test all the flavors of PowerShell we create.
**Question**: Can AppVeyor/Travis service that need?


#### Organization
**Proposal**: Create 3 tiers of testing:

* Checkin 
  * These are run as part of the CI process, and should run quickly. How quickly is an open question. We need to determine
  the right amount of coverage without spending too much time. It may be that we can improve our coverage here through parallelization
  but we have not investigated enough to determine whether it's possible.
* Feature 
  * the tests which look at corner cases, and stand-alone modules (for example, the archive module tests could fall into this
  category)
* Scenario 
  * these are tests which span features, and determine whether the whole product is working correctly. The current P3 tests fall
  largely here

**Actions**: Decide what goes where. My initial thoughts are to migrate our current TTEST unittests into tier 1 (Checkin)

**Current Migration Activity**

We have teams working on migrating tests which are in non-portable frameworks (TTest, Lite1, Lite3, etc) to portable frameworks. 
The first effort is to migrate our TTEST cmdlet unit tests to Pester, we should be taking those migrated tests and get them into  
SD 

##### Layout
We need to have a reasonable layout of our tests, not sure what that looks like yet. We need to make it
easy to find both feature and test code to reduce our maintainance burden.

##### Self Hosting
Self-Hosting remains problematic while are still so early in the development phase, but it is _imperative_
that we dog food as early as possible. This is especially true on the non-Windows platforms where we have made
assumptions about the working environment with regard to a number of issues:
* removal of well known aliases
* case sensitivity of some operations
* coverage
We should be using these non-windows platforms as much as possible to 

>>>>>>> Create Testing.md
