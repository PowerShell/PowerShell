# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

#
# Demo simple interoperation between PowerShell and Python

# Basic execution of a Python script fragment
python -c "print('Hi!')"

# Capture output in a variable
$data = python -c "print('Hi!')"

# And show the data
$data

# Use in expressions
5 + (python -c "print(2 + 3)") + 7

# Create a Python script using a PowerShell here-string, no extension
@"
#!/usr/bin/python3
print('Hi!')
"@ | Out-File -Encoding ascii hi

# Make it executable
chmod +x hi

# Run it - shows that PowerShell really is a shell
./hi

# A more complex script that outputs JSON
cat class1.py

# Run the script
./class1.py

# Capture the data as structured objects (arrays and hashtables)
$data = ./class1.py | ConvertFrom-Json

# look at the first element of the returned array
$data[0]

# Look at the second
$data[1]

# Get a specific element from the data
$data[1].buz[1]

# Finally wrap it all up so it looks like a simple PowerShell command
cat class1.ps1

# And run it, treating the output as structured data.
(./class1)[1].buz[1]

# Finally a PowerShell script with in-line Python
cat inline_python.ps1

# and run it
./inline_python

####################################
# cleanup
rm hi
