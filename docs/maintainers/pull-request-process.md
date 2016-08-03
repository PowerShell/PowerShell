# Pull Request Process

Our [pull request template][pr-template] includes the bare minimum requirements for a pull request to be accepted into PowerShell. This includes:
* Writing tests
* Writing documentation (where does thie one live already? is it where this guidance should exist all up?)
* Our [code review process][code-review]
* Repository maintainer sign-off, per our [governance model][governance]

## Pull Request Workflow

1. A contributor opens a pull request.
1. The contributor ensures that their pull request passes the [CI system][ci-system] build.
  - If the build fails, a [Repository Maintainer][repository-maintainer] adds the ```waiting for author``` label to the pull request. 
  The contributor can then continue to update the pull request until the build passes.
1. Once the build passes, the maintainer either reviews the pull request immediately or adds the ```need review``` label.
1. An [Area Expert][area-expert] reviews the pull request code.
  - If the contributor does not meet the reviewer's standards, the reviewer makes comments. A maintainer then removes the ```need review``` label and adds the ```waiting for author``` label. The contributor must address the comments and repeat from step 2.
  - If the contributor meets the reviewer's standards, the reviewer comments that they are satisfied. A maintainer then removes the ```need review``` label.
1. Once the code review is completed, a maintainer merges the pull request.

### Abandoned Pull Requests
A pull request with the label ```waiting for the author``` for **more than two weeks** without a word from the author is considered abandoned.

In these cases:

1. Ping the author of PR to remind him of pending changes.
  - If the contributor responds, it's no longer an abandoned pull request, proceed as normal.
2. If the contributor does not respond **within a week**:
  - If the reviewer's comments are very minor, merge the change, fix the code immediately, and create a new PR with the fixes addressing the minor comments.
  - If the changes required to merge the pull request are significant but needed, create a new branch with the changes and open an issue to merge the code into the dev branch. Mention the original pull request ID in the description of the new issue and close the abandoned pull request. 
  - If the changes in an abandoned pull request are no longer needed (e.g. due to refactoring of the code base or a design change), simply close the pull request.

[pr-template]: ../.github/PULL_REQUEST_TEMPLATE.md
[code-review]: code-review-guidelines.md
[governance]: ../community/governance.md
[repository-maintainer]: ../community/governance.md#repository-maintainers
[area-expert]: ../community/governance.md#area-experts#area-experts