/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/

#if !SILVERLIGHT // ComObject

namespace System.Management.Automation.ComInterop
{
    internal static class ComHresults
    {
        internal const int S_OK = 0;

        internal const int CONNECT_E_NOCONNECTION = unchecked((int)0x80040200);

        internal const int DISP_E_UNKNOWNINTERFACE = unchecked((int)0x80020001);
        internal const int DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003);
        internal const int DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004);
        internal const int DISP_E_TYPEMISMATCH = unchecked((int)0x80020005);
        internal const int DISP_E_UNKNOWNNAME = unchecked((int)0x80020006); // GetIDsOfName
        internal const int DISP_E_NONAMEDARGS = unchecked((int)0x80020007);
        internal const int DISP_E_BADVARTYPE = unchecked((int)0x80020008);
        internal const int DISP_E_EXCEPTION = unchecked((int)0x80020009);
        internal const int DISP_E_OVERFLOW = unchecked((int)0x8002000A);
        internal const int DISP_E_BADINDEX = unchecked((int)0x8002000B); // GetTypeInfo
        internal const int DISP_E_UNKNOWNLCID = unchecked((int)0x8002000C);
        internal const int DISP_E_ARRAYISLOCKED = unchecked((int)0x8002000D); // VariantClear
        internal const int DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E);
        internal const int DISP_E_PARAMNOTOPTIONAL = unchecked((int)0x8002000F);

        internal const int E_NOTIMPL = unchecked((int)0x80004001);
        internal const int E_NOINTERFACE = unchecked((int)0x80004002);
        internal const int E_FAIL = unchecked((int)0x80004005);

        internal const int TYPE_E_LIBNOTREGISTERED = unchecked((int)0x8002801D);
    }
}
#endif

