---
name: Release Process
about: Maintainers Only - Release Process

---
<!--

This template is for maintainers to create an issues to track the release process.
Please **only** use this template if you are a maintainer.

-->

# Release Process for v6.x.x

- [ ] Create a private branch named `release-v6.x.x` in Azure Dev Ops repository.
   All release related changes should happen in this branch.
- [ ] Run tests on all supported Linux distributions and publish results
- [ ] Prepare packages
    - [ ] Build release packages
    - [ ] Sign the MSI packages and RPM package.
    - [ ] Install and verify the packages.
    - [ ] Trigger the docker staging builds (signing must be done.)
- [ ]  Update documentation, and scripts.
    - [ ] Update [CHANGELOG.md](../../CHANGELOG.md) with the finalized change log draft.
    - [ ] Update other documents and scripts to use the new package names, links, and `metadata.json`.
- [ ] Create NuGet packages and publish them to `powershell-core` feed.
- [ ] Run release build to publish Linux packages to Microsoft YUM/APT repositories.
- [ ] Create the release tag and push the tag to `PowerShell/PowerShell` repository.
- [ ] Run release build to create the draft and publish the release in Github.
- [ ] For preview releases,
  merge the release branch to Github `master` with a merge commit.
- [ ] For non-preview releases,
  make sure all changes are either already in master or have
- [ ] Delete the release branch.
- [ ] Trigger the docker image release.
- [ ] Test and publish NuGet packages on NuGet.org
- [ ] Retain builds.
