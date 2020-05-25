# ----------------------------------------------------------------------------- 
# Script: Get-WmiClassMethodsAndProperties.ps1 
# Author: ed wilson, msft 
# Date: 03/09/2011 15:27:53 
# Keywords: Scripting Techniques, WMI 
# comments: Combines scripts from HSG-3-10-11 and HSG-3-11-11 
# WES-3-12-11 
# ----------------------------------------------------------------------------- 
function New-Underline 
{ 
<# 
.Synopsis 
 Creates an underline the length of the input string 
.Example 
 New-Underline -strIN "Hello world" 
.Example 
 New-Underline -strIn "Morgen welt" -char "-" -sColor "blue" -uColor "yellow" 
.Example 
 "this is a string" | New-Underline 
.Notes 
 NAME: 
 AUTHOR: Ed Wilson 
 LASTEDIT: 5/20/2009 
 KEYWORDS: 
.Link 
 Http://www.ScriptingGuys.com 
#> 
[CmdletBinding()] 
param( 
      [Parameter(Mandatory = $true,Position = 0,valueFromPipeline=$true)] 
      [string] 
      $strIN, 
      [string] 
      $char = "=", 
      [string] 
      $sColor = "Green", 
      [string] 
      $uColor = "darkGreen", 
      [switch] 
      $pipe 
 ) #end param 
 $strLine= $char * $strIn.length 
 if(-not $pipe) 
  { 
   Write-Host -ForegroundColor $sColor $strIN 
   Write-Host -ForegroundColor $uColor $strLine 
  } 
  Else 
  { 
  $strIn 
  $strLine 
  } 
} #end new-underline function 
 
Function Get-WmiClassMethods 
{  
 Param( 
   [string]$namespace = "root\cimv2", 
   [string]$computer = ".", 
   $class 
) 
 $abstract = $false 
 $method = $null 
 #$classes = Get-WmiObject -List -Namespace $namespace | Where-Object { $_.methods } 
 #Foreach($class in $classes) 
 #{ 
  Foreach($q in $class.Qualifiers) 
   { if ($q.name -eq 'Abstract') {$abstract = $true} } 
  If(!$abstract)  
    {  
     Foreach($m in $class.methods) 
      {  
       Foreach($q in $m.qualifiers)  
        {  
         if($q.name -match "implemented")  
          {  
            $method += $m.name + "`r`n" 
          } #end if name 
        } #end foreach q 
      } #end foreach m 
      if($method)  
        { 
         New-Underline -strIN $class.name  
         New-Underline "METHODS" -char "-" 
        } 
      $method 
    } #end if not abstract 
  $abstract = $false 
  $method = $null 
# } #end foreach class 
} #end function Get-WmiClassMethods 
 
Function Get-WmiClassProperties 
{  
 Param( 
   [string]$namespace = "root\cimv2", 
   [string]$computer = ".", 
   $class 
) 
 $abstract = $false 
 $property = $null 
 #$classes = Get-WmiObject -List -Namespace $namespace  
 #Foreach($class in $classes) 
 #{ 
  Foreach($q in $class.Qualifiers) 
   { if ($q.name -eq 'Abstract') {$abstract = $true} } 
  If(!$abstract)  
    {  
     Foreach($p in $class.Properties) 
      {  
       Foreach($q in $p.qualifiers)  
        {  
         if($q.name -match "write")  
          {  
            $property += $p.name + "`r`n" 
          } #end if name 
        } #end foreach q 
      } #end foreach p 
      if($property)  
        { 
         New-Underline -strIN $class.name 
         New-Underline "PROPERTIES" -char "-" 
        } 
      $property 
    } #end if not abstract 
  $abstract = $false 
  $property = $null 
# } #end foreach class 
} #end function Get-WmiClassProperties 
 
# *** Entry Point to Script *** 
 
$classes = Get-WmiObject -List -Namespace $namespace  
Foreach($class in $classes) 
 { 
  Get-WmiClassMethods -class $class 
  Get-WmiClassProperties -class $class 
 }