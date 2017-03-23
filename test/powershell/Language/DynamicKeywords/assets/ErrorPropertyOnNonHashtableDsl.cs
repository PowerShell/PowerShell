using System.Management.Automation.Language;

[Keyword()]
public class ErrorKeyword : Keyword
{
    [KeywordProperty()]
    public string BadProperty { get; set; }
}