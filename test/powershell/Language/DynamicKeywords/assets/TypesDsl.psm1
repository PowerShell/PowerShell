function Get-MethodInfo
{
    param([string]$TypeExprStr)

    $typeParts = $TypeExprStr -split "::"
    if ($typeParts.Length -ne 2)
    {
        throw "Syntax error: Expected a code reference like '<type-name>::<method-name>'"
    }

    [type]$type = $typeParts[0].TrimStart("[").TrimEnd("]") -as [type]
    if ($type -eq $null)
    {
        throw "The type '$($typeParts[0])' could not be found"
    }

    $method = $type.GetMethod($typeParts[1])
    if ($method -eq $null)
    {
        throw "Type '$($type.Name)' has no method '$($typeParts[1])'"
    }

    $method[0]
}

function Set-DisplayProperties
{
    param([string]$TypeName, [hashtable]$DisplayProperties)

    foreach ($displayKind in $DisplayProperties.Keys)
    {
        switch ($displayKind)
        {
            "WideDefaultDisplayProperty" { Update-TypeData -TypeName $TypeName -DefaultDisplayProperty $DisplayProperties.$displayKind }
            "ListDefaultDisplayProperties" { Update-TypeData -TypeName $TypeName -DefaultDisplayPropertySet $DisplayProperties.$displayKind }
            "DefaultSortPropertyKeys" { Update-TypeData -TypeName $TypeName -DefaultKeyPropertySet $DisplayProperties.$displayKind }
            default { throw "Unknown display option: '$displayKind'" }
        }
    }
}

function TypesDsl\TypeExtension
{
    param($KeywordData, $Name, [scriptblock]$Value, $SourceMetadata)

    if ([type]::GetType($Name) -eq $null)
    {
        throw "Unable to find type '$Name'"
    }

    foreach ($typeAddition in (& $Value))
    {
        switch ($typeAddition.Kind)
        {
            "ScriptMethod" { Update-TypeData -MemberType ScriptMethod -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.ScriptMethod }
            "CodeMethod" { Update-TypeData -MemberType CodeMethod -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.MemberInfo }

            "ScriptProperty" { Update-TypeData -MemberType ScriptProperty -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.ScriptProperty }
            "NoteProperty" { Update-TypeData -MemberType NoteProperty -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.NoteProperty }
            "AliasProperty" { Update-TypeData -MemberType AliasProperty -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.AliasProperty }
            "CodeProperty" { Update-TypeData -MemberType CodeProperty -TypeName $Name -MemberName $typeAddition.Name -Value $typeAddition.MemberInfo }

            "DisplayProperties" { Set-DisplayProperties -TypeName $Name -DisplayProperties $typeAddition.DisplayProperties }
        }
    }
}

function TypesDsl\Method
{
    param($KeywordData, [Parameter(Position=0)]$InstanceName, $Value, $SourceMetadata,
          [Parameter(ParameterSetName="ScriptMethod", Mandatory=$true)][scriptblock]$ScriptMethod,
          [Parameter(ParameterSetName="CodeReference", Mandatory=$true)][string]$CodeReference)

    if ($ScriptMethod -ne $null)
    {
        return @{
            Kind = "ScriptMethod"
            Name = $InstanceName
            ScriptMethod = $ScriptMethod
        }
    }

    if ($CodeReference -ne $null)
    {
        $memberInfo = Get-MethodInfo -TypeExprStr $CodeReference

        return @{
            Kind = "CodeMethod"
            Name = $InstanceName
            MemberInfo = $memberInfo
        }
    }
}

function TypesDsl\Property
{
    param($KeywordData, [Parameter(Position=0)]$InstanceName, $Value, $SourceMetadata,
          [Parameter(ParameterSetName="Alias", Mandatory=$true)][string]$Alias,
          [Parameter(ParameterSetName="ScriptProperty", Mandatory=$true)][scriptblock]$ScriptProperty,
          [Parameter(ParameterSetName="NoteProperty", Mandatory=$true)][object]$NoteProperty,
          [Parameter(ParameterSetName="CodeReference", Mandatory=$true)][string]$CodeReference)

    if (-not [string]::IsNullOrEmpty($Alias))
    {
        return @{
            Kind = "AliasProperty"
            Name = $InstanceName
            AliasProperty = $Alias
        }
    }

    if (-not [string]::IsNullOrEmpty($CodeReference))
    {
        $memberInfo = Get-MethodInfo -TypeExprStr $CodeReference

        return @{
            Kind = "CodeProperty"
            Name = $InstanceName
            MemberInfo = $memberInfo
        }
    }

    if ($NoteProperty -ne $null)
    {
        return @{
            Kind = "NoteProperty"
            Name = $InstanceName
            NoteProperty = $NoteProperty
        }
    }

    if ($ScriptProperty -ne $null)
    {
        return @{
            Kind = "ScriptProperty"
            Name = $InstanceName
            ScriptProperty = $ScriptProperty
        }
    }
}

function TypesDsl\DisplayProperty
{
    param($KeywordData, $Name, [hashtable]$Value, $SourceMetadata)

    return @{
        Name = "DisplayProperties"
        DisplayProperties = $Value
    }
}