// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;
using Microsoft.PowerShell.Telemetry;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Class used to represent the metadata and state of a subsystem.
    /// </summary>
    public abstract class SubsystemInfo
    {
        #region "Metadata of a Subsystem (public)"

        /// <summary>
        /// Gets the kind of a concrete subsystem.
        /// </summary>
        public SubsystemKind Kind { get; }

        /// <summary>
        /// Gets the type of a concrete subsystem.
        /// </summary>
        public Type SubsystemType { get; }

        /// <summary>
        /// Gets a value indicating whether the subsystem allows to unregister an implementation.
        /// </summary>
        public bool AllowUnregistration { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the subsystem allows to have multiple implementations registered.
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
        // public ReadOnlyCollection<SubsystemKind> DependsOn { get; private set; }

        #endregion

        #region "State of a Subsystem (public)"

        /// <summary>
        /// Indicate whether there is any implementation registered to the subsystem.
        /// </summary>
        public bool IsRegistered => _cachedImplInfos.Count > 0;

        /// <summary>
        /// Get the information about the registered implementations.
        /// </summary>
        public ReadOnlyCollection<ImplementationInfo> Implementations => _cachedImplInfos;

        #endregion

        #region "private/internal instance members"

        private protected readonly object _syncObj;
        private protected ReadOnlyCollection<ImplementationInfo> _cachedImplInfos;

        private protected SubsystemInfo(SubsystemKind kind, Type subsystemType)
        {
            _syncObj = new object();
            _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();

            Kind = kind;
            SubsystemType = subsystemType;
            AllowUnregistration = false;
            AllowMultipleRegistration = false;
            RequiredCmdlets = Utils.EmptyReadOnlyCollection<string>();
            RequiredFunctions = Utils.EmptyReadOnlyCollection<string>();
        }

        private protected abstract void AddImplementation(ISubsystem rawImpl);

        private protected abstract ISubsystem RemoveImplementation(Guid id);

        internal void RegisterImplementation(ISubsystem impl)
        {
            AddImplementation(impl);
            ApplicationInsightsTelemetry.SendUseTelemetry(ApplicationInsightsTelemetry.s_subsystemRegistration, impl.Name);
        }

        internal ISubsystem UnregisterImplementation(Guid id)
        {
            return RemoveImplementation(id);
        }

        #endregion

        #region "Static factory overloads"

        internal static SubsystemInfo Create<TConcreteSubsystem>(SubsystemKind kind)
            where TConcreteSubsystem : class, ISubsystem
        {
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind);
        }

        internal static SubsystemInfo Create<TConcreteSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration) where TConcreteSubsystem : class, ISubsystem
        {
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind)
            {
                AllowUnregistration = allowUnregistration,
                AllowMultipleRegistration = allowMultipleRegistration,
            };
        }

        internal static SubsystemInfo Create<TConcreteSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration,
            ReadOnlyCollection<string> requiredCmdlets,
            ReadOnlyCollection<string> requiredFunctions) where TConcreteSubsystem : class, ISubsystem
        {
            if (allowMultipleRegistration &&
                (requiredCmdlets.Count > 0 || requiredFunctions.Count > 0))
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.InvalidSubsystemInfo,
                        kind.ToString()));
            }

            return new SubsystemInfoImpl<TConcreteSubsystem>(kind)
            {
                AllowUnregistration = allowUnregistration,
                AllowMultipleRegistration = allowMultipleRegistration,
                RequiredCmdlets = requiredCmdlets,
                RequiredFunctions = requiredFunctions,
            };
        }

        #endregion

        #region "ImplementationInfo"

        /// <summary>
        /// Information about an implementation of a subsystem.
        /// </summary>
        public class ImplementationInfo
        {
            internal ImplementationInfo(SubsystemKind kind, ISubsystem implementation)
            {
                Id = implementation.Id;
                Kind = kind;
                Name = implementation.Name;
                Description = implementation.Description;
                ImplementationType = implementation.GetType();
            }

            /// <summary>
            /// Gets the unique identifier for a subsystem implementation.
            /// </summary>
            public Guid Id { get; }

            /// <summary>
            /// Gets the kind of subsystem.
            /// </summary>
            public SubsystemKind Kind { get; }

            /// <summary>
            /// Gets the name of a subsystem implementation.
            /// </summary>
            public string Name { get; }

            /// <summary>
            /// Gets the description of a subsystem implementation.
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Gets the implementation type.
            /// </summary>
            public Type ImplementationType { get; }
        }

        #endregion
    }

    internal sealed class SubsystemInfoImpl<TConcreteSubsystem> : SubsystemInfo
        where TConcreteSubsystem : class, ISubsystem
    {
        private ReadOnlyCollection<TConcreteSubsystem> _registeredImpls;

        internal SubsystemInfoImpl(SubsystemKind kind)
            : base(kind, typeof(TConcreteSubsystem))
        {
            _registeredImpls = Utils.EmptyReadOnlyCollection<TConcreteSubsystem>();
        }

        /// <summary>
        /// The 'add' and 'remove' operations are implemented in a way to optimize the 'reading' operation,
        /// so that reading is lock-free and allocation-free, at the cost of O(n) copy in 'add' and 'remove'
        /// ('n' is the number of registered implementations).
        /// </summary>
        /// <remarks>
        /// In the subsystem scenario, registration operations will be minimum, and in most cases, the registered
        /// implementation will never be unregistered, so optimization for reading is more important.
        /// </remarks>
        /// <param name="rawImpl">The subsystem implementation to be added.</param>
        private protected override void AddImplementation(ISubsystem rawImpl)
        {
            lock (_syncObj)
            {
                var impl = (TConcreteSubsystem)rawImpl;

                if (_registeredImpls.Count == 0)
                {
                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(new[] { impl });
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(new[] { new ImplementationInfo(Kind, impl) });
                    return;
                }

                if (!AllowMultipleRegistration)
                {
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.MultipleRegistrationNotAllowed,
                            Kind.ToString()));
                }

                foreach (TConcreteSubsystem item in _registeredImpls)
                {
                    if (item.Id == impl.Id)
                    {
                        throw new InvalidOperationException(
                            StringUtil.Format(
                                SubsystemStrings.ImplementationAlreadyRegistered,
                                impl.Id,
                                Kind.ToString()));
                    }
                }

                int newCapacity = _registeredImpls.Count + 1;
                var implList = new List<TConcreteSubsystem>(newCapacity);
                implList.AddRange(_registeredImpls);
                implList.Add(impl);

                var implInfo = new List<ImplementationInfo>(newCapacity);
                implInfo.AddRange(_cachedImplInfos);
                implInfo.Add(new ImplementationInfo(Kind, impl));

                _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(implList);
                _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(implInfo);
            }
        }

        /// <summary>
        /// The 'add' and 'remove' operations are implemented in a way to optimize the 'reading' operation,
        /// so that reading is lock-free and allocation-free, at the cost of O(n) copy in 'add' and 'remove'
        /// ('n' is the number of registered implementations).
        /// </summary>
        /// <remarks>
        /// In the subsystem scenario, registration operations will be minimum, and in most cases, the registered
        /// implementation will never be unregistered, so optimization for reading is more important.
        /// </remarks>
        /// <param name="id">The id of the subsystem implementation to be removed.</param>
        /// <returns>The subsystem implementation that was removed.</returns>
        private protected override ISubsystem RemoveImplementation(Guid id)
        {
            if (!AllowUnregistration)
            {
                throw new InvalidOperationException(
                    StringUtil.Format(
                        SubsystemStrings.UnregistrationNotAllowed,
                        Kind.ToString()));
            }

            lock (_syncObj)
            {
                if (_registeredImpls.Count == 0)
                {
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.NoImplementationRegistered,
                            Kind.ToString()));
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
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.ImplementationNotFound,
                            id.ToString()));
                }

                ISubsystem target = _registeredImpls[index];
                if (_registeredImpls.Count == 1)
                {
                    _registeredImpls = Utils.EmptyReadOnlyCollection<TConcreteSubsystem>();
                    _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();
                }
                else
                {
                    int newCapacity = _registeredImpls.Count - 1;
                    var implList = new List<TConcreteSubsystem>(newCapacity);
                    var implInfo = new List<ImplementationInfo>(newCapacity);

                    for (int i = 0; i < _registeredImpls.Count; i++)
                    {
                        if (index == i)
                        {
                            continue;
                        }

                        implList.Add(_registeredImpls[i]);
                        implInfo.Add(_cachedImplInfos[i]);
                    }

                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(implList);
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(implInfo);
                }

                return target;
            }
        }

        internal TConcreteSubsystem? GetImplementation()
        {
            var localRef = _registeredImpls;
            return localRef.Count > 0 ? localRef[localRef.Count - 1] : null;
        }

        internal ReadOnlyCollection<TConcreteSubsystem> GetAllImplementations()
        {
            return _registeredImpls;
        }
    }
}
