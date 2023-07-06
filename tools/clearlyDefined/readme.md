ClearlyDefined

## Purpose

This tool is intended to test if all the license data in [ClearlyDefined](https://clearlydefined.io) is present to generate the PowerShell license.
If the data is not present, it can request that ClearlyDefined gather (called Harvest in their terminology) the data.

## Use

### Testing

Run `./ClearlyDefined.ps1 -test`.

If there is any missing data, the script should write verbose messages about the missing data and throw.
If there is no missing data, the script should not throw.

### Harvesting

Run `./ClearlyDefined.ps1 -Harvest`.
The script will trigger the harvest and output the result from ClearlyDefined.
**Give ClearlyDefined 24 hours to harvest the data.**
You can use the `-Test` switch without the `-Harvest` switch to test if Harvesting is done.

## Caching

If you run in the same PowerShell session, the script will be faster due to caching.

The module will cache any results from ClearlyDefined that indicate the package is Harvested for 60 minutes.
No caching is done for packages that are not yet harvested.
To clear the cache, run with the `-ForceModuleReload` switch.
