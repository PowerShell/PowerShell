using System.Management.Automation; 

public enum OuterType
{
    OuterOne,
    OuterTwo,
}

[Keyword()]
public class ParameterKeyword : Keyword
{
    public enum InnerType
    {
        InnerOne,
        InnerTwo,
    }

    [KeywordParameter()]
    public string StringParameter { get; set; }

    [KeywordParameter()]
    public int IntParameter { get; set; }

    [KeywordParameter()]
    public OuterType OuterParameter { get; set; }
    
    [KeywordParameter()]
    public InnerType InnerParameter { get; set; }
}