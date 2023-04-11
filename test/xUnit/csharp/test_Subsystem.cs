// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Subsystem;
using System.Management.Automation.Subsystem.DSC;
using System.Management.Automation.Subsystem.Feedback;
using System.Management.Automation.Subsystem.Prediction;
using System.Threading;
using Xunit;

namespace PSTests.Sequential
{
    public class MyInvalidSubsystem : ISubsystem
    {
        private readonly Guid _id;

        public static readonly MyInvalidSubsystem Singleton;

        static MyInvalidSubsystem()
        {
            Singleton = new MyInvalidSubsystem(Guid.NewGuid());
        }

        private MyInvalidSubsystem(Guid id)
        {
            _id = id;
        }

        public Guid Id => _id;

        public string Name => "Invalid";

        public string Description => "An invalid subsystem implementation";

        public Dictionary<string, string> FunctionsToDefine => null;
    }

    public class MyCompositeSubsystem : ICommandPredictor, IFeedbackProvider
    {
        private readonly Guid _id;

        public static readonly MyCompositeSubsystem Singleton;

        static MyCompositeSubsystem()
        {
            Singleton = new MyCompositeSubsystem(Guid.NewGuid());
        }

        private MyCompositeSubsystem(Guid id)
        {
            _id = id;
        }

        public Guid Id => _id;

        public string Name => "Composite";

        public string Description => "A composite implementation that serves as both a feedback provider and a command predictor.";

        Dictionary<string, string> ISubsystem.FunctionsToDefine => null;

        #region IFeedbackProvider

        public FeedbackItem GetFeedback(string commandLine, ErrorRecord errorRecord, CancellationToken token) => new FeedbackItem("nothing", null);

        #endregion

        #region ICommandPredictor

        public SuggestionPackage GetSuggestion(PredictionClient client, PredictionContext context, CancellationToken cancellationToken) => default;

        #endregion
    }

    public static class SubsystemTests
    {
        private static readonly MyPredictor predictor1, predictor2;

        static SubsystemTests()
        {
            predictor1 = MyPredictor.FastPredictor;
            predictor2 = MyPredictor.SlowPredictor;
        }

        private static void VerifyCommandPredictorMetadata(SubsystemInfo ssInfo)
        {
            Assert.Equal(SubsystemKind.CommandPredictor, ssInfo.Kind);
            Assert.Equal(typeof(ICommandPredictor), ssInfo.SubsystemType);
            Assert.True(ssInfo.AllowUnregistration);
            Assert.True(ssInfo.AllowMultipleRegistration);
            Assert.Empty(ssInfo.RequiredCmdlets);
            Assert.Empty(ssInfo.RequiredFunctions);
        }

        private static void VerifyCrossPlatformDscMetadata(SubsystemInfo ssInfo)
        {
            Assert.Equal(SubsystemKind.CrossPlatformDsc, ssInfo.Kind);
            Assert.Equal(typeof(ICrossPlatformDsc), ssInfo.SubsystemType);
            Assert.True(ssInfo.AllowUnregistration);
            Assert.False(ssInfo.AllowMultipleRegistration);
            Assert.Empty(ssInfo.RequiredCmdlets);
            Assert.Empty(ssInfo.RequiredFunctions);
        }

        private static void VerifyFeedbackProviderMetadata(SubsystemInfo ssInfo)
        {
            Assert.Equal(SubsystemKind.FeedbackProvider, ssInfo.Kind);
            Assert.Equal(typeof(IFeedbackProvider), ssInfo.SubsystemType);
            Assert.True(ssInfo.AllowUnregistration);
            Assert.True(ssInfo.AllowMultipleRegistration);
            Assert.Empty(ssInfo.RequiredCmdlets);
            Assert.Empty(ssInfo.RequiredFunctions);
        }

        [Fact]
        public static void GetSubsystemInfo()
        {
            #region Predictor
            SubsystemInfo predictorInfo = SubsystemManager.GetSubsystemInfo(typeof(ICommandPredictor));
            SubsystemInfo predictorInfo2 = SubsystemManager.GetSubsystemInfo(SubsystemKind.CommandPredictor);
            Assert.Same(predictorInfo2, predictorInfo);

            VerifyCommandPredictorMetadata(predictorInfo);
            Assert.False(predictorInfo.IsRegistered);
            Assert.Empty(predictorInfo.Implementations);
            #endregion

            #region Feedback
            SubsystemInfo feedbackProviderInfo = SubsystemManager.GetSubsystemInfo(typeof(IFeedbackProvider));
            SubsystemInfo feedback2 = SubsystemManager.GetSubsystemInfo(SubsystemKind.FeedbackProvider);
            Assert.Same(feedback2, feedbackProviderInfo);

            VerifyFeedbackProviderMetadata(feedbackProviderInfo);
            Assert.True(feedbackProviderInfo.IsRegistered);
            Assert.Single(feedbackProviderInfo.Implementations);
            #endregion

            #region DSC
            SubsystemInfo crossPlatformDscInfo = SubsystemManager.GetSubsystemInfo(typeof(ICrossPlatformDsc));
            SubsystemInfo crossPlatformDscInfo2 = SubsystemManager.GetSubsystemInfo(SubsystemKind.CrossPlatformDsc);
            Assert.Same(crossPlatformDscInfo2, crossPlatformDscInfo);
            VerifyCrossPlatformDscMetadata(crossPlatformDscInfo);

            Assert.False(crossPlatformDscInfo.IsRegistered);
            Assert.Empty(crossPlatformDscInfo.Implementations);
            #endregion

            ReadOnlyCollection<SubsystemInfo> ssInfos = SubsystemManager.GetAllSubsystemInfo();
            Assert.Equal(3, ssInfos.Count);
            Assert.Same(ssInfos[0], predictorInfo);
            Assert.Same(ssInfos[1], crossPlatformDscInfo);
            Assert.Same(ssInfos[2], feedbackProviderInfo);

            ICommandPredictor predictorImpl = SubsystemManager.GetSubsystem<ICommandPredictor>();
            Assert.Null(predictorImpl);
            ReadOnlyCollection<ICommandPredictor> predictorImpls = SubsystemManager.GetSubsystems<ICommandPredictor>();
            Assert.Empty(predictorImpls);

            ReadOnlyCollection<IFeedbackProvider> feedbackImpls = SubsystemManager.GetSubsystems<IFeedbackProvider>();
            Assert.Single(feedbackImpls);

            ICrossPlatformDsc crossPlatformDscImpl = SubsystemManager.GetSubsystem<ICrossPlatformDsc>();
            Assert.Null(crossPlatformDscImpl);
            ReadOnlyCollection<ICrossPlatformDsc> crossPlatformDscImpls = SubsystemManager.GetSubsystems<ICrossPlatformDsc>();
            Assert.Empty(crossPlatformDscImpls);
        }

        [Fact]
        public static void RegisterSubsystemExpectedFailures()
        {
            Assert.Throws<ArgumentNullException>(
                paramName: "proxy",
                () => SubsystemManager.RegisterSubsystem<ICommandPredictor, MyPredictor>(null));
            Assert.Throws<ArgumentNullException>(
                paramName: "proxy",
                () => SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, null));

            ArgumentException ex = Assert.Throws<ArgumentException>(
                paramName: "proxy",
                () => SubsystemManager.RegisterSubsystem(SubsystemKind.CrossPlatformDsc, predictor1));
            Assert.Contains(nameof(ICrossPlatformDsc), ex.Message);

            ex = Assert.Throws<ArgumentException>(
                paramName: "kind",
                () => SubsystemManager.RegisterSubsystem((SubsystemKind)0, predictor1));
            Assert.Contains("0", ex.Message);

            ex = Assert.Throws<ArgumentException>(
                paramName: "kind",
                () => SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor | SubsystemKind.CrossPlatformDsc, predictor1));
            Assert.Contains("3", ex.Message);

            // You cannot register the instance of a type that only implements 'ISubsystem'.
            ex = Assert.Throws<ArgumentException>(
                paramName: "proxy",
                () => SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, MyInvalidSubsystem.Singleton));
            Assert.Contains(nameof(ICommandPredictor), ex.Message);

            ex = Assert.Throws<ArgumentException>(
                paramName: "subsystemType",
                () => SubsystemManager.RegisterSubsystem<ISubsystem, MyInvalidSubsystem>(MyInvalidSubsystem.Singleton));
            Assert.Contains(nameof(ISubsystem), ex.Message);
        }

        [Fact]
        public static void RegisterSubsystemForCompositeImplementation()
        {
            try
            {
                SubsystemManager.RegisterSubsystem<ICommandPredictor, MyCompositeSubsystem>(MyCompositeSubsystem.Singleton);
                SubsystemManager.RegisterSubsystem(SubsystemKind.FeedbackProvider, MyCompositeSubsystem.Singleton);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, MyCompositeSubsystem.Singleton.Id);
                SubsystemManager.UnregisterSubsystem<IFeedbackProvider>(MyCompositeSubsystem.Singleton.Id);
            }
        }

        [Fact]
        public static void RegisterSubsystem()
        {
            try
            {
                // Register 'predictor1'
                SubsystemManager.RegisterSubsystem<ICommandPredictor, MyPredictor>(predictor1);

                // Now validate the SubsystemInfo of the 'ICommandPredictor' subsystem
                SubsystemInfo ssInfo = SubsystemManager.GetSubsystemInfo(typeof(ICommandPredictor));
                VerifyCommandPredictorMetadata(ssInfo);
                Assert.True(ssInfo.IsRegistered);
                Assert.Single(ssInfo.Implementations);

                // Now validate the 'ImplementationInfo'
                var implInfo = ssInfo.Implementations[0];
                Assert.Equal(predictor1.Id, implInfo.Id);
                Assert.Equal(predictor1.Name, implInfo.Name);
                Assert.Equal(predictor1.Description, implInfo.Description);
                Assert.Equal(SubsystemKind.CommandPredictor, implInfo.Kind);
                Assert.Same(typeof(MyPredictor), implInfo.ImplementationType);

                // Now validate the subsystem implementation itself.
                ICommandPredictor impl = SubsystemManager.GetSubsystem<ICommandPredictor>();
                Assert.Same(impl, predictor1);
                Assert.Null(impl.FunctionsToDefine);

                const string Client = "SubsystemTest";
                const string Input = "Hello world";
                var predClient = new PredictionClient(Client, PredictionClientKind.Terminal);
                var predCxt = PredictionContext.Create(Input);
                var results = impl.GetSuggestion(predClient, predCxt, CancellationToken.None);
                Assert.Equal($"'{Input}' from '{Client}' - TEST-1 from {impl.Name}", results.SuggestionEntries[0].SuggestionText);
                Assert.Equal($"'{Input}' from '{Client}' - TeSt-2 from {impl.Name}", results.SuggestionEntries[1].SuggestionText);

                // Now validate the all-subsystem-implementation collection.
                ReadOnlyCollection<ICommandPredictor> impls = SubsystemManager.GetSubsystems<ICommandPredictor>();
                Assert.Single(impls);
                Assert.Same(predictor1, impls[0]);

                // Register 'predictor2'
                SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, predictor2);

                // Now validate the SubsystemInfo of the 'ICommandPredictor' subsystem
                VerifyCommandPredictorMetadata(ssInfo);
                Assert.True(ssInfo.IsRegistered);
                Assert.Equal(2, ssInfo.Implementations.Count);

                // Now validate the new 'ImplementationInfo'
                implInfo = ssInfo.Implementations[1];
                Assert.Equal(predictor2.Id, implInfo.Id);
                Assert.Equal(predictor2.Name, implInfo.Name);
                Assert.Equal(predictor2.Description, implInfo.Description);
                Assert.Equal(SubsystemKind.CommandPredictor, implInfo.Kind);
                Assert.Same(typeof(MyPredictor), implInfo.ImplementationType);

                // Now validate the new subsystem implementation.
                impl = SubsystemManager.GetSubsystem<ICommandPredictor>();
                Assert.Same(impl, predictor2);

                // Now validate the all-subsystem-implementation collection.
                impls = SubsystemManager.GetSubsystems<ICommandPredictor>();
                Assert.Equal(2, impls.Count);
                Assert.Same(predictor1, impls[0]);
                Assert.Same(predictor2, impls[1]);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem<ICommandPredictor>(predictor1.Id);
                SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, predictor2.Id);
            }
        }

        [Fact]
        public static void UnregisterSubsystem()
        {
            // Exception expected when no implementation is registered
            Assert.Throws<InvalidOperationException>(() => SubsystemManager.UnregisterSubsystem<ICommandPredictor>(predictor1.Id));

            SubsystemManager.RegisterSubsystem<ICommandPredictor, MyPredictor>(predictor1);
            SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, predictor2);

            // Exception is expected when specified id cannot be found
            Assert.Throws<InvalidOperationException>(() => SubsystemManager.UnregisterSubsystem<ICommandPredictor>(Guid.NewGuid()));

            // Unregister 'predictor1'
            SubsystemManager.UnregisterSubsystem<ICommandPredictor>(predictor1.Id);

            SubsystemInfo ssInfo = SubsystemManager.GetSubsystemInfo(SubsystemKind.CommandPredictor);
            VerifyCommandPredictorMetadata(ssInfo);
            Assert.True(ssInfo.IsRegistered);
            Assert.Single(ssInfo.Implementations);

            var implInfo = ssInfo.Implementations[0];
            Assert.Equal(predictor2.Id, implInfo.Id);
            Assert.Equal(predictor2.Name, implInfo.Name);
            Assert.Equal(predictor2.Description, implInfo.Description);
            Assert.Equal(SubsystemKind.CommandPredictor, implInfo.Kind);
            Assert.Same(typeof(MyPredictor), implInfo.ImplementationType);

            ICommandPredictor impl = SubsystemManager.GetSubsystem<ICommandPredictor>();
            Assert.Same(impl, predictor2);

            ReadOnlyCollection<ICommandPredictor> impls = SubsystemManager.GetSubsystems<ICommandPredictor>();
            Assert.Single(impls);
            Assert.Same(predictor2, impls[0]);

            // Unregister 'predictor2'
            SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, predictor2.Id);

            VerifyCommandPredictorMetadata(ssInfo);
            Assert.False(ssInfo.IsRegistered);
            Assert.Empty(ssInfo.Implementations);

            impl = SubsystemManager.GetSubsystem<ICommandPredictor>();
            Assert.Null(impl);

            impls = SubsystemManager.GetSubsystems<ICommandPredictor>();
            Assert.Empty(impls);
        }
    }
}
