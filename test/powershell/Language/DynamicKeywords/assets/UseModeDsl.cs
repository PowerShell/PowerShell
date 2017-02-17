using System.Management.Automation;
using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class UseModeDsl : Keyword
{
    [Keyword()]
    public class RequiredUseKeyword : Keyword
    {
    }

    [Keyword()]
    public class RequiredManyUseKeyword : Keyword
    {
    }

    [Keyword()]
    public class OptionalUseKeyword : Keyword
    {
    }

    [Keyword()]
    public class OptionalManyUseKeyword : Keyword
    {
    }
}