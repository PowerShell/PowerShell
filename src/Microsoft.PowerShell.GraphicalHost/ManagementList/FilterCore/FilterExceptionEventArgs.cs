//-----------------------------------------------------------------------
// <copyright file="FilterExceptionEventArgs.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Microsoft.Management.UI.Internal
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// The EventArgs detailing the exception raised while
    /// evaluating the filter.
    /// </summary>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public class FilterExceptionEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the Exception that was raised when filtering was
        /// evaluated.
        /// </summary>
        public Exception Exception
        {
            get;
            private set;
        }

        /// <summary>
        /// Initializes a new instance of the FilterExceptionEventArgs
        /// class.
        /// </summary>
        /// <param name="exception">
        /// The Exception that was raised when filtering was evaluated.
        /// </param>
        public FilterExceptionEventArgs(Exception exception)
        {
            if (null == exception)
            {
                throw new ArgumentNullException("exception");
            }

            this.Exception = exception;
        }
    }
}
