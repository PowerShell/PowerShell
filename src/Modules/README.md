Modules
==========

There are 3 directories with **content** files.
Content files includes: 

- ps1xml
- psm1
- psd1
- ps1

These files are copied as-is by `dotnet`

- **Shared** is shared between all flavours
- **Full** for FullCLR (Windows)
- **Core** for CoreCLR (all platforms)

Notes
-----------

* We have files with the same names in "Full" and "Core" folders.
That means that the contents of these two files are different. 
I.e. if it's .psd1 file, it could be because `CmdletsToExport` are different for different platforms.

* Also, we should never have files with the same names under "Full" and "Shared" (or "Core" and "Shared").
