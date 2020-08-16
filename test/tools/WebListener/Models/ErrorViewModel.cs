// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace mvc.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
