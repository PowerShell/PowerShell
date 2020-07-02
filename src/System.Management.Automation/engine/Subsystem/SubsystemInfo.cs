// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Management.Automation.Subsystem
{
    /// <summary>
    /// Class used to represent the metadata and state of a subsystem.
    /// </summary>
    public abstract class SubsystemInfo
    {
        #region "Metadata of a Subsystem (public)"

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

        private protected abstract void AddImplementation(ISubsystem impl);
        private protected abstract ISubsystem RemoveImplementation(Guid id);

        internal void RegisterImplementation(ISubsystem impl)
        {
            AddImplementation(impl);
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
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind) {
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
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind) {
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
            internal ImplementationInfo(ISubsystem implementation)
            {
                Id = implementation.Id;
                Kind = implementation.Kind;
                Name = implementation.Name;
                Description = implementation.Description;
                ImplementationType = implementation.GetType();
            }

            /// <summary>
            /// Gets the unique identifier for a subsystem implementation.
            /// </summary>
            public readonly Guid Id;

            /// <summary>
            /// Gets the kind of subsystem.
            /// </summary>
            public readonly SubsystemKind Kind;

            /// <summary>
            /// Gets the name of a subsystem implementation.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Gets the description of a subsystem implementation.
            /// </summary>
            public readonly string Description;

            /// <summary>
            /// Gets the implementation type.
            /// </summary>
            public readonly Type ImplementationType;
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

        private protected override void AddImplementation(ISubsystem rawImpl)
        {
            lock (_syncObj)
            {
                var impl = (TConcreteSubsystem)rawImpl;

                if (_registeredImpls.Count == 0)
                {
                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(new[] { impl });
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

                var list = new List<TConcreteSubsystem>(_registeredImpls.Count + 1);
                list.AddRange(_registeredImpls);
                list.Add(impl);

                _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(list);
                _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(list.ConvertAll(s => new ImplementationInfo(s)));
            }
        }

        private protected override ISubsystem RemoveImplementation(Guid id)
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
                    _registeredImpls = Utils.EmptyReadOnlyCollection<TConcreteSubsystem>();
                    _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();
                }
                else
                {
                    var list = new List<TConcreteSubsystem>(_registeredImpls.Count - 1);
                    for (int i = 0; i < _registeredImpls.Count; i++)
                    {
                        if (index == i)
                        {
                            continue;
                        }

                        list.Add(_registeredImpls[i]);
                    }

                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(list);
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(list.ConvertAll(s => new ImplementationInfo(s)));
                }

                return target;
            }
        }

        internal TConcreteSubsystem GetImplementation()
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
