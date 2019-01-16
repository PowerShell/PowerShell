# Code Coverage Automation

The script Start-CodeCoverageRun.ps1 automates the execution of tests and uploading the results to Coveralls.io.

The script is self contained and can be executed on any Windows system which supports Powershell v5.0 or above. It has not been tested on Powershell v6.0.

## Execution

The virtual machine hosting the script has a scheduled task to start the tests at 5 am PDT.

The script follows the steps below:

1. Download the code coverage binaries package from Azure DevOps Windows nightly builds artifacts (CodeCoverage.zip).
2. Download the OpenCover powershell module from Azure DevOps Windows nightly builds artifacts. (OpenCover.zip)
3. Download the tests from Azure DevOps Windows nightly builds artifacts (tests.zip)
4. Download Coveralls.net from [here](https://github.com/csMACnz/coveralls.net/releases/download/0.7.0/coveralls.net.0.7.0.nupkg)
5. Invoke 'Install-OpenCover' to install OpenCover toolset.
6. Invoke 'Invoke-OpenCover' to execute tests.
7. Invoke powershell to get the git commit ID of the downloaded daily build package.
8. Using the commit ID get committer info like, message, author and email using the github REST API.
9. Invoke 'csmacnz.Coveralls.exe' to upload the coverage results to Coveralls.net

The uploaded coverage data can be viewed at: https://coveralls.io/github/PowerShell/PowerShell
