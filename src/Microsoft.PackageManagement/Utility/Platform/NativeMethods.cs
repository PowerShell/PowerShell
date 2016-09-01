// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

#if !UNIX
namespace Microsoft.PackageManagement.Internal.Utility.Platform {
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Text;

    internal enum WinVerifyTrustResult : uint {
        Success = 0,
        ProviderUnknown = 0x800b0001, // The trust provider is not recognized on this system
        ActionUnknown = 0x800b0002, // The trust provider does not support the specified action
        SubjectFormUnknown = 0x800b0003, // The trust provider does not support the form specified for the subject
        SubjectNotTrusted = 0x800b0004, // The subject failed the specified verification action
        UntrustedRootCert = 0x800B0109 //A certificate chain processed, but terminated in a root certificate which is not trusted by the trust provider.
    }

    [Flags]
    internal enum LoadLibraryFlags : uint {
        DontResolveDllReferences = 0x00000001,
        AsDatafile = 0x00000002,
        LoadWithAlteredSearchPath = 0x00000008,
        LoadIgnoreCodeAuthzLevel = 0x00000010,
        AsImageResource = 0x00000020,
    }

    [Flags]
    internal enum ResourceEnumFlags : uint {
        None = 0x00000000,
        LanguageNeutral = 0x00000001,
        Mui = 0x00000002,
        Validate = 0x00000008,
    }

    public class DisposableModule : IDisposable {
        private Module _module;

        public bool IsInvalid {
            get {
                return _module.IsInvalid;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing) {
            if (disposing) {
                _module.Free();
            }
        }

        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "There is no need for such.")]
        public static implicit operator Module(DisposableModule instance) {
            return instance._module;
        }

        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates", Justification = "There is no need for such.")]
        public static implicit operator DisposableModule(Module module) {
            return new DisposableModule {
                _module = module
            };
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Module {
        [FieldOffset(0)]
        public IntPtr handle;

        public Module(IntPtr ptr) {
            handle = ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }

        public void Free() {
            if (!IsInvalid) {
                NativeMethods.FreeLibrary(this);
            }

            handle = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ResourceType {
        public static ResourceType None = new ResourceType(0);
        public static ResourceType Cursor = new ResourceType(1);
        public static ResourceType Bitmap = new ResourceType(2);
        public static ResourceType Icon = new ResourceType(3);
        public static ResourceType Menu = new ResourceType(4);
        public static ResourceType Dialog = new ResourceType(5);
        public static ResourceType String = new ResourceType(6);
        public static ResourceType FontDir = new ResourceType(7);
        public static ResourceType Font = new ResourceType(8);
        public static ResourceType Accelerator = new ResourceType(9);
        public static ResourceType RCData = new ResourceType(10);
        public static ResourceType MessageTable = new ResourceType(11);
        public static ResourceType GroupCursor = new ResourceType(12);
        public static ResourceType GroupIcon = new ResourceType(14);
        public static ResourceType Version = new ResourceType(16);
        public static ResourceType DialogInclude = new ResourceType(17);
        public static ResourceType PlugPlay = new ResourceType(19);
        public static ResourceType Vxd = new ResourceType(20);
        public static ResourceType AniCursor = new ResourceType(21);
        public static ResourceType AniIcon = new ResourceType(22);
        public static ResourceType Html = new ResourceType(23);
        public static ResourceType Manifest = new ResourceType(24);

        [FieldOffset(0)]
        public IntPtr handle;

        public ResourceType(IntPtr ptr) {
            handle = ptr;
        }

        public ResourceType(int ptr) {
            handle = (IntPtr)ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ResourceId {
        [FieldOffset(0)]
        public IntPtr handle;

        public ResourceId(IntPtr ptr) {
            handle = ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Resource {
        [FieldOffset(0)]
        public IntPtr handle;

        public Resource(IntPtr ptr) {
            handle = ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ResourceData {
        [FieldOffset(0)]
        public IntPtr handle;

        public ResourceData(IntPtr ptr) {
            handle = ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct Unused {
        internal static Unused Nothing;

        [FieldOffset(0)]
        public IntPtr handle;

        public Unused(IntPtr ptr) {
            handle = ptr;
        }

        public bool IsInvalid {
            get {
                return handle == IntPtr.Zero;
            }
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct LanguageId {
        internal static LanguageId None;

        [FieldOffset(0)]
        private UInt16 value;
    }

    internal delegate bool EnumResourceTypes([MarshalAs(UnmanagedType.SysInt)] Module module, ResourceType type, Unused unused);

    internal delegate bool EnumResourceNames(Module module, ResourceType type, ResourceId resourceId, Unused unused);

    internal delegate bool EnumResourceLanguages(Module module, ResourceType type, ResourceId resourceId, LanguageId language, Unused unused);

    internal static class NativeMethods {
        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int WNetGetConnection([MarshalAs(UnmanagedType.LPTStr)] string localName, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder remoteName, ref int length);

        [DllImport("user32")]
        internal static extern int LoadString(Module module, uint stringId, StringBuilder buffer, int bufferSize);

        [DllImport("wintrust.dll", ExactSpelling = true, SetLastError = false, CharSet = CharSet.Unicode)]
        internal static extern WinVerifyTrustResult WinVerifyTrust(IntPtr hwnd, [MarshalAs(UnmanagedType.LPStruct)] Guid pgActionID, WinTrustData pWVTData);

#if !CORECLR
        /// <summary>
        ///     Loads the specified module into the address space of the calling process.
        /// </summary>
        /// <param name="filename">The name of the module.</param>
        /// <param name="unused">This parameter is reserved for future use.</param>
        /// <param name="dwFlags">The action to be taken when loading the module.</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Module LoadLibraryEx(string filename, Unused unused, LoadLibraryFlags dwFlags);

        [DllImport("kernel32.dll", EntryPoint = "MoveFileEx", CharSet = CharSet.Unicode)]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);

        /// <summary>
        ///     Enumerates resource types within a binary.
        /// </summary>
        /// <param name="module">Handle to a module to search.</param>
        /// <param name="callback">Pointer to the function to be called for each resource type.</param>
        /// <param name="unused">Value passed to the callback function.</param>
        /// <param name="flags">The type of file to be searched.</param>
        /// <param name="langid">Language ID</param>
        /// <returns>Returns TRUE if successful, otherwise, FALSE.</returns>
        [DllImport("kernel32.dll", EntryPoint = "EnumResourceTypesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceTypesEx(Module module, EnumResourceTypes callback, Unused unused, ResourceEnumFlags flags, LanguageId langid);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceNamesEx(Module module, ResourceType resourceType, EnumResourceNames callback, Unused unused, ResourceEnumFlags flags, uint langid);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceLanguagesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceLanguagesEx(Module module, ResourceType resourceType, ResourceId resourceId, EnumResourceLanguages callback, Unused unused, ResourceEnumFlags flags, LanguageId language);

        [DllImport("kernel32.dll", EntryPoint = "FindResourceExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Resource FindResourceEx(Module module, ResourceType resourceType, ResourceId resourceId, LanguageId language);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern ResourceData LoadResource(Module module, Resource resource);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr LockResource(ResourceData data);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int SizeofResource(Module module, Resource hResInfo);

        [DllImport("kernel32")]
        internal static extern bool FreeLibrary(Module instance);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string debugMessageText);
#else
        [DllImport("api-ms-win-core-file-l2-1-1.dll", EntryPoint="MoveFileEx", CharSet=CharSet.Unicode)]
        internal static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);
        /// <summary>
        ///     Loads the specified module into the address space of the calling process.
        /// </summary>
        /// <param name="filename">The name of the module.</param>
        /// <param name="unused">This parameter is reserved for future use.</param>
        /// <param name="dwFlags">The action to be taken when loading the module.</param>
        /// <returns></returns>

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", EntryPoint="LoadLibraryExW", SetLastError=true, CharSet=CharSet.Unicode)]
        internal static extern Module LoadLibraryEx(string filename, Unused unused, LoadLibraryFlags dwFlags);

        /// <summary>
        ///     Enumerates resource types within a binary.
        /// </summary>
        /// <param name="module">Handle to a module to search.</param>
        /// <param name="callback">Pointer to the function to be called for each resource type.</param>
        /// <param name="unused">Value passed to the callback function.</param>
        /// <param name="flags">The type of file to be searched.</param>
        /// <param name="langid">Language ID</param>
        /// <returns>Returns TRUE if successful, otherwise, FALSE.</returns>
        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", EntryPoint = "EnumResourceTypesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceTypesEx(Module module, EnumResourceTypes callback, Unused unused, ResourceEnumFlags flags, LanguageId langid);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", EntryPoint = "EnumResourceNamesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceNamesEx(Module module, ResourceType resourceType, EnumResourceNames callback, Unused unused, ResourceEnumFlags flags, uint langid);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", EntryPoint = "EnumResourceLanguagesExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern bool EnumResourceLanguagesEx(Module module, ResourceType resourceType, ResourceId resourceId, EnumResourceLanguages callback, Unused unused, ResourceEnumFlags flags, LanguageId language);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", EntryPoint = "FindResourceExW", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern Resource FindResourceEx(Module module, ResourceType resourceType, ResourceId resourceId, LanguageId language);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", SetLastError = true)]
        internal static extern ResourceData LoadResource(Module module, Resource resource);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", SetLastError = true)]
        internal static extern IntPtr LockResource(ResourceData data);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll", SetLastError = true)]
        internal static extern int SizeofResource(Module module, Resource hResInfo);

        [DllImport("api-ms-win-core-libraryloader-l1-2-0.dll")]
        internal static extern bool FreeLibrary(Module instance);

        [DllImport("api-ms-win-core-debug-l1-1-1", CharSet=CharSet.Unicode, EntryPoint="OutputDebugStringW")]
        public static extern void OutputDebugString(string debugMessageText);
#endif
    }
}
#endif

