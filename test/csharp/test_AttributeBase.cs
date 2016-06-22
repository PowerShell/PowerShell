using Xunit;
using System;
using System.Management.Automation;

namespace PSTests
{
    public class PowerShellTestBase
    {
        public static object ExecuteScript(string script)
        {
            if ( script == null || string.IsNullOrWhiteSpace(script))
            {
                throw new ArgumentException("Script my not be null or empty");
            }
            using(PowerShell ps = PowerShell.Create())
            {
                return ps.AddCommand(script).Invoke();
                
            }
        }
    }

    public class PowerShellTestAttribute : FactAttribute
    {
        public string Pending 
        { 
            get { return this.Skip; } 
            set { this.Skip = string.Format("Pending: {0}", value); } 
        }
    }
    public class CiFact : PowerShellTestAttribute { }
    public class FeatureFact : PowerShellTestAttribute { }
    public class ScenarioFact : PowerShellTestAttribute { }

    public class PowerShellTheoryAttribute : TheoryAttribute
    {
        public string Pending 
        { 
            get { return this.Skip; } 
            set { this.Skip = string.Format("Pending: {0}", value); } 
        }
    }
    public class CiTheory : PowerShellTheoryAttribute { }
    public class FeatureTheory : PowerShellTheoryAttribute { }
    public class ScenarioTheory : PowerShellTheoryAttribute { }

}
