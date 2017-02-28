using System.Management.Automation.Language;

[Keyword(Name = DynamicKeywordNameMode.NameRequired, Body = DynamicKeywordBodyMode.ScriptBlock)]
public class TypeExtension : Keyword
{
    [Keyword(DirectCall = true)]
    public class Method : Keyword
    {
        [KeywordParameter()]
        public ScriptBlock ScriptMethod { get; set; }

        [KeywordParameter()]
        public string CodeReference { get; set; }

        [KeywordParameter()]
        public string ReferencedType { get; set; }
    }

    [Keyword(DirectCall = true)]
    public class Property : Keyword
    {
        [KeywordParameter()]
        public string Alias { get; set; }

        [KeywordParameter()]
        public ScriptBlock ScriptProperty { get; set; }

        [KeywordParameter()]
        public object NoteProperty { get; set; }

        [KeywordParameter()]
        public string CodeReference { get; set; }
    }

    [Keyword(DirectCall = true)]
    public class PropertySet : Keyword
    {
        [KeywordParameter()]
        public string[] ReferencedProperties { get; set; }
    }

    [Keyword(Name = DynamicKeywordNameMode.NameRequired, Body = DynamicKeywordBodyMode.ScriptBlock)]
    public class MemberSet : Keyword
    {
    }
}