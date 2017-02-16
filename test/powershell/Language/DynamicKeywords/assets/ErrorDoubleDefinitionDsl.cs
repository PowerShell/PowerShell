using System.Management.Automation.Language;
using System.Management.Automation;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class DoubleDefKeyword : Keyword
{
    [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
    public class SomethingElseKeyword : Keyword
    {
        [Keyword()]
        public class DoubleDefKeyword : Keyword
        {

        }
    }
}