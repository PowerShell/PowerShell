# PowerShell for Linux

This readme is targeted at PowerShell for Linux users looking to write test suites to ensure quality of PowerShell products.

## Getting started

These instructions assume Ubuntu 14.04 LTS. It is assumed that PowerShell for Linux is currently installed on the system.

### Obtain PowerShell

PowerShell is required to enable and run the test suites.

### Testing technology

Technology | Purpose
-------|------
Pester | default cmdlet test framework
xUnit | default C# test framework
cppUnit | default C/C++ testing framework

### Running the test suite

Tests can be run from the `scripts` folder. If you are currently in `monad-linux/scripts`, then run `./build.sh make test` to run the complete tests suite.  The table below shows the commands to run for the various test products bundled with Powershell for Linux (the table assumes the current working directory is `monad-linux/scripts`).

It is strongly recommended that before major changes are tested, that users run `./build.sh make clean cleanall prepare` to ensure the environment is completely clean.

Technology | Run Method
------|---------
Pester | ./build.sh make pester-tests
xUnit | ./build.sh make xunit-tests
cppunit | ./build.sh make native-tests
hashbang tests | ./build.sh make hashbang-tests

within the `scripts` directory, you may also wish to run Pester tests on a single file.  This can be done easily using `./build.sh make {path/from/scripts/to/pester/test} (E.g, `./build.sh make ../src/pester-test/Test-TESTFILE.Test.ps1`).
