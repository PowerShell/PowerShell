// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Formatting Command Line Parameters

    /// <summary>
    /// It holds the command line parameter values
    /// It unifies the data structures across the various
    /// formatting command (e.g. table, wide and list)
    /// </summary>
    internal sealed class FormattingCommandLineParameters
    {
        /// <summary>
        /// MshParameter collection, as specified by metadata
        /// the list can be empty of no data is specified.
        /// </summary>
        internal List<MshParameter> mshParameterList = new List<MshParameter>();

        /// <summary>
        /// Name of the group by property, it can be null.
        /// </summary>
        internal MshParameter groupByParameter = null;

        /// <summary>
        /// Name of a view from format.ps1xml, it can be null.
        /// </summary>
        internal string viewName = null;

        /// <summary>
        /// Flag to force a shape even on out of band objects.
        /// </summary>
        internal bool forceFormattingAlsoOnOutOfBand = false;

        /// <summary>
        /// Autosize formatting flag. If true, the output command is instructed
        /// to get the "best fit" for the device screen.
        /// </summary>
        internal bool? autosize = null;

        /// <summary>
        /// If true, the header for a table is repeated after each screen full
        /// of content.
        /// </summary>
        internal bool repeatHeader = false;

        /// <summary>
        /// Errors are shown as out of band messages.
        /// </summary>
        internal bool? showErrorsAsMessages = null;

        /// <summary>
        /// Errors are shown in the formatted output.
        /// </summary>
        internal bool? showErrorsInFormattedOutput = null;

        /// <summary>
        /// Expand IEnumerable flag.
        /// </summary>
        internal EnumerableExpansion? expansion = null;

        /// <summary>
        /// Extension mechanism for shape specific parameters.
        /// </summary>
        internal ShapeSpecificParameters shapeParameters = null;
    }

    /// <summary>
    /// Class to derive from to pass shape specific data.
    /// </summary>
    internal abstract class ShapeSpecificParameters
    {
    }

    internal sealed class TableSpecificParameters : ShapeSpecificParameters
    {
        internal bool? hideHeaders = null;
        internal bool? multiLine = null;
    }

    internal sealed class WideSpecificParameters : ShapeSpecificParameters
    {
        internal int? columns = null;
    }

    internal sealed class ComplexSpecificParameters : ShapeSpecificParameters
    {
        /// <summary>
        /// Options for class info display on objects.
        /// </summary>
        internal enum ClassInfoDisplay { none, fullName, shortName }

        internal ClassInfoDisplay classDisplay = ClassInfoDisplay.shortName;

        internal const int maxDepthAllowable = 5;

        /// <summary>
        /// Max depth of recursion on sub objects.
        /// </summary>
        internal int maxDepth = maxDepthAllowable;
    }

    #endregion

    #region MshParameter metadata

    /// <summary>
    /// Specialized class for the "expression" property.
    /// </summary>
    internal class ExpressionEntryDefinition : HashtableEntryDefinition
    {
        internal ExpressionEntryDefinition() : this(false)
        {
        }

        internal ExpressionEntryDefinition(bool noGlobbing) : base(FormatParameterDefinitionKeys.ExpressionEntryKey,
                                    new Type[] { typeof(string), typeof(ScriptBlock) }, true)
        {
            _noGlobbing = noGlobbing;
        }

        internal override Hashtable CreateHashtableFromSingleType(object val)
        {
            Hashtable hash = new Hashtable();

            hash.Add(FormatParameterDefinitionKeys.ExpressionEntryKey, val);
            return hash;
        }

#if false
        internal override Hashtable CreateHashtableFromSingleType (object val)
        {
            Hashtable hash = new Hashtable ();

            // a simple type was specified, it could be a ScriptBlock
            if (val is ScriptBlock)
            {
                hash.Add (FormatParameterDefinitionKeys.NameEntryKey, val);
                return hash;
            }

            // it could be a string

            // build a hash with "hotrodded" entries if there

            string s = val as string;
            object width, align;
            string nameVal = UnpackString (s, out width, out align);

            hash.Add (FormatParameterDefinitionKeys.NameEntryKey, nameVal);
            if (width != null)
                hash.Add (FormatParameterDefinitionKeys.WidthEntryKey, width);

            if (align != null)
                hash.Add (FormatParameterDefinitionKeys.AlignmentEntryKey, align);

            return hash;
        }
#endif

        internal override object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            if (val == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(val));
            }

            // need to check the type:
            // it can be a string or a script block
            ScriptBlock sb = val as ScriptBlock;
            if (sb != null)
            {
                PSPropertyExpression ex = new PSPropertyExpression(sb);
                return ex;
            }

            string s = val as string;
            if (s != null)
            {
                if (string.IsNullOrEmpty(s))
                {
                    ProcessEmptyStringError(originalParameterWasHashTable, invocationContext);
                }

                PSPropertyExpression ex = new PSPropertyExpression(s);
                if (_noGlobbing)
                {
                    if (ex.HasWildCardCharacters)
                        ProcessGlobbingCharactersError(originalParameterWasHashTable, s, invocationContext);
                }

                return ex;
            }

            PSTraceSource.NewArgumentException(nameof(val));
            return null;
        }

        #region Error Processing

        private void ProcessEmptyStringError(bool originalParameterWasHashTable,
                                                TerminatingErrorContext invocationContext)
        {
            string msg;
            string errorID;
            if (originalParameterWasHashTable)
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.MshExEmptyStringHashError,
                    this.KeyName);
                errorID = "ExpressionEmptyString1";
            }
            else
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.MshExEmptyStringError);
                errorID = "ExpressionEmptyString2";
            }

            ParameterProcessor.ThrowParameterBindingException(invocationContext, errorID, msg);
        }

        private void ProcessGlobbingCharactersError(bool originalParameterWasHashTable, string expression, TerminatingErrorContext invocationContext)
        {
            string msg;
            string errorID;
            if (originalParameterWasHashTable)
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.MshExGlobbingHashError,
                    this.KeyName, expression);
                errorID = "ExpressionGlobbing1";
            }
            else
            {
                msg = StringUtil.Format(FormatAndOut_MshParameter.MshExGlobbingStringError,
                    expression);
                errorID = "ExpressionGlobbing2";
            }

            ParameterProcessor.ThrowParameterBindingException(invocationContext, errorID, msg);
        }

        #endregion

        private readonly bool _noGlobbing;
    }

    internal class AlignmentEntryDefinition : HashtableEntryDefinition
    {
        internal AlignmentEntryDefinition() : base(FormatParameterDefinitionKeys.AlignmentEntryKey,
                                    new Type[] { typeof(string) })
        {
        }

        internal override object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            if (!originalParameterWasHashTable)
            {
                // this should never happen
                throw PSTraceSource.NewInvalidOperationException();
            }

            // it is a string, need to check for partial match in a case insensitive way
            // and normalize
            string s = val as string;

            if (!string.IsNullOrEmpty(s))
            {
                for (int k = 0; k < s_legalValues.Length; k++)
                {
                    if (CommandParameterDefinition.FindPartialMatch(s, s_legalValues[k]))
                    {
                        if (k == 0)
                            return TextAlignment.Left;

                        if (k == 1)
                            return TextAlignment.Center;

                        return TextAlignment.Right;
                    }
                }
            }

            // nothing found, we have an illegal value
            ProcessIllegalValue(s, invocationContext);
            return null;
        }

        #region Error Processing

        private void ProcessIllegalValue(string s, TerminatingErrorContext invocationContext)
        {
            string msg = StringUtil.Format(FormatAndOut_MshParameter.IllegalAlignmentValueError,
                s,
                this.KeyName,
                ParameterProcessor.CatenateStringArray(s_legalValues)
                );
            ParameterProcessor.ThrowParameterBindingException(invocationContext, "AlignmentIllegalValue", msg);
        }

        #endregion

        private static readonly string[] s_legalValues = new string[] { LeftAlign, CenterAlign, RightAlign };

        private const string LeftAlign = "left";
        private const string CenterAlign = "center";
        private const string RightAlign = "right";
    }

    internal class WidthEntryDefinition : HashtableEntryDefinition
    {
        internal WidthEntryDefinition() : base(FormatParameterDefinitionKeys.WidthEntryKey,
                                    new Type[] { typeof(int) })
        {
        }

        internal override object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            if (!originalParameterWasHashTable)
            {
                // this should never happen
                throw PSTraceSource.NewInvalidOperationException();
            }

            // it's an int, just check range, no need to change it
            VerifyRange((int)val, invocationContext);
            return null;
        }

        private void VerifyRange(int width, TerminatingErrorContext invocationContext)
        {
            if (width <= 0)
            {
                string msg = StringUtil.Format(FormatAndOut_MshParameter.OutOfRangeWidthValueError,
                    width,
                    this.KeyName
                    );
                ParameterProcessor.ThrowParameterBindingException(invocationContext, "WidthOutOfRange", msg);
            }
        }
    }

    internal class LabelEntryDefinition : HashtableEntryDefinition
    {
        internal LabelEntryDefinition() : base(FormatParameterDefinitionKeys.LabelEntryKey, new string[] { NameEntryDefinition.NameEntryKey }, new Type[] { typeof(string) }, false)
        {
        }
    }

    internal class FormatStringDefinition : HashtableEntryDefinition
    {
        internal FormatStringDefinition() : base(FormatParameterDefinitionKeys.FormatStringEntryKey,
                                    new Type[] { typeof(string) })
        {
        }

        internal override object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            if (!originalParameterWasHashTable)
            {
                // this should never happen
                throw PSTraceSource.NewInvalidOperationException();
            }

            string s = val as string;
            if (string.IsNullOrEmpty(s))
            {
                string msg = StringUtil.Format(FormatAndOut_MshParameter.EmptyFormatStringValueError,
                    this.KeyName
                    );

                ParameterProcessor.ThrowParameterBindingException(invocationContext, "FormatStringEmpty", msg);
            }

            // we expect a string and we build a field formatting directive
            FieldFormattingDirective directive = new FieldFormattingDirective();
            directive.formatString = s;
            return directive;
        }
    }

    internal class BooleanEntryDefinition : HashtableEntryDefinition
    {
        internal BooleanEntryDefinition(string entryKey) : base(entryKey, null)
        {
        }

        internal override object Verify(object val,
                                        TerminatingErrorContext invocationContext,
                                        bool originalParameterWasHashTable)
        {
            if (!originalParameterWasHashTable)
            {
                // this should never happen
                throw PSTraceSource.NewInvalidOperationException();
            }

            return LanguagePrimitives.IsTrue(val);
        }
    }

    /// <summary>
    /// Definitions for hash table keys.
    /// </summary>
    internal static class FormatParameterDefinitionKeys
    {
        // common entries
        internal const string ExpressionEntryKey = "expression";
        internal const string FormatStringEntryKey = "formatString";

        // specific to format-table
        internal const string AlignmentEntryKey = "alignment";
        internal const string WidthEntryKey = "width";

        // specific to format-table,list and wide
        internal const string LabelEntryKey = "label";

        // specific to format-wide
        // NONE

        // specific to format-custom (no format string for it, just the name)
        internal const string DepthEntryKey = "depth";
    }

    internal class FormatGroupByParameterDefinition : CommandParameterDefinition
    {
        protected override void SetEntries()
        {
            this.hashEntries.Add(new ExpressionEntryDefinition());
            this.hashEntries.Add(new FormatStringDefinition());
            this.hashEntries.Add(new LabelEntryDefinition());
        }
    }

    internal class FormatParameterDefinitionBase : CommandParameterDefinition
    {
        protected override void SetEntries()
        {
            this.hashEntries.Add(new ExpressionEntryDefinition());
            this.hashEntries.Add(new FormatStringDefinition());
        }
    }

    internal class FormatTableParameterDefinition : FormatParameterDefinitionBase
    {
        protected override void SetEntries()
        {
            base.SetEntries();
            this.hashEntries.Add(new WidthEntryDefinition());
            this.hashEntries.Add(new AlignmentEntryDefinition());
            this.hashEntries.Add(new LabelEntryDefinition());
        }
    }

    internal class FormatListParameterDefinition : FormatParameterDefinitionBase
    {
        protected override void SetEntries()
        {
            base.SetEntries();
            this.hashEntries.Add(new LabelEntryDefinition());
        }
    }

    internal class FormatWideParameterDefinition : FormatParameterDefinitionBase
    {
        // no additional entries
    }

    internal class FormatObjectParameterDefinition : CommandParameterDefinition
    {
        protected override void SetEntries()
        {
            this.hashEntries.Add(new ExpressionEntryDefinition());
            this.hashEntries.Add(new HashtableEntryDefinition(FormatParameterDefinitionKeys.DepthEntryKey, new Type[] { typeof(int) }));
        }
    }
    #endregion
}
