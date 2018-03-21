// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Syntax;
using Markdig.Renderers;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Renderer for adding VT100 escape sequences for items in a list block.
    /// </summary>
    internal class ListItemBlockRenderer : VT100ObjectRenderer<ListItemBlock>
    {
        protected override void Write(VT100Renderer renderer, ListItemBlock obj)
        {
            var parent = obj.Parent as ListBlock;

            if (parent != null)
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
            string indent = "".PadLeft(indentLevel * 2);

            var paragraphBlock = block as ParagraphBlock;

            if(paragraphBlock != null)
            {
                renderer.Write(indent).Write(listBullet).Write(" ").Write(paragraphBlock.Inline);
            }
            else //If there is a sublist, the block is a ListBlock instead of ParagraphBlock.
            {
                var subList = block as ListBlock;
                if (subList != null)
                {
                    foreach(var subListItem in subList)
                    {
                        var subListItemBlock = subListItem as ListItemBlock;

                        if(subListItemBlock != null)
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
    }
}
