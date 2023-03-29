// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// FormObjectCollection used in HtmlWebResponseObject.
    /// </summary>
    public class FormObjectCollection : Collection<FormObject>
    {
        /// <summary>
        /// Gets the FormObject from the key.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public FormObject? this[string key]
        {
            get
            {
                FormObject? form = null;
                foreach (FormObject f in this)
                {
                    if (string.Equals(key, f.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        form = f;
                        break;
                    }
                }

                return form;
            }
        }
    }
}
