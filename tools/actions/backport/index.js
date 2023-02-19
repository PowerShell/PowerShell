// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// from https://github.com/dotnet/runtime/blob/main/eng/actions/backport/index.js

function BackportException(message, postToGitHub = true) {
    this.message = message;
    this.postToGitHub = postToGitHub;
  }

  async function run() {
    const util = require("util");
    const jsExec = util.promisify(require("child_process").exec);

    console.log("Installing npm dependencies");
    const { stdout, stderr } = await jsExec("npm install @actions/core @actions/github @actions/exec");
    console.log("npm-install stderr:\n\n" + stderr);
    console.log("npm-install stdout:\n\n" + stdout);
    console.log("Finished installing npm dependencies");

    const core = require("@actions/core");
    const github = require("@actions/github");
    const exec = require("@actions/exec");

    const repo_owner = github.context.payload.repository.owner.login;
    const repo_name = github.context.payload.repository.name;
    const pr_number = github.context.payload.issue.number;
    const comment_user = github.context.payload.comment.user.login;

    let octokit = github.getOctokit(core.getInput("auth_token", { required: true }));
    let target_branch = core.getInput("target_branch", { required: true });

    try {
      // verify the comment user is a repo collaborator
      try {
        await octokit.rest.repos.checkCollaborator({
          owner: repo_owner,
          repo: repo_name,
          username: comment_user
        });
        console.log(`Verified ${comment_user} is a repo collaborator.`);
      } catch (error) {
        console.log(error);
        throw new BackportException(`Error: @${comment_user} is not a repo collaborator, backporting is not allowed. If you're a collaborator please make sure your Microsoft team membership visibility is set to Public on https://github.com/orgs/microsoft/people?query=${comment_user}`);
      }

      try { await exec.exec(`git ls-remote --exit-code --heads origin ${target_branch}`) } catch { throw new BackportException(`Error: The specified backport target branch ${target_branch} wasn't found in the repo.`); }
      console.log(`Backport target branch: ${target_branch}`);

      console.log("Applying backport patch");

      await exec.exec(`git checkout ${target_branch}`);
      await exec.exec(`git clean -xdff`);

      // configure git
      await exec.exec(`git config user.name "github-actions"`);
      await exec.exec(`git config user.email "github-actions@github.com"`);

      // create temporary backport branch
      const temp_branch = `backport/pr-${pr_number}-to-${target_branch}`;
      await exec.exec(`git checkout -b ${temp_branch}`);

      // skip opening PR if the branch already exists on the origin remote since that means it was opened
      // by an earlier backport and force pushing to the branch updates the existing PR
      let should_open_pull_request = true;
      try {
        await exec.exec(`git ls-remote --exit-code --heads origin ${temp_branch}`);
        should_open_pull_request = false;
      } catch { }

      // download and apply patch
      await exec.exec(`curl -sSL "${github.context.payload.issue.pull_request.patch_url}" --output changes.patch`);

      const git_am_command = "git am --3way --ignore-whitespace --keep-non-patch changes.patch";
      let git_am_output = `$ ${git_am_command}\n\n`;
      let git_am_failed = false;
      try {
        await exec.exec(git_am_command, [], {
          listeners: {
            stdout: function stdout(data) { git_am_output += data; },
            stderr: function stderr(data) { git_am_output += data; }
          }
        });
      } catch (error) {
        git_am_output += error;
        git_am_failed = true;
      }

      if (git_am_failed) {
        const git_am_failed_body = `@${github.context.payload.comment.user.login} backporting to ${target_branch} failed, the patch most likely resulted in conflicts:\n\n\`\`\`shell\n${git_am_output}\n\`\`\`\n\nPlease backport manually!`;
        await octokit.rest.issues.createComment({
          owner: repo_owner,
          repo: repo_name,
          issue_number: pr_number,
          body: git_am_failed_body
        });
        throw new BackportException("Error: git am failed, most likely due to a merge conflict.", false);
      }
      else {
        // push the temp branch to the repository
        await exec.exec(`git push --force --set-upstream origin HEAD:${temp_branch}`);
      }

      if (!should_open_pull_request) {
        console.log("Backport temp branch already exists, skipping opening a PR.");
        return;
      }

      // prepare the GitHub PR details
      let backport_pr_title = core.getInput("pr_title_template");
      let backport_pr_description = core.getInput("pr_description_template");

      // get users to cc (append PR author if different from user who issued the backport command)
      let cc_users = `@${comment_user}`;
      if (comment_user != github.context.payload.issue.user.login) cc_users += ` @${github.context.payload.issue.user.login}`;

      // replace the special placeholder tokens with values
      backport_pr_title = backport_pr_title
        .replace(/%target_branch%/g, target_branch)
        .replace(/%source_pr_title%/g, github.context.payload.issue.title)
        .replace(/%source_pr_number%/g, github.context.payload.issue.number)
        .replace(/%cc_users%/g, cc_users);

      backport_pr_description = backport_pr_description
        .replace(/%target_branch%/g, target_branch)
        .replace(/%source_pr_title%/g, github.context.payload.issue.title)
        .replace(/%source_pr_number%/g, github.context.payload.issue.number)
        .replace(/%cc_users%/g, cc_users);

      // open the GitHub PR
      await octokit.rest.pulls.create({
        owner: repo_owner,
        repo: repo_name,
        title: backport_pr_title,
        body: backport_pr_description,
        head: temp_branch,
        base: target_branch
      });

      console.log("Successfully opened the GitHub PR.");
    } catch (error) {

      core.setFailed(error);

      if (error.postToGitHub === undefined || error.postToGitHub == true) {
        // post failure to GitHub comment
        const unknown_error_body = `@${comment_user} an error occurred while backporting to ${target_branch}, please check the run log for details!\n\n${error.message}`;
        await octokit.rest.issues.createComment({
          owner: repo_owner,
          repo: repo_name,
          issue_number: pr_number,
          body: unknown_error_body
        });
      }
    }
  }

  run();
