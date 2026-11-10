// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

#if UNIX

using System;
using System.Runtime.Serialization;

namespace Microsoft.PowerShell.Commands
{

    // Custom serialization support is intentional so that the ServiceName property is
    // preserved when the exception is serialized (for example in remoting scenarios).
    // The base Exception serialization members are marked obsolete (SYSLIB0051) because
    // they were designed for the legacy formatter-based serialization.
#pragma warning disable SYSLIB0051

    /// <summary>
    /// Non-terminating errors occurring in the service noun commands on Unix platforms.
    /// </summary>
    [Serializable]
    public class ServiceCommandException : SystemException, System.Runtime.Serialization.ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCommandException"/> class.
        /// </summary>
        public ServiceCommandException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCommandException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ServiceCommandException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCommandException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ServiceCommandException(string message, Exception? innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceCommandException"/> class.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        protected ServiceCommandException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            ServiceName = info.GetString(nameof(ServiceName)) ?? string.Empty;
        }

        /// <summary>
        /// Gets or sets the name of the service which could not be found or operated upon.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Serializes the exception, preserving the service name.
        /// </summary>
        /// <param name="info">The serialization information.</param>
        /// <param name="context">The streaming context.</param>
        [Obsolete("Legacy serialization support is deprecated since .NET 8, hence this method is now marked as obsolete", DiagnosticId = "SYSLIB0051")]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(ServiceName), ServiceName);
        }
    }

#pragma warning restore SYSLIB0051
}

#endif
