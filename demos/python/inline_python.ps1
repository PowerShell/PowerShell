# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#
# An example showing inline Python code in a PowerShell script
#

"Hello from PowerShell!"

# Inline Python code in a "here string" which allows for a multi-line script
python3 -c @"
print('    Hello from Python!')
print('    Python and PowerShell get along great!')
"@

# Back to PowerShell...
"Back to PowerShell."
"Bye now!"

