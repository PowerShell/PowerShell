/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

// TODO/FIXME: move this to Microsoft.PowerShell.Cim namespace (and move in source depot folder as well)
namespace Microsoft.PowerShell.Cmdletization.Cim
{
    using System.Management.Automation;
    using System.Text;

    /// <summary>
    /// Translates a <see cref="WildcardPattern"/> into a like-operand for WQL.
    /// </summary>
    /// <remarks>
    /// Documentation on MSDN (http://msdn.microsoft.com/en-us/library/aa392263(VS.85).aspx) is
    /// 1) rather slim / incomplete
    /// 2) sometimes incorrect (i.e. says that '=' is used for character ranges, when it should have said '-')
    /// 
    /// The code below is therefore mainly based on reverse engineering of admin\wmi\wbem\winmgmt\wbecomn\like.cpp
    /// </remarks>
    internal class WildcardPatternToCimQueryParser : WildcardPatternParser
    {
        private readonly StringBuilder result = new StringBuilder();
        private bool needClientSideFiltering;

        protected override void AppendLiteralCharacter(char c)
        {
            switch (c)
            {
                case '%':
                case '_':
                case '[': // no need to escape ']' character
                    this.BeginBracketExpression();
                    this.AppendLiteralCharacterToBracketExpression(c);
                    this.EndBracketExpression();
                    break;
                default:
                    result.Append(c);
                    break;
            }
        }

        protected override void AppendAsterix()
        {
            result.Append('%');
        }

        protected override void AppendQuestionMark()
        {
            result.Append('_');
        }

        protected override void BeginBracketExpression()
        {
            result.Append('[');
        }

        protected override void AppendLiteralCharacterToBracketExpression(char c)
        {
            switch (c)
            {
                case '^':
                case ']':
                case '-':
                case '\\':
                    this.AppendCharacterRangeToBracketExpression(c, c);
                    break;
                default:
                    result.Append(c);
                    break;
            }
        }

        protected override void AppendCharacterRangeToBracketExpression(char startOfCharacterRange, char endOfCharacterRange)
        {
            // 90 = Z
            // 91 = [
            // 92 = \
            // 93 = ]
            // 94 = ^
            // 95 = _
            if ((91 <= startOfCharacterRange) && (startOfCharacterRange <= 94))
            {
                startOfCharacterRange = (char)90;
                needClientSideFiltering = true;
            }
            if ((91 <= endOfCharacterRange) && (endOfCharacterRange <= 94))
            {
                endOfCharacterRange = (char)95;
                needClientSideFiltering = true;
            }

            // 44 = ,
            // 45 = -
            // 46 = .
            if (startOfCharacterRange == 45)
            {
                startOfCharacterRange = (char)44;
                needClientSideFiltering = true;
            }
            if (endOfCharacterRange == 45)
            {
                endOfCharacterRange = (char)46;
                needClientSideFiltering = true;
            }

            result.Append(startOfCharacterRange);
            result.Append('-');
            result.Append(endOfCharacterRange);
        }

        protected override void EndBracketExpression()
        {
            result.Append(']');
        }

        /// <summary>
        /// Converts <paramref name="wildcardPattern"/> into a value of a right-hand-side operand of LIKE operator of a WQL query.  
        /// Return value still has to be string-escaped (i.e. by doubling '\'' character), before embedding it into a query.
        /// </summary>
        static internal string Parse(WildcardPattern wildcardPattern, out bool needsClientSideFiltering)
        {
            var parser = new WildcardPatternToCimQueryParser();
            WildcardPatternParser.Parse(wildcardPattern, parser);
            needsClientSideFiltering = parser.needClientSideFiltering;
            return parser.result.ToString();
        }
    }
}
