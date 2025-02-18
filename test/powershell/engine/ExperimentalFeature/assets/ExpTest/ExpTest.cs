// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;

namespace ExperimentalFeatureTest
{
    #region "Replace existing cmdlet"

    [Experimental("ExpTest.FeatureOne", ExperimentAction.Hide)]
    [Cmdlet("Invoke", "AzureFunctionCSharp")]
    public class InvokeAzureFunctionCommand : PSCmdlet
    {
        [Parameter]
        public string Token { get; set; }

        [Parameter]
        public string Command { get; set; }

        protected override void EndProcessing()
        {
            WriteObject("Invoke-AzureFunction Version ONE");
        }
    }

    [Experimental("ExpTest.FeatureOne", ExperimentAction.Show)]
    [Cmdlet("Invoke", "AzureFunctionCSharp")]
    public class InvokeAzureFunctionCommandV2 : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Token { get; set; }

        [Parameter(Mandatory = true)]
        public string Command { get; set; }

        protected override void EndProcessing()
        {
            WriteObject("Invoke-AzureFunction Version TWO");
        }
    }

    #endregion

    #region "Make parameter set experimental"

    [Cmdlet("Get", "GreetingMessageCSharp", DefaultParameterSetName = "Default")]
    public class GetGreetingMessageCommand : PSCmdlet
    {
        [Parameter(Mandatory = true)]
        public string Name { get; set; }

        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, ParameterSetName = "SwitchOneSet")]
        public SwitchParameter SwitchOne { get; set; }

        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, ParameterSetName = "SwitchTwoSet")]
        public SwitchParameter SwitchTwo { get; set; }

        protected override void EndProcessing()
        {
            string message = $"Hello World {Name}.";
            if (ExperimentalFeature.IsEnabled("ExpTest.FeatureOne"))
            {
                if (SwitchOne.IsPresent)
                {
                    message += "-SwitchOne is on.";
                }

                if (SwitchTwo.IsPresent)
                {
                    message += "-SwitchTwo is on.";
                }
            }

            WriteObject(message);
        }
    }

    [Cmdlet("Invoke", "MyCommandCSharp")]
    public class InvokeMyCommandCommand : PSCmdlet
    {
        [Parameter(Mandatory = true, ParameterSetName = "ComputerSet")]
        public string UserName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "ComputerSet")]
        public string ComputerName { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "VMSet")]
        public string VMName { get; set; }

        // Enable web socket only if the feature is turned on.
        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, Mandatory = true, ParameterSetName = "WebSocketSet")]
        public string Token { get; set; }

        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, Mandatory = true, ParameterSetName = "WebSocketSet")]
        public string WebSocketUrl { get; set; }

        // Add -ConfigurationName to parameter set "WebSocketSet" only if the feature is turned on.
        [Parameter(ParameterSetName = "ComputerSet")]
        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, ParameterSetName = "WebSocketSet")]
        public string ConfigurationName { get; set; }

        // Add -Port to parameter set "WebSocketSet" only if the feature is turned on.
        [Parameter(ParameterSetName = "VMSet")]
        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show, ParameterSetName = "WebSocketSet")]
        public int Port { get; set; }

        [Parameter]
        public int ThrottleLimit { get; set; }

        [Parameter]
        public string Command { get; set; }

        protected override void EndProcessing()
        {
            switch (this.ParameterSetName)
            {
                case "ComputerSet": WriteObject("Invoke-MyCommand with ComputerSet"); break;
                case "VMSet": WriteObject("Invoke-MyCommand with VMSet"); break;
                case "WebSocketSet": WriteObject("Invoke-MyCommand with WebSocketSet"); break;
                default: break;
            }
        }
    }

    [Cmdlet("Test", "MyRemotingCSharp")]
    public class TestMyRemotingCommand : PSCmdlet
    {
        // Replace one parameter with another one when the feature is turned on.
        [Parameter("ExpTest.FeatureOne", ExperimentAction.Hide)]
        public string SessionName { get; set; }

        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show)]
        public string ComputerName { get; set; }

        protected override void EndProcessing() { }
    }

    #endregion

    #region "Use 'Experimental' attribute on parameters"

    [Cmdlet("Save", "MyFileCSharp")]
    public class SaveMyFileCommand : PSCmdlet
    {
        [Parameter(ParameterSetName = "UrlSet")]
        public SwitchParameter ByUrl { get; set; }

        [Parameter(ParameterSetName = "RadioSet")]
        public SwitchParameter ByRadio { get; set; }

        [Parameter]
        public string FileName { get; set; }
        
        [Experimental("ExpTest.FeatureOne", ExperimentAction.Show)]
        [Parameter]
        public string Destination { get; set; }

        [Experimental("ExpTest.FeatureOne", ExperimentAction.Hide)]
        [Parameter(ParameterSetName = "UrlSet")]
        [Parameter(ParameterSetName = "RadioSet")]
        public string Configuration { get; set; }

        protected override void EndProcessing() { }
    }

    #endregion

    #region "Dynamic parameters"

    public class DynamicParamOne
    {
        [Parameter("ExpTest.FeatureOne", ExperimentAction.Show)]
        [ValidateNotNullOrEmpty]
        public string ConfigFile { get; set; }

        [Parameter("ExpTest.FeatureOne", ExperimentAction.Hide)]
        [ValidateNotNullOrEmpty]
        public string ConfigName { get; set; }
    }

    [Cmdlet("Test", "MyDynamicParamOneCSharp")]
    public class TestMyDynamicParamOneCommand : PSCmdlet, IDynamicParameters
    {
        [Parameter(Position = 0)]
        public string Name { get; set; }

        public object GetDynamicParameters()
        {
            return Name == "Joe" ? new DynamicParamOne() : null;
        }

        protected override void EndProcessing() { }
    }

    public class DynamicParamTwo
    {
        [Experimental("ExpTest.FeatureOne", ExperimentAction.Show)]
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string ConfigFile { get; set; }

        [Experimental("ExpTest.FeatureOne", ExperimentAction.Hide)]
        [Parameter]
        [ValidateNotNullOrEmpty]
        public string ConfigName { get; set; }
    }

    [Cmdlet("Test", "MyDynamicParamTwoCSharp")]
    public class TestMyDynamicParamTwoCommand : PSCmdlet, IDynamicParameters
    {
        [Parameter(Position = 0)]
        public string Name { get; set; }

        public object GetDynamicParameters()
        {
            return Name == "Joe" ? new DynamicParamTwo() : null;
        }

        protected override void EndProcessing() { }
    }

    #endregion
}
