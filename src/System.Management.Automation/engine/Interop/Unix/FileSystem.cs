// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Unix
    {
        /// <summary>The <c>mode</c> value passed to <c>access</c> to test for execute permission.</summary>
        internal const int X_OK = 1;

        /// <summary>Create a symbolic link named <paramref name="linkPath"/> pointing to <paramref name="target"/>.</summary>
        /// <param name="target">The path the symbolic link points to.</param>
        /// <param name="linkPath">The path of the symbolic link to create.</param>
        /// <returns>0 on success, -1 on failure (see <c>errno</c>).</returns>
        [LibraryImport("libc", EntryPoint = "symlink", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int Symlink(string target, string linkPath);

        /// <summary>Create a hard link named <paramref name="newPath"/> for the existing file <paramref name="oldPath"/>.</summary>
        /// <param name="oldPath">The path of the existing file.</param>
        /// <param name="newPath">The path of the hard link to create.</param>
        /// <returns>0 on success, -1 on failure (see <c>errno</c>).</returns>
        [LibraryImport("libc", EntryPoint = "link", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int Link(string oldPath, string newPath);

        /// <summary>Check the calling process's permissions for the file <paramref name="pathName"/>.</summary>
        /// <param name="pathName">The path to test.</param>
        /// <param name="mode">The accessibility check(s) to perform (for example <see cref="X_OK"/>).</param>
        /// <returns>0 if all requested access is granted, -1 otherwise (see <c>errno</c>).</returns>
        [LibraryImport("libc", EntryPoint = "access", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static partial int Access(string pathName, int mode);
    }
}
