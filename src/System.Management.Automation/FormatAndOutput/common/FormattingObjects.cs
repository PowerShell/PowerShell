// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file contains the definitions for the objects
// used in the communication protocol between formatting
// and output commands. the format/xxx commands instantiate
// these objects and write them to the pipeline. The out-xxx
// commands read them from the pipeline.
//
// NOTE:
// Since format/xxx and out-xxx commands can be separated by
// serialization boundaries, the structure of these objects
// must adhere to the Monad serialization constraints.
//
// Since the out-xxx commands heavily access these objects and
// there is an up front need for protocol validation, the out-xxx
// commands do deserialize the objects back from the property bag
// representation that mig have been introduced by serialization.
//
// There is also the need to preserve type information across serialization
// boundaries, therefore the objects provide a GUID based mechanism to
// preserve the information.
//

using System.Collections.Generic;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    #region Root of Class Hierarchy
    /// <summary>
    /// Base class from which all the formatting objects
    /// will derive from.
    /// It provides the mechanism to preserve type information.
    /// </summary>
    internal abstract partial class FormatInfoData
    {
        /// <summary>
        /// Name of the "get" property that allows access to CLSID information.
        /// This is needed by the ERS API's.
        /// </summary>
        internal const string classidProperty = "ClassId2e4f51ef21dd47e99d3c952918aff9cd";

        /// <summary>
        /// String containing a GUID, to be set by each derived class
        /// "get" property to get CLSID information.
        /// It is named with a GUID like name to avoid potential collisions with
        /// properties of payload objects.
        /// </summary>
        public abstract string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get; }
    }

    #endregion

    #region Top Level Messages

    internal abstract class PacketInfoData : FormatInfoData
    {
    }

    internal abstract partial class ControlInfoData : PacketInfoData
    {
        /// <summary>
        /// Null by default, present only if grouping specified.
        /// </summary>
        public GroupingEntry groupingEntry = null;
    }

    internal abstract partial class StartData : ControlInfoData
    {
        /// <summary>
        /// It needs to be either on FormatStartData or GroupStartData
        /// but not both or neither.
        /// </summary>
        public ShapeInfo shapeInfo;
    }

    /// <summary>
    /// Sequence start: the very first message sent.
    /// </summary>
    internal sealed partial class FormatStartData : StartData
    {
        internal const string CLSID = "033ecb2bc07a4d43b5ef94ed5a35d280";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        /// <summary>
        /// Optional.
        /// </summary>
        public PageHeaderEntry pageHeaderEntry;

        /// <summary>
        /// Optional.
        /// </summary>
        public PageFooterEntry pageFooterEntry;

        /// <summary>
        /// Autosize formatting directive. If present, the output command is instructed
        /// to get the autosize "best fit" for the device screen according to the flags
        /// this object contains.
        /// </summary>
        public AutosizeInfo autosizeInfo;
    }

    /// <summary>
    /// Sequence end: the very last message sent.
    /// </summary>
    internal sealed class FormatEndData : ControlInfoData
    {
        internal const string CLSID = "cf522b78d86c486691226b40aa69e95c";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    /// <summary>
    /// Group start: message marking the beginning of a group.
    /// </summary>
    internal sealed class GroupStartData : StartData
    {
        internal const string CLSID = "9e210fe47d09416682b841769c78b8a3";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    /// <summary>
    /// Group end: message marking the end of a group.
    /// </summary>
    internal sealed class GroupEndData : ControlInfoData
    {
        internal const string CLSID = "4ec4f0187cb04f4cb6973460dfe252df";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    /// <summary>
    /// Generic entry containing payload data and related formatting info.
    /// </summary>
    internal sealed partial class FormatEntryData : PacketInfoData
    {
        internal const string CLSID = "27c87ef9bbda4f709f6b4002fa4af63c";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        /// <summary>
        /// Mandatory, but depending on the shape we send in
        /// it must match what got sent in the format start message.
        /// </summary>
        public FormatEntryInfo formatEntryInfo = null;

        public bool outOfBand = false;
        public WriteStreamType writeStream = WriteStreamType.None;
        internal bool isHelpObject = false;
    }
    #endregion

    #region Shape Info Classes

    internal abstract class ShapeInfo : FormatInfoData
    {
    }

    internal sealed partial class WideViewHeaderInfo : ShapeInfo
    {
        internal const string CLSID = "b2e2775d33d544c794d0081f27021b5c";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        /// <summary>
        /// Desired number of columns on the screen.
        /// Advisory, the outputter can decide otherwise
        ///
        /// A zero value signifies let the outputter get the
        /// best fit on the screen (possibly blocking until the end)
        /// </summary>
        public int columns = 0;
    }

    internal sealed partial class TableHeaderInfo : ShapeInfo
    {
        internal const string CLSID = "e3b7a39c089845d388b2e84c5d38f5dd";

        public TableHeaderInfo()
        {
            tableColumnInfoList = new List<TableColumnInfo>();
        }

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public bool hideHeader;
        public bool repeatHeader;
        public List<TableColumnInfo> tableColumnInfoList;
    }

    internal sealed partial class TableColumnInfo : FormatInfoData
    {
        internal const string CLSID = "7572aa4155ec4558817a615acf7dd92e";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        /// <summary>
        /// Width of the column:
        /// == 0 -> let the outputter decide
        /// > 0 -> user provided value.
        /// </summary>
        public int width = 0;

        public int alignment = TextAlignment.Left;
        public string label = null;
        public string propertyName = null;
        public bool HeaderMatchesProperty = true;
    }

    internal sealed class ListViewHeaderInfo : ShapeInfo
    {
        internal const string CLSID = "830bdcb24c1642258724e441512233a4";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed class ComplexViewHeaderInfo : ShapeInfo
    {
        internal const string CLSID = "5197dd85ca6f4cce9ae9e6fd6ded9d76";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    #endregion

    #region Formatting Entries Classes

    internal abstract class FormatEntryInfo : FormatInfoData
    {
    }

    internal sealed partial class RawTextFormatEntry : FormatEntryInfo
    {
        internal const string CLSID = "29ED81BA914544d4BC430F027EE053E9";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public string text = null;
    }

    internal abstract partial class FreeFormatEntry : FormatEntryInfo
    {
        public List<FormatValue> formatValueList = new List<FormatValue>();
    }

    internal sealed partial class ListViewEntry : FormatEntryInfo
    {
        internal const string CLSID = "cf58f450baa848ef8eb3504008be6978";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public List<ListViewField> listViewFieldList = new List<ListViewField>();
    }

    internal sealed partial class ListViewField : FormatInfoData
    {
        internal const string CLSID = "b761477330ce4fb2a665999879324d73";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public string label = null;
        public string propertyName = null;
        public FormatPropertyField formatPropertyField = new FormatPropertyField();
    }

    internal sealed partial class TableRowEntry : FormatEntryInfo
    {
        internal const string CLSID = "0e59526e2dd441aa91e7fc952caf4a36";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public List<FormatPropertyField> formatPropertyFieldList = new List<FormatPropertyField>();
        public bool multiLine = false;
    }

    internal sealed partial class WideViewEntry : FormatEntryInfo
    {
        internal const string CLSID = "59bf79de63354a7b9e4d1697940ff188";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public FormatPropertyField formatPropertyField = new FormatPropertyField();
    }

    internal sealed class ComplexViewEntry : FreeFormatEntry
    {
        internal const string CLSID = "22e7ef3c896449d4a6f2dedea05dd737";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed class GroupingEntry : FreeFormatEntry
    {
        internal const string CLSID = "919820b7eadb48be8e202c5afa5c2716";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed class PageHeaderEntry : FreeFormatEntry
    {
        internal const string CLSID = "dd1290a5950b4b27aa76d9f06199c3b3";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed class PageFooterEntry : FreeFormatEntry
    {
        internal const string CLSID = "93565e84730645c79d4af091123eecbc";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed partial class AutosizeInfo : FormatInfoData
    {
        internal const string CLSID = "a27f094f0eec4d64845801a4c06a32ae";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        /// <summary>
        /// Number of objects to compute the best fit.
        /// Zero: all the objects
        /// a positive number N: use the first N.
        /// </summary>
        public int objectCount = 0;
    }

    #endregion

    #region Format Values

    internal abstract class FormatValue : FormatInfoData
    {
    }

    internal sealed class FormatNewLine : FormatValue
    {
        internal const string CLSID = "de7e8b96fbd84db5a43aa82eb34580ec";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }
    }

    internal sealed partial class FormatTextField : FormatValue
    {
        internal const string CLSID = "b8d9e369024a43a580b9e0c9279e3354";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public string text;
    }

    internal sealed partial class FormatPropertyField : FormatValue
    {
        internal const string CLSID = "78b102e894f742aca8c1d6737b6ff86a";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public string propertyValue = null;
        public int alignment = TextAlignment.Undefined;
    }

    internal sealed partial class FormatEntry : FormatValue
    {
        internal const string CLSID = "fba029a113a5458d932a2ed4871fadf2";

        public FormatEntry()
        {
            formatValueList = new List<FormatValue>();
        }

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public List<FormatValue> formatValueList;

        /// <summary>
        /// Optional information of frame data (indentation, etc.)
        /// </summary>
        public FrameInfo frameInfo;
    }

    internal sealed partial class FrameInfo : FormatInfoData
    {
        internal const string CLSID = "091C9E762E33499eBE318901B6EFB733";

        public override string ClassId2e4f51ef21dd47e99d3c952918aff9cd { get { return CLSID; } }

        public int leftIndentation = 0;
        public int rightIndentation = 0;
        public int firstLine = 0;
    }

    #endregion
}
