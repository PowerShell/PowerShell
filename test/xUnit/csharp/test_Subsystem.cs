// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Management.Automation.Subsystem;
using System.Threading;
using Xunit;

namespace PSTests.Sequential
{
    public static class SubsystemTests
    {
        private readonly static MyPredictor predictor1, predictor2;

        static SubsystemTests()
        {
            predictor1 = MyPredictor.FastPredictor;
            predictor2 = MyPredictor.SlowPredictor;
        }

        // This method needs to be updated when there are more than 1 subsystem defined.
        private static void VerifySubsystemMetadata(SubsystemInfo ssInfo)
        {
            Assert.Equal(SubsystemKind.CommandPredictor, ssInfo.Kind);
            Assert.Equal(typeof(IPredictor), ssInfo.SubsystemType);
            Assert.True(ssInfo.AllowUnregistration);
            Assert.True(ssInfo.AllowMultipleRegistration);
            Assert.Empty(ssInfo.RequiredCmdlets);
            Assert.Empty(ssInfo.RequiredFunctions);
        }

        [Fact]
        public static void GetSubsystemInfo()
        {
            SubsystemInfo ssInfo = SubsystemManager.GetSubsystemInfo(typeof(IPredictor));

            VerifySubsystemMetadata(ssInfo);
            // Subsystem status
            Assert.False(ssInfo.IsRegistered);
            Assert.Empty(ssInfo.Implementations);

            SubsystemInfo ssInfo2 = SubsystemManager.GetSubsystemInfo(SubsystemKind.CommandPredictor);
            Assert.Same(ssInfo2, ssInfo);

            ReadOnlyCollection<SubsystemInfo> ssInfos = SubsystemManager.GetAllSubsystemInfo();
            Assert.Single(ssInfos);
            Assert.Same(ssInfos[0], ssInfo);

            IPredictor impl = SubsystemManager.GetSubsystem<IPredictor>();
            Assert.Null(impl);
            ReadOnlyCollection<IPredictor> impls = SubsystemManager.GetSubsystems<IPredictor>();
            Assert.Empty(impls);
        }

        [Fact]
        public static void RegisterSubsystem()
        {
            try
            {
                Assert.Throws<ArgumentNullException>(
                    paramName: "proxy",
                    () => SubsystemManager.RegisterSubsystem<IPredictor, MyPredictor>(null));
                Assert.Throws<ArgumentNullException>(
                    paramName: "proxy",
                    () => SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, null));
                Assert.Throws<ArgumentException>(
                    paramName: "proxy",
                    () => SubsystemManager.RegisterSubsystem((SubsystemKind)0, predictor1));

                // Register 'predictor1'
                SubsystemManager.RegisterSubsystem<IPredictor, MyPredictor>(predictor1);

                // Now validate the SubsystemInfo of the 'IPredictor' subsystem
                SubsystemInfo ssInfo = SubsystemManager.GetSubsystemInfo(typeof(IPredictor));
                VerifySubsystemMetadata(ssInfo);
                // Subsystem status changed
                Assert.True(ssInfo.IsRegistered);
                Assert.Single(ssInfo.Implementations);

                // Now validate the 'ImplementationInfo'
                var implInfo = ssInfo.Implementations[0];
                Assert.Equal(predictor1.Id, implInfo.Id);
                Assert.Equal(predictor1.Name, implInfo.Name);
                Assert.Equal(predictor1.Description, implInfo.Description);
                Assert.Equal(SubsystemKind.CommandPredictor, implInfo.Kind);
                Assert.Same(typeof(MyPredictor), implInfo.ImplementationType);

                // Now validate the all-subsystem-info collection.
                ReadOnlyCollection<SubsystemInfo> ssInfos = SubsystemManager.GetAllSubsystemInfo();
                Assert.Single(ssInfos);
                Assert.Same(ssInfos[0], ssInfo);

                // Now validate the subsystem implementation itself.
                IPredictor impl = SubsystemManager.GetSubsystem<IPredictor>();
                Assert.Same(impl, predictor1);
                Assert.Null(impl.FunctionsToDefine);
                Assert.Equal(SubsystemKind.CommandPredictor, impl.Kind);

                var predCxt = PredictionContext.Create("Hello world");
                var results = impl.GetSuggestion(predCxt, CancellationToken.None);
                Assert.Equal($"Hello world TEST-1 from {impl.Name}", results[0]);
                Assert.Equal($"Hello world TeSt-2 from {impl.Name}", results[1]);

                // Now validate the all-subsystem-implementation collection.
                ReadOnlyCollection<IPredictor> impls = SubsystemManager.GetSubsystems<IPredictor>();
                Assert.Single(impls);
                Assert.Same(predictor1, impls[0]);

                // Register 'predictor2'
                SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, predictor2);

                // Now validate the SubsystemInfo of the 'IPredictor' subsystem
                VerifySubsystemMetadata(ssInfo);
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
                impl = SubsystemManager.GetSubsystem<IPredictor>();
                Assert.Same(impl, predictor2);

                // Now validate the all-subsystem-implementation collection.
                impls = SubsystemManager.GetSubsystems<IPredictor>();
                Assert.Equal(2, impls.Count);
                Assert.Same(predictor1, impls[0]);
                Assert.Same(predictor2, impls[1]);
            }
            finally
            {
                SubsystemManager.UnregisterSubsystem<IPredictor>(predictor1.Id);
                SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, predictor2.Id);
            }
        }

        [Fact]
        public static void UnregisterSubsystem()
        {
            // Exception expected when no implementation is registered
            Assert.Throws<InvalidOperationException>(() => SubsystemManager.UnregisterSubsystem<IPredictor>(predictor1.Id));

            SubsystemManager.RegisterSubsystem<IPredictor, MyPredictor>(predictor1);
            SubsystemManager.RegisterSubsystem(SubsystemKind.CommandPredictor, predictor2);

            // Exception is expected when specified id cannot be found
            Assert.Throws<InvalidOperationException>(() => SubsystemManager.UnregisterSubsystem<IPredictor>(Guid.NewGuid()));

            // Unregister 'predictor1'
            SubsystemManager.UnregisterSubsystem<IPredictor>(predictor1.Id);

            SubsystemInfo ssInfo = SubsystemManager.GetSubsystemInfo(SubsystemKind.CommandPredictor);
            VerifySubsystemMetadata(ssInfo);
            // Subsystem status changed
            Assert.True(ssInfo.IsRegistered);
            Assert.Single(ssInfo.Implementations);

            var implInfo = ssInfo.Implementations[0];
            Assert.Equal(predictor2.Id, implInfo.Id);
            Assert.Equal(predictor2.Name, implInfo.Name);
            Assert.Equal(predictor2.Description, implInfo.Description);
            Assert.Equal(SubsystemKind.CommandPredictor, implInfo.Kind);
            Assert.Same(typeof(MyPredictor), implInfo.ImplementationType);

            IPredictor impl = SubsystemManager.GetSubsystem<IPredictor>();
            Assert.Same(impl, predictor2);

            ReadOnlyCollection<IPredictor> impls = SubsystemManager.GetSubsystems<IPredictor>();
            Assert.Single(impls);
            Assert.Same(predictor2, impls[0]);

            // Unregister 'predictor2'
            SubsystemManager.UnregisterSubsystem(SubsystemKind.CommandPredictor, predictor2.Id);

            VerifySubsystemMetadata(ssInfo);
            Assert.False(ssInfo.IsRegistered);
            Assert.Empty(ssInfo.Implementations);

            impl = SubsystemManager.GetSubsystem<IPredictor>();
            Assert.Null(impl);

            impls = SubsystemManager.GetSubsystems<IPredictor>();
            Assert.Empty(impls);
        }
    }
}
