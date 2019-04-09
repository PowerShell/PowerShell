// Copyright (c) Microsoft Corporation. All rights reserved. Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

#if !CORECLR // Only enable/port what is needed by CORE CLR.
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides APIs to enumerate and look up assemblies stored in the Global Assembly Cache.
    /// </summary>
    internal static class GlobalAssemblyCache
    {
        /// <summary>
        /// Represents the current Processor architecture.
        /// </summary>
        public static readonly ProcessorArchitecture[] CurrentArchitectures = (IntPtr.Size == 4)
            ? new[] { ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.X86 }

            : new[] { ProcessorArchitecture.None, ProcessorArchitecture.MSIL, ProcessorArchitecture.Amd64 };

#region Interop

        private const int MAX_PATH = 260;

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("21b8916c-f28e-11d2-a473-00c04f8ef448")]
        private interface IAssemblyEnum
        {
            [PreserveSig]
            int GetNextAssembly(out FusionAssemblyIdentity.IApplicationContext ppAppCtx, out FusionAssemblyIdentity.IAssemblyName ppName, uint dwFlags);

            [PreserveSig]
            int Reset();

            [PreserveSig]
            int Clone(out IAssemblyEnum ppEnum);
        }

        [ComImport, Guid("e707dcde-d1cd-11d2-bab9-00c04f8eceae"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAssemblyCache
        {
            void UninstallAssembly();

            void QueryAssemblyInfo(uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string pszAssemblyName, ref ASSEMBLY_INFO pAsmInfo);

            void CreateAssemblyCacheItem();
            void CreateAssemblyScavenger();
            void InstallAssembly();
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct ASSEMBLY_INFO
        {
            public uint cbAssemblyInfo;
            public uint dwAssemblyFlags;
            public ulong uliAssemblySizeInKB;
            public char* pszCurrentAssemblyPathBuf;
            public uint cchBuf;
        }

        private enum ASM_CACHE
        {
            ZAP = 0x1,
            GAC = 0x2,                // C:\Windows\Assembly\GAC
            DOWNLOAD = 0x4,
            ROOT = 0x8,               // C:\Windows\Assembly
            GAC_MSIL = 0x10,
            GAC_32 = 0x20,            // C:\Windows\Assembly\GAC_32
            GAC_64 = 0x40,            // C:\Windows\Assembly\GAC_64
            ROOT_EX = 0x80,           // C:\Windows\Microsoft.NET\assembly
        }

        [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = true)]
        private static extern int CreateAssemblyEnum(out IAssemblyEnum ppEnum, FusionAssemblyIdentity.IApplicationContext pAppCtx, FusionAssemblyIdentity.IAssemblyName pName, ASM_CACHE dwFlags, IntPtr pvReserved);

        [DllImport("clr", CharSet = CharSet.Auto, PreserveSig = false)]
        private static extern void CreateAssemblyCache(out IAssemblyCache ppAsmCache, uint dwReserved);

#endregion

        private const int S_OK = 0;
        private const int S_FALSE = 1;

        // Internal for testing.
        internal static IEnumerable<FusionAssemblyIdentity.IAssemblyName> GetAssemblyObjects(
            FusionAssemblyIdentity.IAssemblyName partialNameFilter,
            ProcessorArchitecture[] architectureFilter)
        {
            IAssemblyEnum enumerator;

            int hr = CreateAssemblyEnum(out enumerator, null, partialNameFilter, ASM_CACHE.GAC, IntPtr.Zero);
            if (hr == S_FALSE)
            {
                // no assembly found
                yield break;
            }

            if (hr != S_OK)
            {
                Exception e = Marshal.GetExceptionForHR(hr);
                if (e is FileNotFoundException)
                {
                    // invalid assembly name:
                    yield break;
                }

                if (e != null)
                {
                    throw e;
                }
                // for some reason it might happen that CreateAssemblyEnum returns non-zero HR that doesn't correspond to any exception:
                throw new ArgumentException("Invalid assembly name");
            }

            while (true)
            {
                FusionAssemblyIdentity.IAssemblyName nameObject;

                FusionAssemblyIdentity.IApplicationContext applicationContext;
                hr = enumerator.GetNextAssembly(out applicationContext, out nameObject, 0);
                if (hr != 0)
                {
                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    break;
                }

                if (architectureFilter != null)
                {
                    var assemblyArchitecture = FusionAssemblyIdentity.GetProcessorArchitecture(nameObject);
                    if (!architectureFilter.Contains(assemblyArchitecture))
                    {
                        continue;
                    }
                }

                yield return nameObject;
            }
        }

        /// <summary>
        /// Looks up specified partial assembly name in the GAC and returns the best matching full assembly name/>.
        /// </summary>
        /// <param name="displayName">The display name of an assembly.</param>
        /// <param name="location">Full path name of the resolved assembly.</param>
        /// <param name="architectureFilter">The optional processor architecture.</param>
        /// <param name="preferredCulture">The optional preferred culture information.</param>
        /// <returns>An assembly identity or null, if <paramref name="displayName"/> can't be resolved.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="displayName"/> is null.</exception>
        public static unsafe string ResolvePartialName(
            string displayName,
            out string location,
            ProcessorArchitecture[] architectureFilter = null,
            CultureInfo preferredCulture = null)
        {
            if (displayName == null)
            {
                throw new ArgumentNullException("displayName");
            }

            location = null;
            FusionAssemblyIdentity.IAssemblyName nameObject = FusionAssemblyIdentity.ToAssemblyNameObject(displayName);
            if (nameObject == null)
            {
                return null;
            }

            var candidates = GetAssemblyObjects(nameObject, architectureFilter);
            string cultureName = (preferredCulture != null && !preferredCulture.IsNeutralCulture) ? preferredCulture.Name : null;

            var bestMatch = FusionAssemblyIdentity.GetBestMatch(candidates, cultureName);
            if (bestMatch == null)
            {
                return null;
            }

            string fullName = FusionAssemblyIdentity.GetDisplayName(bestMatch, FusionAssemblyIdentity.ASM_DISPLAYF.FULL);

            fixed (char* p = new char[MAX_PATH])
            {
                ASSEMBLY_INFO info = new ASSEMBLY_INFO
                {
                    cbAssemblyInfo = (uint)Marshal.SizeOf(typeof(ASSEMBLY_INFO)),
                    pszCurrentAssemblyPathBuf = p,
                    cchBuf = (uint)MAX_PATH
                };

                IAssemblyCache assemblyCacheObject;
                CreateAssemblyCache(out assemblyCacheObject, 0);
                assemblyCacheObject.QueryAssemblyInfo(0, fullName, ref info);
                Debug.Assert(info.pszCurrentAssemblyPathBuf != null);
                Debug.Assert(info.pszCurrentAssemblyPathBuf[info.cchBuf - 1] == '\0');

                var result = Marshal.PtrToStringUni((IntPtr)info.pszCurrentAssemblyPathBuf, (int)info.cchBuf - 1);
                Debug.Assert(result.IndexOf('\0') == -1);
                location = result;
            }

            return fullName;
        }
    }
}
#endif // !CORECLR
