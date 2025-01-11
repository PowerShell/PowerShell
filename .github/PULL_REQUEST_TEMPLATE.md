<!-- Anything that looks like this is a comment and can't be seen after the Pull Request is created. -->

# PR Summary

<!-- Summarize your PR between here and the checklist. -->

## PR Context

<!-- Provide a little reasoning as to why this Pull Request helps and why you have opened it. -->

## PR Checklist

- [ ] [PR has a meaningful title](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---submission)
  - Use the present tense and imperative mood when describing your changes
- [ ] [Summarized changes](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---submission)
- [ ] [Make sure all `.h`, `.cpp`, `.cs`, `.ps1` and `.psm1` files have the correct copyright header](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---submission)
- [ ] This PR is ready to merge and is not [Work in Progress](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---work-in-progress).
  - If the PR is work in progress, please add the prefix `WIP:` or `[ WIP ]` to the beginning of the title (the `WIP` bot will keep its status check at `Pending` while the prefix is present) and remove the prefix when the PR is ready.
- **[Breaking changes](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#making-breaking-changes)**
  - [ ] None
  - **OR**
  - [ ] [Experimental feature(s) needed](https://github.com/MicrosoftDocs/PowerShell-Docs/blob/main/reference/7.5/Microsoft.PowerShell.Core/About/about_Experimental_Features.md)
    - [ ] Experimental feature name(s): <!-- Experimental feature name(s) here -->
- **User-facing changes**
  - [ ] Not Applicable
  - **OR**
  - [ ] [Documentation needed](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#pull-request---submission)
        - [ ] Issue filed: <!-- Number/link of that issue here -->
- **Testing - New and feature**
  - [ ] N/A or can only be tested interactively
  - **OR**
  - [ ] [Make sure you've added a new test if existing tests do not effectively test the code changed](https://github.com/PowerShell/PowerShell/blob/master/.github/CONTRIBUTING.md#before-submitting)
- **Tooling**
  - [ ] I have considered the user experience from a tooling perspective and don't believe tooling will be impacted.
  - **OR**
  - [ ] I have considered the user experience from a tooling perspective and opened an issue in the relevant tool repository. This may include:
    - [ ] Impact on [PowerShell Editor Services](https://github.com/PowerShell/PowerShellEditorServices) which is used in the [PowerShell extension](https://github.com/PowerShell/vscode-powershell) for VSCode
        (which runs in a different PS Host).
      - [ ] Issue filed: <!-- Number/link of that issue here -->
    - [ ] Impact on Completions (both in the console and in editors) - one of PowerShell's most powerful features.
      - [ ] Issue filed: <!-- Number/link of that issue here -->
    - [ ] Impact on [PSScriptAnalyzer](https://github.com/PowerShell/PSScriptAnalyzer) (which provides linting & formatting in the editor extensions).
      - [ ] Issue filed: <!-- Number/link of that issue here -->
    - [ ] Impact on [EditorSyntax](https://github.com/PowerShell/EditorSyntax) (which provides syntax highlighting with in VSCode, GitHub, and many other editors).
      - [ ] Issue filed: <!-- Number/link of that issue here -->
