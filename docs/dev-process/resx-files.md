# Resources

Resources are `.resx` files with string values that we use for error messages and such.
They live in `src\<project>\resources` folders.

At the moment `dotnet cli` doesn't support generating C# bindings (strongly typed resource files).

We are using our own `Start-ResGen` to generate them.

Usually it's called as part of the regular build with

```powershell
Start-PSBuild -ResGen
```

If you see compilation errors related to resources, try to call `Start-ResGen` explicitly.

```powershell
Start-ResGen
```

## Editing `.resx` files

**Don't edit** `.resx` files from Visual Studio.
It will try to create `.cs` files for you and you will get whole bunch of hard-to-understand errors.

To edit a resource file, use any **plain text editor**.
A resource file is a simple XML file, and it's easy to edit.

## Convert `.txt` resource files into `.resx` files

`dotnet cli` doesn't support embedding old-fashioned `.txt` resource.
You can do a one-time conversion of `.txt` resources into `.resx` files with a helper function:

```powershell
# example, converting all .txt resources under src\Microsoft.WSMan.Management\resources
Convert-TxtResourceToXml -Path src\Microsoft.WSMan.Management\resources
```

`.resx` files would be placed next to `.txt` files.
