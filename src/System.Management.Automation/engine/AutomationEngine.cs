// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation
{
    /// <summary>
    /// This class aggregates the objects necessary for the Monad
    /// engine to run.
    /// </summary>
    internal class AutomationEngine
    {
        // Holds the parser to use for this instance of the engine...
        internal Language.Parser EngineParser;

        /// <summary>
        /// Returns the handle to the execution context
        /// for this instance of the automation engine.
        /// </summary>
        internal ExecutionContext Context { get; }

        /// <summary>
        /// Gets the CommandDiscovery instance for the current engine.
        /// </summary>
        internal CommandDiscovery CommandDiscovery { get; }

        /// <summary>
        /// The principal constructor that most hosts will use when creating
        /// an instance of the automation engine. It allows you to pass in an
        /// instance of PSHost that provides the host-specific I/O routines, etc.
        /// </summary>
        internal AutomationEngine(PSHost hostInterface, InitialSessionState iss)
        {
            Context = new ExecutionContext(this, hostInterface, iss);

            EngineParser = new Language.Parser();
            CommandDiscovery = new CommandDiscovery(Context);

            // Load the iss, resetting everything to it's defaults...
            iss.Bind(Context, /*updateOnly*/ false);

            InitialSessionState.SetSessionStateDrive(Context, true);
        }

        /// <summary>
        /// Method to take a string and expand any metachars in it.
        /// </summary>
        internal string Expand(string s)
        {
            var ast = Parser.ScanString(s);

            // ExpandString is assumed to invoke code, so passing 'IsTrustedInput'
            return Compiler.GetExpressionValue(ast, true, Context, Context.EngineSessionState) as string ?? string.Empty;
        }

        /// <summary>
        /// Compile a piece of text into a parse tree for later execution.
        /// </summary>
        /// <param name="script">The text to parse.</param>
        /// <param name="addToHistory">True iff the scriptblock will be added to history.</param>
        /// <returns>The parse text as a parsetree node.</returns>
        internal ScriptBlock ParseScriptBlock(string script, bool addToHistory)
        {
            return ParseScriptBlock(script, null, addToHistory);
        }

        internal ScriptBlock ParseScriptBlock(string script, string fileName, bool addToHistory)
        {
            ParseError[] errors;
            var ast = EngineParser.Parse(fileName, script, null, out errors, ParseMode.Default);

            if (addToHistory)
            {
                EngineParser.SetPreviousFirstLastToken(Context);
            }

            if (errors.Any())
            {
                if (errors[0].IncompleteInput)
                {
                    throw new IncompleteParseException(errors[0].Message, errors[0].ErrorId);
                }

                throw new ParseException(errors);
            }

            return new ScriptBlock(ast, isFilter: false);
        }
    }
}

