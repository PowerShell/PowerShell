using System.Management.Automation;
using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class TypeExtension : Keyword
{
    [KeywordParameter(Mandatory = true)]
    public string Name { get; set; }

    [Keyword()]
    public class Method : Keyword
    {
        [KeywordParameter(Mandatory = true)]
        public string Name { get; set; }

        [KeywordParameter()]
        public ScriptBlock ScriptMethod { get; set; }

        [KeywordParameter()]
        public string CodeReference { get; set; }

        [KeywordParameter()]
        public string ReferencedType { get; set; }
    }

    [Keyword()]
    public class Property : Keyword
    {
        [KeywordParameter(Mandatory = true)]
        public string Name { get; set; }

        [KeywordParameter()]
        public string Alias { get; set; }

        [KeywordParameter()]
        public ScriptBlock ScriptProperty { get; set; }

        [KeywordParameter()]
        public object NoteProperty { get; set; }

        [KeywordParameter()]
        public string ReferencedType { get; set; }
    }
}