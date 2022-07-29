// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Reflection;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    /// <summary>
    /// Flags.
    /// </summary>
    [Flags]
    internal enum ParameterBindingFlags
    {
        /// <summary>
        /// No flags specified.
        /// </summary>
        None = 0,

        /// <summary>
        /// Set when the argument should be converted to the parameter type.
        /// </summary>
        ShouldCoerceType = 0x01,

        /// <summary>
        /// Set when the argument should not be validated or recorded in BoundParameters.
        /// </summary>
        IsDefaultValue = 0x02,

        /// <summary>
        /// Set when script blocks can be bound as a script block parameter instead of a normal argument.
        /// </summary>
        DelayBindScriptBlock = 0x04,

        /// <summary>
        /// Set when an exception will be thrown if a matching parameter could not be found.
        /// </summary>
        ThrowOnParameterNotFound = 0x08,
    }

    /// <summary>
    /// An abstract class used by the CommandProcessor to bind parameters to a bindable object.
    /// Derived classes are used to provide specific binding behavior for different object types,
    /// like Cmdlet, PsuedoParameterCollection, and dynamic parameter objects.
    /// </summary>
    [DebuggerDisplay("Command = {command}")]
    internal abstract class ParameterBinderBase
    {
        #region tracer
        [TraceSource("ParameterBinderBase", "A abstract helper class for the CommandProcessor that binds parameters to the specified object.")]
        private static readonly PSTraceSource s_tracer = PSTraceSource.GetTracer("ParameterBinderBase", "A abstract helper class for the CommandProcessor that binds parameters to the specified object.");

        [TraceSource("ParameterBinding", "Traces the process of binding the arguments to the parameters of cmdlets, scripts, and applications.")]
        internal static readonly PSTraceSource bindingTracer =
            PSTraceSource.GetTracer(
                "ParameterBinding",
                "Traces the process of binding the arguments to the parameters of cmdlets, scripts, and applications.",
                false);

        #endregion tracer

        #region ctor

        /// <summary>
        /// Constructs the parameter binder with the specified type metadata. The binder is only valid
        /// for a single instance of a bindable object and only for the duration of a command.
        /// </summary>
        /// <param name="target">
        /// The target object that the parameter values will be bound to.
        /// </param>
        /// <param name="invocationInfo">
        /// The invocation information for the code that is being bound.
        /// </param>
        /// <param name="context">
        /// The context of the currently running engine.
        /// </param>
        /// <param name="command">
        /// The command that the parameter binder is binding to. The command can be null.
        /// </param>
        internal ParameterBinderBase(
            object target,
            InvocationInfo invocationInfo,
            ExecutionContext context,
            InternalCommand command)
        {
            Diagnostics.Assert(target != null, "caller to verify target is not null.");
            Diagnostics.Assert(invocationInfo != null, "caller to verify invocationInfo is not null.");
            Diagnostics.Assert(context != null, "caller to verify context is not null.");

            bindingTracer.ShowHeaders = false;

            _command = command;
            _target = target;
            _invocationInfo = invocationInfo;
            _context = context;
            _engine = context.EngineIntrinsics;
            _isTranscribing = context.EngineHostInterface.UI.IsTranscribing;
        }

        /// <summary>
        /// Constructs the parameter binder with the specified type metadata. The binder is only valid
        /// for a single instance of a bindable object and only for the duration of a command.
        /// </summary>
        /// <param name="invocationInfo">
        /// The invocation information for the code that is being bound.
        /// </param>
        /// <param name="context">
        /// The context of the currently running engine.
        /// </param>
        /// <param name="command">
        /// The command that the parameter binder is binding to. The command can be null.
        /// </param>
        internal ParameterBinderBase(
            InvocationInfo invocationInfo,
            ExecutionContext context,
            InternalCommand command)
        {
            Diagnostics.Assert(invocationInfo != null, "caller to verify invocationInfo is not null.");
            Diagnostics.Assert(context != null, "caller to verify context is not null.");

            bindingTracer.ShowHeaders = false;

            _command = command;
            _invocationInfo = invocationInfo;
            _context = context;
            _engine = context.EngineIntrinsics;
            _isTranscribing = context.EngineHostInterface.UI.IsTranscribing;
        }

        #endregion ctor

        #region internal members

        /// <summary>
        /// Gets or sets the bindable object that the binder will bind parameters to.
        /// </summary>
        /// <value></value>
        internal object Target
        {
            get
            {
                Diagnostics.Assert(
                    _target != null,
                    "The target should always be set for the binder");

                return _target;
            }

            set
            {
                _target = value;
            }
        }

        /// <summary>
        /// The bindable object that parameters will be bound to.
        /// </summary>
        private object _target;

        /// <summary>
        /// Holds the set of parameters that have been bound from the command line...
        /// </summary>
        internal CommandLineParameters CommandLineParameters
        {
            get { return _commandLineParameters ??= new CommandLineParameters(); }

            // Setter is needed to pass into RuntimeParameterBinder instances
            set { _commandLineParameters = value; }
        }

        private CommandLineParameters _commandLineParameters;

        /// <summary>
        /// If this is true, then we want to record the list of bound parameters...
        /// </summary>
        internal bool RecordBoundParameters = true;

        /// <summary>
        /// Full Qualified ID for the obsolete parameter warning.
        /// </summary>
        internal const string FQIDParameterObsolete = "ParameterObsolete";

        #region Parameter default values

        /// <summary>
        /// Derived classes must override this method to get the default parameter
        /// value so that it can be restored between pipeline input.
        /// </summary>
        /// <param name="name">
        /// The name of the parameter to get the default value of.
        /// </param>
        /// <returns>
        /// The value of the parameter specified by name.
        /// </returns>
        internal abstract object GetDefaultParameterValue(string name);

        #endregion Parameter default values

        #region Parameter binding

        /// <summary>
        /// Derived classes define this method to bind the specified value
        /// to the specified parameter.
        /// </summary>
        /// <param name="name">
        ///     The name of the parameter to bind the value to.
        /// </param>
        /// <param name="value">
        ///     The value to bind to the parameter. It should be assumed by
        ///     derived classes that the proper type coercion has already taken
        ///     place and that any validation metadata has been satisfied.
        /// </param>
        /// <param name="parameterMetadata"></param>
        internal abstract void BindParameter(string name, object value, CompiledCommandParameter parameterMetadata);

        private void ValidatePSTypeName(
            CommandParameterInternal parameter,
            CompiledCommandParameter parameterMetadata,
            bool retryOtherBindingAfterFailure,
            object parameterValue)
        {
            Dbg.Assert(parameter != null, "Caller should verify parameter != null");
            Dbg.Assert(parameterMetadata != null, "Caller should verify parameterMetadata != null");

            if (parameterValue == null)
            {
                return;
            }

            IEnumerable<string> psTypeNamesOfArgumentValue = PSObject.AsPSObject(parameterValue).InternalTypeNames;
            string psTypeNameRequestedByParameter = parameterMetadata.PSTypeName;

            if (!psTypeNamesOfArgumentValue.Contains(psTypeNameRequestedByParameter, StringComparer.OrdinalIgnoreCase))
            {
                // win8: 228176..The callers know when to ignore and when not to ignore invalid cast exceptions.
                PSInvalidCastException e = new PSInvalidCastException(nameof(ErrorCategory.InvalidArgument),
                        null,
                        ParameterBinderStrings.MismatchedPSTypeName,
                        (_invocationInfo != null) && (_invocationInfo.MyCommand != null) ? _invocationInfo.MyCommand.Name : string.Empty,
                        parameterMetadata.Name,
                        parameterMetadata.Type,
                        parameterValue.GetType(),
                        0,
                        0,
                        psTypeNameRequestedByParameter);

                ParameterBindingException parameterBindingException;
                if (!retryOtherBindingAfterFailure)
                {
                    parameterBindingException = new ParameterBindingArgumentTransformationException(
                        e,
                        ErrorCategory.InvalidArgument,
                        this.InvocationInfo,
                        GetErrorExtent(parameter),
                        parameterMetadata.Name,
                        parameterMetadata.Type,
                        parameterValue.GetType(),
                        ParameterBinderStrings.MismatchedPSTypeName,
                        "MismatchedPSTypeName",
                        psTypeNameRequestedByParameter);
                }
                else
                {
                    parameterBindingException = new ParameterBindingException(
                        e,
                        ErrorCategory.InvalidArgument,
                        this.InvocationInfo,
                        GetErrorExtent(parameter),
                        parameterMetadata.Name,
                        parameterMetadata.Type,
                        parameterValue.GetType(),
                        ParameterBinderStrings.MismatchedPSTypeName,
                        "MismatchedPSTypeName",
                        psTypeNameRequestedByParameter);
                }

                throw parameterBindingException;
            }
        }

        /// <summary>
        /// Does all the type coercion, data generation, and validation necessary to bind the
        /// parameter, then calls the protected BindParameter method to have
        /// the derived class do the actual binding.
        /// </summary>
        /// <param name="parameter">
        /// The parameter to be bound.
        /// </param>
        /// <param name="parameterMetadata">
        /// The metadata for the parameter to use in guiding the binding.
        /// </param>
        /// <param name="flags">
        /// Flags for type coercion and validation.
        /// </param>
        /// <returns>
        /// True if the parameter was successfully bound. False if <paramref name="coerceTypeIfNeeded"/>
        /// is false and the type does not match the parameter type.
        /// </returns>
        /// <remarks>
        /// The binding algorithm goes as follows:
        /// 1. The data generation attributes are run
        /// 2. The data is coerced into the correct type
        /// 3. The data if validated using the validation attributes
        /// 4. The data is encoded into the bindable object using the
        ///    protected BindParameter method.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="parameter"/> or <paramref name="parameterMetadata"/> is null.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If argument transformation fails.
        /// or
        /// The argument could not be coerced to the appropriate type for the parameter.
        /// or
        /// The parameter argument transformation, prerequisite, or validation failed.
        /// or
        /// If the binding to the parameter fails.
        /// </exception>
        internal virtual bool BindParameter(
            CommandParameterInternal parameter,
            CompiledCommandParameter parameterMetadata,
            ParameterBindingFlags flags)
        {
            bool result = false;
            bool coerceTypeIfNeeded = (flags & ParameterBindingFlags.ShouldCoerceType) != 0;
            bool isDefaultValue = (flags & ParameterBindingFlags.IsDefaultValue) != 0;

            if (parameter == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameter));
            }

            if (parameterMetadata == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(parameterMetadata));
            }

            using (bindingTracer.TraceScope(
                       "BIND arg [{0}] to parameter [{1}]",
                       parameter.ArgumentValue,
                       parameterMetadata.Name))
            {
                // Set the complete parameter name

                parameter.ParameterName = parameterMetadata.Name;

                object parameterValue = parameter.ArgumentValue;

                do // false loop
                {
                    // Now call any argument transformation attributes that might be present on the parameter
                    ScriptParameterBinder spb = this as ScriptParameterBinder;
                    bool usesCmdletBinding = false;
                    if (spb != null)
                    {
                        usesCmdletBinding = spb.Script.UsesCmdletBinding;
                    }

                    // Now do the argument transformation. No transformation is done for the default values in script that meet the following 2 conditions:
                    //  1. the default value is not specified by the user, but is the powershell default value.
                    //     e.g. the powershell default value for a class type is null, for the string type is string.empty.
                    //  2. the powershell default value is null.
                    // This is to prevent ArgumentTransformationAttributes from making a non-mandatory parameter behave like a mandatory one.
                    // Example:
                    //   ## without the fix, 'CredentialAttribute' would make $Credential like a mandatory parameter
                    //   ## 'PS> test-credential' would prompt for credential input
                    //   function test-credential {
                    //       param(
                    //           [System.Management.Automation.CredentialAttribute()]
                    //           $Credential
                    //       )
                    //       $Credential
                    //   }
                    //
                    foreach (ArgumentTransformationAttribute dma in parameterMetadata.ArgumentTransformationAttributes)
                    {
                        using (bindingTracer.TraceScope(
                            "Executing DATA GENERATION metadata: [{0}]",
                            dma.GetType()))
                        {
                            try
                            {
                                ArgumentTypeConverterAttribute argumentTypeConverter = dma as ArgumentTypeConverterAttribute;

                                if (argumentTypeConverter != null)
                                {
                                    if (coerceTypeIfNeeded)
                                    {
                                        parameterValue = argumentTypeConverter.Transform(_engine, parameterValue, true, usesCmdletBinding);
                                    }
                                }
                                else
                                {
                                    // Only apply argument transformation when the argument is not null, is mandatory, or disallows null as a value.
                                    //
                                    // If we are binding default value for an unbound script parameter, this parameter is guaranteed not mandatory
                                    // in the chosen parameter set. This is because:
                                    //  1. If we use cmdlet binding for this script (CmdletParameterBinderController is used), then binding
                                    //     default value to unbound parameters won't happen until after all mandatory parameters from the
                                    //     chosen parameter set are handled. Therefore, the unbound parameter we are dealing with here won't
                                    //     be mandatory in the chosen parameter set.
                                    //  2. If we use script binding (ScriptParameterBinderController is used), then parameters won't have the
                                    //     ParameterAttribute declared for them, and thus are definitely not mandatory.
                                    // So we check 'IsParameterMandatory' only if we are not binding default values.
                                    if ((parameterValue != null) ||
                                        (!isDefaultValue && (parameterMetadata.IsMandatoryInSomeParameterSet ||
                                                             parameterMetadata.CannotBeNull ||
                                                             dma.TransformNullOptionalParameters)))
                                    {
                                        parameterValue = dma.TransformInternal(_engine, parameterValue);
                                    }
                                }

                                bindingTracer.WriteLine(
                                    "result returned from DATA GENERATION: {0}",
                                    parameterValue);
                            }
                            catch (Exception e) // Catch-all OK, 3rd party callout
                            {
                                bindingTracer.WriteLine(
                                    "ERROR: DATA GENERATION: {0}",
                                    e.Message);

                                ParameterBindingException bindingException =
                                        new ParameterBindingArgumentTransformationException(
                                            e,
                                            ErrorCategory.InvalidData,
                                            this.InvocationInfo,
                                            GetErrorExtent(parameter),
                                            parameterMetadata.Name,
                                            parameterMetadata.Type,
                                            parameterValue?.GetType(),
                                            ParameterBinderStrings.ParameterArgumentTransformationError,
                                            "ParameterArgumentTransformationError",
                                            e.Message);
                                throw bindingException;
                            }
                        }
                    }

                    // Only try to coerce the type if asked. If not asked,
                    // see if the value type matches or is a subclass of
                    // the parameter type.

                    if (coerceTypeIfNeeded)
                    {
                        // Now do the type coercion

                        parameterValue =
                            CoerceTypeAsNeeded(
                                parameter,
                                parameterMetadata.Name,
                                parameterMetadata.Type,
                                parameterMetadata.CollectionTypeInformation,
                                parameterValue);
                    }
                    else
                    {
                        if (!ShouldContinueUncoercedBind(parameter, parameterMetadata, flags, ref parameterValue))
                        {
                            // Don't attempt the bind because the value
                            // is not of the correct
                            // type for the parameter.
                            break;
                        }
                    }

                    if ((parameterMetadata.PSTypeName != null) && (parameterValue != null))
                    {
                        IEnumerable parameterValueAsEnumerable = LanguagePrimitives.GetEnumerable(parameterValue);
                        if (parameterValueAsEnumerable != null)
                        {
                            foreach (object o in parameterValueAsEnumerable)
                            {
                                this.ValidatePSTypeName(parameter, parameterMetadata, !coerceTypeIfNeeded, o);
                            }
                        }
                        else
                        {
                            this.ValidatePSTypeName(parameter, parameterMetadata, !coerceTypeIfNeeded, parameterValue);
                        }
                    }

                    // Now do the data validation.  No validation is done for default values in script as that is
                    // one way for people to have a known "bad" value to detect unspecified parameters.

                    if (!isDefaultValue)
                    {
                        for (int i = 0; i < parameterMetadata.ValidationAttributes.Length; i++)
                        {
                            var validationAttribute = parameterMetadata.ValidationAttributes[i];

                            using (bindingTracer.TraceScope(
                                "Executing VALIDATION metadata: [{0}]",
                                validationAttribute.GetType()))
                            {
                                try
                                {
                                    validationAttribute.InternalValidate(parameterValue, _engine);
                                }
                                catch (Exception e) // Catch-all OK, 3rd party callout
                                {
                                    bindingTracer.WriteLine(
                                        "ERROR: VALIDATION FAILED: {0}",
                                        e.Message);

                                    ParameterBindingValidationException bindingException =
                                        new ParameterBindingValidationException(
                                            e,
                                            ErrorCategory.InvalidData,
                                            this.InvocationInfo,
                                            GetErrorExtent(parameter),
                                            parameterMetadata.Name,
                                            parameterMetadata.Type,
                                            parameterValue?.GetType(),
                                            ParameterBinderStrings.ParameterArgumentValidationError,
                                            "ParameterArgumentValidationError",
                                            e.Message);
                                    throw bindingException;
                                }

                                s_tracer.WriteLine("Validation attribute on {0} returned {1}.", parameterMetadata.Name, result);
                            }
                        }

                        // If the is null, an empty string, or an empty collection,
                        // check the parameter metadata to ensure that binding can continue
                        // This method throws an appropriate ParameterBindingException
                        // if binding cannot continue. If it returns then binding can
                        // proceed.
                        if (parameterMetadata.IsMandatoryInSomeParameterSet)
                        {
                            ValidateNullOrEmptyArgument(parameter, parameterMetadata, parameterMetadata.Type, parameterValue, true);
                        }
                    }

                    // Write out obsolete parameter warning only if
                    //  1. We are binding parameters for a simple function/script
                    //  2. We are not binding a default parameter value

                    if (parameterMetadata.ObsoleteAttribute != null &&
                        (!isDefaultValue) &&
                        spb != null && !usesCmdletBinding)
                    {
                        string obsoleteWarning = string.Format(
                            CultureInfo.InvariantCulture,
                            ParameterBinderStrings.UseOfDeprecatedParameterWarning,
                            parameterMetadata.Name,
                            parameterMetadata.ObsoleteAttribute.Message);

                        var mshCommandRuntime = this.Command.commandRuntime as MshCommandRuntime;

                        // Write out warning only if we are in the context of MshCommandRuntime.
                        // This is because
                        //  1. The overload method WriteWarning(WarningRecord) is only available in MshCommandRuntime;
                        //  2. We write out warnings for obsolete commands and obsolete cmdlet parameters only when in
                        //     the context of MshCommandRuntime. So we do it here to keep consistency.
                        mshCommandRuntime?.WriteWarning(new WarningRecord(FQIDParameterObsolete, obsoleteWarning));
                    }

                    // Finally bind the argument to the parameter

                    Exception bindError = null;

                    try
                    {
                        BindParameter(parameter.ParameterName, parameterValue, parameterMetadata);
                        result = true;
                    }
                    catch (SetValueException setValueException)
                    {
                        bindError = setValueException;
                    }

                    if (bindError != null)
                    {
                        Type specifiedType = parameterValue?.GetType();
                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                bindError,
                                ErrorCategory.WriteError,
                                this.InvocationInfo,
                                GetErrorExtent(parameter),
                                parameterMetadata.Name,
                                parameterMetadata.Type,
                                specifiedType,
                                ParameterBinderStrings.ParameterBindingFailed,
                                "ParameterBindingFailed",
                                bindError.Message);

                        throw bindingException;
                    }
                }
                while (false);

                bindingTracer.WriteLine(
                    "BIND arg [{0}] to param [{1}] {2}",
                    parameterValue,
                    parameter.ParameterName,
                    (result) ? "SUCCESSFUL" : "SKIPPED");

                if (result)
                {
                    // Add this name to the set of bound parameters...
                    if (RecordBoundParameters)
                    {
                        this.CommandLineParameters.Add(parameter.ParameterName, parameterValue);
                    }

                    MshCommandRuntime cmdRuntime = this.Command.commandRuntime as MshCommandRuntime;
                    if ((cmdRuntime != null) &&
                        (cmdRuntime.LogPipelineExecutionDetail || _isTranscribing) &&
                        (cmdRuntime.PipelineProcessor != null))
                    {
                        string stringToPrint = null;
                        try
                        {
                            // Unroll parameter value
                            IEnumerable values = LanguagePrimitives.GetEnumerable(parameterValue);
                            if (values != null)
                            {
                                var sb = new Text.StringBuilder(256);
                                var sep = string.Empty;
                                foreach (var value in values)
                                {
                                    sb.Append(sep);
                                    sep = ", ";
                                    sb.Append(value);
                                    // For better performance, avoid logging too much
                                    if (sb.Length > 256)
                                    {
                                        sb.Append(", ...");
                                        break;
                                    }
                                }

                                stringToPrint = sb.ToString();
                            }
                            else if (parameterValue != null)
                            {
                                stringToPrint = parameterValue.ToString();
                            }
                        }
                        catch (Exception) // Catch-all OK, 3rd party callout
                        {
                        }

                        if (stringToPrint != null)
                        {
                            cmdRuntime.PipelineProcessor.LogExecutionParameterBinding(this.InvocationInfo, parameter.ParameterName, stringToPrint);
                        }
                    }
                }

                return result;
            }
        }

        /// <summary>
        /// This method ensures that if the parameter is mandatory, and AllowNull, AllowEmptyString,
        /// and/or AllowEmptyCollection is not specified, then argument is not null or empty.
        /// </summary>
        /// <param name="parameter">
        /// The argument token.
        /// </param>
        /// <param name="parameterMetadata">
        /// The metadata for the parameter.
        /// </param>
        /// <param name="argumentType">
        /// The type of the argument to validate against.
        /// </param>
        /// <param name="parameterValue">
        /// The value that will be bound to the parameter.
        /// </param>
        /// <param name="recurseIntoCollections">
        /// If true, then elements of collections will be validated against the metadata.
        /// </param>
        private void ValidateNullOrEmptyArgument(
            CommandParameterInternal parameter,
            CompiledCommandParameter parameterMetadata,
            Type argumentType,
            object parameterValue,
            bool recurseIntoCollections)
        {
            if (parameterValue == null && argumentType != typeof(bool?))
            {
                if (!parameterMetadata.AllowsNullArgument)
                {
                    bindingTracer.WriteLine("ERROR: Argument cannot be null");

                    ParameterBindingValidationException bindingException =
                        new ParameterBindingValidationException(
                            ErrorCategory.InvalidData,
                            this.InvocationInfo,
                            GetErrorExtent(parameter),
                            parameterMetadata.Name,
                            argumentType,
                            null,
                            ParameterBinderStrings.ParameterArgumentValidationErrorNullNotAllowed,
                            "ParameterArgumentValidationErrorNullNotAllowed");
                    throw bindingException;
                }

                return;
            }

            if (argumentType == typeof(string))
            {
                // Since the parameter is of type string, verify that either the argument
                // is not null and not empty or that the parameter can accept null or empty.
                string stringParamValue = parameterValue as string;
                Diagnostics.Assert(
                    stringParamValue != null,
                    "Type coercion should have already converted the argument value to a string");

                if (stringParamValue.Length == 0 && !parameterMetadata.AllowsEmptyStringArgument)
                {
                    bindingTracer.WriteLine("ERROR: Argument cannot be an empty string");

                    ParameterBindingValidationException bindingException =
                        new ParameterBindingValidationException(
                            ErrorCategory.InvalidData,
                            this.InvocationInfo,
                            GetErrorExtent(parameter),
                            parameterMetadata.Name,
                            parameterMetadata.Type,
                            parameterValue?.GetType(),
                            ParameterBinderStrings.ParameterArgumentValidationErrorEmptyStringNotAllowed,
                            "ParameterArgumentValidationErrorEmptyStringNotAllowed");
                    throw bindingException;
                }

                return;
            }

            if (!recurseIntoCollections)
                return;

            switch (parameterMetadata.CollectionTypeInformation.ParameterCollectionType)
            {
                case ParameterCollectionType.IList:
                case ParameterCollectionType.Array:
                case ParameterCollectionType.ICollectionGeneric:
                    break;
                default:
                    // not a recognized collection, no need to recurse
                    return;
            }

            // All these collection types implement IEnumerable
            IEnumerator ienum = LanguagePrimitives.GetEnumerator(parameterValue);
            Diagnostics.Assert(
                ienum != null,
                "Type coercion should have already converted the argument value to an IEnumerator");

            // Ensure that each element abides by the metadata
            bool isEmpty = true;
            Type elementType = parameterMetadata.CollectionTypeInformation.ElementType;
            bool isElementValueType = elementType != null && elementType.IsValueType;

            // Note - we explicitly don't pass the context here because we don't want
            // the overhead of the calls that check for stopping.
            if (ParserOps.MoveNext(null, null, ienum)) { isEmpty = false; }

            // If the element of the collection is of value type, then no need to check for null
            // because a value-type value cannot be null.
            if (!isEmpty && !isElementValueType)
            {
                do
                {
                    object element = ParserOps.Current(null, ienum);
                    ValidateNullOrEmptyArgument(
                        parameter,
                        parameterMetadata,
                        parameterMetadata.CollectionTypeInformation.ElementType,
                        element,
                        false);
                } while (ParserOps.MoveNext(null, null, ienum));
            }

            if (isEmpty && !parameterMetadata.AllowsEmptyCollectionArgument)
            {
                bindingTracer.WriteLine("ERROR: Argument cannot be an empty collection");

                string errorId, resourceString;
                if (parameterMetadata.CollectionTypeInformation.ParameterCollectionType == ParameterCollectionType.Array)
                {
                    errorId = "ParameterArgumentValidationErrorEmptyArrayNotAllowed";
                    resourceString = ParameterBinderStrings.ParameterArgumentValidationErrorEmptyArrayNotAllowed;
                }
                else
                {
                    errorId = "ParameterArgumentValidationErrorEmptyCollectionNotAllowed";
                    resourceString = ParameterBinderStrings.ParameterArgumentValidationErrorEmptyCollectionNotAllowed;
                }

                ParameterBindingValidationException bindingException =
                    new ParameterBindingValidationException(
                        ErrorCategory.InvalidData,
                        this.InvocationInfo,
                        GetErrorExtent(parameter),
                        parameterMetadata.Name,
                        parameterMetadata.Type,
                        parameterValue?.GetType(),
                        resourceString,
                        errorId);
                throw bindingException;
            }
        }

        private bool ShouldContinueUncoercedBind(
            CommandParameterInternal parameter,
            CompiledCommandParameter parameterMetadata,
            ParameterBindingFlags flags,
            ref object parameterValue)
        {
            bool isDefaultValue = (flags & ParameterBindingFlags.IsDefaultValue) != 0;
            Type parameterType = parameterMetadata.Type;

            if (parameterValue == null)
            {
                return parameterType == null ||
                       isDefaultValue ||
                       (!parameterType.IsValueType &&
                        parameterType != typeof(string));
            }

            // If the types are not a direct match, or
            // the value type is not a subclass of the parameter type, or
            // the value is an PSObject and the parameter type is not object and
            //     the PSObject.BaseObject type does not match or is not a subclass
            //     of the parameter type, or
            // the value must be encoded into a collection but it is not of the correct element type
            //
            // then return false

            if (parameterType.IsInstanceOfType(parameterValue))
            {
                return true;
            }

            var psobj = parameterValue as PSObject;
            if (psobj != null && !psobj.ImmediateBaseObjectIsEmpty)
            {
                // See if the base object is of the same type or
                // as subclass of the parameter

                parameterValue = psobj.BaseObject;

                if (parameterType.IsInstanceOfType(parameterValue))
                {
                    return true;
                }
            }

            // Maybe the parameter type is a collection and the value needs to
            // be encoded

            if (parameterMetadata.CollectionTypeInformation.ParameterCollectionType != ParameterCollectionType.NotCollection)
            {
                // See if the value needs to be encoded in a collection

                bool coercionRequired;
                object encodedValue =
                    EncodeCollection(
                        parameter,
                        parameterMetadata.Name,
                        parameterMetadata.CollectionTypeInformation,
                        parameterType,
                        parameterValue,
                        false,
                        out coercionRequired);

                if (encodedValue == null || coercionRequired)
                {
                    // Don't attempt the bind because the
                    // PSObject BaseObject is not of the correct
                    // type for the parameter.
                    return false;
                }

                parameterValue = encodedValue;
                return true;
            }

            return false;
        }

        #endregion Parameter binding

        /// <summary>
        /// The invocation information for the code that is being bound.
        /// </summary>
        private readonly InvocationInfo _invocationInfo;

        internal InvocationInfo InvocationInfo
        {
            get
            {
                return _invocationInfo;
            }
        }

        /// <summary>
        /// The context of the currently running engine.
        /// </summary>
        private readonly ExecutionContext _context;

        internal ExecutionContext Context
        {
            get
            {
                return _context;
            }
        }

        /// <summary>
        /// An instance of InternalCommand that the binder is binding to.
        /// </summary>
        private readonly InternalCommand _command;

        internal InternalCommand Command
        {
            get
            {
                return _command;
            }
        }

        /// <summary>
        /// The engine APIs that need to be passed the attributes when evaluated.
        /// </summary>
        private readonly EngineIntrinsics _engine;

        private readonly bool _isTranscribing;

        #endregion internal members

        #region Private helpers

        /// <summary>
        /// Coerces the argument type to the parameter value type as needed.
        /// </summary>
        /// <param name="argument">
        /// The argument as was specified by the command line.
        /// </param>
        /// <param name="parameterName">
        /// The name of the parameter that the coercion is taking place to bind to. It is
        /// used only for error reporting.
        /// </param>
        /// <param name="toType">
        /// The type to coerce the value to.
        /// </param>
        /// <param name="collectionTypeInfo">
        /// The information about the collection type, like element type, etc.
        /// </param>
        /// <param name="currentValue">
        /// The current value of the argument.
        /// </param>
        /// <returns>
        /// The value of the argument in the type of the parameter.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="argument"/> or <paramref name="toType"/> is null.
        /// </exception>
        /// <exception cref="ParameterBindingException">
        /// If the argument value is missing and the parameter is not a bool or SwitchParameter.
        /// or
        /// If the argument value could not be converted to the parameter type.
        /// </exception>
        private object CoerceTypeAsNeeded(
            CommandParameterInternal argument,
            string parameterName,
            Type toType,
            ParameterCollectionTypeInformation collectionTypeInfo,
            object currentValue)
        {
            if (argument == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(argument));
            }

            if (toType == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(toType));
            }

            // Construct the collection type information if it wasn't passed in.

            collectionTypeInfo ??= new ParameterCollectionTypeInformation(toType);

            object originalValue = currentValue;
            object result = currentValue;

            using (bindingTracer.TraceScope(
                "COERCE arg to [{0}]", toType))
            {
                Type argumentType = null;
                try
                {
                    do // false loop
                    {
                        if (IsNullParameterValue(currentValue))
                        {
                            result = HandleNullParameterForSpecialTypes(argument, parameterName, toType, currentValue);
                            break;
                        }

                        // Do the coercion
                        argumentType = currentValue.GetType();

                        // If the types are identical (or can be cast directly,) then no coercion
                        // needs to be done
                        if (toType.IsAssignableFrom(argumentType))
                        {
                            bindingTracer.WriteLine(
                                "Parameter and arg types the same, no coercion is needed.");

                            result = currentValue;
                            break;
                        }

                        bindingTracer.WriteLine("Trying to convert argument value from {0} to {1}", argumentType, toType);

                        // Likewise shortcircuit the case were the user has asked for a shell object.
                        // He always gets a shell object regardless of the actual type of the object.
                        if (toType == typeof(PSObject))
                        {
                            // It may be the case that we're binding the current pipeline object
                            // as is to a PSObject parameter in which case, we want to make
                            // sure that we're using the same shell object instead of creating an
                            // alias object.
                            if (_command != null &&
                                currentValue == _command.CurrentPipelineObject.BaseObject)
                            {
                                currentValue = _command.CurrentPipelineObject;
                            }

                            bindingTracer.WriteLine(
                                "The parameter is of type [{0}] and the argument is an PSObject, so the parameter value is the argument value wrapped into an PSObject.",
                                toType);
                            result = LanguagePrimitives.AsPSObjectOrNull(currentValue);
                            break;
                        }

                        // NTRAID#Windows OS Bugs-1064175-2004/02/28-JeffJon
                        // If we have an PSObject with null base and we are trying to
                        // convert to a string, then we need to use null instead of
                        // calling LanguagePrimitives.ConvertTo as that will return
                        // string.Empty.

                        if (toType == typeof(string) &&
                            argumentType == typeof(PSObject))
                        {
                            PSObject currentValueAsPSObject = (PSObject)currentValue;

                            if (currentValueAsPSObject == AutomationNull.Value)
                            {
                                bindingTracer.WriteLine(
                                    "CONVERT a null PSObject to a null string.");
                                result = null;
                                break;
                            }
                        }

                        // NTRAID#Windows OS Bugs -<bug id here> - Nana
                        // If we have a boolean, we have to ensure that it can
                        // only take parameters of type boolean or numbers with
                        // 0 indicating false and everything else indicating true
                        // Anything else passed should be reported as an error

                        if (toType == typeof(bool) || toType == typeof(SwitchParameter) ||
                            toType == typeof(bool?))
                        {
                            Type boType = null;
                            if (argumentType == typeof(PSObject))
                            {
                                // Unwrap the PSObject at this point...
                                PSObject currentValueAsPSObject = (PSObject)currentValue;
                                currentValue = currentValueAsPSObject.BaseObject;

                                if (currentValue is SwitchParameter)
                                {
                                    currentValue = ((SwitchParameter)currentValue).IsPresent;
                                }

                                boType = currentValue.GetType();
                            }
                            else
                            {
                                boType = argumentType;
                            }

                            if (boType == typeof(bool))
                            {
                                if (LanguagePrimitives.IsBooleanType(toType))
                                    result = ParserOps.BoolToObject((bool)currentValue);
                                else
                                    result = new SwitchParameter((bool)currentValue);
                            }
                            else if (boType == typeof(int))
                            {
                                if ((int)LanguagePrimitives.ConvertTo(currentValue,
                                            typeof(int), CultureInfo.InvariantCulture) != 0)
                                {
                                    if (LanguagePrimitives.IsBooleanType(toType))
                                        result = ParserOps.BoolToObject(true);
                                    else
                                        result = new SwitchParameter(true);
                                }
                                else
                                {
                                    if (LanguagePrimitives.IsBooleanType(toType))
                                        result = ParserOps.BoolToObject(false);
                                    else
                                        result = new SwitchParameter(false);
                                }
                            }
                            else if (LanguagePrimitives.IsNumeric(boType.GetTypeCode()))
                            {
                                double currentValueAsDouble = (double)LanguagePrimitives.ConvertTo(
                                                                        currentValue, typeof(double), CultureInfo.InvariantCulture);

                                if (currentValueAsDouble != 0)
                                {
                                    if (LanguagePrimitives.IsBooleanType(toType))
                                        result = ParserOps.BoolToObject(true);
                                    else
                                        result = new SwitchParameter(true);
                                }
                                else
                                {
                                    if (LanguagePrimitives.IsBooleanType(toType))
                                        result = ParserOps.BoolToObject(false);
                                    else
                                        result = new SwitchParameter(false);
                                }
                            }
                            else
                            {
                                // Invalid types which cannot be associated with a bool
                                // Since there is a catch block which appropriately
                                // handles this situation we just throw an exception here
                                // throw new PSInvalidCastException();
                                ParameterBindingException pbe =
                                    new ParameterBindingException(
                                        ErrorCategory.InvalidArgument,
                                        this.InvocationInfo,
                                        GetErrorExtent(argument),
                                        parameterName,
                                        toType,
                                        argumentType,
                                        ParameterBinderStrings.CannotConvertArgument,
                                        "CannotConvertArgument",
                                        boType,
                                        string.Empty);

                                throw pbe;
                            }

                            break;
                        }

                        // NTRAID#Windows OS Bugs-1009284-2004/05/05-JeffJon
                        // Need to handle other collection types here as well

                        // Before attempting to encode a collection, we check if we can convert the argument directly via
                        // a restricted set of conversions (as the general conversion mechanism is far too general, it may
                        // succeed where it shouldn't.  We don't bother checking arrays parameters because they won't have
                        // conversions we're allowing.
                        if (collectionTypeInfo.ParameterCollectionType == ParameterCollectionType.ICollectionGeneric
                            || collectionTypeInfo.ParameterCollectionType == ParameterCollectionType.IList)
                        {
                            object currentValueToConvert = PSObject.Base(currentValue);
                            if (currentValueToConvert != null)
                            {
                                ConversionRank rank = LanguagePrimitives.GetConversionRank(currentValueToConvert.GetType(), toType);
                                if (rank == ConversionRank.Constructor || rank == ConversionRank.ImplicitCast || rank == ConversionRank.ExplicitCast)
                                {
                                    // This conversion will fail in the common case, and when it does, we'll use EncodeCollection below.
                                    if (LanguagePrimitives.TryConvertTo(currentValue, toType, CultureInfo.CurrentCulture, out result))
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        if (collectionTypeInfo.ParameterCollectionType != ParameterCollectionType.NotCollection)
                        {
                            bindingTracer.WriteLine(
                                "ENCODING arg into collection");

                            bool ignored = false;
                            result =
                                EncodeCollection(
                                    argument,
                                    parameterName,
                                    collectionTypeInfo,
                                    toType,
                                    currentValue,
                                    (collectionTypeInfo.ElementType != null),
                                    out ignored);

                            break;
                        }
                        else
                        {
                            // Check to see if the current value is a collection. If so, fail because
                            // we don't want to attempt to bind a collection to a scalar unless
                            // the parameter type is Object or PSObject or enum.

                            if (GetIList(currentValue) != null &&
                                toType != typeof(object) &&
                                toType != typeof(PSObject) &&
                                toType != typeof(PSListModifier) &&
                                (!toType.IsGenericType || toType.GetGenericTypeDefinition() != typeof(PSListModifier<>)) &&
                                (!toType.IsGenericType || toType.GetGenericTypeDefinition() != typeof(FlagsExpression<>)) &&
                                !toType.IsEnum)
                            {
                                throw new NotSupportedException();
                            }
                        }

                        bindingTracer.WriteLine(
                            "CONVERT arg type to param type using LanguagePrimitives.ConvertTo");

                        // If we are in constrained language mode and the target command is trusted, which is often
                        // the case for C# cmdlets, then we allow type conversion to the target parameter type.
                        //
                        // However, we don't allow Hashtable-to-Object conversion (PSObject and IDictionary) because
                        // those can lead to property setters that probably aren't expected. This is enforced by
                        // setting 'Context.LanguageModeTransitionInParameterBinding' to true before the conversion.
                        bool changeLanguageModeForTrustedCommand =
                            Context.LanguageMode == PSLanguageMode.ConstrainedLanguage &&
                            this.Command.CommandInfo.DefiningLanguageMode == PSLanguageMode.FullLanguage;
                        bool oldLangModeTransitionStatus = Context.LanguageModeTransitionInParameterBinding;

                        try
                        {
                            if (changeLanguageModeForTrustedCommand)
                            {
                                Context.LanguageMode = PSLanguageMode.FullLanguage;
                                Context.LanguageModeTransitionInParameterBinding = true;
                            }

                            result = LanguagePrimitives.ConvertTo(currentValue, toType, CultureInfo.CurrentCulture);
                        }
                        finally
                        {
                            if (changeLanguageModeForTrustedCommand)
                            {
                                Context.LanguageMode = PSLanguageMode.ConstrainedLanguage;
                                Context.LanguageModeTransitionInParameterBinding = oldLangModeTransitionStatus;
                            }
                        }

                        bindingTracer.WriteLine(
                            "CONVERT SUCCESSFUL using LanguagePrimitives.ConvertTo: [{0}]",
                            (result == null) ? "null" : result.ToString());
                    } while (false);
                }
                catch (NotSupportedException notSupported)
                {
                    bindingTracer.TraceError(
                        "ERROR: COERCE FAILED: arg [{0}] could not be converted to the parameter type [{1}]",
                        result ?? "null",
                        toType);

                    ParameterBindingException pbe =
                        new ParameterBindingException(
                            notSupported,
                            ErrorCategory.InvalidArgument,
                            this.InvocationInfo,
                            GetErrorExtent(argument),
                            parameterName,
                            toType,
                            argumentType,
                            ParameterBinderStrings.CannotConvertArgument,
                            "CannotConvertArgument",
                            result ?? "null",
                            notSupported.Message);

                    throw pbe;
                }
                catch (PSInvalidCastException invalidCast)
                {
                    bindingTracer.TraceError(
                      "ERROR: COERCE FAILED: arg [{0}] could not be converted to the parameter type [{1}]",
                      result ?? "null",
                      toType);

                    ParameterBindingException pbe =
                        new ParameterBindingException(
                            invalidCast,
                            ErrorCategory.InvalidArgument,
                            this.InvocationInfo,
                            GetErrorExtent(argument),
                            parameterName,
                            toType,
                            argumentType,
                            ParameterBinderStrings.CannotConvertArgumentNoMessage,
                            "CannotConvertArgumentNoMessage",
                            invalidCast.Message);

                    throw pbe;
                }
            }

            if (result != null)
            {
                // Set the converted result object untrusted if necessary
                ExecutionContext.PropagateInputSource(originalValue, result, Context.LanguageMode);
            }

            return result;
        }

        private static bool IsNullParameterValue(object currentValue)
        {
            bool result = false;

            if (currentValue == null ||
                currentValue == AutomationNull.Value ||
                currentValue == UnboundParameter.Value)
            {
                result = true;
            }

            return result;
        }

        private object HandleNullParameterForSpecialTypes(
            CommandParameterInternal argument,
            string parameterName,
            Type toType,
            object currentValue)
        {
            object result = null;

            // The presence of the name switch for SwitchParameters (and not booleans)
            // makes them true.

            if (toType == typeof(bool))
            {
                bindingTracer.WriteLine(
                        "ERROR: No argument is specified for parameter and parameter type is BOOL");

                ParameterBindingException exception =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        this.InvocationInfo,
                        GetErrorExtent(argument),
                        parameterName,
                        toType,
                        null,
                        ParameterBinderStrings.ParameterArgumentValidationErrorNullNotAllowed,
                        "ParameterArgumentValidationErrorNullNotAllowed",
                        string.Empty);

                throw exception;
            }
            else
                if (toType == typeof(SwitchParameter))
            {
                bindingTracer.WriteLine(
                    "Arg is null or not present, parameter type is SWITCHPARAMTER, value is true.");
                result = SwitchParameter.Present;
            }
            else if (currentValue == UnboundParameter.Value)
            {
                bindingTracer.TraceError(
                    "ERROR: No argument was specified for the parameter and the parameter is not of type bool");

                ParameterBindingException exception =
                    new ParameterBindingException(
                        ErrorCategory.InvalidArgument,
                        this.InvocationInfo,
                        GetParameterErrorExtent(argument),
                        parameterName,
                        toType,
                        null,
                        ParameterBinderStrings.MissingArgument,
                        "MissingArgument");

                throw exception;
            }
            else
            {
                bindingTracer.WriteLine(
                    "Arg is null, parameter type not bool or SwitchParameter, value is null.");
                result = null;
            }

            return result;
        }

        /// <summary>
        /// Takes the current value specified and converts or adds it to
        /// a collection of the appropriate type.
        /// </summary>
        /// <param name="argument">
        /// The argument the current value comes from. Used for error reporting.
        /// </param>
        /// <param name="parameterName">
        /// The name of the parameter.
        /// </param>
        /// <param name="collectionTypeInformation">
        /// The collection type information to which the current value will be
        /// encoded.
        /// </param>
        /// <param name="toType">
        /// The type the current value will be converted to.
        /// </param>
        /// <param name="currentValue">
        /// The value to be encoded.
        /// </param>
        /// <param name="coerceElementTypeIfNeeded">
        /// If true, the element will be coerced into the appropriate type
        /// for the collection. If false, and the element isn't of the appropriate
        /// type then the <paramref name="coercionRequired"/> out parameter will
        /// be true.
        /// </param>
        /// <param name="coercionRequired">
        /// This out parameter will be true if <paramref name="coerceElementTypeIfNeeded"/>
        /// is true and the value could not be encoded into the collection because it
        /// requires coercion to the element type.
        /// </param>
        /// <returns>
        /// A collection of the appropriate type containing the specified value.
        /// </returns>
        /// <exception cref="ParameterBindingException">
        /// If <paramref name="currentValue"/> is a collection and one of its values
        /// cannot be coerced into the appropriate type.
        /// or
        /// A collection of the appropriate <paramref name="collectionTypeInformation"/>
        /// could not be created.
        /// </exception>
        [SuppressMessage("Microsoft.Maintainability", "CA1505:AvoidUnmaintainableCode")]
        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Consider Simplyfing it")]
        private object EncodeCollection(
            CommandParameterInternal argument,
            string parameterName,
            ParameterCollectionTypeInformation collectionTypeInformation,
            Type toType,
            object currentValue,
            bool coerceElementTypeIfNeeded,
            out bool coercionRequired)
        {
            object originalValue = currentValue;
            object result = null;
            coercionRequired = false;

            do // false loop
            {
                bindingTracer.WriteLine(
                    "Binding collection parameter {0}: argument type [{1}], parameter type [{2}], collection type {3}, element type [{4}], {5}",
                    parameterName,
                    (currentValue == null) ? "null" : currentValue.GetType().Name,
                    toType,
                    collectionTypeInformation.ParameterCollectionType,
                    collectionTypeInformation.ElementType,
                    coerceElementTypeIfNeeded ? "coerceElementType" : "no coerceElementType");

                if (currentValue == null)
                {
                    break;
                }

                int numberOfElements = 1;
                Type collectionElementType = collectionTypeInformation.ElementType;

                // If the current value is an IList, get the count of the elements
                // Or if it is an PSObject which wraps an IList

                IList currentValueAsIList = GetIList(currentValue);

                if (currentValueAsIList != null)
                {
                    numberOfElements = currentValueAsIList.Count;

                    s_tracer.WriteLine("current value is an IList with {0} elements", numberOfElements);
                    bindingTracer.WriteLine(
                        "Arg is IList with {0} elements",
                        numberOfElements);
                }

                object resultCollection = null;
                IList resultAsIList = null;
                MethodInfo addMethod = null;

                // We must special case System.Array to be like an object array since it is an
                // abstract base class and cannot be created in the IList path below.
                bool isSystemDotArray = (toType == typeof(System.Array));

                if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.Array ||
                    isSystemDotArray)
                {
                    if (isSystemDotArray)
                    {
                        // If System.Array is the type we are encoding to, then
                        // the element type should be System.Object.

                        collectionElementType = typeof(object);
                    }

                    bindingTracer.WriteLine(
                        "Creating array with element type [{0}] and {1} elements",
                        collectionElementType,
                        numberOfElements);

                    // Since the destination is an array we will have to create an array
                    // of the element type with the correct length

                    resultCollection = resultAsIList =
                        (IList)Array.CreateInstance(
                            collectionElementType,
                            numberOfElements);
                }
                else if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.IList ||
                         collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.ICollectionGeneric)
                {
                    bindingTracer.WriteLine(
                        "Creating collection [{0}]",
                        toType);

                    // Create an instance of the parameter type

                    // NTRAID#Windows Out Of Band Releases-906820-2005/09/01
                    // This code previously used the ctor(int) ctor form.
                    // System.Collections.ObjectModel.Collection<T> does not
                    // support this ctor form.  More generally, there is no
                    // guarantee that the ctor parameter has the semantic
                    // meaning of "likely list size".  Blindly calling the
                    // parameterless ctor is also risky, but seems like a
                    // safer choice.

                    bool errorOccurred = false;
                    Exception error = null;
                    try
                    {
                        resultCollection =
                            Activator.CreateInstance(
                                toType,
                                0,
                                null,
                                Array.Empty<object>(),
                                System.Globalization.CultureInfo.InvariantCulture);
                        if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.IList)
                            resultAsIList = (IList)resultCollection;
                        else
                        {
                            Diagnostics.Assert(
                                collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.ICollectionGeneric,
                                "invalid collection type"
                                );
                            // extract the ICollection<T>::Add(T) method
                            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
                            Type elementType = collectionTypeInformation.ElementType;
                            Diagnostics.Assert(elementType != null, "null ElementType");
                            Exception getMethodError = null;
                            try
                            {
                                addMethod = toType.GetMethod("Add", bindingFlags, null, new Type[1] { elementType }, null);
                            }
                            catch (AmbiguousMatchException e)
                            {
                                bindingTracer.WriteLine("Ambiguous match to Add(T) for type {0}: {1}", toType.FullName, e.Message);
                                getMethodError = e;
                            }
                            catch (ArgumentException e)
                            {
                                bindingTracer.WriteLine(
                                    "ArgumentException matching Add(T) for type {0}: {1}", toType.FullName, e.Message);
                                getMethodError = e;
                            }

                            if (addMethod == null)
                            {
                                ParameterBindingException bindingException =
                                    new ParameterBindingException(
                                        getMethodError,
                                        ErrorCategory.InvalidArgument,
                                        this.InvocationInfo,
                                        GetErrorExtent(argument),
                                        parameterName,
                                        toType,
                                        currentValue.GetType(),
                                        ParameterBinderStrings.CannotExtractAddMethod,
                                        "CannotExtractAddMethod",
                                        (getMethodError == null) ? string.Empty : getMethodError.Message);
                                throw bindingException;
                            }
                        }
                    }
                    catch (ArgumentException argException)
                    {
                        errorOccurred = true;
                        error = argException;
                    }
                    catch (NotSupportedException notSupported)
                    {
                        errorOccurred = true;
                        error = notSupported;
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        errorOccurred = true;
                        error = targetInvocationException;
                    }
                    catch (MethodAccessException methodAccessException)
                    {
                        errorOccurred = true;
                        error = methodAccessException;
                    }
                    catch (MemberAccessException memberAccessException)
                    {
                        errorOccurred = true;
                        error = memberAccessException;
                    }
                    catch (System.Runtime.InteropServices.InvalidComObjectException invalidComObject)
                    {
                        errorOccurred = true;
                        error = invalidComObject;
                    }
                    catch (System.Runtime.InteropServices.COMException comException)
                    {
                        errorOccurred = true;
                        error = comException;
                    }
                    catch (TypeLoadException typeLoadException)
                    {
                        errorOccurred = true;
                        error = typeLoadException;
                    }

                    if (errorOccurred)
                    {
                        // Throw a ParameterBindingException

                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                error,
                                ErrorCategory.InvalidArgument,
                                this.InvocationInfo,
                                GetErrorExtent(argument),
                                parameterName,
                                toType,
                                currentValue.GetType(),
                                ParameterBinderStrings.CannotConvertArgument,
                                "CannotConvertArgument",
                                "null",
                                error.Message);
                        throw bindingException;
                    }
                }
                else
                {
                    Diagnostics.Assert(
                        false,
                        "This method should not be called for a parameter that is not a collection");
                    break;
                }

                // NTRAID#Windows OS Bugs-966440-2004/05/05-JeffJon
                // This coercion can only go to a collection type.  It cannot take a
                // collection type and coerce it into a scalar type.

                // Now that the new collection instance has been created, coerce each element type
                // of the current value to the element type of the property value and add it

                if (currentValueAsIList != null)
                {
                    // Since arrays don't support the Add method, we must use indexing
                    // to set the value.
                    int arrayIndex = 0;

                    bindingTracer.WriteLine(
                        "Argument type {0} is IList",
                        currentValue.GetType());

                    foreach (object valueElement in currentValueAsIList)
                    {
                        object currentValueElement = PSObject.Base(valueElement);

                        if (coerceElementTypeIfNeeded)
                        {
                            bindingTracer.WriteLine(
                                "COERCE collection element from type {0} to type {1}",
                                (valueElement == null) ? "null" : valueElement.GetType().Name,
                                collectionElementType);

                            // Coerce the element to the appropriate type.
                            // Note, this may be recursive if the element is a
                            // collection itself.

                            currentValueElement =
                                CoerceTypeAsNeeded(
                                        argument,
                                        parameterName,
                                        collectionElementType,
                                        null,
                                        valueElement);
                        }
                        else if (collectionElementType != null && currentValueElement != null)
                        {
                            Type currentValueElementType = currentValueElement.GetType();
                            Type desiredElementType = collectionElementType;

                            if (currentValueElementType != desiredElementType &&
                                !currentValueElementType.IsSubclassOf(desiredElementType))
                            {
                                bindingTracer.WriteLine(
                                    "COERCION REQUIRED: Did not attempt to coerce collection element from type {0} to type {1}",
                                    (valueElement == null) ? "null" : valueElement.GetType().Name,
                                    collectionElementType);

                                coercionRequired = true;
                                break;
                            }
                        }

                        // Add() will fail with ArgumentException
                        // for Collection<T> with the wrong type.
                        try
                        {
                            if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.Array ||
                                isSystemDotArray)
                            {
                                bindingTracer.WriteLine(
                                    "Adding element of type {0} to array position {1}",
                                    (currentValueElement == null) ? "null" : currentValueElement.GetType().Name,
                                    arrayIndex);
                                resultAsIList[arrayIndex++] = currentValueElement;
                            }
                            else if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.IList)
                            {
                                bindingTracer.WriteLine(
                                    "Adding element of type {0} via IList.Add",
                                    (currentValueElement == null) ? "null" : currentValueElement.GetType().Name);
                                resultAsIList.Add(currentValueElement);
                            }
                            else
                            {
                                bindingTracer.WriteLine(
                                    "Adding element of type {0} via ICollection<T>::Add()",
                                    (currentValueElement == null) ? "null" : currentValueElement.GetType().Name);
                                addMethod.Invoke(resultCollection, new object[1] { currentValueElement });
                            }
                        }
                        catch (Exception error) // OK, we catch all here by design
                        {
                            // The inner exception to TargetInvocationException
                            // (if present) has a better Message
                            if (error is TargetInvocationException &&
                                error.InnerException != null)
                            {
                                error = error.InnerException;
                            }

                            ParameterBindingException bindingException =
                                new ParameterBindingException(
                                    error,
                                    ErrorCategory.InvalidArgument,
                                    this.InvocationInfo,
                                    GetErrorExtent(argument),
                                    parameterName,
                                    toType,
                                    currentValueElement?.GetType(),
                                    ParameterBinderStrings.CannotConvertArgument,
                                    "CannotConvertArgument",
                                    currentValueElement ?? "null",
                                    error.Message);
                            throw bindingException;
                        }
                    }
                }
                else // (currentValueAsIList == null)
                {
                    bindingTracer.WriteLine(
                        "Argument type {0} is not IList, treating this as scalar",
                        currentValue.GetType().Name);

                    if (collectionElementType != null)
                    {
                        if (coerceElementTypeIfNeeded)
                        {
                            bindingTracer.WriteLine(
                                "Coercing scalar arg value to type {0}",
                                collectionElementType);

                            // Coerce the scalar type into the collection

                            currentValue =
                                CoerceTypeAsNeeded(
                                    argument,
                                    parameterName,
                                    collectionElementType,
                                    null,
                                    currentValue);
                        }
                        else
                        {
                            Type currentValueElementType = currentValue.GetType();
                            Type desiredElementType = collectionElementType;

                            if (currentValueElementType != desiredElementType &&
                                !currentValueElementType.IsSubclassOf(desiredElementType))
                            {
                                bindingTracer.WriteLine(
                                    "COERCION REQUIRED: Did not coerce scalar arg value to type {1}",
                                    collectionElementType);

                                coercionRequired = true;
                                break;
                            }
                        }
                    }

                    // Add() will fail with ArgumentException
                    // for Collection<T> with the wrong type.
                    try
                    {
                        if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.Array ||
                            isSystemDotArray)
                        {
                            bindingTracer.WriteLine(
                                "Adding scalar element of type {0} to array position {1}",
                                (currentValue == null) ? "null" : currentValue.GetType().Name,
                                0);
                            resultAsIList[0] = currentValue;
                        }
                        else if (collectionTypeInformation.ParameterCollectionType == ParameterCollectionType.IList)
                        {
                            bindingTracer.WriteLine(
                                "Adding scalar element of type {0} via IList.Add",
                                (currentValue == null) ? "null" : currentValue.GetType().Name);
                            resultAsIList.Add(currentValue);
                        }
                        else
                        {
                            bindingTracer.WriteLine(
                                "Adding scalar element of type {0} via ICollection<T>::Add()",
                                (currentValue == null) ? "null" : currentValue.GetType().Name);
                            addMethod.Invoke(resultCollection, new object[1] { currentValue });
                        }
                    }
                    catch (Exception error) // OK, we catch all here by design
                    {
                        // The inner exception to TargetInvocationException
                        // (if present) has a better Message
                        if (error is TargetInvocationException &&
                            error.InnerException != null)
                        {
                            error = error.InnerException;
                        }

                        ParameterBindingException bindingException =
                            new ParameterBindingException(
                                error,
                                ErrorCategory.InvalidArgument,
                                this.InvocationInfo,
                                GetErrorExtent(argument),
                                parameterName,
                                toType,
                                currentValue?.GetType(),
                                ParameterBinderStrings.CannotConvertArgument,
                                "CannotConvertArgument",
                                currentValue ?? "null",
                                error.Message);
                        throw bindingException;
                    }
                }

                if (!coercionRequired)
                {
                    result = resultCollection;

                    // Set the converted result object untrusted if necessary
                    ExecutionContext.PropagateInputSource(originalValue, result, Context.LanguageMode);
                }
            } while (false);

            return result;
        }

        internal static IList GetIList(object value)
        {
            var baseObj = PSObject.Base(value);
            var result = baseObj as IList;
            if (result != null)
            {
                // Reference comparison to determine if 'value' is a PSObject
                s_tracer.WriteLine(baseObj == value
                                     ? "argument is IList"
                                     : "argument is PSObject with BaseObject as IList");
            }

            return result;
        }

        protected IScriptExtent GetErrorExtent(CommandParameterInternal cpi)
        {
            var result = cpi.ErrorExtent;
            if (result == PositionUtilities.EmptyExtent)
                result = InvocationInfo.ScriptPosition;
            // Can't use this assertion - we don't have useful positions when invoked via PowerShell API
            // Diagnostics.Assert(result != PositionUtilities.EmptyExtent, "We are missing a valid position somewhere");
            return result;
        }

        protected IScriptExtent GetParameterErrorExtent(CommandParameterInternal cpi)
        {
            var result = cpi.ParameterExtent;
            if (result == PositionUtilities.EmptyExtent)
                result = InvocationInfo.ScriptPosition;
            // Can't use this assertion - we don't have useful positions when invoked via PowerShell API
            // Diagnostics.Assert(result != PositionUtilities.EmptyExtent, "We are missing a valid position somewhere");
            return result;
        }

        #endregion private helpers
    }

    /// <summary>
    /// Represents an unbound parameter object in the engine. It's similar to
    /// AutomationNull.Value however AutomationNull.Value as a parameter value
    /// is used to say "use the default value for this object" whereas UnboundParameter
    /// says "this parameter is unbound, use the default only if the target type
    /// supports permits this."
    /// </summary>
    /// <remarks>It's a singleton class. Sealed to prevent subclassing</remarks>
    internal sealed class UnboundParameter
    {
        #region ctor

        // Private constructor
        private UnboundParameter() { }

        #endregion ctor

        #region private_members

        // Private member for Value.

        #endregion private_members

        #region public_property

        /// <summary>
        /// Represents an object of the same class (singleton class).
        /// </summary>
        internal static object Value { get; } = new object();

        #endregion public_property
    }

    // This class is a thin wrapper around Dictionary, but adds a member BoundPositionally.
    // $PSBoundParameters used to be a PSObject with an instance member, but that was quite
    // slow for a relatively common case, this class should work identically, except maybe
    // if somebody depends on the typename being the same.
    internal sealed class PSBoundParametersDictionary : Dictionary<string, object>
    {
        internal PSBoundParametersDictionary()
            : base(StringComparer.OrdinalIgnoreCase)
        {
            BoundPositionally = new List<string>();
            ImplicitUsingParameters = s_emptyUsingParameters;
        }

        private static readonly IDictionary s_emptyUsingParameters = new ReadOnlyDictionary<object, object>(new Dictionary<object, object>());

        public List<string> BoundPositionally { get; }

        internal IDictionary ImplicitUsingParameters { get; set; }
    }

    internal sealed class CommandLineParameters
    {
        private readonly PSBoundParametersDictionary _dictionary = new PSBoundParametersDictionary();

        internal bool ContainsKey(string name)
        {
            Dbg.Assert(!string.IsNullOrEmpty(name), "parameter names should not be empty");
            return _dictionary.ContainsKey(name);
        }

        internal void Add(string name, object value)
        {
            Dbg.Assert(!string.IsNullOrEmpty(name), "parameter names should not be empty");
            _dictionary[name] = value;
        }

        internal void MarkAsBoundPositionally(string name)
        {
            Dbg.Assert(!string.IsNullOrEmpty(name), "parameter names should not be empty");
            _dictionary.BoundPositionally.Add(name);
        }

        internal void SetPSBoundParametersVariable(ExecutionContext context)
        {
            Dbg.Assert(context != null, "caller should verify that context != null");

            context.SetVariable(SpecialVariables.PSBoundParametersVarPath, _dictionary);
        }

        internal void SetImplicitUsingParameters(object obj)
        {
            _dictionary.ImplicitUsingParameters = PSObject.Base(obj) as IDictionary;
            if (_dictionary.ImplicitUsingParameters == null)
            {
                // Handle downlevel V4 case where using parameters are passed as an array list.
                IList implicitArrayUsingParameters = PSObject.Base(obj) as IList;
                if ((implicitArrayUsingParameters != null) && (implicitArrayUsingParameters.Count > 0))
                {
                    // Convert array to hash table.
                    _dictionary.ImplicitUsingParameters = new Hashtable();
                    for (int index = 0; index < implicitArrayUsingParameters.Count; index++)
                    {
                        _dictionary.ImplicitUsingParameters.Add(index, implicitArrayUsingParameters[index]);
                    }
                }
            }
        }

        internal IDictionary GetImplicitUsingParameters()
        {
            return _dictionary.ImplicitUsingParameters;
        }

        internal object GetValueToBindToPSBoundParameters()
        {
            return _dictionary;
        }

        internal void UpdateInvocationInfo(InvocationInfo invocationInfo)
        {
            Dbg.Assert(invocationInfo != null, "caller should verify that invocationInfo != null");
            invocationInfo.BoundParameters = _dictionary;
        }

        internal HashSet<string> CopyBoundPositionalParameters()
        {
            HashSet<string> result = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            foreach (string item in _dictionary.BoundPositionally)
            {
                result.Add(item);
            }

            return result;
        }
    }
}
