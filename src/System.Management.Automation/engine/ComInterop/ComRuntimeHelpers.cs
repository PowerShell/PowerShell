// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#if !SILVERLIGHT // ComObject

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace System.Management.Automation.ComInterop
{
    internal static class ComRuntimeHelpers
    {
        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#")]
        public static void CheckThrowException(int hresult, ref ExcepInfo excepInfo, ComMethodDesc method, object[] args, uint argErr)
        {
            if (Utils.Succeeded(hresult))
            {
                return;
            }

            Exception parameterException = null;

            switch (hresult)
            {
                case ComHresults.DISP_E_BADPARAMCOUNT:
                    // The number of elements provided to DISPPARAMS is different from the number of arguments
                    // accepted by the method or property.

                    parameterException = Error.DispBadParamCount(method.Name, args.Length - 1);
                    ThrowWrappedInvocationException(method, parameterException);
                    break;

                case ComHresults.DISP_E_BADVARTYPE:
                    // One of the arguments in rgvarg is not a valid variant type.
                    break;

                case ComHresults.DISP_E_EXCEPTION:
                    // The application needs to raise an exception. In this case, the structure passed in pExcepInfo
                    // should be filled in.
                    throw excepInfo.GetException();

                case ComHresults.DISP_E_MEMBERNOTFOUND:
                    // The requested member does not exist, or the call to Invoke tried to set the value of a
                    // read-only property.
                    throw Error.DispMemberNotFound(method.Name);

                case ComHresults.DISP_E_NONAMEDARGS:
                    // This implementation of IDispatch does not support named arguments.
                    throw Error.DispNoNamedArgs(method.Name);

                case ComHresults.DISP_E_OVERFLOW:
                    // One of the arguments in rgvarg could not be coerced to the specified type.
                    throw Error.DispOverflow(method.Name);

                case ComHresults.DISP_E_PARAMNOTFOUND:
                    break;

                case ComHresults.DISP_E_TYPEMISMATCH:
                    // The index within rgvarg of the first parameter with the incorrect
                    // type is returned in the puArgErr parameter.
                    //
                    // But: Arguments are stored in pDispParams->rgvarg in reverse order, so the first
                    // parameter is the one with the highest index in the array
                    // https://msdn.microsoft.com/library/aa912367.aspx
                    argErr = ((uint)args.Length) - argErr - 2;

                    // One or more of the arguments could not be coerced.

                    Type destinationType = null;
                    if (argErr >= method.ParameterInformation.Length)
                    {
                        destinationType = method.InputType;
                    }
                    else
                    {
                        destinationType = method.ParameterInformation[argErr].parameterType;
                    }

                    object originalValue = args[argErr + 1];

                    // If this is a put, use the InputType and the last argument
                    if (method.IsPropertyPut || method.IsPropertyPutRef)
                    {
                        destinationType = method.InputType;
                        originalValue = args[args.Length - 1];
                    }

                    string originalValueString = originalValue.ToString();
                    string originalTypeName = Microsoft.PowerShell.ToStringCodeMethods.Type(originalValue.GetType(), true);

                    // ByRef arguments should be displayed in the error message as a PSReference
                    if (destinationType == typeof(Object) && method.ParameterInformation[argErr].isByRef)
                    {
                        destinationType = typeof(PSReference);
                    }

                    string destinationTypeName = Microsoft.PowerShell.ToStringCodeMethods.Type(destinationType, true);

                    parameterException = Error.DispTypeMismatch(method.Name, originalValueString, originalTypeName, destinationTypeName);
                    ThrowWrappedInvocationException(method, parameterException);
                    break;

                case ComHresults.DISP_E_UNKNOWNINTERFACE:
                    // The interface identifier passed in riid is not IID_NULL.
                    break;

                case ComHresults.DISP_E_UNKNOWNLCID:
                    // The member being invoked interprets string arguments according to the LCID, and the
                    // LCID is not recognized.
                    break;

                case ComHresults.DISP_E_PARAMNOTOPTIONAL:
                    // A required parameter was omitted.
                    throw Error.DispParamNotOptional(method.Name);
            }

            Marshal.ThrowExceptionForHR(hresult);
        }

        private static void ThrowWrappedInvocationException(ComMethodDesc method, Exception parameterException)
        {
            if ((method.InvokeKind & ComTypes.INVOKEKIND.INVOKE_FUNC) ==
                ComTypes.INVOKEKIND.INVOKE_FUNC)
            {
                throw new MethodException(parameterException.Message, parameterException);
            }

            if (method.IsPropertyGet)
            {
                throw new GetValueInvocationException(parameterException.Message, parameterException);
            }

            if (method.IsPropertyPut || method.IsPropertyPutRef)
            {
                throw new SetValueInvocationException(parameterException.Message, parameterException);
            }

            throw parameterException;
        }

        internal static void GetInfoFromType(ComTypes.ITypeInfo typeInfo, out string name, out string documentation)
        {
            int dwHelpContext;
            string strHelpFile;

            typeInfo.GetDocumentation(-1, out name, out documentation, out dwHelpContext, out strHelpFile);
        }

        internal static string GetNameOfMethod(ComTypes.ITypeInfo typeInfo, int memid)
        {
            int cNames;
            string[] rgNames = new string[1];
            typeInfo.GetNames(memid, rgNames, 1, out cNames);
            return rgNames[0];
        }

        internal static string GetNameOfLib(ComTypes.ITypeLib typeLib)
        {
            string name;
            string strDocString;
            int dwHelpContext;
            string strHelpFile;

            typeLib.GetDocumentation(-1, out name, out strDocString, out dwHelpContext, out strHelpFile);
            return name;
        }

        internal static string GetNameOfType(ComTypes.ITypeInfo typeInfo)
        {
            string name;
            string documentation;
            GetInfoFromType(typeInfo, out name, out documentation);

            return name;
        }

        /// <summary>
        /// Look for typeinfo using IDispatch.GetTypeInfo.
        /// </summary>
        /// <param name="dispatch"></param>
        /// <param name="throwIfMissingExpectedTypeInfo">
        /// Some COM objects just dont expose typeinfo. In these cases, this method will return null.
        /// Some COM objects do intend to expose typeinfo, but may not be able to do so if the type-library is not properly
        /// registered. This will be considered as acceptable or as an error condition depending on throwIfMissingExpectedTypeInfo</param>
        /// <returns></returns>
        internal static ComTypes.ITypeInfo GetITypeInfoFromIDispatch(IDispatch dispatch, bool throwIfMissingExpectedTypeInfo)
        {
            uint typeCount;
            int hresult = dispatch.TryGetTypeInfoCount(out typeCount);

            if ((hresult == ComHresults.E_NOTIMPL) || (hresult == ComHresults.E_NOINTERFACE))
            {
                return null;
            }
            else
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            Debug.Assert(typeCount <= 1);
            if (typeCount == 0)
            {
                return null;
            }

            IntPtr typeInfoPtr = IntPtr.Zero;

            hresult = dispatch.TryGetTypeInfo(0, 0, out typeInfoPtr);
            if (!Utils.Succeeded(hresult))
            {
                CheckIfMissingTypeInfoIsExpected(hresult, throwIfMissingExpectedTypeInfo);
                return null;
            }

            if (typeInfoPtr == IntPtr.Zero)
            { // be defensive against components that return IntPtr.Zero
                if (throwIfMissingExpectedTypeInfo)
                {
                    Marshal.ThrowExceptionForHR(ComHresults.E_FAIL);
                }

                return null;
            }

            ComTypes.ITypeInfo typeInfo = null;
            try
            {
                typeInfo = Marshal.GetObjectForIUnknown(typeInfoPtr) as ComTypes.ITypeInfo;
            }
            finally
            {
                Marshal.Release(typeInfoPtr);
            }

            return typeInfo;
        }

        /// <summary>
        /// This method should be called when typeinfo is not available for an object. The function
        /// will check if the typeinfo is expected to be missing. This can include error cases where
        /// the same error is guaranteed to happen all the time, on all machines, under all circumstances.
        /// In such cases, we just have to operate without the typeinfo.
        ///
        /// However, if accessing the typeinfo is failing in a transient way, we might want to throw
        /// an exception so that we will eagerly predictably indicate the problem.
        /// </summary>
        private static void CheckIfMissingTypeInfoIsExpected(int hresult, bool throwIfMissingExpectedTypeInfo)
        {
            Debug.Assert(!Utils.Succeeded(hresult));

            // Word.Basic always returns this because of an incorrect implementation of IDispatch.GetTypeInfo
            // Any implementation that returns E_NOINTERFACE is likely to do so in all environments
            if (hresult == ComHresults.E_NOINTERFACE)
            {
                return;
            }

            // This assert is potentially over-restrictive since COM components can behave in quite unexpected ways.
            // However, asserting the common expected cases ensures that we find out about the unexpected scenarios, and
            // can investigate the scenarios to ensure that there is no bug in our own code.
            Debug.Assert(hresult == ComHresults.TYPE_E_LIBNOTREGISTERED);

            if (throwIfMissingExpectedTypeInfo)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        internal static ComTypes.TYPEATTR GetTypeAttrForTypeInfo(ComTypes.ITypeInfo typeInfo)
        {
            IntPtr pAttrs = IntPtr.Zero;
            typeInfo.GetTypeAttr(out pAttrs);

            // GetTypeAttr should never return null, this is just to be safe
            if (pAttrs == IntPtr.Zero)
            {
                throw Error.CannotRetrieveTypeInformation();
            }

            try
            {
                return (ComTypes.TYPEATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPEATTR));
            }
            finally
            {
                typeInfo.ReleaseTypeAttr(pAttrs);
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2201:DoNotRaiseReservedExceptionTypes")]
        internal static ComTypes.TYPELIBATTR GetTypeAttrForTypeLib(ComTypes.ITypeLib typeLib)
        {
            IntPtr pAttrs = IntPtr.Zero;
            typeLib.GetLibAttr(out pAttrs);

            // GetTypeAttr should never return null, this is just to be safe
            if (pAttrs == IntPtr.Zero)
            {
                throw Error.CannotRetrieveTypeInformation();
            }

            try
            {
                return (ComTypes.TYPELIBATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPELIBATTR));
            }
            finally
            {
                typeLib.ReleaseTLibAttr(pAttrs);
            }
        }

        public static BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
        {
            return new BoundDispEvent(rcw, sourceIid, dispid);
        }

        public static DispCallable CreateDispCallable(IDispatchComObject dispatch, ComMethodDesc method)
        {
            return new DispCallable(dispatch, method.Name, method.DispId);
        }
    }

    /// <summary>
    /// This class contains methods that either cannot be expressed in C#, or which require writing unsafe code.
    /// Callers of these methods need to use them extremely carefully as incorrect use could cause GC-holes
    /// and other problems.
    /// </summary>
    internal static class UnsafeMethods
    {
        [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
        [System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Process, System.Runtime.Versioning.ResourceScope.Process)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")] // TODO: fix
        [DllImport("oleaut32.dll", PreserveSig = false)]
        internal static extern void VariantClear(IntPtr variant);

        [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.Machine)]
        [System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Machine, System.Runtime.Versioning.ResourceScope.Machine)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass")] // TODO: fix
        [DllImport("oleaut32.dll", PreserveSig = false)]
        internal static extern ComTypes.ITypeLib LoadRegTypeLib(ref Guid clsid, short majorVersion, short minorVersion, int lcid);

        #region public members

        private static readonly MethodInfo s_convertByrefToPtr = Create_ConvertByrefToPtr();

        public delegate IntPtr ConvertByrefToPtrDelegate<T>(ref T value);

        private static readonly ConvertByrefToPtrDelegate<Variant> s_convertVariantByrefToPtr = (ConvertByrefToPtrDelegate<Variant>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Variant>), s_convertByrefToPtr.MakeGenericMethod(typeof(Variant)));

        private static MethodInfo Create_ConvertByrefToPtr()
        {
            // We dont use AssemblyGen.DefineMethod since that can create a anonymously-hosted DynamicMethod which cannot contain unverifiable code.
            var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName("ComSnippets"), AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("ComSnippets");
            var type = moduleBuilder.DefineType("Type$ConvertByrefToPtr", TypeAttributes.Public);

            Type[] paramTypes = new Type[] { typeof(Variant).MakeByRefType() };
            MethodBuilder mb = type.DefineMethod("ConvertByrefToPtr", MethodAttributes.Public | MethodAttributes.Static, typeof(IntPtr), paramTypes);
            GenericTypeParameterBuilder[] typeParams = mb.DefineGenericParameters("T");
            typeParams[0].SetGenericParameterAttributes(GenericParameterAttributes.NotNullableValueTypeConstraint);
            mb.SetSignature(typeof(IntPtr), null, null, new Type[] { typeParams[0].MakeByRefType() }, null, null);

            ILGenerator method = mb.GetILGenerator();

            method.Emit(OpCodes.Ldarg_0);
            method.Emit(OpCodes.Conv_I);
            method.Emit(OpCodes.Ret);

            return type.CreateType().GetMethod("ConvertByrefToPtr");
        }

        #region Generated Convert ByRef Delegates

        // *** BEGIN GENERATED CODE ***
        // generated by function: gen_ConvertByrefToPtrDelegates from: generate_comdispatch.py

        private static readonly ConvertByrefToPtrDelegate<SByte> s_convertSByteByrefToPtr = (ConvertByrefToPtrDelegate<SByte>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<SByte>), s_convertByrefToPtr.MakeGenericMethod(typeof(SByte)));
        private static readonly ConvertByrefToPtrDelegate<Int16> s_convertInt16ByrefToPtr = (ConvertByrefToPtrDelegate<Int16>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int16>), s_convertByrefToPtr.MakeGenericMethod(typeof(Int16)));
        private static readonly ConvertByrefToPtrDelegate<Int32> s_convertInt32ByrefToPtr = (ConvertByrefToPtrDelegate<Int32>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int32>), s_convertByrefToPtr.MakeGenericMethod(typeof(Int32)));
        private static readonly ConvertByrefToPtrDelegate<Int64> s_convertInt64ByrefToPtr = (ConvertByrefToPtrDelegate<Int64>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Int64>), s_convertByrefToPtr.MakeGenericMethod(typeof(Int64)));
        private static readonly ConvertByrefToPtrDelegate<Byte> s_convertByteByrefToPtr = (ConvertByrefToPtrDelegate<Byte>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Byte>), s_convertByrefToPtr.MakeGenericMethod(typeof(Byte)));
        private static readonly ConvertByrefToPtrDelegate<UInt16> s_convertUInt16ByrefToPtr = (ConvertByrefToPtrDelegate<UInt16>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt16>), s_convertByrefToPtr.MakeGenericMethod(typeof(UInt16)));
        private static readonly ConvertByrefToPtrDelegate<UInt32> s_convertUInt32ByrefToPtr = (ConvertByrefToPtrDelegate<UInt32>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt32>), s_convertByrefToPtr.MakeGenericMethod(typeof(UInt32)));
        private static readonly ConvertByrefToPtrDelegate<UInt64> s_convertUInt64ByrefToPtr = (ConvertByrefToPtrDelegate<UInt64>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UInt64>), s_convertByrefToPtr.MakeGenericMethod(typeof(UInt64)));
        private static readonly ConvertByrefToPtrDelegate<IntPtr> s_convertIntPtrByrefToPtr = (ConvertByrefToPtrDelegate<IntPtr>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<IntPtr>), s_convertByrefToPtr.MakeGenericMethod(typeof(IntPtr)));
        private static readonly ConvertByrefToPtrDelegate<UIntPtr> s_convertUIntPtrByrefToPtr = (ConvertByrefToPtrDelegate<UIntPtr>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<UIntPtr>), s_convertByrefToPtr.MakeGenericMethod(typeof(UIntPtr)));
        private static readonly ConvertByrefToPtrDelegate<Single> s_convertSingleByrefToPtr = (ConvertByrefToPtrDelegate<Single>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Single>), s_convertByrefToPtr.MakeGenericMethod(typeof(Single)));
        private static readonly ConvertByrefToPtrDelegate<Double> s_convertDoubleByrefToPtr = (ConvertByrefToPtrDelegate<Double>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Double>), s_convertByrefToPtr.MakeGenericMethod(typeof(Double)));
        private static readonly ConvertByrefToPtrDelegate<Decimal> s_convertDecimalByrefToPtr = (ConvertByrefToPtrDelegate<Decimal>)Delegate.CreateDelegate(typeof(ConvertByrefToPtrDelegate<Decimal>), s_convertByrefToPtr.MakeGenericMethod(typeof(Decimal)));

        // *** END GENERATED CODE ***

        #endregion

        #region Generated Outer ConvertByrefToPtr

        // *** BEGIN GENERATED CODE ***
        // generated by function: gen_ConvertByrefToPtr from: generate_comdispatch.py

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertSByteByrefToPtr(ref SByte value) { return s_convertSByteByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertInt16ByrefToPtr(ref Int16 value) { return s_convertInt16ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertInt32ByrefToPtr(ref Int32 value) { return s_convertInt32ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertInt64ByrefToPtr(ref Int64 value) { return s_convertInt64ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertByteByrefToPtr(ref Byte value) { return s_convertByteByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertUInt16ByrefToPtr(ref UInt16 value) { return s_convertUInt16ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertUInt32ByrefToPtr(ref UInt32 value) { return s_convertUInt32ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertUInt64ByrefToPtr(ref UInt64 value) { return s_convertUInt64ByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertIntPtrByrefToPtr(ref IntPtr value) { return s_convertIntPtrByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertUIntPtrByrefToPtr(ref UIntPtr value) { return s_convertUIntPtrByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertSingleByrefToPtr(ref Single value) { return s_convertSingleByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertDoubleByrefToPtr(ref Double value) { return s_convertDoubleByrefToPtr(ref value); }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertDecimalByrefToPtr(ref Decimal value) { return s_convertDecimalByrefToPtr(ref value); }

        // *** END GENERATED CODE ***

        #endregion

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public static IntPtr ConvertVariantByrefToPtr(ref Variant value) { return s_convertVariantByrefToPtr(ref value); }

        internal static Variant GetVariantForObject(object obj)
        {
            Variant variant = default(Variant);
            if (obj == null)
            {
                return variant;
            }

            InitVariantForObject(obj, ref variant);
            return variant;
        }

        internal static void InitVariantForObject(object obj, ref Variant variant)
        {
            Debug.Assert(obj != null);

            // GetNativeVariantForObject is very expensive for values that marshal as VT_DISPATCH
            // also is is extremely common scenario when object at hand is an RCW.
            // Therefore we are going to test for IDispatch before defaulting to GetNativeVariantForObject.
            IDispatch disp = obj as IDispatch;
            if (disp != null)
            {
                variant.AsDispatch = obj;
                return;
            }

            System.Runtime.InteropServices.Marshal.GetNativeVariantForObject(obj, ConvertVariantByrefToPtr(ref variant));
        }

        [Obsolete("do not use this method", true)]
        public static object GetObjectForVariant(Variant variant)
        {
            IntPtr ptr = UnsafeMethods.ConvertVariantByrefToPtr(ref variant);
            return System.Runtime.InteropServices.Marshal.GetObjectForNativeVariant(ptr);
        }

        [Obsolete("do not use this method", true)]
        public static int IUnknownRelease(IntPtr interfacePointer)
        {
            return s_IUnknownRelease(interfacePointer);
        }

        [Obsolete("do not use this method", true)]
        public static void IUnknownReleaseNotZero(IntPtr interfacePointer)
        {
            if (interfacePointer != IntPtr.Zero)
            {
                IUnknownRelease(interfacePointer);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        [Obsolete("do not use this method", true)]
        public static int IDispatchInvoke(
            IntPtr dispatchPointer,
            int memberDispId,
            ComTypes.INVOKEKIND flags,
            ref ComTypes.DISPPARAMS dispParams,
            out Variant result,
            out ExcepInfo excepInfo,
            out uint argErr
        )
        {
            int hresult = s_IDispatchInvoke(
                dispatchPointer,
                memberDispId,
                flags,
                ref dispParams,
                out result,
                out excepInfo,
                out argErr
            );

            if (hresult == ComHresults.DISP_E_MEMBERNOTFOUND
                && (flags & ComTypes.INVOKEKIND.INVOKE_FUNC) != 0
                && (flags & (ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT | ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF)) == 0)
            {
                // Re-invoke with no result argument to accomodate Word
                hresult = _IDispatchInvokeNoResult(
                    dispatchPointer,
                    memberDispId,
                    ComTypes.INVOKEKIND.INVOKE_FUNC,
                    ref dispParams,
                    out result,
                    out excepInfo,
                    out argErr);
            }

            return hresult;
        }

        [Obsolete("do not use this method", true)]
        public static IntPtr GetIdsOfNamedParameters(IDispatch dispatch, string[] names, int methodDispId, out GCHandle pinningHandle)
        {
            pinningHandle = GCHandle.Alloc(null, GCHandleType.Pinned);
            int[] dispIds = new int[names.Length];
            Guid empty = Guid.Empty;
            int hresult = dispatch.TryGetIDsOfNames(ref empty, names, (uint)names.Length, 0, dispIds);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            if (methodDispId != dispIds[0])
            {
                throw Error.GetIDsOfNamesInvalid(names[0]);
            }

            int[] keywordArgDispIds = dispIds.RemoveFirst(); // Remove the dispId of the method name

            pinningHandle.Target = keywordArgDispIds;
            return Marshal.UnsafeAddrOfPinnedArrayElement(keywordArgDispIds, 0);
        }

        #endregion

        #region non-public members

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static UnsafeMethods()
        {
        }

        private static void EmitLoadArg(ILGenerator il, int index)
        {
            switch (index)
            {
                case 0:
                    il.Emit(OpCodes.Ldarg_0);
                    break;
                case 1:
                    il.Emit(OpCodes.Ldarg_1);
                    break;
                case 2:
                    il.Emit(OpCodes.Ldarg_2);
                    break;
                case 3:
                    il.Emit(OpCodes.Ldarg_3);
                    break;
                default:
                    if (index <= Byte.MaxValue)
                    {
                        il.Emit(OpCodes.Ldarg_S, (byte)index);
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldarg, index);
                    }

                    break;
            }
        }

        /// <summary>
        /// Ensure that "value" is a local variable in some caller's frame. So converting
        /// the byref to an IntPtr is a safe operation. Alternatively, we could also allow
        /// allowed "value"  to be a pinned object.
        /// </summary>
        [Conditional("DEBUG")]
        public static void AssertByrefPointsToStack(IntPtr ptr)
        {
            if (Marshal.ReadInt32(ptr) == _dummyMarker)
            {
                // Prevent recursion
                return;
            }

            int dummy = _dummyMarker;
            IntPtr ptrToLocal = ConvertInt32ByrefToPtr(ref dummy);
            Debug.Assert(ptrToLocal.ToInt64() < ptr.ToInt64());
            Debug.Assert((ptr.ToInt64() - ptrToLocal.ToInt64()) < (16 * 1024));
        }

        private static readonly object s_lock = new object();
        private static ModuleBuilder s_dynamicModule;

        internal static ModuleBuilder DynamicModule
        {
            get
            {
                if (s_dynamicModule != null)
                {
                    return s_dynamicModule;
                }

                lock (s_lock)
                {
                    if (s_dynamicModule == null)
                    {
                        var attributes = new[] {
                            new CustomAttributeBuilder(typeof(UnverifiableCodeAttribute).GetConstructor(Type.EmptyTypes), Array.Empty<object>()),
                            // PermissionSet(SecurityAction.Demand, Unrestricted = true)
                            new CustomAttributeBuilder(typeof(PermissionSetAttribute).GetConstructor(new Type[] { typeof(SecurityAction) }),
                                new object[] { SecurityAction.Demand },
                                new PropertyInfo[] { typeof(PermissionSetAttribute).GetProperty("Unrestricted") },
                                new object[] {true})
                        };

                        string name = typeof(VariantArray).Namespace + ".DynamicAssembly";
                        var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run, attributes);
                        assembly.DefineVersionInfoResource();
                        s_dynamicModule = assembly.DefineDynamicModule(name);
                    }

                    return s_dynamicModule;
                }
            }
        }

        private const int _dummyMarker = 0x10101010;

        /// <summary>
        /// We will emit an indirect call to an unmanaged function pointer from the vtable of the given interface pointer.
        /// This approach can take only ~300 instructions on x86 compared with ~900 for Marshal.Release. We are relying on
        /// the JIT-compiler to do pinvoke-stub-inlining and calling the pinvoke target directly.
        /// </summary>
        private delegate int IUnknownReleaseDelegate(IntPtr interfacePointer);
        private static readonly IUnknownReleaseDelegate s_IUnknownRelease = Create_IUnknownRelease();

        private static IUnknownReleaseDelegate Create_IUnknownRelease()
        {
            DynamicMethod dm = new DynamicMethod("IUnknownRelease", typeof(int), new Type[] { typeof(IntPtr) }, DynamicModule);

            ILGenerator method = dm.GetILGenerator();

            // return functionPtr(...)

            method.Emit(OpCodes.Ldarg_0);

            // functionPtr = *(IntPtr*)(*(interfacePointer) + VTABLE_OFFSET)
            int iunknownReleaseOffset = ((int)IDispatchMethodIndices.IUnknown_Release) * Marshal.SizeOf(typeof(IntPtr));
            method.Emit(OpCodes.Ldarg_0);
            method.Emit(OpCodes.Ldind_I);
            method.Emit(OpCodes.Ldc_I4, iunknownReleaseOffset);
            method.Emit(OpCodes.Add);
            method.Emit(OpCodes.Ldind_I);

            System.Reflection.Emit.SignatureHelper signature = System.Reflection.Emit.SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof(int));
            signature.AddArgument(typeof(IntPtr));
            method.Emit(OpCodes.Calli, signature);

            method.Emit(OpCodes.Ret);

            return (IUnknownReleaseDelegate)dm.CreateDelegate(typeof(IUnknownReleaseDelegate));
        }

        internal static readonly IntPtr NullInterfaceId = GetNullInterfaceId();

        private static IntPtr GetNullInterfaceId()
        {
            int size = Marshal.SizeOf(Guid.Empty);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            for (int i = 0; i < size; i++)
            {
                Marshal.WriteByte(ptr, i, 0);
            }

            return ptr;
        }

        /// <summary>
        /// We will emit an indirect call to an unmanaged function pointer from the vtable of the given IDispatch interface pointer.
        /// It is not possible to express this in C#. Using an indirect pinvoke call allows us to do our own marshalling.
        /// We can allocate the Variant arguments cheaply on the stack. We are relying on the JIT-compiler to do
        /// pinvoke-stub-inlining and calling the pinvoke target directly.
        /// The alternative of calling via a managed interface declaration of IDispatch would have a performance
        /// penalty of going through a CLR stub that would have to re-push the arguments on the stack, etc.
        /// Marshal.GetDelegateForFunctionPointer could be used here, but its too expensive (~2000 instructions on x86).
        /// </summary>
        private delegate int IDispatchInvokeDelegate(
            IntPtr dispatchPointer,
            int memberDispId,
            ComTypes.INVOKEKIND flags,
            ref ComTypes.DISPPARAMS dispParams,
            out Variant result,
            out ExcepInfo excepInfo,
            out uint argErr
        );

        private static readonly IDispatchInvokeDelegate s_IDispatchInvoke = Create_IDispatchInvoke(true);
        private static IDispatchInvokeDelegate s_IDispatchInvokeNoResultImpl;

        private static IDispatchInvokeDelegate _IDispatchInvokeNoResult
        {
            get
            {
                if (s_IDispatchInvokeNoResultImpl == null)
                {
                    lock (s_IDispatchInvoke)
                    {
                        if (s_IDispatchInvokeNoResultImpl == null)
                        {
                            s_IDispatchInvokeNoResultImpl = Create_IDispatchInvoke(false);
                        }
                    }
                }

                return s_IDispatchInvokeNoResultImpl;
            }
        }

        private static IDispatchInvokeDelegate Create_IDispatchInvoke(bool returnResult)
        {
            const int dispatchPointerIndex = 0;
            const int memberDispIdIndex = 1;
            const int flagsIndex = 2;
            const int dispParamsIndex = 3;
            const int resultIndex = 4;
            const int exceptInfoIndex = 5;
            const int argErrIndex = 6;
            Debug.Assert(argErrIndex + 1 == typeof(IDispatchInvokeDelegate).GetMethod("Invoke").GetParameters().Length);

            Type[] paramTypes = new Type[argErrIndex + 1];
            paramTypes[dispatchPointerIndex] = typeof(IntPtr);
            paramTypes[memberDispIdIndex] = typeof(int);
            paramTypes[flagsIndex] = typeof(ComTypes.INVOKEKIND);
            paramTypes[dispParamsIndex] = typeof(ComTypes.DISPPARAMS).MakeByRefType();
            paramTypes[resultIndex] = typeof(Variant).MakeByRefType();
            paramTypes[exceptInfoIndex] = typeof(ExcepInfo).MakeByRefType();
            paramTypes[argErrIndex] = typeof(uint).MakeByRefType();

            // Define the dynamic method in our assembly so we skip verification
            DynamicMethod dm = new DynamicMethod("IDispatchInvoke", typeof(int), paramTypes, DynamicModule);
            ILGenerator method = dm.GetILGenerator();

            // return functionPtr(...)

            EmitLoadArg(method, dispatchPointerIndex);
            EmitLoadArg(method, memberDispIdIndex);

            // burn the address of our empty IID in directly.  This is never freed, relocated, etc...
            // Note passing this as a Guid directly results in a ~30% perf hit for IDispatch invokes so
            // we also pass it directly as an IntPtr instead.
            if (IntPtr.Size == 4)
            {
                method.Emit(OpCodes.Ldc_I4, UnsafeMethods.NullInterfaceId.ToInt32()); // riid
            }
            else
            {
                method.Emit(OpCodes.Ldc_I8, UnsafeMethods.NullInterfaceId.ToInt64()); // riid
            }

            method.Emit(OpCodes.Conv_I);

            method.Emit(OpCodes.Ldc_I4_0); // lcid
            EmitLoadArg(method, flagsIndex);

            EmitLoadArg(method, dispParamsIndex);

            if (returnResult)
            {
                EmitLoadArg(method, resultIndex);
            }
            else
            {
                method.Emit(OpCodes.Ldsfld, typeof(IntPtr).GetField("Zero"));
            }

            EmitLoadArg(method, exceptInfoIndex);
            EmitLoadArg(method, argErrIndex);

            // functionPtr = *(IntPtr*)(*(dispatchPointer) + VTABLE_OFFSET)
            int idispatchInvokeOffset = ((int)IDispatchMethodIndices.IDispatch_Invoke) * Marshal.SizeOf(typeof(IntPtr));
            EmitLoadArg(method, dispatchPointerIndex);
            method.Emit(OpCodes.Ldind_I);
            method.Emit(OpCodes.Ldc_I4, idispatchInvokeOffset);
            method.Emit(OpCodes.Add);
            method.Emit(OpCodes.Ldind_I);

            System.Reflection.Emit.SignatureHelper signature = System.Reflection.Emit.SignatureHelper.GetMethodSigHelper(CallingConvention.Winapi, typeof(int));
            Type[] invokeParamTypes = new Type[] {
                    typeof(IntPtr), // dispatchPointer
                    typeof(int),    // memberDispId
                    typeof(IntPtr), // riid
                    typeof(int),    // lcid
                    typeof(ushort), // flags
                    typeof(IntPtr), // dispParams
                    typeof(IntPtr), // result
                    typeof(IntPtr), // excepInfo
                    typeof(IntPtr), // argErr
                };
            signature.AddArguments(invokeParamTypes, null, null);
            method.Emit(OpCodes.Calli, signature);

            method.Emit(OpCodes.Ret);
            return (IDispatchInvokeDelegate)dm.CreateDelegate(typeof(IDispatchInvokeDelegate));
        }

        #endregion
    }

    internal static class NativeMethods
    {
        [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
        [System.Runtime.Versioning.ResourceConsumption(System.Runtime.Versioning.ResourceScope.Process, System.Runtime.Versioning.ResourceScope.Process)]
        [DllImport("oleaut32.dll", PreserveSig = false)]
        internal static extern void VariantClear(IntPtr variant);
    }
}

#endif

