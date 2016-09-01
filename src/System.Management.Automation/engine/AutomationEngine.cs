/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

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
        /// Gets the CommandDiscovery instance for the current engine
        /// </summary>
        /// 
        internal CommandDiscovery CommandDiscovery { get; }


        /// <summary>
        /// The principal constructor that most hosts will use when creating
        /// an instance of the automation engine. It allows you to pass in an
        /// instance of PSHost that provides the host-specific I/O routines, etc.
        /// </summary>
        internal AutomationEngine(PSHost hostInterface, RunspaceConfiguration runspaceConfiguration, InitialSessionState iss)
        {
#if !CORECLR// There is no control panel items in CSS
            // Update the env variable PathEXT to contain .CPL
            var pathext = Environment.GetEnvironmentVariable("PathEXT");
            pathext = pathext ?? string.Empty;
            bool cplExist = false;
            if (pathext != string.Empty)
            {
                string[] entries = pathext.Split(Utils.Separators.Semicolon);
                foreach (string entry in entries)
                {
                    string ext = entry.Trim();
                    if (ext.Equals(".CPL", StringComparison.OrdinalIgnoreCase))
                    {
                        cplExist = true;
                        break;
                    }
                }
            }
            if (!cplExist)
            {
                pathext = (pathext == string.Empty) ? ".CPL" :
                    pathext.EndsWith(";", StringComparison.OrdinalIgnoreCase)
                    ? (pathext + ".CPL") : (pathext + ";.CPL");
                Environment.SetEnvironmentVariable("PathEXT", pathext);
            }
#endif
            if (runspaceConfiguration != null)
            {
                Context = new ExecutionContext(this, hostInterface, runspaceConfiguration);
            }
            else
            {
                Context = new ExecutionContext(this, hostInterface, iss);
            }

            EngineParser = new Language.Parser();
            CommandDiscovery = new CommandDiscovery(Context);

            // Initialize providers before loading types so that any ScriptBlocks in the
            // types.ps1xml file can be parsed.

            // Bind the execution context with RunspaceConfiguration. 
            // This has the side effect of initializing cmdlet cache and providers from runspace configuration.
            if (runspaceConfiguration != null)
            {
                runspaceConfiguration.Bind(Context);
            }
            else
            {
                // Load the iss, resetting everything to it's defaults...
                iss.Bind(Context, /*updateOnly*/ false);
            }

            InitialSessionState.SetSessionStateDrive(Context, true);
        }

        /// <summary>
        /// Method to take a string and expand any metachars in it.
        /// </summary>
        internal string Expand(string s)
        {
            var ast = Parser.ScanString(s);

            // ExpandString is assumed to invoke code, so passing 'IsTrustedInput'
            return Compiler.GetExpressionValue(ast, true, Context, Context.EngineSessionState) as string ?? "";
        }

        /// <summary>
        /// Compile a piece of text into a parse tree for later execution.
        /// </summary>
        /// <param name="script">The text to parse</param>
        /// <param name="interactiveCommand"></param>
        /// <returns>The parse text as a parsetree node.</returns>
        internal ScriptBlock ParseScriptBlock(string script, bool interactiveCommand)
        {
            return ParseScriptBlock(script, null, interactiveCommand);
        }

        internal ScriptBlock ParseScriptBlock(string script, string fileName, bool interactiveCommand)
        {
            ParseError[] errors;
            var ast = EngineParser.Parse(fileName, script, null, out errors, ParseMode.Default);

            if (interactiveCommand)
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

