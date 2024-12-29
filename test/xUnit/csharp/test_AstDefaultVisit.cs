// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using Xunit;

namespace PSTests.Parallel
{
    internal class MyICustomAstVisitor2 : ICustomAstVisitor2
    {
        public object DefaultVisit(Ast ast) => ast.GetType().Name;
    }

    internal class MyDefaultCustomAstVisitor2 : DefaultCustomAstVisitor2
    {
        public override object DefaultVisit(Ast ast) => ast.GetType().Name;
    }

    internal class MyAstVisitor2 : AstVisitor2
    {
        public List<string> Commands { get; }

        public MyAstVisitor2()
        {
            Commands = new List<string>();
        }

        public override AstVisitAction DefaultVisit(Ast ast)
        {
            if (ast is CommandAst commandAst && commandAst.CommandElements[0] is StringConstantExpressionAst str)
            {
                Commands.Add(str.Value);
            }

            return AstVisitAction.Continue;
        }
    }

    public static class AstDefaultVisitTests
    {
        [Fact]
        public static void TestCustomAstVisitor()
        {
            var ast = Parser.ParseInput("a | b", out _, out _);
            var expected = nameof(NamedBlockAst);

            var myVisitor1 = new MyICustomAstVisitor2();
            var result1 = ast.EndBlock.Accept(myVisitor1);
            Assert.Equal(expected, result1);

            var myVisitor2 = new MyDefaultCustomAstVisitor2();
            var result2 = ast.EndBlock.Accept(myVisitor2);
            Assert.Equal(expected, result2);
        }

        [Fact]
        public static void TestAstVisitor()
        {
            var ast = Parser.ParseInput("a | b", out _, out _);
            var myVisitor = new MyAstVisitor2();

            ast.Visit(myVisitor);
            Assert.Equal(2, myVisitor.Commands.Count);
            Assert.Equal("a", myVisitor.Commands[0]);
            Assert.Equal("b", myVisitor.Commands[1]);
        }
    }
}
