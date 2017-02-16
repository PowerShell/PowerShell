using System.Management.Automation;

[Keyword()]
public class ErrorKeyword : Keyword
{
    [KeywordProperty()]
    public string BadProperty { get; set; }
}