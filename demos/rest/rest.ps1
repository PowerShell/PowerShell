# NOTE:  This demo is still in progress and needs validation in Linux
# ------------------------------------

#region Setup the credentials for use in HTTP header
$user = '<insert GitHub PAT token>'
$pass= ""
$pair = "${user}:${pass}"
$bytes =  [System.Text.Encoding]::ASCII.GetBytes($pair)
$base64 = [System.Convert]::ToBase64String($bytes)
$basicAuthValue = "Basic $base64"
$headers = @{ Authorization = $basicAuthValue }
#endregion

# Changing the status of a Private GitHub repository to Public

# URL to PowerShell Github Repo
$PowerShellGithubUri = 'https://api.github.com/repos/maertend/opstest'

# Get the blob from the Github API as a PS object
$JsonBlock = Invoke-RestMethod -Uri $PowerShellGithubUri -Headers $headers

# Explore the object (Notice that it is a private repo)
$JsonBlock

# Given it is an object, you can explore and interact with it
# Change the private value to false
$JsonBlock.private = 'false'

# Convert the object back to a json
$Json = ConvertTo-Json $JsonBlock

# Post the updated json block back to the GitHub
Invoke-RestMethod -Uri $PowerShellGithubUri -Headers $headers -Method Post -Body $Json


# --------------

# We can also use the PS objects to sort the different repos on github

# If we grab the json from the PowerShell github
Invoke-RestMethod https://api.github.com/users/powershell/repos | sv repoData

# We can sort it based on the number of forks each repo has
$repoData | Sort-Object -Property forks_count -Descending | ft -f id,name,stargazers_count,forks_count

$repoData | Sort-Object -Property stargazers_count -Descending | ft -f id,name,stargazers_count,forks_count
