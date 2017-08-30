# Pull Request Process

Requirements for a pull request to be accepted into PowerShell
* Writing tests
* Writing documentation

## Pull Request Workflow

1. The PR *author* creates a pull request from a fork.
1. The *author* ensures that their pull request passes the [CI system][ci-system] build.
  - If the build fails, a [Repository Maintainer][repository-maintainer] adds the `Review - waiting on author` label to the pull request. 
  The *author* can then continue to update the pull request until the build passes.
3. If the *author* knows whom should participate in the review, they should add them otherwise they can add the recommended *reviewers*.
1. Once the build passes, if there is not sufficient review, the *maintainer* adds the `Review - needed` label.
1. An [Area Expert][area-expert] should also review the pull request.
  - If the *author* does not meet the *reviewer*'s standards, the *reviewer* makes comments. A *maintainer* then removes the `Review - needed` label and adds the `Review - waiting on author` label. The *author* must address the comments and repeat from step 2.
  - If the *author* meets the *reviewer*'s standards, the *reviewer* approves the PR. A maintainer then removes the `need review` label.
6. Once the code review is completed, a *maintainer* merges the pull request after one business day to allow for additional critical feedback.

### Roles and Responsibilities

1. The PR *author* is responsible for moving the PR forward.
This includes addressing feedback within a timely period and indicating feedback has been addressed by adding a comment and mentioning the specific *reviewers*.
1. *Reviewers* are responsible for ensuring the code addresses the issue being fixed, does not create new issues (functional, performance, or reliability), and implements proper design.
*Reviewers* should use the `Review changes` drop down to indicate they are done with their review.
  - `Request changes` if you believe the PR merge should be blocked if your feedback is not addressed,
  - `Approve` if you believe your feedback has been addressed or the code is fine as-is
  - `Comment` if you are making suggestions that the *author* does not have to accept.
  Early in the review, it is acceptable to provide feedback on coding style based on the published [Coding Guidelines](coding-guidelines), however, after
  the *author* has already pushed commits to address feedback, it is generally _not_ acceptable to focus on style issues as it disrupts the PR process.
  Late feedback can be submitted as a new issue or new pull request from the *reviewer*.
3. *Maintainers* ensure that proper review has occurred and if they believe one approval is not sufficient, the *maintainer* is responsible to add more reviewers.
A *maintainer* may also be a reviewer, but the roles are distinct.


### Abandoned Pull Requests
A pull request with the label `Review - waiting on author` for **more than two weeks** without a word from the author is considered abandoned.

In these cases:

1. Ping the author of PR to remind them of pending changes.
  - If the contributor responds, it's no longer an abandoned pull request, proceed as normal.
2. If the contributor does not respond **within a week**:
  - If the reviewer's comments are very minor, merge the change, fix the code immediately, and create a new PR with the fixes addressing the minor comments.
  - If the changes required to merge the pull request are significant but needed, create a new branch with the changes and open an issue to merge the code into the dev branch. Mention the original pull request ID in the description of the new issue and close the abandoned pull request. 
  - If the changes in an abandoned pull request are no longer needed (e.g. due to refactoring of the code base or a design change), simply close the pull request.

[repository-maintainer]: ../community/governance.md#repository-maintainers
[area-expert]: ../community/governance.md#area-experts#area-experts
[coding-guidelines]: ./coding-guidelines.md
[ci-system]: ../testing-guidelines/testing-guidelines.md#ci-system