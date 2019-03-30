---
name: Release Process
about: Maintainers Only - Release Process
title: "Release Process for v6.x.x"
labels: Issue-Meta
assignees: ''

---

<!--
This template is for maintainers to create an issues to track the release process.
Please **only** use this template if you are a maintainer.
-->

# Release Process for v6.x.x

- [ ] Verify that `PowerShell-Native` has been updated/released as needed.
- [ ] Check for `PowerShellGet` and `PackageManagement` release plans.
- [ ] Start process to sync Azure DevOps artifacts feed such as modules and NuGet packages.
- [ ] Create a private branch named `release/v6.x.x` in Azure DevOps repository.
   All release related changes should happen in this branch.
- [ ] Prepare packages
    - [ ] Kick off coordinated build.
- [ ] Kick off Release pipeline.
    - *These tasks are orchestrated by the release pipeline, but here as status to the community.*
    - [ ] Prepare packages
        - [ ] Sign the RPM package.
        - [ ] Install and verify the packages.
        - [ ] Trigger the docker staging builds (signing must be done).
    - [ ] Create the release tag and push the tag to `PowerShell/PowerShell` repository.
    - [ ] Run tests on all supported Linux distributions and publish results.
    - [ ] Update documentation, and scripts.
        - [ ] Update [CHANGELOG.md](../../CHANGELOG.md) with the finalized change log draft.
        - [ ] Stage a PR to master to update other documents and
              scripts to use the new package names, links, and `metadata.json`.
    - [ ] For preview releases,
      merge the release branch to GitHub `master` with a merge commit.
    - [ ] For non-preview releases,
      make sure all changes are either already in master or have a PR open.
    - [ ] Delete the release branch.
    - [ ] Trigger the Docker image release.
    - [ ] Retain builds.
