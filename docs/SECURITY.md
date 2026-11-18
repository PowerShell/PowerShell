# Security

> Parent: [../AGENTS.md](../AGENTS.md)

PowerShell is a general-purpose automation engine that executes arbitrary code and interacts
with the OS, the filesystem, the registry, credentials, and remote systems. Security is a
first-class concern; for reporting suspected vulnerabilities and coordinated disclosure,
see `.github/SECURITY.md`.

## Data Sensitivity

At runtime PowerShell routinely handles sensitive
material: **credentials** (`PSCredential`, `SecureString`), **certificates/keys**, **ACLs and
security descriptors**, and **remote session secrets**. Code touching these must avoid logging
or persisting plaintext secrets. See [OBSERVABILITY / logging] guidance in
[DEV_GUIDE.md](DEV_GUIDE.md) - never write secret values into telemetry, error records, or
transcripts.

## Security Subsystem (`src/System.Management.Automation/security`)

| File | Responsibility |
|---|---|
| `SecurityManager.cs`, `SecurityManagerBase` | Execution policy enforcement for scripts. |
| `SecuritySupport.cs` | Execution policy resolution, trust helpers. |
| `Authenticode.cs`, `MshSignature.cs` | Authenticode signing / signature validation (`Get-AuthenticodeSignature`). |
| `CatalogHelper.cs` | File catalog (`.cat`) validation (`Test-FileCatalog`). |
| `wldpNativeMethods.cs` | Windows Defender Application Control / WLDP + AppLocker policy checks (drives Constrained Language Mode). |
| `SecureStringHelper.cs` | `SecureString` encryption/decryption. |
| `CredentialParameter.cs` | `-Credential` parameter handling / prompting. |

AMSI (Antimalware Scan Interface) integration lets antivirus inspect script content before
execution (see `test/powershell/Modules/Microsoft.PowerShell.Security/AmsiInterface.Tests.ps1`).

## Key Security Boundaries

- **System Lockdown / WLDP** - the actual security boundary.
  `wldpNativeMethods.cs` queries the OS application-control policy to decide trust and to force
  untrusted script into ConstrainedLanguage. Only **App Control for Business (ACfB, WDAC)**
  anchors this as a serviced security boundary; the same enforcement driven by **AppLocker** is
  only defense in depth (see below). For **native command execution** the OS policy owns and
  enforces the boundary directly. For **script execution**, however, PowerShell is an active
  participant: System Lockdown Mode reads the OS policy and decides the trust level, then applies
  the corresponding enforcement mechanism (CLM). The boundary is only as strong as that System
  Lockdown enforcement working correctly with the OS component - it is not something the OS
  enforces on PowerShell's behalf, and CLM itself is only the mechanism, not the decision.
- **JEA (Just Enough Administration)** - constrained remoting endpoints running in
  `NoLanguage` mode with a restricted command set (role capability files). JEA is only a
  boundary when the endpoint is configured according to Microsoft's guidance; a misconfigured
  role capability or visible-command surface can undermine it, so the correctness of the
  configuration is owned by the administrator who authors it. See
  [Securing a restricted PowerShell remoting session](https://learn.microsoft.com/en-us/powershell/scripting/security/securing-restricted-sessions).

> **Who owns the boundary.** PowerShell supplies the *enforcement mechanisms* (language modes,
> AMSI, `NoLanguage`/JEA, untrusted-data marking). A real boundary exists only when something
> anchors those mechanisms to an OS-enforced trust decision. System Lockdown with **ACfB** does
> this using the OS boundary, and that boundary is **jointly owned**: PowerShell must keep
> ConstrainedLanguage enforcement sound, while the administrator who authors and deploys the
> ACfB policy owns the policy's correctness. (The same enforcement driven by AppLocker is *not* a
> serviced boundary - it is defense in depth.) A third party can also build their own boundary on
> top of the same mechanisms (e.g. a custom host enforcing ConstrainedLanguage, or a restricted
> JEA endpoint) - but when they do, the threat model becomes **theirs** to define and protect.
> PowerShell only guarantees the mechanism behaves as documented; it does not guarantee a third
> party's configuration is a sound boundary. Getting this right is non-trivial - see
> [Securing a restricted PowerShell remoting session](https://learn.microsoft.com/en-us/powershell/scripting/security/securing-restricted-sessions)
> for an example of the many considerations involved in building your own boundary on these
> mechanisms.

## Defense in Depth (Not Boundaries)

Some controls raise the bar for accidental or casual misuse but are **not** security
boundaries - they can be intentionally bypassed by a user who can run code, and Microsoft does
not service them as boundaries. Some are also the *building blocks* a real boundary is made of:
they only become a boundary when anchored to an OS-enforced trust decision (see above). See
[PowerShell security features](https://learn.microsoft.com/en-us/powershell/scripting/security/security-features).

- **Language Modes** - `FullLanguage`, `ConstrainedLanguage` (CLM), `RestrictedLanguage`,
  `NoLanguage`. The *enforcement mechanism* the lockdown boundary relies on, **not a boundary by
  themselves**. Only when **ACfB** is active does forcing untrusted script into
  **ConstrainedLanguage** constitute a serviced boundary. The same CLM enforcement driven by
  AppLocker, session configuration, or manually setting
  `$ExecutionContext.SessionState.LanguageMode` is only defense in depth and can be changed by a
  caller who can run full-language code; see [BUSINESS_LOGIC.md](BUSINESS_LOGIC.md).
- **System Lockdown / CLM via AppLocker** - AppLocker can drive the same lockdown/CLM
  enforcement as ACfB, but Microsoft services it only as defense in depth, not as a security
  boundary.
- **AMSI** - PowerShell submits script content to the OS (the Antimalware Scan Interface), which
  in turn forwards it to the registered antivirus for inspection before execution. From
  PowerShell's perspective this is *visibility/telemetry* (it hands content to the OS so AV can
  see and optionally block it), not a boundary PowerShell enforces: the OS/AV decides, and a
  determined caller can obfuscate or otherwise evade detection.
- **Execution Policy** - controls whether/which scripts run (`Restricted`, `RemoteSigned`,
  `AllSigned`, `Bypass`, …). It is a safety/management feature to prevent unintentional script
  execution, not a boundary: it is trivially bypassed (e.g. `-ExecutionPolicy Bypass`, piping
  to `pwsh`, or `Unblock-File`) and enforces nothing against a determined caller.
- **Untrusted data mode** - marks data crossing a trust boundary (deserialization, remoting)
  via the `ValidateTrustedData` attribute so it cannot be implicitly trusted (see
  `test/powershell/engine/Security/UntrustedDataMode.Tests.ps1`). This is an internal
  *enforcement mechanism* supporting the CLM boundary, not a documented standalone boundary.

## Signing & Integrity

- Product binaries are Authenticode-signed; pipeline signing configuration lives under
  `.pipelines/` (see `.github/instructions/onebranch-signing-configuration.instructions.md`).
- `Get-AuthenticodeSignature` / `Set-AuthenticodeSignature` and `Test-FileCatalog` let users
  verify file integrity.

## Access / Ownership

`.github/CODEOWNERS` designates security-sensitive paths. Notably the WLDP path
(`src/System.Management.Automation/security/wldpNativeMethods.cs`) is owned by
`@TravisEz13 @seeminglyscience`. Changes to the `security/` folder should get security-owner
review.

## Reporting Vulnerabilities

Do **not** open a public GitHub issue for a suspected vulnerability. Report via
<https://aka.ms/secure-at> for Coordinated Vulnerability Disclosure (MSRC). Only file a public
issue if MSRC confirms it is appropriate. See `.github/SECURITY.md`.
