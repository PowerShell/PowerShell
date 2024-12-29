// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// This is similar to ComTypes.EXCEPINFO, but lets us do our own custom marshalling.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ExcepInfo
    {
        private short _wCode;
        private short _wReserved;
        private IntPtr _bstrSource;
        private IntPtr _bstrDescription;
        private IntPtr _bstrHelpFile;
        private int _dwHelpContext;
        private IntPtr _pvReserved;
        private IntPtr _pfnDeferredFillIn;
        private int _scode;

#if DEBUG
        static ExcepInfo()
        {
            Debug.Assert(Marshal.SizeOf(typeof(ExcepInfo)) == Marshal.SizeOf(typeof(ComTypes.EXCEPINFO)));
        }
#endif

        private static string ConvertAndFreeBstr(ref IntPtr bstr)
        {
            if (bstr == IntPtr.Zero)
            {
                return null;
            }

            string result = Marshal.PtrToStringBSTR(bstr);
            Marshal.FreeBSTR(bstr);
            bstr = IntPtr.Zero;
            return result;
        }

        internal Exception GetException()
        {
            Debug.Assert(_pfnDeferredFillIn == IntPtr.Zero);
#if DEBUG
            System.Diagnostics.Debug.Assert(_wReserved != -1);
            _wReserved = -1; // to ensure that the method gets called only once
#endif

            int errorCode = (_scode != 0) ? _scode : _wCode;
            Exception exception = Marshal.GetExceptionForHR(errorCode);

            string message = ConvertAndFreeBstr(ref _bstrDescription);
            if (message != null)
            {
                // If we have a custom message, create a new Exception object with the message set correctly.
                // We need to create a new object because "exception.Message" is a read-only property.
                if (exception is COMException)
                {
                    exception = new COMException(message, errorCode);
                }
                else
                {
                    Type exceptionType = exception.GetType();
                    ConstructorInfo ctor = exceptionType.GetConstructor(new Type[] { typeof(string) });
                    if (ctor != null)
                    {
                        exception = (Exception)ctor.Invoke(new object[] { message });
                    }
                }
            }

            exception.Source = ConvertAndFreeBstr(ref _bstrSource);

            string helpLink = ConvertAndFreeBstr(ref _bstrHelpFile);
            if (helpLink != null && _dwHelpContext != 0)
            {
                helpLink += "#" + _dwHelpContext;
            }
            exception.HelpLink = helpLink;

            return exception;
        }
    }
}
