# xUnit Tests

These tests are completely Linux specific.

Every test class *must* belong to
`[Collection("AssemblyLoadContext")]`. This ensures that PowerShell's
AssemblyLoadContext is initialized before any other code is executed.
When this is not the case, late initialization fails with
`System.InvalidOperationException : Binding model is already locked
for the AppDomain and cannot be reset.`

Having every class in the same collection is as close to an xUnit
global init hook as can be done.
