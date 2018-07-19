# Maintainer Best Practices

## Review PRs

- Ask the author to reword the PR title based on guidelines in [Contributing](../../.github/CONTRIBUTING.md).
- Ask the author to apply `[Feature]` tag to trigger full test builds if it's necessary.
- Label the PR with `Breaking-Change` and/or `Documentation Needed` as appropriate.
- When labelling a PR with `Review-Committee`, leave a detailed comment to summarize the issue you want the committee to look into.
  It's recommended to include examples to explain/demonstrate behaviors.

## Merge PRs

- Use `Squash and merge` by default to keep clean commit history in Master branch.
- Use `Create a merge commit` for feature PRs **only if** the commit history of the PR is reasonably clean.
  After using this option, Github will make it your default option for merging a PR.
  So, do remember to change the default back to `Squash and merge`.
- Avoid `Rebase and merge` unless you have a strong argument for using it.
- Before clicking `Confirm squash and merge` or `Confirm merge`,
  make sure you run through the following steps:
  1. The commit title should be a concise summary of the PR.
     - When merging with the `Create a merge commit` option,
       the default commit title would be `Merge pull request XXX from XXX`.
       **Replace it with a concise summary of the PR**, and add the PR number to the end, like `(#1234)`.
     - When merging with the `Squash and merge` option,
       the PR title will be used as the commit title by default.
       **Reword the title as needed** to make sure it makes sense (can be used without change in `CHANGELOG.md`).
  1. The commit description is required for feature PRs or PRs with breaking changes.
     For other PRs, it's not required but good to have based on the judgement of the maintainer.
     - If a PR introduces breaking changes,
       make sure you put the tag `[breaking change]` at the first line of the description,
       and start the description text from the second line.
  1. Use the present tense and imperative mood for both the commit title and description.
