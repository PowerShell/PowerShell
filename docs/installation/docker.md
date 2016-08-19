Docker
======

If you are using Docker, there is a wery simple way to try PowerShell:

```
docker run -it powershell
```

This Docker image is based on Ubuntu 16.04, and follows the instructions from the [Linux Installation docs][u16].
[u16]: linux.md#ubuntu-1604

Build
=====

If you want to build it yourself:
```
docker build -t powershell .
```

