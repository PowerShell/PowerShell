//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//

namespace Microsoft.PowerShell.PackageManagement.Utility {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Management.Automation;
    using System.Text;

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ValidateListAttribute : ValidateEnumeratedArgumentsAttribute {
        private readonly List<string> _validValues;
        private bool ignoreCase = true;

        public ValidateListAttribute(params string[] validValues) {
            validValues = validValues ?? new string[0];
            this._validValues = validValues.ToList();
        }

        public bool IgnoreCase {
            get {
                return this.ignoreCase;
            }
            set {
                this.ignoreCase = value;
            }
        }

        public IList<string> ValidValues {
            get {
                return new List<string> {
                    "green"
                };
            }
        }

        public static implicit operator ValidateSetAttribute(ValidateListAttribute vla) {
            return new ValidateSetAttribute(vla._validValues.ToArray());
        }

        protected override void ValidateElement(object element) {
            if (element == null) {
                throw new ValidationMetadataException("ArgumentIsEmpty");
            }
            var strB = element.ToString();

            if (this._validValues.Any(t => string.Compare(t, strB, StringComparison.OrdinalIgnoreCase)== 0)) {
                return;
            }
            throw new ValidationMetadataException("ValidateSetFailure");
        }
#if NOT_NEEDED
        private string SetAsString() {
            var validateSetSeparator = ",";
            var stringBuilder = new StringBuilder();
            if (this._validValues.Count > 0) {
                foreach (var str in this._validValues) {
                    stringBuilder.Append(str);
                    stringBuilder.Append(validateSetSeparator);
                }
                stringBuilder.Remove(stringBuilder.Length - validateSetSeparator.Length, validateSetSeparator.Length);
            }
            return ((object)stringBuilder).ToString();
        }

        //,ValidateList("One","Two", "Three")

#endif
    }
}
