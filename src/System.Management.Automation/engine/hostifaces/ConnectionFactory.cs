// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation.Host;
using System.Management.Automation.Tracing;

using Microsoft.PowerShell;
using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Runspaces
{
    /// <summary>
    /// Defines a factory class for creating Runspace objects.
    /// </summary>
    public static class RunspaceFactory
    {
        /// <summary>
        /// Static constructor.
        /// </summary>
        static RunspaceFactory()
        {
            // Set ETW activity Id
            Guid activityId = EtwActivity.GetActivityId();

            if (activityId == Guid.Empty)
            {
                EtwActivity.SetActivityId(EtwActivity.CreateActivityId());
            }
        }

        #region Runspace Factory

        /// <summary>
        /// Creates a runspace using host of type <see cref="DefaultHost"/>.
        /// </summary>
        /// <returns>
        /// A runspace object.
        /// </returns>
        public static Runspace CreateRunspace()
        {
            PSHost host = new DefaultHost(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            return CreateRunspace(host);
        }

        /// <summary>
        /// Creates a runspace using specified host. This runspace is created using the
        /// configuration information from EntryAssembly.
        /// </summary>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <returns>
        /// A runspace object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when host is null.
        /// </exception>
        public static Runspace CreateRunspace(PSHost host)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            return new LocalRunspace(host, InitialSessionState.CreateDefault());
        }

        /// <summary>
        /// Creates a runspace using <see cref="DefaultHost"/>
        /// </summary>
        /// <param name="initialSessionState">
        /// InitialSessionState information for the runspace.
        /// </param>
        /// <returns>
        /// A runspace object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when initialSessionState is null
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        public static Runspace CreateRunspace(InitialSessionState initialSessionState)
        {
            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(initialSessionState));
            }

            PSHost host = new DefaultHost(CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

            return CreateRunspace(host, initialSessionState);
        }

        /// <summary>
        /// Creates a runspace using specified PSHost and InitialSessionState.
        /// </summary>
        /// <param name="host">
        /// Host implementation for runspace.
        /// </param>
        /// <param name="initialSessionState">
        /// InitialSessionState information for the runspace.
        /// </param>
        /// <returns>
        /// A runspace object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when host is null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when initialSessionState is null
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        public static Runspace CreateRunspace(PSHost host, InitialSessionState initialSessionState)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(initialSessionState));
            }

            return new LocalRunspace(host, initialSessionState);
        }

        /// <summary>
        /// Creates a runspace using specified PSHost and InitialSessionState.
        /// </summary>
        /// <param name="host">
        /// Host implementation for runspace.
        /// </param>
        /// <param name="initialSessionState">
        /// InitialSessionState information for the runspace.
        /// </param>
        /// <returns>
        /// A runspace object
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when host is null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when initialSessionState is null
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        internal static Runspace CreateRunspaceFromSessionStateNoClone(PSHost host, InitialSessionState initialSessionState)
        {
            if (host == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(host));
            }

            if (initialSessionState == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(initialSessionState));
            }

            return new LocalRunspace(host, initialSessionState, true);
        }

        #endregion

        #region RunspacePool Factory

        /// <summary>
        /// Creates a RunspacePool with MaxRunspaces 1 and MinRunspaces 1.
        /// </summary>
        public static RunspacePool CreateRunspacePool()
        {
            return CreateRunspacePool(1, 1);
        }

        /// <summary>
        /// Creates a RunspacePool
        /// <paramref name="maxRunspaces"/>
        /// limits the number of Runspaces that can exist in this
        /// pool. The minimum pool size is set to <paramref name="minPoolSoze"/>.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces)
        {
            return CreateRunspacePool(minRunspaces, maxRunspaces,
                new DefaultHost
                (
                    CultureInfo.CurrentCulture,
                    CultureInfo.CurrentUICulture
                ));
        }

        /// <summary>
        /// Creates a RunspacePool using the supplied <paramref name="initialSessionState"/>.
        /// The minimum runspaces size is set to 1. The maximum runspaces size is
        /// set to 1.
        /// </summary>
        /// <param name="initialSessionState">
        /// initialSessionState to use when creating a new
        /// Runspace in the pool.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// InitialSessionState is null.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        public static RunspacePool CreateRunspacePool(InitialSessionState initialSessionState)
        {
            return CreateRunspacePool(1, 1, initialSessionState,
                new DefaultHost
                (
                    CultureInfo.CurrentCulture,
                    CultureInfo.CurrentUICulture
                ));
        }

        /// <summary>
        /// Creates a RunspacePool using the supplied <paramref name="host"/>,
        /// <paramref name="minRunspaces"/> and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="host"/> is null.
        /// </exception>
        /// <returns>
        /// A local runspacepool instance.
        /// </returns>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces, PSHost host)
        {
            return new RunspacePool(minRunspaces, maxRunspaces, host);
        }

        /// <summary>
        /// Creates a RunspacePool using the supplied <paramref name="initialSessionState"/>,
        /// <paramref name="minRunspaces"/> and <paramref name="maxRunspaces"/>
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this pool.
        /// Should be greater than or equal to 1.
        /// </param>
        /// <param name="initialSessionState">
        /// initialSessionState to use when creating a new Runspace in the
        /// pool.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// InitialSessionState is null.
        /// </exception>
        /// <param name="host">
        /// The explicit PSHost implementation.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="initialSessionState"/> is null.
        /// <paramref name="host"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Maximum runspaces is less than 1.
        /// Minimum runspaces is less than 1.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces, int maxRunspaces,
            InitialSessionState initialSessionState, PSHost host)
        {
            return new RunspacePool(minRunspaces,
                maxRunspaces, initialSessionState, host);
        }

        #endregion

        #region RunspacePool - remote Factory

        /// <summary>
        /// Creates a RunspacePool
        /// on the specified remote computer.
        /// <paramref name="maxRunspaces"/>
        /// limits the number of Runspaces that can exist in this
        /// pool. The minimum pool size is set to
        /// <paramref name="minPoolSoze"/>.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspace that should exist in this
        /// pool. Should be greater than 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <param name="connectionInfo">RunspaceConnectionInfo object describing
        /// the remote computer on which this runspace pool needs to be
        /// created</param>
        /// <exception cref="ArgumentException">
        /// Maximum Pool size is less than 1.
        /// Minimum Pool size is less than 1.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// connectionInfo is null</exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces,
                                        int maxRunspaces, RunspaceConnectionInfo connectionInfo)
        {
            return CreateRunspacePool(minRunspaces, maxRunspaces, connectionInfo, null);
        }

        /// <summary>
        /// Creates a RunspacePool
        /// on the specified remote runspace computer.
        /// <paramref name="maxRunspaces"/>
        /// limits the number of Runspaces that can exist in this
        /// pool. The minimum pool size is set to
        /// <paramref name="minPoolSoze"/>.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspace that should exist in this
        /// pool. Should be greater than 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <param name="host">Host associated with this
        /// runspace pool</param>
        /// <param name="connectionInfo">RunspaceConnectionInfo object describing
        /// the remote computer on which this runspace pool needs to be
        /// created</param>
        /// <exception cref="ArgumentException">
        /// Maximum Pool size is less than 1.
        /// Minimum Pool size is less than 1.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// connectionInfo is null</exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces,
            int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host)
        {
            return CreateRunspacePool(minRunspaces, maxRunspaces, connectionInfo, host, null);
        }

        /// <summary>
        /// Creates a RunspacePool
        /// on the specified remote runspace computer.
        /// <paramref name="maxRunspaces"/>
        /// limits the number of Runspaces that can exist in this
        /// pool. The minimum pool size is set to
        /// <paramref name="minPoolSoze"/>.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspace that should exist in this
        /// pool. Should be greater than 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        ///
        /// If <paramref name="typeTable"/> is null no custom serialization/deserialization
        /// can be done. Default PowerShell behavior will be used in this case.
        /// </param>
        /// <param name="host">Host associated with this
        /// runspace pool</param>
        /// <param name="connectionInfo">RunspaceConnectionInfo object describing
        /// the remote computer on which this runspace pool needs to be
        /// created</param>
        /// <exception cref="ArgumentException">
        /// Maximum Pool size is less than 1.
        /// Minimum Pool size is less than 1.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// connectionInfo is null</exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces,
            int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            return CreateRunspacePool(minRunspaces, maxRunspaces, connectionInfo, host, typeTable, null);
        }

        /// <summary>
        /// Creates a RunspacePool
        /// on the specified remote runspace computer.
        /// <paramref name="maxRunspaces"/>
        /// limits the number of Runspaces that can exist in this
        /// pool. The minimum pool size is set to
        /// <paramref name="minPoolSoze"/>.
        /// </summary>
        /// <param name="minRunspaces">
        /// The minimum number of Runspace that should exist in this
        /// pool. Should be greater than 1.
        /// </param>
        /// <param name="maxRunspaces">
        /// The maximum number of Runspaces that can exist in this
        /// pool. Should be greater than or equal to 1.
        /// </param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        ///
        /// If <paramref name="typeTable"/> is null no custom serialization/deserialization
        /// can be done. Default PowerShell behavior will be used in this case.
        /// </param>
        /// <param name="host">Host associated with this
        /// runspace pool</param>
        /// <param name="applicationArguments">
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        /// <param name="connectionInfo">RunspaceConnectionInfo object describing
        /// the remote computer on which this runspace pool needs to be
        /// created</param>
        /// <exception cref="ArgumentException">
        /// Maximum Pool size is less than 1.
        /// Minimum Pool size is less than 1.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// connectionInfo is null</exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspaces")]
        public static RunspacePool CreateRunspacePool(int minRunspaces,
            int maxRunspaces, RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments)
        {
            if (connectionInfo is not WSManConnectionInfo &&
                connectionInfo is not NewProcessConnectionInfo &&
                connectionInfo is not NamedPipeConnectionInfo &&
                connectionInfo is not VMConnectionInfo &&
                connectionInfo is not ContainerConnectionInfo)
            {
                throw new NotSupportedException();
            }

            if (connectionInfo is WSManConnectionInfo)
            {
                RemotingCommandUtil.CheckHostRemotingPrerequisites();
            }

            return new RunspacePool(minRunspaces, maxRunspaces, typeTable, host, applicationArguments, connectionInfo);
        }

        #endregion RunspacePool - remote Factory

        #region Runspace - Remote Factory

        /// <summary>
        /// Creates a remote Runspace.
        /// </summary>
        /// <param name="connectionInfo">It defines connection path to a remote runspace that needs to be created.</param>
        /// <param name="host">The explicit PSHost implementation.</param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        ///
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <returns>A remote Runspace.</returns>
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable)
        {
            return CreateRunspace(connectionInfo, host, typeTable, null, null);
        }

        /// <summary>
        /// Creates a remote Runspace.
        /// </summary>
        /// <param name="connectionInfo">It defines connection path to a remote runspace that needs to be created.</param>
        /// <param name="host">The explicit PSHost implementation.</param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        ///
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <param name="applicationArguments">
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        /// <returns>A remote Runspace.</returns>
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments)
        {
            return CreateRunspace(connectionInfo, host, typeTable, applicationArguments, null);
        }

        /// <summary>
        /// Creates a remote Runspace.
        /// </summary>
        /// <param name="connectionInfo">It defines connection path to a remote runspace that needs to be created.</param>
        /// <param name="host">The explicit PSHost implementation.</param>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        ///
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <param name="applicationArguments">
        /// Application arguments the server can see in <see cref="System.Management.Automation.Remoting.PSSenderInfo.ApplicationArguments"/>
        /// </param>
        /// <param name="name">Name for remote runspace.</param>
        /// <returns>A remote Runspace.</returns>
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo, PSHost host, TypeTable typeTable, PSPrimitiveDictionary applicationArguments, string name)
        {
            if (connectionInfo is WSManConnectionInfo)
            {
                RemotingCommandUtil.CheckHostRemotingPrerequisites();
            }

            return new RemoteRunspace(typeTable, connectionInfo, host, applicationArguments, name);
        }

        /// <summary>
        /// Creates a remote Runspace.
        /// </summary>
        /// <param name="host">The explicit PSHost implementation.</param>
        /// <param name="connectionInfo">It defines connection path to a remote runspace that needs to be created.</param>
        /// <returns>A remote Runspace.</returns>
        public static Runspace CreateRunspace(PSHost host, RunspaceConnectionInfo connectionInfo)
        {
            return CreateRunspace(connectionInfo, host, null);
        }

        /// <summary>
        /// Creates a remote Runspace.
        /// </summary>
        /// <param name="connectionInfo">It defines connection path to a remote runspace that needs to be created.</param>
        /// <returns>A remote Runspace.</returns>
        public static Runspace CreateRunspace(RunspaceConnectionInfo connectionInfo)
        {
            return CreateRunspace(null, connectionInfo);
        }

        #endregion Runspace - Remote Factory

        #region V3 Extensions

        /// <summary>
        /// Creates an out-of-process remote Runspace.
        /// </summary>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        ///
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <returns>An out-of-process remote Runspace.</returns>
        public static Runspace CreateOutOfProcessRunspace(TypeTable typeTable)
        {
            NewProcessConnectionInfo connectionInfo = new NewProcessConnectionInfo(null);

            return CreateRunspace(connectionInfo, null, typeTable);
        }

        /// <summary>
        /// Creates an out-of-process remote Runspace.
        /// </summary>
        /// <param name="typeTable">
        /// The TypeTable to use while deserializing/serializing remote objects.
        /// TypeTable has the following information used by serializer:
        ///   1. SerializationMethod
        ///   2. SerializationDepth
        ///   3. SpecificSerializationProperties
        ///
        /// TypeTable has the following information used by deserializer:
        ///   1. TargetTypeForDeserialization
        ///   2. TypeConverter
        /// </param>
        /// <param name="processInstance">It represents a PowerShell process that is used for an out-of-process remote Runspace</param>
        /// <returns>An out-of-process remote Runspace.</returns>
        public static Runspace CreateOutOfProcessRunspace(TypeTable typeTable, PowerShellProcessInstance processInstance)
        {
            NewProcessConnectionInfo connectionInfo = new NewProcessConnectionInfo(null) { Process = processInstance };

            return CreateRunspace(connectionInfo, null, typeTable);
        }

        #endregion V3 Extensions
    }
}
