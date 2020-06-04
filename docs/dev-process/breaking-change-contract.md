# Breaking Changes

We have a serious commitment to backwards compatibility with all earlier versions of PowerShell – including the language, cmdlets, APIs and the various protocols (e.g. PowerShell Remoting Protocol) and data formats (e.g. cdxml).

Below is a summary of our approach to handing breaking changes including what kinds of things constitute breaking changes, how we categorize them, and how we decide what we're willing to take.

Note that these rules only apply to existing stable features that have shipped in a supported release. New features marked as “in preview” that are still under development may be modified from one preview release to the next.
These are **not** considered breaking changes.

To help triage breaking changes, we classify them in to four buckets:

1. Public Contract
1. Reasonable Grey Area
1. Unlikely Grey Area
1. Clearly Non-Public

## Bucket 1: Public Contract

Any change that is a clear violation of the public contract.

### Unacceptable changes

+ A code change that results in a change to the existing behavior observed for a given input with an API, protocol or the PowerShell language.
+ Renaming or removing a public type, type member, or type parameter; renaming or removing a cmdlet or cmdlet parameter (note: it is possible to rename a cmdlet parameter if a parameter alias is added.

This is an acceptable solution for PowerShell scripts but may break .NET code that depends on the name of the original member on the cmdlet object type.)

+ Decreasing the range of accepted values within a given parameter.
+ Changing the value of a public constant or enum member; changing the type of a cmdlet parameter to a more restrictive type.
    + Example: A cmdlet with a parameter -p1 that was previously type as [object] cannot be changed to be or a more restrictive type such as [int].
+ Making an incompatible change to any protocol without increasing the protocol version number.

### Acceptable changes

+ Any existing behavior that results in an error message generally may be changed to provide new functionality.
+ A new instance field is added to a type (this impacts .NET serialization but not PowerShell serialization and so is considered acceptable.)
+ Adding new types, new type members and new cmdlets
+ Making changes to the protocols with a protocol version increment. Older versions of the protocol would still need to be maintained to allow communication with earlier systems. This would require that protocol negotiation take place between the two systems. In addition to any protocol code changes, the Microsoft Open Specification program requires that the formal protocol specification for a protocol be updated in a timely manner.  An example of a MS protocol specification document (MS-PSRP) can be found at: https://msdn.microsoft.com/library/mt242417.aspx

## Bucket 2: Reasonable Grey Area

Change of behavior that customers would have reasonably depended on.

Examples:

+ Change in timing/order of events (even when not specified in docs)
    + Example: PowerShell events are handled by interleaving their execution with the execution of the main pipeline thread. Where and when this interleaving occurs might change. This order of execution is not specified but is deterministic so changing it might break some scripts.
+ Change in parsing of input and throwing new errors (even if parsing behavior is not specified in the docs)
    + Example: a script may be using a JSON parser that is forgiving to minor syntactic errors in the JSON text. Changing that parser to be more rigorous in its processing would result in errors being thrown when no error was generated in the past thus breaking scripts.

Judiciously making changes in these type of features require judgement: how predictable, obvious, consistent was the behavior?
In general, a significant external preview of the change would need to be done also possibly requiring an RFC to be created to allow for community input on the proposal.

## Bucket 3: Unlikely Grey Area

Change of behavior that customers could have depended on, but probably wouldn't.

Examples:

+ correcting behavior in a subtle corner case that has no obvious utility.

    + Example: the existing behavior of the PowerShell cd called without arguments is to do nothing. Changing this behavior to be consistent with UNIX shells which typically set the CWD to the user’s home directory

    + Example: changes to formatting for an object type. We have always considered the output formatting of an object to be a user experience issue and thus open for change. Since PowerShell pipes objects not text, changes to the way an object is rendered to text is generally considered to be allowed.

As with type 2 changes, these require judgement: what is reasonable and what’s not?

## Bucket 4: Clearly Non-Public

Changes to surface area or behavior that is clearly internal or non-breaking in theory, but breaks an app.

Examples:

+ Changes to internal APIs that break private reflection.
+ Changes to APIs in the `System.Management.Automation.Internal` namespace (even if they are public, they are still considered internal and subject to change).
+ Renaming a parameter set (see related discussion [here](https://github.com/PowerShell/PowerShell/issues/10058)).

It is impossible to evolve a code base without making such changes, so we don't require up-front approval for these, but we will sometimes have to go back and
revisit such change if there's too much pain inflicted on the ecosystem through a popular app or library.

## What This Means for Contributors

+ All bucket 1, 2, and 3 breaking changes require contacting team at @powershell/powershell.
+ If you're not sure into which bucket a given change falls, contact us as well.
+ It doesn't matter if the existing behavior is "wrong", we still need to think through the implications. PowerShell has been in broad use for over 10 years so we be must be extremely sensitive to breaking existing users and scripts.
+ If a change is deemed too breaking, we can help identify alternatives such as introducing a new API or cmdlet and obsoleting the old one.

Request for clarifications or suggested alterations to this document should be done by opening issues against this document.
