/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Activities;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;

namespace Microsoft.PowerShell.Activities
{
    /// <summary>
    /// Evaluate the Powershell expression and return the value of type T.
    /// </summary>
    public sealed class PowerShellValue<T> : NativeActivity<T>
    {
        /// <summary>
        /// The PowerShell expression, which will be evaluated and retuned a type of T value.
        /// </summary>
        [RequiredArgument]
        public string Expression { get; set; }

        /// <summary>
        /// Determines whether to connect the input stream for this activity.
        /// </summary>
        [DefaultValue(false)]
        public bool UseDefaultInput
        {
            get;
            set;
        }

        /// <summary>
        /// Validates the syntax of the script text for this activity.
        /// </summary>
        /// <param name="metadata">Activity metadata for this activity</param>
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);

            if (!string.IsNullOrWhiteSpace(Expression))
            {
                var errors = new Collection<PSParseError>();
                PSParser.Tokenize(Expression, out errors);
                if (errors != null && errors.Count > 0)
                {
                    string compositeErrorString = "";
                    foreach (var e in errors)
                    {
                        // Format and add each error message...
                        compositeErrorString += string.Format(CultureInfo.InvariantCulture,
                            "[{0}, {1}]: {2}\n", e.Token.StartLine, e.Token.StartColumn, e.Message);
                    }
                    metadata.AddValidationError(compositeErrorString);
                }
            }
        }

        /// <summary>
        /// Get the scriptblock for this activity, caching it once it's compiled.
        /// </summary>
        private ScriptBlock ExpressionScriptBlock
        {
            get
            {
                if (_expressionScriptBlock == null)
                {
                    lock (syncroot)
                    {
                        if (_expressionScriptBlock == null)
                        {
                            // The guard check for a null expression string is done in Execute() instead
                            // of in this property. It's also done in the validation check for CacheMetadata
                            string updatedExpression = Expression;
                            
                            // Hack to make sure the $input *does* get unrolled...
                            if (string.Equals("$input", Expression.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                updatedExpression = "$(" + updatedExpression + "\n)";
                            }
                            else
                            {
                                Token[] tokens;
                                ParseError[] errors;
                                ScriptBlockAst exprAst = Parser.ParseInput(updatedExpression, out tokens, out errors);
                                if (errors.Length > 0)
                                {
                                    throw new ParseException(errors);
                                }

                                if (exprAst.BeginBlock == null && exprAst.ProcessBlock == null && exprAst.EndBlock != null)
                                {
                                    var statements = exprAst.EndBlock.Statements;
                                    if (statements != null && statements.Count == 1)
                                    {
                                        PipelineAst pipeline = statements[0] as PipelineAst;
                                        if (pipeline != null && pipeline.GetPureExpression() != null)
                                        {
                                            // It is very difficult to get equivalent expression semantics in workflow because the engine
                                            // APIs get in the way necessitating a lot of fiddling with the actual expression as well as post-processing
                                            // the result of the expression.
                                            // We wrap a pure expression in an array so that PowerShell's loop unrolling doesn't impact our
                                            // ability to return collections. We also add a trap/break so that terminating errors in expressions
                                            // are turned into exceptions for the PowerShell object. The trap and closing ')' go on their own line
                                            // for the XAML designer case where the expression might have a trailing '#' making the rest of the
                                            // line into a comment.
                                            updatedExpression = ",(" + updatedExpression + "\n); trap { break }";
                                        }
                                    }
                                }
                            }

                            _expressionScriptBlock = ScriptBlock.Create(updatedExpression);
                        }
                    }
                }
                return _expressionScriptBlock;
            }
        }
        ScriptBlock _expressionScriptBlock;

        /// <summary>
        /// Check to see if the expression only uses elements of the restricted language
        /// as well as only using the allowed commands and variables.
        /// </summary>
        /// <param name="allowedCommands">
        /// List of command names to allow in the expression
        /// </param>
        /// <param name="allowedVariables">
        /// List of variable names to allow in the expression. If the collection contains a single 
        /// element "*", all variables will be allowed including environment variables
        /// functions, etc.
        /// </param>
        /// <param name="allowEnvironmentVariables">
        /// If true, environment variables are allowed even if the allowedVariables list is empty.
        /// </param>
        public void ValidateExpressionConstraints(IEnumerable<string> allowedCommands, IEnumerable<string> allowedVariables, bool allowEnvironmentVariables)
        {
            ExpressionScriptBlock.CheckRestrictedLanguage(allowedCommands, allowedVariables, allowEnvironmentVariables);
        }

        /// <summary>
        /// Execution of PowerShell value activity.
        /// PowerShell expression will be evaluated using PowerShell runspace and the value of Type T will be returned.
        /// </summary>
        /// <param name="context"></param>
        protected override void Execute(NativeActivityContext context)
        {
            Token[] tokens;
            ParseError[] errors;
            ScriptBlockAst exprAst = Parser.ParseInput(Expression, out tokens, out errors);

            bool hasErrorActionPreference = false;
            bool hasWarningPreference = false;
            bool hasInformationPreference = false;

            // Custom activity participant tracker for updating debugger with current variables and sequence stop points.
            // Regex looks for debugger sequence points like: Expression="'3:5:WFFunc1'".
            // We specifically disallow TimeSpan values that look like sequence points: Expression="'00:00:01'".
            bool isDebugSequencePoint = (!string.IsNullOrEmpty(Expression) && (System.Text.RegularExpressions.Regex.IsMatch(Expression, @"^'\d+:\d+:\S+'$")) &&
                                         (typeof(T) != typeof(System.TimeSpan)));
            var dataProperties = context.DataContext.GetProperties();
            if (isDebugSequencePoint || (dataProperties.Count > 0))
            {
                System.Activities.Tracking.CustomTrackingRecord customRecord = new System.Activities.Tracking.CustomTrackingRecord("PSWorkflowCustomUpdateDebugVariablesTrackingRecord");
                foreach (System.ComponentModel.PropertyDescriptor property in dataProperties)
                {
                    if (String.Equals(property.Name, "ParameterDefaults", StringComparison.OrdinalIgnoreCase)) { continue; }

                    Object value = property.GetValue(context.DataContext);
                    if (value != null)
                    {
                        object tempValue = value;

                        PSDataCollection<PSObject> collectionObject = value as PSDataCollection<PSObject>;
                        if (collectionObject != null && collectionObject.Count == 1)
                        {
                            tempValue = collectionObject[0];
                        }

                        customRecord.Data.Add(property.Name, tempValue);
                    }
                }
                if (isDebugSequencePoint)
                {
                    customRecord.Data.Add("DebugSequencePoint", Expression);
                }
                context.Track(customRecord);
            }

            if (tokens != null)
            {
                foreach(Token token in tokens)
                {
                    VariableToken variable = token as VariableToken;

                    if (variable != null)
                    {
                        if (variable.Name.Equals("ErrorActionPreference", StringComparison.OrdinalIgnoreCase))
                        {
                            hasErrorActionPreference = true;
                        }
                        else if (variable.Name.Equals("WarningPreference", StringComparison.OrdinalIgnoreCase))
                        {
                            hasWarningPreference = true;
                        }
                        else if (variable.Name.Equals("InformationPreference", StringComparison.OrdinalIgnoreCase))
                        {
                            hasInformationPreference = true;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(Expression))
            {
                throw new ArgumentException(ActivityResources.NullArgumentExpression);
            }


            if (_ci == null)
            {
                lock (syncroot)
                {
                    // Invoke using the CommandInfo for Invoke-Command directly, rather than going through
                    // command discovery (which is very slow).
                    if (_ci == null)
                    {
                        _ci = new CmdletInfo("Invoke-Command", typeof(Microsoft.PowerShell.Commands.InvokeCommandCommand));
                    }
                }
            }

            Collection<PSObject> returnedvalue;
            Runspace runspace = null;
            bool borrowedRunspace = false;
            PSWorkflowHost workflowHost = null;

            if (typeof(ScriptBlock).IsAssignableFrom(typeof(T)))
            {
                Result.Set(context, ScriptBlock.Create(Expression));
                return;
            }
            else if (typeof(ScriptBlock[]).IsAssignableFrom(typeof(T)))
            {
                Result.Set(context, new ScriptBlock[] { ScriptBlock.Create(Expression) });
                return;
            }

            PropertyDescriptorCollection col = context.DataContext.GetProperties();
            HostParameterDefaults hostValues = context.GetExtension<HostParameterDefaults>();

            // Borrow a runspace from the host if we're not trying to create a ScriptBlock.
            // If we are trying to create one, we need to keep it around so that it can be
            // invoked multiple times.
            if (hostValues != null)
            {
                workflowHost = hostValues.Runtime;
                try
                {
                    runspace = workflowHost.UnboundedLocalRunspaceProvider.GetRunspace(null, 0, 0);
                    borrowedRunspace = true;
                }
                catch (Exception)
                {
                    // it is fine to catch generic exception here 
                    // if the local runspace provider does not give us
                    // a runspace we will create one locally (fallback)
                }
            }

            if (runspace == null)
            {
                // Not running with the PowerShell workflow host so directly create the runspace...
                runspace = RunspaceFactory.CreateRunspace(InitialSessionState.CreateDefault2());
                runspace.Open();
            }

            using (System.Management.Automation.PowerShell ps = System.Management.Automation.PowerShell.Create())
            {
                try
                {
                    ps.Runspace = runspace;

                    // Subscribe to DataAdding on the error stream so that we can add position tracking information
                    if (hostValues != null)
                    {
                        HostSettingCommandMetadata sourceCommandMetadata = hostValues.HostCommandMetadata;

                        CommandMetadataTable.TryAdd(ps.InstanceId, sourceCommandMetadata);
                        ps.Streams.Error.DataAdding += HandleErrorDataAdding;
                    }

                    // First, set the variables from the host defaults
                    if ((hostValues != null) && (hostValues.Parameters != null))
                    {
                        if (hostValues.Parameters.ContainsKey("PSCurrentDirectory"))
                        {
                            string path = hostValues.Parameters["PSCurrentDirectory"] as string;
                            if (path != null)
                            {
                                ps.Runspace.SessionStateProxy.Path.SetLocation(path);
                            }
                        }

                        foreach (string hostDefault in hostValues.Parameters.Keys)
                        {
                            string mappedHostDefault = hostDefault;

                            if (hostDefault.Equals("ErrorAction", StringComparison.OrdinalIgnoreCase))
                            {
                                if (hasErrorActionPreference)
                                {
                                    mappedHostDefault = "ErrorActionPreference";
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if (hostDefault.Equals("WarningAction", StringComparison.OrdinalIgnoreCase))
                            {
                                if (hasWarningPreference)
                                {
                                    mappedHostDefault = "WarningPreference";
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else if (hostDefault.Equals("InformationAction", StringComparison.OrdinalIgnoreCase))
                            {
                                if (hasInformationPreference)
                                {
                                    mappedHostDefault = "InformationPreference";
                                }
                                else
                                {
                                    continue;
                                }
                            }

                            object propertyValue = hostValues.Parameters[hostDefault];
                            if (propertyValue != null)
                            {
                                ps.Runspace.SessionStateProxy.PSVariable.Set(mappedHostDefault, propertyValue);
                            }
                        }
                    }

                    // Then, set the variables from the workflow
                    foreach (PropertyDescriptor p in col)
                    {
                        string name = p.Name;
                        object value = p.GetValue(context.DataContext);

                        if (value != null)
                        {
                            object tempValue = value;

                            PSDataCollection<PSObject> collectionObject = value as PSDataCollection<PSObject>;

                            if (collectionObject != null && collectionObject.Count == 1)
                            {
                                tempValue = collectionObject[0];
                            }

                            ps.Runspace.SessionStateProxy.PSVariable.Set(name, tempValue);
                        }
                    }

                    ps.AddCommand(_ci).AddParameter("NoNewScope").AddParameter("ScriptBlock", ExpressionScriptBlock);


                    // If this needs to consume input, take it from the host stream.
                    PSDataCollection<PSObject> inputStream = null;
                    if (UseDefaultInput)
                    {
                        // Retrieve our host overrides
                        hostValues = context.GetExtension<HostParameterDefaults>();

                        if (hostValues != null)
                        {
                            Dictionary<string, object> incomingArguments = hostValues.Parameters;
                            if (incomingArguments.ContainsKey("Input"))
                            {
                                inputStream = incomingArguments["Input"] as PSDataCollection<PSObject>;
                            }
                        }
                    }

                    // Now invoke the pipeline
                    try
                    {
                        if (inputStream != null)
                        {
                            returnedvalue = ps.Invoke(inputStream);
                            inputStream.Clear();
                        }
                        else
                        {
                            returnedvalue = ps.Invoke();
                        }
                    }
                    catch (CmdletInvocationException cie)
                    {
                        if (cie.ErrorRecord != null && cie.ErrorRecord.Exception != null)
                        {
                            throw cie.InnerException;
                        }
                        else
                        {
                            throw;
                        }
                    }

                }
                finally
                {
                    if (hostValues != null)
                    {
                        ps.Streams.Error.DataAdding -= HandleErrorDataAdding;
                        HostSettingCommandMetadata removedValue;
                        CommandMetadataTable.TryRemove(ps.InstanceId, out removedValue);
                    }

                    if (borrowedRunspace)
                    {
                        workflowHost.UnboundedLocalRunspaceProvider.ReleaseRunspace(runspace);
                    }
                    else
                    {
                        // This will be disposed  when the command is done with it.
                        runspace.Dispose();
                        runspace = null;
                    }
                }


                if (ps.Streams.Error != null && ps.Streams.Error.Count > 0)
                {
                    PSDataCollection<ErrorRecord> errorStream = null;

                    // Retrieve our host overrides
                    hostValues = context.GetExtension<HostParameterDefaults>();

                    if (hostValues != null)
                    {
                        Dictionary<string, object> incomingArguments = hostValues.Parameters;
                        if (incomingArguments.ContainsKey("PSError"))
                        {
                            errorStream = incomingArguments["PSError"] as PSDataCollection<ErrorRecord>;
                        }
                    }

                    if (errorStream != null && errorStream.IsOpen)
                    {
                        foreach (ErrorRecord record in ps.Streams.Error)
                        {
                            errorStream.Add(record);
                        }
                    }
                }

                T valueToReturn = default(T);
                if (returnedvalue != null && returnedvalue.Count > 0)
                {
                    try
                    {
                        if (returnedvalue.Count == 1)
                        {
                            if (returnedvalue[0] != null)
                            {
                               Object result = returnedvalue[0];
                               Object baseObject = ((PSObject)result).BaseObject;
                               if (! (baseObject is PSCustomObject))
                               {
                                   result = baseObject;
                               }

                                // Try regular PowerShell conversion
                                valueToReturn = LanguagePrimitives.ConvertTo<T>( result );
                            }
                        }
                        else
                        {
                            valueToReturn = LanguagePrimitives.ConvertTo<T>(returnedvalue);
                        }
                    }
                    catch (PSInvalidCastException)
                    {
                        // Handle the special case of emitting a PSDataCollection - use its array constructor.
                        // This special case is why we aren't using PowerShell.Invoke<T>
                        if (typeof(T) == typeof(PSDataCollection<PSObject>))
                        {
                            Object tempValueToReturn = new PSDataCollection<PSObject>(
                                new List<PSObject> { LanguagePrimitives.ConvertTo<PSObject>(returnedvalue[0]) });
                            valueToReturn = (T)tempValueToReturn;
                        }
                        else
                        {
                            throw;
                        }
                    }

                    Result.Set(context, valueToReturn);
                }
            }
        }

        private static void HandleErrorDataAdding(object sender, DataAddingEventArgs e)
        {
            HostSettingCommandMetadata commandMetadata;
            CommandMetadataTable.TryGetValue(e.PowerShellInstanceId, out commandMetadata);

            if (commandMetadata != null)
            {
                PowerShellInvocation_ErrorAdding(sender, e, commandMetadata);
            }
        }

        private static readonly ConcurrentDictionary<Guid, HostSettingCommandMetadata> CommandMetadataTable =
            new ConcurrentDictionary<Guid, HostSettingCommandMetadata>();

        private static void PowerShellInvocation_ErrorAdding(object sender, DataAddingEventArgs e, HostSettingCommandMetadata commandMetadata)
        {
            ErrorRecord errorRecord = e.ItemAdded as ErrorRecord;

            if (errorRecord != null)
            {
                if (commandMetadata != null)
                {
                    ScriptPosition scriptStart = new ScriptPosition(
                        commandMetadata.CommandName,
                        commandMetadata.StartLineNumber,
                        commandMetadata.StartColumnNumber,
                        null);
                    ScriptPosition scriptEnd = new ScriptPosition(
                        commandMetadata.CommandName,
                        commandMetadata.EndLineNumber,
                        commandMetadata.EndColumnNumber,
                        null);
                    ScriptExtent extent = new ScriptExtent(scriptStart, scriptEnd);

                    if (errorRecord.InvocationInfo != null)
                    {
                        errorRecord.InvocationInfo.DisplayScriptPosition = extent;
                    }
                }
            }
        }

        static CommandInfo _ci;
        static object syncroot = new object();
    }
}
