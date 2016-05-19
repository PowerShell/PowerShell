# Resources

Resources are `.resx` files with string values that we use for error messages and such.
They live in `src\<project>\resources` folders.

At the moment `dotnet cli` doesn't support generating C# bindings (strongly typed resource files).

We are using `src\windows-build\gen` folder in [src\windows-build](https://github.com/PowerShell/psl-windows-build) 
with pre-generated `.cs` files to work-around it.
See [issue 756](https://github.com/PowerShell/PowerShell/issues/746) for details.

## Editing resx files

**Don't edit** resx files from Visual Studio. 
It will try to create `.cs` files for you and you will get whole bunch of hard-to-understand errors.

To edit resource file, use any **plain text editor**. 
Resource file is a simple xml, and it's easy to edit.

### Updating string

If you just updated the string value, that's all you need to do: no need to re-generate `.cs` files

### Adding or removing string

When you adding or removing string, `.cs` file need to be changed.

1. Run `Start-ResGen` function from `build.psm1`
1. Make sure your code is building with newly generated resources (run `Start-PSBuild`).
1. Go to submodule (`cd src\windows-build`) and perform the [submodule commit dance](../git/committing.md).
Follow working with [submodule rules](../../.github/CONTRIBUTING.md#submodules)
