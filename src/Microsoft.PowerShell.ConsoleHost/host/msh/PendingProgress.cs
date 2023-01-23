// Copyright (c) Microsoft Corporation.
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
    /// Represents all of the outstanding progress activities received by the host, and includes methods to update that state
    /// upon receipt of new ProgressRecords, and to render that state into an array of strings such that ProgressPane can
    /// display it.
    ///
    /// The set of activities that we're tracking is logically a binary tree, with siblings in one branch and children in
    /// another.  For ease of implementation, this tree is represented as lists of lists.  We use ArrayList as out list type,
    /// although List1 (generic List) would also have worked. I suspect that ArrayList is faster because there are fewer links
    /// to twiddle, though I have not measured that.
    ///
    /// This class uses lots of nearly identical helper functions to recursively traverse the tree. If I weren't so pressed
    /// for time, I would see if generic methods could be used to collapse the number of traversers.
    /// </summary>
    internal
    class PendingProgress
    {
        #region Updating Code

        /// <summary>
        /// Update the data structures that represent the outstanding progress records reported so far.
        /// </summary>
        /// <param name="sourceId">
        /// Identifier of the source of the event.  This is used as part of the "key" for matching newly received records with
        /// records that have already been received. For a record to match (meaning that they refer to the same activity), both
        /// the source and activity identifiers need to match.
        /// </param>
        /// <param name="record">
        /// The ProgressRecord received that will either update the status of an activity which we are already tracking, or
        /// represent a new activity that we need to track.
        /// </param>
        internal
        void
        Update(long sourceId, ProgressRecord record)
        {
            Dbg.Assert(record != null, "record should not be null");

            do
            {
                if (record.ParentActivityId == record.ActivityId)
                {
                    // ignore malformed records.
                    break;
                }

                ArrayList listWhereFound = null;
                int indexWhereFound = -1;
                ProgressNode foundNode =
                    FindNodeById(sourceId, record.ActivityId, out listWhereFound, out indexWhereFound);

                if (foundNode != null)
                {
                    Dbg.Assert(listWhereFound != null, "node found, but list not identified");
                    Dbg.Assert(indexWhereFound >= 0, "node found, but index not returned");

                    if (record.RecordType == ProgressRecordType.Completed)
                    {
                        RemoveNodeAndPromoteChildren(listWhereFound, indexWhereFound);
                        break;
                    }

                    if (record.ParentActivityId == foundNode.ParentActivityId)
                    {
                        // record is an update to an existing activity. Copy the record data into the found node, and
                        // reset the age of the node.

                        foundNode.Activity = record.Activity;
                        foundNode.StatusDescription = record.StatusDescription;
                        foundNode.CurrentOperation = record.CurrentOperation;
                        foundNode.PercentComplete = Math.Min(record.PercentComplete, 100);
                        foundNode.SecondsRemaining = record.SecondsRemaining;
                        foundNode.Age = 0;
                        break;
                    }
                    else
                    {
                        // The record's parent Id mismatches with that of the found node's.  We interpret
                        // this to mean that the activity represented by the record (and the found node) is
                        // being "re-parented" elsewhere. So we remove the found node and treat the record
                        // as a new activity.

                        RemoveNodeAndPromoteChildren(listWhereFound, indexWhereFound);
                    }
                }

                // At this point, the record's activity is not in the tree. So we need to add it.

                if (record.RecordType == ProgressRecordType.Completed)
                {
                    // We don't track completion records that don't correspond to activities we're not
                    // already tracking.

                    break;
                }

                ProgressNode newNode = new ProgressNode(sourceId, record);

                // If we're adding a node, and we have no more space, then we need to pick a node to evict.

                while (_nodeCount >= maxNodeCount)
                {
                    EvictNode();
                }

                if (newNode.ParentActivityId >= 0)
                {
                    ProgressNode parentNode = FindNodeById(newNode.SourceId, newNode.ParentActivityId);
                    if (parentNode != null)
                    {
                        parentNode.Children ??= new ArrayList();

                        AddNode(parentNode.Children, newNode);
                        break;
                    }

                    // The parent node is not in the tree. Make the new node's parent the root,
                    // and add it to the tree.  If the parent ever shows up, then the next time
                    // we receive a record for this activity, the parent id's won't match, and the
                    // activity will be properly re-parented.

                    newNode.ParentActivityId = -1;
                }

                AddNode(_topLevelNodes, newNode);
            } while (false);

            // At this point the tree is up-to-date.  Make a pass to age all of the nodes

            AgeNodesAndResetStyle();
        }

        private
        void
        EvictNode()
        {
            ArrayList listWhereFound = null;
            int indexWhereFound = -1;

            ProgressNode oldestNode = FindOldestLeafmostNode(out listWhereFound, out indexWhereFound);
            if (oldestNode == null)
            {
                // Well that's a surprise.  There's got to be at least one node there that's older than 0.

                Dbg.Assert(false, "Must be an old node in the tree somewhere");

                // We'll just pick the root node, then.

                RemoveNode(_topLevelNodes, 0);
            }
            else
            {
                RemoveNode(listWhereFound, indexWhereFound);
            }
        }

        /// <summary>
        /// Removes a node from the tree.
        /// </summary>
        /// <param name="nodes">
        /// List in the tree from which the node is to be removed.
        /// </param>
        /// <param name="indexToRemove">
        /// Index into the list of the node to be removed.
        /// </param>
        private
        void
        RemoveNode(ArrayList nodes, int indexToRemove)
        {
#if DEBUG || ASSERTIONS_TRACE
            ProgressNode nodeToRemove = (ProgressNode)nodes[indexToRemove];

            Dbg.Assert(nodes != null, "can't remove nodes from a null list");
            Dbg.Assert(indexToRemove < nodes.Count, "index is not in list");
            Dbg.Assert(nodes[indexToRemove] != null, "no node at specified index");
            Dbg.Assert(nodeToRemove.Children == null || nodeToRemove.Children.Count == 0, "can't remove a node with children");
#endif

            nodes.RemoveAt(indexToRemove);
            --_nodeCount;

#if DEBUG || ASSERTIONS_ON
            Dbg.Assert(_nodeCount == this.CountNodes(), "We've lost track of the number of nodes in the tree");
#endif
        }

        private
        void
        RemoveNodeAndPromoteChildren(ArrayList nodes, int indexToRemove)
        {
            ProgressNode nodeToRemove = (ProgressNode)nodes[indexToRemove];

            Dbg.Assert(nodes != null, "can't remove nodes from a null list");
            Dbg.Assert(indexToRemove < nodes.Count, "index is not in list");
            Dbg.Assert(nodeToRemove != null, "no node at specified index");

            if (nodeToRemove == null)
            {
                return;
            }

            if (nodeToRemove.Children != null)
            {
                // promote the children.

                for (int i = 0; i < nodeToRemove.Children.Count; ++i)
                {
                    // unparent the children. If the children are ever updated again, they will be reparented.

                    ((ProgressNode)nodeToRemove.Children[i]).ParentActivityId = -1;
                }

                // add the children as siblings

                nodes.RemoveAt(indexToRemove);
                --_nodeCount;
                nodes.InsertRange(indexToRemove, nodeToRemove.Children);

#if DEBUG || ASSERTIONS_TRACE
                Dbg.Assert(_nodeCount == this.CountNodes(), "We've lost track of the number of nodes in the tree");
#endif
            }
            else
            {
                // nothing to promote

                RemoveNode(nodes, indexToRemove);
                return;
            }
        }

        /// <summary>
        /// Adds a node to the tree, first removing the oldest node if the tree is too large.
        /// </summary>
        /// <param name="nodes">
        /// List in the tree where the node is to be added.
        /// </param>
        /// <param name="nodeToAdd">
        /// Node to be added.
        /// </param>
        private
        void
        AddNode(ArrayList nodes, ProgressNode nodeToAdd)
        {
            nodes.Add(nodeToAdd);
            ++_nodeCount;

#if DEBUG || ASSERTIONS_TRACE
            Dbg.Assert(_nodeCount == this.CountNodes(), "We've lost track of the number of nodes in the tree");
            Dbg.Assert(_nodeCount <= maxNodeCount, "Too many nodes in tree!");
#endif
        }

        private sealed class FindOldestNodeVisitor : NodeVisitor
        {
            internal override
            bool
            Visit(ProgressNode node, ArrayList listWhereFound, int indexWhereFound)
            {
                if (node.Age >= _oldestSoFar)
                {
                    _oldestSoFar = node.Age;
                    FoundNode = node;
                    this.ListWhereFound = listWhereFound;
                    this.IndexWhereFound = indexWhereFound;
                }

                return true;
            }

            internal
            ProgressNode
            FoundNode;

            internal
            ArrayList
            ListWhereFound;

            internal
            int
            IndexWhereFound = -1;

            private int _oldestSoFar;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage(
            "Performance",
            "CA1822:Mark members as static",
            Justification = "Accesses instance members in preprocessor branch.")]
        private
        ProgressNode
        FindOldestLeafmostNodeHelper(ArrayList treeToSearch, out ArrayList listWhereFound, out int indexWhereFound)
        {
            listWhereFound = null;
            indexWhereFound = -1;

            FindOldestNodeVisitor v = new FindOldestNodeVisitor();
            NodeVisitor.VisitNodes(treeToSearch, v);

            listWhereFound = v.ListWhereFound;
            indexWhereFound = v.IndexWhereFound;

#if DEBUG || ASSERTIONS_TRACE
            if (v.FoundNode == null)
            {
                Dbg.Assert(listWhereFound == null, "list should be null when no node found");
                Dbg.Assert(indexWhereFound == -1, "index should indicate no node found");
                Dbg.Assert(_topLevelNodes.Count == 0, "if there is no oldest node, then the tree must be empty");
                Dbg.Assert(_nodeCount == 0, "if there is no oldest node, then the tree must be empty");
            }
#endif
            return v.FoundNode;
        }

        private
        ProgressNode
        FindOldestLeafmostNode(out ArrayList listWhereFound, out int indexWhereFound)
        {
            listWhereFound = null;
            indexWhereFound = -1;

            ProgressNode result = null;
            ArrayList treeToSearch = _topLevelNodes;

            while (true)
            {
                result = FindOldestLeafmostNodeHelper(treeToSearch, out listWhereFound, out indexWhereFound);
                if (result == null || result.Children == null || result.Children.Count == 0)
                {
                    break;
                }

                // search the subtree for the oldest child

                treeToSearch = result.Children;
            }

            return result;
        }

        /// <summary>
        /// Convenience overload.
        /// </summary>
        private
        ProgressNode
        FindNodeById(long sourceId, int activityId)
        {
            ArrayList listWhereFound = null;
            int indexWhereFound = -1;
            return
                FindNodeById(sourceId, activityId, out listWhereFound, out indexWhereFound);
        }

        private sealed class FindByIdNodeVisitor : NodeVisitor
        {
            internal
            FindByIdNodeVisitor(long sourceIdToFind, int activityIdToFind)
            {
                _sourceIdToFind = sourceIdToFind;
                _idToFind = activityIdToFind;
            }

            internal override
            bool
            Visit(ProgressNode node, ArrayList listWhereFound, int indexWhereFound)
            {
                if (node.ActivityId == _idToFind && node.SourceId == _sourceIdToFind)
                {
                    this.FoundNode = node;
                    this.ListWhereFound = listWhereFound;
                    this.IndexWhereFound = indexWhereFound;
                    return false;
                }

                return true;
            }

            internal
            ProgressNode
            FoundNode;

            internal
            ArrayList
            ListWhereFound;

            internal
            int
            IndexWhereFound = -1;

            private readonly int _idToFind = -1;
            private readonly long _sourceIdToFind;
        }

        /// <summary>
        /// Finds a node with a given ActivityId in provided set of nodes. Recursively walks the set of nodes and their children.
        /// </summary>
        /// <param name="sourceId">
        /// Identifier of the source of the record.
        /// </param>
        /// <param name="activityId">
        /// ActivityId to search for.
        /// </param>
        /// <param name="listWhereFound">
        /// Receives reference to the List where the found node was located, or null if no suitable node was found.
        /// </param>
        /// <param name="indexWhereFound">
        /// Receives the index into listWhereFound that indicating where in the list the node was located, or -1 if
        /// no suitable node was found.
        /// </param>
        /// <returns>
        /// The found node, or null if no suitable node was located.
        /// </returns>
        private
        ProgressNode
        FindNodeById(long sourceId, int activityId, out ArrayList listWhereFound, out int indexWhereFound)
        {
            listWhereFound = null;
            indexWhereFound = -1;

            FindByIdNodeVisitor v = new FindByIdNodeVisitor(sourceId, activityId);
            NodeVisitor.VisitNodes(_topLevelNodes, v);

            listWhereFound = v.ListWhereFound;
            indexWhereFound = v.IndexWhereFound;

#if DEBUG || ASSERTIONS_TRACE
            if (v.FoundNode == null)
            {
                Dbg.Assert(listWhereFound == null, "list should be null when no node found");
                Dbg.Assert(indexWhereFound == -1, "index should indicate no node found");
            }
#endif
            return v.FoundNode;
        }

        /// <summary>
        /// Finds the oldest node with a given rendering style that is at least as old as a given age.
        /// </summary>
        /// <param name="nodes">
        /// List of nodes to search. Child lists of each node in this list will also be searched.
        /// </param>
        /// <param name="oldestSoFar"></param>
        /// The minimum age of the node to be located.  To find the oldest node, pass 0.
        /// <param name="style">
        /// The rendering style of the node to be located.
        /// </param>
        /// <returns>
        /// The found node, or null if no suitable node was located.
        /// </returns>
        private
        ProgressNode
        FindOldestNodeOfGivenStyle(ArrayList nodes, int oldestSoFar, ProgressNode.RenderStyle style)
        {
            if (nodes == null)
            {
                return null;
            }

            ProgressNode found = null;
            for (int i = 0; i < nodes.Count; ++i)
            {
                ProgressNode node = (ProgressNode)nodes[i];
                Dbg.Assert(node != null, "nodes should not contain null elements");

                if (node.Age >= oldestSoFar && node.Style == style)
                {
                    found = node;
                    oldestSoFar = found.Age;
                }

                if (node.Children != null)
                {
                    ProgressNode child = FindOldestNodeOfGivenStyle(node.Children, oldestSoFar, style);

                    if (child != null)
                    {
                        // In this universe, parents can be younger than their children. We found a child older than us.

                        found = child;
                        oldestSoFar = found.Age;
                    }
                }
            }

#if DEBUG || ASSERTIONS_TRACE
            if (found != null)
            {
                Dbg.Assert(found.Style == style, "unexpected style");
                Dbg.Assert(found.Age >= oldestSoFar, "unexpected age");
            }
#endif
            return found;
        }

        private sealed class AgeAndResetStyleVisitor : NodeVisitor
        {
            internal override
            bool
            Visit(ProgressNode node, ArrayList unused, int unusedToo)
            {
                node.Age = Math.Min(node.Age + 1, Int32.MaxValue - 1);

                node.Style = ProgressNode.IsMinimalProgressRenderingEnabled()
                    ? ProgressNode.RenderStyle.Ansi
                    : node.Style = ProgressNode.RenderStyle.FullPlus;

                return true;
            }
        }

        /// <summary>
        /// Increments the age of each of the nodes in the given list, and all their children.  Also sets the rendering
        /// style of each node to "full."
        ///
        /// All nodes are aged every time a new ProgressRecord is received.
        /// </summary>
        private
        void
        AgeNodesAndResetStyle()
        {
            AgeAndResetStyleVisitor arsv = new AgeAndResetStyleVisitor();
            NodeVisitor.VisitNodes(_topLevelNodes, arsv);
        }

        #endregion // Updating Code

        #region Rendering Code

        /// <summary>
        /// Generates an array of strings representing as much of the outstanding progress activities as possible within the given
        /// space.  As more outstanding activities are collected, nodes are "compressed" (i.e. rendered in an increasing terse
        /// fashion) in order to display as many as possible.  Ultimately, some nodes may be compressed to the point of
        /// invisibility. The oldest nodes are compressed first.
        /// </summary>
        /// <param name="maxWidth">
        /// The maximum width (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="maxHeight">
        /// The maximum height (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        /// <returns>
        /// An array of strings containing the textual representation of the outstanding progress activities.
        /// </returns>
        internal
        string[]
        Render(int maxWidth, int maxHeight, PSHostRawUserInterface rawUI)
        {
            Dbg.Assert(_topLevelNodes != null, "Shouldn't need to render progress if no data exists");
            Dbg.Assert(maxWidth > 0, "maxWidth is too small");
            Dbg.Assert(maxHeight >= 3, "maxHeight is too small");

            if (_topLevelNodes == null || _topLevelNodes.Count <= 0)
            {
                // we have nothing to render.

                return null;
            }

            int invisible = 0;
            if (TallyHeight(rawUI, maxHeight, maxWidth) > maxHeight)
            {
                // This will smash down nodes until the tree will fit into the alloted number of lines.  If in the
                // process some nodes were made invisible, we will add a line to the display to say so.

                invisible = CompressToFit(rawUI, maxHeight, maxWidth);
            }

            ArrayList result = new ArrayList();

            if (ProgressNode.IsMinimalProgressRenderingEnabled())
            {
                RenderHelper(result, _topLevelNodes, indentation: 0, maxWidth, rawUI);
                return (string[])result.ToArray(typeof(string));
            }

            string border = StringUtil.Padding(maxWidth);

            result.Add(border);
            RenderHelper(result, _topLevelNodes, 0, maxWidth, rawUI);
            if (invisible == 1)
            {
                result.Add(
                    " "
                    + StringUtil.Format(
                        ProgressNodeStrings.InvisibleNodesMessageSingular,
                        invisible));
            }
            else if (invisible > 1)
            {
                result.Add(
                    " "
                    + StringUtil.Format(
                        ProgressNodeStrings.InvisibleNodesMessagePlural,
                        invisible));
            }

            result.Add(border);

            return (string[])result.ToArray(typeof(string));
        }

        /// <summary>
        /// Helper function for Render().  Recursively renders nodes.
        /// </summary>
        /// <param name="strings">
        /// The rendered strings so far.  Additional rendering will be appended.
        /// </param>
        /// <param name="nodes">
        /// The nodes to be rendered.  All child nodes will also be rendered.
        /// </param>
        /// <param name="indentation">
        /// The current indentation level (in BufferCells).
        /// </param>
        /// <param name="maxWidth">
        /// The maximum number of BufferCells that the rendering can consume, horizontally.
        /// </param>
        /// <param name="rawUI">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        private
        void
        RenderHelper(ArrayList strings, ArrayList nodes, int indentation, int maxWidth, PSHostRawUserInterface rawUI)
        {
            Dbg.Assert(strings != null, "strings should not be null");
            Dbg.Assert(nodes != null, "nodes should not be null");

            if (nodes == null)
            {
                return;
            }

            foreach (ProgressNode node in nodes)
            {
                int lines = strings.Count;

                node.Render(strings, indentation, maxWidth, rawUI);

                if (node.Children != null)
                {
                    // indent only if the rendering of node actually added lines to the strings.

                    int indentationIncrement = (strings.Count > lines) ? 2 : 0;

                    RenderHelper(strings, node.Children, indentation + indentationIncrement, maxWidth, rawUI);
                }
            }
        }

        private sealed class HeightTallyer : NodeVisitor
        {
            internal HeightTallyer(PSHostRawUserInterface rawUi, int maxHeight, int maxWidth)
            {
                _rawUi = rawUi;
                _maxHeight = maxHeight;
                _maxWidth = maxWidth;
            }

            internal override
            bool
            Visit(ProgressNode node, ArrayList unused, int unusedToo)
            {
                Tally += node.LinesRequiredMethod(_rawUi, _maxWidth);

                // We don't need to walk all the nodes, once it's larger than the max height, we should stop
                if (Tally > _maxHeight)
                {
                    return false;
                }

                return true;
            }

            private readonly PSHostRawUserInterface _rawUi;
            private readonly int _maxHeight;
            private readonly int _maxWidth;

            internal int Tally;
        }

        /// <summary>
        /// Tallies up the number of BufferCells vertically that will be required to show all the ProgressNodes in the given
        /// list, and all of their children.
        /// </summary>
        /// <param name="maxHeight">
        /// The maximum height (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="rawUi">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        /// <returns>
        /// The vertical height (in BufferCells) that will be required to show all of the nodes in the given list.
        /// </returns>
        /// <param name="maxWidth">
        /// </param>
        private int TallyHeight(PSHostRawUserInterface rawUi, int maxHeight, int maxWidth)
        {
            HeightTallyer ht = new HeightTallyer(rawUi, maxHeight, maxWidth);
            NodeVisitor.VisitNodes(_topLevelNodes, ht);
            return ht.Tally;
        }

#if DEBUG || ASSERTIONS_TRACE

        /// <summary>
        /// Debugging code.  Verifies that all of the nodes in the given list have the given style.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        private
        bool
        AllNodesHaveGivenStyle(ArrayList nodes, ProgressNode.RenderStyle style)
        {
            if (nodes == null)
            {
                return false;
            }

            for (int i = 0; i < nodes.Count; ++i)
            {
                ProgressNode node = (ProgressNode)nodes[i];
                Dbg.Assert(node != null, "nodes should not contain null elements");

                if (node.Style != style)
                {
                    return false;
                }

                if (node.Children != null)
                {
                    if (!AllNodesHaveGivenStyle(node.Children, style))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Debugging code. NodeVisitor that counts up the number of nodes in the tree.
        /// </summary>
        private
        class
        CountingNodeVisitor : NodeVisitor
        {
            internal override
            bool
            Visit(ProgressNode unused, ArrayList unusedToo, int unusedThree)
            {
                ++Count;
                return true;
            }

            internal
            int
            Count;
        }

        /// <summary>
        /// Debugging code.  Counts the number of nodes in the tree of nodes.
        /// </summary>
        /// <returns>
        /// The number of nodes in the tree.
        /// </returns>
        private
        int
        CountNodes()
        {
            CountingNodeVisitor cnv = new CountingNodeVisitor();
            NodeVisitor.VisitNodes(_topLevelNodes, cnv);
            return cnv.Count;
        }

#endif

        /// <summary>
        /// Helper function to CompressToFit.  Considers compressing nodes from one level to another.
        /// </summary>
        /// <param name="rawUi">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        /// <param name="maxHeight">
        /// The maximum height (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum width (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="nodesCompressed">
        /// Receives the number of nodes that were compressed. If the result of the method is false, then this will be the total
        /// number of nodes being tracked (i.e. all of them will have been compressed).
        /// </param>
        /// <param name="priorStyle">
        /// The rendering style (e.g. "compression level") that the nodes are expected to currently have.
        /// </param>
        /// <param name="newStyle">
        /// The new rendering style that a node will have when it is compressed. If the result of the method is false, then all
        /// nodes will have this rendering style.
        /// </param>
        /// <returns>
        /// true to indicate that the nodes are compressed to the point that their rendering will fit within the constraint, or
        /// false to indicate that all of the nodes are compressed to a given level, but that the rendering still can't fit
        /// within the constraint.
        /// </returns>
        private
        bool
        CompressToFitHelper(
            PSHostRawUserInterface rawUi,
            int maxHeight,
            int maxWidth,
            out int nodesCompressed,
            ProgressNode.RenderStyle priorStyle,
            ProgressNode.RenderStyle newStyle)
        {
            nodesCompressed = 0;

            while (true)
            {
                ProgressNode node = FindOldestNodeOfGivenStyle(_topLevelNodes, oldestSoFar: 0, priorStyle);
                if (node == null)
                {
                    // We've compressed every node of the prior style already.

                    break;
                }

                node.Style = newStyle;
                ++nodesCompressed;
                if (TallyHeight(rawUi, maxHeight, maxWidth) <= maxHeight)
                {
                    return true;
                }
            }

            // If we get all the way to here, then we've compressed all the nodes and we still don't fit.
            return false;
        }

        /// <summary>
        /// "Compresses" the nodes representing the outstanding progress activities until their rendering will fit within a
        /// "given height, or until they are compressed to a given level.  The oldest nodes are compressed first.
        ///
        /// This is a 4-stage process -- from least compressed to "invisible".  At each stage we find the oldest nodes in the
        /// tree and change their rendering style to a more compact style.  As soon as the rendering of the nodes will fit within
        /// the maxHeight, we stop.  The result is that the most recent nodes will be the least compressed, the idea being that
        /// the rendering should show the most recently updated activities with the most complete rendering for them possible.
        /// </summary>
        /// <param name="rawUi">
        /// The PSHostRawUserInterface used to gauge string widths in the rendering.
        /// </param>
        /// <param name="maxHeight">
        /// The maximum height (in BufferCells) that the rendering may consume.
        /// </param>
        /// <param name="maxWidth">
        /// The maximum width (in BufferCells) that the rendering may consume.
        /// </param>
        /// <returns>
        /// The number of nodes that were made invisible during the compression.
        ///
        /// </returns>
        private
        int
        CompressToFit(PSHostRawUserInterface rawUi, int maxHeight, int maxWidth)
        {
            Dbg.Assert(_topLevelNodes != null, "Shouldn't need to compress if no data exists");

            int nodesCompressed = 0;

            // This algorithm potentially makes many, many passes over the tree.  It might be possible to optimize
            // that some, but I'm not trying to be too clever just yet.

            if (
                CompressToFitHelper(
                    rawUi,
                    maxHeight,
                    maxWidth,
                    out nodesCompressed,
                    ProgressNode.RenderStyle.FullPlus,
                    ProgressNode.RenderStyle.Full))
            {
                return 0;
            }

            if (
                CompressToFitHelper(
                    rawUi,
                    maxHeight,
                    maxWidth,
                    out nodesCompressed,
                    ProgressNode.RenderStyle.Full,
                    ProgressNode.RenderStyle.Compact))
            {
                return 0;
            }

            if (
                CompressToFitHelper(
                    rawUi,
                    maxHeight,
                    maxWidth,
                    out nodesCompressed,
                    ProgressNode.RenderStyle.Compact,
                    ProgressNode.RenderStyle.Minimal))
            {
                return 0;
            }

            if (
                CompressToFitHelper(
                    rawUi,
                    maxHeight,
                    maxWidth,
                    out nodesCompressed,
                    ProgressNode.RenderStyle.Minimal,
                    ProgressNode.RenderStyle.Invisible))
            {
                // The nodes that we compressed here are now invisible.

                return nodesCompressed;
            }

            return 0;
        }

        #endregion // Rendering Code

        #region Utility Code

        private abstract
        class NodeVisitor
        {
            /// <summary>
            /// Called for each node in the tree.
            /// </summary>
            /// <param name="node">
            /// The node being visited.
            /// </param>
            /// <param name="listWhereFound">
            /// The list in which the node resides.
            /// </param>
            /// <param name="indexWhereFound">
            /// The index into listWhereFound of the node.
            /// </param>
            /// <returns>
            /// true to continue visiting nodes, false if not.
            /// </returns>
            internal abstract
            bool
            Visit(ProgressNode node, ArrayList listWhereFound, int indexWhereFound);

            internal static
            void
            VisitNodes(ArrayList nodes, NodeVisitor v)
            {
                if (nodes == null)
                {
                    return;
                }

                for (int i = 0; i < nodes.Count; ++i)
                {
                    ProgressNode node = (ProgressNode)nodes[i];
                    Dbg.Assert(node != null, "nodes should not contain null elements");

                    if (!v.Visit(node, nodes, i))
                    {
                        return;
                    }

                    if (node.Children != null)
                    {
                        VisitNodes(node.Children, v);
                    }
                }
            }
        }

        #endregion

        private readonly ArrayList _topLevelNodes = new ArrayList();
        private int _nodeCount;

        private const int maxNodeCount = 128;
    }
}   // namespace
