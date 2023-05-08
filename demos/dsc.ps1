# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# DSC MOF Compilation
# DSC Configuration() script that:
# Defines base configuration users, groups, settings
# Uses PS function to set package configuration (ensure=Present) for an array of packages
# Probes for the existence of a package (Apache or MySQL) and conditionally configures the workload. I.e., if Apache is installed, configure Apache settings

# Demo execution:
# Show the .ps1
# Run the .ps1 to generate a MOF
# Apply the MOF locally with Start-DSCConfiguration
# Show the newly configured state
