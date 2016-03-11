/*============================================================================
 * Copyright (C) Microsoft Corporation, All rights reserved. 
 *============================================================================
 */

using System;
using System.Diagnostics;
using System.Reflection;

[assembly: AssemblyTitle("cs")]
[assembly: AssemblyDescription("")]

//
// TODO: DO WE NEED TO THIS FOR CORECLR. WHY CANT WE DO AWAY WITH THIS ??
// 
//[assembly: Debuggable(true, true)]

[assembly: CLSCompliant(false)] // main reason: decision to map unsigned types used in native API to unsigned types in the public surface of the managed layer