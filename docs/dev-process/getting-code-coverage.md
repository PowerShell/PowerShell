# Getting Code Coverage Information for PowerShell
Gathering code coverage data is only available on Windows systems as the required underlying support exists only there.
We use [OpenCover](https://github.com/OpenCover/opencover) to gather our coverage data and have created a module (found in test/tools/OpenCover) to accelerate the process.
When building and gathering code coverage, it should be done from an elevated prompt, this is because we execute tests with elevation and then use `runas` to run un-elevated.

Here's the simplified workflow for gathering data:

    1 Create a Code Coverage build `start-psbuild -configuration CodeCoverage`
    2 Load the OpenCover module `import-module ./test/tools/OpenCover`
    3 install OpenCover `Install-OpenCover`
    4 Gather coverage data `Invoke-OpenCover`
    5 Inspect coverage data 
        `$cc = Get-CoverageData $HOME/Documents/CodeCoverage.xml`
        `$cc.FileCoverage`
    6 Determine where you want to improve coverage
    7 Create new tests
    8 Regather coverage data `Invoke-OpenCover -path <path to new test file> -out $PWD/newcoverage.xml`
    9 Inspect coverage data
        `$newcoverage = Get-CoverageData $PWD/newcoverage.xml`
    10 Compare coverage
        `Compare-CodeCoverage $cc $newcoverage`

Iterating over steps 6-9 is actually pretty quick, depending on how many tests have been added.
It should be noted that OpenCover merges data from one run to the next, so it may be better to remove the `newcoverage.xml` between runs.
Code coverage runs usually take a few hours to run, and it may be more efficient to use our [codecov.io](https://codecov.io/gh/PowerShell/PowerShell) instead of steps 5 and 6.
In this case you won't be able to easily compare runs with the OpenCover module, but you will still be able to inspect coverage improvements.