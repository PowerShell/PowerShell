// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Language
{
    internal static class SpecialChars
    {
        // Uncommon whitespace
        internal const char NoBreakSpace = (char)0x00a0;
        internal const char NextLine = (char)0x0085;

        // Special dashes
        internal const char EnDash = (char)0x2013;
        internal const char EmDash = (char)0x2014;
        internal const char HorizontalBar = (char)0x2015;

        // Special quotes
        internal const char QuoteSingleLeft = (char)0x2018; // left single quotation mark
        internal const char QuoteSingleRight = (char)0x2019; // right single quotation mark
        internal const char QuoteSingleBase = (char)0x201a; // single low-9 quotation mark
        internal const char QuoteReversed = (char)0x201b; // single high-reversed-9 quotation mark
        internal const char QuoteDoubleLeft = (char)0x201c; // left double quotation mark
        internal const char QuoteDoubleRight = (char)0x201d; // right double quotation mark
        internal const char QuoteLowDoubleLeft = (char)0x201E; // low double left quote used in german.
    }

    [Flags]
    internal enum CharTraits
    {
        /// <summary>
        /// No specific character traits.
        /// </summary>
        None = 0x0000,

        /// <summary>
        /// For identifiers, the first character must be a letter or underscore.
        /// </summary>
        IdentifierStart = 0x0002,

        /// <summary>
        /// The character is a valid first character of a multiplier.
        /// </summary>
        MultiplierStart = 0x0004,

        /// <summary>
        /// The character is a valid type suffix for numeric literals.
        /// </summary>
        TypeSuffix = 0x0008,

        /// <summary>
        /// The character is a whitespace character.
        /// </summary>
        Whitespace = 0x0010,

        /// <summary>
        /// The character terminates a line.
        /// </summary>
        Newline = 0x0020,

        /// <summary>
        /// The character is a hexadecimal digit.
        /// </summary>
        HexDigit = 0x0040,

        /// <summary>
        /// The character is a decimal digit.
        /// </summary>
        Digit = 0x0080,

        /// <summary>
        /// The character is allowed as the first character in an unbraced variable name.
        /// </summary>
        VarNameFirst = 0x0100,

        /// <summary>
        /// The character is not part of the token being scanned.
        /// </summary>
        ForceStartNewToken = 0x0200,

        /// <summary>
        /// The character is not part of the token being scanned, when the token is known to be part of an assembly name.
        /// </summary>
        ForceStartNewAssemblyNameSpecToken = 0x0400,

        /// <summary>
        /// The character is the first character of some operator (and hence is not part of a token that starts a number).
        /// </summary>
        ForceStartNewTokenAfterNumber = 0x0800,
    }

    internal static class CharExtensions
    {
        static CharExtensions()
        {
            Diagnostics.Assert(s_traits.Length == 128, "Extension methods rely on this table size.");
        }

        private static readonly CharTraits[] s_traits = new CharTraits[]
        {
/*      0x0 */ CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0x1 */ CharTraits.None,
/*      0x2 */ CharTraits.None,
/*      0x3 */ CharTraits.None,
/*      0x4 */ CharTraits.None,
/*      0x5 */ CharTraits.None,
/*      0x6 */ CharTraits.None,
/*      0x7 */ CharTraits.None,
/*      0x8 */ CharTraits.None,
/*      0x9 */ CharTraits.Whitespace | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0xA */ CharTraits.Newline | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0xB */ CharTraits.Whitespace | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0xC */ CharTraits.Whitespace | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0xD */ CharTraits.Newline | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*      0xE */ CharTraits.None,
/*      0xF */ CharTraits.None,
/*     0x10 */ CharTraits.None,
/*     0x11 */ CharTraits.None,
/*     0x12 */ CharTraits.None,
/*     0x13 */ CharTraits.None,
/*     0x14 */ CharTraits.None,
/*     0x15 */ CharTraits.None,
/*     0x16 */ CharTraits.None,
/*     0x17 */ CharTraits.None,
/*     0x18 */ CharTraits.None,
/*     0x19 */ CharTraits.None,
/*     0x1A */ CharTraits.None,
/*     0x1B */ CharTraits.None,
/*     0x1C */ CharTraits.None,
/*     0x1D */ CharTraits.None,
/*     0x1E */ CharTraits.None,
/*     0x1F */ CharTraits.None,
/*          */ CharTraits.Whitespace | CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*        ! */ CharTraits.ForceStartNewTokenAfterNumber,
/*        " */ CharTraits.None,
/*        # */ CharTraits.ForceStartNewTokenAfterNumber,
/*        $ */ CharTraits.VarNameFirst,
/*        % */ CharTraits.ForceStartNewTokenAfterNumber,
/*        & */ CharTraits.ForceStartNewToken,
/*        ' */ CharTraits.None,
/*        ( */ CharTraits.ForceStartNewToken,
/*        ) */ CharTraits.ForceStartNewToken,
/*        * */ CharTraits.ForceStartNewTokenAfterNumber,
/*        + */ CharTraits.ForceStartNewTokenAfterNumber,
/*        , */ CharTraits.ForceStartNewToken | CharTraits.ForceStartNewAssemblyNameSpecToken,
/*        - */ CharTraits.ForceStartNewTokenAfterNumber,
/*        . */ CharTraits.ForceStartNewTokenAfterNumber,
/*        / */ CharTraits.ForceStartNewTokenAfterNumber,
/*        0 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        1 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        2 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        3 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        4 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        5 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        6 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        7 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        8 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        9 */ CharTraits.Digit | CharTraits.HexDigit | CharTraits.VarNameFirst,
/*        : */ CharTraits.VarNameFirst,
/*        ; */ CharTraits.ForceStartNewToken,
/*        < */ CharTraits.ForceStartNewTokenAfterNumber,
/*        = */ CharTraits.ForceStartNewAssemblyNameSpecToken | CharTraits.ForceStartNewTokenAfterNumber,
/*        > */ CharTraits.ForceStartNewTokenAfterNumber,
/*        ? */ CharTraits.VarNameFirst,
/*        @ */ CharTraits.None,
/*        A */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        B */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        C */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        D */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit | CharTraits.TypeSuffix,
/*        E */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        F */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        G */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        H */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        I */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        J */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        K */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        L */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        M */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        N */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        O */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        P */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        Q */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        R */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        S */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        T */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        U */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        V */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        W */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        X */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        Y */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        Z */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        [ */ CharTraits.None,
/*        \ */ CharTraits.None,
/*        ] */ CharTraits.ForceStartNewAssemblyNameSpecToken | CharTraits.ForceStartNewTokenAfterNumber,
/*        ^ */ CharTraits.VarNameFirst,
/*        _ */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        ` */ CharTraits.None,
/*        a */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        b */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        c */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        d */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit | CharTraits.TypeSuffix,
/*        e */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        f */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.HexDigit,
/*        g */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        h */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        i */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        j */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        k */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        l */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        m */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        n */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        o */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        p */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        q */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        r */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        s */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        t */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.MultiplierStart,
/*        u */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        v */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        w */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        x */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        y */ CharTraits.IdentifierStart | CharTraits.VarNameFirst | CharTraits.TypeSuffix,
/*        z */ CharTraits.IdentifierStart | CharTraits.VarNameFirst,
/*        { */ CharTraits.ForceStartNewToken,
/*        | */ CharTraits.ForceStartNewToken,
/*        } */ CharTraits.ForceStartNewToken,
/*        ~ */ CharTraits.None,
/*     0x7F */ CharTraits.None,
        };

        public static bool IsCurlyBracket(char c)
        {
            return (c == '{' || c == '}');
        }

        // Return true if the character is a whitespace character.
        // Newlines are not whitespace.
        internal static bool IsWhitespace(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.Whitespace) != 0;
            }

            if (c <= 256)
            {
                return (c == SpecialChars.NoBreakSpace
                        || c == SpecialChars.NextLine);
            }

            return char.IsSeparator(c);
        }

        // Return true if the character is any of the normal or special
        // dash characters.
        internal static bool IsDash(this char c)
        {
            return (c == '-'
                || c == SpecialChars.EnDash
                || c == SpecialChars.EmDash
                || c == SpecialChars.HorizontalBar);
        }

        // Return true if the character is any of the normal or special
        // single quote characters.
        internal static bool IsSingleQuote(this char c)
        {
            return (c == '\''
                || c == SpecialChars.QuoteSingleLeft
                || c == SpecialChars.QuoteSingleRight
                || c == SpecialChars.QuoteSingleBase
                || c == SpecialChars.QuoteReversed);
        }

        // Return true if the character is any of the normal or special
        // double quote characters.
        internal static bool IsDoubleQuote(this char c)
        {
            return (c == '"'
                || c == SpecialChars.QuoteDoubleLeft
                || c == SpecialChars.QuoteDoubleRight
                || c == SpecialChars.QuoteLowDoubleLeft);
        }

        // Return true if the character can be the first character of
        // the variable name for unbraced variable tokens.
        internal static bool IsVariableStart(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.VarNameFirst) != 0;
            }

            return char.IsLetterOrDigit(c);
        }

        // Return true if the character can be the first character of
        // an identifier or label.
        internal static bool IsIdentifierStart(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.IdentifierStart) != 0;
            }

            return char.IsLetter(c);
        }

        // Return true if the character can follow the first character of
        // an identifier or label.
        internal static bool IsIdentifierFollow(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & (CharTraits.IdentifierStart | CharTraits.Digit)) != 0;
            }

            return char.IsLetterOrDigit(c);
        }

        // Return true if the character is a hexadecimal digit.
        internal static bool IsHexDigit(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.HexDigit) != 0;
            }

            return false;
        }

        // Returns true if the character is a decimal digit.
        internal static bool IsDecimalDigit(this char c) => (uint)(c - '0') <= 9;

        // These decimal/binary checking methods are more performant than the alternatives due to requiring
        // less overall operations than a more readable check such as {(this char c) => c == 0 | c == 1},
        // especially in the case of IsDecimalDigit().

        // Returns true if the character is a binary digit.
        internal static bool IsBinaryDigit(this char c) => (uint)(c - '0') <= 1;

        // Returns true if the character is a type suffix character.
        internal static bool IsTypeSuffix(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.TypeSuffix) != 0;
            }

            return false;
        }

        // Return true if the character is the first character in a multiplier.
        internal static bool IsMultiplierStart(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.MultiplierStart) != 0;
            }

            return false;
        }

        // Return true if the character ends the current token while scanning
        // a token beginning with a letter or number, regardless of the tokenizing
        // mode.  This helps ensure that we tokenize 'a#b' as a single token, but
        // 'a{' as 2 tokens.
        internal static bool ForceStartNewToken(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.ForceStartNewToken) != 0;
            }

            return c.IsWhitespace();
        }

        /// <summary>
        /// Check if the current character forces to end scanning a number token.
        /// This allows the tokenizer to scan '7z' as a single token, but '7+' as 2 tokens.
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <param name="forceEndNumberOnTernaryOperatorChars">
        /// In some cases, we want '?' and ':' to end a number token too, so they can be
        /// treated as the ternary operator tokens.
        /// </param>
        /// <returns>Return true if the character ends the current number token.</returns>
        internal static bool ForceStartNewTokenAfterNumber(this char c, bool forceEndNumberOnTernaryOperatorChars)
        {
            if (c < 128)
            {
                if ((s_traits[c] & CharTraits.ForceStartNewTokenAfterNumber) != 0)
                {
                    return true;
                }

                return forceEndNumberOnTernaryOperatorChars && (c == '?' || c == ':');
            }

            return c.IsDash();
        }

        // Return true if the character ends the current token while scanning
        // a token in the assembly name spec.
        internal static bool ForceStartNewTokenInAssemblyNameSpec(this char c)
        {
            if (c < 128)
            {
                return (s_traits[c] & CharTraits.ForceStartNewAssemblyNameSpecToken) != 0;
            }

            return c.IsWhitespace();
        }
    }
}
