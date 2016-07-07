# Resources

Resources are `.resx` files with string values that we use for error messages and such.
They live in `src\<project>\resources` folders.

At the moment `dotnet cli` doesn't support generating C# bindings (strongly typed resource files).

We are using our own `Start-ResGen` to generate them.

Usually it's called as part of the regular build with

```
Start-PSBuild -ResGen
```

## Editing resx files

**Don't edit** resx files from Visual Studio. 
It will try to create `.cs` files for you and you will get whole bunch of hard-to-understand errors.

To edit resource file, use any **plain text editor**. 
Resource file is a simple xml, and it's easy to edit.
