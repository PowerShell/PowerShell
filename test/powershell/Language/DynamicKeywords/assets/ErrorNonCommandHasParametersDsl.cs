using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class ErrorNonCommandHasParametersKeyword : Keyword
{
    [KeywordParameter()]
    string Parameter { get; set; }
}