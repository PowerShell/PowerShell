// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.Feedback;
using System.Threading;
using Xunit;

namespace PSTests.Sequential
{
    public class MyFeedback : IFeedbackProvider
    {
        private readonly Guid _id;
        private readonly string _name, _description;
        private readonly bool _delay;

        public static readonly MyFeedback SlowFeedback;

        static MyFeedback()
        {
            SlowFeedback = new MyFeedback(
                Guid.NewGuid(),
                "Slow",
                "Description for #1 feedback provider.",
                delay: true);
        }

        private MyFeedback(Guid id, string name, string description, bool delay)
        {
            _id = id;
            _name = name;
            _description = description;
            _delay = delay;
        }

        public Guid Id => _id;

        public string Name => _name;

        public string Description => _description;

        public FeedbackItem GetFeedback(FeedbackContext context, CancellationToken token)
        {
            if (_delay)
            {
                // The delay is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                Thread.Sleep(2500);
            }

            return new FeedbackItem(
                "slow-feedback-caption",
                new List<string> { $"{context.CommandLine}+{context.LastError.FullyQualifiedErrorId}" });
        }
    }

    public static class FeedbackProviderTests
    {
        [Fact]
        public static void GetFeedback()
        {
            var pwsh = PowerShell.Create();
            var settings = new PSInvocationSettings() { AddToHistory = true };

            // Setup the context for the test.
            // Change current working directory to the temp path.
            pwsh.AddCommand("Set-Location")
                .AddParameter("Path", Path.GetTempPath())
                .Invoke(input: null, settings);
            pwsh.Commands.Clear();

            // Create an empty file 'feedbacktest.ps1' under the temp path;
            pwsh.AddCommand("New-Item")
                .AddParameter("Path", "feedbacktest.ps1")
                .Invoke(input: null, settings);
            pwsh.Commands.Clear();

            // Run a command 'feedbacktest', so as to trigger the 'general' feedback.
            pwsh.AddScript("feedbacktest").Invoke(input: null, settings);
            pwsh.Commands.Clear();

            try
            {
                // Register the slow feedback provider.
                // The 'general' feedback provider is built-in and registered by default.
                SubsystemManager.RegisterSubsystem(SubsystemKind.FeedbackProvider, MyFeedback.SlowFeedback);

                // Expect the result from 'general' only because the 'slow' one cannot finish before the specified timeout.
                // The specified timeout is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                var feedbacks = FeedbackHub.GetFeedback(pwsh.Runspace, millisecondsTimeout: 1500);
                string expectedCmd = Path.Combine(".", "feedbacktest");

                // Test the result from the 'general' feedback provider.
                Assert.Single(feedbacks);
                Assert.Equal("General Feedback", feedbacks[0].Name);
                Assert.Equal(expectedCmd, feedbacks[0].Item.RecommendedActions[0]);

                // Expect the result from both 'general' and the 'slow' feedback providers.
                // Same here -- the specified timeout is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                feedbacks = FeedbackHub.GetFeedback(pwsh.Runspace, millisecondsTimeout: 4000);
                Assert.Equal(2, feedbacks.Count);

                FeedbackResult entry1 = feedbacks[0];
                Assert.Equal("General Feedback", entry1.Name);
                Assert.Equal(expectedCmd, entry1.Item.RecommendedActions[0]);

                FeedbackResult entry2 = feedbacks[1];
                Assert.Equal("Slow", entry2.Name);
                Assert.Equal("feedbacktest+CommandNotFoundException", entry2.Item.RecommendedActions[0]);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem<IFeedbackProvider>(MyFeedback.SlowFeedback.Id);
            }
        }
    }
}
