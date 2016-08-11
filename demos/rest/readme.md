This demo shows interacting with the Github API via Invoke-RestMethod.
NOTE: A repo URL must be specified in these scripts and a Github PAT token with access to the repo must be generated and specified

rest.ps1:
Invoke-RestMethod is used to get the json of a repo as a PowerShell object,
the object is then manipulated and the "private" parameter is changed to 'false'.
The object is converted back to json formating and Posted back to the repo API

The benefit of PowerShell is shown at the end of the script with PS objects.
Enabling users to get info on multiple repos and then sort that data as objects.

curlDemo.txt:
This shows the equavilent bash commmands to change the private status of a Github repo
