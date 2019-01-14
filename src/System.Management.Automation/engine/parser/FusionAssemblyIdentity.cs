// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

#if !CORECLR
namespace Microsoft.CodeAnalysis
{
    internal sealed class FusionAssemblyIdentity
    {
        [Flags]
        internal enum ASM_DISPLAYF
        {
            VERSION = 0x01,
            CULTURE = 0x02,
            PUBLIC_KEY_TOKEN = 0x04,
            PUBLIC_KEY = 0x08,
            CUSTOM = 0x10,
            PROCESSORARCHITECTURE = 0x20,
            LANGUAGEID = 0x40,
            RETARGET = 0x80,
            CONFIG_MASK = 0x100,
            MVID = 0x200,
            CONTENT_TYPE = 0x400,
            FULL = VERSION | CULTURE | PUBLIC_KEY_TOKEN | RETARGET | PROCESSORARCHITECTURE | CONTENT_TYPE
        }

        internal enum PropertyId
        {
            PUBLIC_KEY = 0,        // 0
            PUBLIC_KEY_TOKEN,      // 1
            HASH_VALUE,            // 2
            NAME,                  // 3
            MAJOR_VERSION,         // 4
            MINOR_VERSION,         // 5
            BUILD_NUMBER,          // 6
            REVISION_NUMBER,       // 7
            CULTURE,               // 8
            PROCESSOR_ID_ARRAY,    // 9
            OSINFO_ARRAY,          // 10
            HASH_ALGID,            // 11
            ALIAS,                 // 12
            CODEBASE_URL,          // 13
            CODEBASE_LASTMOD,      // 14
            NULL_PUBLIC_KEY,       // 15
            NULL_PUBLIC_KEY_TOKEN, // 16
            CUSTOM,                // 17
            NULL_CUSTOM,           // 18
            MVID,                  // 19
            FILE_MAJOR_VERSION,    // 20
            FILE_MINOR_VERSION,    // 21
            FILE_BUILD_NUMBER,     // 22
            FILE_REVISION_NUMBER,  // 23
            RETARGET,              // 24
            SIGNATURE_BLOB,        // 25
            CONFIG_MASK,           // 26
            ARCHITECTURE,          // 27
            CONTENT_TYPE,          // 28
            MAX_PARAMS             // 29
        }

        private static class CANOF
        {
            public const uint PARSE_DISPLAY_NAME = 0x1;
            public const uint SET_DEFAULT_VALUES = 0x2;
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("CD193BC0-B4BC-11d2-9833-00C04FC31D2E")]
        internal unsafe interface IAssemblyName
        {
            void SetProperty(PropertyId id, void* data, uint size);

            [PreserveSig]
            int GetProperty(PropertyId id, void* data, ref uint size);

            [PreserveSig]
            int Finalize();

            [PreserveSig]
            int GetDisplayName(byte* buffer, ref uint characterCount, ASM_DISPLAYF dwDisplayFlags);

            [PreserveSig]
            int __BindToObject(/*...*/);

            [PreserveSig]
            int __GetName(/*...*/);

            [PreserveSig]
            int GetVersion(out uint versionHi, out uint versionLow);

            [PreserveSig]
            int IsEqual(IAssemblyName pName, uint dwCmpFlags);

            [PreserveSig]
            int Clone(out IAssemblyName pName);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("7c23ff90-33af-11d3-95da-00a024a85b51")]
        internal interface IApplicationContext
        {
        }

        // NOTE: The CLR caches assembly identities, but doesn't do so in a threadsafe manner.
        // Wrap all calls to this with a lock.
        private static object s_assemblyIdentityGate = new object();
        private static int CreateAssemblyNameObject(out IAssemblyName ppEnum, string szAssemblyName, uint dwFlags, IntPtr pvReserved)
        {
            lock (s_assemblyIdentityGate)
            {
                return RealCreateAssemblyNameObject(out ppEnum, szAssemblyName, dwFlags, pvReserved);
            }
        }

        [DllImport("clr", EntryPoint = "CreateAssemblyNameObject", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int RealCreateAssemblyNameObject(out IAssemblyName ppEnum, [MarshalAs(UnmanagedType.LPWStr)]string szAssemblyName, uint dwFlags, IntPtr pvReserved);

        private const int ERROR_INSUFFICIENT_BUFFER = unchecked((int)0x8007007A);
        private const int FUSION_E_INVALID_NAME = unchecked((int)0x80131047);

        internal static unsafe string GetDisplayName(IAssemblyName nameObject, ASM_DISPLAYF displayFlags)
        {
            int hr;
            uint characterCountIncludingTerminator = 0;

            hr = nameObject.GetDisplayName(null, ref characterCountIncludingTerminator, displayFlags);
            if (hr == 0)
            {
                return string.Empty;
            }

            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[(int)characterCountIncludingTerminator * 2];
            fixed (byte* p = data)
            {
                hr = nameObject.GetDisplayName(p, ref characterCountIncludingTerminator, displayFlags);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }

                return Marshal.PtrToStringUni((IntPtr)p, (int)characterCountIncludingTerminator - 1);
            }
        }

        internal static unsafe byte[] GetPropertyBytes(IAssemblyName nameObject, PropertyId propertyId)
        {
            int hr;
            uint size = 0;

            hr = nameObject.GetProperty(propertyId, null, ref size);
            if (hr == 0)
            {
                return null;
            }

            if (hr != ERROR_INSUFFICIENT_BUFFER)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            byte[] data = new byte[(int)size];
            fixed (byte* p = data)
            {
                hr = nameObject.GetProperty(propertyId, p, ref size);
                if (hr != 0)
                {
                    throw Marshal.GetExceptionForHR(hr);
                }
            }

            return data;
        }

        internal static unsafe string GetPropertyString(IAssemblyName nameObject, PropertyId propertyId)
        {
            byte[] data = GetPropertyBytes(nameObject, propertyId);
            if (data == null)
            {
                return null;
            }

            fixed (byte* p = data)
            {
                return Marshal.PtrToStringUni((IntPtr)p, (data.Length / 2) - 1);
            }
        }

        internal static unsafe Version GetVersion(IAssemblyName nameObject)
        {
            uint hi, lo;
            int hr = nameObject.GetVersion(out hi, out lo);
            if (hr != 0)
            {
                Debug.Assert(hr == FUSION_E_INVALID_NAME);
                return null;
            }

            return new Version((int)(hi >> 16), (int)(hi & 0xffff), (int)(lo >> 16), (int)(lo & 0xffff));
        }

        internal static unsafe uint? GetPropertyWord(IAssemblyName nameObject, PropertyId propertyId)
        {
            uint result;
            uint size = sizeof(uint);
            int hr = nameObject.GetProperty(propertyId, &result, ref size);
            if (hr != 0)
            {
                throw Marshal.GetExceptionForHR(hr);
            }

            if (size == 0)
            {
                return null;
            }

            return result;
        }

        internal static string GetCulture(IAssemblyName nameObject)
        {
            return GetPropertyString(nameObject, PropertyId.CULTURE);
        }

        internal static ProcessorArchitecture GetProcessorArchitecture(IAssemblyName nameObject)
        {
            return (ProcessorArchitecture)(GetPropertyWord(nameObject, PropertyId.ARCHITECTURE) ?? 0);
        }

        /// <summary>
        /// Creates <see cref="IAssemblyName"/> object by parsing given display name.
        /// </summary>
        internal static IAssemblyName ToAssemblyNameObject(string displayName)
        {
            // CLR doesn't handle \0 in the display name well:
            if (displayName.IndexOf('\0') >= 0)
            {
                return null;
            }

            Debug.Assert(displayName != null);
            IAssemblyName result;
            int hr = CreateAssemblyNameObject(out result, displayName, CANOF.PARSE_DISPLAY_NAME, IntPtr.Zero);
            if (hr != 0)
            {
                return null;
            }

            Debug.Assert(result != null);
            return result;
        }

        /// <summary>
        /// Selects the candidate assembly with the largest version number.  Uses culture as a tie-breaker if it is provided.
        /// All candidates are assumed to have the same name and must include versions and cultures.
        /// </summary>
        internal static IAssemblyName GetBestMatch(IEnumerable<IAssemblyName> candidates, string preferredCultureOpt)
        {
            IAssemblyName bestCandidate = null;
            Version bestVersion = null;
            string bestCulture = null;
            foreach (var candidate in candidates)
            {
                if (bestCandidate != null)
                {
                    Version candidateVersion = GetVersion(candidate);
                    Debug.Assert(candidateVersion != null);

                    if (bestVersion == null)
                    {
                        bestVersion = GetVersion(bestCandidate);
                        Debug.Assert(bestVersion != null);
                    }

                    int cmp = bestVersion.CompareTo(candidateVersion);
                    if (cmp == 0)
                    {
                        if (preferredCultureOpt != null)
                        {
                            string candidateCulture = GetCulture(candidate);
                            Debug.Assert(candidateCulture != null);

                            if (bestCulture == null)
                            {
                                bestCulture = GetCulture(candidate);
                                Debug.Assert(bestCulture != null);
                            }

                            // we have exactly the preferred culture or
                            // we have neutral culture and the best candidate's culture isn't the preferred one:
                            if (StringComparer.OrdinalIgnoreCase.Equals(candidateCulture, preferredCultureOpt) ||
                                candidateCulture.Length == 0 && !StringComparer.OrdinalIgnoreCase.Equals(bestCulture, preferredCultureOpt))
                            {
                                bestCandidate = candidate;
                                bestVersion = candidateVersion;
                                bestCulture = candidateCulture;
                            }
                        }
                    }
                    else if (cmp < 0)
                    {
                        bestCandidate = candidate;
                        bestVersion = candidateVersion;
                    }
                }
                else
                {
                    bestCandidate = candidate;
                }
            }

            return bestCandidate;
        }
    }
}
#endif // !CORECLR
