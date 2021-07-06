// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Management.Automation;
using System.Net;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Xml;

using Microsoft.Management.Infrastructure;

using Dbg = System.Management.Automation.Diagnostics;

// TODO/FIXME: Move this class to src/cimSupport/other directory (to map to the namespace it lives in and functionality it implements [cmdletization independent])

namespace Microsoft.PowerShell.Cim
{
    internal class CimSensitiveValueConverter : IDisposable
    {
        private sealed class SensitiveString : IDisposable
        {
            private GCHandle _gcHandle;
            private string _string;

            public string Value { get { return _string; } }

            internal SensitiveString(int numberOfCharacters)
            {
                _string = new string('\0', numberOfCharacters);

                Debug.Assert(
                    string.IsInterned(_string) == null,
                    "We will overwrite string contents - we can't / shouldn't do this for interned strings.");

                /* The string is pinned (while still being filled with insignificant data)
                 * to prevent copying of sensitive data by garbage collection.
                 * This allows the sensitive data to be cleaned-up from SensitiveString.Dispose method.
                 */
                _gcHandle = GCHandle.Alloc(_string, GCHandleType.Pinned);
            }

            private unsafe void Copy(char* source, int offset, int charsToCopy)
            {
                if ((offset < 0) || (offset >= _string.Length))
                {
                    throw new ArgumentOutOfRangeException(nameof(offset));
                }

                if (offset + charsToCopy > _string.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(charsToCopy));
                }

                fixed (char* target = _string)
                {
                    for (int i = 0; i < charsToCopy; i++)
                    {
                        target[offset + i] = source[i];
                    }
                }
            }

            internal void Copy(string source, int offset)
            {
                unsafe
                {
                    fixed (char* sourcePointer = source)
                    {
                        Copy(sourcePointer, offset, source.Length);
                    }
                }
            }

            internal void Copy(SecureString source, int offset)
            {
                IntPtr plainTextString = Marshal.SecureStringToCoTaskMemUnicode(source);
                try
                {
                    unsafe
                    {
                        Copy((char*)plainTextString, offset, source.Length);
                    }
                }
                finally
                {
                    Marshal.ZeroFreeCoTaskMemUnicode(plainTextString);
                }
            }

            ~SensitiveString()
            {
                Dispose(false);
            }

            /// <summary>
            /// Releases resources associated with this object.
            /// </summary>
            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Releases resources associated with this object.
            /// </summary>
            private void Dispose(bool disposing)
            {
                if (_string == null)
                {
                    return;
                }

                /* this clobbers sensitive data */
                Copy(new string('\0', _string.Length), 0);

                /* this allows garbage collector to move and/or free the string */
                _gcHandle.Free();
                _string = null;
            }
        }

        private readonly List<IDisposable> _trackedDisposables = new();

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources associated with this object.
        /// </summary>
        private void Dispose(bool disposing)
        {
            lock (_trackedDisposables)
            {
                foreach (var d in _trackedDisposables)
                {
                    d.Dispose();
                }

                _trackedDisposables.Clear();
            }
        }

        private const string PSCredentialDelimiter = ":";

        internal object ConvertFromDotNetToCim(object dotNetObject)
        {
            if (dotNetObject == null)
            {
                return null;
            }

            PSObject psObject = PSObject.AsPSObject(dotNetObject);
            Type dotNetType = psObject.BaseObject.GetType();
            if (typeof(PSCredential).IsAssignableFrom(dotNetType))
            {
                var credential = (PSCredential)(psObject.BaseObject);

                string escapedUsername = credential.UserName;
                escapedUsername = escapedUsername.Replace("\\", "\\\\"); // Esc backslashes
                escapedUsername = escapedUsername.Replace(PSCredentialDelimiter, "\\" + PSCredentialDelimiter);

                var sensitiveString = new SensitiveString(escapedUsername.Length + PSCredentialDelimiter.Length + credential.Password.Length);
                lock (_trackedDisposables) { _trackedDisposables.Add(sensitiveString); }

                sensitiveString.Copy(escapedUsername, 0);
                sensitiveString.Copy(PSCredentialDelimiter, escapedUsername.Length);
                sensitiveString.Copy(credential.Password, escapedUsername.Length + PSCredentialDelimiter.Length);
                return sensitiveString.Value;
            }

            if (typeof(SecureString).IsAssignableFrom(dotNetType))
            {
                SecureString secureString = (SecureString)psObject.BaseObject;
                var sensitiveString = new SensitiveString(secureString.Length);
                lock (_trackedDisposables) { _trackedDisposables.Add(sensitiveString); }

                sensitiveString.Copy(secureString, 0);
                return sensitiveString.Value;
            }

            if (dotNetType.IsArray)
            {
                Type dotNetElementType = CimValueConverter.GetElementType(dotNetType);
                if (dotNetElementType != null)
                {
                    var dotNetArray = (Array)psObject.BaseObject;
                    Type cimElementType = GetCimType(dotNetElementType);
                    Array cimArray = Array.CreateInstance(cimElementType, dotNetArray.Length);
                    for (int i = 0; i < cimArray.Length; i++)
                    {
                        object cimElement = ConvertFromDotNetToCim(dotNetArray.GetValue(i));
                        cimArray.SetValue(cimElement, i);
                    }

                    return cimArray;
                }
            }

            return CimValueConverter.ConvertFromDotNetToCim(dotNetObject);
        }

        internal static Type GetCimType(Type dotNetType)
        {
            if (dotNetType == typeof(SecureString))
            {
                return typeof(string);
            }

            if (dotNetType == typeof(PSCredential))
            {
                return typeof(string);
            }

            return CimValueConverter.GetCimType(dotNetType);
        }
    }

    internal static class CimValueConverter
    {
        /// <exception cref="PSInvalidCastException">The only kind of exception this method can throw.</exception>
        internal static object ConvertFromDotNetToCim(object dotNetObject)
        {
            if (dotNetObject == null)
            {
                return null;
            }

            PSObject psObject = PSObject.AsPSObject(dotNetObject);
            Type dotNetType = psObject.BaseObject.GetType();
            Dbg.Assert(
                !(dotNetType.GetTypeInfo().IsGenericType && dotNetType.GetGenericTypeDefinition() == typeof(Nullable<>)),
                "GetType on a boxed object should never return Nullable<T>");

            if (LanguagePrimitives.IsCimIntrinsicScalarType(dotNetType))
            {
                return psObject.BaseObject;
            }

            if (typeof(CimInstance).IsAssignableFrom(dotNetType))
            {
                return psObject.BaseObject;
            }

            if (typeof(PSReference).IsAssignableFrom(dotNetType))
            {
                PSReference psReference = (PSReference)psObject.BaseObject;
                if (psReference.Value == null)
                {
                    return null;
                }
                else
                {
                    PSObject innerPso = PSObject.AsPSObject(psReference.Value);
                    return ConvertFromDotNetToCim(innerPso.BaseObject);
                }
            }

            if (dotNetType.IsArray)
            {
                Type dotNetElementType = GetElementType(dotNetType);
                if (dotNetElementType != null)
                {
                    var dotNetArray = (Array)psObject.BaseObject;
                    Type cimElementType = CimValueConverter.GetCimType(dotNetElementType);
                    Array cimArray = Array.CreateInstance(cimElementType, dotNetArray.Length);
                    for (int i = 0; i < cimArray.Length; i++)
                    {
                        object cimElement = ConvertFromDotNetToCim(dotNetArray.GetValue(i));
                        cimArray.SetValue(cimElement, i);
                    }

                    return cimArray;
                }
            }

            Type convertibleCimType = GetConvertibleCimType(dotNetType);
            if (convertibleCimType != null)
            {
                object cimIntrinsicValue = LanguagePrimitives.ConvertTo(dotNetObject, convertibleCimType, CultureInfo.InvariantCulture);
                return cimIntrinsicValue;
            }

            if (typeof(ObjectSecurity).IsAssignableFrom(dotNetType))
            {
                string cimIntrinsicValue = Microsoft.PowerShell.Commands.SecurityDescriptorCommandsBase.GetSddl(psObject);
                return cimIntrinsicValue;
            }

            if (typeof(X509Certificate2).IsAssignableFrom(dotNetType))
            {
                var cert = (X509Certificate2)(psObject.BaseObject);
                byte[] cimIntrinsicValue = cert.RawData;
                return cimIntrinsicValue;
            }

            if (typeof(X500DistinguishedName).IsAssignableFrom(dotNetType))
            {
                var x500name = (X500DistinguishedName)(psObject.BaseObject);
                byte[] cimIntrinsicValue = x500name.RawData;
                return cimIntrinsicValue;
            }

            if (typeof(PhysicalAddress).IsAssignableFrom(dotNetType))
            {
                object cimIntrinsicValue = LanguagePrimitives.ConvertTo(dotNetObject, typeof(string), CultureInfo.InvariantCulture);
                return cimIntrinsicValue;
            }

            if (typeof(IPEndPoint).IsAssignableFrom(dotNetType))
            {
                object cimIntrinsicValue = LanguagePrimitives.ConvertTo(dotNetObject, typeof(string), CultureInfo.InvariantCulture);
                return cimIntrinsicValue;
            }

            if (typeof(WildcardPattern).IsAssignableFrom(dotNetType))
            {
                var wildcardPattern = (WildcardPattern)(psObject.BaseObject);
                return wildcardPattern.ToWql();
            }

            if (typeof(XmlDocument).IsAssignableFrom(dotNetType))
            {
                var xmlDocument = (XmlDocument)(psObject.BaseObject);
                string cimIntrinsicValue = xmlDocument.OuterXml;
                return cimIntrinsicValue;
            }

            // unrecognized type = throw invalid cast exception
            throw CimValueConverter.GetInvalidCastException(
                null, /* inner exception */
                "InvalidDotNetToCimCast",
                dotNetObject,
                CmdletizationResources.CimConversion_CimIntrinsicValue);
        }

        /// <exception cref="PSInvalidCastException">The only kind of exception this method can throw.</exception>
        internal static object ConvertFromCimToDotNet(object cimObject, Type expectedDotNetType)
        {
            if (expectedDotNetType == null) { throw new ArgumentNullException(nameof(expectedDotNetType)); }

            if (cimObject == null)
            {
                return null;
            }

            if (expectedDotNetType.GetTypeInfo().IsGenericType && expectedDotNetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                expectedDotNetType = expectedDotNetType.GetGenericArguments()[0];
            }

            if (LanguagePrimitives.IsCimIntrinsicScalarType(expectedDotNetType))
            {
                return LanguagePrimitives.ConvertTo(cimObject, expectedDotNetType, CultureInfo.InvariantCulture);
            }

            if (expectedDotNetType == typeof(CimInstance))
            {
                return LanguagePrimitives.ConvertTo(cimObject, expectedDotNetType, CultureInfo.InvariantCulture);
            }

            if (expectedDotNetType.IsArray)
            {
                Type dotNetElementType = GetElementType(expectedDotNetType);
                if (dotNetElementType != null)
                {
                    var cimArray = (Array)LanguagePrimitives.ConvertTo(cimObject, typeof(Array), CultureInfo.InvariantCulture);
                    Array dotNetArray = Array.CreateInstance(dotNetElementType, cimArray.Length);
                    for (int i = 0; i < dotNetArray.Length; i++)
                    {
                        object dotNetElement = ConvertFromCimToDotNet(cimArray.GetValue(i), dotNetElementType);
                        dotNetArray.SetValue(dotNetElement, i);
                    }

                    return dotNetArray;
                }
            }

            Type convertibleCimType = GetConvertibleCimType(expectedDotNetType);
            if (convertibleCimType != null)
            {
                object cimIntrinsicValue = LanguagePrimitives.ConvertTo(cimObject, convertibleCimType, CultureInfo.InvariantCulture);
                object dotNetObject = LanguagePrimitives.ConvertTo(cimIntrinsicValue, expectedDotNetType, CultureInfo.InvariantCulture);
                return dotNetObject;
            }

            Func<Func<object>, object> exceptionSafeReturn = (Func<object> innerAction) =>
            {
                try
                {
                    return innerAction();
                }
                catch (Exception e)
                {
                    throw CimValueConverter.GetInvalidCastException(
                        e,
                        "InvalidCimToDotNetCast",
                        cimObject,
                        expectedDotNetType.FullName);
                }
            };

            if (typeof(ObjectSecurity).IsAssignableFrom(expectedDotNetType))
            {
                var sddl = (string)LanguagePrimitives.ConvertTo(cimObject, typeof(string), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   var objectSecurity = (ObjectSecurity)Activator.CreateInstance(expectedDotNetType);
                                                   objectSecurity.SetSecurityDescriptorSddlForm(sddl);
                                                   return objectSecurity;
                                               });
            }

            if (typeof(X509Certificate2) == expectedDotNetType)
            {
                var cimIntrinsicValue = (byte[])LanguagePrimitives.ConvertTo(cimObject, typeof(byte[]), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   return new X509Certificate2(cimIntrinsicValue);
                                               });
            }

            if (typeof(X500DistinguishedName) == expectedDotNetType)
            {
                var cimIntrinsicValue = (byte[])LanguagePrimitives.ConvertTo(cimObject, typeof(byte[]), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   return new X500DistinguishedName(cimIntrinsicValue);
                                               });
            }

            if (typeof(PhysicalAddress) == expectedDotNetType)
            {
                var cimIntrinsicValue = (string)LanguagePrimitives.ConvertTo(cimObject, typeof(string), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   return PhysicalAddress.Parse(cimIntrinsicValue);
                                               });
            }

            if (typeof(IPEndPoint) == expectedDotNetType)
            {
                var cimIntrinsicValue = (string)LanguagePrimitives.ConvertTo(cimObject, typeof(string), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   int indexOfLastColon = cimIntrinsicValue.LastIndexOf(':');
                                                   int port = int.Parse(cimIntrinsicValue.AsSpan(indexOfLastColon + 1), NumberStyles.Integer, CultureInfo.InvariantCulture);
                                                   IPAddress address = IPAddress.Parse(cimIntrinsicValue.AsSpan(0, indexOfLastColon));
                                                   return new IPEndPoint(address, port);
                                               });
            }

            // WildcardPattern is only supported as an "in" parameter - we do not support the reverse translation (i.e. from "a%" to "a*")

            if (typeof(XmlDocument) == expectedDotNetType)
            {
                var cimIntrinsicValue = (string)LanguagePrimitives.ConvertTo(cimObject, typeof(string), CultureInfo.InvariantCulture);
                return exceptionSafeReturn(delegate
                                               {
                                                   XmlDocument doc = InternalDeserializer.LoadUnsafeXmlDocument(
                                                       cimIntrinsicValue,
                                                       true, /* preserve non elements: whitespace, processing instructions, comments, etc. */
                                                       null); /* default maxCharactersInDocument */
                                                   return doc;
                                               });
            }

            // unrecognized type = throw invalid cast exception
            throw CimValueConverter.GetInvalidCastException(
                null, /* inner exception */
                "InvalidCimToDotNetCast",
                cimObject,
                expectedDotNetType.FullName);
        }

        internal static CimType GetCimTypeEnum(Type dotNetType)
        {
            Dbg.Assert(dotNetType != null, "Caller should make sure that dotNetType != null");

            if (typeof(PSReference).IsAssignableFrom(dotNetType))
            {
                return CimType.Reference;
            }

            if (typeof(PSReference[]).IsAssignableFrom(dotNetType))
            {
                return CimType.ReferenceArray;
            }
            else
            {
                return CimConverter.GetCimType(dotNetType);
            }
        }

        internal static Type GetCimType(Type dotNetType)
        {
            if (dotNetType.IsArray)
            {
                return GetCimType(GetElementType(dotNetType)).MakeArrayType();
            }

            if (dotNetType.GetTypeInfo().IsGenericType && dotNetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return GetCimType(dotNetType.GetGenericArguments()[0]);
            }

            if (LanguagePrimitives.IsCimIntrinsicScalarType(dotNetType))
            {
                return dotNetType;
            }

            if (dotNetType == typeof(CimInstance))
            {
                return dotNetType;
            }

            if (dotNetType == typeof(PSReference))
            {
                return dotNetType;
            }

            Type result = CimValueConverter.GetConvertibleCimType(dotNetType);
            if (result != null)
            {
                return result;
            }

            if (typeof(ObjectSecurity).IsAssignableFrom(dotNetType))
            {
                return typeof(string);
            }

            if (typeof(X509Certificate2) == dotNetType)
            {
                return typeof(byte[]);
            }

            if (typeof(X500DistinguishedName) == dotNetType)
            {
                return typeof(byte[]);
            }

            if (typeof(PhysicalAddress) == dotNetType)
            {
                return typeof(string);
            }

            if (typeof(IPEndPoint) == dotNetType)
            {
                return typeof(string);
            }

            if (typeof(WildcardPattern) == dotNetType)
            {
                return typeof(string);
            }

            if (typeof(XmlDocument) == dotNetType)
            {
                return typeof(string);
            }

            if (typeof(PSCredential) == dotNetType)
            {
                return typeof(string);
            }

            Dbg.Assert(false, ".NET Type that is not supported in a .NET <-> CIM conversion");
            return null;
        }

        /// <summary>
        /// Returns a type of CIM representation if conversion from/to CIM can be done purely with LanguagePrimitives.ConvertTo.
        /// </summary>
        /// <param name="dotNetType"></param>
        /// <returns></returns>
        private static Type GetConvertibleCimType(Type dotNetType)
        {
            Dbg.Assert(
                !(dotNetType.GetTypeInfo().IsGenericType && dotNetType.GetGenericTypeDefinition() == typeof(Nullable<>)),
                "Caller should strip out Nullable<T> before calling CimValueConverter.GetConvertibleCimType");

            if (dotNetType.GetTypeInfo().IsEnum)
            {
                return Enum.GetUnderlyingType(dotNetType);
            }

            if (dotNetType == typeof(SwitchParameter))
            {
                return typeof(bool);
            }

            if (dotNetType == typeof(Guid) ||
                dotNetType == typeof(Uri) || // TODO/FIXME - CliXml does some magic around relative URIs - do we want to duplicate this?
                dotNetType == typeof(Version) ||
                dotNetType == typeof(IPAddress) ||
                dotNetType == typeof(MailAddress))
            {
                return typeof(string);
            }

            return null;
        }

        internal static Type GetElementType(Type arrayType)
        {
            Dbg.Assert(arrayType != null, "Caller should verify arrayType != null");
            Dbg.Assert(arrayType.IsArray, "Caller should verify arrayType.IsArray");

            // MOF syntax from Appendix A of DSP0004 doesn't allow expressing
            // of 1) nested arrays and 2) multi-dimensional arrays
            // (see production for "array" and how this production is used in
            //  other productions like "propertyDeclaration" or "parameter")
            if (arrayType.GetArrayRank() != 1)
            {
                return null;
            }

            Type elementType = arrayType.GetElementType();
            if (elementType.IsArray)
            {
                return null;
            }

            return elementType;
        }

        internal static PSInvalidCastException GetInvalidCastException(
            Exception innerException,
            string errorId,
            object sourceValue,
            string descriptionOfTargetType)
        {
            Dbg.Assert(!string.IsNullOrEmpty(errorId), "Caller should verify !string.IsNullOrEmpty(errorId)");
            Dbg.Assert(sourceValue != null, "Caller should verify sourceValue != null");
            Dbg.Assert(!string.IsNullOrEmpty(descriptionOfTargetType), "Caller should verify !string.IsNullOrEmpty(descriptionOfTargetType)");

            throw new PSInvalidCastException(
                errorId,
                innerException,
                ExtendedTypeSystem.InvalidCastException,
                sourceValue,
                PSObject.AsPSObject(sourceValue).BaseObject.GetType().FullName,
                descriptionOfTargetType);
        }

        [Conditional("DEBUG")]
        internal static void AssertIntrinsicCimValue(object value)
        {
            Dbg.Assert(value != null, "Caller should verify value != null");
            Type type = value.GetType();
            AssertIntrinsicCimType(type);
            Dbg.Assert(!typeof(PSReference).IsAssignableFrom(type) && typeof(PSReference[]) != type,
                       "PSReference cannot be used as a CIM *value* (PSReference is only ok as a type)");
        }

        [Conditional("DEBUG")]
        internal static void AssertIntrinsicCimType(Type type)
        {
            Dbg.Assert(type != null, "Caller should verify type != null");
            Dbg.Assert(
                LanguagePrimitives.IsCimIntrinsicScalarType(type) ||
                (type.IsArray && LanguagePrimitives.IsCimIntrinsicScalarType(type.GetElementType())) ||
                typeof(CimInstance).IsAssignableFrom(type) ||
                typeof(PSReference).IsAssignableFrom(type) ||
                type == typeof(CimInstance[]) ||
                type == typeof(PSReference[]),
                "Caller should verify that type is an intrinsic CIM type");
        }
    }
}
