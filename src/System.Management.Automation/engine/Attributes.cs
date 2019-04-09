// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Management.Automation.Internal;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;

namespace System.Management.Automation.Internal
{
    /// <summary>
    /// Serves as the base class for Metadata attributes.
    /// </summary>
    /// <remarks>
    /// PSSnapins may not create custom attributes derived directly from
    /// <seealso cref="CmdletMetadataAttribute"/>,
    /// since it has no public constructor.  Only the public subclasses
    /// <seealso cref="ValidateArgumentsAttribute"/> and
    /// <seealso cref="ArgumentTransformationAttribute"/>
    /// are available.
    /// </remarks>
    /// <seealso cref="ValidateArgumentsAttribute"/>
    /// <seealso cref="ArgumentTransformationAttribute"/>
    [AttributeUsage(AttributeTargets.All)]
    public abstract class CmdletMetadataAttribute : Attribute
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        internal CmdletMetadataAttribute()
        {
        }
    }

    /// <summary>
    /// Serves as the base class for Metadata attributes that serve as guidance to the parser and parameter binder.
    /// </summary>
    /// <remarks>
    /// PSSnapins may not create custom attributes derived
    /// <seealso cref="ParsingBaseAttribute"/>,
    /// since it has no public constructor. Only the sealed public subclasses
    /// <seealso cref="ParameterAttribute"/> and
    /// <seealso cref="AliasAttribute"/>
    /// are available.
    /// </remarks>
    /// <seealso cref="ParameterAttribute"/>
    /// <seealso cref="AliasAttribute"/>
    [AttributeUsage(AttributeTargets.All)]
    public abstract class ParsingBaseAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Constructor with no parameters.
        /// </summary>
        internal ParsingBaseAttribute()
        {
        }
    }
}

namespace System.Management.Automation
{
    #region Base Metadata Classes

    /// <summary>
    /// Serves as the base class for Validate attributes that validate parameter arguments.
    /// </summary>
    /// <remarks>
    /// Argument validation attributes can be attached to
    /// <see cref="Cmdlet"/> and <see cref="Provider.CmdletProvider"/>
    /// parameters to ensure that the Cmdlet or CmdletProvider will not
    /// be invoked with invalid values of the parameter.  Existing
    /// validation attributes include <see cref="ValidateCountAttribute"/>,
    /// <see cref="ValidateNotNullAttribute"/>,
    /// <see cref="ValidateNotNullOrEmptyAttribute"/>,
    /// <see cref="ValidateArgumentsAttribute"/>,
    /// <see cref="ValidateLengthAttribute"/>,
    /// <see cref="ValidateRangeAttribute"/>,
    /// <see cref="ValidatePatternAttribute"/>, and
    /// <see cref="ValidateSetAttribute"/>.
    ///
    /// PSSnapins wishing to create custom argument validation attributes
    /// should derive from
    /// <see cref="ValidateArgumentsAttribute"/>
    /// and override the
    /// <see cref="ValidateArgumentsAttribute.Validate"/>
    /// abstract method, after which they can apply the
    /// attribute to their parameters.
    /// <see cref="ValidateArgumentsAttribute"/> validates the argument
    /// as a whole.  If the argument value is potentially an enumeration,
    /// you can derive from <see cref="ValidateEnumeratedArgumentsAttribute"/>
    /// which will take care of unrolling the enumeration
    /// and validate each element individually.
    ///
    /// It is also recommended to override
    /// <see cref="System.Object.ToString"/> to return a readable string
    /// similar to the attribute declaration, for example
    /// "[ValidateRangeAttribute(5,10)]".
    ///
    /// If this attribute is applied to a string parameter, the string command argument will be validated.
    /// If this attribute is applied to a string[] parameter, the string[] command argument will be validated.
    /// </remarks>
    /// <seealso cref="ValidateEnumeratedArgumentsAttribute"/>
    /// <seealso cref="ValidateCountAttribute"/>
    /// <seealso cref="ValidateNotNullAttribute"/>
    /// <seealso cref="ValidateNotNullOrEmptyAttribute"/>
    /// <seealso cref="ValidateArgumentsAttribute"/>
    /// <seealso cref="ValidateLengthAttribute"/>
    /// <seealso cref="ValidateRangeAttribute"/>
    /// <seealso cref="ValidatePatternAttribute"/>
    /// <seealso cref="ValidateSetAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ValidateArgumentsAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Overridden by subclasses to implement the validation of the parameter arguments.
        /// </summary>
        /// <param name="arguments">Argument value to validate.</param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the prerequisite is being
        /// evaluated.
        /// </param>
        /// <remarks>
        /// Validate that the value of <paramref name="arguments"/> is valid,
        /// and throw <see cref="ValidationMetadataException"/>
        /// if it is invalid.
        /// </remarks>
        /// <exception cref="ValidationMetadataException">Should be thrown for any validation failure.</exception>
        protected abstract void Validate(object arguments, EngineIntrinsics engineIntrinsics);

        /// <summary>
        /// Method that the command processor calls for data validate processing.
        /// </summary>
        /// <param name="o">Object to validate.</param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the prerequisite is being
        /// evaluated.
        /// </param>
        /// <returns>Bool true if the validate succeeded.</returns>
        /// <exception cref="ValidationMetadataException">
        /// Whenever any exception occurs during data validate.
        /// All the system exceptions are wrapped in ValidationMetadataException
        /// </exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        internal void InternalValidate(object o, EngineIntrinsics engineIntrinsics)
        {
            Validate(o, engineIntrinsics);
        }

        /// <summary>
        /// Initializes a new instance of a class derived from ValidateArgumentsAttribute.
        /// </summary>
        protected ValidateArgumentsAttribute()
        {
        }
    }

    /// <summary>
    /// A variant of <see cref="ValidateArgumentsAttribute"/> which
    /// unrolls enumeration values and validates each element
    /// individually.
    /// </summary>
    /// <remarks>
    /// <see cref="ValidateEnumeratedArgumentsAttribute"/> is like
    /// <see cref="ValidateArgumentsAttribute"/>, except that if
    /// the argument value is an enumeration,
    /// <see cref="ValidateEnumeratedArgumentsAttribute"/> will unroll
    /// the enumeration and validate each item individually.
    ///
    /// Existing enumerated validation attributes include
    /// <see cref="ValidateLengthAttribute"/>,
    /// <see cref="ValidateRangeAttribute"/>,
    /// <see cref="ValidatePatternAttribute"/>, and
    /// <see cref="ValidateSetAttribute"/>.
    ///
    /// PSSnapins wishing to create custom enumerated argument validation attributes
    /// should derive from
    /// <seealso cref="ValidateEnumeratedArgumentsAttribute"/>
    /// and override the
    /// <seealso cref="ValidateEnumeratedArgumentsAttribute.ValidateElement"/>
    /// abstract method, after which they can apply the
    /// attribute to their parameters.
    ///
    /// It is also recommended to override
    /// <see cref="System.Object.ToString"/> to return a readable string
    /// similar to the attribute declaration, for example
    /// "[ValidateRangeAttribute(5,10)]".
    ///
    /// If this attribute is applied to a string parameter, the string command argument will be validated.
    /// If this attribute is applied to a string[] parameter, each string command argument will be validated.
    /// </remarks>
    /// <seealso cref="ValidateArgumentsAttribute"/>
    /// <seealso cref="ValidateLengthAttribute"/>
    /// <seealso cref="ValidateRangeAttribute"/>
    /// <seealso cref="ValidatePatternAttribute"/>
    /// <seealso cref="ValidateSetAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ValidateEnumeratedArgumentsAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Initializes a new instance of a class derived from ValidateEnumeratedArgumentsAttribute.
        /// </summary>
        protected ValidateEnumeratedArgumentsAttribute() : base()
        {
        }
        /// <summary>
        /// Overridden by subclasses to implement the validation of each parameter argument.
        /// </summary>
        /// <remarks>
        /// Validate that the value of <paramref name="element"/>
        /// is valid, and throw
        /// <see cref="ValidationMetadataException"/>
        /// if it is invalid.
        /// </remarks>
        /// <param name="element">One of the parameter arguments.</param>
        /// <exception cref="ValidationMetadataException">Should be thrown for any validation failure.</exception>
        protected abstract void ValidateElement(object element);

        /// <summary>
        /// Calls ValidateElement in each element in the enumeration argument value.
        /// </summary>
        /// <param name="arguments">Object to validate.</param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the prerequisite is being
        /// evaluated.
        /// </param>
        /// <remarks>
        /// PSSnapins should override <see cref="ValidateElement"/> instead.
        /// </remarks>
        /// <exception cref="ValidationMetadataException">Should be thrown for any validation failure.</exception>
        protected sealed override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments == null || arguments == AutomationNull.Value)
            {
                throw new ValidationMetadataException(
                    "ArgumentIsEmpty",
                    null,
                    Metadata.ValidateNotNullOrEmptyCollectionFailure);
            }

            var enumerator = _getEnumeratorSite.Target.Invoke(_getEnumeratorSite, arguments);

            if (enumerator == null)
            {
                ValidateElement(arguments);
                return;
            }

            // arguments is IEnumerator
            while (enumerator.MoveNext())
            {
                ValidateElement(enumerator.Current);
            }

            enumerator.Reset();
        }

        private readonly CallSite<Func<CallSite, object, IEnumerator>> _getEnumeratorSite =
            CallSite<Func<CallSite, object, IEnumerator>>.Create(PSEnumerableBinder.Get());
    }

    #endregion Base Metadata Classes

    #region Misc Attributes

    /// <summary>
    /// To specify RunAs behavior for the class
    /// /// </summary>
    public enum DSCResourceRunAsCredential
    {
        /// <summary>Default is same as optional.</summary>
        Default,
        /// <summary>
        /// PsDscRunAsCredential can not be used for this DSC Resource.
        /// </summary>
        NotSupported,
        /// <summary>
        /// PsDscRunAsCredential is mandatory for resource.
        /// </summary>
        Mandatory,
        /// <summary>
        /// PsDscRunAsCredential can or can not be specified.
        /// </summary>
        Optional = Default,
    }

    /// <summary>
    /// Indicates the class defines a DSC resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DscResourceAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// To specify RunAs Behavior for the resource.
        /// </summary>
        public DSCResourceRunAsCredential RunAsCredential { get; set; }
    }

    /// <summary>
    /// When specified on a property or field of a DSC Resource, the property
    /// can or must be specified in a configuration, unless it is marked
    /// <see cref="DscPropertyAttribute.NotConfigurable"/>, in which case it is
    /// returned by the Get() method of the resource.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class DscPropertyAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Indicates the property is a required key property for a DSC resource.
        /// </summary>
        public bool Key { get; set; }

        /// <summary>
        /// Indicates the property is a required property for a DSC resource.
        /// </summary>
        public bool Mandatory { get; set; }

        /// <summary>
        /// Indicates the property is not a parameter to the DSC resource, but the
        /// property will contain a value after the Get() method of the resource is called.
        /// </summary>
        public bool NotConfigurable { get; set; }
    }

    /// <summary>
    /// Indication the configuration is for local configuration manager, also known as meta configuration.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DscLocalConfigurationManagerAttribute : CmdletMetadataAttribute
    {
    }

    /// <summary>
    /// Contains information about a cmdlet's metadata.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class CmdletCommonMetadataAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Gets and sets the cmdlet default parameter set.
        /// </summary>
        public string DefaultParameterSetName { get; set; }

        /// <summary>
        /// Gets and sets a Boolean value that indicates the Cmdlet supports ShouldProcess. By default
        /// the value is false, meaning the cmdlet doesn't support ShouldProcess.
        /// </summary>
        public bool SupportsShouldProcess { get; set; } = false;

        /// <summary>
        /// Gets and sets a Boolean value that indicates the Cmdlet supports Paging. By default
        /// the value is false, meaning the cmdlet doesn't support Paging.
        /// </summary>
        public bool SupportsPaging { get; set; } = false;

        /// <summary>
        /// Gets and sets a Boolean value that indicates the Cmdlet supports Transactions. By default
        /// the value is false, meaning the cmdlet doesn't support Transactions.
        /// </summary>
        public bool SupportsTransactions
        {
            get { return _supportsTransactions; }

            set
            {
#if !CORECLR
                _supportsTransactions = value;
#else
                // Disable 'SupportsTransactions' in CoreCLR
                // No transaction supported on CSS due to the lack of System.Transactions namespace
                _supportsTransactions = false;
#endif
            }
        }

        private bool _supportsTransactions = false;

        private ConfirmImpact _confirmImpact = ConfirmImpact.Medium;

        /// <summary>
        /// Gets and sets a ConfirmImpact value that indicates
        /// the "destructiveness" of the operation and when it
        /// should be confirmed.  This should only be used when
        /// SupportsShouldProcess is specified.
        /// </summary>
        public ConfirmImpact ConfirmImpact
        {
            get => SupportsShouldProcess ? _confirmImpact : ConfirmImpact.None;
            set => _confirmImpact = value;
        }

        /// <summary>
        /// Gets and sets a HelpUri value that indicates
        /// the location of online help. This is used by
        /// Get-Help to retrieve help content when -Online
        /// is specified.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string HelpUri { get; set; } = string.Empty;

        /// <summary>
        /// Gets and sets the RemotingBehavior value that declares how this cmdlet should interact
        /// with ambient remoting.
        /// </summary>
        public RemotingCapability RemotingCapability { get; set; } = RemotingCapability.PowerShell;
    }

    /// <summary>
    /// Identifies a class as a cmdlet and specifies the verb and noun identifying this cmdlet.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class CmdletAttribute : CmdletCommonMetadataAttribute
    {
        /// <summary>
        /// Gets the cmdlet noun.
        /// </summary>
        public string NounName { get; }

        /// <summary>
        /// Gets the cmdlet verb.
        /// </summary>
        public string VerbName { get; }

        /// <summary>
        /// Initializes a new instance of the CmdletAttribute class.
        /// </summary>
        /// <param name="verbName">Verb for the command.</param>
        /// <param name="nounName">Noun for the command.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public CmdletAttribute(string verbName, string nounName)
        {
            // NounName,VerbName have to be Non-Null strings
            if (string.IsNullOrEmpty(nounName))
            {
                throw PSTraceSource.NewArgumentException("nounName");
            }

            if (string.IsNullOrEmpty(verbName))
            {
                throw PSTraceSource.NewArgumentException("verbName");
            }

            NounName = nounName;
            VerbName = verbName;
        }
    }

    /// <summary>
    /// Identifies PowerShell script code as behaving like a cmdlet and hence uses
    /// cmdlet parameter binding instead of script parameter binding.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CmdletBindingAttribute : CmdletCommonMetadataAttribute
    {
        /// <summary>
        /// When true, the script will auto-generate appropriate parameter metadata to support positional
        /// parameters if the script hasn't already specified multiple parameter sets or specified positions
        /// explicitly via the <see cref="ParameterAttribute"/>.
        /// </summary>
        public bool PositionalBinding { get; set; } = true;
    }

    /// <summary>
    /// OutputTypeAttribute is used to specify the type of objects output by a cmdlet
    /// or script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
    public sealed class OutputTypeAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Construct the attribute from a <see>System.Type</see>
        /// </summary>
        internal OutputTypeAttribute(Type type)
        {
            Type = new[] { new PSTypeName(type) };
        }

        /// <summary>
        /// Construct the attribute from a type name.
        /// </summary>
        internal OutputTypeAttribute(string typeName)
        {
            Type = new[] { new PSTypeName(typeName) };
        }

        /// <summary>
        /// Construct the attribute from an array of <see>System.Type</see>
        /// </summary>
        /// <param name="type">The types output by the cmdlet.</param>
        public OutputTypeAttribute(params Type[] type)
        {
            if (type != null && type.Length > 0)
            {
                Type = new PSTypeName[type.Length];
                for (int i = 0; i < type.Length; i++)
                {
                    Type[i] = new PSTypeName(type[i]);
                }
            }
            else
            {
                Type = Array.Empty<PSTypeName>();
            }
        }

        /// <summary>
        /// Construct the attribute from an array of names of types.
        /// </summary>
        /// <param name="type">The types output by the cmdlet.</param>
        public OutputTypeAttribute(params string[] type)
        {
            if (type != null && type.Length > 0)
            {
                Type = new PSTypeName[type.Length];
                for (int i = 0; i < type.Length; i++)
                {
                    Type[i] = new PSTypeName(type[i]);
                }
            }
            else
            {
                Type = Array.Empty<PSTypeName>();
            }
        }

        /// <summary>
        /// The types specified by the attribute.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public PSTypeName[] Type { get; private set; }

        /// <summary>
        /// Attributes implemented by a provider can use:
        ///
        ///     [OutputType(ProviderCmdlet='cmdlet', typeof(...))]
        ///
        /// To specify the provider specific objects returned for a given cmdlet.
        /// </summary>
        public string ProviderCmdlet { get; set; }

        /// <summary>
        /// The list of parameter sets this OutputType specifies.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] ParameterSetName
        {
            get { return _parameterSetName ?? (_parameterSetName = new[] { ParameterAttribute.AllParameterSets }); }

            set { _parameterSetName = value; }
        }

        private string[] _parameterSetName;
    }

    /// <summary>
    /// This attribute is used on a dynamic assembly to mark it as one that is used to implement
    /// a set of classes defined in a PowerShell script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class DynamicClassImplementationAssemblyAttribute : Attribute
    {
        /// <summary>
        /// The (possibly null) path to the file defining this class.
        /// </summary>
        public string ScriptFile { get; set; }
    }

    #endregion Misc Attributes

    #region Parsing guidelines Attributes
    /// <summary>
    /// Declares an alternative name for a parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class AliasAttribute : ParsingBaseAttribute
    {
        internal string[] aliasNames;

        /// <summary>
        /// Gets the alias names passed to the constructor.
        /// </summary>
        public IList<string> AliasNames
        {
            get
            {
                return this.aliasNames;
            }
        }

        /// <summary>
        /// Initializes a new instance of the AliasAttribute class.
        /// </summary>
        /// <param name="aliasNames">The name for this alias.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public AliasAttribute(params string[] aliasNames)
        {
            if (aliasNames == null)
            {
                throw PSTraceSource.NewArgumentNullException("aliasNames");
            }

            this.aliasNames = aliasNames;
        }
    }

    /// <summary>
    /// Identifies parameters to Cmdlets.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class ParameterAttribute : ParsingBaseAttribute
    {
        /// <summary>
        /// ParameterSetName referring to all ParameterSets.
        /// </summary>
        public const string AllParameterSets = "__AllParameterSets";

        /// <summary>
        /// Initializes a new instance of the ParameterAttribute class.
        /// </summary>
        public ParameterAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance that is associated with an experimental feature.
        /// </summary>
        public ParameterAttribute(string experimentName, ExperimentAction experimentAction)
        {
            ExperimentalAttribute.ValidateArguments(experimentName, experimentAction);
            ExperimentName = experimentName;
            ExperimentAction = experimentAction;
        }

        private string _parameterSetName = ParameterAttribute.AllParameterSets;

        private string _helpMessage;
        private string _helpMessageBaseName;
        private string _helpMessageResourceId;

        #region Experimental Feature Related Properties

        /// <summary>
        /// Get name of the experimental feature this attribute is associated with.
        /// </summary>
        public string ExperimentName { get; }

        /// <summary>
        /// Get action for engine to take when the experimental feature is enabled.
        /// </summary>
        public ExperimentAction ExperimentAction { get; }

        internal bool ToHide => EffectiveAction == ExperimentAction.Hide;
        internal bool ToShow => EffectiveAction == ExperimentAction.Show;

        /// <summary>
        /// Get effective action to take at run time.
        /// </summary>
        private ExperimentAction EffectiveAction
        {
            get
            {
                if (_effectiveAction == ExperimentAction.None)
                {
                    _effectiveAction = ExperimentalFeature.GetActionToTake(ExperimentName, ExperimentAction);
                }

                return _effectiveAction;
            }
        }

        private ExperimentAction _effectiveAction = default(ExperimentAction);

        #endregion

        /// <summary>
        /// Gets and sets the parameter position. If not set, the parameter is named.
        /// </summary>
        public int Position { get; set; } = int.MinValue;

        /// <summary>
        /// Gets and sets the name of the parameter set this parameter belongs to. When
        /// it is not specified ParameterAttribute.AllParameterSets is assumed.
        /// </summary>
        public string ParameterSetName
        {
            get { return _parameterSetName; }

            set
            {
                _parameterSetName = value;
                if (string.IsNullOrEmpty(_parameterSetName))
                {
                    _parameterSetName = ParameterAttribute.AllParameterSets;
                }
            }
        }

        /// <summary>
        /// Gets and sets a flag specifying if this parameter is Mandatory. When
        /// it is not specified, false is assumed and the parameter is considered optional.
        /// </summary>
        public bool Mandatory { get; set; } = false;

        /// <summary>
        /// Gets and sets a flag that specifies that this parameter can take values
        /// from the incoming pipeline object. When it is not specified, false is assumed.
        /// </summary>
        public bool ValueFromPipeline { get; set; }

        /// <summary>
        /// Gets and sets a flag that specifies that this parameter can take values from a property
        /// in the incoming pipeline object with the same name as the parameter. When it
        /// is not specified, false is assumed.
        /// </summary>
        public bool ValueFromPipelineByPropertyName { get; set; }

        /// <summary>
        /// Gets and sets a flag that specifies that the remaining command line parameters
        /// should be associated with this parameter in the form of an array. When it
        /// is not specified, false is assumed.
        /// </summary>
        public bool ValueFromRemainingArguments { get; set; } = false;

        /// <summary>
        /// Gets and sets a short description for this parameter, suitable for presentation as a tool tip.
        /// </summary>
        /// <exception cref="ArgumentException">For a null or empty value when setting.</exception>
        public string HelpMessage
        {
            get
            {
                return _helpMessage;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("HelpMessage");
                }

                _helpMessage = value;
            }
        }

        /// <summary>
        /// Gets and sets the base name of the resource for a help message. When this field is specified,
        /// HelpMessageResourceId must also be specified.
        /// </summary>
        /// <exception cref="ArgumentException">For a null or empty value when setting.</exception>
        public string HelpMessageBaseName
        {
            get
            {
                return _helpMessageBaseName;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("HelpMessageBaseName");
                }

                _helpMessageBaseName = value;
            }
        }

        /// <summary>
        /// Gets and sets the Id of the resource for a help message. When this field is specified,
        /// HelpMessageBaseName must also be specified.
        /// </summary>
        /// <exception cref="ArgumentException">For a null or empty value when setting.</exception>
        public string HelpMessageResourceId
        {
            get
            {
                return _helpMessageResourceId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw PSTraceSource.NewArgumentException("HelpMessageResourceId");
                }

                _helpMessageResourceId = value;
            }
        }

        /// <summary>
        /// Indicates that this parameter should not be shown to the user in this like intellisense
        /// This is primarily to be used in functions that are implementing the logic for dynamic keywords.
        /// </summary>
        public bool DontShow
        {
            get;
            set;
        }
    }

    /// <summary>
    /// Specifies PSTypeName of a cmdlet or function parameter.
    /// </summary>
    /// <remarks>
    /// This attribute is used to restrict the type name of the parameter, when the type goes beyond the .NET type system.
    /// For example one could say: [PSTypeName("System.Management.ManagementObject#root\cimv2\Win32_Process")]
    /// to only allow Win32_Process objects to be bound to the parameter.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PSTypeNameAttribute : Attribute
    {
        /// <summary>
        /// </summary>
        public string PSTypeName { get; private set; }

        /// <summary>
        /// Creates a new PSTypeNameAttribute.
        /// </summary>
        /// <param name="psTypeName"></param>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly")]
        public PSTypeNameAttribute(string psTypeName)
        {
            if (string.IsNullOrEmpty(psTypeName))
            {
                throw PSTraceSource.NewArgumentException("psTypeName");
            }

            this.PSTypeName = psTypeName;
        }
    }

    /// <summary>
    /// Specifies that a parameter supports wildcards.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SupportsWildcardsAttribute : ParsingBaseAttribute
    {
    }

    /// <summary>
    /// Specify a default value and/or help comment for a command parameter.  This attribute
    /// does not have any semantic meaning, it is simply an aid to tools to make it simpler
    /// to know the true default value of a command parameter (which may or may not have
    /// any correlation with, e.g., the backing store of the Parameter's property or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class PSDefaultValueAttribute : ParsingBaseAttribute
    {
        /// <summary>
        /// Specify the default value of a command parameter.  The PowerShell engine does not
        /// use this value in any way, it exists for other tools that want to reflect on cmdlets.
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// Specify the help string for the default value of a command parameter.
        /// </summary>
        public string Help { get; set; }
    }

    /// <summary>
    /// Specify that the member is hidden for the purposes of cmdlets like Get-Member and
    /// that the member is not displayed by default by Format-* cmdlets.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Event)]
    public sealed class HiddenAttribute : ParsingBaseAttribute
    {
    }

    #endregion Parsing guidelines Attributes

    #region Data validate Attributes

    /// <summary>
    /// Validates that the length of each parameter argument's Length falls in the range
    /// specified by MinLength and MaxLength.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateLengthAttribute : ValidateEnumeratedArgumentsAttribute
    {
        /// <summary>
        /// Gets the attribute's minimum length.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Gets the attribute's maximum length.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Validates that the length of each parameter argument's Length falls in the range
        /// specified by MinLength and MaxLength.
        /// </summary>
        /// <param name="element">Object to validate.</param>
        /// <exception cref="ValidationMetadataException">If <paramref name="element"/> is not a string
        /// with length between minLength and maxLength</exception>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        protected override void ValidateElement(object element)
        {
            string objectString = element as string;
            if (objectString == null)
            {
                throw new ValidationMetadataException("ValidateLengthNotString",
                    null, Metadata.ValidateLengthNotString);
            }

            int len = objectString.Length;

            if (len < MinLength)
            {
                throw new ValidationMetadataException("ValidateLengthMinLengthFailure",
                    null, Metadata.ValidateLengthMinLengthFailure,
                    MinLength, len);
            }

            if (len > MaxLength)
            {
                throw new ValidationMetadataException("ValidateLengthMaxLengthFailure",
                    null, Metadata.ValidateLengthMaxLengthFailure,
                    MaxLength, len);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidateLengthAttribute class.
        /// </summary>
        /// <param name="minLength">Minimum required length.</param>
        /// <param name="maxLength">Maximum required length.</param>
        /// <exception cref="ArgumentOutOfRangeException">For invalid arguments.</exception>
        /// <exception cref="ValidationMetadataException">If maxLength is less than minLength.</exception>
        public ValidateLengthAttribute(int minLength, int maxLength) : base()
        {
            if (minLength < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("minLength", minLength);
            }

            if (maxLength <= 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("maxLength", maxLength);
            }

            if (maxLength < minLength)
            {
                throw new ValidationMetadataException("ValidateLengthMaxLengthSmallerThanMinLength",
                    null, Metadata.ValidateLengthMaxLengthSmallerThanMinLength);
            }

            MinLength = minLength;
            MaxLength = maxLength;
        }
    }

    /// <Summary>
    /// Predefined range kind to use with ValidateRangeAttribute.
    /// </Summary>
    public enum ValidateRangeKind
    {
        /// <Summary>
        /// Range is greater than 0.
        /// </Summary>
        Positive,

        /// <Summary>
        /// Range is greater than or equal to 0.
        /// </Summary>
        NonNegative,

        /// <Summary>
        /// Range is less than 0.
        /// </Summary>
        Negative,

        /// <Summary>
        /// Range is less than or equal to 0.
        /// </Summary>
        NonPositive
    }
    /// <summary>
    /// Validates that each parameter argument falls in the range
    /// specified by MinRange and MaxRange.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateRangeAttribute : ValidateEnumeratedArgumentsAttribute
    {
        /// <summary>
        /// Gets the attribute's minimum range.
        /// </summary>
        public object MinRange { get; }

        private IComparable _minComparable;

        /// <summary>
        /// Gets the attribute's maximum range.
        /// </summary>
        public object MaxRange { get; }

        private IComparable _maxComparable;

        /// <summary>
        /// The range values and the value to validate will all be converted to the promoted type.
        /// If minRange and maxRange are the same type,
        /// </summary>
        private Type _promotedType;

        ValidateRangeKind? _rangeKind;

        /// <summary>
        /// Validates that each parameter argument falls in the range
        /// specified by MinRange and MaxRange.
        /// </summary>
        /// <param name="element">Object to validate.</param>
        /// <exception cref="ValidationMetadataException">
        /// Thrown if the object to be validated does not implement IComparable,
        /// if the element type is not the same of MinRange, MaxRange,
        /// or if the element is not between MinRange and MaxRange.
        /// </exception>
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            var o = element as PSObject;
            if (o != null)
            {
                element = o.BaseObject;
            }

            if (_rangeKind.HasValue)
            {
                ValidateRange(element, (ValidateRangeKind)_rangeKind);
            }
            else
            {
                ValidateRange(element);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidateRangeAttribute class.
        /// </summary>
        /// <param name="minRange">Minimum value of the range allowed.</param>
        /// <param name="maxRange">Maximum value of the range allowed.</param>
        /// <exception cref="ArgumentNullException">For invalid arguments.</exception>
        /// <exception cref="ValidationMetadataException">
        /// if maxRange has a different type than minRange
        /// if maxRange is smaller than minRange
        /// if maxRange, minRange are not IComparable
        /// </exception>
        public ValidateRangeAttribute(object minRange, object maxRange) : base()
        {
            if (minRange == null)
            {
                throw PSTraceSource.NewArgumentNullException("minRange");
            }

            if (maxRange == null)
            {
                throw PSTraceSource.NewArgumentNullException("maxRange");
            }

            if (maxRange.GetType() != minRange.GetType())
            {
                bool failure = true;
                _promotedType = GetCommonType(minRange.GetType(), maxRange.GetType());
                if (_promotedType != null)
                {
                    object resultValue;
                    if (LanguagePrimitives.TryConvertTo(minRange, _promotedType, out resultValue))
                    {
                        minRange = resultValue;
                        if (LanguagePrimitives.TryConvertTo(maxRange, _promotedType, out resultValue))
                        {
                            maxRange = resultValue;
                            failure = false;
                        }
                    }
                }

                if (failure)
                {
                    throw new ValidationMetadataException("MinRangeNotTheSameTypeOfMaxRange", null,
                        Metadata.ValidateRangeMinRangeMaxRangeType,
                        minRange.GetType().Name, maxRange.GetType().Name);
                }
            }
            else
            {
                _promotedType = minRange.GetType();
            }

            _minComparable = minRange as IComparable;
            // minRange and maxRange have the same type, so we just need
            // to check one of them
            if (_minComparable == null)
            {
                throw new ValidationMetadataException("MinRangeNotIComparable", null,
                    Metadata.ValidateRangeNotIComparable);
            }

            _maxComparable = maxRange as IComparable;
            Diagnostics.Assert(_maxComparable != null, "maxComparable comes from a type that is IComparable");

            // Thanks to the IComparable if above this will not throw. They have the same type
            // and are IComparable
            if (_minComparable.CompareTo(maxRange) > 0)
            {
                throw new ValidationMetadataException("MaxRangeSmallerThanMinRange",
                    null, Metadata.ValidateRangeMaxRangeSmallerThanMinRange);
            }

            MinRange = minRange;
            MaxRange = maxRange;
        }

        /// <summary>
        /// Initializes a new instance of the ValidateRangeAttribute class
        /// this constructor uses a predefined ranged.
        /// </summary>
        public ValidateRangeAttribute(ValidateRangeKind kind) : base()
        {
            _rangeKind = kind;
        }

        private void ValidateRange(object element, ValidateRangeKind rangeKind)
        {
            Type commonType = GetCommonType(typeof(int), element.GetType());
            if (commonType == null)
            {
                throw new ValidationMetadataException(
                    "ValidationRangeElementType",
                    innerException: null,
                    Metadata.ValidateRangeElementType,
                    element.GetType().Name,
                    typeof(int).Name);
            }

            object resultValue;
            IComparable dynamicZero = 0;

            if (LanguagePrimitives.TryConvertTo(element, commonType, out resultValue))
            {
                element = resultValue;

                if (LanguagePrimitives.TryConvertTo(0, commonType, out resultValue))
                {
                    dynamicZero = (IComparable)resultValue;
                }
            }
            else
            {
                throw new ValidationMetadataException(
                    "ValidationRangeElementType",
                    null,
                    Metadata.ValidateRangeElementType,
                    element.GetType().Name,
                    commonType.Name);
            }

            switch (rangeKind)
            {
                case ValidateRangeKind.Positive:
                    if (dynamicZero.CompareTo(element) >= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangePositiveFailure",
                            null,
                            Metadata.ValidateRangePositiveFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonNegative:
                    if (dynamicZero.CompareTo(element) > 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonNegativeFailure",
                            null,
                            Metadata.ValidateRangeNonNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.Negative:
                    if (dynamicZero.CompareTo(element) <= 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNegativeFailure",
                            null,
                            Metadata.ValidateRangeNegativeFailure,
                            element.ToString());
                    }

                    break;
                case ValidateRangeKind.NonPositive:
                    if (dynamicZero.CompareTo(element) < 0)
                    {
                        throw new ValidationMetadataException(
                            "ValidateRangeNonPositiveFailure",
                            null,
                            Metadata.ValidateRangeNonPositiveFailure,
                            element.ToString());
                    }

                    break;
            }
        }

        private void ValidateRange(object element)
        {
            // MinRange and maxRange have the same type, so we just need
            // to compare to one of them.
            if (element.GetType() != _promotedType)
            {
                object resultValue;
                if (LanguagePrimitives.TryConvertTo(element, _promotedType, out resultValue))
                {
                    element = resultValue;
                }
                else
                {
                    throw new ValidationMetadataException(
                        "ValidationRangeElementType",
                        null,
                        Metadata.ValidateRangeElementType,
                        element.GetType().Name,
                        MinRange.GetType().Name);
                }
            }

            // They are the same type and are all IComparable, so this should not throw
            if (_minComparable.CompareTo(element) > 0)
            {
                throw new ValidationMetadataException(
                    "ValidateRangeTooSmall",
                    null,
                    Metadata.ValidateRangeSmallerThanMinRangeFailure,
                    element.ToString(),
                    MinRange.ToString());
            }

            if (_maxComparable.CompareTo(element) < 0)
            {
                throw new ValidationMetadataException(
                    "ValidateRangeTooBig",
                    null,
                    Metadata.ValidateRangeGreaterThanMaxRangeFailure,
                    element.ToString(),
                    MaxRange.ToString());
            }
        }

        private static Type GetCommonType(Type minType, Type maxType)
        {
            Type resultType = null;

            TypeCode minTypeCode = LanguagePrimitives.GetTypeCode(minType);
            TypeCode maxTypeCode = LanguagePrimitives.GetTypeCode(maxType);
            TypeCode opTypeCode = (int)minTypeCode >= (int)maxTypeCode ? minTypeCode : maxTypeCode;
            if ((int)opTypeCode <= (int)TypeCode.Int32)
            {
                resultType = typeof(int);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt32)
            {
                // If one of the operands is signed, we need to promote to double if the value is negative.  We aren't
                // checking the value, so we unconditionally promote to double.
                resultType = LanguagePrimitives.IsSignedInteger(minTypeCode) || LanguagePrimitives.IsSignedInteger(maxTypeCode)
                    ? typeof(double) : typeof(uint);
            }
            else if ((int)opTypeCode <= (int)TypeCode.Int64)
            {
                resultType = typeof(long);
            }
            else if ((int)opTypeCode <= (int)TypeCode.UInt64)
            {
                // If one of the operands is signed, we need to promote to double if the value is negative.  We aren't
                // checking the value, so we unconditionally promote to double.
                resultType = LanguagePrimitives.IsSignedInteger(minTypeCode) || LanguagePrimitives.IsSignedInteger(maxTypeCode)
                    ? typeof(double) : typeof(ulong);
            }
            else if (opTypeCode == TypeCode.Decimal)
            {
                resultType = typeof(decimal);
            }
            else if (opTypeCode == TypeCode.Single || opTypeCode == TypeCode.Double)
            {
                resultType = typeof(double);
            }

            return resultType;
        }
    }

    /// <summary>
    /// Validates that each parameter argument matches the RegexPattern.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidatePatternAttribute : ValidateEnumeratedArgumentsAttribute
    {
        /// <summary>
        /// Gets the Regex pattern to be used in the validation.
        /// </summary>
        public string RegexPattern { get; }

        /// <summary>
        /// Gets or sets the Regex options to be used in the validation.
        /// </summary>
        public RegexOptions Options { set; get; } = RegexOptions.IgnoreCase;

        /// <summary>
        /// Gets or sets the custom error message pattern that is displayed to the user.
        ///
        /// The text representation of the object being validated and the validating regex is passed as
        /// the first and second formatting parameters to the ErrorMessage formatting pattern.
        /// <example>
        /// <code>
        /// [ValidatePattern("\s+", ErrorMessage="The text '{0}' did not pass validation of regex '{1}'")]
        /// </code>
        /// </example>
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Validates that each parameter argument matches the RegexPattern.
        /// </summary>
        /// <param name="element">Object to validate.</param>
        /// <exception cref="ValidationMetadataException">If <paramref name="element"/> is not a string
        ///  that matches the pattern
        ///  and for invalid arguments</exception>
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            string objectString = element.ToString();
            Regex regex = null;
            regex = new Regex(RegexPattern, Options);
            Match match = regex.Match(objectString);
            if (!match.Success)
            {
                var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage) ? Metadata.ValidatePatternFailure : ErrorMessage;
                throw new ValidationMetadataException("ValidatePatternFailure",
                        null, errorMessageFormat,
                        objectString, RegexPattern);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidatePatternAttribute class.
        /// </summary>
        /// <param name="regexPattern">Pattern string to match.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public ValidatePatternAttribute(string regexPattern)
        {
            if (string.IsNullOrEmpty(regexPattern))
            {
                throw PSTraceSource.NewArgumentException("regexPattern");
            }

            RegexPattern = regexPattern;
        }
    }

    /// <summary>
    /// Class for validating against a script block.
    /// </summary>
    public sealed class ValidateScriptAttribute : ValidateEnumeratedArgumentsAttribute
    {
        /// <summary>
        /// Gets or sets the custom error message that is displayed to the user.
        ///
        /// The item being validated and the validating scriptblock is passed as the first and second
        /// formatting argument.
        /// <example>
        /// <code>
        /// [ValidateScript("$_ % 2", ErrorMessage = "The item '{0}' did not pass validation of script '{1}'")]
        /// </code>
        /// </example>
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets the scriptblock to be used in the validation.
        /// </summary>
        public ScriptBlock ScriptBlock { get; }

        /// <summary>
        /// Validates that each parameter argument matches the scriptblock.
        /// </summary>
        /// <param name="element">Object to validate.</param>
        /// <exception cref="ValidationMetadataException">If <paramref name="element"/> is invalid.</exception>
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullFailure);
            }

            object result = ScriptBlock.DoInvokeReturnAsIs(
                useLocalScope: true,
                errorHandlingBehavior: ScriptBlock.ErrorHandlingBehavior.WriteToExternalErrorPipe,
                dollarUnder: LanguagePrimitives.AsPSObjectOrNull(element),
                input: AutomationNull.Value,
                scriptThis: AutomationNull.Value,
                args: Array.Empty<object>());

            if (!LanguagePrimitives.IsTrue(result))
            {
                var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage) ? Metadata.ValidateScriptFailure : ErrorMessage;
                throw new ValidationMetadataException("ValidateScriptFailure",
                        null, errorMessageFormat,
                        element, ScriptBlock);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidateScriptBlockAttribute class.
        /// </summary>
        /// <param name="scriptBlock">Scriptblock to match.</param>
        /// <exception cref="ArgumentException">For invalid arguments.</exception>
        public ValidateScriptAttribute(ScriptBlock scriptBlock)
        {
            if (scriptBlock == null)
            {
                throw PSTraceSource.NewArgumentException("scriptBlock");
            }

            ScriptBlock = scriptBlock;
        }
    }

    /// <summary>
    /// Validates that the parameter argument count is in the specified range.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateCountAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Gets the minimum length of this attribute.
        /// </summary>
        public int MinLength { get; }

        /// <summary>
        /// Gets the maximum length of this attribute.
        /// </summary>
        public int MaxLength { get; }

        /// <summary>
        /// Validates that the parameter argument count is in the specified range.
        /// </summary>
        /// <param name="arguments">Object to validate.</param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the validation is being
        /// evaluated.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// if the element is none of ICollection, IEnumerable, IList, IEnumerator
        /// if the element's length is not between MinLength and MAxLEngth
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            UInt32 len = 0;
            IList il;
            ICollection ic;
            IEnumerable ie;
            IEnumerator ienumerator;

            if (arguments == null || arguments == AutomationNull.Value)
            {
                // treat a nul list the same as an empty list
                // with a count of zero.
                len = 0;
            }
            else if ((il = arguments as IList) != null)
            {
                len = (UInt32)il.Count;
            }
            else if ((ic = arguments as ICollection) != null)
            {
                len = (UInt32)ic.Count;
            }
            else if ((ie = arguments as IEnumerable) != null)
            {
                IEnumerator e = ie.GetEnumerator();
                while (e.MoveNext())
                {
                    len++;
                }
            }
            else if ((ienumerator = arguments as IEnumerator) != null)
            {
                while (ienumerator.MoveNext())
                {
                    len++;
                }
            }
            else
            {
                // No conversion succeeded so throw and exception...
                throw new ValidationMetadataException("NotAnArrayParameter",
                    null, Metadata.ValidateCountNotInArray);
            }

            if (MinLength == MaxLength && len != MaxLength)
            {
                throw new ValidationMetadataException("ValidateCountExactFailure",
                    null, Metadata.ValidateCountExactFailure,
                    MaxLength, len);
            }

            if (len < MinLength || len > MaxLength)
            {
                throw new ValidationMetadataException("ValidateCountMinMaxFailure",
                    null, Metadata.ValidateCountMinMaxFailure,
                    MinLength, MaxLength, len);
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidateCountAttribute class.
        /// </summary>
        /// <param name="minLength">Minimum number of values required.</param>
        /// <param name="maxLength">Maximum number of values required.</param>
        /// <exception cref="ArgumentOutOfRangeException">For invalid arguments.</exception>
        /// <exception cref="ValidationMetadataException">
        /// if minLength is greater than maxLength
        /// </exception>
        public ValidateCountAttribute(int minLength, int maxLength)
        {
            if (minLength < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("minLength", minLength);
            }

            if (maxLength <= 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("maxLength", maxLength);
            }

            if (maxLength < minLength)
            {
                throw new ValidationMetadataException("ValidateRangeMaxLengthSmallerThanMinLength",
                    null, Metadata.ValidateCountMaxLengthSmallerThanMinLength);
            }

            MinLength = minLength;
            MaxLength = maxLength;
        }
    }

    /// <summary>
    /// Optional base class for <see cref="IValidateSetValuesGenerator"/> implementations that want a default implementation to cache valid values.
    /// </summary>
    public abstract class CachedValidValuesGeneratorBase : IValidateSetValuesGenerator
    {
        // Cached valid values.
        private string[] _validValues;
        private int _validValuesCacheExpiration;

        /// <summary>
        /// Initializes a new instance of the CachedValidValuesGeneratorBase class.
        /// </summary>
        /// <param name="cacheExpirationInSeconds">Sets a time interval in seconds to reset the '_validValues' dynamic valid values cache.</param>
        protected CachedValidValuesGeneratorBase(int cacheExpirationInSeconds)
        {
            _validValuesCacheExpiration = cacheExpirationInSeconds;
        }

        /// <summary>
        /// Abstract method to generate a valid values.
        /// </summary>
        public abstract string[] GenerateValidValues();

        /// <summary>
        /// Get a valid values.
        /// </summary>
        public string[] GetValidValues()
        {
            // Because we have a background task to clear the cache by '_validValues = null'
            // we use the local variable to exclude a race condition.
            var validValuesLocal = _validValues;
            if (validValuesLocal != null)
            {
                return validValuesLocal;
            }

            var validValuesNoCache = GenerateValidValues();

            if (validValuesNoCache == null)
            {
                throw new ValidationMetadataException(
                    "ValidateSetGeneratedValidValuesListIsNull",
                    null,
                    Metadata.ValidateSetGeneratedValidValuesListIsNull);
            }

            if (_validValuesCacheExpiration > 0)
            {
                _validValues = validValuesNoCache;
                Task.Delay(_validValuesCacheExpiration * 1000).ContinueWith((task) => _validValues = null);
            }

            return validValuesNoCache;
        }
    }

    /// <summary>
    /// Validates that each parameter argument is present in a specified set.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateSetAttribute : ValidateEnumeratedArgumentsAttribute
    {
        // We can use either static '_validValues'
        // or dynamic valid values list generated by instance of 'validValuesGenerator'.
        private string[] _validValues;
        private IValidateSetValuesGenerator validValuesGenerator = null;

        // The valid values generator cache works across 'ValidateSetAttribute' instances.
        private static ConcurrentDictionary<Type, IValidateSetValuesGenerator> s_ValidValuesGeneratorCache = new ConcurrentDictionary<Type, IValidateSetValuesGenerator>();

        /// <summary>
        /// Gets or sets the custom error message that is displayed to the user
        ///
        /// The item being validated and a text representation of the validation set
        /// is passed as the first and second formatting argument to the ErrorMessage formatting pattern.
        /// <example>
        /// <code>
        /// [ValidateSet("A","B","C", ErrorMessage="The item '{0}' is not part of the set '{1}'.")
        /// </code>
        /// </example>
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets a flag specifying if we should ignore the case when performing string comparison. The
        /// default is true.
        /// </summary>
        public bool IgnoreCase { get; set; } = true;

        /// <summary>
        /// Gets the valid values in the set.
        /// </summary>
        public IList<string> ValidValues
        {
            get
            {
                if (validValuesGenerator == null)
                {
                    return _validValues;
                }

                var validValuesLocal = validValuesGenerator.GetValidValues();

                if (validValuesLocal == null)
                {
                    throw new ValidationMetadataException(
                        "ValidateSetGeneratedValidValuesListIsNull",
                        null,
                        Metadata.ValidateSetGeneratedValidValuesListIsNull);
                }

                return validValuesLocal;
            }
        }

        /// <summary>
        /// Validates that each parameter argument is present in the specified set.
        /// </summary>
        /// <param name="element">Object to validate.</param>
        /// <exception cref="ValidationMetadataException">
        /// if element is not in the set
        /// for invalid argument
        /// </exception>
        protected override void ValidateElement(object element)
        {
            if (element == null)
            {
                throw new ValidationMetadataException(
                            "ArgumentIsEmpty",
                            null,
                            Metadata.ValidateNotNullFailure);
            }

            string objString = element.ToString();
            foreach (string setString in ValidValues)
            {
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(
                        setString,
                        objString,
                        IgnoreCase ? CompareOptions.IgnoreCase : CompareOptions.None) == 0)
                {
                    return;
                }
            }

            var errorMessageFormat = string.IsNullOrEmpty(ErrorMessage) ? Metadata.ValidateSetFailure : ErrorMessage;
            throw new ValidationMetadataException("ValidateSetFailure", null,
                errorMessageFormat,
                element.ToString(), SetAsString());
        }

        private string SetAsString()
        {
            return string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, ValidValues);
        }

        /// <summary>
        /// Initializes a new instance of the ValidateSetAttribute class.
        /// </summary>
        /// <param name="validValues">List of valid values.</param>
        /// <exception cref="ArgumentNullException">For null arguments.</exception>
        /// <exception cref="ArgumentOutOfRangeException">For invalid arguments.</exception>
        public ValidateSetAttribute(params string[] validValues)
        {
            if (validValues == null)
            {
                throw PSTraceSource.NewArgumentNullException("validValues");
            }

            if (validValues.Length == 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException("validValues", validValues);
            }

            _validValues = validValues;
        }

        /// <summary>
        /// Initializes a new instance of the ValidateSetAttribute class.
        /// Valid values is returned dynamically from a custom class implementing 'IValidateSetValuesGenerator' interface.
        /// </summary>
        /// <param name="valuesGeneratorType">Class that implements the 'IValidateSetValuesGenerator' interface.</param>
        /// <exception cref="ArgumentException">For null arguments.</exception>
        public ValidateSetAttribute(Type valuesGeneratorType)
        {
            // We check 'IsNotPublic' because we don't want allow 'Activator.CreateInstance' create an instance of non-public type.
            if (!typeof(IValidateSetValuesGenerator).IsAssignableFrom(valuesGeneratorType) || valuesGeneratorType.IsNotPublic)
            {
                throw PSTraceSource.NewArgumentException("valuesGeneratorType");
            }

            // Add a valid values generator to the cache.
            // We don't cache valid values.
            // We expect that valid values can be cached in the valid values generator.
            validValuesGenerator = s_ValidValuesGeneratorCache.GetOrAdd(valuesGeneratorType, (key) => (IValidateSetValuesGenerator)Activator.CreateInstance(key));
        }
    }

    /// <summary>
    /// Allows dynamically generate set of values for ValidateSetAttribute.
    /// </summary>
    public interface IValidateSetValuesGenerator
    {
        /// <summary>
        /// Get a valid values.
        /// </summary>
        string[] GetValidValues();
    }

    /// <summary>
    /// Validates that each parameter argument is Trusted data.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateTrustedDataAttribute : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Validates that the parameter argument is not untrusted.
        /// </summary>
        /// <param name="arguments">Object to validate.</param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the validation is being
        /// evaluated.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// if the argument is untrusted.
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (ExecutionContext.HasEverUsedConstrainedLanguage &&
                engineIntrinsics.SessionState.Internal.ExecutionContext.LanguageMode == PSLanguageMode.FullLanguage)
            {
                if (ExecutionContext.IsMarkedAsUntrusted(arguments))
                {
                    throw new ValidationMetadataException("ValidateTrustedDataFailure", null, Metadata.ValidateTrustedDataFailure, arguments);
                }
            }
        }
    }

    #region Allow

    /// <summary>
    /// Allows a NULL as the argument to a mandatory parameter.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowNullAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the AllowNullAttribute class.
        /// </summary>
        public AllowNullAttribute() { }
    }

    /// <summary>
    /// Allows an empty string as the argument to a mandatory string parameter.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEmptyStringAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the AllowEmptyStringAttribute class.
        /// </summary>
        public AllowEmptyStringAttribute() { }
    }

    /// <summary>
    /// Allows an empty collection as the argument to a mandatory collection parameter.
    /// </summary>
    [AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class AllowEmptyCollectionAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the AllowEmptyCollectionAttribute class.
        /// </summary>
        public AllowEmptyCollectionAttribute() { }
    }

    #endregion Allow

    #region Path validation attributes

    /// <summary>
    /// Validates that the path has an approved root drive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ValidateDriveAttribute : ValidateArgumentsAttribute
    {
        private string[] _validRootDrives;

        /// <summary>
        /// Gets the values in the set.
        /// </summary>
        public IList<string> ValidRootDrives
        {
            get
            {
                return _validRootDrives;
            }
        }

        /// <summary>
        /// Initializes a new instance of the ValidateDrivePath class.
        /// </summary>
        /// <param name="validRootDrives">List of approved root drives for path.</param>
        public ValidateDriveAttribute(params string[] validRootDrives)
        {
            if (validRootDrives == null)
            {
                throw PSTraceSource.NewArgumentException("validRootDrives");
            }

            _validRootDrives = validRootDrives;
        }

        /// <summary>
        /// Validates path argument.
        /// </summary>
        /// <param name="arguments">Object to validate.</param>
        /// <param name="engineIntrinsics">Engine intrinsics.</param>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments == null)
            {
                throw new ValidationMetadataException(
                            "PathArgumentIsEmpty",
                            null,
                            Metadata.ValidateNotNullFailure);
            }

            var path = arguments as string;
            if (path == null)
            {
                throw new ValidationMetadataException(
                    "PathArgumentIsNotValid",
                    null,
                    Metadata.ValidateDrivePathArgNotString);
            }

            ProviderInfo providerInfo;
            PSDriveInfo driveInfo;
            var resolvedPath = engineIntrinsics.SessionState.Internal.Globber.GetProviderPath(
                path: path,
                context: new CmdletProviderContext(engineIntrinsics.SessionState.Internal.ExecutionContext),
                isTrusted: true,
                provider: out providerInfo,
                drive: out driveInfo);

            string rootDrive = driveInfo.Name;
            if (string.IsNullOrEmpty(rootDrive))
            {
                throw new ValidationMetadataException(
                    "PathArgumentNoRoot",
                    null,
                    Metadata.ValidateDrivePathNoRoot);
            }

            bool rootFound = false;
            foreach (var validDrive in _validRootDrives)
            {
                if (rootDrive.Equals(validDrive, StringComparison.OrdinalIgnoreCase))
                {
                    rootFound = true;
                    break;
                }
            }

            if (!rootFound)
            {
                throw new ValidationMetadataException(
                    "PathRootInvalid",
                    null,
                    Metadata.ValidateDrivePathFailure, rootDrive, ValidDriveListAsString());
            }
        }

        private string ValidDriveListAsString()
        {
            return string.Join(CultureInfo.CurrentUICulture.TextInfo.ListSeparator, _validRootDrives);
        }
    }

    /// <summary>
    /// Validates that the path parameter is a User drive.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateUserDriveAttribute : ValidateDriveAttribute
    {
        /// <summary>
        /// Initializes a new instance of the ValidateUserDrivePathAttribute class.
        /// </summary>
        public ValidateUserDriveAttribute()
            : base(new string[] { "User" })
        { }
    }

    #endregion

    #region NULL validation attributes

    /// <summary>
    /// Base type of Null Validation attributes.
    /// </summary>
    public abstract class NullValidationAttributeBase : ValidateArgumentsAttribute
    {
        /// <summary>
        /// Check if the argument type is a collection.
        /// </summary>
        protected bool IsArgumentCollection(Type argumentType, out bool isElementValueType)
        {
            isElementValueType = false;
            var information = new ParameterCollectionTypeInformation(argumentType);
            switch (information.ParameterCollectionType)
            {
                // If 'arguments' is an array, or implement 'IList', or implement 'ICollection<>'
                // then we continue to check each element of the collection.
                case ParameterCollectionType.Array:
                case ParameterCollectionType.IList:
                case ParameterCollectionType.ICollectionGeneric:
                    Type elementType = information.ElementType;
                    isElementValueType = elementType != null && elementType.IsValueType;
                    return true;
                default:
                    return false;
            }
        }
    }

    /// <summary>
    /// Validates that the parameters's argument is not null.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateNotNullAttribute : NullValidationAttributeBase
    {
        /// <summary>
        /// Verifies the argument is not null and if it is a collection, that each
        /// element in the collection is not null.
        /// If the argument is a collection then each element is validated.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to verify.
        /// </param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the validation is being
        /// evaluated.
        /// </param>
        /// <returns>
        /// true if the argument is valid.
        /// </returns>
        /// <exception cref="ValidationMetadataException">
        /// if element is null or a collection with a null element
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments == null || arguments == AutomationNull.Value)
            {
                throw new ValidationMetadataException(
                    "ArgumentIsNull",
                    null,
                    Metadata.ValidateNotNullFailure);
            }
            else if (IsArgumentCollection(arguments.GetType(), out bool isElementValueType))
            {
                // If the element of the collection is of value type, then no need to check for null
                // because a value-type value cannot be null.
                if (isElementValueType) { return; }

                IEnumerator ienum = LanguagePrimitives.GetEnumerator(arguments);
                while (ienum.MoveNext())
                {
                    object element = ienum.Current;
                    if (element == null || element == AutomationNull.Value)
                    {
                        throw new ValidationMetadataException(
                            "ArgumentIsNull",
                            null,
                            Metadata.ValidateNotNullCollectionFailure);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Validates that the parameters's argument is not null, is not
    /// an empty string, and is not an empty collection.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class ValidateNotNullOrEmptyAttribute : NullValidationAttributeBase
    {
        /// <summary>
        /// Validates that the parameters's argument is not null, is not
        /// an empty string, and is not an empty collection. If arguments
        /// is a collection, each argument is verified.
        /// </summary>
        /// <param name="arguments">
        /// The arguments to verify.
        /// </param>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the validation is being
        /// evaluated.
        /// </param>
        /// <exception cref="ValidationMetadataException">
        /// if the arguments are not valid
        /// </exception>
        protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
        {
            if (arguments == null || arguments == AutomationNull.Value)
            {
                throw new ValidationMetadataException(
                    "ArgumentIsNull",
                    null,
                    Metadata.ValidateNotNullOrEmptyFailure);
            }
            else if (arguments is string str)
            {
                if (string.IsNullOrEmpty(str))
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyFailure);
                }
            }
            else if (IsArgumentCollection(arguments.GetType(), out bool isElementValueType))
            {
                bool isEmpty = true;
                IEnumerator ienum = LanguagePrimitives.GetEnumerator(arguments);
                if (ienum.MoveNext()) { isEmpty = false; }

                // If the element of the collection is of value type, then no need to check for null
                // because a value-type value cannot be null.
                if (!isEmpty && !isElementValueType)
                {
                    do
                    {
                        object element = ienum.Current;
                        if (element == null || element == AutomationNull.Value)
                        {
                            throw new ValidationMetadataException(
                                "ArgumentIsNull",
                                null,
                                Metadata.ValidateNotNullOrEmptyCollectionFailure);
                        }

                        if (element is string elementAsString)
                        {
                            if (string.IsNullOrEmpty(elementAsString))
                            {
                                throw new ValidationMetadataException(
                                    "ArgumentCollectionContainsEmpty",
                                    null,
                                    Metadata.ValidateNotNullOrEmptyCollectionFailure);
                            }
                        }
                    } while (ienum.MoveNext());
                }

                if (isEmpty)
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyCollectionFailure);
                }
            }
            else if (arguments is IDictionary dict)
            {
                if (dict.Count == 0)
                {
                    throw new ValidationMetadataException(
                        "ArgumentIsEmpty",
                        null,
                        Metadata.ValidateNotNullOrEmptyCollectionFailure);
                }
            }
        }
    }

    #endregion NULL validation attributes

    #endregion Data validate Attributes

    #region Data Generation Attributes

    /// <summary>
    /// Serves as the base class for attributes that perform argument transformation.
    /// </summary>
    /// <remarks>
    /// Argument transformation attributes can be attached to
    /// <see cref="Cmdlet"/> and <see cref="Provider.CmdletProvider"/>
    /// parameters to automatically transform the argument value in
    /// some fashion.  The transformation might change the object,
    /// convert the type, or even load a file or AD object based on
    /// the name.
    /// Existing argument transformation attributes include
    /// <see cref="ArgumentTypeConverterAttribute"/>.
    ///
    /// PSSnapins wishing to create custom argument transformation attributes
    /// should derive from
    /// <seealso cref="ArgumentTransformationAttribute"/>
    /// and override the
    /// <seealso cref="ArgumentTransformationAttribute.Transform"/>
    /// abstract method, after which they can apply the
    /// attribute to their parameters.
    ///
    /// It is also recommended to override
    /// <see cref="System.Object.ToString"/> to return a readable string
    /// similar to the attribute declaration, for example
    /// "[ValidateRangeAttribute(5,10)]".
    ///
    /// If multiple transformations are defined on a parameter,
    /// they will be invoked in series, each getting the output
    /// of the previous transformation.
    /// </remarks>
    /// <seealso cref="ArgumentTypeConverterAttribute"/>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public abstract class ArgumentTransformationAttribute : CmdletMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the ArgumentTransformationAttribute class.
        /// </summary>
        protected ArgumentTransformationAttribute()
        {
        }

        /// <summary>
        /// Method that will be overridden by the subclasses to transform the inputData parameter
        /// argument into some other object that will be used for the parameter's value.
        /// </summary>
        /// <param name="engineIntrinsics">
        /// The engine APIs for the context under which the transformation is being
        /// made.
        /// </param>
        /// <param name="inputData">Parameter argument to mutate.</param>
        /// <returns>Mutated object(s).</returns>
        /// <remarks>
        /// Return the transformed value of <paramref name="inputData"/>.
        /// Throw <see cref="ArgumentException"/>
        /// if the value of <paramref name="inputData"/> is invalid,
        /// and throw <see cref="ArgumentTransformationMetadataException"/>
        /// for other recoverable errors.
        /// </remarks>
        /// <exception cref="ArgumentException">Should be thrown for invalid arguments.</exception>
        /// <exception cref="ArgumentTransformationMetadataException">Should be thrown for any problems during transformation.</exception>
        public abstract object Transform(EngineIntrinsics engineIntrinsics, object inputData);

        /// <summary>
        /// Transform inputData and track the flow of untrusted object.
        /// NOTE: All internal handling of ArgumentTransformationAttribute should use this method to track the trustworthiness of
        /// the data input source by default.
        /// </summary>
        /// <remarks>
        /// The default value for <paramref name="trackDataInputSource"/> is True.
        /// You should stick to the default value for this parameter in most cases so that data input source is tracked during the transformation.
        /// The only acceptable exception is when this method is used in Compiler or Binder where you can generate extra code to track input source
        /// when it's necessary. This is to minimize the overhead when tracking is not needed.
        /// </remarks>
        internal object TransformInternal(EngineIntrinsics engineIntrinsics, object inputData, bool trackDataInputSource = true)
        {
            object result = Transform(engineIntrinsics, inputData);
            if (trackDataInputSource && engineIntrinsics != null)
            {
                ExecutionContext.PropagateInputSource(inputData, result, engineIntrinsics.SessionState.Internal.LanguageMode);
            }

            return result;
        }

        /// <summary>
        /// The property is only checked when:
        ///   a) The parameter is not mandatory
        ///   b) The argument is null.
        /// </summary>
        public virtual bool TransformNullOptionalParameters { get { return true; } }
    }

    #endregion Data Generation Attributes
}
