using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.Command)]
public class CommandBodyKeyword : Keyword
{
}

[Keyword(Body = DynamicKeywordBodyMode.Hashtable)]
public class HashtableBodyKeyword : Keyword
{
    [KeywordProperty()]
    public string StringProperty { get; set; }
}

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class ScriptBlockBodyKeyword : Keyword
{
}