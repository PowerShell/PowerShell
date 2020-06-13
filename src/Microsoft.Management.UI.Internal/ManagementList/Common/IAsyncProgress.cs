// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Windows;

namespace Microsoft.Management.UI.Internal
{
    /// <summary>
    /// An interface designed to provide updates about an asynchronous operation.
    /// If the UI is data bound to the properties in this interface then INotifyPropertyChanged should
    /// be implemented by the type implementing IAsyncProgress so the UI can get notification of the properties
    /// being changed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public interface IAsyncProgress
    {
        /// <summary>
        /// Gets a value indicating whether the async operation is currently running.
        /// </summary>
        bool OperationInProgress
        {
            get;
        }

        /// <summary>
        /// Gets a the error for the async operation.  This field is only valid if
        /// OperationInProgress is false.  null indicates there was no error.
        /// </summary>
        Exception OperationError
        {
            get;
        }
    }
}
