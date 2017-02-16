using System.Management.Automation;

[Keyword(Body = DynamicKeywordBodyMode.Command)]
public class CommandBodyKeyword : Keyword
{
}

[Keyword(Body = DynamicKeywordBodyMode.Hashtable)]
public class HashtableBodyKeyword : Keyword
{
}

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class ScriptBlockBodyKeyword : Keyword
{
}