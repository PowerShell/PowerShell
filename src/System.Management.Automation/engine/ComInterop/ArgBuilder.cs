/********************************************************************++
Copyright (c) Microsoft Corporation. All rights reserved.
--********************************************************************/

#if !SILVERLIGHT

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif

namespace System.Management.Automation.ComInterop
{
    /// <summary>
    /// ArgBuilder provides an argument value used by the MethodBinder.  One ArgBuilder exists for each
    /// physical parameter defined on a method.
    ///
    /// Contrast this with ParameterWrapper which represents the logical argument passed to the method.
    /// </summary>
    internal abstract class ArgBuilder
    {
        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// </summary>
        internal abstract Expression Marshal(Expression parameter);

        /// <summary>
        /// Provides the Expression which provides the value to be passed to the argument.
        /// This method is called when result is intended to be used ByRef.
        /// </summary>
        internal virtual Expression MarshalToRef(Expression parameter)
        {
            return Marshal(parameter);
        }

        /// <summary>
        /// Provides an Expression which will update the provided value after a call to the method.
        /// May return null if no update is required.
        /// </summary>
        internal virtual Expression UnmarshalFromRef(Expression newValue)
        {
            return newValue;
        }
    }
}

#endif

