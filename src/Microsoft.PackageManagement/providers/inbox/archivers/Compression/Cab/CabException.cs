//---------------------------------------------------------------------
// <copyright file="CabException.cs" company="Microsoft Corporation">
//   Copyright (c) 1999, Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.PackageManagement.Archivers.Internal.Compression.Cab
{
    using System;
    using System.Reflection;
    using System.Globalization;
    using System.Resources;

    /// <summary>
    /// Exception class for cabinet operations.
    /// </summary>
    public class CabException : ArchiveException
    {
        private static ResourceManager errorResources;
        private int error;
        private int errorCode;

        /// <summary>
        /// Creates a new CabException with a specified error message and a reference to the
        /// inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception. If the
        /// innerException parameter is not a null reference (Nothing in Visual Basic), the current exception
        /// is raised in a catch block that handles the inner exception.</param>
        public CabException(string message, Exception innerException)
            : this(0, 0, message, innerException) { }

        /// <summary>
        /// Creates a new CabException with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CabException(string message)
            : this(0, 0, message, null) { }

        /// <summary>
        /// Creates a new CabException.
        /// </summary>
        public CabException()
            : this(0, 0, null, null) { }

        internal CabException(int error, int errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            this.error = error;
            this.errorCode = errorCode;
        }

        internal CabException(int error, int errorCode, string message)
            : this(error, errorCode, message, null) { }


        /// <summary>
        /// Gets the FCI or FDI cabinet engine error number.
        /// </summary>
        /// <value>A cabinet engine error number, or 0 if the exception was
        /// not related to a cabinet engine error number.</value>
        public int Error
        {
            get
            {
                return this.error;
            }
        }

        /// <summary>
        /// Gets the Win32 error code.
        /// </summary>
        /// <value>A Win32 error code, or 0 if the exception was
        /// not related to a Win32 error.</value>
        public int ErrorCode
        {
            get
            {
                return this.errorCode;
            }
        }

        internal static ResourceManager ErrorResources
        {
            get
            {
                if (errorResources == null)
                {
                    errorResources = new ResourceManager(
                        typeof(CabException).Namespace + ".Errors",
#if CORECLR
                        typeof(CabException).GetTypeInfo().Assembly);
#else
                        typeof(CabException).Assembly);
#endif
                }
                return errorResources;
            }
        }


        internal static string GetErrorMessage(int error, int errorCode, bool extracting)
        {
            const int FCI_ERROR_RESOURCE_OFFSET = 1000;
            const int FDI_ERROR_RESOURCE_OFFSET = 2000;
            int resourceOffset = (extracting ? FDI_ERROR_RESOURCE_OFFSET : FCI_ERROR_RESOURCE_OFFSET);

            string msg = CabException.ErrorResources.GetString(
                (resourceOffset + error).ToString(CultureInfo.InvariantCulture.NumberFormat),
                CultureInfo.CurrentUICulture);

            if (msg == null)
            {
                msg = CabException.ErrorResources.GetString(
                    resourceOffset.ToString(CultureInfo.InvariantCulture.NumberFormat),
                    CultureInfo.CurrentUICulture);
            }

            if (errorCode != 0)
            {
                const string GENERIC_ERROR_RESOURCE = "1";
                string msg2 = CabException.ErrorResources.GetString(GENERIC_ERROR_RESOURCE, CultureInfo.CurrentUICulture);
                msg = String.Format(CultureInfo.InvariantCulture, "{0} " + msg2, msg, errorCode);
            }
            return msg;
        }
    }
}
