function Get-PropertyValue
{
    param($Object, $PropertyName)

    $bindingFlags = [System.Reflection.BindingFlags]::NonPublic -bor
        [System.Reflection.BindingFlags]::Instance

    $property = $Object.GetType().GetProperty($PropertyName, $bindingFlags)
    $property.GetValue($Object)
}

function Get-MethodInfo
{
    param([type] $Type, $MethodName)

    $bindingFlags = [System.Reflection.BindingFlags]::NonPublic -bor
        [System.Reflection.BindingFlags]::Public -bor
        [System.Reflection.BindingFlags]::Instance

    $Type.GetMethod($MethodName, $bindingFlags)
}

# Expects a string like "System.Management.Automation.Language.DynamicKeyword::GetKeyword"
function Get-CodeReferenceMethod
{
    param([string]$CodeReference)

    $typeParts = $CodeReference -split "::"

    if ($typeParts.Count -ne 2)
    {
        throw "Syntax error in code reference '$CodeReference'; the reference should look like 'FullTypeName::MethodName'"
    }

    $type = [type]::GetType($typeParts[0])
    if ($type -eq $null)
    {
        throw "No type '$($typeParts[0])' is defined"
    }

    $method = Get-MethodInfo -Type $type -MethodName $typeParts[1]
    if ($method -eq $null)
    {
        throw "No method '$($typeParts[1])' is present on type '$($typeParts[0])'"
    }

    $method
}

function TypesDsl\TypeExtension
{
    param($KeywordData, $Name, $Value, $SourceMetadata)

    if ([type]::GetType($Name) -eq $null)
    {
        throw "The type '$Name' is not known"
    }

    $typeData = [System.Management.Automation.Runspaces.TypeData]::new($Name)

    foreach ($typeAddition in (& $Value))
    {
        $typeData.Members.Add($typeAddition.Name, $typeAddition.Value)
    }

    $errors = [System.Collections.Concurrent.ConcurrentBag`1[string]]::new()

    $ssi = Get-PropertyValue -Object $ExecutionContext.SessionState -PropertyName "Internal"
    $ec = Get-PropertyValue -Object $ssi -PropertyName "ExecutionContext"
    $typeTable = Get-PropertyValue -Object $ec -PropertyName "TypeTable"

    $processType = Get-MethodInfo -Type $typeTable.GetType() -MethodName "ProcessTypeDataToAdd"

    $processType.Invoke($typeTable, @($errors, $typeData))
}

function TypesDsl\Method
{
    param($KeywordData, $Name, $Value, $SourceMetadata,
          [Parameter(ParameterSetName="ScriptMethod")][scriptblock]$ScriptMethod,
          [Parameter(ParameterSetName="CodeReference")][string]$CodeReference)

    if ($ScriptMethod -ne $null)
    {
        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.ScriptMethodData]::new($Name, $ScriptMethod)
        }
    }

    if ($CodeReference -ne $null)
    {
        $method = Get-CodeReferenceMethod -CodeReference $CodeReference

        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.CodeMethodData]::new($Name, $method)
        }
    }
}

function TypesDsl\Property
{
    param($KeywordData, $Name, $Value, $SourceMetadata,
          [Parameter(ParameterSetName="Alias")][string]$Alias,
          [Parameter(ParameterSetName="ScriptProperty")][scriptblock]$ScriptProperty,
          [Parameter(ParameterSetName="NoteProperty")][object]$NoteProperty,
          [Parameter(ParameterSetName="CodeReference")][string]$CodeReference)

    if (-not [string]::IsNullOrEmpty($Alias))
    {
        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.AliasPropertyData]::new($Name, $Alias)
        }
    }

    if (-not [string]::IsNullOrEmpty($CodeReference))
    {
        $method = Get-CodeReferenceMethod -CodeReference $CodeReference

        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.CodeMethodData]::new($Name, $method)
        }
    }

    if ($NoteProperty -ne $null)
    {
        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.NotePropertyData]::new($Name, $NoteProperty)
        }
    }

    if ($ScriptProperty -ne $null)
    {
        return @{
            Name = $Name
            Value = [System.Management.Automation.Runspaces.ScriptPropertyData]::new($Name, $ScriptProperty)
        }
    }
}