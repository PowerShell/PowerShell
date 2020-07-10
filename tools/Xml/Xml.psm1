# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Adds an attribute to a XmlElement
function New-XmlAttribute
{
    param(
        [Parameter(Mandatory)]
        [string]$Name,
        [Parameter(Mandatory)]
        [object]$Value,
        [Parameter(Mandatory)]
        [System.Xml.XmlElement]$Element,
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]$XmlDoc
    )

    $attribute = $XmlDoc.CreateAttribute($Name)
    $attribute.Value = $value
    $null = $Element.Attributes.Append($attribute)
}

# Adds an XmlElement to an XmlNode
# Returns the new Element
function New-XmlElement
{
    param(
        [Parameter(Mandatory)]
        [string]$LocalName,
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]$XmlDoc,
        [Parameter(Mandatory)]
        [System.Xml.XmlNode]$Node,
        [Switch]$PassThru,
        [string]$NamespaceUri
    )

    if($NamespaceUri)
    {
        $newElement = $XmlDoc.CreateElement($LocalName, $NamespaceUri)
    }
    else
    {
        $newElement = $XmlDoc.CreateElement($LocalName)
    }

    $null = $Node.AppendChild($newElement)
    if($PassThru.IsPresent)
    {
        return $newElement
    }
}

# Removes an XmlElement and its parent if it is empty
function Remove-XmlElement
{
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlElement]$Element,
        [Switch]$RemoveEmptyParents
    )

    $parentNode = $Element.ParentNode
    $null = $parentNode.RemoveChild($Element)
    if(!$parentNode.HasChildNodes -and $RemoveEmptyParent.IsPresent)
    {
        Remove-XmlElement -Element $parentNode -RemoveEmptyParents
    }
}

# Get a node by XPath
# Returns null if the node is not found
function Get-XmlNodeByXPath
{
    param(
        [Parameter(Mandatory)]
        [System.Xml.XmlDocument]
        $XmlDoc,
        [System.Xml.XmlNamespaceManager]
        $XmlNsManager,
        [Parameter(Mandatory)]
        [string]
        $XPath
    )

    if($XmlNsManager)
    {
        return $XmlDoc.SelectSingleNode($XPath,$XmlNsManager)
    }
    else
    {
        return $XmlDoc.SelectSingleNode($XPath)
    }
}

Export-ModuleMember -Function @(
    'Get-XmlNodeByXPath'
    'Remove-XmlElement'
    'New-XmlElement'
    'New-XmlAttribute'
)
