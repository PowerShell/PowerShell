// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    #region VERBS

    /// <summary>
    /// Verbs that are commonly used in cmdlet names.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsCommon
    {
        /// <summary>
        /// Synonyms: Add to, append or attach.
        /// </summary>
        public const string Add = "Add";

        /// <summary>
        /// Remove all the elements or content of a container.
        /// </summary>
        public const string Clear = "Clear";

        /// <summary>
        /// Change the state of a resource to make it inaccessible, unavailable, or unusable.
        /// </summary>
        public const string Close = "Close";

        /// <summary>
        /// Copy a resource to another name or another container.
        /// </summary>
        public const string Copy = "Copy";

        /// <summary>
        /// Enters a context.
        /// </summary>
        public const string Enter = "Enter";

        /// <summary>
        /// Exits a context.
        /// </summary>
        public const string Exit = "Exit";

        /// <summary>
        /// Search for an object.
        /// </summary>
        public const string Find = "Find";

        /// <summary>
        /// Formats an object for output.
        /// </summary>
        public const string Format = "Format";

        /// <summary>
        /// Get the contents/object/children/properties/relations/... of a resource.
        /// </summary>
        public const string Get = "Get";

        /// <summary>
        /// Remove from visibility.
        /// </summary>
        public const string Hide = "Hide";

        /// <summary>
        /// Combines resources into one resource.
        /// </summary>
        public const string Join = "Join";

        /// <summary>
        /// Lock a resource.
        /// </summary>
        public const string Lock = "Lock";

        /// <summary>
        /// Move a resource.
        /// </summary>
        public const string Move = "Move";

        /// <summary>
        /// Create a new resource.
        /// </summary>
        public const string New = "New";

        /// <summary>
        /// Change the state of a resource to make it accessible, available, or usable.
        /// </summary>
        public const string Open = "Open";

        /// <summary>
        /// Increases the effectiveness of a resource.
        /// </summary>
        public const string Optimize = "Optimize";

        /// <summary>
        /// To set as the current context, including the ability
        /// to reverse this action.
        /// </summary>
        public const string Push = "Push";

        /// <summary>
        /// To restore a context saved by a Push operation.
        /// </summary>
        public const string Pop = "Pop";

        /// <summary>
        /// Act on a resource again.
        /// </summary>
        public const string Redo = "Redo";

        /// <summary>
        /// Remove a resource from a container.
        /// </summary>
        public const string Remove = "Remove";

        /// <summary>
        /// Give a resource a new name.
        /// </summary>
        public const string Rename = "Rename";

        /// <summary>
        /// Set/reset the contents/object/properties/relations... of a resource.
        /// </summary>
        public const string Reset = "Reset";

        /// <summary>
        /// Changes the size of a resource.
        /// </summary>
        public const string Resize = "Resize";

        /// <summary>
        /// Get a reference to a resource or summary information about a resource by looking in a specified collection.
        /// Does not actually retrieve that resource.
        /// </summary>
        public const string Search = "Search";

        /// <summary>
        /// To take as a choice from among several; pick out.
        /// </summary>
        public const string Select = "Select";

        /// <summary>
        /// Set the contents/object/properties/relations... of a resource.
        /// </summary>
        public const string Set = "Set";

        /// <summary>
        /// Makes visible, or displays information. Combines get, format, and out verbs.
        /// </summary>
        public const string Show = "Show";

        /// <summary>
        /// Pass from one resource or point to another while disregarding or omitting intervening resources or points.
        /// </summary>
        public const string Skip = "Skip";

        /// <summary>
        /// Split an object into portions. parts or fragments.
        /// </summary>
        public const string Split = "Split";

        /// <summary>
        /// Move to the next point or resource.
        /// </summary>
        public const string Step = "Step";

        /// <summary>
        /// </summary>
        public const string Switch = "Switch";

        /// <summary>
        /// Reverse an action or process.
        /// </summary>
        public const string Undo = "Undo";

        /// <summary>
        /// Unlock a resource.
        /// </summary>
        public const string Unlock = "Unlock";

        /// <summary>
        /// Continually inspect a resource for changes.
        /// </summary>
        public const string Watch = "Watch";
    }

    /// <summary>
    /// Verbs that are commonly used in cmdlet names when the cmdlet manipulates data.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsData
    {
        /// <summary>
        /// Backup.
        /// </summary>
        public const string Backup = "Backup";

        /// <summary>
        /// Establish a well defined state to be able to roll back to.
        /// </summary>
        public const string Checkpoint = "Checkpoint";

        /// <summary>
        /// Compare this resource with another one and produce a set of differences.
        /// </summary>
        public const string Compare = "Compare";

        /// <summary>
        /// Reduce in size.
        /// </summary>
        public const string Compress = "Compress";

        /// <summary>
        /// Change from one encoding to another or from one unit base to another (e.g. feet to meters)
        /// </summary>
        public const string Convert = "Convert";

        /// <summary>
        /// Convert from the format named in the noun to a general-purpose format (e.g. string or int).
        /// </summary>
        public const string ConvertFrom = "ConvertFrom";

        /// <summary>
        /// Convert from a general-purpose format (e.g. string or int) to the format named in the noun.
        /// </summary>
        public const string ConvertTo = "ConvertTo";

        /// <summary>
        /// To dismount - to get off. To detach.
        /// </summary>
        public const string Dismount = "Dismount";

        /// <summary>
        /// Performs an in-place modification of a resource.
        /// </summary>
        public const string Edit = "Edit";

        /// <summary>
        /// Uncompress or increase in size.
        /// </summary>
        public const string Expand = "Expand";

        /// <summary>
        /// Make a copy of a set of resources using an interchange format.
        /// </summary>
        public const string Export = "Export";

        /// <summary>
        /// Arrange or associate one or more resources.
        /// </summary>
        public const string Group = "Group";

        /// <summary>
        /// Create a set of resources using an interchange format.
        /// </summary>
        public const string Import = "Import";

        /// <summary>
        /// Prepare a resource for use. Assign a beginning value to something.
        /// </summary>
        public const string Initialize = "Initialize";

        /// <summary>
        /// Limit the consumption of a resource or apply a constraint on a resource.
        /// </summary>
        public const string Limit = "Limit";

        /// <summary>
        /// Take multiple instances and create a single instance.
        /// </summary>
        public const string Merge = "Merge";

        /// <summary>
        /// To mount - to attache a named entity to a hierarchy at the pathname location. To set in position.
        /// </summary>
        public const string Mount = "Mount";

        /// <summary>
        /// Out - direct to a port. Output something to a port.
        /// </summary>
        public const string Out = "Out";

        /// <summary>
        /// Make known and accessible to another.
        /// </summary>
        public const string Publish = "Publish";

        /// <summary>
        /// Rollback state to a predefined snapshot/checkpoint.
        /// </summary>
        public const string Restore = "Restore";

        /// <summary>
        /// Store state in a permanent location.
        /// </summary>
        public const string Save = "Save";

        /// <summary>
        /// Coerce one or more resources to the same state.
        /// </summary>
        public const string Sync = "Sync";

        /// <summary>
        /// Remove from public access and visibility.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unpublish")]
        public const string Unpublish = "Unpublish";

        /// <summary>
        /// Update a resource with new elements or refresh from a source of truth.
        /// </summary>
        public const string Update = "Update";
    }

    /// <summary>
    /// Verbs that are commonly used in cmdlet names when the cmdlet manipulates the lifecycle of something.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsLifecycle
    {
        /// <summary>
        /// Agree to the status of a resource or process.
        /// </summary>
        public const string Approve = "Approve";

        /// <summary>
        /// State or affirm the state of an object.
        /// </summary>
        public const string Assert = "Assert";

        /// <summary>
        /// Creates an artifact (usually a binary or document) out of some set of input files (usually source code or declarative documents)
        /// </summary>
        public const string Build = "Build";

        /// <summary>
        /// Finalize an interruptible activity. Makes pending changes permanent.
        /// </summary>
        public const string Complete = "Complete";

        /// <summary>
        /// Acknowledge, verify, or validate the state of a resource.
        /// </summary>
        public const string Confirm = "Confirm";

        /// <summary>
        /// Refuse, object, block, or oppose the state of a resource or process.
        /// </summary>
        public const string Deny = "Deny";

        /// <summary>
        /// Sends an application, website, or solution to a remote target[s] in such a way that a consumer of that solution can access it after deployment is complete.
        /// </summary>
        public const string Deploy = "Deploy";

        /// <summary>
        /// Stop and/or configure something to be unavailable (e.g unable to not start again)
        /// </summary>
        public const string Disable = "Disable";

        /// <summary>
        /// Configure to be available (e.g. able to start)
        /// </summary>
        public const string Enable = "Enable";

        /// <summary>
        /// Settle in an indicated place or condition (optionally initializing for use)
        /// </summary>
        public const string Install = "Install";

        /// <summary>
        /// Calls or launches an activity that cannot be stopped.
        /// </summary>
        public const string Invoke = "Invoke";

        /// <summary>
        /// Record details about an item in a public store or publishing location
        /// </summary>
        public const string Register = "Register";

        /// <summary>
        /// Ask for a resource or permissions.
        /// </summary>
        public const string Request = "Request";

        /// <summary>
        /// Terminate existing activity and begin it again (with the same configuration)
        /// </summary>
        public const string Restart = "Restart";

        /// <summary>
        /// Begin an activity again after it was suspended.
        /// </summary>
        public const string Resume = "Resume";

        /// <summary>
        /// Begin an activity.
        /// </summary>
        public const string Start = "Start";

        /// <summary>
        ///Discontinue or cease an activity
        /// </summary>
        public const string Stop = "Stop";

        /// <summary>
        /// Present a resource for approval.
        /// </summary>
        public const string Submit = "Submit";

        /// <summary>
        /// Suspend an activity temporarily.
        /// </summary>
        public const string Suspend = "Suspend";

        /// <summary>
        /// Remove or disassociate.
        /// </summary>
        public const string Uninstall = "Uninstall";

        /// <summary>
        /// Remove details of an item from a public store or publishing location
        /// </summary>
        public const string Unregister = "Unregister";

        /// <summary>
        /// Suspend execution until an expected event
        /// </summary>
        public const string Wait = "Wait";
    }

    /// <summary>
    /// Verbs that are commonly used in cmdlet names when the cmdlet is used to diagnose the health of something.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsDiagnostic
    {
        /// <summary>
        /// Iteratively interact with a resource or activity for the purpose finding a flaw or better understanding of what is occurring.
        /// </summary>
        public const string Debug = "Debug";

        /// <summary>
        /// Calculate/identify resources consumed by a specified operation or retrieve statistics about a resource.
        /// </summary>
        public const string Measure = "Measure";

        /// <summary>
        /// Determine whether a resource is alive and responding to requests.
        /// </summary>
        public const string Ping = "Ping";

        /// <summary>
        /// Detect and correct problems.
        /// </summary>
        public const string Repair = "Repair";

        /// <summary>
        /// Map a shorthand name will be bound to a longname.
        /// </summary>
        public const string Resolve = "Resolve";

        /// <summary>
        /// Verify the operational validity or consistency of a resource.
        /// </summary>
        public const string Test = "Test";

        /// <summary>
        /// Trace activities performed by a specified operation.
        /// </summary>
        public const string Trace = "Trace";
    }

    /// <summary>
    /// Verbs that are commonly used in cmdlet names when the cmdlet is used to communicate with something.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsCommunications
    {
        /// <summary>
        /// Associate subsequent activities with a resource.
        /// </summary>
        public const string Connect = "Connect";

        /// <summary>
        /// Disassociate from a resource.
        /// </summary>
        public const string Disconnect = "Disconnect";

        /// <summary>
        /// To read - to obtain (data) from a storage medium or port.
        /// </summary>
        public const string Read = "Read";

        /// <summary>
        /// Take or acquire from a source.
        /// </summary>
        public const string Receive = "Receive";

        /// <summary>
        /// Convey by an intermediary to a destination.
        /// </summary>
        public const string Send = "Send";

        /// <summary>
        /// TO write - communicate or express. Display data.
        /// </summary>
        public const string Write = "Write";
    }

    /// <summary>
    /// Verbs that are commonly used in cmdlet names when the cmdlet is used to secure a resource.
    /// </summary>
    /// <remarks>
    /// These verbs are recommended over their synonyms when used as the verb name
    /// for cmdlets.
    /// </remarks>
    public static class VerbsSecurity
    {
        /// <summary>
        /// Prevent access to or usage of a resource.
        /// </summary>
        public const string Block = "Block";

        /// <summary>
        /// Gives access to a resource.
        /// </summary>
        public const string Grant = "Grant";

        /// <summary>
        /// Guard a resource from attack or loss.
        /// </summary>
        public const string Protect = "Protect";

        /// <summary>
        /// Removes access to a resource.
        /// </summary>
        public const string Revoke = "Revoke";

        /// <summary>
        /// Allow access to or usage of a resource.
        /// </summary>
        public const string Unblock = "Unblock";

        /// <summary>
        /// Remove guards from a resource that prevent it from attack or loss.
        /// </summary>
        public const string Unprotect = "Unprotect";
    }

    /// <summary>
    /// Canonical verbs that don't fit into any of the other categories.
    /// </summary>
    public static class VerbsOther
    {
        /// <summary>
        /// To use or include a resource. To set as the context of an action.
        /// </summary>
        public const string Use = "Use";
    }

    /// <summary>
    /// Verb descriptions.
    /// </summary>
    internal static class VerbDescriptions
    {
        /// <summary>
        /// Gets verb description from the resource file.
        /// </summary>
        public static string GetVerbDescription(string verb)
        {
            return VerbDescriptionStrings.ResourceManager.GetString(verb);
        }
    }

    /// <summary>
    /// Verb Alias prefixes.
    /// </summary>
    internal static class VerbAliasPrefixes
    {
        /// <summary>
        /// "Add" verb alias prefix.
        /// </summary>
        public const string Add = "a";

        /// <summary>
        /// "Approve" verb alias prefix.
        /// </summary>
        public const string Approve = "ap";

        /// <summary>
        /// "Assert" verb alias prefix.
        /// </summary>
        public const string Assert = "as";

        /// <summary>
        /// "Backup" verb alias prefix.
        /// </summary>
        public const string Backup = "ba";

        /// <summary>
        /// "Block" verb alias prefix.
        /// </summary>
        public const string Block = "bl";

        /// <summary>
        /// "Build" verb alias prefix.
        /// </summary>
        public const string Build = "bd";

        /// <summary>
        /// "Checkpoint" verb alias prefix.
        /// </summary>
        public const string Checkpoint = "ch";

        /// <summary>
        /// "Clear" verb alias prefix.
        /// </summary>
        public const string Clear = "cl";

        /// <summary>
        /// "Close" verb alias prefix.
        /// </summary>
        public const string Close = "cs";

        /// <summary>
        /// "Compare" verb alias prefix.
        /// </summary>
        public const string Compare = "cr";

        /// <summary>
        /// "Complete" verb alias prefix.
        /// </summary>
        public const string Complete = "cmp";

        /// <summary>
        /// "Compress" verb alias prefix.
        /// </summary>
        public const string Compress = "cm";

        /// <summary>
        /// "Confirm" verb alias prefix.
        /// </summary>
        public const string Confirm = "cn";

        /// <summary>
        /// "Connect" verb alias prefix.
        /// </summary>
        public const string Connect = "cc";

        /// <summary>
        /// "Convert" verb alias prefix.
        /// </summary>
        public const string Convert = "cv";

        /// <summary>
        /// "ConvertFrom" verb alias prefix.
        /// </summary>
        public const string ConvertFrom = "cf";

        /// <summary>
        /// "ConvertTo" verb alias prefix.
        /// </summary>
        public const string ConvertTo = "ct";

        /// <summary>
        /// "Copy" verb alias prefix.
        /// </summary>
        public const string Copy = "cp";

        /// <summary>
        /// "Debug" verb alias prefix.
        /// </summary>
        public const string Debug = "db";

        /// <summary>
        /// "Deny" verb alias prefix.
        /// </summary>
        public const string Deny = "dn";

        /// <summary>
        /// "Deploy" verb alias prefix.
        /// </summary>
        public const string Deploy = "dp";

        /// <summary>
        /// "Disable" verb alias prefix.
        /// </summary>
        public const string Disable = "d";

        /// <summary>
        /// "Disconnect" verb alias prefix.
        /// </summary>
        public const string Disconnect = "dc";

        /// <summary>
        /// "Dismount" verb alias prefix.
        /// </summary>
        public const string Dismount = "dm";

        /// <summary>
        /// "Edit" verb alias prefix.
        /// </summary>
        public const string Edit = "ed";

        /// <summary>
        /// "Enable" verb alias prefix.
        /// </summary>
        public const string Enable = "e";

        /// <summary>
        /// "Enter" verb alias prefix.
        /// </summary>
        public const string Enter = "et";

        /// <summary>
        /// "Exit" verb alias prefix.
        /// </summary>
        public const string Exit = "ex";

        /// <summary>
        /// "Expand" verb alias prefix.
        /// </summary>
        public const string Expand = "en";

        /// <summary>
        /// "Export" verb alias prefix.
        /// </summary>
        public const string Export = "ep";

        /// <summary>
        /// "Find" verb alias prefix.
        /// </summary>
        public const string Find = "fd";

        /// <summary>
        /// "Format" verb alias prefix.
        /// </summary>
        public const string Format = "f";

        /// <summary>
        /// "Get" verb alias prefix.
        /// </summary>
        public const string Get = "g";

        /// <summary>
        /// "Grant" verb alias prefix.
        /// </summary>
        public const string Grant = "gr";

        /// <summary>
        /// "Group" verb alias prefix.
        /// </summary>
        public const string Group = "gp";

        /// <summary>
        /// "Hide" verb alias prefix.
        /// </summary>
        public const string Hide = "h";

        /// <summary>
        /// "Import" verb alias prefix.
        /// </summary>
        public const string Import = "ip";

        /// <summary>
        /// "Initialize" verb alias prefix.
        /// </summary>
        public const string Initialize = "in";

        /// <summary>
        /// "Install" verb alias prefix.
        /// </summary>
        public const string Install = "is";

        /// <summary>
        /// "Invoke" verb alias prefix.
        /// </summary>
        public const string Invoke = "i";

        /// <summary>
        /// "Join" verb alias prefix.
        /// </summary>
        public const string Join = "j";

        /// <summary>
        /// "Limit" verb alias prefix.
        /// </summary>
        public const string Limit = "l";

        /// <summary>
        /// "Lock" verb alias prefix.
        /// </summary>
        public const string Lock = "lk";

        /// <summary>
        /// "Measure" verb alias prefix.
        /// </summary>
        public const string Measure = "ms";

        /// <summary>
        /// "Merge" verb alias prefix.
        /// </summary>
        public const string Merge = "mg";

        /// <summary>
        /// "Mount" verb alias prefix.
        /// </summary>
        public const string Mount = "mt";

        /// <summary>
        /// "Move" verb alias prefix.
        /// </summary>
        public const string Move = "m";

        /// <summary>
        /// "New" verb alias prefix.
        /// </summary>
        public const string New = "n";

        /// <summary>
        /// "Open" verb alias prefix.
        /// </summary>
        public const string Open = "op";

        /// <summary>
        /// "Optimize" verb alias prefix.
        /// </summary>
        public const string Optimize = "om";

        /// <summary>
        /// "Out" verb alias prefix.
        /// </summary>
        public const string Out = "o";

        /// <summary>
        /// "Ping" verb alias prefix.
        /// </summary>
        public const string Ping = "pi";

        /// <summary>
        /// "Pop" verb alias prefix.
        /// </summary>
        public const string Pop = "pop";

        /// <summary>
        /// "Protect" verb alias prefix.
        /// </summary>
        public const string Protect = "pt";

        /// <summary>
        /// "Publish" verb alias prefix.
        /// </summary>
        public const string Publish = "pb";

        /// <summary>
        /// "Push" verb alias prefix.
        /// </summary>
        public const string Push = "pu";

        /// <summary>
        /// "Read" verb alias prefix.
        /// </summary>
        public const string Read = "rd";

        /// <summary>
        /// "Receive" verb alias prefix.
        /// </summary>
        public const string Receive = "rc";

        /// <summary>
        /// "Redo" verb alias prefix.
        /// </summary>
        public const string Redo = "re";

        /// <summary>
        /// "Register" verb alias prefix.
        /// </summary>
        public const string Register = "rg";

        /// <summary>
        /// "Remove" verb alias prefix.
        /// </summary>
        public const string Remove = "r";

        /// <summary>
        /// "Rename" verb alias prefix.
        /// </summary>
        public const string Rename = "rn";

        /// <summary>
        /// "Repair" verb alias prefix.
        /// </summary>
        public const string Repair = "rp";

        /// <summary>
        /// "Request" verb alias prefix.
        /// </summary>
        public const string Request = "rq";

        /// <summary>
        /// "Reset" verb alias prefix.
        /// </summary>
        public const string Reset = "rs";

        /// <summary>
        /// "Resize" verb alias prefix.
        /// </summary>
        public const string Resize = "rz";

        /// <summary>
        /// "Resolve" verb alias prefix.
        /// </summary>
        public const string Resolve = "rv";

        /// <summary>
        /// "Restart" verb alias prefix.
        /// </summary>
        public const string Restart = "rt";

        /// <summary>
        /// "Restore" verb alias prefix.
        /// </summary>
        public const string Restore = "rr";

        /// <summary>
        /// "Resume" verb alias prefix.
        /// </summary>
        public const string Resume = "ru";

        /// <summary>
        /// "Revoke" verb alias prefix.
        /// </summary>
        public const string Revoke = "rk";

        /// <summary>
        /// "Save" verb alias prefix.
        /// </summary>
        public const string Save = "sv";

        /// <summary>
        /// "Search" verb alias prefix.
        /// </summary>
        public const string Search = "sr";

        /// <summary>
        /// "Select" verb alias prefix.
        /// </summary>
        public const string Select = "sc";

        /// <summary>
        /// "Send" verb alias prefix.
        /// </summary>
        public const string Send = "sd";

        /// <summary>
        /// "Set" verb alias prefix.
        /// </summary>
        public const string Set = "s";

        /// <summary>
        /// "Show" verb alias prefix.
        /// </summary>
        public const string Show = "sh";

        /// <summary>
        /// "Sync" verb alias prefix.
        /// </summary>
        public const string Sync = "sy";

        /// <summary>
        /// "Skip" verb alias prefix.
        /// </summary>
        public const string Skip = "sk";

        /// <summary>
        /// "Split" verb alias prefix.
        /// </summary>
        public const string Split = "sl";

        /// <summary>
        /// "Start" verb alias prefix.
        /// </summary>
        public const string Start = "sa";

        /// <summary>
        /// "Step" verb alias prefix.
        /// </summary>
        public const string Step = "st";

        /// <summary>
        /// "Stop" verb alias prefix.
        /// </summary>
        public const string Stop = "sp";

        /// <summary>
        /// "Submit" verb alias prefix.
        /// </summary>
        public const string Submit = "sb";

        /// <summary>
        /// "Suspend" verb alias prefix.
        /// </summary>
        public const string Suspend = "ss";

        /// <summary>
        /// "Switch" verb alias prefix.
        /// </summary>
        public const string Switch = "sw";

        /// <summary>
        /// "Test" verb alias prefix.
        /// </summary>
        public const string Test = "t";

        /// <summary>
        /// "Trace" verb alias prefix.
        /// </summary>
        public const string Trace = "tr";

        /// <summary>
        /// "Unblock" verb alias prefix.
        /// </summary>
        public const string Unblock = "ul";

        /// <summary>
        /// "Undo" verb alias prefix.
        /// </summary>
        public const string Undo = "un";

        /// <summary>
        /// "Uninstall" verb alias prefix.
        /// </summary>
        public const string Uninstall = "us";

        /// <summary>
        /// "Unlock" verb alias prefix.
        /// </summary>
        public const string Unlock = "uk";

        /// <summary>
        /// "Unprotect" verb alias prefix.
        /// </summary>
        public const string Unprotect = "up";

        /// <summary>
        /// "Unpublish" verb alias prefix.
        /// </summary>
        public const string Unpublish = "ub";

        /// <summary>
        /// "Unregister" verb alias prefix.
        /// </summary>
        public const string Unregister = "ur";

        /// <summary>
        /// "Update" verb alias prefix.
        /// </summary>
        public const string Update = "ud";

        /// <summary>
        /// "Use" verb alias prefix.
        /// </summary>
        public const string Use = "u";

        /// <summary>
        /// "Wait" verb alias prefix.
        /// </summary>
        public const string Wait = "w";

        /// <summary>
        /// "Watch" verb alias prefix.
        /// </summary>
        public const string Watch = "wc";

        /// <summary>
        /// "Write" verb alias prefix.
        /// </summary>
        public const string Write = "wr";

        /// <summary>
        /// Gets verb prefix.
        /// </summary>
        public static string GetVerbAliasPrefix(string verb)
        {
            FieldInfo aliasField = typeof(VerbAliasPrefixes).GetField(verb);
            if (aliasField != null)
            {
                return (string)aliasField.GetValue(null);
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Provides information about a verb used to name commands defined in PowerShell.
    /// </summary>
    public class VerbInfo
    {
        /// <summary>
        /// The verb name, used to begin command names.
        /// </summary>
        public string Verb
        {
            get; set;
        }

        /// <summary>
        /// The alias prefix, recommended for aliases to commands that begin with this verb.
        /// </summary>
        public string AliasPrefix
        {
            get; set;
        }

        /// <summary>
        /// The name of the functional category of commands that begin with this verb.
        /// </summary>
        public string Group
        {
            get; set;
        }

        /// <summary>
        /// Explains what the verb is meant to do with its object.
        /// </summary>
        public string Description
        {
            get; set;
        }
    }

    internal static class Verbs
    {
        static Verbs()
        {
            foreach (Type type in VerbTypes)
            {
                foreach (FieldInfo field in type.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        s_validVerbs.Add((string)field.GetValue(null), true);
                    }
                }
            }

            s_recommendedAlternateVerbs.Add("accept", new string[] { "Receive" });
            s_recommendedAlternateVerbs.Add("acquire", new string[] { "Get", "Read" });
            s_recommendedAlternateVerbs.Add("allocate", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("allow", new string[] { "Enable", "Grant", "Unblock" });
            s_recommendedAlternateVerbs.Add("amend", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("analyze", new string[] { "Measure", "Test" });
            s_recommendedAlternateVerbs.Add("append", new string[] { "Add" });
            s_recommendedAlternateVerbs.Add("assign", new string[] { "Set" });
            s_recommendedAlternateVerbs.Add("associate", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("attach", new string[] { "Add", "Debug" });
            s_recommendedAlternateVerbs.Add("bc", new string[] { "Compare" });
            s_recommendedAlternateVerbs.Add("boot", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("break", new string[] { "Disconnect" });
            s_recommendedAlternateVerbs.Add("broadcast", new string[] { "Send" });
            s_recommendedAlternateVerbs.Add("burn", new string[] { "Backup" });
            s_recommendedAlternateVerbs.Add("calculate", new string[] { "Measure" });
            s_recommendedAlternateVerbs.Add("cancel", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("cat", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("change", new string[] { "Convert", "Edit", "Rename" });
            s_recommendedAlternateVerbs.Add("clean", new string[] { "Uninstall" });
            s_recommendedAlternateVerbs.Add("clone", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("combine", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("compact", new string[] { "Compress" });
            s_recommendedAlternateVerbs.Add("compile", new string[] { "Build" });
            s_recommendedAlternateVerbs.Add("concatenate", new string[] { "Add" });
            s_recommendedAlternateVerbs.Add("configure", new string[] { "Set" });
            s_recommendedAlternateVerbs.Add("create", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("cut", new string[] { "Remove" });
            // recommendedAlternateVerbs.Add("debug",      new string[] {"Ping"});
            s_recommendedAlternateVerbs.Add("delete", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("detach", new string[] { "Dismount", "Remove" });
            s_recommendedAlternateVerbs.Add("determine", new string[] { "Measure", "Resolve" });
            s_recommendedAlternateVerbs.Add("diagnose", new string[] { "Debug", "Test" });
            s_recommendedAlternateVerbs.Add("diff", new string[] { "Checkpoint", "Compare" });
            s_recommendedAlternateVerbs.Add("difference", new string[] { "Checkpoint", "Compare" });
            s_recommendedAlternateVerbs.Add("dig", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("dir", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("discard", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("display", new string[] { "Show", "Write" });
            s_recommendedAlternateVerbs.Add("dispose", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("divide", new string[] { "Split" });
            s_recommendedAlternateVerbs.Add("dump", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("duplicate", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("empty", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("end", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("erase", new string[] { "Clear", "Remove" });
            s_recommendedAlternateVerbs.Add("examine", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("execute", new string[] { "Invoke" });
            s_recommendedAlternateVerbs.Add("explode", new string[] { "Expand" });
            s_recommendedAlternateVerbs.Add("extract", new string[] { "Export" });
            s_recommendedAlternateVerbs.Add("fix", new string[] { "Repair", "Restore" });
            s_recommendedAlternateVerbs.Add("flush", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("follow", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("generate", new string[] { "New" });
            // recommendedAlternateVerbs.Add("get",        new string[] {"Read"});
            s_recommendedAlternateVerbs.Add("halt", new string[] { "Disable" });
            s_recommendedAlternateVerbs.Add("in", new string[] { "ConvertTo" });
            s_recommendedAlternateVerbs.Add("index", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("initiate", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("input", new string[] { "ConvertTo", "Unregister" });
            s_recommendedAlternateVerbs.Add("insert", new string[] { "Add", "Unregister" });
            s_recommendedAlternateVerbs.Add("inspect", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("kill", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("launch", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("load", new string[] { "Import" });
            s_recommendedAlternateVerbs.Add("locate", new string[] { "Search", "Select" });
            s_recommendedAlternateVerbs.Add("logoff", new string[] { "Disconnect" });
            s_recommendedAlternateVerbs.Add("mail", new string[] { "Send" });
            s_recommendedAlternateVerbs.Add("make", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("match", new string[] { "Select" });
            s_recommendedAlternateVerbs.Add("migrate", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("modify", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("name", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("nullify", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("obtain", new string[] { "Get" });
            // recommendedAlternateVerbs.Add("out",        new string[] {"ConvertFrom"});
            s_recommendedAlternateVerbs.Add("output", new string[] { "ConvertFrom" });
            s_recommendedAlternateVerbs.Add("pause", new string[] { "Suspend", "Wait" });
            s_recommendedAlternateVerbs.Add("peek", new string[] { "Receive" });
            s_recommendedAlternateVerbs.Add("permit", new string[] { "Enable" });
            s_recommendedAlternateVerbs.Add("purge", new string[] { "Clear", "Remove" });
            s_recommendedAlternateVerbs.Add("pick", new string[] { "Select" });
            // recommendedAlternateVerbs.Add("pop",        new string[] {"Enter", "Exit"});
            s_recommendedAlternateVerbs.Add("prevent", new string[] { "Block" });
            s_recommendedAlternateVerbs.Add("print", new string[] { "Write" });
            s_recommendedAlternateVerbs.Add("prompt", new string[] { "Read" });
            // recommendedAlternateVerbs.Add("push",       new string[] {"Enter", "Exit"});
            s_recommendedAlternateVerbs.Add("put", new string[] { "Send", "Write" });
            s_recommendedAlternateVerbs.Add("puts", new string[] { "Write" });
            s_recommendedAlternateVerbs.Add("quota", new string[] { "Limit" });
            s_recommendedAlternateVerbs.Add("quote", new string[] { "Limit" });
            s_recommendedAlternateVerbs.Add("rebuild", new string[] { "Initialize" });
            s_recommendedAlternateVerbs.Add("recycle", new string[] { "Restart" });
            s_recommendedAlternateVerbs.Add("refresh", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("reinitialize", new string[] { "Initialize" });
            s_recommendedAlternateVerbs.Add("release", new string[] { "Clear", "Install", "Publish", "Unlock" });
            s_recommendedAlternateVerbs.Add("reload", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("renew", new string[] { "Initialize", "Update" });
            s_recommendedAlternateVerbs.Add("replicate", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("resample", new string[] { "Convert" });
            // recommendedAlternateVerbs.Add("reset",      new string[] {"Set"});
            // recommendedAlternateVerbs.Add("resize",     new string[] {"Convert"});
            s_recommendedAlternateVerbs.Add("restrict", new string[] { "Lock" });
            s_recommendedAlternateVerbs.Add("return", new string[] { "Repair", "Restore" });
            s_recommendedAlternateVerbs.Add("revert", new string[] { "Unpublish" });
            s_recommendedAlternateVerbs.Add("revise", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("run", new string[] { "Invoke", "Start" });
            s_recommendedAlternateVerbs.Add("salvage", new string[] { "Test" });
            // recommendedAlternateVerbs.Add("save",       new string[] {"Backup"});
            s_recommendedAlternateVerbs.Add("secure", new string[] { "Lock" });
            s_recommendedAlternateVerbs.Add("separate", new string[] { "Split" });
            s_recommendedAlternateVerbs.Add("setup", new string[] { "Initialize", "Install" });
            s_recommendedAlternateVerbs.Add("sleep", new string[] { "Suspend", "Wait" });
            s_recommendedAlternateVerbs.Add("starttransaction", new string[] { "Checkpoint" });
            s_recommendedAlternateVerbs.Add("telnet", new string[] { "Connect" });
            s_recommendedAlternateVerbs.Add("terminate", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("track", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("transfer", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("type", new string[] { "Get" });
            // recommendedAlternateVerbs.Add("undo",       new string[] {"Repair", "Restore"});
            s_recommendedAlternateVerbs.Add("unite", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("unlink", new string[] { "Dismount" });
            s_recommendedAlternateVerbs.Add("unmark", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("unrestrict", new string[] { "Unlock" });
            s_recommendedAlternateVerbs.Add("unsecure", new string[] { "Unlock" });
            s_recommendedAlternateVerbs.Add("unset", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("verify", new string[] { "Test" });

#if DEBUG
            foreach (KeyValuePair<string, string[]> entry in s_recommendedAlternateVerbs)
            {
                Dbg.Assert(!IsStandard(entry.Key), "prohibited verb is standard");
                foreach (string suggested in entry.Value)
                {
                    Dbg.Assert(IsStandard(suggested), "suggested verb is not standard");
                }
            }
#endif
        }

        /// <summary>
        /// Gets all verb types.
        /// </summary>
        /// <value>List of all verb types.</value>
        private static Type[] VerbTypes => new Type[] {
            typeof(VerbsCommon),
            typeof(VerbsCommunications),
            typeof(VerbsData),
            typeof(VerbsDiagnostic),
            typeof(VerbsLifecycle),
            typeof(VerbsOther),
            typeof(VerbsSecurity)
        };

        /// <summary>
        /// Gets verb group display name from type.
        /// </summary>
        /// <param name="verbType">The verb type.</param>
        /// <returns>Verb group display name.</returns>
        private static string GetVerbGroupDisplayName(Type verbType) => verbType.Name.Substring(5);

        /// <summary>
        /// Filters by verbs and commands.
        /// </summary>
        /// <param name="verbs">The array of verbs.</param>
        /// <param name="commands">The collection of commands.</param>
        /// <returns>List of Verbs.</returns>
        private static IEnumerable<string> FilterByVerbsAndCommands(string[] verbs, Collection<CmdletInfo> commands)
        {
            if (commands is null || commands.Count == 0)
            {
                yield break;
            }

            Collection<WildcardPattern> verbPatterns = SessionStateUtilities.CreateWildcardsFromStrings(
                verbs,
                WildcardOptions.IgnoreCase);

            foreach (CmdletInfo command in commands)
            {
                if (SessionStateUtilities.MatchesAnyWildcardPattern(
                    command.Verb,
                    verbPatterns,
                    defaultValue: false))
                {
                    yield return command.Verb;
                }
            }
        }

        /// <summary>
        /// Filters by verbs and groups.
        /// </summary>
        /// <param name="verbs">The array of verbs.</param>
        /// <param name="groups">The array of groups.</param>
        /// <returns>List of Verbs.</returns>
        internal static IEnumerable<VerbInfo> FilterByVerbsAndGroups(string[] verbs, string[] groups)
        {
            if (groups is null || groups.Length == 0)
            {
                foreach (Type verbType in VerbTypes)
                {
                    foreach (VerbInfo verb in FilterVerbsByType(verbs, verbType))
                    {
                        yield return verb;
                    }
                }

                yield break;
            }

            foreach (Type verbType in VerbTypes)
            {
                if (SessionStateUtilities.CollectionContainsValue(
                    groups,
                    GetVerbGroupDisplayName(verbType),
                    StringComparer.OrdinalIgnoreCase))
                {
                    foreach (VerbInfo verb in FilterVerbsByType(verbs, verbType))
                    {
                        yield return verb;
                    }
                }
            }
        }

        /// <summary>
        /// Filters verbs by type.
        /// </summary>
        /// <param name="verbs">The array of verbs.</param>
        /// <param name="verbType">The verb type.</param>
        /// <returns>List of Verbs.</returns>
        private static IEnumerable<VerbInfo> FilterVerbsByType(string[] verbs, Type verbType)
        {
            if (verbs is null || verbs.Length == 0)
            {
                foreach (FieldInfo field in verbType.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        yield return CreateVerbFromField(field, verbType);
                    }
                }

                yield break;
            }

            Collection<WildcardPattern> verbPatterns = SessionStateUtilities.CreateWildcardsFromStrings(
                verbs,
                WildcardOptions.IgnoreCase);

            foreach (FieldInfo field in verbType.GetFields())
            {
                if (field.IsLiteral)
                {
                    if (SessionStateUtilities.MatchesAnyWildcardPattern(
                        field.Name,
                        verbPatterns,
                        defaultValue: false))
                    {
                        yield return CreateVerbFromField(field, verbType);
                    }
                }
            }
        }

        /// <summary>
        /// Creates Verb info object from field info.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="verbType">The verb type.</param>
        /// <returns>VerbInfo object.</returns>
        private static VerbInfo CreateVerbFromField(FieldInfo field, Type verbType) => new()
        {
            Verb = field.Name,
            AliasPrefix = VerbAliasPrefixes.GetVerbAliasPrefix(field.Name),
            Group = GetVerbGroupDisplayName(verbType),
            Description = VerbDescriptions.GetVerbDescription(field.Name)
        };

        /// <summary>
        /// Provides argument completion for Verb parameter.
        /// </summary>
        public class VerbArgumentCompleter : IArgumentCompleter
        {
            /// <summary>
            /// Returns completion results for verb parameter.
            /// </summary>
            /// <param name="commandName">The command name.</param>
            /// <param name="parameterName">The parameter name.</param>
            /// <param name="wordToComplete">The word to complete.</param>
            /// <param name="commandAst">The command AST.</param>
            /// <param name="fakeBoundParameters">The fake bound parameters.</param>
            /// <returns>List of Completion Results.</returns>
            public IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                var verbs = new string[] { wordToComplete + "*" };

                // Completion: Get-Verb -Group <group> -Verb <wordToComplete>
                if (commandName.Equals("Get-Verb", StringComparison.OrdinalIgnoreCase)
                    && fakeBoundParameters.Contains("Group"))
                {
                    string[] groups = null;

                    object groupParameterValue = fakeBoundParameters["Group"];
                    Type groupParameterValueType = groupParameterValue.GetType();

                    if (groupParameterValueType == typeof(string))
                    {
                        groups = new string[] { groupParameterValue.ToString() };
                    }

                    else if (groupParameterValueType.IsArray
                             && groupParameterValueType.GetElementType() == typeof(object))
                    {
                        groups = Array.ConvertAll((object[])groupParameterValue, group => group.ToString());
                    }

                    foreach (VerbInfo verb in FilterByVerbsAndGroups(verbs, groups))
                    {
                        yield return new CompletionResult(verb.Verb);
                    }

                    yield break;
                }

                // Completion: Get-Command -Noun <noun> -Verb <wordToComplete>
                else if (commandName.Equals("Get-Command", StringComparison.OrdinalIgnoreCase)
                         && fakeBoundParameters.Contains("Noun"))
                {
                    using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

                    var commandInfo = new CmdletInfo("Get-Command", typeof(GetCommandCommand));

                    ps.AddCommand(commandInfo);
                    ps.AddParameter("Noun", fakeBoundParameters["Noun"]);

                    if (fakeBoundParameters.Contains("Module"))
                    {
                        ps.AddParameter("Module", fakeBoundParameters["Module"]);
                    }

                    Collection<CmdletInfo> commands = ps.Invoke<CmdletInfo>();

                    foreach (string verb in FilterByVerbsAndCommands(verbs, commands))
                    {
                        yield return new CompletionResult(verb);
                    }

                    yield break;
                }

                // Complete all verbs by default if above cases not completed
                foreach (Type verbType in VerbTypes)
                {
                    foreach (VerbInfo verb in FilterVerbsByType(verbs, verbType))
                    {
                        yield return new CompletionResult(verb.Verb);
                    }
                }
            }
        }

        private static readonly Dictionary<string, bool> s_validVerbs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string[]> s_recommendedAlternateVerbs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsStandard(string verb)
        {
            return s_validVerbs.ContainsKey(verb);
        }

        internal static string[] SuggestedAlternates(string verb)
        {
            string[] result = null;
            s_recommendedAlternateVerbs.TryGetValue(verb, out result);
            return result;
        }
    }
    #endregion VERBS
}
