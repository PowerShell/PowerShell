// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;

namespace Microsoft.PowerShell.MarkdownRender
{
    /// <summary>
    /// Implement the MarkdownObjectRenderer with VT100Renderer.
    /// </summary>
    /// <typeparam name="T">The element type of the renderer.</typeparam>
    public abstract class VT100ObjectRenderer<T> : MarkdownObjectRenderer<VT100Renderer, T> where T : MarkdownObject
    {
    }
}
