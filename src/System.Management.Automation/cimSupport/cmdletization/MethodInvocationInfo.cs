// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Cmdletization
{
    /// <summary>
    /// Information about invocation of a method in an object model wrapped by an instance of <see cref="CmdletAdapter{TObjectInstance}"/>
    /// </summary>
    public sealed class MethodInvocationInfo
    {
        /// <summary>
        /// Creates a new instance of MethodInvocationInfo.
        /// </summary>
        /// <param name="name">Name of the method to invoke.</param>
        /// <param name="parameters">Method parameters.</param>
        /// <param name="returnValue">Return value of the method (ok to pass <c>null</c> if the method doesn't return anything).</param>
        public MethodInvocationInfo(string name, IEnumerable<MethodParameter> parameters, MethodParameter returnValue)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (parameters == null) throw new ArgumentNullException("parameters");
            // returnValue can be null

            MethodName = name;
            ReturnValue = returnValue;

            KeyedCollection<string, MethodParameter> mpk = new MethodParametersCollection();
            foreach (var parameter in parameters)
            {
                mpk.Add(parameter);
            }

            Parameters = mpk;
        }

        /// <summary>
        /// Name of the method to invoke.
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        /// Method parameters.
        /// </summary>
        public KeyedCollection<string, MethodParameter> Parameters { get; }

        /// <summary>
        /// Return value of the method.  Can be <c>null</c> if the method doesn't return anything.
        /// </summary>
        public MethodParameter ReturnValue { get; }

        internal IEnumerable<T> GetArgumentsOfType<T>() where T : class
        {
            List<T> result = new List<T>();
            foreach (var methodParameter in this.Parameters)
            {
                if (MethodParameterBindings.In != (methodParameter.Bindings & MethodParameterBindings.In))
                {
                    continue;
                }

                var objectInstance = methodParameter.Value as T;
                if (objectInstance != null)
                {
                    result.Add(objectInstance);
                    continue;
                }

                var objectInstanceArray = methodParameter.Value as IEnumerable;
                if (objectInstanceArray != null)
                {
                    foreach (object element in objectInstanceArray)
                    {
                        var objectInstance2 = element as T;
                        if (objectInstance2 != null)
                        {
                            result.Add(objectInstance2);
                        }
                    }

                    continue;
                }
            }

            return result;
        }
    }
}
