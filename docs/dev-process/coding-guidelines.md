
# C# Coding Style

## Coding Conventions

As a general rule, our coding convention is to follow the style of the surrounding code.
Avoid reformatting any code when submitting a PR as it obscures the functional changes of your change.
We run the [.NET code formatter tool](https://github.com/dotnet/codeformatter) regularly help keep consistent formatting.

A basic rule of formatting is to use "Visual Studio defaults".
Here are some general guidelines

* No tabs, indent 4 spaces.
* Braces usually go on their own line,
  with the exception of single line statements that are properly indented.
* Use `_camelCase` for instance fields,
  use `readonly` where possible.
* Use of `this` is neither encouraged nor discouraged.
* Avoid more than one blank empty line.
* Public members should use [doc comments](https://msdn.microsoft.com/en-us/library/b2s063f7.aspx),
  internal members may use doc comments but it is not encouraged.
* Public members in a namespace that ends with `Internal`,
  for example `System.Management.Automation.Internal` are not considered a supported public API.
  Such members are necessarily public as implementation details in code shared between C# and PowerShell script,
  or must be available publicly by generated code.
* File encoding should be ASCII (preferred)
  or UTF8 (with BOM) if absolutely necessary.

## Preprocessor defines

There are 3 primary preprocessor macros we define during builds:

* DEBUG - guard code that should not be included in release builds
* CORECLR - guard code that differs between Full CLR and CoreCLR
* UNIX - guard code that is specific to Unix (Linux and Mac OS X)

Any other preprocessor defines found in the source are used for one-off custom builds,
typically to help debug specific scenarios.

### Runtimes

The PowerShell repo is used to build PowerShell targeting CoreCLR as well as CLR 4.5.

Code under !CORECLR must build against CLR 4.5.
We will not accept changes that require a later version of the full CLR.
In extremely rare cases, we may use reflection to use an API in a later version of the CLR,
but the feature must robustly handle running with CLR 4.5.

We may reject code under !CORECLR without explanation because
we do not support installation or testing of such code in this repo.
All new features should support CoreCLR.

## Performance considerations

PowerShell has a lot of performance sensitive code as well as a lot of inefficient code.
We have some guidelines that we typically apply widely even in less important code
because code and patterns are copied we want certain inefficient code to stay out of the performance critical code.

Some general guidelines:

* Avoid LINQ - it can create lots of avoidable garbage
* Prefer `for` and `foreach`,
  with a slight preference towards `for` when you're uncertain if `foreach` allocates an iterator.
* Avoid `params` arrays, prefer adding overloads with 1, 2, 3, and maybe more parameters.
* Be aware of APIs such as `String.Split(params char[])` that do not provide overloads to avoid array allocation.
  When calling such APIs, reuse a static array when possible.
* Avoid unnecessary memory allocation in a loop.
  Move the memory allocation outside the loop if possible.

## Portable code

The PowerShell code base started on Windows and depends on many Win32 APIs through P/Invoke.
Going forward, we try to depend on CoreCLR to handle platform differences,
so avoid adding new P/Invoke calls where a suitable alternative exists in .NET.

Try to minimize the use of `#if UNIX` and `#if CORECLR`.
When absolutely necessary, avoid duplicating more code than necessary,
and instead prefer introducing helper functions to minimize the platform differences.

When adding platform dependent code, prefer preprocessor directives
over runtime checks.

We produce a single binary for all UNIX variants,
so runtime checks are currently necessary for some platform differences, e.g. OS X and Linux.
