// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Threading;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Renderer for adding VT100 escape sequences for items in a list block.
    /// </summary>
    internal class ListItemBlockRenderer : VT100ObjectRenderer<ListItemBlock>
    {
        protected override void Write(VT100Renderer renderer, ListItemBlock obj)
        {
            if (obj.Parent is ListBlock parent)
            {
                if (!parent.IsOrdered)
                {
                    foreach (var line in obj)
                    {
                        RenderWithIndent(renderer, line, parent.BulletType, 0);
                    }
                }
            }
        }

        private void RenderWithIndent(VT100Renderer renderer, MarkdownObject block, char listBullet, int indentLevel)
        {
            // Indent left by 2 for each level on list.
            string indent = Padding(indentLevel * 2);

            if (block is ParagraphBlock paragraphBlock)
            {
                renderer.Write(indent).Write(listBullet).Write(" ").Write(paragraphBlock.Inline);
            }
            else
            {
                // If there is a sublist, the block is a ListBlock instead of ParagraphBlock.
                if (block is ListBlock subList)
                {
                    foreach (var subListItem in subList)
                    {
                        if (subListItem is ListItemBlock subListItemBlock)
                        {
                            foreach (var line in subListItemBlock)
                            {
                                // Increment indent level for sub list.
                                RenderWithIndent(renderer, line, listBullet, indentLevel + 1);
                            }
                        }
                    }
                }
            }
        }

        // Typical padding is at most a screen's width, any more than that and we won't bother caching.
        private const int IndentCacheMax = 120;
        private static readonly string[] IndentCache = new string[IndentCacheMax];

        internal static string Padding(int countOfSpaces)
        {
            if (countOfSpaces >= IndentCacheMax)
            {
                return new string(' ', countOfSpaces);
            }

            var result = IndentCache[countOfSpaces];

            if (result == null)
            {
                Interlocked.CompareExchange(ref IndentCache[countOfSpaces], new string(' ', countOfSpaces), comparand : null);
                result = IndentCache[countOfSpaces];
            }

            return result;
        }
    }
}
