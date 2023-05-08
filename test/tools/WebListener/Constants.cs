// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace mvc.Controllers
{
    internal static class Constants
    {
        public const string HeaderSeparator = ", ";
        public const string ApplicationJson = "application/json";
        public const string LinkUriTemplate = "<{0}?maxlinks={1}&linknumber={2}&type={3}>;{4}rel=\"{5}\"";
        public const string MalformedUrlLinkHeader = "{url}; foo";
        public const string NoRelLinkHeader = "<url>; foo=\"bar\"";
        public const string NoUrlLinkHeader = "<>; rel=\"next\"";

    }
}
