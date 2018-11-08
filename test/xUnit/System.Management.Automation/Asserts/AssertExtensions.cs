// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;
using System.Linq;

namespace System
{
    // The tests come from CoreFX tests: src/CoreFx.Private.TestUtilities/src/System/

    public static class AssertExtensions
    {
        private static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");

        public static void Throws<T>(Action action, string message)
            where T : Exception
        {
            Assert.Equal(Assert.Throws<T>(action).Message, message);
        }

        public static void Throws<T>(string netCoreParamName, string netFxParamName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            if (netFxParamName == null && IsFullFramework)
            {
                // Param name varies between NETFX versions -- skip checking it
                return;
            }

            string expectedParamName =
                IsFullFramework ?
                netFxParamName : netCoreParamName;

            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                Assert.Equal(expectedParamName, exception.ParamName);
        }

        public static void Throws<T>(string netCoreParamName, string netFxParamName, Func<object> testCode)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(testCode);

            if (netFxParamName == null && IsFullFramework)
            {
                // Param name varies between NETFX versions -- skip checking it
                return;
            }

            string expectedParamName =
                IsFullFramework ?
                netFxParamName : netCoreParamName;

            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                Assert.Equal(expectedParamName, exception.ParamName);
        }

        public static T Throws<T>(string paramName, Action action)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(action);

            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                Assert.Equal(paramName, exception.ParamName);

            return exception;
        }

        public static T Throws<T>(Action action)
            where T : Exception
        {
            T exception = Assert.Throws<T>(action);

            return exception;
        }

        public static T Throws<T>(string paramName, Func<object> testCode)
            where T : ArgumentException
        {
            T exception = Assert.Throws<T>(testCode);

            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                Assert.Equal(paramName, exception.ParamName);

            return exception;
        }

        public static async Task<T> ThrowsAsync<T>(string paramName, Func<Task> testCode)
            where T : ArgumentException
        {
            T exception = await Assert.ThrowsAsync<T>(testCode);

            if (!RuntimeInformation.FrameworkDescription.StartsWith(".NET Native"))
                Assert.Equal(paramName, exception.ParamName);

            return exception;
        }

        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string paramName, Action action)
            where TNetCoreExceptionType : ArgumentException
            where TNetFxExceptionType : ArgumentException
        {
            Throws<TNetCoreExceptionType, TNetFxExceptionType>(paramName, paramName, action);
        }

        public static Exception Throws<TNetCoreExceptionType, TNetFxExceptionType>(Action action)
            where TNetCoreExceptionType : Exception
            where TNetFxExceptionType : Exception
        {
            return Throws(typeof(TNetCoreExceptionType), typeof(TNetFxExceptionType), action);
        }

        public static Exception Throws(Type netCoreExceptionType, Type netFxExceptionType, Action action)
        {
            if (IsFullFramework)
            {
                return Assert.Throws(netFxExceptionType, action);
            }
            else
            {
                return Assert.Throws(netCoreExceptionType, action);
            }
        }

        public static void Throws<TNetCoreExceptionType, TNetFxExceptionType>(string netCoreParamName, string netFxParamName, Action action)
            where TNetCoreExceptionType : ArgumentException
            where TNetFxExceptionType : ArgumentException
        {
            if (IsFullFramework)
            {
                Throws<TNetFxExceptionType>(netFxParamName, action);
            }
            else
            {
                Throws<TNetCoreExceptionType>(netCoreParamName, action);
            }
        }

        public static void ThrowsAny(Type firstExceptionType, Type secondExceptionType, Action action)
        {
            ThrowsAnyInternal(action, firstExceptionType, secondExceptionType);
        }

        private static void ThrowsAnyInternal(Action action, params Type[] exceptionTypes)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Type exceptionType = e.GetType();
                if (exceptionTypes.Any(t => t.Equals(exceptionType)))
                    return;

                throw new XunitException($"Expected one of: ({string.Join<Type>(", ", exceptionTypes)}) -> Actual: ({e.GetType()})");
            }

            throw new XunitException($"Expected one of: ({string.Join<Type>(", ", exceptionTypes)}) -> Actual: No exception thrown");
        }

        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType>(Action action)
            where TFirstExceptionType : Exception
            where TSecondExceptionType : Exception
        {
            ThrowsAnyInternal(action, typeof(TFirstExceptionType), typeof(TSecondExceptionType));
        }

        public static void ThrowsAny<TFirstExceptionType, TSecondExceptionType, TThirdExceptionType>(Action action)
            where TFirstExceptionType : Exception
            where TSecondExceptionType : Exception
            where TThirdExceptionType : Exception
        {
            ThrowsAnyInternal(action, typeof(TFirstExceptionType), typeof(TSecondExceptionType), typeof(TThirdExceptionType));
        }

        public static void ThrowsIf<T>(bool condition, Action action)
            where T : Exception
        {
            if (condition)
            {
                Assert.Throws<T>(action);
            }
            else
            {
                action();
            }
        }

        private static string AddOptionalUserMessage(string message, string userMessage)
        {
            if (userMessage == null)
                return message;
            else
                return $"{message} {userMessage}";
        }
        
        /// <summary>
        /// Tests whether the specified string contains the specified substring
        /// and throws an exception if the substring does not occur within the
        /// test string or if either string or substring is null.
        /// </summary>
        /// <param name="value">
        /// The string that is expected to contain <paramref name="substring"/>.
        /// </param>
        /// <param name="substring">
        /// The string expected to occur within <paramref name="value"/>.
        /// </param>
        public static void Contains(string value, string substring)
        {
            Assert.NotNull(value);
            Assert.NotNull(substring);
            Assert.Contains(substring, value, StringComparison.Ordinal);
        }

        /// <summary>
        /// Validate that a given value is greater than another value.
        /// </summary>
        /// <param name="actual">The value that should be greater than <paramref name="greaterThan"/>.</param>
        /// <param name="greaterThan">The value that <paramref name="actual"/> should be greater than.</param>
        public static void GreaterThan<T>(T actual, T greaterThan, string userMessage = null) where T : IComparable
        {
            if (actual == null)
                throw new XunitException(
                    greaterThan == null
                        ? AddOptionalUserMessage($"Expected: <null> to be greater than <null>.", userMessage)
                        : AddOptionalUserMessage($"Expected: <null> to be greater than {greaterThan}.", userMessage));

            if (actual.CompareTo(greaterThan) <= 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be greater than {greaterThan}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is less than another value.
        /// </summary>
        /// <param name="actual">The value that should be less than <paramref name="lessThan"/>.</param>
        /// <param name="lessThan">The value that <paramref name="actual"/> should be less than.</param>
        public static void LessThan<T>(T actual, T lessThan, string userMessage = null) where T : IComparable
        {
            if (actual == null)
            {
                if (lessThan == null)
                {
                    throw new XunitException(AddOptionalUserMessage($"Expected: <null> to be less than <null>.", userMessage));
                }
                else
                {
                    // Null is always less than non-null
                    return;
                }
            }

            if (actual.CompareTo(lessThan) >= 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be less than {lessThan}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is less than or equal to another value.
        /// </summary>
        /// <param name="actual">The value that should be less than or equal to <paramref name="lessThanOrEqualTo"/></param>
        /// <param name="lessThanOrEqualTo">The value that <paramref name="actual"/> should be less than or equal to.</param>
        public static void LessThanOrEqualTo<T>(T actual, T lessThanOrEqualTo, string userMessage = null) where T : IComparable
        {
            // null, by definition is always less than or equal to
            if (actual == null)
                return;

            if (actual.CompareTo(lessThanOrEqualTo) > 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be less than or equal to {lessThanOrEqualTo}", userMessage));
        }

        /// <summary>
        /// Validate that a given value is greater than or equal to another value.
        /// </summary>
        /// <param name="actual">The value that should be greater than or equal to <paramref name="greaterThanOrEqualTo"/></param>
        /// <param name="greaterThanOrEqualTo">The value that <paramref name="actual"/> should be greater than or equal to.</param>
        public static void GreaterThanOrEqualTo<T>(T actual, T greaterThanOrEqualTo, string userMessage = null) where T : IComparable
        {
            // null, by definition is always less than or equal to
            if (actual == null)
            {
                if (greaterThanOrEqualTo == null)
                {
                    // We're equal
                    return;
                }
                else
                {
                    // Null is always less than non-null
                    throw new XunitException(AddOptionalUserMessage($"Expected: <null> to be greater than or equal to <null>.", userMessage));
                }
            }

            if (actual.CompareTo(greaterThanOrEqualTo) < 0)
                throw new XunitException(AddOptionalUserMessage($"Expected: {actual} to be greater than or equal to {greaterThanOrEqualTo}", userMessage));
        }

        /// <summary>
        /// Validates that the actual byte array is equal to the expected byte array. XUnit only displays the first 5 values
        /// of each collection if the test fails. This doesn't display at what point or how the equality assertion failed.
        /// </summary>
        /// <param name="expected">The byte array that <paramref name="actual"/> should be equal to.</param>
        /// <param name="actual"></param>
        public static void Equal(byte[] expected, byte[] actual)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (AssertActualExpectedException)
            {
                string expectedString = string.Join(", ", expected);
                string actualString = string.Join(", ", actual);
                throw new AssertActualExpectedException(expectedString, actualString, null);
            }
        }
    }
}
