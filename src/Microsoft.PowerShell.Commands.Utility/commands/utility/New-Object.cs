// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Language;
using System.Management.Automation.Security;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

#endregion

namespace Microsoft.PowerShell.Commands
{
    /// <summary>Create a new .net object</summary>
    [Cmdlet(VerbsCommon.New, "Object", DefaultParameterSetName = netSetName, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113355")]
    public sealed class NewObjectCommand : PSCmdlet
    {
        #region parameters

        /// <summary> the number</summary>
        [Parameter(ParameterSetName = netSetName, Mandatory = true, Position = 0)]
        [ValidateTrustedData]
        public string TypeName { get; set; } = null;

#if !UNIX
        private Guid _comObjectClsId = Guid.Empty;
        /// <summary>
        /// The ProgID of the Com object.
        /// </summary>
        [Parameter(ParameterSetName = "Com", Mandatory = true, Position = 0)]
        [ValidateTrustedData]
        public string ComObject { get; set; } = null;
#endif

        /// <summary>
        /// The parameters for the constructor.
        /// </summary>
        /// <value></value>
        [Parameter(ParameterSetName = netSetName, Mandatory = false, Position = 1)]
        [ValidateTrustedData]
        [Alias("Args")]
        public object[] ArgumentList { get; set; } = null;

        /// <summary>
        /// True if we should have an error when Com objects will use an interop assembly.
        /// </summary>
        [Parameter(ParameterSetName = "Com")]
        public SwitchParameter Strict { get; set; }

        // Updated from Hashtable to IDictionary to support the work around ordered hashtables.
        /// <summary>
        /// Gets the properties to be set.
        /// </summary>
        [Parameter]
        [ValidateTrustedData]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary Property { get; set; }

        #endregion parameters

        #region private
        private object CallConstructor(Type type, ConstructorInfo[] constructors, object[] args)
        {
            object result = null;
            try
            {
                result = DotNetAdapter.ConstructorInvokeDotNet(type, constructors, args);
            }
            catch (MethodException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "ConstructorInvokedThrowException",
                    ErrorCategory.InvalidOperation, null));
            }
            // let other exceptions propagate
            return result;
        }

        private void CreateMemberNotFoundError(PSObject pso, DictionaryEntry property, Type resultType)
        {
            string message = StringUtil.Format(NewObjectStrings.MemberNotFound, null, property.Key.ToString(), ParameterSet2ResourceString(ParameterSetName));

            ThrowTerminatingError(
                new ErrorRecord(
                    new InvalidOperationException(message),
                    "InvalidOperationException",
                    ErrorCategory.InvalidOperation,
                    null));
        }

        private void CreateMemberSetValueError(SetValueException e)
        {
            Exception ex = new Exception(StringUtil.Format(NewObjectStrings.InvalidValue, e));
            ThrowTerminatingError(
                new ErrorRecord(ex, "SetValueException", ErrorCategory.InvalidData, null));
        }

        private static string ParameterSet2ResourceString(string parameterSet)
        {
            if (parameterSet.Equals(netSetName, StringComparison.OrdinalIgnoreCase))
            {
                return ".NET";
            }
            else if (parameterSet.Equals("Com", StringComparison.OrdinalIgnoreCase))
            {
                return "COM";
            }
            else
            {
                Dbg.Assert(false, "Should never get here - unknown parameter set");
                return parameterSet;
            }
        }

        #endregion private

        #region Overrides
        /// <summary> Create the object </summary>
        protected override void BeginProcessing()
        {
            Type type = null;
            PSArgumentException mshArgE = null;

            if (string.Compare(ParameterSetName, netSetName, StringComparison.Ordinal) == 0)
            {
                object _newObject = null;
                try
                {
                    type = LanguagePrimitives.ConvertTo(TypeName, typeof(Type), CultureInfo.InvariantCulture) as Type;
                }
                catch (Exception e)
                {
                    // these complications in Exception handling are aim to make error messages better.
                    if (e is InvalidCastException || e is ArgumentException)
                    {
                        if (e.InnerException != null && e.InnerException is TypeResolver.AmbiguousTypeException)
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                    e,
                                    "AmbiguousTypeReference",
                                    ErrorCategory.InvalidType,
                                    targetObject: null));
                        }

                        mshArgE = PSTraceSource.NewArgumentException(
                            "TypeName",
                            NewObjectStrings.TypeNotFound,
                            TypeName);

                        ThrowTerminatingError(
                            new ErrorRecord(
                                mshArgE,
                                "TypeNotFound",
                                ErrorCategory.InvalidType,
                                targetObject: null));
                    }

                    throw e;
                }

                Diagnostics.Assert(type != null, "LanguagePrimitives.TryConvertTo failed but returned true");

                if (type.IsByRefLike)
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                            PSTraceSource.NewInvalidOperationException(
                                NewObjectStrings.CannotInstantiateBoxedByRefLikeType,
                                type),
                            nameof(NewObjectStrings.CannotInstantiateBoxedByRefLikeType),
                            ErrorCategory.InvalidOperation,
                            targetObject: null));
                }

                if (Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                {
                    if (!CoreTypes.Contains(type))
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new PSNotSupportedException(NewObjectStrings.CannotCreateTypeConstrainedLanguage), "CannotCreateTypeConstrainedLanguage", ErrorCategory.PermissionDenied, null));
                    }
                }

                // WinRT does not support creating instances of attribute & delegate WinRT types.
                if (WinRTHelper.IsWinRTType(type) && ((typeof(System.Attribute)).IsAssignableFrom(type) || (typeof(System.Delegate)).IsAssignableFrom(type)))
                {
                    ThrowTerminatingError(new ErrorRecord(new InvalidOperationException(NewObjectStrings.CannotInstantiateWinRTType),
                        "CannotInstantiateWinRTType", ErrorCategory.InvalidOperation, null));
                }

                if (ArgumentList == null || ArgumentList.Length == 0)
                {
                    ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
                    if (ci != null && ci.IsPublic)
                    {
                        _newObject = CallConstructor(type, new ConstructorInfo[] { ci }, new object[] { });
                        if (_newObject != null && Property != null)
                        {
                            // The method invocation is disabled for "Hashtable to Object conversion" (Win8:649519), but we need to keep it enabled for New-Object for compatibility to PSv2
                            _newObject = LanguagePrimitives.SetObjectProperties(_newObject, Property, type, CreateMemberNotFoundError, CreateMemberSetValueError, enableMethodCall: true);
                        }

                        WriteObject(_newObject);
                        return;
                    }
                    else if (type.GetTypeInfo().IsValueType)
                    {
                        // This is for default parameterless struct ctor which is not returned by
                        // Type.GetConstructor(System.Type.EmptyTypes).
                        try
                        {
                            _newObject = Activator.CreateInstance(type);
                            if (_newObject != null && Property != null)
                            {
                                // Win8:649519
                                _newObject = LanguagePrimitives.SetObjectProperties(_newObject, Property, type, CreateMemberNotFoundError, CreateMemberSetValueError, enableMethodCall: true);
                            }
                        }
                        catch (TargetInvocationException e)
                        {
                            ThrowTerminatingError(
                                new ErrorRecord(
                                e.InnerException ?? e,
                                "ConstructorCalledThrowException",
                                ErrorCategory.InvalidOperation, null));
                        }

                        WriteObject(_newObject);
                        return;
                    }
                }
                else
                {
                    ConstructorInfo[] ctorInfos = type.GetConstructors();

                    if (ctorInfos.Length != 0)
                    {
                        _newObject = CallConstructor(type, ctorInfos, ArgumentList);
                        if (_newObject != null && Property != null)
                        {
                            // Win8:649519
                            _newObject = LanguagePrimitives.SetObjectProperties(_newObject, Property, type, CreateMemberNotFoundError, CreateMemberSetValueError, enableMethodCall: true);
                        }

                        WriteObject(_newObject);
                        return;
                    }
                }

                mshArgE = PSTraceSource.NewArgumentException(
                    "TypeName", NewObjectStrings.CannotFindAppropriateCtor, TypeName);
                ThrowTerminatingError(
                  new ErrorRecord(
                      mshArgE,
                     "CannotFindAppropriateCtor",
                     ErrorCategory.ObjectNotFound, null));
            }
#if !UNIX
            else // Parameterset -Com
            {
                int result = NewObjectNativeMethods.CLSIDFromProgID(ComObject, out _comObjectClsId);

                // If we're in ConstrainedLanguage, do additional restrictions
                if (Context.LanguageMode == PSLanguageMode.ConstrainedLanguage)
                {
                    bool isAllowed = false;

                    // If it's a system-wide lockdown, we may allow additional COM types
                    if (SystemPolicy.GetSystemLockdownPolicy() == SystemEnforcementMode.Enforce)
                    {
                        if ((result >= 0) &&
                            SystemPolicy.IsClassInApprovedList(_comObjectClsId))
                        {
                            isAllowed = true;
                        }
                    }

                    if (!isAllowed)
                    {
                        ThrowTerminatingError(
                            new ErrorRecord(
                                new PSNotSupportedException(NewObjectStrings.CannotCreateTypeConstrainedLanguage), "CannotCreateComTypeConstrainedLanguage", ErrorCategory.PermissionDenied, null));
                        return;
                    }
                }

                object comObject = CreateComObject();
                string comObjectTypeName = comObject.GetType().FullName;
                if (!comObjectTypeName.Equals("System.__ComObject"))
                {
                    mshArgE = PSTraceSource.NewArgumentException(
                        "TypeName", NewObjectStrings.ComInteropLoaded, comObjectTypeName);
                    WriteVerbose(mshArgE.Message);
                    if (Strict)
                    {
                        WriteError(new ErrorRecord(
                          mshArgE,
                         "ComInteropLoaded",
                         ErrorCategory.InvalidArgument, comObject));
                    }
                }

                if (comObject != null && Property != null)
                {
                    // Win8:649519
                    comObject = LanguagePrimitives.SetObjectProperties(comObject, Property, type, CreateMemberNotFoundError, CreateMemberSetValueError, enableMethodCall: true);
                }

                WriteObject(comObject);
            }
#endif
        }

        #endregion Overrides

#if !UNIX
        #region Com

        private object SafeCreateInstance(Type t, object[] args)
        {
            object result = null;
            try
            {
                result = Activator.CreateInstance(t, args);
            }
            // Does not catch InvalidComObjectException because ComObject is obtained from GetTypeFromProgID
            catch (ArgumentException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "CannotNewNonRuntimeType",
                    ErrorCategory.InvalidOperation, null));
            }
            catch (NotSupportedException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "CannotNewTypeBuilderTypedReferenceArgIteratorRuntimeArgumentHandle",
                    ErrorCategory.InvalidOperation, null));
            }
            catch (MethodAccessException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "CtorAccessDenied",
                    ErrorCategory.PermissionDenied, null));
            }
            catch (MissingMethodException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "NoPublicCtorMatch",
                    ErrorCategory.InvalidOperation, null));
            }
            catch (MemberAccessException e)
            {
                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "CannotCreateAbstractClass",
                    ErrorCategory.InvalidOperation, null));
            }
            catch (COMException e)
            {
                if (e.HResult == RPC_E_CHANGED_MODE)
                {
                    throw;
                }

                ThrowTerminatingError(
                    new ErrorRecord(
                    e,
                    "NoCOMClassIdentified",
                    ErrorCategory.ResourceUnavailable, null));
            }

            return result;
        }

#if !CORECLR
        private class ComCreateInfo
        {
            public object objectCreated;
            public bool success;
            public Exception e;
        }

        private ComCreateInfo createInfo;

        private void STAComCreateThreadProc(Object createstruct)
        {
            ComCreateInfo info = (ComCreateInfo)createstruct;
            try
            {
                Type type = null;
                PSArgumentException mshArgE = null;

                type = Type.GetTypeFromCLSID(_comObjectClsId);
                if (type == null)
                {
                    mshArgE = PSTraceSource.NewArgumentException(
                        "ComObject",
                        NewObjectStrings.CannotLoadComObjectType,
                        ComObject);

                    info.e = mshArgE;
                    info.success = false;
                    return;
                }

                info.objectCreated = SafeCreateInstance(type, ArgumentList);
                info.success = true;
            }
            catch (Exception e)
            {
                info.e = e;
                info.success = false;
            }
        }
#endif

        private object CreateComObject()
        {
            Type type = null;
            PSArgumentException mshArgE = null;

            try
            {
                type = Marshal.GetTypeFromCLSID(_comObjectClsId);
                if (type == null)
                {
                    mshArgE = PSTraceSource.NewArgumentException("ComObject", NewObjectStrings.CannotLoadComObjectType, ComObject);
                    ThrowTerminatingError(
                        new ErrorRecord(mshArgE, "CannotLoadComObjectType", ErrorCategory.InvalidType, null));
                }

                return SafeCreateInstance(type, ArgumentList);
            }
            catch (COMException e)
            {
                // Check Error Code to see if Error is because of Com apartment Mismatch.
                if (e.HResult == RPC_E_CHANGED_MODE)
                {
#if CORECLR
                    ThrowTerminatingError(
                        new ErrorRecord(
                            new COMException(StringUtil.Format(NewObjectStrings.ApartmentNotSupported, e.Message), e),
                            "NoCOMClassIdentified",
                            ErrorCategory.ResourceUnavailable, null));
#else
                    createInfo = new ComCreateInfo();

                    Thread thread = new Thread(new ParameterizedThreadStart(STAComCreateThreadProc));
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start(createInfo);

                    thread.Join();

                    if (createInfo.success == true)
                    {
                        return createInfo.objectCreated;
                    }

                    ThrowTerminatingError(
                             new ErrorRecord(createInfo.e, "NoCOMClassIdentified",
                                                    ErrorCategory.ResourceUnavailable, null));
#endif
                }
                else
                {
                    ThrowTerminatingError(
                        new ErrorRecord(
                        e,
                        "NoCOMClassIdentified",
                        ErrorCategory.ResourceUnavailable, null));
                }

                return null;
            }
        }

        #endregion Com
#endif

        // HResult code '-2147417850' - Cannot change thread mode after it is set.
        private const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);
        private const string netSetName = "Net";
    }

    /// <summary>
    /// Native methods for dealing with COM objects.
    /// </summary>
    internal class NewObjectNativeMethods
    {
        private NewObjectNativeMethods()
        {
        }

        /// Return Type: HRESULT->LONG->int
        [DllImport(PinvokeDllNames.CLSIDFromProgIDDllName)]
        internal static extern int CLSIDFromProgID([MarshalAs(UnmanagedType.LPWStr)] string lpszProgID, out Guid pclsid);
    }
}
