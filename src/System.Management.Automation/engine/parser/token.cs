// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Text;

namespace System.Management.Automation.Language
{
    /// <summary>
    /// The specific kind of token.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    public enum TokenKind
    {
        // When adding any new tokens, be sure to update the following tables:
        //     * TokenTraits.StaticTokenFlags
        //     * TokenTraits.Text

        #region Unclassified Tokens

        /// <summary>An unknown token, signifies an error condition.</summary>
        Unknown = 0,

        /// <summary>
        /// A variable token, always begins with '$' and followed by the variable name, possibly enclose in curly braces.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.VariableToken"/>.
        /// </summary>
        Variable = 1,

        /// <summary>
        /// A splatted variable token, always begins with '@' and followed by the variable name.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.VariableToken"/>.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        SplattedVariable = 2,

        /// <summary>
        /// A parameter to a command, always begins with a dash ('-'), followed by the parameter name.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.ParameterToken"/>.
        /// </summary>
        Parameter = 3,

        /// <summary>
        /// Any numerical literal token.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.NumberToken"/>.
        /// </summary>
        Number = 4,

        /// <summary>
        /// A label token - always begins with ':', followed by the label name.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.LabelToken"/>.
        /// </summary>
        Label = 5,

        /// <summary>
        /// A simple identifier, always begins with a letter or '_', and is followed by letters, numbers, or '_'.
        /// </summary>
        Identifier = 6,

        /// <summary>
        /// A token that is only valid as a command name, command argument, function name, or configuration name.  It may contain
        /// characters not allowed in identifiers.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.StringLiteralToken"/>
        /// or <see cref="System.Management.Automation.Language.StringExpandableToken"/> if the token contains variable
        /// references or subexpressions.
        /// </summary>
        Generic = 7,

        /// <summary>A newline (one of '\n', '\r', or '\r\n').</summary>
        NewLine = 8,

        /// <summary>A line continuation (backtick followed by newline).</summary>
        LineContinuation = 9,

        /// <summary>A single line comment, or a delimited comment.</summary>
        Comment = 10,

        /// <summary>Marks the end of the input script or file.</summary>
        EndOfInput = 11,

        #endregion Unclassified Tokens

        #region Strings

        /// <summary>
        /// A single quoted string literal.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.StringLiteralToken"/>.
        /// </summary>
        StringLiteral = 12,

        /// <summary>
        /// A double quoted string literal.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.StringExpandableToken"/>
        /// even if there are no nested tokens to expand.
        /// </summary>
        StringExpandable = 13,

        /// <summary>
        /// A single quoted here string literal.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.StringLiteralToken"/>.
        /// </summary>
        HereStringLiteral = 14,

        /// <summary>
        /// A double quoted here string literal.
        /// Tokens with this kind are always instances of <see cref="System.Management.Automation.Language.StringExpandableToken"/>.
        /// even if there are no nested tokens to expand.
        /// </summary>
        HereStringExpandable = 15,

        #endregion Strings

        #region Punctuators

        /// <summary>The opening parenthesis token '('.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        LParen = 16,

        /// <summary>The closing parenthesis token ')'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        RParen = 17,

        /// <summary>The opening curly brace token '{'.</summary>
        LCurly = 18,

        /// <summary>The closing curly brace token '}'.</summary>
        RCurly = 19,

        /// <summary>The opening square brace token '['.</summary>
        LBracket = 20,

        /// <summary>The closing square brace token ']'.</summary>
        RBracket = 21,

        /// <summary>The opening token of an array expression '@('.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        AtParen = 22,

        /// <summary>The opening token of a hash expression '@{'.</summary>
        AtCurly = 23,

        /// <summary>The opening token of a sub-expression '$('.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        DollarParen = 24,

        /// <summary>The statement terminator ';'.</summary>
        Semi = 25,

        #endregion Punctuators

        #region Operators

        /// <summary>The (unimplemented) operator '&amp;&amp;'.</summary>
        AndAnd = 26,

        /// <summary>The (unimplemented) operator '||'.</summary>
        OrOr = 27,

        /// <summary>The invocation operator '&amp;'.</summary>
        Ampersand = 28,

        /// <summary>The pipe operator '|'.</summary>
        Pipe = 29,

        /// <summary>The unary or binary array operator ','.</summary>
        Comma = 30,

        /// <summary>The pre-decrement operator '--'.</summary>
        MinusMinus = 31,

        /// <summary>The pre-increment operator '++'.</summary>
        PlusPlus = 32,

        /// <summary>The range operator '..'.</summary>
        DotDot = 33,

        /// <summary>The static member access operator '::'.</summary>
        ColonColon = 34,

        /// <summary>The instance member access or dot source invocation operator '.'.</summary>
        Dot = 35,

        /// <summary>The logical not operator '!'.</summary>
        Exclaim = 36,

        /// <summary>The multiplication operator '*'.</summary>
        Multiply = 37,

        /// <summary>The division operator '/'.</summary>
        Divide = 38,

        /// <summary>The modulo division (remainder) operator '%'.</summary>
        Rem = 39,

        /// <summary>The addition operator '+'.</summary>
        Plus = 40,

        /// <summary>The subtraction operator '-'.</summary>
        Minus = 41,

        /// <summary>The assignment operator '='.</summary>
        Equals = 42,

        /// <summary>The addition assignment operator '+='.</summary>
        PlusEquals = 43,

        /// <summary>The subtraction assignment operator '-='.</summary>
        MinusEquals = 44,

        /// <summary>The multiplication assignment operator '*='.</summary>
        MultiplyEquals = 45,

        /// <summary>The division assignment operator '/='.</summary>
        DivideEquals = 46,

        /// <summary>The modulo division (remainder) assignment operator '%='.</summary>
        RemainderEquals = 47,

        /// <summary>A redirection operator such as '2>&amp;1' or '>>'.</summary>
        Redirection = 48,

        /// <summary>The (unimplemented) stdin redirection operator '&lt;'.</summary>
        RedirectInStd = 49,

        /// <summary>The string format operator '-f'.</summary>
        Format = 50,

        /// <summary>The logical not operator '-not'.</summary>
        Not = 51,

        /// <summary>The bitwise not operator '-bnot'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bnot = 52,

        /// <summary>The logical and operator '-and'.</summary>
        And = 53,

        /// <summary>The logical or operator '-or'.</summary>
        Or = 54,

        /// <summary>The logical exclusive or operator '-xor'.</summary>
        Xor = 55,

        /// <summary>The bitwise and operator '-band'.</summary>
        Band = 56,

        /// <summary>The bitwise or operator '-bor'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bor = 57,

        /// <summary>The bitwise exclusive or operator '-xor'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Bxor = 58,

        /// <summary>The join operator '-join'.</summary>
        Join = 59,

        /// <summary>The case insensitive equal operator '-ieq' or '-eq'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ieq = 60,

        /// <summary>The case insensitive not equal operator '-ine' or '-ne'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ine = 61,

        /// <summary>The case insensitive greater than or equal operator '-ige' or '-ge'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ige = 62,

        /// <summary>The case insensitive greater than operator '-igt' or '-gt'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Igt = 63,

        /// <summary>The case insensitive less than operator '-ilt' or '-lt'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ilt = 64,

        /// <summary>The case insensitive less than or equal operator '-ile' or '-le'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ile = 65,

        /// <summary>The case insensitive like operator '-ilike' or '-like'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ilike = 66,

        /// <summary>The case insensitive not like operator '-inotlike' or '-notlike'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotlike = 67,

        /// <summary>The case insensitive match operator '-imatch' or '-match'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Imatch = 68,

        /// <summary>The case insensitive not match operator '-inotmatch' or '-notmatch'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotmatch = 69,

        /// <summary>The case insensitive replace operator '-ireplace' or '-replace'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ireplace = 70,

        /// <summary>The case insensitive contains operator '-icontains' or '-contains'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Icontains = 71,

        /// <summary>The case insensitive notcontains operator '-inotcontains' or '-notcontains'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotcontains = 72,

        /// <summary>The case insensitive in operator '-iin' or '-in'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Iin = 73,

        /// <summary>The case insensitive notin operator '-inotin' or '-notin'</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Inotin = 74,

        /// <summary>The case insensitive split operator '-isplit' or '-split'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Isplit = 75,

        /// <summary>The case sensitive equal operator '-ceq'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ceq = 76,

        /// <summary>The case sensitive not equal operator '-cne'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cne = 77,

        /// <summary>The case sensitive greater than or equal operator '-cge'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cge = 78,

        /// <summary>The case sensitive greater than operator '-cgt'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cgt = 79,

        /// <summary>The case sensitive less than operator '-clt'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Clt = 80,

        /// <summary>The case sensitive less than or equal operator '-cle'.</summary>
        Cle = 81,

        /// <summary>The case sensitive like operator '-clike'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Clike = 82,

        /// <summary>The case sensitive notlike operator '-cnotlike'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotlike = 83,

        /// <summary>The case sensitive match operator '-cmatch'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cmatch = 84,

        /// <summary>The case sensitive not match operator '-cnotmatch'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotmatch = 85,

        /// <summary>The case sensitive replace operator '-creplace'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Creplace = 86,

        /// <summary>The case sensitive contains operator '-ccontains'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Ccontains = 87,

        /// <summary>The case sensitive not contains operator '-cnotcontains'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotcontains = 88,

        /// <summary>The case sensitive in operator '-cin'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cin = 89,

        /// <summary>The case sensitive not in operator '-notin'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Cnotin = 90,

        /// <summary>The case sensitive split operator '-csplit'.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Csplit = 91,

        /// <summary>The type test operator '-is'.</summary>
        Is = 92,

        /// <summary>The type test operator '-isnot'.</summary>
        IsNot = 93,

        /// <summary>The type conversion operator '-as'.</summary>
        As = 94,

        /// <summary>The post-increment operator '++'.</summary>
        PostfixPlusPlus = 95,

        /// <summary>The post-decrement operator '--'.</summary>
        PostfixMinusMinus = 96,

        /// <summary>The shift left operator.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Shl = 97,

        /// <summary>The shift right operator.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Shr = 98,

        /// <summary>The PS class base class and implemented interfaces operator ':'. Also used in base class ctor calls.</summary>
        Colon = 99,

        /// <summary>The ternary operator '?'.</summary>
        QuestionMark = 100,

        /// <summary>The null conditional assignment operator '??='.</summary>
        QuestionQuestionEquals = 101,

        /// <summary>The null coalesce operator '??'.</summary>
        QuestionQuestion = 102,

        /// <summary>The null conditional member access operator '?.'.</summary>
        QuestionDot = 103,

        /// <summary>The null conditional index access operator '?[]'.</summary>
        QuestionLBracket = 104,

        #endregion Operators

        #region Keywords

        /// <summary>The 'begin' keyword.</summary>
        Begin = 119,

        /// <summary>The 'break' keyword.</summary>
        Break = 120,

        /// <summary>The 'catch' keyword.</summary>
        Catch = 121,

        /// <summary>The 'class' keyword.</summary>
        Class = 122,

        /// <summary>The 'continue' keyword.</summary>
        Continue = 123,

        /// <summary>The 'data' keyword.</summary>
        Data = 124,

        /// <summary>The (unimplemented) 'define' keyword.</summary>
        Define = 125,

        /// <summary>The 'do' keyword.</summary>
        Do = 126,

        /// <summary>The 'dynamicparam' keyword.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Dynamicparam = 127,

        /// <summary>The 'else' keyword.</summary>
        Else = 128,

        /// <summary>The 'elseif' keyword.</summary>
        ElseIf = 129,

        /// <summary>The 'end' keyword.</summary>
        End = 130,

        /// <summary>The 'exit' keyword.</summary>
        Exit = 131,

        /// <summary>The 'filter' keyword.</summary>
        Filter = 132,

        /// <summary>The 'finally' keyword.</summary>
        Finally = 133,

        /// <summary>The 'for' keyword.</summary>
        For = 134,

        /// <summary>The 'foreach' keyword.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Foreach = 135,

        /// <summary>The (unimplemented) 'from' keyword.</summary>
        From = 136,

        /// <summary>The 'function' keyword.</summary>
        Function = 137,

        /// <summary>The 'if' keyword.</summary>
        If = 138,

        /// <summary>The 'in' keyword.</summary>
        In = 139,

        /// <summary>The 'param' keyword.</summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        Param = 140,

        /// <summary>The 'process' keyword.</summary>
        Process = 141,

        /// <summary>The 'return' keyword.</summary>
        Return = 142,

        /// <summary>The 'switch' keyword.</summary>
        Switch = 143,

        /// <summary>The 'throw' keyword.</summary>
        Throw = 144,

        /// <summary>The 'trap' keyword.</summary>
        Trap = 145,

        /// <summary>The 'try' keyword.</summary>
        Try = 146,

        /// <summary>The 'until' keyword.</summary>
        Until = 147,

        /// <summary>The (unimplemented) 'using' keyword.</summary>
        Using = 148,

        /// <summary>The (unimplemented) 'var' keyword.</summary>
        Var = 149,

        /// <summary>The 'while' keyword.</summary>
        While = 150,

        /// <summary>The 'workflow' keyword.</summary>
        Workflow = 151,

        /// <summary>The 'parallel' keyword.</summary>
        Parallel = 152,

        /// <summary>The 'sequence' keyword.</summary>
        Sequence = 153,

        /// <summary>The 'InlineScript' keyword</summary>
        InlineScript = 154,

        /// <summary>The "configuration" keyword</summary>
        Configuration = 155,

        /// <summary>The token kind for dynamic keywords</summary>
        DynamicKeyword = 156,

        /// <summary>The 'public' keyword</summary>
        Public = 157,

        /// <summary>The 'private' keyword</summary>
        Private = 158,

        /// <summary>The 'static' keyword</summary>
        Static = 159,

        /// <summary>The 'interface' keyword</summary>
        Interface = 160,

        /// <summary>The 'enum' keyword</summary>
        Enum = 161,

        /// <summary>The 'namespace' keyword</summary>
        Namespace = 162,

        /// <summary>The 'module' keyword</summary>
        Module = 163,

        /// <summary>The 'type' keyword</summary>
        Type = 164,

        /// <summary>The 'assembly' keyword</summary>
        Assembly = 165,

        /// <summary>The 'command' keyword</summary>
        Command = 166,

        /// <summary>The 'hidden' keyword</summary>
        Hidden = 167,

        /// <summary>The 'base' keyword</summary>
        Base = 168,

        /// <summary>The 'default' keyword</summary>
        Default = 169,

        /// <summary>The 'clean' keyword.</summary>
        Clean = 170,

        #endregion Keywords
    }

    /// <summary>
    /// Flags that specify additional information about a given token.
    /// </summary>
    [Flags]
    public enum TokenFlags
    {
        /// <summary>
        /// The token has no flags.
        /// </summary>
        None = 0x00000000,

        #region Precedence Values

        /// <summary>
        /// The precedence of the logical operators '-and', '-or', and '-xor'.
        /// </summary>
        BinaryPrecedenceLogical = 0x1,

        /// <summary>
        /// The precedence of the bitwise operators '-band', '-bor', and '-bxor'
        /// </summary>
        BinaryPrecedenceBitwise = 0x2,

        /// <summary>
        /// The precedence of comparison operators including: '-eq', '-ne', '-ge', '-gt', '-lt', '-le', '-like', '-notlike',
        /// '-match', '-notmatch', '-replace', '-contains', '-notcontains', '-in', '-notin', '-split', '-join', '-is', '-isnot', '-as',
        /// and all of the case sensitive variants of these operators, if they exists.
        /// </summary>
        BinaryPrecedenceComparison = 0x5,

        /// <summary>
        /// The precedence of null coalesce operator '??'.
        /// </summary>
        BinaryPrecedenceCoalesce = 0x7,

        /// <summary>
        /// The precedence of the binary operators '+' and '-'.
        /// </summary>
        BinaryPrecedenceAdd = 0x9,

        /// <summary>
        /// The precedence of the operators '*', '/', and '%'.
        /// </summary>
        BinaryPrecedenceMultiply = 0xa,

        /// <summary>
        /// The precedence of the '-f' operator.
        /// </summary>
        BinaryPrecedenceFormat = 0xc,

        /// <summary>
        /// The precedence of the '..' operator.
        /// </summary>
        BinaryPrecedenceRange = 0xd,

        #endregion Precedence Values

        /// <summary>
        /// A bitmask to get the precedence of binary operators.
        /// </summary>
        BinaryPrecedenceMask = 0x0000000f,

        /// <summary>
        /// The token is a keyword.
        /// </summary>
        Keyword = 0x00000010,

        /// <summary>
        /// The token is one of the keywords that is a part of a script block: 'begin', 'process', 'end', 'clean', or 'dynamicparam'.
        /// </summary>
        ScriptBlockBlockName = 0x00000020,

        /// <summary>
        /// The token is a binary operator.
        /// </summary>
        BinaryOperator = 0x00000100,

        /// <summary>
        /// The token is a unary operator.
        /// </summary>
        UnaryOperator = 0x00000200,

        /// <summary>
        /// The token is a case sensitive operator such as '-ceq' or '-ccontains'.
        /// </summary>
        CaseSensitiveOperator = 0x00000400,

        /// <summary>
        /// The token is a ternary operator '?'.
        /// </summary>
        TernaryOperator = 0x00000800,

        /// <summary>
        /// The operators '&amp;', '|', and the member access operators ':' and '::'.
        /// </summary>
        SpecialOperator = 0x00001000,

        /// <summary>
        /// The token is one of the assignment operators: '=', '+=', '-=', '*=', '/=', '%=' or '??='
        /// </summary>
        AssignmentOperator = 0x00002000,

        /// <summary>
        /// The token is scanned identically in expression mode or command mode.
        /// </summary>
        ParseModeInvariant = 0x00008000,

        /// <summary>
        /// The token has some error associated with it.  For example, it may be a string missing it's terminator.
        /// </summary>
        TokenInError = 0x00010000,

        /// <summary>
        /// The operator is not allowed in restricted language mode or in the data language.
        /// </summary>
        DisallowedInRestrictedMode = 0x00020000,

        /// <summary>
        /// The token is either a prefix or postfix '++' or '--'.
        /// </summary>
        PrefixOrPostfixOperator = 0x00040000,

        /// <summary>
        /// The token names a command in a pipeline.
        /// </summary>
        CommandName = 0x00080000,

        /// <summary>
        /// The token names a member of a class.
        /// </summary>
        MemberName = 0x00100000,

        /// <summary>
        /// The token names a type.
        /// </summary>
        TypeName = 0x00200000,

        /// <summary>
        /// The token names an attribute.
        /// </summary>
        AttributeName = 0x00400000,

        /// <summary>
        /// The token is a valid operator to use when doing constant folding.
        /// </summary>
        // Some operators that could be marked with this flag aren't because the current implementation depends
        // on the execution context (e.g. -split, -join, -like, etc.)
        // If the operator is culture sensitive (e.g. -f), then it shouldn't be marked as suitable for constant
        // folding because evaluation of the operator could differ if the thread's culture changes.
        CanConstantFold = 0x00800000,

        /// <summary>
        /// The token is a statement but does not support attributes.
        /// </summary>
        StatementDoesntSupportAttributes = 0x01000000,
    }

    /// <summary>
    /// A utility class to get statically known traits and invariant traits about PowerShell tokens.
    /// </summary>
    public static class TokenTraits
    {
        private static readonly TokenFlags[] s_staticTokenFlags = new TokenFlags[]
        {
            #region Flags for unclassified tokens

            /*              Unknown */ TokenFlags.None,
            /*             Variable */ TokenFlags.None,
            /*     SplattedVariable */ TokenFlags.None,
            /*            Parameter */ TokenFlags.None,
            /*               Number */ TokenFlags.None,
            /*                Label */ TokenFlags.None,
            /*           Identifier */ TokenFlags.None,

            /*              Generic */ TokenFlags.None,
            /*              Newline */ TokenFlags.ParseModeInvariant,
            /*     LineContinuation */ TokenFlags.ParseModeInvariant,
            /*              Comment */ TokenFlags.ParseModeInvariant,
            /*           EndOfInput */ TokenFlags.ParseModeInvariant,

            #endregion Flags for unclassified tokens

            #region Flags for strings

            /*        StringLiteral */ TokenFlags.ParseModeInvariant,
            /*     StringExpandable */ TokenFlags.ParseModeInvariant,
            /*    HereStringLiteral */ TokenFlags.ParseModeInvariant,
            /* HereStringExpandable */ TokenFlags.ParseModeInvariant,

            #endregion Flags for strings

            #region Flags for punctuators

            /*               LParen */ TokenFlags.ParseModeInvariant,
            /*               RParen */ TokenFlags.ParseModeInvariant,
            /*               LCurly */ TokenFlags.ParseModeInvariant,
            /*               RCurly */ TokenFlags.ParseModeInvariant,
            /*             LBracket */ TokenFlags.None,
            /*             RBracket */ TokenFlags.ParseModeInvariant,
            /*              AtParen */ TokenFlags.ParseModeInvariant,
            /*              AtCurly */ TokenFlags.ParseModeInvariant,
            /*          DollarParen */ TokenFlags.ParseModeInvariant,
            /*                 Semi */ TokenFlags.ParseModeInvariant,

            #endregion Flags for punctuators

            #region Flags for operators

            /*               AndAnd */ TokenFlags.ParseModeInvariant,
            /*                 OrOr */ TokenFlags.ParseModeInvariant,
            /*            Ampersand */ TokenFlags.SpecialOperator | TokenFlags.ParseModeInvariant,
            /*                 Pipe */ TokenFlags.SpecialOperator | TokenFlags.ParseModeInvariant,
            /*                Comma */ TokenFlags.UnaryOperator | TokenFlags.ParseModeInvariant,
            /*           MinusMinus */ TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
            /*             PlusPlus */ TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
            /*               DotDot */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceRange | TokenFlags.DisallowedInRestrictedMode,
            /*           ColonColon */ TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
            /*                  Dot */ TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
            /*              Exclaim */ TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
            /*             Multiply */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
            /*               Divide */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
            /*                  Rem */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceMultiply | TokenFlags.CanConstantFold,
            /*                 Plus */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceAdd | TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
            /*                Minus */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceAdd | TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
            /*               Equals */ TokenFlags.AssignmentOperator,
            /*           PlusEquals */ TokenFlags.AssignmentOperator,
            /*          MinusEquals */ TokenFlags.AssignmentOperator,
            /*       MultiplyEquals */ TokenFlags.AssignmentOperator,
            /*         DivideEquals */ TokenFlags.AssignmentOperator,
            /*      RemainderEquals */ TokenFlags.AssignmentOperator,
            /*          Redirection */ TokenFlags.DisallowedInRestrictedMode,
            /*        RedirectInStd */ TokenFlags.ParseModeInvariant | TokenFlags.DisallowedInRestrictedMode,
            /*               Format */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceFormat | TokenFlags.DisallowedInRestrictedMode,
            /*                  Not */ TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
            /*                 Bnot */ TokenFlags.UnaryOperator | TokenFlags.CanConstantFold,
            /*                  And */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
            /*                   Or */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
            /*                  Xor */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceLogical | TokenFlags.CanConstantFold,
            /*                 Band */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
            /*                  Bor */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
            /*                 Bxor */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceBitwise | TokenFlags.CanConstantFold,
            /*                 Join */ TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*                  Ieq */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Ine */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Ige */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Igt */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Ilt */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Ile */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                Ilike */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*             Inotlike */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*               Imatch */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*            Inotmatch */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*             Ireplace */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*            Icontains */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*         Inotcontains */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                  Iin */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*               Inotin */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*               Isplit */ TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*                  Ceq */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Cne */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Cge */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Cgt */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Clt */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Cle */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                Clike */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*             Cnotlike */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*               Cmatch */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
            /*            Cnotmatch */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
            /*             Creplace */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
            /*            Ccontains */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*         Cnotcontains */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*                  Cin */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*               Cnotin */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator,
            /*               Csplit */ TokenFlags.BinaryOperator | TokenFlags.UnaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CaseSensitiveOperator | TokenFlags.DisallowedInRestrictedMode,
            /*                   Is */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                IsNot */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison,
            /*                   As */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.DisallowedInRestrictedMode,
            /*      PostFixPlusPlus */ TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
            /*    PostFixMinusMinus */ TokenFlags.UnaryOperator | TokenFlags.PrefixOrPostfixOperator | TokenFlags.DisallowedInRestrictedMode,
            /*                  Shl */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold,
            /*                  Shr */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold,
            /*                Colon */ TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
            /*         QuestionMark */ TokenFlags.TernaryOperator | TokenFlags.DisallowedInRestrictedMode,
          /* QuestionQuestionEquals */ TokenFlags.AssignmentOperator,
            /*     QuestionQuestion */ TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceCoalesce,
            /*          QuestionDot */ TokenFlags.SpecialOperator | TokenFlags.DisallowedInRestrictedMode,
            /*     QuestionLBracket */ TokenFlags.None,
            /*     Reserved slot 7  */ TokenFlags.None,
            /*     Reserved slot 8  */ TokenFlags.None,
            /*     Reserved slot 9  */ TokenFlags.None,
            /*     Reserved slot 10 */ TokenFlags.None,
            /*     Reserved slot 11 */ TokenFlags.None,
            /*     Reserved slot 12 */ TokenFlags.None,
            /*     Reserved slot 13 */ TokenFlags.None,
            /*     Reserved slot 14 */ TokenFlags.None,
            /*     Reserved slot 15 */ TokenFlags.None,
            /*     Reserved slot 16 */ TokenFlags.None,
            /*     Reserved slot 17 */ TokenFlags.None,
            /*     Reserved slot 18 */ TokenFlags.None,
            /*     Reserved slot 19 */ TokenFlags.None,
            /*     Reserved slot 20 */ TokenFlags.None,

            #endregion Flags for operators

            #region Flags for keywords

            /*                Begin */ TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
            /*                Break */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                Catch */ TokenFlags.Keyword,
            /*                Class */ TokenFlags.Keyword,
            /*             Continue */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                 Data */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*               Define */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                   Do */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*         Dynamicparam */ TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
            /*                 Else */ TokenFlags.Keyword,
            /*               ElseIf */ TokenFlags.Keyword,
            /*                  End */ TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
            /*                 Exit */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*               Filter */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*              Finally */ TokenFlags.Keyword,
            /*                  For */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*              Foreach */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                 From */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*             Function */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                   If */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                   In */ TokenFlags.Keyword,
            /*                Param */ TokenFlags.Keyword,
            /*              Process */ TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,
            /*               Return */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*               Switch */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                Throw */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                 Trap */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                  Try */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                Until */ TokenFlags.Keyword,
            /*                Using */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                  Var */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*                While */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*             Workflow */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*             Parallel */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*             Sequence */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*         InlineScript */ TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes,
            /*        Configuration */ TokenFlags.Keyword,
            /*    <dynamic keyword> */ TokenFlags.Keyword,
            /*               Public */ TokenFlags.Keyword,
            /*              Private */ TokenFlags.Keyword,
            /*               Static */ TokenFlags.Keyword,
            /*            Interface */ TokenFlags.Keyword,
            /*                 Enum */ TokenFlags.Keyword,
            /*            Namespace */ TokenFlags.Keyword,
            /*               Module */ TokenFlags.Keyword,
            /*                 Type */ TokenFlags.Keyword,
            /*             Assembly */ TokenFlags.Keyword,
            /*              Command */ TokenFlags.Keyword,
            /*               Hidden */ TokenFlags.Keyword,
            /*                 Base */ TokenFlags.Keyword,
            /*              Default */ TokenFlags.Keyword,
            /*                Clean */ TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName,

            #endregion Flags for keywords
        };

        private static readonly string[] s_tokenText = new string[]
        {
            #region Text for unclassified tokens

            /*              Unknown */ "unknown",
            /*             Variable */ "var",
            /*     SplattedVariable */ "@var",
            /*            Parameter */ "param",
            /*               Number */ "number",
            /*                Label */ "label",
            /*           Identifier */ "ident",

            /*              Generic */ "generic",
            /*              Newline */ "newline",
            /*     LineContinuation */ "line continuation",
            /*              Comment */ "comment",
            /*           EndOfInput */ "eof",

            #endregion Text for unclassified tokens

            #region Text for strings

            /*        StringLiteral */ "sqstr",
            /*     StringExpandable */ "dqstr",
            /*    HereStringLiteral */ "sq here string",
            /* HereStringExpandable */ "dq here string",

            #endregion Text for strings

            #region Text for punctuators

            /*               LParen */ "(",
            /*               RParen */ ")",
            /*               LCurly */ "{",
            /*               RCurly */ "}",
            /*             LBracket */ "[",
            /*             RBracket */ "]",
            /*              AtParen */ "@(",
            /*              AtCurly */ "@{",
            /*          DollarParen */ "$(",
            /*                 Semi */ ";",

            #endregion Text for punctuators

            #region Text for operators

            /*               AndAnd */ "&&",
            /*                 OrOr */ "||",
            /*            Ampersand */ "&",
            /*                 Pipe */ "|",
            /*                Comma */ ",",
            /*           MinusMinus */ "--",
            /*             PlusPlus */ "++",
            /*               DotDot */ "..",
            /*           ColonColon */ "::",
            /*                  Dot */ ".",
            /*              Exclaim */ "!",
            /*             Multiply */ "*",
            /*               Divide */ "/",
            /*                  Rem */ "%",
            /*                 Plus */ "+",
            /*                Minus */ "-",
            /*               Equals */ "=",
            /*           PlusEquals */ "+=",
            /*          MinusEquals */ "-=",
            /*       MultiplyEquals */ "*=",
            /*         DivideEquals */ "/=",
            /*      RemainderEquals */ "%=",
            /*          Redirection */ "redirection",
            /*        RedirectInStd */ "<",
            /*               Format */ "-f",
            /*                  Not */ "-not",
            /*                 Bnot */ "-bnot",
            /*                  And */ "-and",
            /*                   Or */ "-or",
            /*                  Xor */ "-xor",
            /*                 Band */ "-band",
            /*                  Bor */ "-bor",
            /*                 Bxor */ "-bxor",
            /*                 Join */ "-join",
            /*                  Ieq */ "-eq",
            /*                  Ine */ "-ne",
            /*                  Ige */ "-ge",
            /*                  Igt */ "-gt",
            /*                  Ilt */ "-lt",
            /*                  Ile */ "-le",
            /*                Ilike */ "-ilike",
            /*             Inotlike */ "-inotlike",
            /*               Imatch */ "-imatch",
            /*            Inotmatch */ "-inotmatch",
            /*             Ireplace */ "-ireplace",
            /*            Icontains */ "-icontains",
            /*         Inotcontains */ "-inotcontains",
            /*                  Iin */ "-iin",
            /*               Inotin */ "-inotin",
            /*               Isplit */ "-isplit",
            /*                  Ceq */ "-ceq",
            /*                  Cne */ "-cne",
            /*                  Cge */ "-cge",
            /*                  Cgt */ "-cgt",
            /*                  Clt */ "-clt",
            /*                  Cle */ "-cle",
            /*                Clike */ "-clike",
            /*             Cnotlike */ "-cnotlike",
            /*               Cmatch */ "-cmatch",
            /*            Cnotmatch */ "-cnotmatch",
            /*             Creplace */ "-creplace",
            /*            Ccontains */ "-ccontains",
            /*         Cnotcontains */ "-cnotcontains",
            /*                  Cin */ "-cin",
            /*               Cnotin */ "-cnotin",
            /*               Csplit */ "-csplit",
            /*                   Is */ "-is",
            /*                IsNot */ "-isnot",
            /*                   As */ "-as",
            /*      PostFixPlusPlus */ "++",
            /*    PostFixMinusMinus */ "--",
            /*                  Shl */ "-shl",
            /*                  Shr */ "-shr",
            /*                Colon */ ":",
            /*         QuestionMark */ "?",
          /* QuestionQuestionEquals */ "??=",
            /*     QuestionQuestion */ "??",
            /*          QuestionDot */ "?.",
            /*     QuestionLBracket */ "?[",
            /*    Reserved slot 7   */ string.Empty,
            /*    Reserved slot 8   */ string.Empty,
            /*    Reserved slot 9   */ string.Empty,
            /*    Reserved slot 10  */ string.Empty,
            /*    Reserved slot 11  */ string.Empty,
            /*    Reserved slot 12  */ string.Empty,
            /*    Reserved slot 13  */ string.Empty,
            /*    Reserved slot 14  */ string.Empty,
            /*    Reserved slot 15  */ string.Empty,
            /*    Reserved slot 16  */ string.Empty,
            /*    Reserved slot 17  */ string.Empty,
            /*    Reserved slot 18  */ string.Empty,
            /*    Reserved slot 19  */ string.Empty,
            /*    Reserved slot 20  */ string.Empty,

            #endregion Text for operators

            #region Text for keywords

            /*                Begin */ "begin",
            /*                Break */ "break",
            /*                Catch */ "catch",
            /*                Class */ "class",
            /*             Continue */ "continue",
            /*                 Data */ "data",
            /*               Define */ "define",
            /*                   Do */ "do",
            /*         Dynamicparam */ "dynamicparam",
            /*                 Else */ "else",
            /*               ElseIf */ "elseif",
            /*                  End */ "end",
            /*                 Exit */ "exit",
            /*               Filter */ "filter",
            /*              Finally */ "finally",
            /*                  For */ "for",
            /*              Foreach */ "foreach",
            /*                 From */ "from",
            /*             Function */ "function",
            /*                   If */ "if",
            /*                   In */ "in",
            /*                Param */ "param",
            /*              Process */ "process",
            /*               Return */ "return",
            /*               Switch */ "switch",
            /*                Throw */ "throw",
            /*                 Trap */ "trap",
            /*                  Try */ "try",
            /*                Until */ "until",
            /*                Using */ "using",
            /*                  Var */ "var",
            /*                While */ "while",
            /*             Workflow */ "workflow",
            /*             Parallel */ "parallel",
            /*             Sequence */ "sequence",
            /*         InlineScript */ "inlinescript",
            /*        Configuration */ "configuration",
            /*    <dynamic keyword> */ "<dynamic keyword>",
            /*               Public */ "public",
            /*              Private */ "private",
            /*               Static */ "static",
            /*            Interface */ "interface",
            /*                 Enum */ "enum",
            /*            Namespace */ "namespace",
            /*               Module */ "module",
            /*                 Type */ "type",
            /*             Assembly */ "assembly",
            /*              Command */ "command",
            /*               Hidden */ "hidden",
            /*                 Base */ "base",
            /*              Default */ "default",
            /*                Clean */ "clean",

            #endregion Text for keywords
        };

#if DEBUG
        static TokenTraits()
        {
            Diagnostics.Assert(
                s_staticTokenFlags.Length == ((int)TokenKind.Clean + 1),
                "Table size out of sync with enum - _staticTokenFlags");
            Diagnostics.Assert(
                s_tokenText.Length == ((int)TokenKind.Clean + 1),
                "Table size out of sync with enum - _tokenText");
            // Some random assertions to make sure the enum and the traits are in sync
            Diagnostics.Assert(GetTraits(TokenKind.Begin) == (TokenFlags.Keyword | TokenFlags.ScriptBlockBlockName),
                               "Table out of sync with enum - flags Begin");
            Diagnostics.Assert(GetTraits(TokenKind.Workflow) == (TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes),
                               "Table out of sync with enum - flags Workflow");
            Diagnostics.Assert(GetTraits(TokenKind.Sequence) == (TokenFlags.Keyword | TokenFlags.StatementDoesntSupportAttributes),
                               "Table out of sync with enum - flags Sequence");
            Diagnostics.Assert(GetTraits(TokenKind.Shr) == (TokenFlags.BinaryOperator | TokenFlags.BinaryPrecedenceComparison | TokenFlags.CanConstantFold),
                               "Table out of sync with enum - flags Shr");
            Diagnostics.Assert(s_tokenText[(int)TokenKind.Shr].Equals("-shr", StringComparison.OrdinalIgnoreCase),
                               "Table out of sync with enum - text Shr");
        }
#endif

        /// <summary>
        /// Return all the flags for a given <see cref="TokenKind" />.
        /// </summary>
        public static TokenFlags GetTraits(this TokenKind kind)
        {
            return s_staticTokenFlags[(int)kind];
        }

        /// <summary>
        /// Return true if the <see cref="TokenKind" /> has the given trait.
        /// </summary>
        public static bool HasTrait(this TokenKind kind, TokenFlags flag)
        {
            return (GetTraits(kind) & flag) != TokenFlags.None;
        }

        internal static int GetBinaryPrecedence(this TokenKind kind)
        {
            Diagnostics.Assert(HasTrait(kind, TokenFlags.BinaryOperator), "Token doesn't have binary precedence.");
            return (int)(s_staticTokenFlags[(int)kind] & TokenFlags.BinaryPrecedenceMask);
        }

        /// <summary>
        /// Return the text for a given <see cref="TokenKind" />.
        /// </summary>
        public static string Text(this TokenKind kind)
        {
            return s_tokenText[(int)kind];
        }
    }

    /// <summary>
    /// Represents many of the various PowerShell tokens, and is the base class for all PowerShell tokens.
    /// </summary>
    public class Token
    {
        private TokenKind _kind;
        private TokenFlags _tokenFlags;
        private readonly InternalScriptExtent _scriptExtent;

        internal Token(InternalScriptExtent scriptExtent, TokenKind kind, TokenFlags tokenFlags)
        {
            _scriptExtent = scriptExtent;
            _kind = kind;
            _tokenFlags = tokenFlags | kind.GetTraits();
        }

        internal void SetIsCommandArgument()
        {
            // Rather than expose the setter, we have an explicit method to change a token so that it is
            // considered a command argument.  This prevent arbitrary changes to the kind which should be safer.
            if (_kind != TokenKind.Identifier)
            {
                _kind = TokenKind.Generic;
            }
        }

        /// <summary>
        /// Return the text of the token as it appeared in the script.
        /// </summary>
        public string Text { get { return _scriptExtent.Text; } }

        /// <summary>
        /// Return the flags for the token.
        /// </summary>
        public TokenFlags TokenFlags { get { return _tokenFlags; } internal set { _tokenFlags = value; } }

        /// <summary>
        /// Return the kind of token.
        /// </summary>
        public TokenKind Kind { get { return _kind; } }

        /// <summary>
        /// Returns true if the token is in error somehow, such as missing a closing quote.
        /// </summary>
        public bool HasError { get { return (_tokenFlags & TokenFlags.TokenInError) != 0; } }

        /// <summary>
        /// Return the extent in the script of the token.
        /// </summary>
        public IScriptExtent Extent { get { return _scriptExtent; } }

        /// <summary>
        /// Return the text of the token as it appeared in the script.
        /// </summary>
        public override string ToString()
        {
            return (_kind == TokenKind.EndOfInput) ? "<eof>" : Text;
        }

        internal virtual string ToDebugString(int indent)
        {
            return string.Create(CultureInfo.InvariantCulture, $"{StringUtil.Padding(indent)}{_kind}: <{Text}>");
        }
    }

    /// <summary>
    /// A constant number token.  The value may be any numeric type including int, long, double, or decimal.
    /// </summary>
    public class NumberToken : Token
    {
        private readonly object _value;

        internal NumberToken(InternalScriptExtent scriptExtent, object value, TokenFlags tokenFlags)
            : base(scriptExtent, TokenKind.Number, tokenFlags)
        {
            _value = value;
        }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Value:<{3}> Type:<{4}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                _value,
                _value.GetType().Name);
        }

        /// <summary>
        /// The numeric value of this token.
        /// </summary>
        public object Value { get { return _value; } }
    }

    /// <summary>
    /// A parameter to a cmdlet (always starts with a dash, like -Path).
    /// </summary>
    public class ParameterToken : Token
    {
        private readonly string _parameterName;
        private readonly bool _usedColon;

        internal ParameterToken(InternalScriptExtent scriptExtent, string parameterName, bool usedColon)
            : base(scriptExtent, TokenKind.Parameter, TokenFlags.None)
        {
            Diagnostics.Assert(!string.IsNullOrEmpty(parameterName), "parameterName can't be null or empty");
            _parameterName = parameterName;
            _usedColon = usedColon;
        }

        /// <summary>
        /// The parameter name without the leading dash.  It is never
        /// null or an empty string.
        /// </summary>
        public string ParameterName { get { return _parameterName; } }

        /// <summary>
        /// When passing an parameter with argument in the form:
        ///     dir -Path:*
        /// The colon is part of the ParameterToken.  This property
        /// returns true when this form is used, false otherwise.
        /// </summary>
        public bool UsedColon { get { return _usedColon; } }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <-{2}{3}>",
                StringUtil.Padding(indent),
                Kind,
                _parameterName,
                _usedColon ? ":" : string.Empty);
        }
    }

    /// <summary>
    /// A variable token - either a regular variable, such as $_, or a splatted variable like @PSBoundParameters.
    /// </summary>
    public class VariableToken : Token
    {
        internal VariableToken(InternalScriptExtent scriptExtent, VariablePath path, TokenFlags tokenFlags, bool splatted)
            : base(scriptExtent, splatted ? TokenKind.SplattedVariable : TokenKind.Variable, tokenFlags)
        {
            VariablePath = path;
        }

        /// <summary>
        /// The simple name of the variable, without any scope or drive qualification.
        /// </summary>
        public string Name { get { return VariablePath.UnqualifiedPath; } }

        /// <summary>
        /// The full details of the variable path.
        /// </summary>
        public VariablePath VariablePath { get; }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Name:<{3}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                Name);
        }
    }

    /// <summary>
    /// The base class for any string token, including single quoted string, double quoted strings, and here strings.
    /// </summary>
    public abstract class StringToken : Token
    {
        internal StringToken(InternalScriptExtent scriptExtent, TokenKind kind, TokenFlags tokenFlags, string value)
            : base(scriptExtent, kind, tokenFlags)
        {
            Value = value;
        }

        /// <summary>
        /// The string value without quotes or leading newlines in the case of a here string.
        /// </summary>
        public string Value { get; }

        internal override string ToDebugString(int indent)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}{1}: <{2}> Value:<{3}>",
                StringUtil.Padding(indent),
                Kind,
                Text,
                Value);
        }
    }

    /// <summary>
    /// A single quoted string, or a single quoted here string.
    /// </summary>
    public class StringLiteralToken : StringToken
    {
        internal StringLiteralToken(InternalScriptExtent scriptExtent, TokenFlags flags, TokenKind tokenKind, string value)
            : base(scriptExtent, tokenKind, flags, value)
        {
        }
    }

    /// <summary>
    /// A double quoted string, or a double quoted here string.
    /// </summary>
    public class StringExpandableToken : StringToken
    {
        private ReadOnlyCollection<Token> _nestedTokens;

        internal StringExpandableToken(InternalScriptExtent scriptExtent, TokenKind tokenKind, string value, string formatString, List<Token> nestedTokens, TokenFlags flags)
            : base(scriptExtent, tokenKind, flags, value)
        {
            if (nestedTokens != null && nestedTokens.Count > 0)
            {
                _nestedTokens = new ReadOnlyCollection<Token>(nestedTokens.ToArray());
            }

            FormatString = formatString;
        }

        internal static void ToDebugString(ReadOnlyCollection<Token> nestedTokens,
                                           StringBuilder sb, int indent)
        {
            Diagnostics.Assert(nestedTokens != null, "caller to verify");

            foreach (Token token in nestedTokens)
            {
                sb.Append(Environment.NewLine);
                sb.Append(token.ToDebugString(indent + 4));
            }
        }

        /// <summary>
        /// This collection holds any tokens from variable references and sub-expressions within the string.
        /// For example:
        ///     "In $([DateTime]::Now.Year - $age), $name was born"
        /// has a nested expression with a sequence of tokens, plus the variable reference $name.
        /// </summary>
        public ReadOnlyCollection<Token> NestedTokens
        {
            get { return _nestedTokens; }

            internal set { _nestedTokens = value; }
        }

        internal string FormatString { get; }

        internal override string ToDebugString(int indent)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(base.ToDebugString(indent));
            if (_nestedTokens != null)
            {
                ToDebugString(_nestedTokens, sb, indent);
            }

            return sb.ToString();
        }
    }

    /// <summary>
    /// </summary>
    public class LabelToken : Token
    {
        internal LabelToken(InternalScriptExtent scriptExtent, TokenFlags tokenFlags, string labelText)
            : base(scriptExtent, TokenKind.Label, tokenFlags)
        {
            LabelText = labelText;
        }

        /// <summary>
        /// </summary>
        public string LabelText { get; }
    }

    /// <summary>
    /// An abstract base class for merging and file redirections.
    /// </summary>
    public abstract class RedirectionToken : Token
    {
        internal RedirectionToken(InternalScriptExtent scriptExtent, TokenKind kind)
            : base(scriptExtent, kind, TokenFlags.None)
        {
        }
    }

    /// <summary>
    /// The (currently unimplemented) input redirection.
    /// </summary>
    public class InputRedirectionToken : RedirectionToken
    {
        internal InputRedirectionToken(InternalScriptExtent scriptExtent)
            : base(scriptExtent, TokenKind.RedirectInStd)
        {
        }
    }

    /// <summary>
    /// A merging redirection.
    /// </summary>
    public class MergingRedirectionToken : RedirectionToken
    {
        internal MergingRedirectionToken(InternalScriptExtent scriptExtent, RedirectionStream from, RedirectionStream to)
            : base(scriptExtent, TokenKind.Redirection)
        {
            this.FromStream = from;
            this.ToStream = to;
        }

        /// <summary>
        /// The stream being redirected.
        /// </summary>
        public RedirectionStream FromStream { get; }

        /// <summary>
        /// The stream being written to.
        /// </summary>
        public RedirectionStream ToStream { get; }
    }

    /// <summary>
    /// A file redirection.
    /// </summary>
    public class FileRedirectionToken : RedirectionToken
    {
        internal FileRedirectionToken(InternalScriptExtent scriptExtent, RedirectionStream from, bool append)
            : base(scriptExtent, TokenKind.Redirection)
        {
            this.FromStream = from;
            this.Append = append;
        }

        /// <summary>
        /// The stream being redirected.
        /// </summary>
        public RedirectionStream FromStream { get; }

        /// <summary>
        /// True if the redirection should append the file rather than create a new file.
        /// </summary>
        public bool Append { get; }
    }

    internal class UnscannedSubExprToken : StringLiteralToken
    {
        internal UnscannedSubExprToken(InternalScriptExtent scriptExtent, TokenFlags tokenFlags, string value, BitArray skippedCharOffsets)
            : base(scriptExtent, tokenFlags, TokenKind.StringLiteral, value)
        {
            this.SkippedCharOffsets = skippedCharOffsets;
        }

        internal BitArray SkippedCharOffsets { get; }
    }
}
