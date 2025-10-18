// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Xunit;

namespace PSTests.Parallel
{
    /// <summary>
    /// Test class to verify that xUnit test failures are correctly propagated in CI workflows.
    /// This test is intentionally designed to fail to validate the refactored xUnit workflow.
    /// </summary>
    public static class WorkflowValidationTests
    {
        /// <summary>
        /// This test intentionally fails to verify that the refactored xUnit workflow
        /// correctly propagates test failures and fails the CI job immediately.
        /// 
        /// Expected behavior:
        /// - Test fails with Assert.True(false)
        /// - Workflow job fails immediately (no masking with continue-on-error)
        /// - No delayed verification in separate job
        /// - Test results uploaded as artifacts for analysis
        /// 
        /// To restore normal CI behavior, either:
        /// 1. Remove this test file, or
        /// 2. Change Assert.True(false) to Assert.True(true)
        /// </summary>
        [Fact]
        public static void ValidateWorkflowFailurePropagation()
        {
            // This assertion will fail to verify the workflow correctly handles failures
            Assert.True(false, "This test intentionally fails to validate that xUnit test failures " +
                              "are correctly propagated in the refactored CI workflow. " +
                              "The workflow should fail immediately without masking errors.");
        }
    }
}
