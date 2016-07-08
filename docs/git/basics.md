Getting started with git
========================

We are using Git version 2.9.0, but any recent version should be good.
It's recommended to learn the `git` command line tool for full
cross-platform experience and a deeper understanding of Git itself.

Install
---------

#### Windows

Install [Git for Windows][].

During the install process, choose these recommended settings:

* Use Git from the Windows Command Prompt
* Use OpenSSH
* Checkout Windows-style, commit Unix-style line endings
* Use Windows' default console window
* Enable file system caching

[Git for Windows]: https://git-scm.com/download/win

#### Linux

Install via the package manager:

```sh
sudo apt-get install git
```

Interactive tutorials
----------------------

There are (too) many Git tutorials on the internet. Here we post
references to our favorites.

#### Hello world

If you're new to Git, learn the following commands: `checkout`, `branch`,
`pull`, `push`, `merge`.

Use GitHub's [Hello World][] to learn how to create a feature branch, commit
changes, and issue a pull request.

[hello world]: https://guides.github.com/activities/hello-world/

#### Githug

[Githug](https://github.com/Gazler/githug) is a great gamified way to
learn Git in couple hours. After finishing 50+ real-world scenarios
you will have a pretty good idea about what and when you can do with
Git.


Authentication
--------------

If you do not have a preferred method of authentication, enable the storage
credential helper, which will cache your credentials in plaintext on your
system, so use a [token][].

```sh
git config --global credential.helper store
```

Alternatively, on Windows, you can try the
[Git Credential Manager for Windows][manager].

[token]: https://help.github.com/articles/creating-an-access-token-for-command-line-use/
[manager]: https://github.com/Microsoft/Git-Credential-Manager-for-Windows


Permissions
-----------

If you have difficulty in pushing your changes, there is a high probability that
you actually don't have permissions.

Be sure that you have write access to corresponding repo (remember that
submodules have their own privilege).

Your should push to this repository instead of a fork so that the CI system can
provide credentials to your pull request. If you make a pull request from a
fork, the CI *will* fail.

