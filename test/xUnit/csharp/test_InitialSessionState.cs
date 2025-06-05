// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Xunit;
using DriveNotFoundException = System.Management.Automation.DriveNotFoundException;

namespace PSTests.Parallel;

public class InitialSessionStateTests
{
    [Fact]
    public void TestDefaultLocation()
    {
        var wd = GetResultingLocation(null);
        Assert.Equal(Environment.CurrentDirectory, wd.Path);
    }

    [Fact]
    public void TestCustomFileSystemLocation()
    {
        // any path different from the process working directory would work here
        var location = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

        // sanity check to ensure the test is not running from the tmp dir
        Assert.NotEqual(location, Environment.CurrentDirectory);

        var wd = GetResultingLocation(location);
        Assert.Equal(location, wd.Path);
    }

    [Fact]
    public void TestCustomEnvLocation()
    {
        var wd = GetResultingLocation("Env:");
        Assert.Equal("Env:" + Path.DirectorySeparatorChar, wd.Path);
    }

    [Fact]
    public void TestCustomLocation_NonExistentDrive()
    {
        Assert.Throws<DriveNotFoundException>(() => { GetResultingLocation("NonExistentDrive:"); });
    }

    [Fact]
    public void TestCustomLocation_NonExistentPath()
    {
        Assert.Throws<ItemNotFoundException>(() => { GetResultingLocation("Temp:/nonexistent test directory"); });
    }

    private static PathInfo GetResultingLocation(string issLocation)
    {
        var iss = InitialSessionState.CreateDefault2();
        iss.Location = issLocation;

        using var runspace = RunspaceFactory.CreateRunspace(iss);
        runspace.Open();

        using var ps = PowerShell.Create(runspace);
        return ps.AddCommand("pwd").Invoke<PathInfo>().Single();
    }
}
