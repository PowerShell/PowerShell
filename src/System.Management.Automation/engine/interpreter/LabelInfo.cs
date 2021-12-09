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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
#else
using Microsoft.Scripting.Ast;
#endif
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Management.Automation.Interpreter
{
    /// <summary>
    /// Contains compiler state corresponding to a LabelTarget
    /// See also LabelScopeInfo.
    /// </summary>
    internal sealed class LabelInfo
    {
        // The tree node representing this label
        private readonly LabelTarget _node;

        // The BranchLabel label, will be mutated if Node is redefined
        private BranchLabel _label;

        // The blocks where this label is defined. If it has more than one item,
        // the blocks can't be jumped to except from a child block
        // If there's only 1 block (the common case) it's stored here, if there's multiple blocks it's stored
        // as a HashSet<LabelScopeInfo>
        private object _definitions;

        // Blocks that jump to this block
        private readonly List<LabelScopeInfo> _references = new List<LabelScopeInfo>();

        // True if at least one jump is across blocks
        // If we have any jump across blocks to this label, then the
        // LabelTarget can only be defined in one place
        private bool _acrossBlockJump;

        internal LabelInfo(LabelTarget node)
        {
            _node = node;
        }

        internal BranchLabel GetLabel(LightCompiler compiler)
        {
            EnsureLabel(compiler);
            return _label;
        }

        internal void Reference(LabelScopeInfo block)
        {
            _references.Add(block);
            if (HasDefinitions)
            {
                ValidateJump(block);
            }
        }

        internal void Define(LabelScopeInfo block)
        {
            // Prevent the label from being shadowed, which enforces cleaner
            // trees. Also we depend on this for simplicity (keeping only one
            // active IL Label per LabelInfo)
            for (LabelScopeInfo j = block; j != null; j = j.Parent)
            {
                if (j.ContainsTarget(_node))
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Label target already defined: {0}", _node.Name));
                }
            }

            AddDefinition(block);
            block.AddLabelInfo(_node, this);

            // Once defined, validate all jumps
            if (HasDefinitions && !HasMultipleDefinitions)
            {
                foreach (var r in _references)
                {
                    ValidateJump(r);
                }
            }
            else
            {
                // Was just redefined, if we had any across block jumps, they're
                // now invalid
                if (_acrossBlockJump)
                {
                    throw new InvalidOperationException("Ambiguous jump");
                }
                // For local jumps, we need a new IL label
                // This is okay because:
                //   1. no across block jumps have been made or will be made
                //   2. we don't allow the label to be shadowed
                _label = null;
            }
        }

        private void ValidateJump(LabelScopeInfo reference)
        {
            // look for a simple jump out
            for (LabelScopeInfo j = reference; j != null; j = j.Parent)
            {
                if (DefinedIn(j))
                {
                    // found it, jump is valid!
                    return;
                }

                if (j.Kind == LabelScopeKind.Filter)
                {
                    break;
                }
            }

            _acrossBlockJump = true;

            if (HasMultipleDefinitions)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Ambiguous jump {0}", _node.Name));
            }

            // We didn't find an outward jump. Look for a jump across blocks
            LabelScopeInfo def = FirstDefinition();
            LabelScopeInfo common = CommonNode(def, reference, static b => b.Parent);

            // Validate that we aren't jumping across a finally
            for (LabelScopeInfo j = reference; j != common; j = j.Parent)
            {
                if (j.Kind == LabelScopeKind.Filter)
                {
                    throw new InvalidOperationException("Control cannot leave filter test");
                }
            }

            // Validate that we aren't jumping into a catch or an expression
            for (LabelScopeInfo j = def; j != common; j = j.Parent)
            {
                if (!j.CanJumpInto)
                {
                    if (j.Kind == LabelScopeKind.Expression)
                    {
                        throw new InvalidOperationException("Control cannot enter an expression");
                    }
                    else
                    {
                        throw new InvalidOperationException("Control cannot enter try");
                    }
                }
            }
        }

        internal void ValidateFinish()
        {
            // Make sure that if this label was jumped to, it is also defined
            if (_references.Count > 0 && !HasDefinitions)
            {
                throw new InvalidOperationException("label target undefined");
            }
        }

        private void EnsureLabel(LightCompiler compiler)
        {
            if (_label == null)
            {
                _label = compiler.Instructions.MakeLabel();
            }
        }

        private bool DefinedIn(LabelScopeInfo scope)
        {
            if (_definitions == scope)
            {
                return true;
            }

            HashSet<LabelScopeInfo> definitions = _definitions as HashSet<LabelScopeInfo>;
            if (definitions != null)
            {
                return definitions.Contains(scope);
            }

            return false;
        }

        private bool HasDefinitions
        {
            get
            {
                return _definitions != null;
            }
        }

        private LabelScopeInfo FirstDefinition()
        {
            LabelScopeInfo scope = _definitions as LabelScopeInfo;
            if (scope != null)
            {
                return scope;
            }

            return ((HashSet<LabelScopeInfo>)_definitions).First();
        }

        private void AddDefinition(LabelScopeInfo scope)
        {
            if (_definitions == null)
            {
                _definitions = scope;
            }
            else
            {
                HashSet<LabelScopeInfo> set = _definitions as HashSet<LabelScopeInfo>;
                if (set == null)
                {
                    _definitions = set = new HashSet<LabelScopeInfo>() { (LabelScopeInfo)_definitions };
                }

                set.Add(scope);
            }
        }

        private bool HasMultipleDefinitions
        {
            get
            {
                return _definitions is HashSet<LabelScopeInfo>;
            }
        }

        internal static T CommonNode<T>(T first, T second, Func<T, T> parent) where T : class
        {
            var cmp = EqualityComparer<T>.Default;
            if (cmp.Equals(first, second))
            {
                return first;
            }

            var set = new HashSet<T>(cmp);
            for (T t = first; t != null; t = parent(t))
            {
                set.Add(t);
            }

            for (T t = second; t != null; t = parent(t))
            {
                if (set.Contains(t))
                {
                    return t;
                }
            }

            return null;
        }
    }

    internal enum LabelScopeKind
    {
        // any "statement like" node that can be jumped into
        Statement,

        // these correspond to the node of the same name
        Block,
        Switch,
        Lambda,
        Try,

        // these correspond to the part of the try block we're in
        Catch,
        Finally,
        Filter,

        // the catch-all value for any other expression type
        // (means we can't jump into it)
        Expression,
    }

    //
    // Tracks scoping information for LabelTargets. Logically corresponds to a
    // "label scope". Even though we have arbitrary goto support, we still need
    // to track what kinds of nodes that gotos are jumping through, both to
    // emit property IL ("leave" out of a try block), and for validation, and
    // to allow labels to be duplicated in the tree, as long as the jumps are
    // considered "up only" jumps.
    //
    // We create one of these for every Expression that can be jumped into, as
    // well as creating them for the first expression we can't jump into. The
    // "Kind" property indicates what kind of scope this is.
    //
    internal sealed class LabelScopeInfo
    {
        private HybridReferenceDictionary<LabelTarget, LabelInfo> _labels; // lazily allocated, we typically use this only once every 6th-7th block
        internal readonly LabelScopeKind Kind;
        internal readonly LabelScopeInfo Parent;

        internal LabelScopeInfo(LabelScopeInfo parent, LabelScopeKind kind)
        {
            Parent = parent;
            Kind = kind;
        }

        /// <summary>
        /// Returns true if we can jump into this node.
        /// </summary>
        internal bool CanJumpInto
        {
            get
            {
                switch (Kind)
                {
                    case LabelScopeKind.Block:
                    case LabelScopeKind.Statement:
                    case LabelScopeKind.Switch:
                    case LabelScopeKind.Lambda:
                        return true;
                }

                return false;
            }
        }

        internal bool ContainsTarget(LabelTarget target)
        {
            if (_labels == null)
            {
                return false;
            }

            return _labels.ContainsKey(target);
        }

        internal bool TryGetLabelInfo(LabelTarget target, out LabelInfo info)
        {
            if (_labels == null)
            {
                info = null;
                return false;
            }

            return _labels.TryGetValue(target, out info);
        }

        internal void AddLabelInfo(LabelTarget target, LabelInfo info)
        {
            Debug.Assert(CanJumpInto);

            if (_labels == null)
            {
                _labels = new HybridReferenceDictionary<LabelTarget, LabelInfo>();
            }

            _labels[target] = info;
        }
    }
}
