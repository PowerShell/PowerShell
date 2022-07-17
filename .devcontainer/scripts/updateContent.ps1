#Reference: https://code.visualstudio.com/docs/remote/devcontainerjson-reference#_lifecycle-scripts

#Perform a restore that will be kept in the container percache
'powershell-unix','Modules' | ForEach-Object {
    & dotnet restore ./src/$PSItem --runtime linux-x64 /property:SDKToUse=Microsoft.NET.Sdk
}

