using System.Management.Automation.Language;

public enum OuterType
{
    OuterOne,
    OuterTwo,
}

[Keyword(Body = DynamicKeywordBodyMode.Hashtable)]
public class PropertyKeyword : Keyword
{
    public enum InnerType
    {
        InnerOne,
        InnerTwo,
    }

    [KeywordProperty()]
    public string StringProperty { get; set; }

    [KeywordProperty()]
    public int IntProperty { get; set; }

    [KeywordProperty()]
    public OuterType OuterProperty { get; set; }

    [KeywordProperty()]
    public InnerType InnerProperty { get; set; }

    [KeywordProperty(Mandatory = true)]
    public string MandatoryProperty { get; set; }
}