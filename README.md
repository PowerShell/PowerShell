# ![logo][] PowerShell

Welcome to the PowerShell GitHub Community!
[PowerShell](https://learn.microsoft.com/powershell/scripting/overview) is a cross-platform (Windows, Linux, and macOS) automation and configuration tool/framework that works well with your existing tools and is optimized
for dealing with structured data (e.g. JSON, CSV, XML, etc.), REST APIs, and object models.
It includes a command-line shell, an associated scripting language, and a framework for processing cmdlets.

[logo]: https://raw.githubusercontent.com/PowerShell/PowerShell/master/assets/ps_black_64.svg?sanitize=true

## Windows PowerShell vs. PowerShell 7+

Although this repository started as a fork of the Windows PowerShell codebase, changes made in this repository are not ported back to Windows PowerShell 5.1.
This also means that [issues tracked here][issues] are only for PowerShell 7.x and higher.
Windows PowerShell specific issues should be reported with the [Feedback Hub app][feedback-hub], by choosing "Apps > PowerShell" in the category.

[issues]: https://github.com/PowerShell/PowerShell/issues
[feedback-hub]: https://support.microsoft.com/windows/send-feedback-to-microsoft-with-the-feedback-hub-app-f59187f8-8739-22d6-ba93-f66612949332

## New to PowerShell?

If you are new to PowerShell and want to learn more, we recommend reviewing the [getting started][] documentation.

[getting started]: https://learn.microsoft.com/powershell/scripting/learn/more-powershell-learning

## Get PowerShell

PowerShell is supported on Windows, macOS, and a variety of Linux platforms. For
more information, see [Installing PowerShell](https://learn.microsoft.com/powershell/scripting/install/installing-powershell).

## Upgrading PowerShell

For best results when upgrading, you should use the same install method you used when you first
installed PowerShell. The update method is different for each platform and install method.

## Community Dashboard

[Dashboard](https://aka.ms/PSPublicDashboard) with visualizations for community contributions and project status using PowerShell, Azure, and PowerBI.

For more information on how and why we built this dashboard, check out this [blog post](https://devblogs.microsoft.com/powershell/powershell-open-source-community-dashboard/).

## Discussions

[GitHub Discussions](https://docs.github.com/discussions/quickstart) is a feature to enable free and open discussions within the community
for topics that are not related to code, unlike issues.

This is an experiment we are trying in our repositories, to see if it helps move discussions out of issues so that issues remain actionable by the team or members of the community.
There should be no expectation that PowerShell team members are regular participants in these discussions.
Individual PowerShell team members may choose to participate in discussions, but the expectation is that community members help drive discussions so that team members
can focus on issues.

Create or join a [discussion](https://github.com/PowerShell/PowerShell/discussions).

## Chat

Want to chat with other members of the PowerShell community?

There are dozens of topic-specific channels on our community-driven PowerShell Virtual User Group, which you can join on:

* [Gitter](https://gitter.im/PowerShell/PowerShell)
* [Discord](https://discord.gg/PowerShell)
* [IRC](https://web.libera.chat/#powershell) on Libera.Chat
* [Slack](https://aka.ms/psslack)

## Building the Repository

| Linux                    | Windows                    | macOS                   |
|--------------------------|----------------------------|------------------------|
| [Instructions][bd-linux] | [Instructions][bd-windows] | [Instructions][bd-macOS] |

If you have any problems building, consult the developer [FAQ][].

### Build status of nightly builds

| Azure CI (Windows)                       | Azure CI (Linux)                               | Azure CI (macOS)                               | CodeFactor Grade         |
|:-----------------------------------------|:-----------------------------------------------|:-----------------------------------------------|:-------------------------|
| [![windows-nightly-image][]][windows-nightly-site] | [![linux-nightly-image][]][linux-nightly-site] | [![macOS-nightly-image][]][macos-nightly-site] | [![cf-image][]][cf-site] |

[bd-linux]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/linux.md
[bd-windows]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/windows-core.md
[bd-macOS]: https://github.com/PowerShell/PowerShell/tree/master/docs/building/macos.md

[FAQ]: https://github.com/PowerShell/PowerShell/tree/master/docs/FAQ.md

[windows-nightly-site]: https://powershell.visualstudio.com/PowerShell/_build?definitionId=32
[linux-nightly-site]: https://powershell.visualstudio.com/PowerShell/_build?definitionId=23
[macos-nightly-site]: https://powershell.visualstudio.com/PowerShell/_build?definitionId=24
[windows-nightly-image]: https://powershell.visualstudio.com/PowerShell/_apis/build/status/PowerShell-CI-Windows-daily
[linux-nightly-image]: https://powershell.visualstudio.com/PowerShell/_apis/build/status/PowerShell-CI-linux-daily?branchName=master
[macOS-nightly-image]: https://powershell.visualstudio.com/PowerShell/_apis/build/status/PowerShell-CI-macos-daily?branchName=master
[cf-site]: https://www.codefactor.io/repository/github/powershell/powershell
[cf-image]: https://www.codefactor.io/repository/github/powershell/powershell/badge

## Downloading the Source Code

You can clone the repository:

```sh
git clone https://github.com/PowerShell/PowerShell.git
```

For more information, see [working with the PowerShell repository](https://github.com/PowerShell/PowerShell/tree/master/docs/git).

## Developing and Contributing

Please look into the [Contribution Guide][]  to know how to develop and contribute.
If you are developing .NET Core C# applications targeting PowerShell Core, [check out our FAQ][] to learn more about the PowerShell SDK NuGet package.

Also, make sure to check out our [PowerShell-RFC repository](https://github.com/powershell/powershell-rfc) for request-for-comments (RFC) documents to submit and give comments on proposed and future designs.

[Contribution Guide]: https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md
[check out our FAQ]: https://github.com/PowerShell/PowerShell/tree/master/docs/FAQ.md#where-do-i-get-the-powershell-core-sdk-package

## Support

For support, see the [Support Section][].

[Support Section]: https://github.com/PowerShell/PowerShell/tree/master/.github/SUPPORT.md

## Legal and Licensing

PowerShell is licensed under the [MIT license][].

[MIT license]: https://github.com/PowerShell/PowerShell/tree/master/LICENSE.txt

### Windows Docker Files and Images

License: By requesting and using the Container OS Image for Windows containers, you acknowledge, understand, and consent to the Supplemental License Terms available on [Microsoft Artifact Registry][mcr].

[mcr]: https://mcr.microsoft.com/en-us/product/powershell/tags

### Telemetry

Please visit our [about_Telemetry](https://learn.microsoft.com/powershell/module/microsoft.powershell.core/about/about_telemetry)
topic to read details about telemetry gathered by PowerShell.

## Governance

The governance policy for the PowerShell project is described the [PowerShell Governance][gov] document.

[gov]: https://github.com/PowerShell/PowerShell/blob/master/docs/community/governance.md

## [Code of Conduct](CODE_OF_CONDUCT.md)

Please see our [Code of Conduct](CODE_OF_CONDUCT.md) before participating in this project.

## [Security Policy](.github/SECURITY.md)

For any security issues, please see our [Security Policy](.github/SECURITY.md).
