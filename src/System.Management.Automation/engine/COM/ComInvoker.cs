// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

using COM = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation
{
    internal static class ComInvoker
    {
        // DISP HRESULTS - may be returned by IDispatch.Invoke
        private const int DISP_E_EXCEPTION = unchecked((int)0x80020009);
        // LCID for en-US culture
        private const int LCID_DEFAULT = 0x0409;
        // The dispatch identifier for a parameter that receives the value of an assignment in a PROPERTYPUT.
        // See https://msdn.microsoft.com/library/windows/desktop/ms221242(v=vs.85).aspx for details.
        private const int DISPID_PROPERTYPUT = -3;
        // Alias of GUID_NULL. It's a GUID set to all zero
        private static readonly Guid s_IID_NULL = new Guid();
        // Size of the Variant struct
        private static readonly int s_variantSize = Marshal.SizeOf<Variant>();

        /// <summary>
        /// Make a by-Ref VARIANT value based on the passed-in VARIANT argument.
        /// </summary>
        /// <param name="srcVariantPtr">The source Variant pointer.</param>
        /// <param name="destVariantPtr">The destination Variant pointer.</param>
        private static unsafe void MakeByRefVariant(IntPtr srcVariantPtr, IntPtr destVariantPtr)
        {
            var srcVariant = (Variant*)srcVariantPtr;
            var destVariant = (Variant*)destVariantPtr;

            switch ((VarEnum)srcVariant->_typeUnion._vt)
            {
                case VarEnum.VT_EMPTY:
                case VarEnum.VT_NULL:
                    // These cannot combine with VT_BYREF. Should try passing as a variant reference
                    // We follow the code in ComBinder to handle 'VT_EMPTY' and 'VT_NULL'
                    destVariant->_typeUnion._unionTypes._byref = new IntPtr(srcVariant);
                    destVariant->_typeUnion._vt = (ushort)VarEnum.VT_VARIANT | (ushort)VarEnum.VT_BYREF;
                    return;

                case VarEnum.VT_RECORD:
                    // Representation of record is the same with or without byref
                    destVariant->_typeUnion._unionTypes._record._record = srcVariant->_typeUnion._unionTypes._record._record;
                    destVariant->_typeUnion._unionTypes._record._recordInfo = srcVariant->_typeUnion._unionTypes._record._recordInfo;
                    break;

                case VarEnum.VT_VARIANT:
                    destVariant->_typeUnion._unionTypes._byref = new IntPtr(srcVariant);
                    break;

                case VarEnum.VT_DECIMAL:
                    destVariant->_typeUnion._unionTypes._byref = new IntPtr(&(srcVariant->_decimal));
                    break;

                default:
                    // All the other cases start at the same offset (it's a Union) so using &_i4 should work.
                    // This is the same code as in CLR implementation. It could be &_i1, &_i2 and etc. CLR implementation just prefer using &_i4.
                    destVariant->_typeUnion._unionTypes._byref = new IntPtr(&(srcVariant->_typeUnion._unionTypes._i4));
                    break;
            }

            destVariant->_typeUnion._vt = (ushort)(srcVariant->_typeUnion._vt | (ushort)VarEnum.VT_BYREF);
        }

        /// <summary>
        /// Alloc memory for a VARIANT array with the specified length.
        /// Also initialize the VARIANT elements to be the type 'VT_EMPTY'.
        /// </summary>
        /// <param name="length">Array length.</param>
        /// <returns>Pointer to the array.</returns>
        private static unsafe IntPtr NewVariantArray(int length)
        {
            IntPtr variantArray = Marshal.AllocCoTaskMem(s_variantSize * length);

            for (int i = 0; i < length; i++)
            {
                IntPtr currentVarPtr = variantArray + s_variantSize * i;
                var currentVar = (Variant*)currentVarPtr;
                currentVar->_typeUnion._vt = (ushort)VarEnum.VT_EMPTY;
            }

            return variantArray;
        }

        /// <summary>
        /// Generate the ByRef array indicating whether the corresponding argument is by-reference.
        /// </summary>
        /// <param name="parameters">Parameters retrieved from metadata.</param>
        /// <param name="argumentCount">Count of arguments to pass in IDispatch.Invoke.</param>
        /// <param name="isPropertySet">Indicate if we are handling arguments for PropertyPut/PropertyPutRef.</param>
        /// <returns></returns>
        internal static bool[] GetByRefArray(ParameterInformation[] parameters, int argumentCount, bool isPropertySet)
        {
            if (parameters.Length == 0)
            {
                return null;
            }

            var byRef = new bool[argumentCount];
            int argsToProcess = argumentCount;
            if (isPropertySet)
            {
                // If it's PropertySet, then the last value in arguments is the right-hand side value.
                // There is no corresponding parameter for that value, so it's for sure not by-ref.
                // Hence, set the last item of byRef array to be false.
                argsToProcess = argumentCount - 1;
                byRef[argsToProcess] = false;
            }

            Diagnostics.Assert(parameters.Length >= argsToProcess,
                               "There might be more parameters than argsToProcess due unspecified optional arguments");

            for (int i = 0; i < argsToProcess; i++)
            {
                byRef[i] = parameters[i].isByRef;
            }

            return byRef;
        }

        /// <summary>
        /// Invoke the COM member.
        /// </summary>
        /// <param name="target">IDispatch object.</param>
        /// <param name="dispId">Dispatch identifier that identifies the member.</param>
        /// <param name="args">Arguments passed in.</param>
        /// <param name="byRef">Boolean array that indicates by-Ref parameters.</param>
        /// <param name="invokeKind">Invocation kind.</param>
        /// <returns></returns>
        internal static object Invoke(IDispatch target, int dispId, object[] args, bool[] byRef, COM.INVOKEKIND invokeKind)
        {
            Diagnostics.Assert(target != null, "Caller makes sure an IDispatch object passed in.");
            Diagnostics.Assert(args == null || byRef == null || args.Length == byRef.Length,
                "If 'args' and 'byRef' are not null, then they should be one-on-one mapping.");

            int argCount = args != null ? args.Length : 0;
            int refCount = byRef != null ? byRef.Count(c => c) : 0;
            IntPtr variantArgArray = IntPtr.Zero, dispIdArray = IntPtr.Zero, tmpVariants = IntPtr.Zero;

            try
            {
                // Package arguments
                if (argCount > 0)
                {
                    variantArgArray = NewVariantArray(argCount);

                    int refIndex = 0;
                    for (int i = 0; i < argCount; i++)
                    {
                        // !! The arguments should be in REVERSED order!!
                        int actualIndex = argCount - i - 1;
                        IntPtr varArgPtr = variantArgArray + s_variantSize * actualIndex;

                        // If need to pass by ref, create a by-ref variant
                        if (byRef != null && byRef[i])
                        {
                            // Allocate memory for temporary VARIANTs used in by-ref marshalling
                            if (tmpVariants == IntPtr.Zero)
                            {
                                tmpVariants = NewVariantArray(refCount);
                            }

                            // Create a VARIANT that the by-ref VARIANT points to
                            IntPtr tmpVarPtr = tmpVariants + s_variantSize * refIndex;
                            Marshal.GetNativeVariantForObject(args[i], tmpVarPtr);

                            // Create the by-ref VARIANT
                            MakeByRefVariant(tmpVarPtr, varArgPtr);
                            refIndex++;
                        }
                        else
                        {
                            Marshal.GetNativeVariantForObject(args[i], varArgPtr);
                        }
                    }
                }

                var paramArray = new COM.DISPPARAMS[1];
                paramArray[0].rgvarg = variantArgArray;
                paramArray[0].cArgs = argCount;

                if (invokeKind == COM.INVOKEKIND.INVOKE_PROPERTYPUT || invokeKind == COM.INVOKEKIND.INVOKE_PROPERTYPUTREF)
                {
                    // For property putters, the first DISPID argument needs to be DISPID_PROPERTYPUT
                    dispIdArray = Marshal.AllocCoTaskMem(4); // Allocate 4 bytes to hold a 32-bit signed integer
                    Marshal.WriteInt32(dispIdArray, DISPID_PROPERTYPUT);

                    paramArray[0].cNamedArgs = 1;
                    paramArray[0].rgdispidNamedArgs = dispIdArray;
                }
                else
                {
                    // Otherwise, no named parameters are necessary since powershell parser doesn't support named parameter
                    paramArray[0].cNamedArgs = 0;
                    paramArray[0].rgdispidNamedArgs = IntPtr.Zero;
                }

                // Make the call
                EXCEPINFO info = default(EXCEPINFO);
                object result = null;

                try
                {
                    // 'puArgErr' is set when IDispatch.Invoke fails with error code 'DISP_E_PARAMNOTFOUND' and 'DISP_E_TYPEMISMATCH'.
                    // Appropriate exceptions will be thrown in such cases, but FullCLR doesn't use 'puArgErr' in the exception handling, so we also ignore it.
                    uint puArgErrNotUsed = 0;
                    target.Invoke(dispId, s_IID_NULL, LCID_DEFAULT, invokeKind, paramArray, out result, out info, out puArgErrNotUsed);
                }
                catch (Exception innerException)
                {
                    // When 'IDispatch.Invoke' returns error code, CLR will raise exception based on internal HR-to-Exception mapping.
                    // Description of the return code can be found at https://msdn.microsoft.com/library/windows/desktop/ms221479(v=vs.85).aspx
                    // According to CoreCLR team (yzha), the exception needs to be wrapped as an inner exception of TargetInvocationException.

                    string exceptionMsg = null;
                    if (innerException.HResult == DISP_E_EXCEPTION)
                    {
                        // Invoke was successful but the actual underlying method failed.
                        // In this case, we use EXCEPINFO to get additional error info.

                        // Use EXCEPINFO.scode or EXCEPINFO.wCode as HR to construct the correct exception.
                        int code = info.scode != 0 ? info.scode : info.wCode;
                        innerException = Marshal.GetExceptionForHR(code, IntPtr.Zero) ?? innerException;

                        // Get the richer error description if it's available.
                        if (info.bstrDescription != IntPtr.Zero)
                        {
                            exceptionMsg = Marshal.PtrToStringBSTR(info.bstrDescription);
                            Marshal.FreeBSTR(info.bstrDescription);
                        }

                        // Free the BSTRs
                        if (info.bstrSource != IntPtr.Zero)
                        {
                            Marshal.FreeBSTR(info.bstrSource);
                        }

                        if (info.bstrHelpFile != IntPtr.Zero)
                        {
                            Marshal.FreeBSTR(info.bstrHelpFile);
                        }
                    }

                    var outerException = exceptionMsg == null
                                              ? new TargetInvocationException(innerException)
                                              : new TargetInvocationException(exceptionMsg, innerException);
                    throw outerException;
                }

                // Now back propagate the by-ref arguments
                if (refCount > 0)
                {
                    for (int i = 0; i < argCount; i++)
                    {
                        // !! The arguments should be in REVERSED order!!
                        int actualIndex = argCount - i - 1;

                        // If need to pass by ref, back propagate
                        if (byRef != null && byRef[i])
                        {
                            args[i] = Marshal.GetObjectForNativeVariant(variantArgArray + s_variantSize * actualIndex);
                        }
                    }
                }

                return result;
            }
            finally
            {
                // Free the variant argument array
                if (variantArgArray != IntPtr.Zero)
                {
                    for (int i = 0; i < argCount; i++)
                    {
                        Interop.Windows.VariantClear(variantArgArray + s_variantSize * i);
                    }

                    Marshal.FreeCoTaskMem(variantArgArray);
                }

                // Free the dispId array
                if (dispIdArray != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(dispIdArray);
                }

                // Free the temporary variants created when handling by-Ref arguments
                if (tmpVariants != IntPtr.Zero)
                {
                    for (int i = 0; i < refCount; i++)
                    {
                        Interop.Windows.VariantClear(tmpVariants + s_variantSize * i);
                    }

                    Marshal.FreeCoTaskMem(tmpVariants);
                }
            }
        }

        /// <summary>
        /// We have to declare 'bstrSource', 'bstrDescription' and 'bstrHelpFile' as pointers because
        /// CLR marshalling layer would try to free those BSTRs by default and that is not correct.
        /// Therefore, manually marshalling might be needed to extract 'bstrDescription'.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct EXCEPINFO
        {
            public short wCode;
            public short wReserved;
            public IntPtr bstrSource;
            public IntPtr bstrDescription;
            public IntPtr bstrHelpFile;
            public int dwHelpContext;
            public IntPtr pvReserved;
            public IntPtr pfnDeferredFillIn;
            public int scode;
        }

        /// <summary>
        /// VARIANT type used for passing arguments in COM interop.
        /// </summary>
        [StructLayout(LayoutKind.Explicit)]
        internal struct Variant
        {
            // Most of the data types in the Variant are carried in _typeUnion
            [FieldOffset(0)]
            internal TypeUnion _typeUnion;

            // Decimal is the largest data type and it needs to use the space that is normally unused in TypeUnion._wReserved1, etc.
            // Hence, it is declared to completely overlap with TypeUnion. A Decimal does not use the first two bytes, and so
            // TypeUnion._vt can still be used to encode the type.
            [FieldOffset(0)]
            internal Decimal _decimal;

            [StructLayout(LayoutKind.Explicit)]
            internal struct TypeUnion
            {
                [FieldOffset(0)]
                internal ushort _vt;

                [FieldOffset(2)]
                internal ushort _wReserved1;

                [FieldOffset(4)]
                internal ushort _wReserved2;

                [FieldOffset(6)]
                internal ushort _wReserved3;

                [FieldOffset(8)]
                internal UnionTypes _unionTypes;
            }

            [StructLayout(LayoutKind.Sequential)]
            internal struct Record
            {
                internal IntPtr _record;
                internal IntPtr _recordInfo;
            }

            [StructLayout(LayoutKind.Explicit)]
            internal struct UnionTypes
            {
                [FieldOffset(0)]
                internal sbyte _i1;

                [FieldOffset(0)]
                internal Int16 _i2;

                [FieldOffset(0)]
                internal Int32 _i4;

                [FieldOffset(0)]
                internal Int64 _i8;

                [FieldOffset(0)]
                internal byte _ui1;

                [FieldOffset(0)]
                internal UInt16 _ui2;

                [FieldOffset(0)]
                internal UInt32 _ui4;

                [FieldOffset(0)]
                internal UInt64 _ui8;

                [FieldOffset(0)]
                internal Int32 _int;

                [FieldOffset(0)]
                internal UInt32 _uint;

                [FieldOffset(0)]
                internal Int16 _bool;

                [FieldOffset(0)]
                internal Int32 _error;

                [FieldOffset(0)]
                internal Single _r4;

                [FieldOffset(0)]
                internal double _r8;

                [FieldOffset(0)]
                internal Int64 _cy;

                [FieldOffset(0)]
                internal double _date;

                [FieldOffset(0)]
                internal IntPtr _bstr;

                [FieldOffset(0)]
                internal IntPtr _unknown;

                [FieldOffset(0)]
                internal IntPtr _dispatch;

                [FieldOffset(0)]
                internal IntPtr _pvarVal;

                [FieldOffset(0)]
                internal IntPtr _byref;

                [FieldOffset(0)]
                internal Record _record;
            }
        }
    }
}
