// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#define TRACE

using System.Reflection;
using System.Management.Automation.Internal;

namespace System.Management.Automation
{
    /// <summary>
    /// An PSTraceSource is a representation of a System.Diagnostics.TraceSource instance
    /// that is used the the Monad components to produce trace output.
    /// </summary>
    /// <remarks>
    /// It is permitted to subclass <see cref="PSTraceSource"/>
    /// but there is no established scenario for doing this, nor has it been tested.
    /// </remarks>
    /// <!--
    /// IF YOU ARE NOT PART OF THE MONAD DEVELOPMENT TEAM PLEASE
    /// DO NOT USE THIS CLASS!!!!!
    ///
    /// The PSTraceSource class is derived from Switch to provide granular
    /// control over the tracing in a program.  An instance of PSTraceSource
    /// is created for each category of tracing such that separate flags
    /// (filters) can be set. Each flag enables one or more method for tracing.
    ///
    /// For instance, the Exception flag will enable tracing on these methods:
    ///     TraceException.
    /// </summary>
    /// <remarks>
    /// To get an instance of this class a user should define a public static
    /// field of the type PSTraceSource, decorated it with an attribute of
    /// PSTraceSourceAttribute, and assign the results of GetTracer to it.
    /// <newpara/>
    /// <example>
    /// <code>
    /// [PSTraceSourceAttribute("category", "description")]
    /// public static PSTraceSource tracer = GetTracer("category", "description");
    /// </code>
    /// </example>
    /// <newpara/>
    /// Other than initial creation of this class through the GetTracer method,
    /// this class should throw no exceptions. Any call to a PSTraceSource method
    /// that results in an exception being thrown will be ignored.
    /// -->
    public partial class PSTraceSource
    {
        /// <summary>
        /// Lock object for the GetTracer method.
        /// </summary>
        private static object s_getTracerLock = new object();

        /// <summary>
        /// A helper to get an instance of the PSTraceSource class.
        /// </summary>
        /// <param name="name">
        /// The name of the category that this class
        /// will control the tracing for.
        /// </param>
        /// <param name="description">
        /// The description to describe what the category
        /// is used for.
        /// </param>
        /// <returns>
        /// An instance of the PSTraceSource class which is initialized
        /// to trace for the specified category. If multiple callers ask for the same category,
        /// the same PSTraceSource will be returned.
        /// </returns>
        internal static PSTraceSource GetTracer(
            string name,
            string description)
        {
            return PSTraceSource.GetTracer(name, description, true);
        }

        /// <summary>
        /// A helper to get an instance of the PSTraceSource class.
        /// </summary>
        /// <param name="name">
        /// The name of the category that this class
        /// will control the tracing for.
        /// </param>
        /// <param name="description">
        /// The description to describe what the category
        /// is used for.
        /// </param>
        /// <param name="traceHeaders">
        /// If true, the line headers will be traced, if false, only the trace message will be traced.
        /// </param>
        /// <returns>
        /// An instance of the PSTraceSource class which is initialized
        /// to trace for the specified category. If multiple callers ask for the same category,
        /// the same PSTraceSource will be returned.
        /// </returns>
        internal static PSTraceSource GetTracer(
            string name,
            string description,
            bool traceHeaders)
        {
            if (string.IsNullOrEmpty(name))
            {
                // 2005/04/13-JonN In theory this should be ArgumentException,
                // but I don't want to deal with loading the string in this
                // low-level code.
                throw new ArgumentNullException("name");
            }

            lock (PSTraceSource.s_getTracerLock)
            {
                PSTraceSource result = null;

                // See if we can find an PSTraceSource for this category in the catalog.
                PSTraceSource.TraceCatalog.TryGetValue(name, out result);

                // If it's not already in the catalog, see if we can find it in the
                // pre-configured trace source list

                if (result == null)
                {
                    string keyName = name;
                    if (!PSTraceSource.PreConfiguredTraceSource.ContainsKey(keyName))
                    {
                        if (keyName.Length > 16)
                        {
                            keyName = keyName.Substring(0, 16);
                            if (!PSTraceSource.PreConfiguredTraceSource.ContainsKey(keyName))
                            {
                                keyName = null;
                            }
                        }
                        else
                        {
                            keyName = null;
                        }
                    }

                    if (keyName != null)
                    {
                        // Get the pre-configured trace source from the catalog
                        PSTraceSource preconfiguredSource = PSTraceSource.PreConfiguredTraceSource[keyName];

                        result = PSTraceSource.GetNewTraceSource(keyName, description, traceHeaders);
                        result.Options = preconfiguredSource.Options;
                        result.Listeners.Clear();
                        result.Listeners.AddRange(preconfiguredSource.Listeners);

                        // Add it to the TraceCatalog
                        PSTraceSource.TraceCatalog.Add(keyName, result);

                        // Remove it from the pre-configured catalog
                        PSTraceSource.PreConfiguredTraceSource.Remove(keyName);
                    }
                }

                // Even if there was a PSTraceSource in the catalog, let's replace
                // it with an PSTraceSource to get the added functionality. Anyone using
                // a StructuredTraceSource should be able to do so even with the PSTraceSource
                // instance.

                if (result == null)
                {
                    result = PSTraceSource.GetNewTraceSource(name, description, traceHeaders);
                    PSTraceSource.TraceCatalog[result.FullName] = result;
                }

                if (result.Options != PSTraceSourceOptions.None &&
                    traceHeaders)
                {
                    result.TraceGlobalAppDomainHeader();

                    // Trace the object specific tracer information
                    result.TracerObjectHeader(Assembly.GetCallingAssembly());
                }

                return result;
            }
        }

        internal static PSTraceSource GetNewTraceSource(
            string name,
            string description,
            bool traceHeaders)
        {
            if (string.IsNullOrEmpty(name))
            {
                // Note, all callers should have already verified the name before calling this
                // API, so this exception should never be exposed to an end-user.

                throw new ArgumentException("name");
            }

            // Keep the fullName as it was passed, but truncate or pad
            // the category name to 16 characters.  This allows for
            // uniform output

            string fullName = name;
            /*
                            // This is here to ensure all the trace category names are 16 characters,
                            // the problem is that the app-config file would need to contain the same
                            // trailing spaces if this actually does pad the name.

                            name =
                                string.Format(
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    "{0,-16}",
                                    name);
            */
            PSTraceSource result =
                new PSTraceSource(
                    fullName,
                    name,
                    description,
                    traceHeaders);
            return result;
        }

        #region TraceFlags.New*Exception methods/helpers

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This is not allowed to call other
        /// Throw*Exception variants, since they call this.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was null
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentNullException NewArgumentNullException(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException("paramName");
            }

            string message = StringUtil.Format(AutomationExceptions.ArgumentNull, paramName);
            var e = new PSArgumentNullException(paramName, message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant allows the caller to
        /// specify alternate template text, but only in assembly S.M.A.Core.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was invalid
        /// </param>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentNullException NewArgumentNullException(
            string paramName, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException("paramName");
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);

            // Note that the paramName param comes first
            var e = new PSArgumentNullException(paramName, message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant uses the default
        /// ArgumentException template text. This is not allowed to call
        /// other Throw*Exception variants, since they call this.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was invalid
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentException NewArgumentException(string paramName)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException("paramName");
            }

            string message = StringUtil.Format(AutomationExceptions.Argument, paramName);
            // Note that the message param comes first
            var e = new PSArgumentException(message, paramName);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant allows the caller to
        /// specify alternate template text, but only in assembly S.M.A.Core.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was invalid
        /// </param>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentException NewArgumentException(
            string paramName, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException("paramName");
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);

            // Note that the message param comes first
            var e = new PSArgumentException(message, paramName);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception.
        /// </summary>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSInvalidOperationException NewInvalidOperationException()
        {
            string message = StringUtil.Format(AutomationExceptions.InvalidOperation,
                    new System.Diagnostics.StackTrace().GetFrame(1).GetMethod().Name);
            var e = new PSInvalidOperationException(message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant allows the caller to
        /// specify alternate template text, but only in assembly S.M.A.Core.
        /// </summary>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSInvalidOperationException NewInvalidOperationException(
            string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);

            var e = new PSInvalidOperationException(message);
            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant allows the caller to
        /// specify alternate template text, but only in assembly S.M.A.Core.
        /// </summary>
        /// <param name="innerException">
        /// This is the InnerException for the InvalidOperationException
        /// </param>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSInvalidOperationException NewInvalidOperationException(
            Exception innerException,
            string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);

            var e = new PSInvalidOperationException(message, innerException);
            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This is not allowed to call other
        /// Throw*Exception variants, since they call this.
        /// </summary>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSNotSupportedException NewNotSupportedException()
        {
            string message = StringUtil.Format(AutomationExceptions.NotSupported,
                new System.Diagnostics.StackTrace().GetFrame(0).ToString());
            var e = new PSNotSupportedException(message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This is not allowed to call other
        /// Throw*Exception variants, since they call this.
        /// </summary>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSNotSupportedException NewNotSupportedException(
            string resourceString,
            params object[] args)
        {
            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);
            var e = new PSNotSupportedException(message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This is not allowed to call other
        /// Throw*Exception variants, since they call this.
        /// </summary>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSNotImplementedException NewNotImplementedException()
        {
            string message = StringUtil.Format(AutomationExceptions.NotImplemented,
                new System.Diagnostics.StackTrace().GetFrame(0).ToString());
            var e = new PSNotImplementedException(message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant uses the default
        /// ArgumentOutOfRangeException template text. This is not allowed to call
        /// other Throw*Exception variants, since they call this.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was out of range
        /// </param>
        /// <param name="actualValue">
        /// The value of the argument causing the exception
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentOutOfRangeException NewArgumentOutOfRangeException(string paramName, object actualValue)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw new ArgumentNullException("paramName");
            }

            string message = StringUtil.Format(AutomationExceptions.ArgumentOutOfRange, paramName);
            var e = new PSArgumentOutOfRangeException(paramName, actualValue, message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant allows the caller to
        /// specify alternate template text, but only in assembly S.M.A.Core.
        /// </summary>
        /// <param name="paramName">
        /// The name of the parameter whose argument value was invalid
        /// </param>
        /// <param name="actualValue">
        /// The value of the argument causing the exception
        /// </param>
        /// <param name="resourceString">
        /// The template string for this error
        /// </param>
        /// <param name="args">
        /// Objects corresponding to {0}, {1}, etc. in the resource string
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        internal static PSArgumentOutOfRangeException NewArgumentOutOfRangeException(
            string paramName, object actualValue, string resourceString, params object[] args)
        {
            if (string.IsNullOrEmpty(paramName))
            {
                throw NewArgumentNullException("paramName");
            }

            if (string.IsNullOrEmpty(resourceString))
            {
                throw NewArgumentNullException("resourceString");
            }

            string message = StringUtil.Format(resourceString, args);
            var e = new PSArgumentOutOfRangeException(paramName, actualValue, message);

            return e;
        }

        /// <summary>
        /// Traces the Message and StackTrace properties of the exception
        /// and returns the new exception. This variant uses the default
        /// ObjectDisposedException template text. This is not allowed to call
        /// other Throw*Exception variants, since they call this.
        /// </summary>
        /// <param name="objectName">
        /// The name of the disposed object
        /// </param>
        /// <returns>Exception instance ready to throw.</returns>
        /// <remarks>
        /// Note that the parameter is the object name and not the message.
        /// </remarks>
        internal static PSObjectDisposedException NewObjectDisposedException(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                throw NewArgumentNullException("objectName");
            }

            string message = StringUtil.Format(AutomationExceptions.ObjectDisposed, objectName);
            var e = new PSObjectDisposedException(objectName, message);

            return e;
        }

        #endregion TraceFlags.New*Exception methods/helpers
    }
}

