using System.Management.Automation;
using System.Management.Automation.Language; 

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

    [KeywordParameter()]
    public SwitchParameter Switch { get; set; }

    [KeywordParameter(Mandatory = true)]
    public string MandatoryParameter { get; set; }
}