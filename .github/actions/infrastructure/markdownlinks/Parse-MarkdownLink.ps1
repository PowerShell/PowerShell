# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#requires -version 7
# Markdig is always available in PowerShell 7
<#
.SYNOPSIS
    Parse CHANGELOG files using Markdig to extract links.

.DESCRIPTION
    This script uses Markdig.Markdown.Parse to parse all markdown files in the CHANGELOG directory
    and extract different types of links (inline links, reference links, etc.).

.PARAMETER ChangelogPath
    Path to the CHANGELOG directory. Defaults to ./CHANGELOG

.PARAMETER LinkType
    Filter by link type: All, Inline, Reference, AutoLink. Defaults to All.

.EXAMPLE
    .\Parse-MarkdownLink.ps1

.EXAMPLE
    .\Parse-MarkdownLink.ps1 -LinkType Reference
#>

param(
    [string]$ChangelogPath = "./CHANGELOG",
    [ValidateSet("All", "Inline", "Reference", "AutoLink")]
    [string]$LinkType = "All"
)

Write-Verbose "Using built-in Markdig functionality to parse markdown files"

function Get-LinksFromMarkdownAst {
    param(
        [Parameter(Mandatory)]
        [object]$Node,
        [Parameter(Mandatory)]
        [string]$FileName,
        [System.Collections.ArrayList]$Links
    )

    if ($null -eq $Links) {
        return
    }

    # Check if current node is a link
    if ($Node -is [Markdig.Syntax.Inlines.LinkInline]) {
        $linkInfo = [PSCustomObject]@{
            Path = $FileName
            Line = $Node.Line + 1  # Convert to 1-based line numbering
            Column = $Node.Column + 1  # Convert to 1-based column numbering
            Url = $Node.Url ?? ""
            Text = $Node.FirstChild?.ToString() ?? ""
            Type = "Inline"
            IsImage = $Node.IsImage
        }
        [void]$Links.Add($linkInfo)
    }
    elseif ($Node -is [Markdig.Syntax.Inlines.AutolinkInline]) {
        $linkInfo = [PSCustomObject]@{
            Path = $FileName
            Line = $Node.Line + 1
            Column = $Node.Column + 1
            Url = $Node.Url ?? ""
            Text = $Node.Url ?? ""
            Type = "AutoLink"
            IsImage = $false
        }
        [void]$Links.Add($linkInfo)
    }
    elseif ($Node -is [Markdig.Syntax.LinkReferenceDefinitionGroup]) {
        foreach ($refDef in $Node) {
            $linkInfo = [PSCustomObject]@{
                Path = $FileName
                Line = $refDef.Line + 1
                Column = $refDef.Column + 1
                Url = $refDef.Url ?? ""
                Text = $refDef.Label ?? ""
                Type = "Reference"
                IsImage = $false
            }
            [void]$Links.Add($linkInfo)
        }
    }
    elseif ($Node -is [Markdig.Syntax.LinkReferenceDefinition]) {
        $linkInfo = [PSCustomObject]@{
            Path = $FileName
            Line = $Node.Line + 1
            Column = $Node.Column + 1
            Url = $Node.Url ?? ""
            Text = $Node.Label ?? ""
            Type = "Reference"
            IsImage = $false
        }
        [void]$Links.Add($linkInfo)
    }

    # For MarkdownDocument (root), iterate through all blocks
    if ($Node -is [Markdig.Syntax.MarkdownDocument]) {
        foreach ($block in $Node) {
            Get-LinksFromMarkdownAst -Node $block -FileName $FileName -Links $Links
        }
    }
    # For block containers, iterate through children
    elseif ($Node -is [Markdig.Syntax.ContainerBlock]) {
        foreach ($child in $Node) {
            Get-LinksFromMarkdownAst -Node $child -FileName $FileName -Links $Links
        }
    }
    # For leaf blocks with inlines, process the inline content
    elseif ($Node -is [Markdig.Syntax.LeafBlock] -and $Node.Inline) {
        Get-LinksFromMarkdownAst -Node $Node.Inline -FileName $FileName -Links $Links
    }
    # For inline containers, process all child inlines
    elseif ($Node -is [Markdig.Syntax.Inlines.ContainerInline]) {
        $child = $Node.FirstChild
        while ($child) {
            Get-LinksFromMarkdownAst -Node $child -FileName $FileName -Links $Links
            $child = $child.NextSibling
        }
    }
    # For other inline elements that might have children
    elseif ($Node.PSObject.Properties.Name -contains "FirstChild" -and $Node.FirstChild) {
        $child = $Node.FirstChild
        while ($child) {
            Get-LinksFromMarkdownAst -Node $child -FileName $FileName -Links $Links
            $child = $child.NextSibling
        }
    }
}

function Parse-ChangelogFiles {
    param(
        [string]$Path
    )

    if (-not (Test-Path $Path)) {
        Write-Error "CHANGELOG directory not found: $Path"
        return
    }

    $markdownFiles = Get-ChildItem -Path $Path -Filter "*.md" -File

    if ($markdownFiles.Count -eq 0) {
        Write-Warning "No markdown files found in $Path"
        return
    }

    $allLinks = [System.Collections.ArrayList]::new()

    foreach ($file in $markdownFiles) {
        Write-Verbose "Processing file: $($file.Name)"

        try {
            $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8

            # Parse the markdown content using Markdig
            $document = [Markdig.Markdown]::Parse($content, [Markdig.MarkdownPipelineBuilder]::new())

            # Extract links from the AST
            Get-LinksFromMarkdownAst -Node $document -FileName $file.FullName -Links $allLinks

        } catch {
            Write-Warning "Error processing file $($file.Name): $($_.Exception.Message)"
        }
    }    # Filter by link type if specified
    if ($LinkType -ne "All") {
        $allLinks = $allLinks | Where-Object { $_.Type -eq $LinkType }
    }

    return $allLinks
}

# Main execution
$links = Parse-ChangelogFiles -Path $ChangelogPath

# Output PowerShell objects
$links
