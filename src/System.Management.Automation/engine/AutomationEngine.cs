// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// This class aggregates the objects necessary for the Monad
    /// engine to run.
    /// </summary>
    internal class AutomationEngine
    {
        static AutomationEngine()
        {
            // Register the encoding provider to load encodings that are not supported by default,
            // so as to allow them to be used in user's script/code.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        // Holds the parser to use for this instance of the engine...
        internal Parser EngineParser;

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
#if !UNIX
            // Update the env variable PATHEXT to contain .CPL
            var pathext = Environment.GetEnvironmentVariable("PATHEXT");

            if (string.IsNullOrEmpty(pathext))
            {
                Environment.SetEnvironmentVariable("PATHEXT", ".CPL");
            }
            else if (!(pathext.EndsWith(";.CPL", StringComparison.OrdinalIgnoreCase) ||
                       pathext.StartsWith(".CPL;", StringComparison.OrdinalIgnoreCase) ||
                       pathext.Contains(";.CPL;", StringComparison.OrdinalIgnoreCase) ||
                       pathext.Equals(".CPL", StringComparison.OrdinalIgnoreCase)))
            {
                // Fast skip if we already added the extension as ";.CPL".
                // Fast skip if user already added the extension.
                pathext += pathext[pathext.Length - 1] == ';' ? ".CPL" : ";.CPL";
                Environment.SetEnvironmentVariable("PATHEXT", pathext);
            }
#endif

            Context = new ExecutionContext(this, hostInterface, iss);

            EngineParser = new Language.Parser();
            CommandDiscovery = new CommandDiscovery(Context);

            // Load the iss, resetting everything to it's defaults...
            iss.Bind(Context, updateOnly: false, module: null, noClobber: false, local: false, setLocation: true);
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

            if (errors.Length > 0)
            {
                ParseException ex = errors[0].IncompleteInput
                    ? new IncompleteParseException(errors[0].Message, errors[0].ErrorId)
                    : new ParseException(errors);

                if (addToHistory)
                {
                    // Try associating the parsing error with the history item if we can.
                    InvocationInfo invInfo = ex.ErrorRecord.InvocationInfo;
                    LocalRunspace localRunspace = Context.CurrentRunspace as LocalRunspace;
                    if (invInfo is not null && localRunspace?.History is not null)
                    {
                        invInfo.HistoryId = localRunspace.History.GetNextHistoryId();
                    }
                }

                throw ex;
            }

            return new ScriptBlock(ast, isFilter: false);
        }
    }
}
