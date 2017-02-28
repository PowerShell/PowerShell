using System.Management.Automation.Language;

static class Helper
{
    private static ScriptPosition s_emptyPosition;
    private static IScriptExtent s_emptyExtent;
    public static ScriptPosition EmptyPosition
    {
        get
        {
            return s_emptyPosition ??
                (s_emptyPosition = new ScriptPosition("", 0 , 0, "", ""));
        }
    }
    public static IScriptExtent EmptyExtent
    {
        get
        {
            return s_emptyExtent ??
                (s_emptyExtent = new ScriptExtent(EmptyPosition, EmptyPosition));
        }
    }
}

[Keyword()]
public class SimplePreParseKeyword : Keyword
{
    public SimplePreParseKeyword()
    {
        PreParse = ThrowError;
    }

    private static ParseError[] ThrowError(DynamicKeyword thisKeyword)
    {
        return new [] { new ParseError(Helper.EmptyExtent, "SuccessfulPreParse", "Successful PreParse action") };
    }
}

[Keyword()]
public class SimplePostParseKeyword : Keyword
{
    public SimplePostParseKeyword()
    {
        PostParse = ThrowError;
    }
    
    private static ParseError[] ThrowError(DynamicKeywordStatementAst kwAst)
    {
        return new [] { new ParseError(kwAst.Extent, "SuccessfulPostParse", "Successful PostParse action") };
    }
}

[Keyword()]
public class SimpleSemanticCheckKeyword : Keyword
{
    public SimpleSemanticCheckKeyword()
    {
        SemanticCheck = ThrowError;
    }

    private static ParseError[] ThrowError(DynamicKeywordStatementAst kwAst)
    {
        return new [] { new ParseError(kwAst.Extent, "SuccessfulSemanticCheck", "Successful SemanticCheck action") };
    }
}

[Keyword()]
public class AstManipulationPreParseKeyword : Keyword
{
    public AstManipulationPreParseKeyword()
    {
        PreParse = AddGreetingParameter;
    }

    public static ParseError[] AddGreetingParameter(DynamicKeyword keyword)
    {
        var newParam = new DynamicKeywordParameter()
        {
            Name = "Greeting",
            TypeConstraint = "System.String",
        };

        if (!keyword.Parameters.ContainsKey(newParam.Name))
        {
            keyword.Parameters.Add(newParam.Name, newParam);
        }

        return null;
    }
}

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class AstManipulationPostParseKeyword : Keyword
{
    public AstManipulationPostParseKeyword()
    {
        PostParse = GetFirstStringInBodyAsError;
    }

    public static ParseError[] GetFirstStringInBodyAsError(DynamicKeywordStatementAst kwAst)
    {
        StringConstantExpressionAst strAst = (StringConstantExpressionAst)kwAst.CommandElements[kwAst.CommandElements.Count-1].Find(ast => ast is StringConstantExpressionAst, true);

        return new [] { new ParseError(kwAst.Extent, strAst.Value, strAst.Value) };
    }
}

[Keyword(Body = DynamicKeywordBodyMode.ScriptBlock)]
public class AstManipulationSemanticCheckKeyword : Keyword
{
    public AstManipulationSemanticCheckKeyword()
    {
        SemanticCheck = GetFirstStringInBodyAsError;
    }

    public static ParseError[] GetFirstStringInBodyAsError(DynamicKeywordStatementAst kwAst)
    {
        StringConstantExpressionAst strAst = (StringConstantExpressionAst)kwAst.CommandElements[kwAst.CommandElements.Count-1].Find(ast => ast is StringConstantExpressionAst, true);

        return new [] { new ParseError(kwAst.Extent, strAst.Value, strAst.Value) };
    }
}