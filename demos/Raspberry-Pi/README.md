# Running PowerShell Core 6 on a Raspberry-Pi

## Setup your Pi

Note that CoreCLR (and thus PowerShell Core) will only work on Pi 2 and Pi 3 devices as other devices
like [Pi Zero](https://github.com/dotnet/coreclr/issues/10605) have an unsupported processor.

Download [Raspbian](https://www.raspberrypi.org/downloads/raspbian/) and follow the [installation instructions](https://www.raspberrypi.org/documentation/installation/installing-images/README.md) to get it onto your Pi.

Once your Pi is up and running, [enable SSH remoting](https://www.raspberrypi.org/documentation/remote-access/ssh/).

## Building PowerShell Core 6 for arm32

We'll need to cross-compile for the Linux arm32 architecture from Ubuntu.

Follow the [Linux instructions to Build PowerShell](https://github.com/PowerShell/PowerShell/blob/master/docs/building/linux.md).

Once your environment is working, you'll need to setup the toolchain for cross compilation:

```powershell
Start-PSBootstrap -BuildLinuxArm
```

You can now build PowerShell Core:

```powershell
Start-PSBuild -Clean -Runtime linux-arm -PSModuleRestore
```

Note that it's important to do a `-Clean` build because if you previously built for Ubuntu, it won't try to rebuild the native library `pslnative` for arm32.

## Copy the bits to your Pi

Use SSH to copy the bits remotely, replace `yourPi` with the name or IP address of your Pi.

```powershell
scp -r "$(split-path (Get-PSOutput))/*" pi@yourPi:/home/pi/powershell
```

## Get latest CoreCLR runtime

We need to get a CoreCLR that fixes a [threading bug](https://github.com/dotnet/coreclr/pull/13922) which is in DotNetCore 2.0.0.

You can do these steps locally on your Pi, but we're using SSH remoting here.

We'll be using the latest [build](https://github.com/dotnet/core-setup#daily-builds) from master which has the fix.
Note that at the time of authoring these instructions, the 2.0.x servicing build didn't have the necessary fix and the 2.1.x builds may be more unstable.

We'll use `curl` to get the latest DotNetCore runtime.

```bash
sudo apt install curl
```

Now we'll download it and unpack it.

```bash
# Connect to your Pi.
ssh pi@yourpi
# We'll make a folder to put latest CoreCLR runtime.
mkdir dotnet
cd dotnet
# Download the latest CoreCLR runtime.
curl -O https://dotnetcli.blob.core.windows.net/dotnet/Runtime/master/dotnet-runtime-latest-linux-arm.tar.gz
# Unpack it.
tar xvf ./dotnet-runtime-latest-linux-arm.tar.gz
# We're going to overwrite the CoreCLR bits we built with newer ones, replace the version named folder below as appropriate.
# If you build a newer version of PowerShell Core, you'll need to make sure you get latest CoreCLR runtime otherwise you may hit a segmentation fault.
cp shared/Microsoft.NetCore.App/2.1.0-preview1-25719-04/* ~/powershell
```

## Start PowerShell

```bash
~/powershell/powershell
```

Note that until arm32 is [fully supported by CoreCLR](https://github.com/dotnet/coreclr/issues/3977), it's not supported by PowerShell Core.

If you get an error complaining about `libunwind.so.8` not being found, you'll need to install it as it's required by CoreCLR.

```bash
sudo apt install libunwind8
```

Have fun!
