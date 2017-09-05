
# C# Coding Guidelines

## Coding Conventions

As a general rule, our coding convention is to follow the style of the surrounding code.
So if a file happens to differ in style from conventions defined here
(e.g. private members are named `m_member` rather than `_member`),
the existing style in that file takes precedence.

When making changes, you may find some existing code goes against the conventions defined here.
In such cases, please avoid reformatting any existing code when submitting a PR as it obscures the functional changes of the PR.
A separate PR should be submitted for style-only changes.
We also run the [.NET code formatter tool](https://github.com/dotnet/codeformatter) regularly to keep consistent formatting.

### Naming Conventions

* Use meaningful, descriptive words for names.
  For method names, it's encouraged to use `Verb-Object` pair such as **`LoadModule`**.

* Use `_camelCase` to name internal and private fields and use `readonly` where possible.
  Prefix instance fields with `_`, static fields with `s_` and thread static fields with `t_`.
  When used on static fields, `readonly` should come after `static` (i.e. `static readonly` not `readonly static`).

* Use `camelCase` to name non-constant local variables.

* Use `PascalCase` to name constant local variables and fields.
  The only exception is for interop code where the constant should exactly match the name and value of the code you are calling via interop (i.e. `const int ERROR_SUCCESS = 0`).

* Use `PascalCase` to name types and all other type members.

### Layout Conventions

* Use four spaces of indentation (no tabs).

* Avoid more than one blank empty line at any time.

* Avoid unnecessary trailing spaces at the end of a line.

* Braces usually go on their own lines,
  with the exception of single line statements that are properly indented.

* Namespace imports should be specified at the top of the file,
  outside of `namespace` declarations and should be sorted alphabetically.

* Fields should be specified at the top within type declarations.

* File encoding should be `ASCII` (preferred) or `UTF8` (with `BOM`) if absolutely necessary.

### Member Conventions

* Use of `this` is neither encouraged nor discouraged.

* Use `nameof(<member-name>)` instead of `"<member-name>"` whenever possible and relevant.

* Always specify the visibility, even if it's the default (i.e. `private string _foo` not `string _foo`).
  Visibility should be the first modifier (i.e. `public abstract` not `abstract public`).

* Make members private where possible.
  Avoid declaring public members unless it's absolutely necessary.

* Public members in a namespace that ends with `Internal`,
  for example `System.Management.Automation.Internal` are not considered a supported public API.
  Such members are necessarily public as implementation details in code shared between C# and PowerShell script,
  or must be available publicly by generated code.

### Commenting Conventions

* Add comments when changes are not trivial or could be confusing.

* Add comments when a reviewer needs help to understand your changes.

* Update existing comments when you are changing the corresponding code.

* Make sure the added/updated comments are accurate and easy to understand.

* Public members must use [doc comments](https://msdn.microsoft.com/en-us/library/b2s063f7.aspx).
  Internal and private members may use doc comments but it is not required.

## Performance Considerations

PowerShell has a lot of performance sensitive code as well as a lot of inefficient code.
We have some guidelines that we typically apply widely even in less important code because code and patterns are copied,
and we want certain inefficient code to stay out of the performance critical code.

Some general guidelines:

* Avoid LINQ - it can create lots of avoidable garbage.

* Prefer `for` and `foreach`,
  with a slight preference towards `for` when you're uncertain if `foreach` allocates an iterator.

* Avoid `params` arrays, prefer adding overloads with 1, 2, 3, and maybe more parameters.

* Be aware of APIs such as `String.Split(params char[])` that do not provide overloads to avoid array allocation.
  When calling such APIs, reuse a static array when possible (i.e. `Utils.Separators.Colon`).

* Avoid creating empty arrays.
  Instead, reuse the static ones via `Utils.EmptyArray<T>`.

* Avoid unnecessary memory allocation in a loop.
  Move the memory allocation outside the loop if possible.

* Use `dict.TryGetValue` instead of `dict.Contains` and `dict[<key>]` when retrieving value from a `Dictionary`.
  In this way you can avoid hashing the key twice.

## Security Considerations

Security is an important aspect of PowerShell and we need to be very careful about changes that may introduce security risks.
Reviewers of a PR should be sensitive to changes that may affect security.
Some security related keywords may serve as good indicators,
such as `crypto`, `encryption`, `decryption`, `certificate`, `authenticate`, `ssl/tls` and `protected data`.

When facing a PR with such changes,
the reviewers should request a designated security Subject Matter Expert (SME) to review the PR.
Currently, @PaulHigin and @TravisEz13 are our security SMEs.
See [CODEOWNERS](../../.github/CODEOWNERS) for more information about the area experts.

## Best Practices

* Avoid hard-coding anything unless it's absolutely necessary.

* Avoid a method that is too long and complex.
  In such case, separate it to multiple methods or even a nested class as you see fit.

* Use `using` statement instead of `try/finally` if the only code in the `finally` block is to call the `Dispose` method.

* Use of object initializers (i.e. `new Example { Name = "Name", ID = 1 }`) is encouraged for better readability,
  but not required.

* Stick to the `DRY` principle -- Don't Repeat Yourself.
   * Wrap the commonly used code in methods, or even put it in a utility class if that makes sense,
     so that the same code can be reused.
   * Check if the code for the same purpose already exists in the code base before inventing your own wheel.
   * Avoid repeating literal strings in code. Instead, use `const` variable to hold the string.

* Use of new C# language syntax is encouraged.
  But avoid refactoring any existing code using new language syntax when submitting a PR
  as it obscures the functional changes of the PR.
  A separate PR should be submitted for such refactoring without any functional changes.

## Portable Code

There are 3 primary preprocessor macros we use during builds:

* `DEBUG` - guard code that should not be included in release builds
* `CORECLR` - guard code that differs between Full CLR and CoreCLR
* `UNIX` - guard code that is specific to Unix (Linux and macOS)

Any other preprocessor defines found in the source are used for one-off custom builds,
typically to help debug specific scenarios.

Here are some general guidelines for writing portable code: 

* We are in the process of cleaning up Full CLR specific code (code enclosed in `!CORECLR`),
  so do not use `CORECLR` or `!CORECLR` in new code.
  PowerShell Core targets .NET Core only and all new changes should support .NET Core only.

* The PowerShell code base started on Windows and depends on many Win32 APIs through P/Invoke.
  Going forward, we try to depend on .NET Core to handle platform differences,
  so avoid adding new P/Invoke calls where a suitable alternative exists in .NET Core.

* Try to minimize the use of `#if UNIX`.
  When absolutely necessary, avoid duplicating more code than necessary,
  and instead prefer introducing helper functions to minimize the platform differences.

* When adding platform dependent code (`Windows` vs. `UNIX`), prefer preprocessor directives over runtime checks.
  However, runtime checks are acceptable if it would greatly improve readability
  without causing performance concerns in performance-sensitive code.

* We produce a single binary for all UNIX variants,
  so runtime checks are currently necessary for some of them (e.g. macOS vs. Linux).
