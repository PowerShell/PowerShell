# NOTE:  This demo is still in progress and needs validation in Linux
# ------------------------------------

# Setup the credentials for use in HTTP header
$user = "<insert GitHub PAT token>"
$pass= ""
$pair = "${user}:${pass}"
$bytes =  [System.Text.Encoding]::ASCII.GetBytes($pair)
$base64 = [System.Convert]::ToBase64String($bytes)
$basicAuthValue = "Basic $base64"
$headers = @{ Authorization = $basicAuthValue }



# View the repos of a specified user
Invoke-RestMethod -Uri https://api.github.com/users/maertend/repos

# EQ Linux
# curl https://api.github.com/users/maertend/repos


# ---------------------

# Creating a new Gist on GitHub

$payload = @{description="created via API"; public="true"; files=@{'testGist.txt'=@{'content'="teststring"}}}
$jsonPayload = ConvertTo-Json $payload

Invoke-RestMethod -Uri https://api.github.com/gists -Headers $headers -Method Post -Body $jsonPayload

# EQ Linux
# curl -d '{"description":"created via API","public":true,"files":{"testGist_curl.txt":{"content":"teststring"}}}' -u <insert GitHub PAT token> https://api.github.com/gists


# ---------------------

# Changing the status of a Private GitHub repository to Public

$Uri = "https://api.github.com/repos/maertend/opstest"
# Get repo info (json) from the github api
$APIobject = Invoke-RestMethod -Uri $Uri -Headers $headers
 Set the private status to false
$APIobject.private = "False"
# Convert back to json
$APIjson = ConvertTo-Json $APIobject
# Push the new json back to the github api
Invoke-RestMethod -Uri $Uri -Headers $headers -Method Post -Body $APIjson

# EQ Linux

    # get the json from the repo api and assign it to the txt file
#curl -u <insert GitHub PAT token> https://api.github.com/repos/maertend/opstest > output.txt

    # Manually modify the value of the field "private" to "false"
    # Manually modify to have contiguous string with no newline chars
#vim output.txt

    # Push the updated JSON data to the repo
#curl -u <insert GitHub PAT token> https://api.github.com/repos/maertend/opstest --data @output.txt



#-------------------

# Get all repositories from the GitHub API, sort by most pull requests, most stars, most forks, etc.
