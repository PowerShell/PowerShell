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

#if !CLR2
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AstUtils = System.Management.Automation.Interpreter.Utils;

namespace System.Management.Automation.Interpreter
{
    /// <summary>
    /// Visits a LambdaExpression, replacing the constants with direct accesses
    /// to their StrongBox fields. This is very similar to what
    /// ExpressionQuoter does for LambdaCompiler.
    ///
    /// Also inserts debug information tracking similar to what the interpreter
    /// would do.
    /// </summary>
    internal sealed class LightLambdaClosureVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Local variable mapping.
        /// </summary>
        private readonly Dictionary<ParameterExpression, LocalVariable> _closureVars;

        /// <summary>
        /// The variable that holds onto the StrongBox{object}[] closure from
        /// the interpreter.
        /// </summary>
        private readonly ParameterExpression _closureArray;

        /// <summary>
        /// A stack of variables that are defined in nested scopes. We search
        /// this first when resolving a variable in case a nested scope shadows
        /// one of our variable instances.
        /// </summary>
        private readonly Stack<HashSet<ParameterExpression>> _shadowedVars = new Stack<HashSet<ParameterExpression>>();

        private LightLambdaClosureVisitor(Dictionary<ParameterExpression, LocalVariable> closureVariables, ParameterExpression closureArray)
        {
            Assert.NotNull(closureVariables, closureArray);
            _closureArray = closureArray;
            _closureVars = closureVariables;
        }

        /// <summary>
        /// Walks the lambda and produces a higher order function, which can be
        /// used to bind the lambda to a closure array from the interpreter.
        /// </summary>
        /// <param name="lambda">The lambda to bind.</param>
        /// <param name="closureVariables">Variables which are being accessed defined in the outer scope.</param>
        /// <returns>A delegate that can be called to produce a delegate bound to the passed in closure array.</returns>
        internal static Func<StrongBox<object>[], Delegate> BindLambda(LambdaExpression lambda, Dictionary<ParameterExpression, LocalVariable> closureVariables)
        {
            // 1. Create rewriter
            var closure = Expression.Parameter(typeof(StrongBox<object>[]), "closure");
            var visitor = new LightLambdaClosureVisitor(closureVariables, closure);

            // 2. Visit the lambda
            lambda = (LambdaExpression)visitor.Visit(lambda);

            // 3. Create a higher-order function which fills in the parameters
            var result = Expression.Lambda<Func<StrongBox<object>[], Delegate>>(lambda, closure);

            // 4. Compile it
            return result.Compile();
        }

        #region closures

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            _shadowedVars.Push(new HashSet<ParameterExpression>(node.Parameters));
            Expression b = Visit(node.Body);
            _shadowedVars.Pop();
            if (b == node.Body)
            {
                return node;
            }

            return Expression.Lambda<T>(b, node.Name, node.TailCall, node.Parameters);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            if (node.Variables.Count > 0)
            {
                _shadowedVars.Push(new HashSet<ParameterExpression>(node.Variables));
            }

            var b = Visit(node.Expressions);
            if (node.Variables.Count > 0)
            {
                _shadowedVars.Pop();
            }

            if (b == node.Expressions)
            {
                return node;
            }

            return Expression.Block(node.Variables, b);
        }

        protected override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            if (node.Variable != null)
            {
                _shadowedVars.Push(new HashSet<ParameterExpression>(new[] { node.Variable }));
            }

            Expression b = Visit(node.Body);
            Expression f = Visit(node.Filter);
            if (node.Variable != null)
            {
                _shadowedVars.Pop();
            }

            if (b == node.Body && f == node.Filter)
            {
                return node;
            }

            return Expression.MakeCatchBlock(node.Test, node.Variable, b, f);
        }

        protected override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            int count = node.Variables.Count;
            var boxes = new List<Expression>();
            var vars = new List<ParameterExpression>();
            var indexes = new int[count];
            for (int i = 0; i < count; i++)
            {
                Expression box = GetClosureItem(node.Variables[i], false);
                if (box == null)
                {
                    indexes[i] = vars.Count;
                    vars.Add(node.Variables[i]);
                }
                else
                {
                    indexes[i] = -1 - boxes.Count;
                    boxes.Add(box);
                }
            }

            // No variables were rewritten. Just return the original node.
            if (boxes.Count == 0)
            {
                return node;
            }

            var boxesArray = Expression.NewArrayInit(typeof(IStrongBox), boxes);

            // All of them were rewritten. Just return the array, wrapped in a
            // read-only collection.
            if (vars.Count == 0)
            {
                return Expression.Invoke(
                    Expression.Constant((Func<IStrongBox[], IRuntimeVariables>)RuntimeVariables.Create),
                    boxesArray
                );
            }

            // Otherwise, we need to return an object that merges them
            Func<IRuntimeVariables, IRuntimeVariables, int[], IRuntimeVariables> helper = MergedRuntimeVariables.Create;
            return Expression.Invoke(AstUtils.Constant(helper), Expression.RuntimeVariables(vars), boxesArray, AstUtils.Constant(indexes));
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            Expression closureItem = GetClosureItem(node, true);
            if (closureItem == null)
            {
                return node;
            }
            // Convert can go away if we switch to strongly typed StrongBox
            return AstUtils.Convert(closureItem, node.Type);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign &&
                node.Left.NodeType == ExpressionType.Parameter)
            {
                var variable = (ParameterExpression)node.Left;
                Expression closureItem = GetClosureItem(variable, true);
                if (closureItem != null)
                {
                    // We need to convert to object to store the value in the box.
                    return Expression.Block(
                        new[] { variable },
                        Expression.Assign(variable, Visit(node.Right)),
                        Expression.Assign(closureItem, AstUtils.Convert(variable, typeof(object))),
                        variable
                    );
                }
            }

            return base.VisitBinary(node);
        }

        private Expression GetClosureItem(ParameterExpression variable, bool unbox)
        {
            // Skip variables that are shadowed by a nested scope/lambda
            foreach (HashSet<ParameterExpression> hidden in _shadowedVars)
            {
                if (hidden.Contains(variable))
                {
                    return null;
                }
            }

            LocalVariable loc;
            if (!_closureVars.TryGetValue(variable, out loc))
            {
                throw new InvalidOperationException("unbound variable: " + variable.Name);
            }

            var result = loc.LoadFromArray(null, _closureArray);
            return (unbox) ? LightCompiler.Unbox(result) : result;
        }

        protected override Expression VisitExtension(Expression node)
        {
            // Reduce extensions now so we can find embedded variables
            return Visit(node.ReduceExtensions());
        }

        #region MergedRuntimeVariables

        /// <summary>
        /// Provides a list of variables, supporting read/write of the values.
        /// </summary>
        private sealed class MergedRuntimeVariables : IRuntimeVariables
        {
            private readonly IRuntimeVariables _first;
            private readonly IRuntimeVariables _second;

            // For reach item, the index into the first or second list
            // Positive values mean the first array, negative means the second
            private readonly int[] _indexes;

            private MergedRuntimeVariables(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
            {
                _first = first;
                _second = second;
                _indexes = indexes;
            }

            internal static IRuntimeVariables Create(IRuntimeVariables first, IRuntimeVariables second, int[] indexes)
            {
                return new MergedRuntimeVariables(first, second, indexes);
            }

            int IRuntimeVariables.Count
            {
                get { return _indexes.Length; }
            }

            object IRuntimeVariables.this[int index]
            {
                get
                {
                    index = _indexes[index];
                    return (index >= 0) ? _first[index] : _second[-1 - index];
                }

                set
                {
                    index = _indexes[index];
                    if (index >= 0)
                    {
                        _first[index] = value;
                    }
                    else
                    {
                        _second[-1 - index] = value;
                    }
                }
            }
        }
        #endregion

        #endregion
    }
}
