// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Class used to manage subsystems.
    /// </summary>
    public class SubsystemManager
    {
        private static readonly ReadOnlyCollection<SubsystemInfo> s_subsystems;
        private static readonly ReadOnlyDictionary<Type, SubsystemInfo> s_subSystemTypeMap;
        private static readonly ReadOnlyDictionary<SubsystemKind, SubsystemInfo> s_subSystemKindMap;

        static SubsystemManager()
        {
            var subsystems = new SubsystemInfo[] {
                SubsystemInfo.Create<IPredictor>(SubsystemKind.CommandPredictor, allowUnregistration: true, allowMultipleRegistration: true),
            };

            var subSystemTypeMap = new Dictionary<Type, SubsystemInfo>(subsystems.Length);
            var subSystemKindMap = new Dictionary<SubsystemKind, SubsystemInfo>(subsystems.Length);

            foreach (var subsystem in subsystems)
            {
                subSystemTypeMap.Add(subsystem.SubsystemType, subsystem);
                subSystemKindMap.Add(subsystem.Kind, subsystem);
            }

            s_subsystems = new ReadOnlyCollection<SubsystemInfo>(subsystems);
            s_subSystemTypeMap = new ReadOnlyDictionary<Type, SubsystemInfo>(subSystemTypeMap);
            s_subSystemKindMap = new ReadOnlyDictionary<SubsystemKind, SubsystemInfo>(subSystemKindMap);
        }

        #region internal - Retrieve subsystem proxy object

        /// <summary>
        /// Get the proxy object registered for a specific subsystem.
        /// Return null when the given subsystem is not registered.
        /// </summary>
        /// <remarks>
        /// Design point:
        /// The implemnentation proxy object is not supposed to expose to users.
        /// Users shouldn't depend on a implementation proxy object directly, but instead should depend on PowerShell APIs.
        ///
        /// Example: if a user want to use prediction functionality, he/she should use the PowerShell prediction API instead of
        /// directly interacting with the implementation proxy object of `IPrediction`.
        /// </remarks>
        internal static TConcreteSubsystem GetSubsystem<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            if (s_subSystemTypeMap.TryGetValue(typeof(TConcreteSubsystem), out SubsystemInfo subsystemInfo))
            {
                var subsystemInfoImpl = (SubsystemInfoImpl<TConcreteSubsystem>) subsystemInfo;
                return subsystemInfoImpl.GetImplementation();
            }

            throw new ArgumentException("The specified subsystem type '{0}' is unknown.");
        }

        /// <summary>
        /// Get all the proxy objects registered for a specific subsystem.
        /// Return an empty collection when the given subsystem is not registered.
        /// </summary>
        internal static ReadOnlyCollection<TConcreteSubsystem> GetSubsystems<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            if (s_subSystemTypeMap.TryGetValue(typeof(TConcreteSubsystem), out SubsystemInfo subsystemInfo))
            {
                var subsystemInfoImpl = (SubsystemInfoImpl<TConcreteSubsystem>) subsystemInfo;
                return subsystemInfoImpl.GetAllImplementations();
            }

            throw new ArgumentException("The specified subsystem type '{0}' is unknown.");
        }

        #endregion

        #region public - Subsystem metadata

        /// <summary>
        /// Get the information about all subsystems.
        /// </summary>
        public static ReadOnlyCollection<SubsystemInfo> GetAllSubsystemInfo()
        {
            return s_subsystems;
        }

        /// <summary>
        /// Get the information about a subsystem by the subsystem type.
        /// </summary>
        public static SubsystemInfo GetSubsystemInfo(Type subsystemType)
        {
            if (s_subSystemTypeMap.TryGetValue(subsystemType, out SubsystemInfo subsystemInfo))
            {
                return subsystemInfo;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "The specified subsystem type '{0}' is unknown.",
                    subsystemType.FullName));
        }

        /// <summary>
        /// Get the information about a subsystem by the subsystem kind.
        /// </summary>
        public static SubsystemInfo GetSubsystemInfo(SubsystemKind kind)
        {
            if (s_subSystemKindMap.TryGetValue(kind, out SubsystemInfo subsystemInfo))
            {
                return subsystemInfo;
            }

            throw new ArgumentException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    "The specified subsystem kind '{0}' is unknown.",
                    kind.ToString()));
        }

        #endregion

        #region public - Subsystem registration

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        public static void RegisterSubsystem<TConcreteSubsystem, TImplementation>(TImplementation proxy)
            where TConcreteSubsystem : class, ISubsystem
            where TImplementation : class, TConcreteSubsystem
        {
            if (proxy == null)
            {
                throw new ArgumentNullException(nameof(proxy));
            }

            RegisterSubsystem(GetSubsystemInfo(typeof(TConcreteSubsystem)), proxy);
        }

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        public static void RegisterSubsystem(SubsystemKind kind, ISubsystem proxy)
        {
            if (proxy == null)
            {
                throw new ArgumentNullException(nameof(proxy));
            }

            if (kind != proxy.Kind)
            {
                throw new ArgumentException("Invalid subsystem implementation.", nameof(proxy));
            }

            RegisterSubsystem(GetSubsystemInfo(kind), proxy);
        }

        private static void RegisterSubsystem(SubsystemInfo subsystemInfo, ISubsystem proxy)
        {
            if (subsystemInfo.RequiredCmdlets.Any() || subsystemInfo.RequiredFunctions.Any())
            {
                // Process 'proxy.CmdletImplementationAssembly' and 'proxy.FunctionsToDefine'
                // Functions are added to global scope.
                // Cmdlets are loaded in a way like a snapin, making the 'Source' of the cmdlets to be 'Microsoft.PowerShell.Core'.
                //
                // For example, let's say the Job adapter is made a subsystem, then all `*-Job` cmdlets will be moved out of S.M.A
                // into a subsystem implementation DLL. After registration, all `*-Job` cmdlets should be back in the
                // 'Microsoft.PowerShell.Core' namespace to keep backward compatibility.
                //
                // Both cmdlets and functions are added to the default InitialSessionState used for creating a new Runspace,
                // so the subsystem works for all subsequent new runspaces after it's registered.
                // Take the Job adapter subsystem as an instance again, so when creating another Runspace after the registration,
                // all '*-Job' cmdlets should be available in the 'Microsoft.PowerShell.Core' namespace by default.
            }

            subsystemInfo.RegisterImplementation(proxy);
        }

        #endregion

        #region public - Subsystem unregistration

        /// <summary>
        /// Subsystem unregistration.
        /// Throw 'InvalidOperationException' when called for subsystems that cannot be unregistered.
        /// </summary>
        public static void UnregisterSubsystem<TConcreteSubsystem>(Guid id)
            where TConcreteSubsystem : class, ISubsystem
        {
            UnregisterSubsystem(GetSubsystemInfo(typeof(TConcreteSubsystem)), id);
        }

        /// <summary>
        /// Subsystem unregistration.
        /// Throw 'InvalidOperationException' when called for subsystems that cannot be unregistered.
        /// </summary>
        public static void UnregisterSubsystem(SubsystemKind kind, Guid id)
        {
            UnregisterSubsystem(GetSubsystemInfo(kind), id);
        }

        private static void UnregisterSubsystem(SubsystemInfo subsystemInfo, Guid id)
        {
            if (subsystemInfo.RequiredCmdlets.Any() || subsystemInfo.RequiredFunctions.Any())
            {
                throw new NotSupportedException("NotSupported yet: unregister subsystem that introduced new cmdlets/functions.");
            }

            ISubsystem impl = subsystemInfo.UnregisterImplementation(id);
            if (impl is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        #endregion
    }
}
