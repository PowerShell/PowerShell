using System.Management.Automation.Language;

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class NestedKeyword : Keyword
{
    [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
    public class NestedKeyword1 : Keyword
    {
        [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
        public class NestedKeyword1_1 : Keyword
        {
            [Keyword()]
            public class NestedKeyword1_1_1 : Keyword
            {
            }
        }

        [Keyword()]
        public class NestedKeyword1_2 : Keyword
        {
        }
    }

    [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
    public class NestedKeyword2 : Keyword
    {
        [Keyword()]
        public class NestedKeyword2_1 : Keyword
        {
        }

        [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
        public class NestedKeyword2_2 : Keyword
        {
            [Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
            public class NestedKeyword2_2_1 : Keyword
            {
                [Keyword()]
                public class NestedKeyword2_2_1_1 : Keyword
                {
                }
            }
        }
    }
}