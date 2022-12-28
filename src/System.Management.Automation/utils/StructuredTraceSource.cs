// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#define TRACE

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;

namespace System.Management.Automation
{
    #region PSTraceSourceOptions
    /// <summary>
    /// These flags enable tracing based on the types of a tracing supplied.
    /// Each type of tracing will allow for one or more methods
    /// in the StructuredTraceSource class to become "enabled".
    /// </summary>
    [Flags]
    public enum PSTraceSourceOptions
    {
        /// <summary>All tracing off.</summary>
        None = 0x00000000,

        /// <summary>Constructors will be traced.</summary>
        Constructor = 0x00000001,

        /// <summary>Dispose will be traced.</summary>
        Dispose = 0x00000002,

        /// <summary>Finalize will be traced.</summary>
        Finalizer = 0x00000004,

        /// <summary>Methods will be traced.</summary>
        Method = 0x00000008,

        /// <summary>Properties will be traced.</summary>
        Property = 0x00000010,

        /// <summary>Delegates will be traced.</summary>
        Delegates = 0x00000020,

        /// <summary>Events will be traced.</summary>
        Events = 0x00000040,

        /// <summary>Exceptions will be traced.</summary>
        Exception = 0x00000080,

        /// <summary>Locks will be traced.</summary>
        Lock = 0x00000100,

        /// <summary>Errors will be traced.</summary>
        Error = 0x00000200,

        /// <summary>Warnings will be traced.</summary>
        Warning = 0x00000400,

        /// <summary>Verbose messages will be traced.</summary>
        Verbose = 0x00000800,

        /// <summary>WriteLines will be traced.</summary>
        WriteLine = 0x00001000,

        /// <summary>TraceScope calls will be traced.</summary>
        Scope = 0x00002000,

        /// <summary>Assertions will be traced.</summary>
        Assert = 0x00004000,

        /// <summary>A combination of flags that trace the execution flow will be traced.</summary>
        /// <remarks>
        /// The methods associated with the flags; Constructor, Dispose,
        /// Finalizer, Method, Delegates, and Events will be enabled
        /// </remarks>
        ExecutionFlow =
            Constructor |
            Dispose |
            Finalizer |
            Method |
            Delegates |
            Events |
            Scope,

        /// <summary>A combination of flags that trace the data will be traced be traced.</summary>
        /// <remarks>
        /// The methods associated with the flags; Constructor, Dispose,
        /// Finalizer, Property, and WriteLine will be enabled
        /// </remarks>
        Data =
            Constructor |
            Dispose |
            Finalizer |
            Property |
            Verbose |
            WriteLine,

        /// <summary>A combination of flags that trace the errors.</summary>
        /// <remarks>
        /// The methods associated with the flags; Error,
        /// and Exception will be enabled
        /// </remarks>
        Errors =
            Error |
            Exception,

        /// <summary>All combination of trace flags will be set be traced.</summary>
        /// <remarks>
        /// All methods for tracing will be enabled.
        /// </remarks>
        All =
            Constructor |
            Dispose |
            Finalizer |
            Method |
            Property |
            Delegates |
            Events |
            Exception |
            Error |
            Warning |
            Verbose |
            Lock |
            WriteLine |
            Scope |
            Assert
    }

    #endregion PSTraceSourceOptions

    /// <summary>
    /// An PSTraceSource is a representation of a System.Diagnostics.TraceSource instance
    /// that is used in the PowerShell components to produce trace output.
    /// </summary>
    /// <remarks>
    /// To get an instance of this class a user should define a static
    /// field of the type StructuredTraceSource, and assign the results of GetTracer() to it.
    /// If the category should be automatically put in the application config file the
    /// field should be decorated with the TraceSourceAttribute so that GenerateAppConfigFile.exe
    /// can find it through reflection.
    /// <example>
    /// <code>
    /// [TraceSourceAttribute("category", "description")]
    /// public static StructuredTraceSource tracer = GetTracer("category", "description", true);
    /// </code>
    /// </example>
    /// Other than initial creation of this class through the GetTracer method,
    /// this class should throw no exceptions. Any call to a StructuredTraceSource method
    /// that results in an exception being thrown will be ignored.
    /// </remarks>
    public partial class PSTraceSource
    {
        #region PSTraceSource construction methods

        /// <summary>
        /// Initializes a new instance of the <see cref="PSTraceSource"/> class.
        /// </summary>
        /// <param name="fullName">
        /// The full name for the trace category. This is different from the name parameter as
        /// it is not limited to 16 characters.
        /// </param>
        /// <param name="name">
        /// The name of the category that this class
        /// will control the tracing for. This parameter must always be 16 characters to ensure
        /// proper formatting of the output.
        /// </param>
        /// <param name="description">
        /// The description to describe what the category
        /// is used for.
        /// </param>
        /// <param name="traceHeaders">
        /// If true, the line headers will be traced, if false, only the trace message will be traced.
        /// </param>
        internal PSTraceSource(string fullName, string name, string? description, bool traceHeaders)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(fullName);
            ArgumentNullException.ThrowIfNullOrEmpty(name);

            FullName = fullName;
            _name = name;

            try
            {
                // TODO: move this to startup json file instead of using env var
                string? tracingEnvVar = Environment.GetEnvironmentVariable("MshEnableTrace");

                if (string.Equals(tracingEnvVar, "True", StringComparison.OrdinalIgnoreCase))
                {
                    string? options = this.TraceSource.Attributes["Options"];
                    if (options is not null)
                    {
                        _flags = Enum.Parse<PSTraceSourceOptions>(options, true);
                    }
                }

                ShowHeaders = traceHeaders;
                Description = description;
            }
            catch (System.Xml.XmlException)
            {
                // This exception occurs when the config
                // file is malformed. Just default to Off.
                _flags = PSTraceSourceOptions.None;
            }
            catch (System.Configuration.ConfigurationException)
            {
                // This exception occurs when the config
                // file is malformed. Just default to Off.
                _flags = PSTraceSourceOptions.None;
            }
        }

        private static bool globalTraceInitialized;

        /// <summary>
        /// Traces the app domain header with information about the execution
        /// time, the platform, etc.
        /// </summary>
        internal void TraceGlobalAppDomainHeader()
        {
            // Only trace the global header if it hasn't
            // already been traced
            if (globalTraceInitialized)
            {
                return;
            }

            // AppDomain
            Write(PSTraceSourceOptions.All, $"Initializing tracing for AppDomain: {AppDomain.CurrentDomain.FriendlyName}");

            // Current time
            Write(PSTraceSourceOptions.All, $"\tCurrent time: {DateTime.Now}");

            // OS build
            Write(PSTraceSourceOptions.All, $"\tOS Build: {Environment.OSVersion}");

            // .NET Framework version
            Write(PSTraceSourceOptions.All, $"\tFramework Build: {Environment.Version}\n");

            // Mark that we have traced the global header
            globalTraceInitialized = true;
        }

        /// <summary>
        /// Outputs a header when a new StructuredTraceSource object is created.
        /// </summary>
        /// <param name="callingAssembly">
        /// The assembly that created the instance of the StructuredTraceSource.
        /// </param>
        /// <remarks>
        /// A header will be output that contains information such as;
        /// the category and description of the new trace object,
        /// the assembly in which the new trace object
        /// will be stored.
        /// </remarks>
        internal void TracerObjectHeader(
            Assembly callingAssembly)
        {
            if (_flags == PSTraceSourceOptions.None)
            {
                return;
            }

            // Write the header for the new trace object
            Write(PSTraceSourceOptions.All, $"Creating tracer:");

            // Category
            Write(PSTraceSourceOptions.All, $"\tCategory: {Name}");

            // Description
            Write(PSTraceSourceOptions.All, $"\tDescription: {Description}");

            if (callingAssembly != null)
            {
                // Assembly name
                Write(PSTraceSourceOptions.All, $"\tAssembly: {callingAssembly.FullName}");

                // Assembly location
                Write(PSTraceSourceOptions.All, $"\tAssembly Location: {callingAssembly.Location}");

                // Assembly File timestamp
                FileInfo assemblyFileInfo =
                    new FileInfo(callingAssembly.Location);

                Write(PSTraceSourceOptions.All, $"\tAssembly File Timestamp: {assemblyFileInfo.CreationTime}");
            }

            // Label
            Write(PSTraceSourceOptions.All, $"\tFlags: {_flags}");
        }
        #endregion StructuredTraceSource constructor methods

        #region PSTraceSourceOptions.Error,Warning,Normal methods/helpers

        /// <summary>Traces the formatted output.</summary>
        /// <param name="handler">The interpolated string handler.</param>
        /// <param name="traceType">Trace type based on <see cref="PSTraceSourceOptions"/>.</param>
        internal void Write(PSTraceSourceOptions traceType, [InterpolatedStringHandlerArgument("", "traceType")] OutputLineIfInterpolatedStringHandler handler)
        {
            if (_flags.HasFlag(traceType))
            {
                try
                {
                    this.TraceSource.TraceInformation(handler.ToStringAndClear());
                }
                catch
                {
                    // Eat all exceptions.
                    //
                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }
        }

        /// <summary>
        /// Add trace prefix to the interpolated string handler buffer.
        /// </summary>
        /// <param name="handler">Buffer to add the prefix to.</param>
        /// <param name="traceOption">Name of <see cref="PSTraceSourceOptions"/> flag that traced.</param>
        internal void AppendOutputLinePrefix(
            ref DefaultInterpolatedStringHandler handler,
            PSTraceSourceOptions traceOption)
        {
            try
            {
                if (ShowHeaders)
                {
                    // -11 is length of largest element from PSTraceSourceOptions, i.e. 'Constructor'.
                    handler.AppendFormatted(Enum.GetName<PSTraceSourceOptions>(traceOption), -11);
                    handler.AppendLiteral(": ");
                }

                // Add the spaces for the indent.
                // The Trace.IndentSize does not change at all
                // through the running of the process so there
                // are no thread issues here.
                int indentSize = Trace.IndentSize;
                int threadIndentLevel = ThreadIndentLevel;

                handler.AppendLiteral(System.Management.Automation.Internal.StringUtil.Padding(indentSize * threadIndentLevel));
            }
            catch
            {
                // Eat all exceptions.
                //
                // Do not assert here because exceptions can be
                // raised while a thread is shutting down during
                // normal operation.
            }
        }

        #endregion PSTraceSourceOptions.Error methods/helpers

        #region Class helper methods and properties

        /// <summary>
        /// Gets the method name of the method that called this one
        /// plus the skipFrames.
        /// </summary>
        /// <remarks>
        /// For instance, GetCallingMethodNameAndParameters(1)
        /// will return the method that called the method that is calling
        /// GetCallingMethodNameAndParameters.
        /// </remarks>
        /// <param name="skipFrames">
        /// The number of frames to skip in the calling stack.
        /// </param>
        /// <returns>
        /// The name of the method on the stack.
        /// </returns>
        internal static string GetCallingMethodNameAndParameters(int skipFrames)
        {
            string result = string.Empty;

            try
            {
                // Use the stack to get the method and type information
                // for the calling method
                StackFrame stackFrame = new StackFrame(++skipFrames);
                MethodBase? callingMethod = stackFrame.GetMethod();

                if (callingMethod is not null && callingMethod.DeclaringType is not null)
                {

                    Type? declaringType = callingMethod.DeclaringType;

                    // Note: don't use the FullName for the declaringType
                    // as it is usually way too long and makes the trace
                    // output hard to read.
                    result = string.Create(CultureInfo.CurrentCulture, $"{declaringType.Name}.{callingMethod.Name}()");
                }
            }
            catch
            {
                // Eat all exceptions

                // Do not assert here because exceptions can be
                // raised while a thread is shutting down during
                // normal operation.
            }

            return result;
        }

        /// <summary>
        /// Property to access the indent level in thread local storage.
        /// </summary>
        internal static int ThreadIndentLevel
        {
            get
            {
                // The first time access the ThreadLocal instance, the default int value will be used
                // to initialize the instance. The default int value is 0.
                return s_localIndentLevel.Value;
            }

            set
            {
                if (value >= 0)
                {
                    // Set the new indent level in thread local storage
                    s_localIndentLevel.Value = value;
                }
                else
                {
                    Diagnostics.Assert(value >= 0, "The indention value cannot be less than zero");
                }
            }
        }

        /// <summary>Allocates some thread local storage to hold the indent level.</summary>
        private static readonly ThreadLocal<int> s_localIndentLevel = new ThreadLocal<int>();

        /// <summary>Local storage for the trace switch flags.</summary>
        private PSTraceSourceOptions _flags = PSTraceSourceOptions.None;
        private readonly string _name;
        private TraceSource? _traceSource;


        /// <summary>Gets the full name of the trace source category.</summary>
        internal string FullName { get; } = string.Empty;

        internal bool IsEnabled
        {
            get => _flags != PSTraceSourceOptions.None;
        }

        /// <summary>Determines if the line and switch headers should be shown.</summary>
        internal bool ShowHeaders { get; set; } = true;

        /// <summary>Creates an instance of the TraceSource on demand.</summary>
        internal TraceSource TraceSource
        {
            get { return _traceSource ??= new MonadTraceSource(_name); }
        }

        #endregion Class helper methods and properties

        #region Public members

        /// <summary>Gets the attributes of the TraceSource.</summary>
        public StringDictionary Attributes
        {
            get => TraceSource.Attributes;
        }

        /// <summary>Gets or sets the description for this trace sources.</summary>
        public string? Description { get; set; } = string.Empty;

        /// <summary>Gets the listeners for the TraceSource.</summary>
        public TraceListenerCollection Listeners
        {
            get => TraceSource.Listeners;
        }

        /// <summary>Gets the TraceSource name (also known as category).</summary>
        /// <remarks>
        /// Note, this name is truncated to 16 characters due to limitations
        /// in the TraceSource class.
        /// </remarks>
        public string Name
        {
            get => _name;
        }

        /// <summary>Gets or sets the options for what will be traced.</summary>
        public PSTraceSourceOptions Options
        {
            get => _flags;

            set
            {
                _flags = value;
                this.TraceSource.Switch.Level = (SourceLevels)_flags;
            }
        }

        /// <summary>Gets or sets the TraceSource's Switch.</summary>
        public SourceSwitch Switch
        {
            get => TraceSource.Switch;

            set => TraceSource.Switch = value;
        }
        #endregion Public members

        #region TraceCatalog

        /// <summary>Storage for all the PSTraceSource instances.</summary>
        internal static Dictionary<string, PSTraceSource> TraceCatalog { get; } = new Dictionary<string, PSTraceSource>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Storage for trace source instances which have not been instantiated but for which
        /// the user has specified Options.
        ///
        /// If the PSTraceSource cannot be found in the TraceCatalog, the same name is used
        /// to look in this dictionary to see if the PSTraceSource has been pre-configured.
        /// </summary>
        internal static Dictionary<string, PSTraceSource> PreConfiguredTraceSource { get; } = new Dictionary<string, PSTraceSource>(StringComparer.OrdinalIgnoreCase);

        #endregion TraceCatalog
    }

    #region ScopeTracer object/helpers
    /// <summary>
    /// A light-weight object to manage the indention of
    /// trace output for each thread.
    /// </summary>
    /// <remarks>
    /// An instance of this object is returned when any scoping
    /// Trace method (like TraceMethod, TraceProperty, etc.)
    /// is called. In the constructor to the object the indention
    /// level for the thread is incremented.
    /// The Dispose method will decrement the thread indent level.
    /// </remarks>
    internal class ScopeTracer : IDisposable
    {
        /// <summary>
        /// Constructor that traces the scope name
        /// and raises the indent level in thread
        /// local storage.
        /// </summary>
        /// <param name="tracer">
        /// The trace object that is to be used for output
        /// </param>
        /// <param name="flag">
        /// The PSTraceSourceOptions that is causing the scope object to
        /// be created.
        /// </param>
        /// <param name="scopeOutputFormatter">
        /// This format string is used to determine the
        /// general output format for the scope. For instance,
        /// TraceMethod would probably provide a formatter similar
        /// to "Entering: {0}: {1}" where {0} is the name of the
        /// method and {1} is the additional formatted info provided.
        /// </param>
        /// <param name="leavingScopePrefix">
        /// The prefix string used to determine the general output
        /// format for the scope when the Dispose method is called.
        /// </param>
        /// <param name="scopeName">
        /// The name of the scope that is being traced
        /// </param>
        internal ScopeTracer(
            PSTraceSource tracer,
            PSTraceSourceOptions flag,
            string? scopeOutputFormatter,
            string? leavingScopePrefix,
            string scopeName)
        {
            ArgumentNullException.ThrowIfNull(tracer);

            _tracer = tracer;

            ScopeTracerHelper(
                flag,
                scopeOutputFormatter,
                leavingScopePrefix,
                scopeName,
                format: string.Empty);
        }

        /// <summary>
        /// Constructor that traces the scope name
        /// and raises the indent level in thread
        /// local storage.
        /// </summary>
        /// <param name="tracer">
        /// The trace object that is to be used for output
        /// </param>
        /// <param name="flag">
        /// The PSTraceSourceOptions that is causing the scope object to
        /// be created.
        /// </param>
        /// <param name="scopeOutputFormatter">
        /// This format string is used to determine the
        /// general output format for the scope. For instance,
        /// TraceMethod would probably provide a formatter similar
        /// to "Entering: {0}: {1}" where {0} is the name of the
        /// method and {1} is the additional formatted info provided.
        /// </param>
        /// <param name="leavingScopePrefix">
        /// The prefix string used to determine the general output
        /// format for the scope when the Dispose method is called.
        /// </param>
        /// <param name="scopeName">
        /// The name of the scope that is being traced
        /// </param>
        /// <param name="format">
        /// The format of any additional arguments which will be appended
        /// to the line of trace output
        /// </param>
        /// <param name="args">
        /// Arguments to the format string.
        /// </param>
        internal ScopeTracer(
            PSTraceSource tracer,
            PSTraceSourceOptions flag,
            string? scopeOutputFormatter,
            string? leavingScopePrefix,
            string scopeName,
            string format,
            params object?[] args)
        {
            ArgumentNullException.ThrowIfNull(tracer);
            ArgumentNullException.ThrowIfNull(args);

            _tracer = tracer;

            if (format is not null)
            {
                ScopeTracerHelper(
                    flag,
                    scopeOutputFormatter,
                    leavingScopePrefix,
                    scopeName,
                    format,
                    args);
            }
            else
            {
                ScopeTracerHelper(
                    flag,
                    scopeOutputFormatter,
                    leavingScopePrefix,
                    scopeName,
                    format: string.Empty);
            }
        }

        /// <summary>
        /// Helper for the ScopeTracer constructor.
        /// </summary>
        /// <param name="flag">
        /// The flag that caused this line of tracing to be traced.
        /// </param>
        /// <param name="scopeOutputFormatter">
        /// This format string is used to determine the
        /// general output format for the scope. For instance,
        /// TraceMethod would probably provide a formatter similar
        /// to "Entering: {0}: {1}" where {0} is the name of the
        /// method and {1} is the additional formatted info provided.
        /// </param>
        /// <param name="leavingScopePrefix">
        /// The prefix string used to determine the general output
        /// format for the scope when the Dispose method is called.
        /// </param>
        /// <param name="scopeName">
        /// The name of the scope being entered
        /// </param>
        /// <param name="format">
        /// The format of any additional arguments which will be appended
        /// to the "Entering" line of trace output
        /// </param>
        /// <param name="args">
        /// Arguments to the format string.
        /// </param>
        [MemberNotNull(nameof(_scopeName))]
        internal void ScopeTracerHelper(
            PSTraceSourceOptions flag,
            string? scopeOutputFormatter,
            string? leavingScopePrefix,
            string scopeName,
            string format,
            params object?[] args)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(scopeName);

            // Store the flags, scopeName, and the leavingScopePrefix
            // so that it can be used in the Dispose method
            _flag = flag;
            _scopeName = scopeName;
            _leavingScopePrefix = leavingScopePrefix;

            // Format the string for output
            StringBuilder output = new StringBuilder();

            if (!string.IsNullOrEmpty(scopeOutputFormatter))
            {
                output.AppendFormat(
                    CultureInfo.CurrentCulture,
                    scopeOutputFormatter,
                    _scopeName);
            }

            if (!string.IsNullOrEmpty(format))
            {
                output.AppendFormat(
                    CultureInfo.CurrentCulture,
                    format,
                    args);
            }

            // Now write the trace
            _tracer.Write(_flag, $"{output}");

            // Increment the current thread indent level
            PSTraceSource.ThreadIndentLevel++;
        }

        /// <summary>
        /// Decrements the indent level in thread local
        /// storage and then traces the scope name.
        /// </summary>
        public void Dispose()
        {
            // Decrement the indent level in thread local storage
            PSTraceSource.ThreadIndentLevel--;

            // Trace out the scope name
            if (!string.IsNullOrEmpty(_leavingScopePrefix))
            {
                _tracer.Write(_flag, $"{_leavingScopePrefix}: {_scopeName}");
            }

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The trace object that is used for any output.
        /// </summary>
        private readonly PSTraceSource _tracer;

        /// <summary>
        /// The flag which caused this scope object to be created.
        /// </summary>
        private PSTraceSourceOptions _flag;

        /// <summary>
        /// Stores the scope name that is passed to the constructor.
        /// </summary>
        private string _scopeName;

        /// <summary>
        /// Stores the format string used when formatting output when
        /// leaving the scope.
        /// </summary>
        private string? _leavingScopePrefix;
    }
    #endregion ScopeTracer object/helpers

    #region PSTraceSourceAttribute
    /// <summary>
    /// This attribute is placed on the field of the PSTraceSource class
    /// in the class that is consuming the tracing methods defined in
    /// this file. It defines the trace category and description
    /// for that instance of PSTraceSource.
    /// </summary>
    /// <remarks>
    /// This attribute is only allowed on fields and there can only
    /// be one for each instance. Only one instance of this attribute
    /// should be used in any one class.
    /// In order for the attribute to be used to help in constructing
    /// the PSTraceSource object, reflection is used to find the field
    /// that the PSTraceSource object will be assigned to. This attribute
    /// declares the category and description for the PSTraceSource object
    /// in that field.  Having multiple instances of this attribute on
    /// multiple fields in the same class will cause unexpected results.
    /// For instance, trace output for one category may actually be
    /// considered part of another category.
    /// </remarks>
    [AttributeUsage(
         AttributeTargets.Field,
         AllowMultiple = false)]
    internal class TraceSourceAttribute : Attribute
    {
        /// <summary>
        /// Constructor for the TraceSourceAttribute class.
        /// </summary>
        /// <param name="category">
        /// The name of the category for which the TraceSource instance
        /// will be used.
        /// </param>
        /// <param name="description">
        /// A description for the category.
        /// </param>
        internal TraceSourceAttribute(
            string category,
            string? description)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(category);

            Category = category;
            Description = description;
        }

        /// <summary>The category to be used for the TraceSource.</summary>
        internal string Category { get; }

        /// <summary>The description for the category to be used for the TraceSource.</summary>
        internal string? Description { get; set; }
    }
    #endregion TraceSourceAttribute

    #region MonadTraceSource

    /// <summary>
    /// This derived class of TraceSource is required so that we can tell
    /// the configuration infrastructure which attributes are supported in
    /// the XML app-config file for our trace source.
    /// </summary>
    internal class MonadTraceSource : TraceSource
    {
        internal MonadTraceSource(string name)
            : base(name)
        {
        }

        /// <summary>Tells the config infrastructure which attributes are supported for our TraceSource.</summary>
        /// <returns>A string array with the names of the attributes supported by our trace source.</returns>
        protected override string[] GetSupportedAttributes()
        {
            return new string[] { "Options" };
        }
    }
    #endregion MonadTraceSource

    [InterpolatedStringHandler]
    internal ref struct OutputLineIfInterpolatedStringHandler
    {
        /// <summary>The handler we use to perform the conditional formatting in <see cref="PSTraceSource.Write(PSTraceSourceOptions, OutputLineIfInterpolatedStringHandler)"/>.</summary>
        private DefaultInterpolatedStringHandler _handler;

        /// <summary>Creates an instance of the handler.</summary>
        /// <param name="literalLength">The number of constant characters outside of interpolation expressions in the interpolated string.</param>
        /// <param name="formattedCount">The number of interpolation expressions in the interpolated string.</param>
        /// <param name="trace">Instance of current <see cref="PSTraceSource"/>.</param>
        /// <param name="option">A flag of <see cref="PSTraceSource"/>.</param>
        /// <param name="shouldAppend">A value indicating whether formatting should proceed.</param>
        /// <remarks>This is intended to be called only by compiler-generated code. Arguments are not validated as they'd otherwise be for members intended to be used directly.</remarks>
        public OutputLineIfInterpolatedStringHandler(int literalLength, int formattedCount, PSTraceSource trace, PSTraceSourceOptions option, out bool shouldAppend)
        {
            if ((trace.Options & option) != PSTraceSourceOptions.None)
            {
                _handler = new DefaultInterpolatedStringHandler(literalLength, formattedCount, CultureInfo.CurrentCulture);

                shouldAppend = true;

                trace.AppendOutputLinePrefix(ref _handler, option);

            }
            else
            {
                shouldAppend = false;
                _handler = default;
            }
        }

        /// <summary>Extracts the built string from the handler.</summary>
        internal string ToStringAndClear()
        {
            string result = _handler.ToStringAndClear();

            // defensive clear
            this = default;
            return result;
        }

        /// <summary>Writes the specified string to the handler.</summary>
        /// <param name="value">The string to write.</param>
        public void AppendLiteral(string value) => _handler.AppendLiteral(value);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value) => _handler.AppendFormatted(value);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, string? format) => _handler.AppendFormatted(value, format);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment) => _handler.AppendFormatted(value, alignment);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="format">The format string.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <typeparam name="T">The type of the value to write.</typeparam>
        public void AppendFormatted<T>(T value, int alignment, string? format) => _handler.AppendFormatted(value, alignment, format);

        /// <summary>Writes the specified character span to the handler.</summary>
        /// <param name="value">The span to write.</param>
        public void AppendFormatted(ReadOnlySpan<char> value) => _handler.AppendFormatted(value);

        /// <summary>Writes the specified string of chars to the handler.</summary>
        /// <param name="value">The span to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        public void AppendFormatted(string? value) => _handler.AppendFormatted(value);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(string? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);

        /// <summary>Writes the specified value to the handler.</summary>
        /// <param name="value">The value to write.</param>
        /// <param name="alignment">Minimum number of characters that should be written for this value. If the value is negative, it indicates left-aligned and the required minimum is the absolute value.</param>
        /// <param name="format">The format string.</param>
        public void AppendFormatted(object? value, int alignment = 0, string? format = null) => _handler.AppendFormatted(value, alignment, format);
    }

    /// <summary>Allocation free scope trace.</summary>
    /// <remarks>
    /// It is disposable ref struct. 'Dispose()' method is autovatically called.
    /// <example>
    /// <code>
    ///     private static readonly PSTraceSource s_pathResolutionTracer =
    ///         PSTraceSource.GetTracer(
    ///             "PathResolution",
    ///             "Traces the path resolution algorithm.");
    ///     ...
    ///     using (new PSTraceScope(s_pathResolutionTracer, PSTraceSourceOptions.Scope, "<>", $"Path '{path}'"))
    ///     {
    ///         ...
    ///     }
    /// </code>
    /// <example>
    /// </remarks>
    internal ref struct PSTraceScope
    {
        private readonly PSTraceSource _traceSource;
        private readonly PSTraceSourceOptions _traceType;
        private readonly string _scopeName;

        /// <summary>Output initialize line and increment thread indentation level.</summary>
        /// <param name="traceSource"><see cref="PSTraceSource"/> instance.</param>
        /// <param name="traceType">Type of tracing. See <see cref="PSTraceSourceOptions"/> for more information.</param>
        /// <param name="scopeName">The scope name. If empty it is replaced with calling method name.</param>
        /// <param name="handler">Interolated string handler.</param>
        public PSTraceScope(PSTraceSource traceSource, PSTraceSourceOptions traceType, string scopeName, [InterpolatedStringHandlerArgument("traceSource", "traceType")] OutputLineIfInterpolatedStringHandler handler)
        {
            ArgumentNullException.ThrowIfNull(traceType);
            ArgumentNullException.ThrowIfNull(scopeName);

            _traceSource = traceSource;
            _traceType = traceType;

            _scopeName = scopeName != string.Empty
                ? scopeName
                : PSTraceSource.GetCallingMethodNameAndParameters(1);

            if (_traceSource.Options.HasFlag(_traceType))
            {
                try
                {
                    _traceSource.Write(_traceType, $"Enter: {_scopeName}");
                    _traceSource.TraceSource.TraceInformation(handler.ToStringAndClear());

                    // Increment the current thread indent level
                    PSTraceSource.ThreadIndentLevel++;
                }
                catch
                {
                    // Eat all exceptions.
                    //
                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }
        }

        /// <summary>Output finilize line and decrement thread indentation level.</summary>
        public void Dispose()
        {
            if (_traceSource.Options.HasFlag(_traceType))
            {
                try
                {
                    // Decrement the indent level in thread local storage
                    PSTraceSource.ThreadIndentLevel--;
                    _traceSource.Write(_traceType, $"Leave: {_scopeName}");
                }
                catch
                {
                    // Eat all exceptions.
                    //
                    // Do not assert here because exceptions can be
                    // raised while a thread is shutting down during
                    // normal operation.
                }
            }
        }
    }
}
