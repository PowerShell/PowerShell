// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// this file contains the data structures for the in memory database
// containing display and formatting information

using System.Collections.Generic;
using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.Commands.Internal.Format;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Complex View Definitions

    /// <summary>
    /// In line definition of a complex control.
    /// </summary>
    internal sealed class ComplexControlBody : ControlBody
    {
        /// <summary>
        /// Default list entry definition
        /// It's mandatory.
        /// </summary>
        internal ComplexControlEntryDefinition defaultEntry;

        /// <summary>
        /// Optional list of list entry definition overrides. It can be empty if there are no overrides.
        /// </summary>
        internal List<ComplexControlEntryDefinition> optionalEntryList = new List<ComplexControlEntryDefinition>();
    }

    internal sealed class ComplexControlEntryDefinition
    {
        /// <summary>
        /// Applicability clause
        /// Only valid if not the default definition.
        /// </summary>
        internal AppliesTo appliesTo = null;

        /// <summary>
        /// Item associated with this entry definition.
        /// </summary>
        internal ComplexControlItemDefinition itemDefinition = new ComplexControlItemDefinition();
    }

    internal sealed class ComplexControlItemDefinition
    {
        /// <summary>
        /// List of tokens the item can contain.
        /// </summary>
        internal List<FormatToken> formatTokenList = new List<FormatToken>();
    }

    #endregion
}

namespace System.Management.Automation
{
    /// <summary/>
    public sealed class CustomControl : PSControl
    {
        /// <summary/>
        public List<CustomControlEntry> Entries { get; set; }

        internal ComplexControlBody _cachedBody;

        internal CustomControl()
        {
            Entries = new List<CustomControlEntry>();
        }

        internal CustomControl(ComplexControlBody body, ViewDefinition viewDefinition)
        {
            // viewDefinition can be null for nested controls
            if (viewDefinition != null)
            {
                OutOfBand = viewDefinition.outOfBand;
                GroupBy = PSControlGroupBy.Get(viewDefinition.groupBy);
            }

            Entries = new List<CustomControlEntry>();

            // Default entry
            var cce = new CustomControlEntry(body.defaultEntry);
            Entries.Add(cce);

            foreach (var entry in body.optionalEntryList)
            {
                cce = new CustomControlEntry(entry);
                Entries.Add(cce);
            }
        }

        /// <summary/>
        public static CustomControlBuilder Create(bool outOfBand = false)
        {
            var customControl = new CustomControl { OutOfBand = outOfBand };
            return new CustomControlBuilder(customControl);
        }

        internal override void WriteToXml(FormatXmlWriter writer)
        {
            writer.WriteCustomControl(this);
        }

        internal override bool SafeForExport()
        {
            if (!base.SafeForExport())
                return false;

            foreach (var entry in Entries)
            {
                if (!entry.SafeForExport())
                    return false;
            }

            return true;
        }

        internal override bool CompatibleWithOldPowerShell()
        {
            // Old versions of PowerShell know nothing about CustomControl.
            return false;
        }
    }

    /// <summary/>
    public sealed class CustomControlEntry
    {
        internal CustomControlEntry()
        {
            CustomItems = new List<CustomItemBase>();
        }

        internal CustomControlEntry(ComplexControlEntryDefinition entry)
        {
            if (entry.appliesTo != null)
            {
                SelectedBy = EntrySelectedBy.Get(entry.appliesTo.referenceList);
            }

            CustomItems = new List<CustomItemBase>();
            foreach (var tok in entry.itemDefinition.formatTokenList)
            {
                CustomItems.Add(CustomItemBase.Create(tok));
            }
        }

        /// <summary/>
        public EntrySelectedBy SelectedBy { get; set; }

        /// <summary/>
        public List<CustomItemBase> CustomItems { get; set; }

        internal bool SafeForExport()
        {
            foreach (var item in CustomItems)
            {
                if (!item.SafeForExport())
                    return false;
            }

            return SelectedBy == null || SelectedBy.SafeForExport();
        }
    }

    /// <summary/>
    public abstract class CustomItemBase
    {
        internal virtual bool SafeForExport()
        {
            return true;
        }

        internal static CustomItemBase Create(FormatToken token)
        {
            if (token is NewLineToken)
            {
                return new CustomItemNewline();
            }

            if (token is TextToken textToken)
            {
                return new CustomItemText { Text = textToken.text };
            }

            if (token is FrameToken frameToken)
            {
                var frame = new CustomItemFrame
                {
                    RightIndent = (uint)frameToken.frameInfoDefinition.rightIndentation,
                    LeftIndent = (uint)frameToken.frameInfoDefinition.leftIndentation
                };
                var firstLine = frameToken.frameInfoDefinition.firstLine;
                if (firstLine > 0)
                {
                    frame.FirstLineIndent = (uint)firstLine;
                }
                else if (firstLine < 0)
                {
                    frame.FirstLineHanging = (uint)-firstLine;
                }

                foreach (var frameItemToken in frameToken.itemDefinition.formatTokenList)
                {
                    frame.CustomItems.Add(CustomItemBase.Create(frameItemToken));
                }

                return frame;
            }

            if (token is CompoundPropertyToken cpt)
            {
                var cie = new CustomItemExpression { EnumerateCollection = cpt.enumerateCollection };

                if (cpt.conditionToken != null)
                {
                    cie.ItemSelectionCondition = new DisplayEntry(cpt.conditionToken);
                }

                if (cpt.expression.expressionValue != null)
                {
                    cie.Expression = new DisplayEntry(cpt.expression);
                }

                if (cpt.control != null)
                {
                    cie.CustomControl = new CustomControl((ComplexControlBody)cpt.control, null);
                }

                return cie;
            }

            Diagnostics.Assert(false, "Unexpected formatting token kind");

            return null;
        }
    }

    /// <summary/>
    public sealed class CustomItemExpression : CustomItemBase
    {
        internal CustomItemExpression() { }

        /// <summary/>
        public DisplayEntry ItemSelectionCondition { get; set; }

        /// <summary/>
        public DisplayEntry Expression { get; set; }

        /// <summary/>
        public bool EnumerateCollection { get; set; }

        /// <summary/>
        public CustomControl CustomControl { get; set; }

        internal override bool SafeForExport()
        {
            return (ItemSelectionCondition == null || ItemSelectionCondition.SafeForExport()) &&
                   (Expression == null || Expression.SafeForExport()) &&
                   (CustomControl == null || CustomControl.SafeForExport());
        }
    }

    /// <summary/>
    public sealed class CustomItemFrame : CustomItemBase
    {
        /// <summary/>
        public uint LeftIndent { get; set; }
        /// <summary/>
        public uint RightIndent { get; set; }
        /// <summary/>
        public uint FirstLineHanging { get; set; }
        /// <summary/>
        public uint FirstLineIndent { get; set; }

        internal CustomItemFrame()
        {
            CustomItems = new List<CustomItemBase>();
        }

        /// <summary/>
        public List<CustomItemBase> CustomItems { get; set; }

        internal override bool SafeForExport()
        {
            foreach (var frameItem in CustomItems)
            {
                if (!frameItem.SafeForExport())
                    return false;
            }

            return true;
        }
    }

    /// <summary/>
    public sealed class CustomItemNewline : CustomItemBase
    {
        /// <summary/>
        public CustomItemNewline()
        {
            this.Count = 1;
        }
        /// <summary/>
        public int Count { get; set; }
    }

    /// <summary/>
    public sealed class CustomItemText : CustomItemBase
    {
        /// <summary/>
        public string Text { get; set; }
    }

    /// <summary/>
    public sealed class CustomEntryBuilder
    {
        private readonly Stack<List<CustomItemBase>> _entryStack;
        private readonly CustomControlBuilder _controlBuilder;

        internal CustomEntryBuilder(CustomControlBuilder controlBuilder, CustomControlEntry entry)
        {
            _entryStack = new Stack<List<CustomItemBase>>();
            _entryStack.Push(entry.CustomItems);
            _controlBuilder = controlBuilder;
        }

        /// <summary/>
        public CustomEntryBuilder AddNewline(int count = 1)
        {
            _entryStack.Peek().Add(new CustomItemNewline { Count = count });
            return this;
        }

        /// <summary/>
        public CustomEntryBuilder AddText(string text)
        {
            _entryStack.Peek().Add(new CustomItemText { Text = text });
            return this;
        }

        private void AddDisplayExpressionBinding(
            string value,
            DisplayEntryValueType valueType,
            bool enumerateCollection = false,
            string selectedByType = null,
            string selectedByScript = null,
            CustomControl customControl = null)
        {
            _entryStack.Peek().Add(new CustomItemExpression()
            {
                ItemSelectionCondition = selectedByScript != null
                    ? new DisplayEntry(selectedByScript, DisplayEntryValueType.ScriptBlock)
                    : selectedByType != null
                        ? new DisplayEntry(selectedByType, DisplayEntryValueType.Property)
                        : null,
                EnumerateCollection = enumerateCollection,
                Expression = new DisplayEntry(value, valueType),
                CustomControl = customControl
            });
        }

        /// <summary/>
        public CustomEntryBuilder AddPropertyExpressionBinding(
            string property,
            bool enumerateCollection = false,
            string selectedByType = null,
            string selectedByScript = null,
            CustomControl customControl = null)
        {
            AddDisplayExpressionBinding(property, DisplayEntryValueType.Property, enumerateCollection, selectedByType, selectedByScript, customControl);
            return this;
        }

        /// <summary/>
        public CustomEntryBuilder AddScriptBlockExpressionBinding(
            string scriptBlock,
            bool enumerateCollection = false,
            string selectedByType = null,
            string selectedByScript = null,
            CustomControl customControl = null)
        {
            AddDisplayExpressionBinding(scriptBlock, DisplayEntryValueType.ScriptBlock, enumerateCollection, selectedByType, selectedByScript, customControl);
            return this;
        }

        /// <summary/>
        public CustomEntryBuilder AddCustomControlExpressionBinding(
            CustomControl customControl,
            bool enumerateCollection = false,
            string selectedByType = null,
            string selectedByScript = null)
        {
            _entryStack.Peek().Add(new CustomItemExpression()
            {
                ItemSelectionCondition = selectedByScript != null
                    ? new DisplayEntry(selectedByScript, DisplayEntryValueType.ScriptBlock)
                    : selectedByType != null
                        ? new DisplayEntry(selectedByType, DisplayEntryValueType.Property)
                        : null,
                EnumerateCollection = enumerateCollection,
                CustomControl = customControl
            });

            return this;
        }

        /// <summary/>
        public CustomEntryBuilder StartFrame(uint leftIndent = 0, uint rightIndent = 0, uint firstLineHanging = 0, uint firstLineIndent = 0)
        {
            // Mutually exclusive
            if (leftIndent != 0 && rightIndent != 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(leftIndent));
            }

            // Mutually exclusive
            if (firstLineHanging != 0 && firstLineIndent != 0)
            {
                throw PSTraceSource.NewArgumentException(nameof(firstLineHanging));
            }

            var frame = new CustomItemFrame
            {
                LeftIndent = leftIndent,
                RightIndent = rightIndent,
                FirstLineHanging = firstLineHanging,
                FirstLineIndent = firstLineIndent
            };
            _entryStack.Peek().Add(frame);
            _entryStack.Push(frame.CustomItems);
            return this;
        }

        /// <summary/>
        public CustomEntryBuilder EndFrame()
        {
            if (_entryStack.Count < 2)
            {
                throw PSTraceSource.NewInvalidOperationException();
            }

            _entryStack.Pop();
            return this;
        }

        /// <summary/>
        public CustomControlBuilder EndEntry()
        {
            if (_entryStack.Count != 1)
            {
                throw PSTraceSource.NewInvalidOperationException();
            }

            _entryStack.Pop();
            return _controlBuilder;
        }
    }

    /// <summary/>
    public sealed class CustomControlBuilder
    {
        internal CustomControl _control;

        internal CustomControlBuilder(CustomControl control)
        {
            _control = control;
        }

        /// <summary>Group instances by the property name with an optional label.</summary>
        public CustomControlBuilder GroupByProperty(string property, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(property, DisplayEntryValueType.Property),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary>Group instances by the script block expression with an optional label.</summary>
        public CustomControlBuilder GroupByScriptBlock(string scriptBlock, CustomControl customControl = null, string label = null)
        {
            _control.GroupBy = new PSControlGroupBy
            {
                Expression = new DisplayEntry(scriptBlock, DisplayEntryValueType.ScriptBlock),
                CustomControl = customControl,
                Label = label
            };
            return this;
        }

        /// <summary/>
        public CustomEntryBuilder StartEntry(IEnumerable<string> entrySelectedByType = null, IEnumerable<DisplayEntry> entrySelectedByCondition = null)
        {
            var entry = new CustomControlEntry
            {
                SelectedBy = EntrySelectedBy.Get(entrySelectedByType, entrySelectedByCondition)
            };
            _control.Entries.Add(entry);
            return new CustomEntryBuilder(this, entry);
        }

        /// <summary/>
        public CustomControl EndControl()
        {
            return _control;
        }
    }
}
