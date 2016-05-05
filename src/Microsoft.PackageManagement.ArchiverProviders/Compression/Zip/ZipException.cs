//---------------------------------------------------------------------
// <copyright file="ZipException.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Zip
{
    using System;

    /// <summary>
    /// Exception class for zip operations.
    /// </summary>
    public class ZipException : ArchiveException
    {
        /// <summary>
        /// Creates a new ZipException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public ZipException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Creates a new ZipException with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ZipException(string message)
            : this(message, null) { }

        /// <summary>
        /// Creates a new ZipException.
        /// </summary>
        public ZipException()
            : this(null, null) { }

    }
}
