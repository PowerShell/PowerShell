/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation.
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A
 * copy of the license can be found in the License.html file at the root of this distribution. If
 * you cannot locate the Apache License, Version 2.0, please send an email to
 * dlr@microsoft.com. By using this source code in any fashion, you are agreeing to be bound
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *
 * ***************************************************************************/

using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    internal sealed class RuntimeVariables : IRuntimeVariables
    {
        private readonly IStrongBox[] _boxes;

        private RuntimeVariables(IStrongBox[] boxes)
        {
            _boxes = boxes;
        }

        int IRuntimeVariables.Count
        {
            get
            {
                return _boxes.Length;
            }
        }

        object IRuntimeVariables.this[int index]
        {
            get
            {
                return _boxes[index].Value;
            }

            set
            {
                _boxes[index].Value = value;
            }
        }

        internal static IRuntimeVariables Create(IStrongBox[] boxes)
        {
            return new RuntimeVariables(boxes);
        }
    }
}
