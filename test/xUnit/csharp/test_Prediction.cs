// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Management.Automation.Subsystem;
using System.Threading;
using Xunit;

namespace PSTests.Sequential
{
    public class MyPredictor : ICommandPredictor
    {
        private readonly Guid _id;
        private readonly string _name, _description;
        private readonly bool _delay;

        public List<string> History { get; }

        public List<string> AcceptedSuggestions { get; }

        public List<string> DisplayedSuggestions { get; }

        public static readonly MyPredictor SlowPredictor, FastPredictor;

        static MyPredictor()
        {
            SlowPredictor = new MyPredictor(
                Guid.NewGuid(),
                "Test Predictor #1",
                "Description for #1 predictor.",
                delay: true);

            FastPredictor = new MyPredictor(
                Guid.NewGuid(),
                "Test Predictor #2",
                "Description for #2 predictor.",
                delay: false);
        }

        private MyPredictor(Guid id, string name, string description, bool delay)
        {
            _id = id;
            _name = name;
            _description = description;
            _delay = delay;

            History = new List<string>();
            AcceptedSuggestions = new List<string>();
            DisplayedSuggestions = new List<string>();
        }

        public Guid Id => _id;

        public string Name => _name;

        public string Description => _description;

        bool ICommandPredictor.SupportEarlyProcessing => true;

        bool ICommandPredictor.AcceptFeedback => true;

        public void StartEarlyProcessing(string clientId, IReadOnlyList<string> history)
        {
            foreach (string item in history)
            {
                History.Add($"{clientId}-{item}");
            }
        }

        public void OnSuggestionDisplayed(string clientId, uint session, int countOrIndex)
        {
            DisplayedSuggestions.Add($"{clientId}-{session}-{countOrIndex}");
        }

        public void OnSuggestionAccepted(string clientId, uint session, string acceptedSuggestion)
        {
            AcceptedSuggestions.Add($"{clientId}-{session}-{acceptedSuggestion}");
        }

        public SuggestionPackage GetSuggestion(string clientId, PredictionContext context, CancellationToken cancellationToken)
        {
            if (_delay)
            {
                // The delay is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                Thread.Sleep(2000);
            }

            // You can get the user input from the AST.
            var userInput = context.InputAst.Extent.Text;
            var entries = new List<PredictiveSuggestion>
            {
                new PredictiveSuggestion($"'{userInput}' from '{clientId}' - TEST-1 from {Name}"),
                new PredictiveSuggestion($"'{userInput}' from '{clientId}' - TeSt-2 from {Name}"),
            };

            return new SuggestionPackage(56, entries);
        }
    }

    public static class CommandPredictionTests
    {
        private const string Client = "PredictionTest";
        private const uint Session = 56;

        [Fact]
        public static void PredictInput()
        {
            const string Input = "Hello world";
            MyPredictor slow = MyPredictor.SlowPredictor;
            MyPredictor fast = MyPredictor.FastPredictor;
            Ast ast = Parser.ParseInput(Input, out Token[] tokens, out _);

            // Returns null when no predictor implementation registered
            List<PredictionResult> results = CommandPrediction.PredictInput(Client, ast, tokens).Result;
            Assert.Null(results);

            try
            {
                // Register 2 predictor implementations
                SubsystemManager.RegisterSubsystem<ICommandPredictor, MyPredictor>(slow);
                SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, fast);

                // Expect the results from 'fast' predictor only b/c the 'slow' one
                // cannot finish before the specified timeout.
                // The specified timeout is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                results = CommandPrediction.PredictInput(Client, ast, tokens, millisecondsTimeout: 1000).Result;
                Assert.Single(results);

                PredictionResult res = results[0];
                Assert.Equal(fast.Id, res.Id);
                Assert.Equal(Session, res.Session);
                Assert.Equal(2, res.Suggestions.Count);
                Assert.Equal($"'{Input}' from '{Client}' - TEST-1 from {fast.Name}", res.Suggestions[0].SuggestionText);
                Assert.Equal($"'{Input}' from '{Client}' - TeSt-2 from {fast.Name}", res.Suggestions[1].SuggestionText);

                // Expect the results from both 'slow' and 'fast' predictors
                // Same here -- the specified timeout is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                results = CommandPrediction.PredictInput(Client, ast, tokens, millisecondsTimeout: 4000).Result;
                Assert.Equal(2, results.Count);

                PredictionResult res1 = results[0];
                Assert.Equal(slow.Id, res1.Id);
                Assert.Equal(Session, res1.Session);
                Assert.Equal(2, res1.Suggestions.Count);
                Assert.Equal($"'{Input}' from '{Client}' - TEST-1 from {slow.Name}", res1.Suggestions[0].SuggestionText);
                Assert.Equal($"'{Input}' from '{Client}' - TeSt-2 from {slow.Name}", res1.Suggestions[1].SuggestionText);

                PredictionResult res2 = results[1];
                Assert.Equal(fast.Id, res2.Id);
                Assert.Equal(Session, res2.Session);
                Assert.Equal(2, res2.Suggestions.Count);
                Assert.Equal($"'{Input}' from '{Client}' - TEST-1 from {fast.Name}", res2.Suggestions[0].SuggestionText);
                Assert.Equal($"'{Input}' from '{Client}' - TeSt-2 from {fast.Name}", res2.Suggestions[1].SuggestionText);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem<ICommandPredictor>(slow.Id);
                SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, fast.Id);
            }
        }

        [Fact]
        public static void Feedback()
        {
            MyPredictor slow = MyPredictor.SlowPredictor;
            MyPredictor fast = MyPredictor.FastPredictor;

            try
            {
                // Register 2 predictor implementations
                SubsystemManager.RegisterSubsystem<ICommandPredictor, MyPredictor>(slow);
                SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, fast);

                var history = new[] { "hello", "world" };
                var ids = new HashSet<Guid> { slow.Id, fast.Id };

                CommandPrediction.OnCommandLineAccepted(Client, history);
                CommandPrediction.OnSuggestionDisplayed(Client, slow.Id, Session, 2);
                CommandPrediction.OnSuggestionDisplayed(Client, fast.Id, Session, -1);
                CommandPrediction.OnSuggestionAccepted(Client, slow.Id, Session, "Yeah");

                // The calls to 'StartEarlyProcessing' and 'OnSuggestionAccepted' are queued in thread pool,
                // so we wait a bit to make sure the calls are done.
                while (slow.History.Count == 0 || slow.AcceptedSuggestions.Count == 0)
                {
                    Thread.Sleep(10);
                }

                Assert.Equal(2, slow.History.Count);
                Assert.Equal($"{Client}-{history[0]}", slow.History[0]);
                Assert.Equal($"{Client}-{history[1]}", slow.History[1]);

                Assert.Equal(2, fast.History.Count);
                Assert.Equal($"{Client}-{history[0]}", fast.History[0]);
                Assert.Equal($"{Client}-{history[1]}", fast.History[1]);

                Assert.Single(slow.DisplayedSuggestions);
                Assert.Equal($"{Client}-{Session}-2", slow.DisplayedSuggestions[0]);

                Assert.Single(fast.DisplayedSuggestions);
                Assert.Equal($"{Client}-{Session}--1", fast.DisplayedSuggestions[0]);

                Assert.Single(slow.AcceptedSuggestions);
                Assert.Equal($"{Client}-{Session}-Yeah", slow.AcceptedSuggestions[0]);

                Assert.Empty(fast.AcceptedSuggestions);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem<ICommandPredictor>(slow.Id);
                SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, fast.Id);
            }
        }
    }
}
