# PowerShell LTS Servicing Criteria

## Purpose and Scope

This document describes the criteria and process for bringing fixes into PowerShell Long-Term Servicing (LTS) release branches. LTS releases are supported for three years from general availability and receive only critical security updates and servicing fixes designed to minimize impact on existing workloads.

## LTS Servicing Philosophy: Stability First

The primary goal of LTS servicing is **stability and reliability**. LTS releases are designed for production environments where predictability and minimal change are essential. Every update to an LTS release must balance the need to address critical issues against the risk of introducing regressions or unexpected behavior changes.

### Key Principles

- **Conservative by design**: Only the most critical fixes are backported to LTS branches
- **Minimal impact**: Changes must be surgical and well-contained
- **Proven stability**: Fixes should be proven in supported release channels before LTS backport
- **Production-focused**: Prioritize issues that affect production workloads

## Decision Authority

While the PowerShell community is encouraged to propose backports to LTS releases, **Microsoft makes the final decision on what fixes are accepted into LTS servicing branches and patch releases**. This ensures alignment with support commitments, internal requirements, and long-term stability goals.

There may be internal requirements, risk assessments, or support considerations that are not publicly documented but factor into servicing decisions.

## Criteria for LTS Fixes

To be considered for an LTS release, a fix must meet one of the following criteria **AND** satisfy the risk and impact requirements below:

### Eligible Fix Categories

1. **Security Fixes**
   - Fixes for security vulnerabilities (CVEs)
   - Security hardening changes required by Microsoft Security Response Center (MSRC)
   - Fixes that address security compliance requirements

2. **Critical Servicing Fixes**
   - Fixes for crashes or data corruption issues
   - Fixes for regressions introduced in previous LTS patch releases
   - Fixes for high-impact bugs that block core scenarios in production environments
   - Fixes required for compatibility with critical ecosystem updates (e.g., OS updates, .NET runtime CVEs)

3. **Build and Packaging Fixes**
   - Fixes to address critical issues in build or release processes that affect LTS release quality
   - Updates to maintain compatibility with supported build infrastructure

### Generally NOT Eligible

The following types of changes are **generally not accepted** into LTS releases:

- New features or capabilities
- Behavior changes (even if considered "improvements")
- Performance optimizations (unless addressing a critical regression)
- Refactoring or code cleanup
- Documentation-only updates (may be acceptable in rare cases)
- Low-impact bugs or minor usability issues
- Experimental or preview features

## Risk and Impact Assessment

Being low-risk alone is **not sufficient** for LTS inclusion. A fix must be:

1. **Sufficiently impactful**: The issue must have meaningful impact on production workloads
2. **Low to moderate risk**: The fix must have minimal risk of introducing regressions
3. **Well-contained**: Changes should be surgical and affect minimal code paths
4. **Thoroughly tested**: The fix must include appropriate test coverage

### Questions to Ask

When proposing an LTS backport, consider:

- **Impact**: How many users/scenarios are affected? Is this blocking production workloads?
- **Severity**: What happens if this is not fixed? (crash, data loss, security issue, minor inconvenience?)
- **Risk**: How complex is the fix? How many code paths does it touch?
- **Alternatives**: Can users work around the issue?
- **Timing**: Can this wait for the next supported release, or is immediate action required?

## Sequencing and Prerequisites

Before requesting an LTS backport:

1. **Fix the current supported release first**: In most cases, fixes should land in the current supported non-LTS release channel (e.g., 7.5.x) before being considered for LTS (e.g., 7.4.x)
   - This ensures the fix is validated in a more current codebase
   - Allows time for community testing and feedback
   - Reduces risk to the more stable LTS channel

2. **Exceptions**: In rare cases, a fix may be required directly in LTS:
   - Security fixes requiring coordinated disclosure
   - Issues specific to the LTS release that don't exist in newer versions
   - Critical production issues requiring immediate resolution

3. **Merge to `master` first**: All changes should merge to the `master` branch before being backported to any release branch, unless the issue is specific to a release branch

## Backport Request Process

### Before Requesting a Backport

1. Ensure your fix meets the eligibility criteria above
2. Ensure the fix is merged to the appropriate branch (`master` or current release)
3. Verify the fix has been validated and tested
4. Allow time for community feedback (typically 1-2 weeks in a supported release)

### How to Request an LTS Backport

When requesting an LTS backport, open an issue or comment on the original PR/issue with the following information:

#### LTS Backport Request Template

```markdown
## LTS Backport Request

**Target LTS Version**: [e.g., 7.4.x]

**Original PR/Issue**: [Link to merged PR and/or original issue]

**Fix Category**: [Security | Critical Servicing | Build/Packaging]

### Impact Assessment

**Customer/User Impact**:
- Describe who is affected and how
- Provide evidence of impact (support tickets, community reports, etc.)
- Estimate scope: [number of users/scenarios affected]

**Severity**:
- [ ] Critical (crashes, data corruption, security vulnerability)
- [ ] High (blocks key scenarios, significant production impact)
- [ ] Medium (noticeable impact but workarounds available)
- [ ] Low (minor issue, limited impact)

**Is this a regression from a previous LTS patch?**: [Yes/No]
- If Yes, which version introduced the regression?

### Risk Assessment

**Complexity**: [Low/Medium/High]
- Describe the scope of code changes
- How many code paths are affected?

**Testing**:
- [ ] Includes new automated tests
- [ ] Tested manually in scenarios X, Y, Z
- [ ] Validated in [current release] for [duration]

**Risk of Regression**: [Low/Medium/High]
- Describe any concerns about potential side effects

### Release Validation

- [ ] Fix is already in the newest supported non-LTS release (e.g., 7.5.x)
  - If Yes, which version: [e.g., 7.5.2]
  - How long has it been available: [e.g., 3 weeks]
- [ ] Fix is specific to LTS branch only (explain why)

### Additional Context

[Any additional information that supports this backport request]

### Alternatives Considered

[Describe any workarounds or alternative solutions]
```

### Review Process

1. Microsoft will review the backport request considering:
   - Alignment with LTS servicing criteria
   - Risk assessment and testing coverage
   - Impact evidence and severity
   - Support commitments and internal requirements
   - Overall release stability

2. Decisions will be communicated on the issue/PR

3. Approved backports will be scheduled for an upcoming LTS patch release

## Official Support Lifecycle Information

For official information about PowerShell support lifecycle and servicing policies:

- [PowerShell Support Lifecycle](https://learn.microsoft.com/powershell/scripting/install/powershell-support-lifecycle)
- [.NET Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) (PowerShell's underlying platform)
- [Microsoft Lifecycle Policy](https://learn.microsoft.com/lifecycle/products/?terms=PowerShell)

## Questions?

If you have questions about LTS servicing criteria or the backport process:

1. Review this document and the support lifecycle documentation above
2. Search existing issues for similar discussions
3. Open a new issue with your question, referencing this document

## Document History

- **2026-02**: Initial version created in response to [PR #26167](https://github.com/PowerShell/PowerShell/pull/26167) discussions

---

*This document reflects the general approach to LTS servicing. Final decisions on what is included in LTS releases remain at Microsoft's discretion and may consider factors not fully documented here.*
