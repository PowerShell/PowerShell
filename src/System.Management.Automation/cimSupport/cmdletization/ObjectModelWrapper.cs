// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Cmdletization
{
    /// <summary>
    /// ObjectModelWrapper integrates OM-specific operations into generic cmdletization framework.
    /// For example - CimCmdletAdapter knows how to invoke a static method "Foo" in the CIM OM.
    /// </summary>
    /// <typeparam name="TObjectInstance">Type that represents instances of objects from the wrapped object model</typeparam>
    public abstract class CmdletAdapter<TObjectInstance>
        where TObjectInstance : class
    {
        internal void Initialize(PSCmdlet cmdlet, string className, string classVersion, IDictionary<string, string> privateData)
        {
            ArgumentNullException.ThrowIfNull(cmdlet);
            ArgumentException.ThrowIfNullOrEmpty(className);

            // possible and ok to have classVersion==string.Empty
            ArgumentNullException.ThrowIfNull(classVersion);
            ArgumentNullException.ThrowIfNull(privateData);

            _cmdlet = cmdlet;
            _className = className;
            _classVersion = classVersion;
            _privateData = privateData;

            if (this.Cmdlet is PSScriptCmdlet compiledScript)
            {
                compiledScript.StoppingEvent += delegate { this.StopProcessing(); };
                compiledScript.DisposingEvent +=
                        delegate
                        {
                            var disposable = this as IDisposable;
                            disposable?.Dispose();
                        };
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="cmdlet"></param>
        /// <param name="className"></param>
        /// <param name="classVersion"></param>
        /// <param name="moduleVersion"></param>
        /// <param name="privateData"></param>
        public void Initialize(PSCmdlet cmdlet, string className, string classVersion, Version moduleVersion, IDictionary<string, string> privateData)
        {
            _moduleVersion = moduleVersion;

            Initialize(cmdlet, className, classVersion, privateData);
        }

        /// <summary>
        /// When overridden in the derived class, creates a query builder for a given object model.
        /// </summary>
        /// <returns>Query builder for a given object model.</returns>
        public virtual QueryBuilder GetQueryBuilder()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries for object instances in the object model.
        /// </summary>
        /// <param name="query">Query parameters.</param>
        /// <returns>A lazy evaluated collection of object instances.</returns>
        public virtual void ProcessRecord(QueryBuilder query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// When overridden in the derived class, performs initialization of cmdlet execution.
        /// Default implementation in the base class just returns.
        /// </summary>
        public virtual void BeginProcessing()
        {
        }

        /// <summary>
        /// When overridden in the derived class, performs cleanup after cmdlet execution.
        /// Default implementation in the base class just returns.
        /// </summary>
        public virtual void EndProcessing()
        {
        }

        /// <summary>
        /// When overridden in the derived class, interrupts currently
        /// running code within the <see cref="CmdletAdapter&lt;TObjectInstance&gt;"/>.
        /// Default implementation in the base class just returns.
        /// </summary>
        /// <remarks>
        /// The PowerShell engine will call this method on a separate thread
        /// from the pipeline thread where BeginProcessing, EndProcessing
        /// and other methods are normally being executed.
        /// </remarks>
        public virtual void StopProcessing()
        {
        }

        /// <summary>
        /// Invokes an instance method in the object model.
        /// </summary>
        /// <param name="objectInstance">The object on which to invoke the method.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the <paramref name="objectInstance"/> being operated on.</param>
        public virtual void ProcessRecord(TObjectInstance objectInstance, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Combines <see cref="ProcessRecord(QueryBuilder)"/> and <see cref="ProcessRecord(TObjectInstance,Microsoft.PowerShell.Cmdletization.MethodInvocationInfo,bool)"/>.
        /// </summary>
        /// <param name="query">Query parameters.</param>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        /// <param name="passThru"><see langword="true"/> if successful method invocations should emit downstream the object instance being operated on.</param>
        public virtual void ProcessRecord(QueryBuilder query, MethodInvocationInfo methodInvocationInfo, bool passThru)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Invokes a static method in the object model.
        /// </summary>
        /// <param name="methodInvocationInfo">Method invocation details.</param>
        public virtual void ProcessRecord(
            MethodInvocationInfo methodInvocationInfo)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Cmdlet that this ObjectModelWrapper is associated with.
        /// </summary>
        public PSCmdlet Cmdlet
        {
            get
            {
                return _cmdlet;
            }
        }

        private PSCmdlet _cmdlet;

        /// <summary>
        /// Name of the class (from the object model handled by this ObjectModelWrapper) that is wrapped by the currently executing cmdlet.
        /// </summary>
        public string ClassName
        {
            get
            {
                return _className;
            }
        }

        private string _className;

        /// <summary>
        /// Name of the class (from the object model handled by this ObjectModelWrapper) that is wrapped by the currently executing cmdlet.
        /// This value can be <see langword="null"/> (i.e. when ClassVersion attribute is omitted in the ps1xml)
        /// </summary>
        public string ClassVersion
        {
            get
            {
                return _classVersion;
            }
        }

        private string _classVersion;

        /// <summary>
        /// Module version.
        /// </summary>
        public Version ModuleVersion
        {
            get
            {
                return _moduleVersion;
            }
        }

        private Version _moduleVersion;

        /// <summary>
        /// Private data from Cmdlet Definition XML (from &lt;ObjectModelWrapperPrivateData&gt; element)
        /// </summary>
        public IDictionary<string, string> PrivateData
        {
            get
            {
                return _privateData;
            }
        }

        private IDictionary<string, string> _privateData;
    }
}
