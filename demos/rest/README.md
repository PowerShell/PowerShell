## REST demo

This demo shows how to interact with the GitHub API using the Invoke-WebRequest cmdlet.

rest.ps1:
Invoke-WebRequest and ConvertFrom-Json cmdlets are used to get the issues of a repo.
The issues are processed as objects to find the most commented on issues.
