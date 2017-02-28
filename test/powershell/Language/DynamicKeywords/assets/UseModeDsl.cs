using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class UseModeDsl : Keyword
{
    [Keyword(Use = DynamicKeywordUseMode.Required)]
    public class RequiredUseKeyword : Keyword
    {
    }

    [Keyword(Use = DynamicKeywordUseMode.RequiredMany)]
    public class RequiredManyUseKeyword : Keyword
    {
    }

    [Keyword(Use = DynamicKeywordUseMode.Optional)]
    public class OptionalUseKeyword : Keyword
    {
    }

    [Keyword(Use = DynamicKeywordUseMode.OptionalMany)]
    public class OptionalManyUseKeyword : Keyword
    {
    }
}