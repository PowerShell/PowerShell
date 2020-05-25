Function Combine-Object {

    [CmdletBinding()]
    Param (
        [Parameter(Mandatory = $true)]
        [ValidateCount(2, 2147483646)]
        [PSObject[]]$InputObject,
        [String[]]$Property = @()
    )

    # query if the user has given property namens to select from the Objects
    # for performance reasons we do this query outside of the ForEach loop
    $FilterProperties = $false
    If ($Property.count -gt 0) {$FilterProperties = $True}

    # to avoid the use of the slow speed += operator or the Add-Member cmdlet
    # we are using a hashtabel here
    # create empty hashtable to hold the properties for the resulting Object
    $ResultHash = @{}

    # process each object from the InputObject array
    ForEach ($InObject in $InputObject) {
        # process each property from the current processed object
        ForEach ( $InObjProperty in $InObject.psobject.Properties) {
            # If the user which to select only given Properties
            If ($FilterProperties) {
                # add only selected property to the hashtable
                If ($Property -contains $InObjProperty.Name) {
                    $ResultHash.($InObjProperty.Name) = $InObjProperty.value
                }
            }
            Else {
                # user do not want to filter down the Properties out of the object
                # so we add all properties from the objects to the hashtable
                # ATENTION! If a Property from one Object has the same Name like another Property
                # this will result in a non terminating error!
                $ResultHash.($InObjProperty.Name) = $InObjProperty.value
            }
        }
    }

    # create (and return) the resulting Object from from the hashtable
    # this is the PowerShell 2.0 compatible and fast way to create an custom object!
    New-Object -TypeName PSObject -Property $ResultHash
}