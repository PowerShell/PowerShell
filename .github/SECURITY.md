<!-- BEGIN MICROSOFT SECURITY.MD V0.0.9 BLOCK -->

## Security

Microsoft takes the security of our software products and services seriously, which includes all source code repositories managed through our GitHub organizations, which include [Microsoft](https://github.com/Microsoft), [Azure](https://github.com/Azure), [DotNet](https://github.com/dotnet), [AspNet](https://github.com/aspnet), [Xamarin](https://github.com/xamarin) and [PowerShell](https://github.com/PowerShell).

If you believe you have found a security vulnerability in any Microsoft-owned repository that meets [Microsoft's definition of a security vulnerability](https://aka.ms/security.md/definition), please report it to us as described below.

## Reporting Security Issues

**Please do not report security vulnerabilities through public GitHub issues.**

Instead, please report them to the Microsoft Security Response Center (MSRC) at [https://msrc.microsoft.com/create-report](https://aka.ms/security.md/msrc/create-report).

You should receive a response within 24 hours. Additional information can be found at [microsoft.com/msrc](https://www.microsoft.com/msrc). 

Please include the requested information listed below (as much as you can provide) to help us better understand the nature and scope of the possible issue:

  * Type of issue (e.g. buffer overflow, SQL injection, cross-site scripting, etc.)
  * Full paths of source file(s) related to the manifestation of the issue
  * The location of the affected source code (tag/branch/commit or direct URL)
  * Any special configuration required to reproduce the issue
  * Step-by-step instructions to reproduce the issue
  * Proof-of-concept or exploit code (if possible)
  * Impact of the issue, including how an attacker might exploit the issue

This information will help us triage your report more quickly.

If you are reporting for a bug bounty, more complete reports can contribute to a higher bounty award. Please visit our [Microsoft Bug Bounty Program](https://aka.ms/security.md/msrc/bounty) page for more details about our active programs.

## Preferred Languages

We prefer all communications to be in English.

## Policy

Microsoft follows the principle of [Coordinated Vulnerability Disclosure](https://aka.ms/security.md/cvd).

<!-- END MICROSOFT SECURITY.MD BLOCK -->

## Before You Report: PowerShell Security Boundaries

Before reporting a security issue in PowerShell, please review
[docs/SECURITY.md](../docs/SECURITY.md), in particular the **Key Security Boundaries** and
**Defense in Depth (Not Boundaries)** sections, and the official
[PowerShell security features](https://learn.microsoft.com/powershell/scripting/security/security-features)
documentation.

Not every control that can be bypassed is a serviced security boundary. In particular, the
following are typically treated as **defense-in-depth** features rather than security
boundaries, so a bypass by itself may not be serviced as a vulnerability:

- **Execution Policy**
- **Constrained Language Mode (CLM)** when it is *not* enforced by App Control for Business
  (ACfB / WDAC) - e.g. CLM via AppLocker, session configuration, or manually setting
  `$ExecutionContext.SessionState.LanguageMode`
- **AMSI** (antivirus content inspection)

The security boundary Microsoft services is generally **System Lockdown with App Control for
Business (ACfB / WDAC)** forcing untrusted script into Constrained Language Mode. If your report
depends on bypassing a defense-in-depth feature without also crossing that boundary, it may not
qualify as a serviced vulnerability. When in doubt, report it - MSRC will make the determination.

