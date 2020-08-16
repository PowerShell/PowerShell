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

        public static readonly MyPredictor SlowPredictor;

        public static readonly MyPredictor FastPredictor;

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
        }

        public Guid Id => _id;

        public string Name => _name;

        public string Description => _description;

        bool ICommandPredictor.SupportEarlyProcessing => true;

        bool ICommandPredictor.AcceptFeedback => true;

        public void StartEarlyProcessing(IReadOnlyList<string> history)
        {
            History.AddRange(history);
        }

        public void OnSuggestionAccepted(string acceptedSuggestion)
        {
            AcceptedSuggestions.Add(acceptedSuggestion);
        }

        public List<PredictiveSuggestion> GetSuggestion(PredictionContext context, CancellationToken cancellationToken)
        {
            if (_delay)
            {
                // The delay is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                Thread.Sleep(2000);
            }

            // You can get the user input from the AST.
            var userInput = context.InputAst.Extent.Text;
            return new List<PredictiveSuggestion> {
                new PredictiveSuggestion($"{userInput} TEST-1 from {Name}"),
                new PredictiveSuggestion($"{userInput} TeSt-2 from {Name}"),
            };
        }
    }

    public static class CommandPredictionTests
    {
        [Fact]
        public static void PredictInput()
        {
            MyPredictor slow = MyPredictor.SlowPredictor;
            MyPredictor fast = MyPredictor.FastPredictor;
            Ast ast = Parser.ParseInput("Hello world", out Token[] tokens, out _);

            // Returns null when no predictor implementation registered
            List<PredictionResult> results = CommandPrediction.PredictInput(ast, tokens).Result;
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
                results = CommandPrediction.PredictInput(ast, tokens, millisecondsTimeout: 1000).Result;
                Assert.Single(results);

                PredictionResult res = results[0];
                Assert.Equal(fast.Id, res.Id);
                Assert.Equal(2, res.Suggestions.Count);
                Assert.Equal($"Hello world TEST-1 from {fast.Name}", res.Suggestions[0].SuggestionText);
                Assert.Equal($"Hello world TeSt-2 from {fast.Name}", res.Suggestions[1].SuggestionText);

                // Expect the results from both 'slow' and 'fast' predictors
                // Same here -- the specified timeout is exaggerated to make the test reliable.
                // xUnit must spin up a lot tasks, which makes the test unreliable when the time difference between 'delay' and 'timeout' is small.
                results = CommandPrediction.PredictInput(ast, tokens, millisecondsTimeout: 4000).Result;
                Assert.Equal(2, results.Count);

                PredictionResult res1 = results[0];
                Assert.Equal(slow.Id, res1.Id);
                Assert.Equal(2, res1.Suggestions.Count);
                Assert.Equal($"Hello world TEST-1 from {slow.Name}", res1.Suggestions[0].SuggestionText);
                Assert.Equal($"Hello world TeSt-2 from {slow.Name}", res1.Suggestions[1].SuggestionText);

                PredictionResult res2 = results[1];
                Assert.Equal(fast.Id, res2.Id);
                Assert.Equal(2, res2.Suggestions.Count);
                Assert.Equal($"Hello world TEST-1 from {fast.Name}", res2.Suggestions[0].SuggestionText);
                Assert.Equal($"Hello world TeSt-2 from {fast.Name}", res2.Suggestions[1].SuggestionText);
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

                CommandPrediction.OnCommandLineAccepted(history);
                CommandPrediction.OnSuggestionAccepted(slow.Id, "Yeah");

                // The calls to 'EarlyProcessWithHistory' and the feedback methods are queued in thread pool,
                // so we wait a bit to make sure the calls are done.
                Thread.Sleep(10);

                Assert.Equal(2, slow.History.Count);
                Assert.Equal(history[0], slow.History[0]);
                Assert.Equal(history[1], slow.History[1]);

                Assert.Equal(2, fast.History.Count);
                Assert.Equal(history[0], fast.History[0]);
                Assert.Equal(history[1], fast.History[1]);

                Assert.Single(slow.AcceptedSuggestions);
                Assert.Equal("Yeah", slow.AcceptedSuggestions[0]);

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
