// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell
{
    /// <summary>
    /// ProgressNode is an augmentation of the ProgressRecord type that adds extra fields for the purposes of tracking
    /// outstanding activities received by the host, and rendering them in the console.
    /// </summary>

    internal
    class
    ProgressNode : ProgressRecord
    {
        /// <summary>
        /// Indicates the various layouts for rendering a particular node.  Each style is progressively less terse.
        /// </summary>

        internal
        enum
        RenderStyle
        {
            Invisible = 0,
            Minimal = 1,
            Compact = 2,

            /// <summary>
            /// Allocate only one line for displaying the StatusDescription or the CurrentOperation,
            /// truncate the rest if the StatusDescription or CurrentOperation doesn't fit in one line.
            /// </summary>
            Full = 3,

            /// <summary>
            /// The node will be displayed the same as Full, plus, the whole StatusDescription and CurrentOperation will be displayed (in multiple lines if needed).
            /// </summary>
            FullPlus = 4,
        };

        /// <summary>
        /// Constructs an instance from a ProgressRecord.
        /// </summary>

        internal
        ProgressNode(Int64 sourceId, ProgressRecord record)
            :
            base(record.ActivityId, record.Activity, record.StatusDescription)
        {
            Dbg.Assert(record.RecordType == ProgressRecordType.Processing, "should only create node for Processing records");

            this.ParentActivityId = record.ParentActivityId;
            this.CurrentOperation = record.CurrentOperation;
            this.PercentComplete = Math.Min(record.PercentComplete, 100);
            this.SecondsRemaining = record.SecondsRemaining;
            this.RecordType = record.RecordType;
            this.Style = RenderStyle.FullPlus;
            this.SourceId = sourceId;
        }

        /// <summary>
        /// Renders a single progress node as strings of text according to that node's style. The text is appended to the
        /// supplied list of strings.
        /// </summary>
        /// <param name="strCollection">
        /// List of strings to which the node's rendering will be appended.
        /// </param>
        /// <param name="indentation">
        /// The indentation level (in BufferCells) at which the node should be rendered.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum number of BufferCells that the rendering is allowed to consume.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>

        internal
        void
        Render(ArrayList strCollection, int indentation, int maxWidth, PSHostRawUserInterface rawUI)
        {
            Dbg.Assert(strCollection != null, "strCollection should not be null");
            Dbg.Assert(indentation >= 0, "indentation is negative");
            Dbg.Assert(this.RecordType != ProgressRecordType.Completed, "should never render completed records");

            switch (Style)
            {
                case RenderStyle.FullPlus:
                    RenderFull(strCollection, indentation, maxWidth, rawUI, isFullPlus: true);
                    break;
                case RenderStyle.Full:
                    RenderFull(strCollection, indentation, maxWidth, rawUI, isFullPlus: false);
                    break;
                case RenderStyle.Compact:
                    RenderCompact(strCollection, indentation, maxWidth, rawUI);
                    break;
                case RenderStyle.Minimal:
                    RenderMinimal(strCollection, indentation, maxWidth, rawUI);
                    break;
                case RenderStyle.Invisible:
                    // do nothing
                    break;
                default:
                    Dbg.Assert(false, "unrecognized RenderStyle value");
                    break;
            }
        }

        /// <summary>
        /// Renders a node in the "Full" style.
        /// </summary>
        /// <param name="strCollection">
        /// List of strings to which the node's rendering will be appended.
        /// </param>
        /// <param name="indentation">
        /// The indentation level (in BufferCells) at which the node should be rendered.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum number of BufferCells that the rendering is allowed to consume.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        /// <param name="isFullPlus">
        /// Indicate if the full StatusDescription and CurrentOperation should be displayed.
        /// </param>

        private
        void
        RenderFull(ArrayList strCollection, int indentation, int maxWidth, PSHostRawUserInterface rawUI, bool isFullPlus)
        {
            string indent = StringUtil.Padding(indentation);

            // First line: the activity

            strCollection.Add(
                StringUtil.TruncateToBufferCellWidth(
                    rawUI, StringUtil.Format(" {0}{1} ", indent, this.Activity), maxWidth));

            indentation += 3;
            indent = StringUtil.Padding(indentation);

            // Second line: the status description

            RenderFullDescription(this.StatusDescription, indent, maxWidth, rawUI, strCollection, isFullPlus);

            // Third line: the percentage thermometer. The size of this is proportional to the width we're allowed
            // to consume. -2 for the whitespace, -2 again for the brackets around thermo, -5 to not be too big

            if (PercentComplete >= 0)
            {
                int thermoWidth = Math.Max(3, maxWidth - indentation - 2 - 2 - 5);
                int mercuryWidth = 0;
                mercuryWidth = PercentComplete * thermoWidth / 100;
                if (PercentComplete < 100 && mercuryWidth == thermoWidth)
                {
                    // back off a tad unless we're totally complete to prevent the appearance of completion before
                    // the fact.

                    --mercuryWidth;
                }

                strCollection.Add(
                    StringUtil.TruncateToBufferCellWidth(
                        rawUI,
                        StringUtil.Format(
                            " {0}[{1}{2}] ",
                            indent,
                            new string('o', mercuryWidth),
                            StringUtil.Padding(thermoWidth - mercuryWidth)),
                        maxWidth));
            }

            // Fourth line: the seconds remaining

            if (SecondsRemaining >= 0)
            {
                TimeSpan span = new TimeSpan(0, 0, this.SecondsRemaining);

                strCollection.Add(
                    StringUtil.TruncateToBufferCellWidth(
                        rawUI,
                        " "
                        + StringUtil.Format(
                            ProgressNodeStrings.SecondsRemaining,
                            indent,
                            span)
                        + " ",
                    maxWidth));
            }

            // Fifth and Sixth lines: The current operation

            if (!string.IsNullOrEmpty(CurrentOperation))
            {
                strCollection.Add(" ");
                RenderFullDescription(this.CurrentOperation, indent, maxWidth, rawUI, strCollection, isFullPlus);
            }
        }

        private static void RenderFullDescription(string description, string indent, int maxWidth, PSHostRawUserInterface rawUi, ArrayList strCollection, bool isFullPlus)
        {
            string oldDescription = StringUtil.Format(" {0}{1} ", indent, description);
            string newDescription;

            do
            {
                newDescription = StringUtil.TruncateToBufferCellWidth(rawUi, oldDescription, maxWidth);
                strCollection.Add(newDescription);

                if (oldDescription.Length == newDescription.Length)
                {
                    break;
                }
                else
                {
                    oldDescription = StringUtil.Format(" {0}{1}", indent, oldDescription.Substring(newDescription.Length));
                }
            } while (isFullPlus);
        }

        /// <summary>
        /// Renders a node in the "Compact" style.
        /// </summary>
        /// <param name="strCollection">
        /// List of strings to which the node's rendering will be appended.
        /// </param>
        /// <param name="indentation">
        /// The indentation level (in BufferCells) at which the node should be rendered.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum number of BufferCells that the rendering is allowed to consume.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>

        private
        void
        RenderCompact(ArrayList strCollection, int indentation, int maxWidth, PSHostRawUserInterface rawUI)
        {
            string indent = StringUtil.Padding(indentation);

            // First line: the activity

            strCollection.Add(
                StringUtil.TruncateToBufferCellWidth(
                    rawUI,
                    StringUtil.Format(" {0}{1} ", indent, this.Activity), maxWidth));

            indentation += 3;
            indent = StringUtil.Padding(indentation);

            // Second line: the status description with percentage and time remaining, if applicable.

            string percent = string.Empty;
            if (PercentComplete >= 0)
            {
                percent = StringUtil.Format("{0}% ", PercentComplete);
            }

            string secRemain = string.Empty;
            if (SecondsRemaining >= 0)
            {
                TimeSpan span = new TimeSpan(0, 0, SecondsRemaining);
                secRemain = span.ToString() + " ";
            }

            strCollection.Add(
                StringUtil.TruncateToBufferCellWidth(
                    rawUI,
                    StringUtil.Format(
                        " {0}{1}{2}{3} ",
                        indent,
                        percent,
                        secRemain,
                        StatusDescription),
                    maxWidth));

            // Third line: The current operation

            if (!string.IsNullOrEmpty(CurrentOperation))
            {
                strCollection.Add(
                    StringUtil.TruncateToBufferCellWidth(
                        rawUI,
                        StringUtil.Format(" {0}{1} ", indent, this.CurrentOperation), maxWidth));
            }
        }

        /// <summary>
        /// Renders a node in the "Minimal" style.
        /// </summary>
        /// <param name="strCollection">
        /// List of strings to which the node's rendering will be appended.
        /// </param>
        /// <param name="indentation">
        /// The indentation level (in BufferCells) at which the node should be rendered.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum number of BufferCells that the rendering is allowed to consume.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>

        private
        void
        RenderMinimal(ArrayList strCollection, int indentation, int maxWidth, PSHostRawUserInterface rawUI)
        {
            string indent = StringUtil.Padding(indentation);

            // First line: Everything mushed into one line

            string percent = string.Empty;
            if (PercentComplete >= 0)
            {
                percent = StringUtil.Format("{0}% ", PercentComplete);
            }

            string secRemain = string.Empty;
            if (SecondsRemaining >= 0)
            {
                TimeSpan span = new TimeSpan(0, 0, SecondsRemaining);
                secRemain = span.ToString() + " ";
            }

            strCollection.Add(
                StringUtil.TruncateToBufferCellWidth(
                    rawUI,
                    StringUtil.Format(
                        " {0}{1} {2}{3}{4} ",
                        indent,
                        Activity,
                        percent,
                        secRemain,
                        StatusDescription),
                    maxWidth));
        }

        /// <summary>
        /// The nodes that have this node as their parent.
        /// </summary>

        internal
        ArrayList
        Children;

        /// <summary>
        /// The "age" of the node.  A node's age is incremented by PendingProgress.Update each time a new ProgressRecord is
        /// received by the host. A node's age is reset when a corresponding ProgressRecord is received.  Thus, the age of
        /// a node reflects the number of ProgressRecord that have been received since the node was last updated.
        ///
        /// The age is used by PendingProgress.Render to determine which nodes should be rendered on the display, and how. As the
        /// display has finite size, it may be possible to have many more outstanding progress activities than will fit in that
        /// space. The rendering of nodes can be progressively "compressed" into a more terse format, or not rendered at all in
        /// order to fit as many nodes as possible in the available space. The oldest nodes are compressed or skipped first.
        /// </summary>

        internal
        int
        Age;

        /// <summary>
        /// The style in which this node should be rendered.
        /// </summary>

        internal
        RenderStyle
        Style = RenderStyle.FullPlus;

        /// <summary>
        /// Identifies the source of the progress record.
        /// </summary>

        internal
        Int64
        SourceId;

        /// <summary>
        /// The number of vertical BufferCells that are required to render the node in its current style.
        /// </summary>
        /// <value></value>

        internal int LinesRequiredMethod(PSHostRawUserInterface rawUi, int maxWidth)
        {
            Dbg.Assert(this.RecordType != ProgressRecordType.Completed, "should never render completed records");

            switch (Style)
            {
                case RenderStyle.FullPlus:
                    return LinesRequiredInFullStyleMethod(rawUi, maxWidth, isFullPlus: true);

                case RenderStyle.Full:
                    return LinesRequiredInFullStyleMethod(rawUi, maxWidth, isFullPlus: false);

                case RenderStyle.Compact:
                    return LinesRequiredInCompactStyle;

                case RenderStyle.Minimal:
                    return 1;

                case RenderStyle.Invisible:
                    return 0;

                default:
                    Dbg.Assert(false, "Unknown RenderStyle value");
                    break;
            }

            return 0;
        }

        /// <summary>
        /// The number of vertical BufferCells that are required to render the node in the Full style.
        /// </summary>
        /// <value></value>

        private int LinesRequiredInFullStyleMethod(PSHostRawUserInterface rawUi, int maxWidth, bool isFullPlus)
        {
            // Since the fields of this instance could have been changed, we compute this on-the-fly.

            // NTRAID#Windows OS Bugs-1062104-2004/12/15-sburns we assume 1 line for each field.  If we ever need to
            // word-wrap text fields, then this calculation will need updating.

            // Start with 1 for the Activity
            int lines = 1;
            // Use 5 spaces as the heuristic indent. 5 spaces stand for the indent for the CurrentOperation of the first-level child node
            var indent = StringUtil.Padding(5);
            var temp = new ArrayList();

            if (isFullPlus)
            {
                temp.Clear();
                RenderFullDescription(StatusDescription, indent, maxWidth, rawUi, temp, isFullPlus: true);
                lines += temp.Count;
            }
            else
            {
                // 1 for the Status
                lines++;
            }

            if (PercentComplete >= 0)
            {
                ++lines;
            }

            if (SecondsRemaining >= 0)
            {
                ++lines;
            }

            if (!string.IsNullOrEmpty(CurrentOperation))
            {
                if (isFullPlus)
                {
                    lines += 1;
                    temp.Clear();
                    RenderFullDescription(CurrentOperation, indent, maxWidth, rawUi, temp, isFullPlus: true);
                    lines += temp.Count;
                }
                else
                {
                    lines += 2;
                }
            }

            return lines;
        }

        /// <summary>
        /// The number of vertical BufferCells that are required to render the node in the Compact style.
        /// </summary>
        /// <value></value>

        private
        int
        LinesRequiredInCompactStyle
        {
            get
            {
                // Since the fields of this instance could have been changed, we compute this on-the-fly.

                // NTRAID#Windows OS Bugs-1062104-2004/12/15-sburns we assume 1 line for each field.  If we ever need to
                // word-wrap text fields, then this calculation will need updating.

                // Start with 1 for the Activity, and 1 for the Status.

                int lines = 2;
                if (!string.IsNullOrEmpty(CurrentOperation))
                {
                    ++lines;
                }

                return lines;
            }
        }
    }
}   // namespace

