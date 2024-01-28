// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Runtime.CompilerServices;

namespace System.Management.Automation
{
    /// <summary>
    /// Represents a variable in the PowerShell language.
    /// </summary>
    public class PSVariable : IHasSessionStateEntryVisibility
    {
        #region Ctor

        /// <summary>
        /// Constructs a variable with the given name.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        public PSVariable(string name)
            : this(name, null, ScopedItemOptions.None, (Collection<Attribute>)null)
        {
        }

        /// <summary>
        /// Constructs a variable with the given name, and value.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="value">
        /// The value of the variable.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        public PSVariable(string name, object value)
            : this(name, value, ScopedItemOptions.None, (Collection<Attribute>)null)
        {
        }

        /// <summary>
        /// Constructs a variable with the given name, value, and options.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="value">
        /// The value of the variable.
        /// </param>
        /// <param name="options">
        /// The constraints of the variable. Note, variables can only be made constant
        /// in the constructor.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        public PSVariable(string name, object value, ScopedItemOptions options)
            : this(name, value, options, (Collection<Attribute>)null)
        {
        }

        /// <summary>
        /// Constructs a variable with the given name, value, options, and description.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="value">
        /// The value of the variable.
        /// </param>
        /// <param name="options">
        /// The constraints of the variable. Note, variables can only be made constant
        /// in the constructor.
        /// </param>
        /// <param name="description">
        /// The description for the variable.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        internal PSVariable(string name, object value, ScopedItemOptions options, string description)
            : this(name, value, options, (Collection<Attribute>)null)
        {
            _description = description;
        }

        /// <summary>
        /// Constructs a variable with the given name, value, options, and description.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="value">
        /// The value of the variable.
        /// </param>
        /// <param name="options">
        /// The constraints of the variable. Note, variables can only be made constant
        /// in the constructor.
        /// </param>
        /// <param name="attributes">
        /// The attributes for the variable. ValidateArgumentsAttribute and derived types
        /// will be used to validate a value before setting it.
        /// </param>
        /// <param name="description">
        /// The description for the variable.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        internal PSVariable(
            string name,
            object value,
            ScopedItemOptions options,
            Collection<Attribute> attributes,
            string description)
                : this(name, value, options, attributes)
        {
            _description = description;
        }

        /// <summary>
        /// Constructs a variable with the given name, value, options, and attributes.
        /// </summary>
        /// <param name="name">
        /// The name of the variable.
        /// </param>
        /// <param name="value">
        /// The value of the variable.
        /// </param>
        /// <param name="options">
        /// The constraints of the variable. Note, variables can only be made constant
        /// in the constructor.
        /// </param>
        /// <param name="attributes">
        /// The attributes for the variable. ValidateArgumentsAttribute and derived types
        /// will be used to validate a value before setting it.
        /// </param>
        /// <exception cref="ArgumentException">
        /// If <paramref name="name"/> is null or empty.
        /// </exception>
        /// <exception cref="ValidationMetadataException">
        /// If the validation metadata identified in <paramref name="attributes"/>
        /// throws an exception.
        /// </exception>
        public PSVariable(
            string name,
            object value,
            ScopedItemOptions options,
            Collection<Attribute> attributes)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw PSTraceSource.NewArgumentException(nameof(name));
            }

            Name = name;

            _attributes = new PSVariableAttributeCollection(this);

            // Note, it is OK to set the value before setting the attributes
            // because each attribute will be validated as it is set.

            SetValueRawImpl(value, true);

            if (attributes != null)
            {
                foreach (Attribute attribute in attributes)
                {
                    _attributes.Add(attribute);
                }
            }

            // Set the options after setting the initial value.
            _options = options;

            if (IsAllScope)
            {
                Language.VariableAnalysis.NoteAllScopeVariable(name);
            }
        }

        // Should be protected, but that makes it public which we don't want.
        // The dummy parameter is to make the signature distinct from the public constructor taking a string.
        // This constructor exists to avoid calling SetValueRaw, which when overridden, might not work because
        // the derived class isn't fully constructed yet.
        internal PSVariable(string name, bool dummy)
        {
            Name = name;
        }

        #endregion ctor

        /// <summary>
        /// Gets the name of the variable.
        /// </summary>
        public string Name { get; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the variable.
        /// </summary>
        public virtual string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
            }
        }

        private string _description = string.Empty;

        internal void DebuggerCheckVariableRead()
        {
            var context = SessionState != null
                              ? SessionState.ExecutionContext
                              : LocalPipeline.GetExecutionContextFromTLS();
            if (context != null && context._debuggingMode > 0)
            {
                context.Debugger.CheckVariableRead(Name);
            }
        }

        internal void DebuggerCheckVariableWrite()
        {
            var context = SessionState != null
                              ? SessionState.ExecutionContext
                              : LocalPipeline.GetExecutionContextFromTLS();
            if (context != null && context._debuggingMode > 0)
            {
                context.Debugger.CheckVariableWrite(Name);
            }
        }

        /// <summary>
        /// Gets the value without triggering debugger check.
        /// </summary>
        internal virtual object GetValueRaw()
        {
            return _value;
        }

        /// <summary>
        /// Gets or sets the value of the variable.
        /// </summary>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant upon call to set.
        /// </exception>
        /// <exception cref="ValidationMetadataException">
        /// <paramref name="value"/> is not valid according to one or more
        /// of the attributes of this shell variable.
        /// </exception>
        public virtual object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return _value;
            }

            set
            {
                SetValue(value);
            }
        }

        private object _value;

        /// <summary>
        /// If true, then this variable is visible outside the runspace.
        /// </summary>
        public SessionStateEntryVisibility Visibility { get; set; } = SessionStateEntryVisibility.Public;

        /// <summary>
        /// The module where this variable was defined.
        /// </summary>
        public PSModuleInfo Module { get; private set; }

        internal void SetModule(PSModuleInfo module)
        {
            Module = module;
        }

        /// <summary>
        /// The name of the module that defined this variable.
        /// </summary>
        public string ModuleName
        {
            get
            {
                if (Module != null)
                    return Module.Name;
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the scope options on the variable.
        /// </summary>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// Upon set, if the variable is constant or if <paramref name="value"/>
        /// contains the constant flag.
        /// </exception>
        public virtual ScopedItemOptions Options
        {
            get
            {
                return _options;
            }

            set
            {
                SetOptions(value, false);
            }
        }

        internal void SetOptions(ScopedItemOptions newOptions, bool force)
        {
            // Check to see if the variable is constant or readonly, if so
            // throw an exception because the options cannot be changed.

            if (IsConstant || (!force && IsReadOnly))
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableNotWritable",
                            SessionStateStrings.VariableNotWritable);

                throw e;
            }

            // Now check to see if the caller is trying to set
            // the options to constant. This is only allowed at
            // variable creation

            if ((newOptions & ScopedItemOptions.Constant) != 0)
            {
                // user is trying to set the variable to constant after
                // creating the variable. Do not allow this (as per spec).

                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableCannotBeMadeConstant",
                            SessionStateStrings.VariableCannotBeMadeConstant);

                throw e;
            }

            // Now check to see if the caller is trying to
            // remove the AllScope option. This is not allowed
            // at any time.

            if (IsAllScope && ((newOptions & ScopedItemOptions.AllScope) == 0))
            {
                // user is trying to remove the AllScope option from the variable.
                // Do not allow this (as per spec).

                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableAllScopeOptionCannotBeRemoved",
                            SessionStateStrings.VariableAllScopeOptionCannotBeRemoved);

                throw e;
            }

            _options = newOptions;
        }

        private ScopedItemOptions _options = ScopedItemOptions.None;

        /// <summary>
        /// Gets the collection that contains the attributes for the variable.
        /// </summary>
        /// <remarks>
        /// To add or remove attributes, get the collection and then add or remove
        /// attributes to that collection.
        /// </remarks>
        public Collection<Attribute> Attributes
        {
            get { return _attributes ??= new PSVariableAttributeCollection(this); }
        }

        private PSVariableAttributeCollection _attributes;

        /// <summary>
        /// Checks if the given value meets the validation attribute constraints on the PSVariable.
        /// </summary>
        /// <param name="value">
        /// value which needs to be checked
        /// </param>
        /// <remarks>
        /// If <paramref name="value"/> is null or if no attributes are set, then
        /// the value is deemed valid.
        /// </remarks>
        /// <exception cref="ValidationMetadataException">
        /// If the validation metadata throws an exception.
        /// </exception>
        public virtual bool IsValidValue(object value)
        {
            return IsValidValue(_attributes, value);
        }

        internal static bool IsValidValue(IEnumerable<Attribute> attributes, object value)
        {
            if (attributes != null)
            {
                foreach (Attribute attribute in attributes)
                {
                    if (!IsValidValue(value, attribute))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the value is valid for the specified attribute.
        /// </summary>
        /// <param name="value">
        /// The variable value to validate.
        /// </param>
        /// <param name="attribute">
        /// The attribute to use to validate that value.
        /// </param>
        /// <returns>
        /// True if the value is valid with respect to the attribute, or false otherwise.
        /// </returns>
        internal static bool IsValidValue(object value, Attribute attribute)
        {
            bool result = true;

            ValidateArgumentsAttribute validationAttribute = attribute as ValidateArgumentsAttribute;
            if (validationAttribute != null)
            {
                try
                {
                    // Get an EngineIntrinsics instance using the context of the thread.

                    ExecutionContext context = Runspaces.LocalPipeline.GetExecutionContextFromTLS();
                    EngineIntrinsics engine = null;

                    if (context != null)
                    {
                        engine = context.EngineIntrinsics;
                    }

                    validationAttribute.InternalValidate(value, engine);
                }
                catch (ValidationMetadataException)
                {
                    result = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Runs all ArgumentTransformationAttributes that are specified in the Attributes
        /// collection on the given value in the order that they are in the collection.
        /// </summary>
        /// <param name="attributes">
        /// The attributes to use to transform the value.
        /// </param>
        /// <param name="value">
        /// The value to be transformed.
        /// </param>
        /// <returns>
        /// The transformed value.
        /// </returns>
        /// <exception cref="ArgumentTransformationMetadataException">
        /// If the argument transformation fails.
        /// </exception>
        internal static object TransformValue(IEnumerable<Attribute> attributes, object value)
        {
            Diagnostics.Assert(attributes != null, "caller to verify attributes is not null");

            object result = value;

            // Get an EngineIntrinsics instance using the context of the thread.

            ExecutionContext context = Runspaces.LocalPipeline.GetExecutionContextFromTLS();
            EngineIntrinsics engine = null;

            if (context != null)
            {
                engine = context.EngineIntrinsics;
            }

            foreach (Attribute attribute in attributes)
            {
                ArgumentTransformationAttribute transformationAttribute =
                    attribute as ArgumentTransformationAttribute;
                if (transformationAttribute != null)
                {
                    result = transformationAttribute.TransformInternal(engine, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Parameter binding does the checking and conversions as specified by the
        /// attributes, so repeating that process is slow and wrong.  This function
        /// applies the attributes without repeating the checks.
        /// </summary>
        /// <param name="attributes">The list of attributes to add.</param>
        internal void AddParameterAttributesNoChecks(Collection<Attribute> attributes)
        {
            foreach (Attribute attribute in attributes)
            {
                _attributes.AddAttributeNoCheck(attribute);
            }
        }

        #region internal members

        /// <summary>
        /// Returns true if the PSVariable is constant (only visible in the
        /// current scope), false otherwise.
        /// </summary>
        internal bool IsConstant
        {
            get
            {
                return (_options & ScopedItemOptions.Constant) != 0;
            }
        }

        /// <summary>
        /// Returns true if the PSVariable is readonly (only visible in the
        /// current scope), false otherwise.
        /// </summary>
        internal bool IsReadOnly
        {
            get
            {
                return (_options & ScopedItemOptions.ReadOnly) != 0;
            }
        }

        /// <summary>
        /// Returns true if the PSVariable is private (only visible in the
        /// current scope), false otherwise.
        /// </summary>
        internal bool IsPrivate
        {
            get
            {
                return (_options & ScopedItemOptions.Private) != 0;
            }
        }

        /// <summary>
        /// Returns true if the PSVariable is propagated to all scopes
        /// when the scope is created.
        /// </summary>
        internal bool IsAllScope
        {
            get
            {
                return (_options & ScopedItemOptions.AllScope) != 0;
            }
        }

        /// <summary>
        /// Indicates that the variable has been removed from session state
        /// and should no longer be considered valid. This is necessary because
        /// we surface variable references and can consequently not maintain
        /// transparent integrity.
        /// </summary>
        internal bool WasRemoved
        {
            get
            {
                return _wasRemoved;
            }

            set
            {
                _wasRemoved = value;
                // If set to true, clean up the variable...
                if (value)
                {
                    _options = ScopedItemOptions.None;
                    _value = null;
                    _wasRemoved = true;
                    _attributes = null;
                }
            }
        }

        private bool _wasRemoved;

        internal SessionStateInternal SessionState { get; set; }

        #endregion internal members

        /// <summary>
        /// Verifies the constraints and attributes before setting the value.
        /// </summary>
        /// <param name="value">
        /// The value to be set.
        /// </param>
        /// <exception cref="SessionStateUnauthorizedAccessException">
        /// If the variable is read-only or constant.
        /// </exception>
        /// <exception cref="ValidationMetadataException">
        /// If the validation metadata throws an exception or the value doesn't
        /// pass the validation metadata.
        /// </exception>
        private void SetValue(object value)
        {
            // Check to see if the variable is writable

            if ((_options & (ScopedItemOptions.ReadOnly | ScopedItemOptions.Constant)) != ScopedItemOptions.None)
            {
                SessionStateUnauthorizedAccessException e =
                    new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableNotWritable",
                            SessionStateStrings.VariableNotWritable);

                throw e;
            }

            // Now perform all ArgumentTransformations that are needed
            object transformedValue = value;
            if (_attributes != null && _attributes.Count > 0)
            {
                transformedValue = TransformValue(_attributes, value);

                // Next check to make sure the value is valid

                if (!IsValidValue(transformedValue))
                {
                    ValidationMetadataException e = new ValidationMetadataException(
                        "ValidateSetFailure",
                        null,
                        Metadata.InvalidValueFailure,
                        Name,
                        ((transformedValue != null) ? transformedValue.ToString() : "$null"));

                    throw e;
                }
            }

            if (transformedValue != null)
            {
                transformedValue = CopyMutableValues(transformedValue);
            }

            // Set the value before triggering any write breakpoints
            _value = transformedValue;

            DebuggerCheckVariableWrite();
        }

        private void SetValueRawImpl(object newValue, bool preserveValueTypeSemantics)
        {
            if (preserveValueTypeSemantics)
            {
                newValue = CopyMutableValues(newValue);
            }

            _value = newValue;
        }

        internal virtual void SetValueRaw(object newValue, bool preserveValueTypeSemantics)
        {
            SetValueRawImpl(newValue, preserveValueTypeSemantics);
        }

        private readonly CallSite<Func<CallSite, object, object>> _copyMutableValueSite =
            CallSite<Func<CallSite, object, object>>.Create(PSVariableAssignmentBinder.Get());

        internal object CopyMutableValues(object o)
        {
            // The variable assignment binder copies mutable values and returns other values as is.
            return _copyMutableValueSite.Target.Invoke(_copyMutableValueSite, o);
        }

        internal void WrapValue()
        {
            if (!this.IsConstant)
            {
                if (_value != null)
                {
                    _value = PSObject.AsPSObject(_value);
                }
            }
        }

#if FALSE
        // Replaced with a DLR based binder - but code is preserved in case that approach doesn't
        // work well performance wise.

        // See if it's a value type being assigned and
        // make a copy if it is...
        private static object PreserveValueType(object value)
        {
            if (value == null)
                return null;

            // Primitive types are immutable so just return them...
            Type valueType = value.GetType();
            if (valueType.IsPrimitive)
                return value;

            PSObject valueAsPSObject = value as PSObject;
            if (valueAsPSObject != null)
            {
                object baseObject = valueAsPSObject.BaseObject;
                if (baseObject != null)
                {
                    valueType = baseObject.GetType();
                    if (valueType.IsValueType && !valueType.IsPrimitive)
                    {
                        return valueAsPSObject.Copy();
                    }
                }
            }
            else if (valueType.IsValueType)
            {
                return PSObject.CopyValueType(value);
            }

            return value;
        }
#endif
    }

    internal sealed class LocalVariable : PSVariable
    {
        private readonly MutableTuple _tuple;
        private readonly int _tupleSlot;

        public LocalVariable(string name, MutableTuple tuple, int tupleSlot)
            : base(name, false)
        {
            _tuple = tuple;
            _tupleSlot = tupleSlot;
        }

        public override ScopedItemOptions Options
        {
            get
            {
                return base.Options;
            }

            set
            {
                // Throw, but only if someone is actually changing the options.
                if (value != base.Options)
                {
                    SessionStateUnauthorizedAccessException e =
                        new SessionStateUnauthorizedAccessException(
                            Name,
                            SessionStateCategory.Variable,
                            "VariableOptionsNotSettable",
                            SessionStateStrings.VariableOptionsNotSettable);

                    throw e;
                }
            }
        }

        public override object Value
        {
            get
            {
                DebuggerCheckVariableRead();
                return _tuple.GetValue(_tupleSlot);
            }

            set
            {
                _tuple.SetValue(_tupleSlot, value);
                DebuggerCheckVariableWrite();
            }
        }

        internal override object GetValueRaw()
        {
            return _tuple.GetValue(_tupleSlot);
        }

        internal override void SetValueRaw(object newValue, bool preserveValueTypeSemantics)
        {
            if (preserveValueTypeSemantics)
            {
                newValue = CopyMutableValues(newValue);
            }

            this.Value = newValue;
        }
    }

    /// <summary>
    /// This class is used for $null.  It always returns null as a value and accepts
    /// any value when it is set and throws it away.
    /// </summary>
    internal sealed class NullVariable : PSVariable
    {
        /// <summary>
        /// Constructor that calls the base class constructor with name "null" and
        /// value null.
        /// </summary>
        internal NullVariable() : base(StringLiterals.Null, null, ScopedItemOptions.Constant | ScopedItemOptions.AllScope)
        {
        }

        /// <summary>
        /// Always returns null from get, and always accepts
        /// but ignores the value on set.
        /// </summary>
        public override object Value
        {
            get
            {
                return null;
            }

            set
            {
                // All values are just ignored
            }
        }

        /// <summary>
        /// Gets the description for $null.
        /// </summary>
        public override string Description
        {
            get { return _description ??= SessionStateStrings.DollarNullDescription; }

            set { /* Do nothing */ }
        }

        private string _description;

        /// <summary>
        /// Gets the scope options for $null which is always None.
        /// </summary>
        public override ScopedItemOptions Options
        {
            get { return ScopedItemOptions.None; }

            set { /* Do nothing */ }
        }
    }

    /// <summary>
    /// The options that define some of the constraints for session state items like
    /// variables, aliases, and functions.
    /// </summary>
    [Flags]
    public enum ScopedItemOptions
    {
        /// <summary>
        /// There are no constraints on the item.
        /// </summary>
        None = 0,

        /// <summary>
        /// The item is readonly. It can be removed but cannot be changed.
        /// </summary>
        ReadOnly = 0x1,

        /// <summary>
        /// The item cannot be removed or changed.
        /// This flag can only be set a variable creation.
        /// </summary>
        Constant = 0x2,

        /// <summary>
        /// The item is private to the scope it was created in and
        /// cannot be seen from child scopes.
        /// </summary>
        Private = 0x4,

        /// <summary>
        /// The item is propagated to each new child scope created.
        /// </summary>
        AllScope = 0x8,

        /// <summary>
        /// The option is not specified by the user.
        /// </summary>
        Unspecified = 0x10
    }
}
