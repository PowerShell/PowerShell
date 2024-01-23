// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// Base proxy class for other classes which wish to have save and restore functionality.
    /// </summary>
    /// <typeparam name="T">There are no restrictions on T.</typeparam>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class StateDescriptor<T>
    {
        private Guid id;
        private string name;

        /// <summary>
        /// Creates a new instances of the StateDescriptor class and creates a new GUID.
        /// </summary>
        protected StateDescriptor()
        {
            this.Id = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor overload to provide name.
        /// </summary>
        /// <param name="name">The friendly name for the StateDescriptor.</param>
        protected StateDescriptor(string name)
            : this()
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the global unique identification number.
        /// </summary>
        public Guid Id
        {
            get
            {
                return this.id;
            }

            protected set
            {
                this.id = value;
            }
        }

        /// <summary>
        /// Gets or sets the friendly display name.
        /// </summary>
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Saves a snapshot of the subject's current state.
        /// </summary>
        /// <param name="subject">The object whose state will be saved.</param>
        public abstract void SaveState(T subject);

        /// <summary>
        /// Restores the state of subject to the saved state.
        /// </summary>
        /// <param name="subject">The object whose state will be restored.</param>
        public abstract void RestoreState(T subject);
    }
}
