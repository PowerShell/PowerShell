// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using Dbg = System.Management.Automation;
using System.Globalization;

namespace System.Management.Automation
{
    /// <summary>
    /// Takes as input a collection of strings and builds an expression tree from the input.
    /// At the evaluation stage, it walks down the tree and evaluates the result.
    /// </summary>
    public sealed class FlagsExpression<T> where T : struct, IConvertible
    {
        #region Constructors

        /// <summary>
        /// Construct the expression from a single string.
        /// </summary>
        /// <param name="expression">
        /// The specified flag attribute expression string.
        /// </param>
        public FlagsExpression(string expression)
        {
            if (!typeof(T).IsEnum)
            {
                throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                    null, "InvalidGenericType", EnumExpressionEvaluatorStrings.InvalidGenericType);
            }

            _underType = Enum.GetUnderlyingType(typeof(T));

            if (string.IsNullOrWhiteSpace(expression))
            {
                throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                    null, "EmptyInputString", EnumExpressionEvaluatorStrings.EmptyInputString);
            }

            List<Token> tokenList = TokenizeInput(expression);
            // Append an OR at the end of the list for construction
            tokenList.Add(new Token(TokenKind.Or));

            CheckSyntaxError(tokenList);

            Root = ConstructExpressionTree(tokenList);
        }

        /// <summary>
        /// Construct the tree from an object collection when arguments are comma separated.
        /// If valid, all elements are OR separated.
        /// </summary>
        /// <param name="expression">
        /// The array of specified flag attribute subexpression strings.
        /// </param>
        public FlagsExpression(object[] expression)
        {
            if (!typeof(T).IsEnum)
            {
                throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                    null, "InvalidGenericType", EnumExpressionEvaluatorStrings.InvalidGenericType);
            }

            _underType = Enum.GetUnderlyingType(typeof(T));

            if (expression == null)
            {
                throw InterpreterError.NewInterpreterException(null, typeof(ArgumentNullException),
                    null, "EmptyInputString", EnumExpressionEvaluatorStrings.EmptyInputString);
            }

            foreach (string inputClause in expression)
            {
                if (string.IsNullOrWhiteSpace(inputClause))
                {
                    throw InterpreterError.NewInterpreterException(expression, typeof(RuntimeException),
                        null, "EmptyInputString", EnumExpressionEvaluatorStrings.EmptyInputString);
                }
            }

            List<Token> tokenList = new List<Token>();

            foreach (string orClause in expression)
            {
                tokenList.AddRange(TokenizeInput(orClause));
                tokenList.Add(new Token(TokenKind.Or));
            }
            // Unnecessary OR at the end not removed for tree construction

            Debug.Assert(tokenList.Count > 0, "Input must not all be white characters.");

            CheckSyntaxError(tokenList);

            Root = ConstructExpressionTree(tokenList);
        }

        #endregion

        #region parser tokens

        internal enum TokenKind
        {
            Identifier,
            And,
            Or,
            Not
        }

        internal class Token
        {
            public string Text { get; set; }

            public TokenKind Kind { get; set; }

            internal Token(TokenKind kind)
            {
                Kind = kind;
                switch (kind)
                {
                    case TokenKind.Or:
                        Text = "OR";
                        break;
                    case TokenKind.And:
                        Text = "AND";
                        break;
                    case TokenKind.Not:
                        Text = "NOT";
                        break;
                    default:
                        Debug.Assert(false, "Invalid token kind passed in.");
                        break;
                }
            }

            internal Token(string identifier)
            {
                Kind = TokenKind.Identifier;
                Text = identifier;
            }
        }

        #endregion

        #region tree nodes

        /// <summary>
        /// Abstract base type for other types of nodes in the tree.
        /// </summary>
        internal abstract class Node
        {
            // Only used in internal nodes holding operators.

            public Node Operand1 { get; set; }

            internal abstract bool Eval(object val);
            internal abstract bool ExistEnum(object enumVal);
        }

        /// <summary>
        /// OR node for attributes separated by a comma.
        /// </summary>
        internal class OrNode : Node
        {
            public Node Operand2 { get; set; }

            public OrNode(Node n)
            {
                Operand2 = n;
            }

            internal override bool Eval(object val)
            {
                // bitwise OR
                bool satisfy = Operand1.Eval(val) || Operand2.Eval(val);
                return satisfy;
            }

            internal override bool ExistEnum(object enumVal)
            {
                bool exist = Operand1.ExistEnum(enumVal) || Operand2.ExistEnum(enumVal);
                return exist;
            }
        }

        /// <summary>
        /// AND node for attributes separated by a plus(+) operator.
        /// </summary>
        internal class AndNode : Node
        {
            public Node Operand2 { get; set; }

            public AndNode(Node n)
            {
                Operand2 = n;
            }

            internal override bool Eval(object val)
            {
                // bitwise AND
                bool satisfy = Operand1.Eval(val) && Operand2.Eval(val);
                return satisfy;
            }

            internal override bool ExistEnum(object enumVal)
            {
                bool exist = Operand1.ExistEnum(enumVal) || Operand2.ExistEnum(enumVal);
                return exist;
            }
        }

        /// <summary>
        /// NOT node for attribute preceded by an exclamation(!) operator.
        /// </summary>
        internal class NotNode : Node
        {
            internal override bool Eval(object val)
            {
                // bitwise NOT
                bool satisfy = !(Operand1.Eval(val));
                return satisfy;
            }

            internal override bool ExistEnum(object enumVal)
            {
                bool exist = Operand1.ExistEnum(enumVal);
                return exist;
            }
        }

        /// <summary>
        /// Leaf nodes of the expression tree.
        /// </summary>
        internal class OperandNode : Node
        {
            internal object _operandValue;

            public object OperandValue
            {
                get
                {
                    return _operandValue;
                }

                set
                {
                    _operandValue = value;
                }
            }

            /// <summary>
            /// Takes a string value and converts to corresponding enum value.
            /// The string value should be checked at parsing stage prior to
            /// tree construction to ensure it is valid.
            /// </summary>
            internal OperandNode(string enumString)
            {
                Type enumType = typeof(T);
                Type underType = Enum.GetUnderlyingType(enumType);
                FieldInfo enumItem = enumType.GetField(enumString);
                _operandValue = LanguagePrimitives.ConvertTo(enumItem.GetValue(enumType), underType, CultureInfo.InvariantCulture);
            }

            internal override bool Eval(object val)
            {
                Type underType = Enum.GetUnderlyingType(typeof(T));
                // bitwise AND checking
                bool satisfy = false;
                if (isUnsigned(underType))
                {
                    ulong valueToCheck = (ulong)LanguagePrimitives.ConvertTo(val, typeof(ulong), CultureInfo.InvariantCulture);
                    ulong operandValue = (ulong)LanguagePrimitives.ConvertTo(_operandValue, typeof(ulong), CultureInfo.InvariantCulture);
                    satisfy = (operandValue == (valueToCheck & operandValue));
                }
                // allow for negative enum value input (though it's not recommended practice for flags attribute)
                else
                {
                    long valueToCheck = (long)LanguagePrimitives.ConvertTo(val, typeof(long), CultureInfo.InvariantCulture);
                    long operandValue = (long)LanguagePrimitives.ConvertTo(_operandValue, typeof(long), CultureInfo.InvariantCulture);
                    satisfy = (operandValue == (valueToCheck & operandValue));
                }

                return satisfy;
            }

            internal override bool ExistEnum(object enumVal)
            {
                Type underType = Enum.GetUnderlyingType(typeof(T));
                // bitwise AND checking
                bool exist = false;
                if (isUnsigned(underType))
                {
                    ulong valueToCheck = (ulong)LanguagePrimitives.ConvertTo(enumVal, typeof(ulong), CultureInfo.InvariantCulture);
                    ulong operandValue = (ulong)LanguagePrimitives.ConvertTo(_operandValue, typeof(ulong), CultureInfo.InvariantCulture);
                    exist = valueToCheck == (valueToCheck & operandValue);
                }
                // allow for negative enum value input (though it's not recommended practice for flags attribute)
                else
                {
                    long valueToCheck = (long)LanguagePrimitives.ConvertTo(enumVal, typeof(long), CultureInfo.InvariantCulture);
                    long operandValue = (long)LanguagePrimitives.ConvertTo(_operandValue, typeof(long), CultureInfo.InvariantCulture);
                    exist = valueToCheck == (valueToCheck & operandValue);
                }

                return exist;
            }

            private bool isUnsigned(Type type)
            {
                return (type == typeof(ulong) || type == typeof(uint) || type == typeof(ushort) || type == typeof(byte));
            }
        }

        #endregion

        #region private members

        private Type _underType = null;

        #endregion

        #region properties

        internal Node Root { get; set; } = null;

        #endregion

        #region public methods

        /// <summary>
        /// Evaluate a given flag enum value against the expression.
        /// </summary>
        /// <param name="value">
        /// The flag enum value to be evaluated.
        /// </param>
        /// <returns>
        /// Whether the enum value satisfy the expression.
        /// </returns>
        public bool Evaluate(T value)
        {
            object val = LanguagePrimitives.ConvertTo(value, _underType, CultureInfo.InvariantCulture);
            return Root.Eval(val);
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Given an enum element, check if the element is present in the expression tree,
        /// which is also present in the input expression.
        /// </summary>
        /// <param name="flagName">
        /// The enum element to be examined.
        /// </param>
        /// <returns>
        /// Whether the enum element is present in the expression.
        /// </returns>
        /// <remarks>
        /// The enum value passed in should be a single enum element value,
        /// not a flag enum value with multiple bits set.
        /// </remarks>
        internal bool ExistsInExpression(T flagName)
        {
            bool exist = false;
            object val = LanguagePrimitives.ConvertTo(flagName, _underType, CultureInfo.InvariantCulture);
            exist = Root.ExistEnum(val);
            return exist;
        }

        #endregion

        #region parser methods

        /// <summary>
        /// Takes a string of input tokenize into a list of ordered tokens.
        /// </summary>
        /// <param name="input">
        /// The input argument string,
        /// could be partial input (one element from the argument collection).
        /// </param>
        /// <returns>
        /// A generic list of tokenized input.
        /// </returns>
        private List<Token> TokenizeInput(string input)
        {
            List<Token> tokenList = new List<Token>();
            int _offset = 0;

            while (_offset < input.Length)
            {
                FindNextToken(input, ref _offset);
                if (_offset < input.Length)
                {
                    tokenList.Add(GetNextToken(input, ref _offset));
                }
            }

            return tokenList;
        }

        /// <summary>
        /// Find the start of the next token, skipping white spaces.
        /// </summary>
        /// <param name="input">
        /// Input string
        /// </param>
        /// <param name="_offset">
        /// Current offset position for the string parser.
        /// </param>
        private void FindNextToken(string input, ref int _offset)
        {
            while (_offset < input.Length)
            {
                char cc = input[_offset++];
                if (!char.IsWhiteSpace(cc))
                {
                    _offset--;
                    break;
                }
            }
        }

        /// <summary>
        /// Given the start (offset) of the next token, traverse through
        /// the string to find the next token, stripping correctly
        /// enclosed quotes.
        /// </summary>
        /// <param name="input">
        /// Input string
        /// </param>
        /// <param name="_offset">
        /// Current offset position for the string parser.
        /// </param>
        /// <returns>
        /// The next token on the input string
        /// </returns>
        private Token GetNextToken(string input, ref int _offset)
        {
            StringBuilder sb = new StringBuilder();
            // bool singleQuoted = false;
            // bool doubleQuoted = false;
            bool readingIdentifier = false;
            while (_offset < input.Length)
            {
                char cc = input[_offset++];
                if ((cc == ',') || (cc == '+') || (cc == '!'))
                {
                    if (!readingIdentifier)
                    {
                        sb.Append(cc);
                    }
                    else
                    {
                        _offset--;
                    }

                    break;
                }
                else
                {
                    sb.Append(cc);
                    readingIdentifier = true;
                }
            }

            string result = sb.ToString().Trim();
            // If resulting identifier is enclosed in paired quotes,
            // remove the only the first pair of quotes from the string
            if (result.Length >= 2 &&
                ((result[0] == '\'' && result[result.Length - 1] == '\'') ||
                (result[0] == '\"' && result[result.Length - 1] == '\"')))
            {
                result = result.Substring(1, result.Length - 2);
            }

            result = result.Trim();

            // possible empty token because white spaces are enclosed in quotation marks.
            if (string.IsNullOrWhiteSpace(result))
            {
                throw InterpreterError.NewInterpreterException(input, typeof(RuntimeException),
                    null, "EmptyTokenString", EnumExpressionEvaluatorStrings.EmptyTokenString,
                    EnumMinimumDisambiguation.EnumAllValues(typeof(T)));
            }
            else if (result[0] == '(')
            {
                int matchIndex = input.IndexOf(')', _offset);
                if (result[result.Length - 1] == ')' || matchIndex >= 0)
                {
                    throw InterpreterError.NewInterpreterException(input, typeof(RuntimeException),
                        null, "NoIdentifierGroupingAllowed", EnumExpressionEvaluatorStrings.NoIdentifierGroupingAllowed);
                }
            }

            if (result.Equals(","))
            {
                return (new Token(TokenKind.Or));
            }
            else if (result.Equals("+"))
            {
                return (new Token(TokenKind.And));
            }
            else if (result.Equals("!"))
            {
                return (new Token(TokenKind.Not));
            }
            else
            {
                return (new Token(result));
            }
        }

        /// <summary>
        /// Checks syntax errors on input expression,
        /// as well as performing disambiguation for identifiers.
        /// </summary>
        /// <param name="tokenList">
        /// A list of tokenized input.
        /// </param>
        private void CheckSyntaxError(List<Token> tokenList)
        {
            // Initialize, assuming preceded by OR
            TokenKind previous = TokenKind.Or;

            for (int i = 0; i < tokenList.Count; i++)
            {
                Token token = tokenList[i];
                // Not allowed: ... AND/OR AND/OR ...
                // Allowed: ... AND/OR NOT/ID ...
                if (previous == TokenKind.Or || previous == TokenKind.And)
                {
                    if ((token.Kind == TokenKind.Or) || (token.Kind == TokenKind.And))
                    {
                        throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                            null, "SyntaxErrorUnexpectedBinaryOperator", EnumExpressionEvaluatorStrings.SyntaxErrorUnexpectedBinaryOperator);
                    }
                }
                // Not allowed: ... NOT AND/OR/NOT ...
                // Allowed: ... NOT ID ...
                else if (previous == TokenKind.Not)
                {
                    if (token.Kind != TokenKind.Identifier)
                    {
                        throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                            null, "SyntaxErrorIdentifierExpected", EnumExpressionEvaluatorStrings.SyntaxErrorIdentifierExpected);
                    }
                }
                // Not allowed: ... ID NOT/ID ...
                // Allowed: ... ID AND/OR ...
                else if (previous == TokenKind.Identifier)
                {
                    if ((token.Kind == TokenKind.Identifier) || (token.Kind == TokenKind.Not))
                    {
                        throw InterpreterError.NewInterpreterException(null, typeof(RuntimeException),
                            null, "SyntaxErrorBinaryOperatorExpected", EnumExpressionEvaluatorStrings.SyntaxErrorBinaryOperatorExpected);
                    }
                }

                if (token.Kind == TokenKind.Identifier)
                {
                    string text = token.Text;
                    token.Text = EnumMinimumDisambiguation.EnumDisambiguate(text, typeof(T));
                }

                previous = token.Kind;
            }
        }

        /// <summary>
        /// Takes a list of tokenized input and create the corresponding expression tree.
        /// </summary>
        /// <param name="tokenList">
        /// Tokenized list of the input string.
        /// </param>
        private Node ConstructExpressionTree(List<Token> tokenList)
        {
            bool notFlag = false;
            Queue<Node> andQueue = new Queue<Node>();
            Queue<Node> orQueue = new Queue<Node>();

            for (int i = 0; i < tokenList.Count; i++)
            {
                Token token = tokenList[i];
                TokenKind kind = token.Kind;
                if (kind == TokenKind.Identifier)
                {
                    Node idNode = new OperandNode(token.Text);
                    if (notFlag)    // identifier preceded by NOT
                    {
                        Node notNode = new NotNode();
                        notNode.Operand1 = idNode;
                        notFlag = false;
                        andQueue.Enqueue(notNode);
                    }
                    else
                    {
                        andQueue.Enqueue(idNode);
                    }
                }
                else if (kind == TokenKind.Not)
                {
                    notFlag = true;
                }
                else if (kind == TokenKind.And)
                {
                    // do nothing
                }
                else if (kind == TokenKind.Or)
                {
                    // Dequeue all nodes from AND queue,
                    // create the AND tree, then add to the OR queue.
                    Node andCurrent = andQueue.Dequeue();
                    while (andQueue.Count > 0)
                    {
                        Node andNode = new AndNode(andCurrent);
                        andNode.Operand1 = andQueue.Dequeue();
                        andCurrent = andNode;
                    }

                    orQueue.Enqueue(andCurrent);
                }
            }

            // Dequeue all nodes from OR queue,
            // create the OR tree (final expression tree)
            Node orCurrent = orQueue.Dequeue();
            while (orQueue.Count > 0)
            {
                Node orNode = new OrNode(orCurrent);
                orNode.Operand1 = orQueue.Dequeue();
                orCurrent = orNode;
            }

            return orCurrent;
        }

        #endregion
    }
}
