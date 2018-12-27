Modules
==========

There are 3 directories with **content** files.
Content files includes: 

- ps1xml
- psm1
- psd1
- ps1

These files are copied as-is by `dotnet`

- Shared (shared between Windows and Unix)
- Windows
- Unix

Notes
-----------

We have files with the same names in different folders.
That means that the contents of these two files are different. 
I.e. if it's .psd1 file, it could be because `CmdletsToExport`
are different for different runtimes (platforms) or frameworks.
