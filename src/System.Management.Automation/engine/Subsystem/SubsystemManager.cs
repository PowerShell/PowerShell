// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using System.Management.Automation.Subsystem.DSC;
using System.Management.Automation.Subsystem.Feedback;
using System.Management.Automation.Subsystem.Prediction;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Class used to manage subsystems.
    /// </summary>
    public static class SubsystemManager
    {
        private static readonly ReadOnlyCollection<SubsystemInfo> s_subsystems;
        private static readonly ReadOnlyDictionary<Type, SubsystemInfo> s_subSystemTypeMap;
        private static readonly ReadOnlyDictionary<SubsystemKind, SubsystemInfo> s_subSystemKindMap;

        static SubsystemManager()
        {
            var subsystems = new SubsystemInfo[]
            {
                SubsystemInfo.Create<ICommandPredictor>(
                    SubsystemKind.CommandPredictor,
                    allowUnregistration: true,
                    allowMultipleRegistration: true),

                SubsystemInfo.Create<ICrossPlatformDsc>(
                    SubsystemKind.CrossPlatformDsc,
                    allowUnregistration: true,
                    allowMultipleRegistration: false),

                SubsystemInfo.Create<IFeedbackProvider>(
                    SubsystemKind.FeedbackProvider,
                    allowUnregistration: true,
                    allowMultipleRegistration: true),
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

            // Register built-in suggestion providers.
            RegisterSubsystem(SubsystemKind.FeedbackProvider, new GeneralCommandErrorFeedback());
        }

        #region internal - Retrieve subsystem proxy object

        /// <summary>
        /// Get the proxy object registered for a specific subsystem.
        /// Return null when the given subsystem is not registered.
        /// </summary>
        /// <remarks>
        /// Design point:
        /// The implementation proxy object is not supposed to expose to users.
        /// Users shouldn't depend on a implementation proxy object directly, but instead should depend on PowerShell APIs.
        /// <para/>
        /// Example: if a user want to use prediction functionality, he/she should use the PowerShell prediction API instead of
        /// directly interacting with the implementation proxy object of `IPrediction`.
        /// </remarks>
        /// <typeparam name="TConcreteSubsystem">The concrete subsystem base type.</typeparam>
        /// <returns>The most recently registered implementation object of the concrete subsystem.</returns>
        internal static TConcreteSubsystem? GetSubsystem<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            if (s_subSystemTypeMap.TryGetValue(typeof(TConcreteSubsystem), out SubsystemInfo? subsystemInfo))
            {
                var subsystemInfoImpl = (SubsystemInfoImpl<TConcreteSubsystem>)subsystemInfo;
                return subsystemInfoImpl.GetImplementation();
            }

            throw new ArgumentException(
                StringUtil.Format(
                    SubsystemStrings.SubsystemTypeUnknown,
                    typeof(TConcreteSubsystem).FullName));
        }

        /// <summary>
        /// Get all the proxy objects registered for a specific subsystem.
        /// Return an empty collection when the given subsystem is not registered.
        /// </summary>
        /// <typeparam name="TConcreteSubsystem">The concrete subsystem base type.</typeparam>
        /// <returns>A readonly collection of all implementation objects registered for the concrete subsystem.</returns>
        internal static ReadOnlyCollection<TConcreteSubsystem> GetSubsystems<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            if (s_subSystemTypeMap.TryGetValue(typeof(TConcreteSubsystem), out SubsystemInfo? subsystemInfo))
            {
                var subsystemInfoImpl = (SubsystemInfoImpl<TConcreteSubsystem>)subsystemInfo;
                return subsystemInfoImpl.GetAllImplementations();
            }

            throw new ArgumentException(
                StringUtil.Format(
                    SubsystemStrings.SubsystemTypeUnknown,
                    typeof(TConcreteSubsystem).FullName));
        }

        #endregion

        #region public - Subsystem metadata

        /// <summary>
        /// Get the information about all subsystems.
        /// </summary>
        /// <returns>A readonly collection of all <see cref="SubsystemInfo"/> objects.</returns>
        public static ReadOnlyCollection<SubsystemInfo> GetAllSubsystemInfo()
        {
            return s_subsystems;
        }

        /// <summary>
        /// Get the information about a subsystem by the subsystem type.
        /// </summary>
        /// <param name="subsystemType">The base type of a specific concrete subsystem.</param>
        /// <returns>The <see cref="SubsystemInfo"/> object that represents the concrete subsystem.</returns>
        public static SubsystemInfo GetSubsystemInfo(Type subsystemType)
        {
            ArgumentNullException.ThrowIfNull(subsystemType);

            if (s_subSystemTypeMap.TryGetValue(subsystemType, out SubsystemInfo? subsystemInfo))
            {
                return subsystemInfo;
            }

            throw new ArgumentException(
                subsystemType == typeof(ISubsystem)
                    ? SubsystemStrings.MustUseConcreteSubsystemType
                    : StringUtil.Format(
                        SubsystemStrings.SubsystemTypeUnknown,
                        subsystemType.FullName),
                nameof(subsystemType));
        }

        /// <summary>
        /// Get the information about a subsystem by the subsystem kind.
        /// </summary>
        /// <param name="kind">A specific <see cref="SubsystemKind"/>.</param>
        /// <returns>The <see cref="SubsystemInfo"/> object that represents the concrete subsystem.</returns>
        public static SubsystemInfo GetSubsystemInfo(SubsystemKind kind)
        {
            if (s_subSystemKindMap.TryGetValue(kind, out SubsystemInfo? subsystemInfo))
            {
                return subsystemInfo;
            }

            throw new ArgumentException(
                StringUtil.Format(
                    SubsystemStrings.SubsystemKindUnknown,
                    kind.ToString()),
                nameof(kind));
        }

        #endregion

        #region public - Subsystem registration

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        /// <typeparam name="TConcreteSubsystem">The concrete subsystem base type.</typeparam>
        /// <typeparam name="TImplementation">The implementation type of that concrete subsystem.</typeparam>
        /// <param name="proxy">An instance of the implementation.</param>
        public static void RegisterSubsystem<TConcreteSubsystem, TImplementation>(TImplementation proxy)
            where TConcreteSubsystem : class, ISubsystem
            where TImplementation : class, TConcreteSubsystem
        {
            ArgumentNullException.ThrowIfNull(proxy);

            RegisterSubsystem(GetSubsystemInfo(typeof(TConcreteSubsystem)), proxy);
        }

        /// <summary>
        /// Register an implementation for a subsystem.
        /// </summary>
        /// <param name="kind">The target <see cref="SubsystemKind"/> of the registration.</param>
        /// <param name="proxy">An instance of the implementation.</param>
        public static void RegisterSubsystem(SubsystemKind kind, ISubsystem proxy)
        {
            ArgumentNullException.ThrowIfNull(proxy);

            SubsystemInfo info = GetSubsystemInfo(kind);
            if (!info.SubsystemType.IsAssignableFrom(proxy.GetType()))
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.ConcreteSubsystemNotImplemented,
                        kind.ToString(),
                        info.SubsystemType.Name),
                    nameof(proxy));
            }

            RegisterSubsystem(info, proxy);
        }

        private static void RegisterSubsystem(SubsystemInfo subsystemInfo, ISubsystem proxy)
        {
            if (proxy.Id == Guid.Empty)
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.EmptyImplementationId,
                        subsystemInfo.Kind.ToString()),
                    nameof(proxy));
            }

            if (string.IsNullOrEmpty(proxy.Name))
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.NullOrEmptyImplementationName,
                        subsystemInfo.Kind.ToString()),
                    nameof(proxy));
            }

            if (string.IsNullOrEmpty(proxy.Description))
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.NullOrEmptyImplementationDescription,
                        subsystemInfo.Kind.ToString()),
                    nameof(proxy));
            }

            if (subsystemInfo.RequiredCmdlets.Count > 0 || subsystemInfo.RequiredFunctions.Count > 0)
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
        /// <typeparam name="TConcreteSubsystem">The base type of the target concrete subsystem of the un-registration.</typeparam>
        /// <param name="id">The Id of the implementation to be unregistered.</param>
        public static void UnregisterSubsystem<TConcreteSubsystem>(Guid id)
            where TConcreteSubsystem : class, ISubsystem
        {
            UnregisterSubsystem(GetSubsystemInfo(typeof(TConcreteSubsystem)), id);
        }

        /// <summary>
        /// Subsystem unregistration.
        /// Throw 'InvalidOperationException' when called for subsystems that cannot be unregistered.
        /// </summary>
        /// <param name="kind">The target <see cref="SubsystemKind"/> of the un-registration.</param>
        /// <param name="id">The Id of the implementation to be unregistered.</param>
        public static void UnregisterSubsystem(SubsystemKind kind, Guid id)
        {
            UnregisterSubsystem(GetSubsystemInfo(kind), id);
        }

        private static void UnregisterSubsystem(SubsystemInfo subsystemInfo, Guid id)
        {
            if (subsystemInfo.RequiredCmdlets.Count > 0 || subsystemInfo.RequiredFunctions.Count > 0)
            {
                throw new NotSupportedException("NotSupported yet: unregister subsystem that introduced new cmdlets/functions.");
            }

            ISubsystem impl = subsystemInfo.UnregisterImplementation(id);
            if (impl is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    // It's OK to ignore all exceptions when disposing the object.
                }
            }
        }

        #endregion
    }
}
