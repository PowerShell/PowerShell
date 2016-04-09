Artifact installation instructions
==================================

We publish an archive with CoreCLR and FullCLR bits on every CI build
with [AppVeyor][].

[appveyor]: https://ci.appveyor.com/project/PowerShell/powershell-linux

CoreCLR artifacts
=================

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in file explorer -> properties ->
  check 'Unblock' box -> apply
* Extract zip file to `bin` directory
* `./bin/powershell.exe`

FullCLR artifacts
=================

* Download zip package from **artifacts** tab of the particular build.
* Unblock zip file: right-click in file explorer -> properties ->
  check 'Unblock' box -> apply
* Extract zip file to `$in` directory
* `Start-DevPSGithub -binDir bin`
