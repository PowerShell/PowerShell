// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Management.Automation;
using System.Runtime;
using Xunit;

namespace PSTests.Parallel
{
    public static class ModuleIntrinsicsTests
    {
        private static string personalModulePath = ModuleIntrinsics.GetPersonalModulePath();
        private static string allUsersModulePath = ModuleIntrinsics.GetPSHomeModulePath();
        private static string sharedModulePath = ModuleIntrinsics.GetSharedModulePath();

        [Fact]
        public static void GetsDefaultModulePathNoExistingEnv()
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"{allUsersModulePath}";

            string actual = ModuleIntrinsics.GetModulePath(null, null, null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetsDefaultModulePathWithExistingEnv()
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"{allUsersModulePath}{Path.PathSeparator}" +
                $"Foo{Path.PathSeparator}bAr";

            string actual = ModuleIntrinsics.GetModulePath($"Foo{Path.PathSeparator}bAr", null, null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetsModulePathWithCustomUserPath()
        {
            string expected = $"foo{Path.PathSeparator}Bar{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"{allUsersModulePath}";

            string actual = ModuleIntrinsics.GetModulePath(null, null, $"foo{Path.PathSeparator}Bar");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void GetsModulePathWithCustomAllUsersPath()
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"foo{Path.PathSeparator}Bar";

            string actual = ModuleIntrinsics.GetModulePath(null, $"foo{Path.PathSeparator}Bar", null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void IgnoresEmptyModulePathValues()
        {
            string expected = $"{sharedModulePath}";

            string actual = ModuleIntrinsics.GetModulePath(null, $" {Path.PathSeparator} {Path.PathSeparator}", $"{Path.PathSeparator}");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void IgnoresDuplicatePathValues()
        {
            string expected = $"foo1{Path.PathSeparator}bar1{Path.PathSeparator}bar2{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                "foo2";

            string actual = ModuleIntrinsics.GetModulePath("bar1", $"foo1{Path.PathSeparator}bar2{Path.PathSeparator}foo2", $"foo1{Path.PathSeparator}bar1{Path.PathSeparator}bar2");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void IgnoresPathEntryEndingWithSeparator()
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                "foo";

            string actual = ModuleIntrinsics.GetModulePath(null, $"foo{Path.PathSeparator}", null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void IgnoresWhitespaceAroundPathEntry()
        {
            string expected = $"foobar{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"bar{Path.PathSeparator}" +
                "foo";

            string actual = ModuleIntrinsics.GetModulePath($"foo  {Path.PathSeparator}bar ", "  bar", "  foobar  ");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void IgnoresTrailingDirectorySeparatorAroundPathEntry()
        {
            string expected = $"dir3{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"dir2{Path.PathSeparator}" +
                "dir1";

            string actual = ModuleIntrinsics.GetModulePath(
                $"dir1{Path.DirectorySeparatorChar}",
                $"dir2{Path.DirectorySeparatorChar} ",
                $"dir3{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void PreservedWhitespaceInsideDirectorySeparatorInPathEntry()
        {
            string expected = $"{Path.DirectorySeparatorChar} dir3 {Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"{Path.DirectorySeparatorChar}dir2 {Path.PathSeparator}" +
                $"{Path.DirectorySeparatorChar} dir1";

            string actual = ModuleIntrinsics.GetModulePath(
                $"{Path.DirectorySeparatorChar} dir1{Path.DirectorySeparatorChar}",
                $"{Path.DirectorySeparatorChar}dir2 {Path.DirectorySeparatorChar}",
                $"{Path.DirectorySeparatorChar} dir3 {Path.DirectorySeparatorChar}");

            Assert.Equal(expected, actual);
        }

#if UNIX
        [Fact]
        public static void AddsRootPathToModulePath()
        {
            string expected = $"/{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                "/foo/bar";

            string actual = ModuleIntrinsics.GetModulePath("/foo/bar", "/", "//");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void TreatsModulePathsAsCaseSensitiveOnUnix()
        {
            string expected = $"foo{Path.PathSeparator}bar{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"Foo{Path.PathSeparator}" +
                "Bar";

            string actual = ModuleIntrinsics.GetModulePath(
                $"foo{Path.PathSeparator}Bar",
                $"foo{Path.PathSeparator}Foo{Path.PathSeparator}Bar",
                $"foo{Path.PathSeparator}bar");

            Assert.Equal(expected, actual);
        }
#else
        [Fact]
        public static void TreatsModulePathAsCaseInSensitiveOnWindows()
        {
            string expected = $"foo{Path.PathSeparator}bar{Path.PathSeparator}" +
                $"{sharedModulePath}";

            string actual = ModuleIntrinsics.GetModulePath(
                $"foo{Path.PathSeparator}Bar",
                $"foo{Path.PathSeparator}Foo{Path.PathSeparator}Bar",
                $"foo{Path.PathSeparator}bar");

            Assert.Equal(expected, actual);
        }

        [Fact]
        public static void TreatsAltDirSepTheSameAsDirSepOnWindows()
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                $"{allUsersModulePath}{Path.PathSeparator}" +
                "C:\\folder;C:\\other1;C:\\other2;c:\\dir1\\dir2";

            string actual = ModuleIntrinsics.GetModulePath(
                $"C:/folder;C:\\folder;C:/folder/;C:\\folder\\;C:/other1/\\;C:/other2\\/;c:/dir1\\dir2",
                null,
                null);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData("C:\\")]
        [InlineData("C:/")]
        [InlineData("C:\\\\")]
        [InlineData("C://")]
        [InlineData("C:")]
        [InlineData("C: ")]
        public static void AddsDriveRootToModulePath(string pathEntry)
        {
            string expected = $"{personalModulePath}{Path.PathSeparator}" +
                $"{sharedModulePath}{Path.PathSeparator}" +
                "C:\\";

            string actual = ModuleIntrinsics.GetModulePath(
                pathEntry,
                pathEntry,
                null);

            Assert.Equal(expected, actual);
        }
#endif
    }
}
