// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;

// Disable the warning 'xUnit1031': https://xunit.net/xunit.analyzers/rules/xUnit1031
[assembly: SuppressMessage("xUnit", "xUnit1031", Justification = "Parallelization is disabled")]
