# Submodules

While most developers will not have to deal with submodules on a regular basis, those who do should read this information.
The submodules currently in this project are:

- `src/libpsl-native/test/googletest`: The GoogleTest framework for
  Linux native code

[submodules]: https://www.git-scm.com/book/en/v2/Git-Tools-Submodules

## Rebase and Fast-Forward Merge Pull Requests in Submodules

Note: *This is not necessary in the superproject, only submodules!*

**DO NOT** commit updates unless absolutely necessary.
When submodules must be updated, a separate Pull Request must be submitted, reviewed, and merged before updating the superproject.

Because GitHub's "Merge Pull Request" button merges with `--no-ff`, an extra merge commit will always be created.
This is especially annoying when trying to commit updates to submodules.
Therefore our policy is to merge using the Git CLI after approval, with a rebase onto master to enable a fast-forward merge.

When committing submodule updates, ensure no other changes are in the same commit.
Submodule bumps may be included in feature branches for ease of work,
but the update must be independently approved before merging into master.
