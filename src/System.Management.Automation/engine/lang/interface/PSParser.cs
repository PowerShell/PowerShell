// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/********************************************************************++

    Project:     PowerShell

    Contents:    PowerShell parser interface for syntax editors

    Classes:     System.Management.Automation.PSParser

--********************************************************************/

using System.Text;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// PSParser class.
    /// </summary>
    /// <remarks>
    /// This is a class providing the interface for parsing a script into a collection of
    /// tokens, which primarily can be used for syntax colorization.
    ///
    /// Classes provided for syntax colorization includes,
    ///
    ///     1. PSParser: this class provides the main interface to be used.
    ///     2. PSToken: this class provides a public representation of powershell tokens.
    ///     3. PSParseError: this class provides a public representation of syntax errors.
    ///
    /// These three classes are provided for exposing interfaces only. They
    /// should not be used in PowerShell engine code.
    /// </remarks>
    //
    //  1. Design
    //
    //  PSParser class is a public wrapper class of internal Parser class. It is mail goal
    //  is to provide a public interface for parsing a script into a collection of tokens.
    //
    //  Design of this class is made up of two parts,
    //
    //      1. interface part: which implement the static public interface for parsing a script.
    //      2. logic part: which implement the parsing logic for parsing.
    //
    //  2. Interface
    //
    //  The only public interface provided by this class is the static member
    //
    //     static Collection<PSToken> Parse(string script, out Collection<PSParseError> errors)
    //
    //  3. Parsing Logic
    //
    //  Script parsing is done through instances of PSParser object. Each PSParser object
    //  wraps an internal Parser object. It is PSParser object's responsibility to
    //      a. setup local runspace and retrieve internal Parser object from it.
    //      b. call internal parser for actual parsing
    //      c. translate parsing result from internal Token and RuntimeException type
    //         into public PSToken and PSParseError type.
    //
    public sealed class PSParser
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <remarks>
        /// This constructor is made private intentionally. The only way to create an instance
        /// of PSParser object is from PSParser pool maintained in this class.
        /// </remarks>
        private PSParser()
        {
        }

        #region Parsing Logic

        private readonly List<Language.Token> _tokenList = new List<Language.Token>();
        private Language.ParseError[] _errors;

        private void Parse(string script)
        {
            try
            {
                var parser = new Language.Parser { ProduceV2Tokens = true };
                parser.Parse(null, script, _tokenList, out _errors, ParseMode.Default);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Return collection of tokens generated for recent parsing task.
        /// </summary>
        private Collection<PSToken> Tokens
        {
            get
            {
                Collection<PSToken> resultTokens = new Collection<PSToken>();
                // Skip the last token, it's always EOF.
                for (int i = 0; i < _tokenList.Count - 1; i++)
                {
                    var token = _tokenList[i];
                    resultTokens.Add(new PSToken(token));
                }

                return resultTokens;
            }
        }

        /// <summary>
        /// Return collection of errors happened for recent parsing task.
        /// </summary>
        private Collection<PSParseError> Errors
        {
            get
            {
                Collection<PSParseError> resultErrors = new Collection<PSParseError>();
                foreach (var error in _errors)
                {
                    resultErrors.Add(new PSParseError(error));
                }

                return resultErrors;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Parse a script into a collection of tokens.
        /// </summary>
        /// <param name="script">Script to parse.</param>
        /// <param name="errors">Errors happened during parsing.</param>
        /// <returns>Collection of tokens generated during parsing.</returns>
        /// <exception cref="System.Management.Automation.RuntimeException">
        /// Although this API returns most parse-time exceptions in the errors
        /// collection, there are some scenarios where resource limits will result
        /// in an exception being thrown by this API. This allows the caller to
        /// distinguish between a successful parse with errors and a failed parse.
        /// All exceptions thrown will be derived from System.Management.Automation.RuntimeException
        /// but may contain an inner exception that describes the real issue.
        /// </exception>
        public static Collection<PSToken> Tokenize(string script, out Collection<PSParseError> errors)
        {
            if (script == null)
                throw PSTraceSource.NewArgumentNullException(nameof(script));

            PSParser psParser = new PSParser();

            psParser.Parse(script);
            errors = psParser.Errors;

            return psParser.Tokens;
        }

        /// <summary>
        /// Parse a script into a collection of tokens.
        /// </summary>
        /// <param name="script">Script to parse, as an array of lines.</param>
        /// <param name="errors">Errors happened during parsing.</param>
        /// <returns>Collection of tokens generated during parsing.</returns>
        /// <exception cref="System.Management.Automation.RuntimeException">
        /// Although this API returns most parse-time exceptions in the errors
        /// collection, there are some scenarios where resource limits will result
        /// in an exception being thrown by this API. This allows the caller to
        /// distinguish between a successful parse with errors and a failed parse.
        /// All exceptions thrown will be derived from System.Management.Automation.RuntimeException
        /// but may contain an inner exception that describes the real issue.
        /// </exception>
        public static Collection<PSToken> Tokenize(object[] script, out Collection<PSParseError> errors)
        {
            if (script == null)
                throw PSTraceSource.NewArgumentNullException(nameof(script));

            StringBuilder sb = new StringBuilder();
            foreach (object obj in script)
            {
                if (obj != null)
                {
                    sb.AppendLine(obj.ToString());
                }
            }

            return Tokenize(sb.ToString(), out errors);
        }

        #endregion
    }
}
