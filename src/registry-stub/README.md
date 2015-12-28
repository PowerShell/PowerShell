This stubs the `RegCloseKey` function provided on Windows by
`api-ms-win-core-registry-l1-1-0.dll`.

Without this DLL in the library path, the following exception is
thrown upon shutdown of PowerShell. While I do not like hiding this
error, it is neccesary for the rest of PowerShell to shutdown
properly, and release its file descriptors. Otherwise the shell will
mess up Bash, and it needs to be reset.

This must be removed as soon as it can be addressed in a better way.

```
Unhandled Exception: System.DllNotFoundException: Unable to load DLL 'api-ms-win-core-registry-l1-1-0.dll': The specified module could not be found.
 (Exception from HRESULT: 0x8007007E)
   at Interop.mincore.RegCloseKey(IntPtr hKey)
   at Microsoft.Win32.SafeHandles.SafeRegistryHandle.ReleaseHandle()
   at System.Runtime.InteropServices.SafeHandle.InternalFinalize()
   at System.Runtime.InteropServices.SafeHandle.Dispose(Boolean disposing)
   at System.Runtime.InteropServices.SafeHandle.Finalize()
```
