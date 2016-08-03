#
# Wrap python script in such a way to make it easy to
# consume from powershell
#
# The variable $PSScriptRoot points to the directory
# from which the script was executed. This allows
# picking up the python script from the same directory
#

& $PSScriptRoot/class1.py | ConvertFrom-JSON

