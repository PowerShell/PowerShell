# libpsl-native

This library provides functionality missing from .NET Core via system calls,
that are called from from the `CorePsPlatform.cs` file of PowerShell. The
method to do this is a Platform Invoke, which is C#'s Foreign Function
Interface to C code (and C++ by way of `extern C`).

## Build

[CMake][] is used to build the project, which results in a `libpsl-native.so`
library on Linux, and `libpsl-native.dylib` on OS X.

```sh
cmake -DCMAKE_BUILD_TYPE=Debug .
make -j
```

[CMake]: https://cmake.org/cmake/help/v2.8.12/cmake.html

## Test

The [Google Test][] framework is used for unit tests.

Use either `make test` or `ctest --verbose` for more output.

[Google Test]: https://github.com/google/googletest/tree/release-1.7.0

## Notes

Marshalling data from native to managed code is much easier on Linux than it is
on Windows. For instance, to return a string, you simply return a copy of it on
the heap. Since only one memory allocator is used on Linux, the .NET runtime
has no problem later freeing the buffer. Additionally, .NET presumes that the
codepage "Ansi" on Linux is always UTF-8. So just marshal the string as
`UnmanagedType.LPStr`.

### C# (Managed)

```c#
[DllImport("libpsl-native", CharSet = CharSet.Ansi)]
[return: MarshalAs(UnmanagedType.LPStr)]
internal static extern string GetSomeString();
```

### C (Native)

```c
char *GetSomeString()
{
    return strdup("some string");
}
```

The CoreFX team has an excellent guide for [UNIX Interop][].

[UNIX Interop]: https://github.com/dotnet/corefx/blob/master/Documentation/coding-guidelines/interop-guidelines.md#unix-shims
