using Xunit;
using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace PSTests
{
    public static class RunspaceTests
    {
        [Fact]
        public static void TestMethod()
        {
            InitialSessionState iss = InitialSessionState.CreateDefault2();

            // NOTE: instantiate custom host myHost for the next line to capture stdout and stderr output
            //       in addition to just the PSObjects
            using (Runspace rs = RunspaceFactory.CreateRunspace(/*myHost,*/iss))
            {
                rs.Open();
                using (PowerShell ps = PowerShell.Create())
                {
                    ps.Runspace = rs;

                    string script = "get-process | select-object -first 3";
                    ps.AddScript(script);

                    // IMPORTANT NOTE: do not call AddCommand("out-host") here or
                    // MergeMyResults, otherwise Invoke will not return any objects
                    
                    var results = ps.Invoke();

                    // check that there are 3 captured objects
                    int objCount = 0;
                    foreach (var result in results)
                    {
                        // this is how an object would be captured here and looked at,
                        // each result is a PSObject with the data from the pipeline
                        ++objCount;
                        Assert.NotNull(result);
                    }
                    Assert.Equal(3,objCount);
                    ps.Dispose();
                }
            }
        }
    }
}

