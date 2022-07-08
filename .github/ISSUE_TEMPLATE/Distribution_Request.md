---
name: Distribution Support Request
about: Requests support for a new distribution
title: "Distribution Support Request"
labels: Distribution-Request, Needs-Triage
assignees: ''

---

## Details of the Distribution

- Name of the Distribution:
- Version of the Distribution:
- Package Types
    - [ ] Deb
    - [ ] RPM
    - [ ] Tar.gz
    - Snap - Please file issue in https://github.com/powershell/powershell-snap.  This issues type is unrelated to snap packages with a distribution neutral.
- Processor Architecture (One per request):
- The following is a requirement for supporting a distribution **without exception.**
    - [ ] The version and architecture of the Distribution is [supported by .NET Core](https://github.com/dotnet/core/blob/master/release-notes/5.0/5.0-supported-os.md#linux).
- The following are requirements for supporting a distribution.
  Please write a justification for any exception where these criteria are not met and
  the PowerShell committee will review the request.
    - [ ] The version of the Distribution is supported for at least one year.
    - [ ] The version of the Distribution is not an [interim release](https://ubuntu.com/about/release-cycle) or equivalent.

## Progress

- [ ] An issues has been filed to create a Docker image in https://github.com/powershell/powershell-docker

### For PowerShell Team **ONLY**

- [ ] Docker image created
- [ ] Docker image published
- [ ] Distribution tested
- [ ] Update `packages.microsoft.com` deployment
- [ ] [Lifecycle](https://github.com/MicrosoftDocs/PowerShell-Docs/blob/staging/reference/docs-conceptual/PowerShell-Support-Lifecycle.md) updated
- [ ] Documentation Updated
