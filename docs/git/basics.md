# Getting started with Git

We are using Git version 2.9.0, but any recent version should be good.
It's recommended to learn the `git` command-line tool for full
cross-platform experience and a deeper understanding of Git itself.

## Install

### Windows

Install [Git for Windows][].

During the installation process, choose these recommended settings:

* Use Git from the Windows Command Prompt
* Use OpenSSH
* Checkout Windows-style, commit Unix-style line endings
* Use Windows' default console window
* Enable file system caching

[Git for Windows]: https://git-scm.com/download/win

### Linux

Install by using the package manager on your system.
A list of all the package managers and commands can be found [here][linux-git-dl].

### Interactive tutorials

There are (too) many Git tutorials on the internet. Here we post
references to our favorites.

#### Hello World

If you're new to Git, learn the following commands: `checkout`, `branch`,
`pull`, `push`, `merge`.

Use GitHub's [Hello World][] to learn how to create a feature branch, commit
changes, and issue a pull request.

[Hello World]: https://guides.github.com/activities/hello-world/

#### Katacoda

Learn basic Git scenarios in the browser with interactive labs.
[Git lessons on katacoda](https://www.katacoda.com/courses/git/).

#### Githug

[Githug](https://github.com/Gazler/githug) is a great gamified way to
learn Git in couple hours. After finishing 50+ real-world scenarios
you will have a pretty good idea about what and when you can do with
Git.

## Authentication

### Windows

On Windows, the best way to use Git securely is [Git Credential Manager for Windows][manager].
It's included in the official Git installer for Windows.

#### Linux and macOS

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

Alternatively, you can use [SSH key][].
In this case, you may want to use git-ssh even for HTTPS Git URLs.

```none
git config --global url.git@github.com:.insteadOf https://github.com/
```

[SSH key]: https://help.github.com/articles/generating-a-new-ssh-key-and-adding-it-to-the-ssh-agent/#generating-a-new-ssh-key
[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/
[manager]: https://github.com/Microsoft/Git-Credential-Manager-for-Windows
[linux-git-dl]: https://git-scm.com/download/linux
