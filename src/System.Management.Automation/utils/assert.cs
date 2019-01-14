// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// The define below is only valid for this file. It allows the methods
// defined here to call Diagnostics.Assert when only ASSERTIONS_TRACE is defined
// Any #if DEBUG is pointless (always true) in this file because of this declaration.
// The presence of the define will cause the System.Diagnostics.Debug.Asser calls
// always to be compiled in for this file. What can be compiled out are the calls to
// System.Management.Automation.Diagnostics.Assert in other files when neither DEBUG
// nor ASSERTIONS_TRACE is defined.
#define DEBUG

using System.Diagnostics;
using System.Text;

namespace System.Management.Automation
{
    /// <summary>
    /// Exception with a full stack trace excluding the last two frames.
    /// </summary>
    internal class AssertException : SystemException
    {
        /// <summary>
        /// Calls the base class with message and sets the stack frame.
        /// </summary>
        /// <param name="message">Repassed to the base class.</param>
        internal AssertException(string message) : base(message)
        {
            // 3 will skip the assertion caller, this method and AssertException.StackTrace
            StackTrace = Diagnostics.StackTrace(3);
        }

        /// <summary>
        /// Returns the stack trace set in the constructor.
        /// </summary>
        /// <value>the constructor's stackTrace</value>
        public override string StackTrace { get; }
    }

    /// <summary>
    /// This class contain the few methods necessary for
    /// the basic assertion use.
    /// </summary>
    /// <remarks>
    /// All methods are public and static.
    /// The class cannot derive from the sealed System.Diagnostics.Debug
    /// The class was also made sealed.
    /// <newpara/>
    /// <example>
    /// <code>
    /// Diagnostics.Assert(x >= 0,"A negative x would have caused early return.");
    /// </code>
    /// </example>
    /// <newpara/>
    /// </remarks>
    internal sealed class Diagnostics
    {
        internal static string StackTrace(int framesToSkip)
        {
            StackTrace trace = new StackTrace(true);
            StackFrame[] frames = trace.GetFrames();
            StringBuilder frameString = new StringBuilder();
            int maxFrames = 10;
            maxFrames += framesToSkip;
            for (int i = framesToSkip; (i < frames.Length) && (i < maxFrames); i++)
            {
                StackFrame frame = frames[i];
                frameString.Append(frame.ToString());
            }

            return frameString.ToString();
        }

        private static object s_throwInsteadOfAssertLock = 1;

        private static bool s_throwInsteadOfAssert = false;
        /// <summary>
        /// If set to true will prevent the assertion dialog from showing up
        /// by throwing an exception instead of calling Debug.Assert.
        /// </summary>
        /// <value>false for dialog, true for exception</value>
        internal static bool ThrowInsteadOfAssert
        {
            get
            {
                lock (s_throwInsteadOfAssertLock)
                {
                    return s_throwInsteadOfAssert;
                }
            }

            set
            {
                lock (s_throwInsteadOfAssertLock)
                {
                    s_throwInsteadOfAssert = value;
                }
            }
        }

        /// <summary>
        /// This class only has statics, so we shouldn't need to instantiate any object.
        /// </summary>
        private Diagnostics() { }

        /// <summary>
        /// Basic assertion with logical condition and message.
        /// </summary>
        /// <param name="condition">
        /// logical condition that should be true for program to proceed
        /// </param>
        /// <param name="whyThisShouldNeverHappen">
        /// Message to explain why condition should always be true
        /// </param>
        // These two lines are playing havoc with asmmeta. Since only one asmmeta file
        // can be checked in at a time if you compile the asmmeta for a fre build then
        // the checked can't compile against it since these methods will not exist. If
        // you check in the chk asmmeta the fre build will not compile because it is
        // not expecting these methods to exist.
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.Conditional("ASSERTIONS_TRACE")]
#if RESHARPER_ATTRIBUTES
        [JetBrains.Annotations.AssertionMethod]
#endif
        internal static void Assert(
#if RESHARPER_ATTRIBUTES
            [JetBrains.Annotations.AssertionCondition(JetBrains.Annotations.AssertionConditionType.IS_TRUE)]
#endif
            bool condition,
            string whyThisShouldNeverHappen)
        {
            Diagnostics.Assert(condition, whyThisShouldNeverHappen, string.Empty);
        }

        /// <summary>
        /// Basic assertion with logical condition, message and detailed message.
        /// </summary>
        /// <param name="condition">
        /// logical condition that should be true for program to proceed
        /// </param>
        /// <param name="whyThisShouldNeverHappen">
        /// Message to explain why condition should always be true
        /// </param>
        /// <param name="detailMessage">
        /// Additional information about the assertion
        /// </param>
        // These two lines are playing havoc with asmmeta. Since only one asmmeta file
        // can be checked in at a time if you compile the asmmeta for a fre build then
        // the checked can't compile against it since these methods will not exist. If
        // you check in the chk asmmeta the fre build will not compile because it is
        // not expecting these methods to exist.
        [System.Diagnostics.Conditional("DEBUG")]
        [System.Diagnostics.Conditional("ASSERTIONS_TRACE")]
#if RESHARPER_ATTRIBUTES
        [JetBrains.Annotations.AssertionMethod]
#endif
        internal static void
        Assert(
#if RESHARPER_ATTRIBUTES
            [JetBrains.Annotations.AssertionCondition(JetBrains.Annotations.AssertionConditionType.IS_TRUE)]
#endif
            bool condition,
            string whyThisShouldNeverHappen, string detailMessage)
        {
            // Early out avoids some slower code below (mostly the locking done in ThrowInsteadOfAssert).
            if (condition) return;

#if ASSERTIONS_TRACE
            if (!condition)
            {
                if (Diagnostics.ThrowInsteadOfAssert)
                {
                    string assertionMessage = "ASSERT: " + whyThisShouldNeverHappen + "  " + detailMessage + " ";
                    AssertException e = new AssertException(assertionMessage);
                    tracer.TraceException(e);
                    throw e;
                }

                StringBuilder builder = new StringBuilder();
                builder.Append("ASSERT: ");
                builder.Append(whyThisShouldNeverHappen);
                builder.Append(".");
                if (detailMessage.Length != 0)
                {
                    builder.Append(detailMessage);
                    builder.Append(".");
                }
                // 2 to skip this method and Diagnostics.StackTace
                builder.Append(Diagnostics.StackTrace(2));
                tracer.TraceError(builder.ToString());
            }
#else
            if (Diagnostics.ThrowInsteadOfAssert)
            {
                string assertionMessage = "ASSERT: " + whyThisShouldNeverHappen + "  " + detailMessage + " ";
                throw new AssertException(assertionMessage);
            }

            System.Diagnostics.Debug.Fail(whyThisShouldNeverHappen, detailMessage);
#endif
        }
    }
}

