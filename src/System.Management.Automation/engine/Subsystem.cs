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
    /// Define the kinds of subsystems.
    /// </summary>
    public enum SubsystemKind
    {
        /// <summary>
        /// Predictive suggestion.
        /// </summary>
        Prediction = 1,
    }

    /// <summary>
    /// Define the base interface to implement a subsystem.
    /// The API contracts for specific subsystems are defined within the specific interfaces/abstract classes that implements this interface.
    /// </summary>
    /// <remarks>
    /// There are two purposes to have the internal member `Kind` declared in 'ISubsystem':
    /// 1. Make the mapping from an `ISubsystem` implementation to the `SubsystemKind` easy;
    /// 2. Make sure a user cannot directly implement 'ISubsystem', but have to derive from one of the concrete subsystem interface or abstract class.
    ///
    /// The internal member needs to have a default implementation defined by the specific subsystem interfaces or abstract class,
    /// because it should be the same for a specific kind of subsystem.
    /// </remarks>
    public interface ISubsystem
    {
        /// <summary>
        /// The unique identifier for a subsystem implementation.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// The name of a subsystem implementation.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The description of a subsystem implementation.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// A dictionary that contains the functions to define at the global scope.
        /// Key: function name; Value: function script.
        /// </summary>
        ReadOnlyDictionary<string, string> FunctionsToDefine { get; }

        /// <summary>
        /// Subsystem kind.
        /// </summary>
        internal SubsystemKind Kind { get; }
    }

    /// <summary>
    /// Class used to represent the metadata and state of a subsystem.
    /// </summary>
    public sealed class SubsystemInfo
    {
        #region "Metadata of a Subsystem"

        /// <summary>
        /// The kind of a concrete subsystem.
        /// </summary>
        public SubsystemKind Kind { get; private set; }

        /// <summary>
        /// The type of a concrete subsystem.
        /// </summary>
        public Type SubsystemType { get; private set; }

        /// <summary>
        /// Indicate whether the subsystem allows to unregister an implementation.
        /// </summary>
        public bool AllowUnregistration { get; private set; }

        /// <summary>
        /// Indicate whether the subsystem allows to have multiple implementations registered.
        /// </summary>
        public bool AllowMultipleRegistration { get; private set; }

        /// <summary>
        /// Gets the names of the required cmdlets that have to be implemented by the subsystem implementation.
        /// </summary>
        public ReadOnlyCollection<string> RequiredCmdlets { get; private set; }

        /// <summary>
        /// Gets the names of the required functions that have to be implemented by the subsystem implementation.
        /// </summary>
        public ReadOnlyCollection<string> RequiredFunctions { get; private set; }

        // /// <summary>
        // /// A subsystem may depend on or more other subsystems.
        // /// Maybe add a 'DependsOn' member?
        // /// This can be validated when registering a subsystem implementation,
        // /// to make sure its prerequisites have already been registered.
        // /// </summary>
        // SubsystemKind[] DependsOn { get; }

        #endregion

        #region "State of a Subsystem"

        private readonly object _syncObj;
        private ReadOnlyCollection<ISubsystem> _registeredImpls;
        private ReadOnlyCollection<ImplementationInfo> _cachedImplInfos;

        /// <summary>
        /// Indicate whether there is any implementation registered to the subsystem.
        /// </summary>
        public bool IsRegistered => _registeredImpls.Count > 0;

        /// <summary>
        /// Get the information about the registered implementations.
        /// </summary>
        public ReadOnlyCollection<ImplementationInfo> Implementations => _cachedImplInfos;

        #endregion

        private SubsystemInfo()
        {
            _syncObj = new object();
            _registeredImpls = Utils.EmptyReadOnlyCollection<ISubsystem>();
            _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();
        }

        #region "Static factory overloads"

        internal static SubsystemInfo Create<TSubsystem>(SubsystemKind kind)
            where TSubsystem : class, ISubsystem
        {
            var empty = Utils.EmptyReadOnlyCollection<string>();
            return Create<TSubsystem>(kind, allowUnregistration: false, allowMultipleRegistration: false, empty, empty);
        }

        internal static SubsystemInfo Create<TSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration) where TSubsystem : class, ISubsystem
        {
            var empty = Utils.EmptyReadOnlyCollection<string>();
            return Create<TSubsystem>(kind, allowUnregistration, allowMultipleRegistration, empty, empty);
        }

        internal static SubsystemInfo Create<TSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration,
            ReadOnlyCollection<string> requiredCmdlets,
            ReadOnlyCollection<string> requiredFunctions) where TSubsystem : class, ISubsystem
        {
            return new SubsystemInfo() {
                SubsystemType = typeof(TSubsystem),
                Kind = kind,
                AllowUnregistration = allowUnregistration,
                RequiredCmdlets = requiredCmdlets,
                RequiredFunctions = requiredFunctions,
            };
        }

        #endregion

        #region "Manage implementations"

        internal void AddImplementation(ISubsystem impl)
        {
            if (!SubsystemType.IsAssignableFrom(impl.GetType()))
            {
                throw new ArgumentException("An implementation of the subsystem '{0}' must derive from or implement the type '{1}'.");
            }

            lock (_syncObj)
            {
                if (_registeredImpls.Count == 0)
                {
                    _registeredImpls = new ReadOnlyCollection<ISubsystem>(new[] { impl });
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(new[] { new ImplementationInfo(impl) });
                    return;
                }

                if (!AllowMultipleRegistration)
                {
                    throw new InvalidOperationException("The subsystem '{0}' does not allow more than one implementation registration.");
                }

                bool targetExists = false;
                foreach (var item in _registeredImpls)
                {
                    if (item.Id == impl.Id)
                    {
                        targetExists = true;
                        break;
                    }
                }

                if (targetExists)
                {
                    throw new InvalidOperationException("The implementation with ID was already registered.");
                }

                var list = new List<ISubsystem>(_registeredImpls.Count + 1);
                list.AddRange(_registeredImpls);
                list.Add(impl);

                _registeredImpls = new ReadOnlyCollection<ISubsystem>(list);
                _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(list.ConvertAll(s => new ImplementationInfo(s)));
            }
        }

        internal ISubsystem RemoveImplementation(Guid id)
        {
            if (!AllowUnregistration)
            {
                throw new InvalidOperationException("The subsystem '{0}' does not allow unregistration of an implementation.");
            }

            lock (_syncObj)
            {
                if (_registeredImpls.Count == 0)
                {
                    throw new InvalidOperationException("No implementation was registered to the subsystem '{0}'.");
                }

                int index = -1;
                for (int i = 0; i < _registeredImpls.Count; i++)
                {
                    if (_registeredImpls[i].Id == id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    throw new InvalidOperationException($"Cannot find a registered implementation with the ID '$id'");
                }

                ISubsystem target = _registeredImpls[index];
                if (_registeredImpls.Count == 1)
                {
                    _registeredImpls = Utils.EmptyReadOnlyCollection<ISubsystem>();
                    _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();
                }
                else
                {
                    var list = new List<ISubsystem>(_registeredImpls.Count - 1);
                    for (int i = 0; i < _registeredImpls.Count; i++)
                    {
                        if (index == i)
                        {
                            continue;
                        }

                        list.Add(_registeredImpls[i]);
                    }

                    _registeredImpls = new ReadOnlyCollection<ISubsystem>(list);
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(list.ConvertAll(s => new ImplementationInfo(s)));
                }

                return target;
            }
        }

        internal ISubsystem GetImplementation()
        {
            var localRef = _registeredImpls;
            return localRef.Count > 0 ? localRef[localRef.Count - 1] : null;
        }

        internal ReadOnlyCollection<ISubsystem> GetAllImplementations()
        {
            return _registeredImpls;
        }

        #endregion

        #region "ImplementationInfo"

        /// <summary>
        /// Information about an implementation of a subsystem.
        /// </summary>
        public class ImplementationInfo
        {
            internal ImplementationInfo(ISubsystem implementation)
            {
                Id = implementation.Id;
                Name = implementation.Name;
                Description = implementation.Description;
                ImplementationType = implementation.GetType();
            }

            /// <summary>
            /// The unique identifier for a subsystem implementation.
            /// </summary>
            public readonly Guid Id;

            /// <summary>
            /// The name of a subsystem implementation.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The description of a subsystem implementation.
            /// </summary>
            public readonly string Description;

            /// <summary>
            /// The type of the implementation.
            /// </summary>
            public readonly Type ImplementationType;
        }

        #endregion
    }

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
                SubsystemInfo.Create<IPrediction>(
                    SubsystemKind.Prediction,
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
                return (TConcreteSubsystem) subsystemInfo.GetImplementation();
            }

            // Return null when the requested subsystem is not registered.
            return null;
        }

        /// <summary>
        /// Get all the proxy objects registered for a specific subsystem.
        /// Return an empty collection when the given subsystem is not registered.
        /// </summary>
        internal static IEnumerable<TConcreteSubsystem> GetSubsystems<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            if (s_subSystemTypeMap.TryGetValue(typeof(TConcreteSubsystem), out SubsystemInfo subsystemInfo))
            {
                return subsystemInfo.GetImplementation();
            }

            // Return null when the requested subsystem is not registered.
            return null;
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
        /// Get the information about a subsystem by the subsystem type.
        /// </summary>
        public static SubsystemInfo GetSubsystemInfo<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            return GetSubsystemInfo(typeof(TConcreteSubsystem));
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
            var subsystemInfo = GetSubsystemInfo<TConcreteSubsystem>();

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

            // The proxy object needs to be properly preserved so it can be returned by 'GetSubsystem' and 'GetRequiredSubsystem'.
        }

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        public static bool TryRegisterSubsystem<TConcreteSubsystem, TImplementation>(TImplementation proxy)
            where TConcreteSubsystem : class, ISubsystem
            where TImplementation : class, TConcreteSubsystem
        {
            try
            {
                RegisterSubsystem<TConcreteSubsystem, TImplementation>(proxy);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        public static void RegisterSubsystem(SubsystemKind kind, ISubsystem proxy)
        {
            if (kind != proxy.Kind)
            {
                throw new ArgumentException("Invalid subsystem implementation.", nameof(proxy));
            }

            var subsystemInfo = GetSubsystemInfo(kind);

            if (subsystemInfo.RequiredCmdlets.Any() || subsystemInfo.RequiredFunctions.Any())
            {

            }

            // Save the proxy object properly.
        }

        /// <summary>
        /// Subsystem registration.
        /// </summary>
        public static bool TryRegisterSubsystem(SubsystemKind kind, ISubsystem proxy)
        {
            try
            {
                RegisterSubsystem(kind, proxy);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region public - Subsystem unregistration

        /// <summary>
        /// Subsystem unregistration.
        /// Throw 'InvalidOperationException' when called for subsystems that cannot be unregistered.
        /// </summary>
        public static void UnregisterSubsystem<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            var subsystemInfo = GetSubsystemInfo<TConcreteSubsystem>();

            if (!subsystemInfo.AllowUnregistration)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "The subsystem '{0}' cannot be unregistered",
                        subsystemInfo.Kind.ToString()));
            }

            if (subsystemInfo.RequiredCmdlets.Any() || subsystemInfo.RequiredFunctions.Any())
            {
                throw new NotSupportedException("NotSupported yet: unregister subsystem that introduced new cmdlets/functions.");
            }

            // unregistration goes here
        }

        /// <summary>
        /// Subsystem unregistration.
        /// Return false when called for subsystems that cannot be unregistered.
        /// </summary>
        public static bool TryUnregisterSubsystem<TConcreteSubsystem>()
            where TConcreteSubsystem : class, ISubsystem
        {
            try
            {
                UnregisterSubsystem<TConcreteSubsystem>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Subsystem unregistration.
        /// Throw 'InvalidOperationException' when called for subsystems that cannot be unregistered.
        /// </summary>
        public static void UnregisterSubsystem(SubsystemKind kind)
        {
            var subsystemInfo = GetSubsystemInfo(kind);

            if (!subsystemInfo.AllowUnregistration)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        "The subsystem '{0}' cannot be unregistered",
                        subsystemInfo.Kind.ToString()));
            }

            if (subsystemInfo.RequiredCmdlets.Any() || subsystemInfo.RequiredFunctions.Any())
            {
                throw new NotSupportedException("NotSupported yet: unregister subsystem that introduced new cmdlets/functions.");
            }

            // Implementation goes here ...
        }

        /// <summary>
        /// Subsystem unregistration.
        /// Return false when called for subsystems that cannot be unregistered.
        /// </summary>
        public static bool TryUnregisterSubsystem(SubsystemKind kind)
        {
            try
            {
                UnregisterSubsystem(kind);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
