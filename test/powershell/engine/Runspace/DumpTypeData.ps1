
using namespace System.Reflection
using namespace System.Management.Automation.Runspaces
using namespace System.Collections.Generic

function Print-TypeData([TypeData]$typeData)
{
    function ConditionalProperty($property)
    {
        if ($typeData.$property)
        {
            "$([Environment]::NewLine)    $property = $($typeData.$property)"
        }
    }

    function PropertySet([string]$propertySetName)
    {
        $propertySet = $typeData.$propertySetName
        if ($propertySet)
        {
            "$([Environment]::NewLine)    $propertySetName =" + $(
                foreach ($p in $propertySet.ReferencedProperties)
                {
                    "$([Environment]::NewLine)        $p"
                }
            )
        }
    }

    function Member([TypeMemberData]$member)
    {
        function MaybeHidden($m)
        {
            if ($m.IsHidden) { "(hidden) " }
        }
        function MaybeType($m)
        {
            if ($m.MemberType) { "[$($m.MemberType)]" }
        }
        function CodeReference([MethodInfo]$m)
        {
            $t = [Microsoft.PowerShell.ToStringCodeMethods]::Type($m.DeclaringType)
            "[$t]::$($m.Name)"
        }

        [AliasPropertyData]$alias = $member -as [AliasPropertyData]
        if ($alias)
        {
            "Alias: $(MaybeHidden $alias)$(MaybeType $alias)$($alias.ReferencedMemberName)"
            return
        }

        [CodeMethodData]$codeMethod = $member -as [CodeMethodData]
        if ($codeMethod)
        {
            "CodeMethod: $(CodeReference $codeMethod.CodeReference)"
            return
        }

        [CodePropertyData]$codeProperty = $member -as [CodePropertyData]
        if ($codeProperty)
        {
            "CodeProperty: $(MaybeHidden $codeProperty)$([Environment]::NewLine)" +
            $(if ($codeProperty.GetCodeReference) { "            get: $(CodeReference $codeProperty.GetCodeReference)"}) +
            $(if ($codeProperty.SetCodeReference) { "            set: $(CodeReference $codeProperty.SGetCodeReference)"})
            return
        }

        [NotePropertyData]$noteProperty = $member -as [NotePropertyData]
        if ($noteProperty)
        {
            "NoteProperty: $(MaybeHidden $noteProperty)$Value"
            return
        }

        [ScriptMethodData]$scriptMethod = $member -as [ScriptMethodData]
        if ($scriptMethod)
        {
            "ScriptMethod: {$($scriptMethod.Script)}"
            return
        }

        [ScriptPropertyData]$scriptProperty = $member -as [ScriptPropertyData]
        if ($scriptProperty)
        {
            "ScriptProperty: " +
            $(if ($scriptProperty.GetScriptBlock) { "            get: {$($scriptProperty.GetScriptBlock)}" }) +
            $(if ($scriptProperty.SetScriptBlock) { "            set: {$($scriptProperty.SetScriptBlock)}" }) 
            return
        }

        "UnknownType: $($member.GetType().Name)"
    }

    function MemberSet([Dictionary[string,TypeMemberData]]$memberSet)
    {
        # Call property via method instead of property syntax because
        # we do have dictionaries with a key value of 'Count'.
        if ($memberSet.get_Count() -gt 0)
        {
            "$([Environment]::NewLine)    Members =" + $(
                foreach ($m in $memberSet.Keys | Sort-Object)
                {
                    "$([Environment]::NewLine)        $m = $(Member $memberSet[$m])"
                }
            )
        }
    }

    "TypeData $($typeData.TypeName)$([Environment]::NewLine){" +
    (ConditionalProperty TypeConverter) +
    (ConditionalProperty TypeAdapter) +
    (ConditionalProperty IsOverride) +
    (ConditionalProperty DefaultDisplayProperty) +
    (ConditionalProperty SerializationDepth) +
    (ConditionalProperty SerializationMethod) +
    (ConditionalProperty TargetTypeForDeserialization) +
    (ConditionalProperty StringSerializationSource) +
    (ConditionalProperty InheritPropertySerializationSet) +
    (PropertySet DefaultDisplayPropertySet) +
    (PropertySet DefaultKeyPropertySet) +
    (PropertySet PropertySerializationSet) +
    (MemberSet $typeData.Members) +
"$([Environment]::NewLine)}"
}

foreach ($t in Get-TypeData | Sort -Property TypeName)
{
    Print-TypeData $t
}
