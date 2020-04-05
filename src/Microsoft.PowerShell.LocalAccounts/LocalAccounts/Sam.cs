// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation.SecurityAccountsManager.Extensions;
using System.Management.Automation.SecurityAccountsManager.Native;
using System.Management.Automation.SecurityAccountsManager.Native.NtSam;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

using Microsoft.PowerShell.Commands;
using Microsoft.PowerShell.LocalAccounts;

[module: SuppressMessage("Microsoft.Design", "CA1014:MarkAssembliesWithClsCompliant")]

namespace System.Management.Automation.SecurityAccountsManager
{
    /// <summary>
    /// Provides methods for manipulating local Users and Groups.
    /// </summary>
    internal class Sam : IDisposable
    {
#region Enums
         /// <summary>
        /// The operation under way. Used in the <see cref="Context"/> class.
        /// </summary>
        private enum ContextOperation
        {
            New = 1,
            Enable,
            Disable,
            Get,
            Remove,
            Rename,
            Set,
            AddMember,
            GetMember,
            RemoveMember
        }

        /// <summary>
        /// The type of object currently operating with.
        /// used in the <see cref="Context"/> class.
        /// </summary>
        private enum ContextObjectType
        {
            User = 1,
            Group
        }
#endregion Enums

#region Internal Classes
        /// <summary>
        /// Holds information about the underway operation.
        /// </summary>
        /// <remarks>
        /// Used primarily by the private ThrowOnFailure method when building
        /// Exception objects to throw.
        /// </remarks>
        private class Context
        {
            public ContextOperation operation;
            public ContextObjectType type;
            public object target;
            public string objectId;
            public string memberId;

            /// <summary>
            /// Initialize a new Context object.
            /// </summary>
            /// <param name="operation">
            /// One of the <see cref="ContextOperation"/> enumerations indicating
            /// the type of operation under way.
            /// </param>
            /// <param name="objectType">
            /// One of the <see cref="ContextObjectType"/> enumerations indicating
            /// the type of object (user or group) being used.
            /// </param>
            /// <param name="objectIdentifier">
            /// A string containing the name of the object. This may be either a
            /// user/group name or a string representation of a SID.
            /// </param>
            /// <param name="target">
            /// The target being operated on.
            /// </param>
            /// <param name="memberIdentifier">
            /// A string containing the name of the member being added or removed
            /// from a group. Used only in such cases.
            /// </param>
            public Context(ContextOperation operation,
                           ContextObjectType objectType,
                           string objectIdentifier,
                           object target,
                           string memberIdentifier = null)
            {
                this.operation = operation;
                this.type = objectType;
                this.objectId = objectIdentifier;
                this.target = target;
                this.memberId = memberIdentifier;
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            public Context()
            {
            }
            /// <summary>
            /// Gets a string containing the type of operation under way.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string OperationName
            {
                get { return operation.ToString(); }
            }

            /// <summary>
            /// Gets a string containing the type of object ("User" or "Group")
            /// being used.
            /// </summary>
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
            public string TypeNamne
            {
                get { return type.ToString(); }
            }

            /// <summary>
            /// Gets a string containing the name of the object being used.
            /// </summary>
            public string ObjectName
            {
                get { return objectId; }
            }

            /// <summary>
            /// Gets a string containing the name of the member being added to
            /// or removed from a group. Returns null if the operation does not
            /// involve group members.
            /// </summary>
            public string MemberName
            {
                get { return memberId; }
            }
        }

        /// <summary>
        /// Contains basic information about an Account.
        /// </summary>
        /// <remarks>
        /// AccountInfo is the return type from the private
        /// LookupAccountInfo method.
        /// </remarks>
        private class AccountInfo
        {
            public string AccountName;
            public string DomainName;
            public SecurityIdentifier Sid;
            public Native.SID_NAME_USE Use;

            public override string ToString()
            {
                if (!string.IsNullOrEmpty(DomainName))
                    return DomainName + '\\' + AccountName;
                else
                    return AccountName;
            }
        }
#endregion Internal Classes

#region Instance Data
        private Context context = null;
        private string machineName = string.Empty;
        private static readonly PrincipalContext s_ctx = new PrincipalContext(ContextType.Machine);
#endregion Instance Data

#region Construction
        internal Sam()
        {
            // CoreCLR does not have Environment.MachineName,
            // so we'll use this instead.
            machineName = System.Net.Dns.GetHostName();
        }
#endregion Construction

#region Public (Internal) Methods
        public string StripMachineName(string name)
        {
            var mn = machineName + '\\';

            if (name.StartsWith(mn, StringComparison.CurrentCultureIgnoreCase))
                return name.Substring(mn.Length);

            return name;
        }
#region Local Groups
        /// <summary>
        /// Retrieve a named local group.
        /// </summary>
        /// <param name="groupName">Name of the desired local group.</param>
        /// <returns>
        /// A <see cref="LocalGroup"/> object containing information about
        /// the local group.
        /// </returns>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the named group cannot be found.
        /// </exception>
        internal LocalGroup GetLocalGroup(string groupName)
        {
            // TODO: groupName == null - exception?

            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = groupName
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, groupName);

            if (groupPrincipal is null)
            {
                throw new GroupNotFoundException(groupName, groupName);
            }

            return MakeLocalGroupObject(groupPrincipal);
        }

        /// <summary>
        /// Retrieve a local group by SID.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the desired group.
        /// </param>
        /// <returns>
        /// A <see cref="LocalGroup"/> object containing information about
        /// the local group.
        /// </returns>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the specified group cannot be found.
        /// </exception>
        internal LocalGroup GetLocalGroup(SecurityIdentifier sid)
        {
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                return GetLocalGroup(account.Value);
            }
            catch (IdentityNotMappedException exc)
            {
                throw new GroupNotFoundException(exc, sid.ToString());
            }
        }

        /// <summary>
        /// Create a local group.
        /// </summary>
        /// <param name="group">A <see cref="LocalGroup"/> object containing
        /// information about the local group to be created.
        /// </param>
        /// <returns>
        /// A new LocalGroup object containing information about the newly
        /// created local group.
        /// </returns>
        /// <exception cref="GroupExistsException">
        /// Thrown when an attempt is made to create a local group that already
        /// exists.
        /// </exception>
        internal LocalGroup CreateLocalGroup(LocalGroup group)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPrincipal = new GroupPrincipal(s_ctx)
            {
                Name = group.Name,
                Description = group.Description,
            };

            try
            {
                groupPrincipal.Save();
            }
            catch (PrincipalOperationException exc)
            {
                if (exc.ErrorCode == -2147023517)
                {
                    // COMException (0x80070563): The specified local group already exists.
                    throw new GroupExistsException(exc, group.Name);
                }

                if (exc.ErrorCode == -2147022694)
                {
                    // COMException (‭0x8007089A‬): The specified username is invalid.
                    throw new InvalidNameException(exc, group.Name);
                }

                throw;
            }

            return MakeLocalGroupObject(groupPrincipal);
        }

        /// <summary>
        /// Update a local group with new property values.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object representing the group to be updated.
        /// </param>
        /// <param name="changed">
        /// A LocalGroup object containing the desired changes.
        /// </param>
        /// <remarks>
        /// Currently, a group's description is the only changeable property.
        /// </remarks>
        internal void UpdateLocalGroup(LocalGroup group, LocalGroup changed)
        {
            // Only description may be changed
            if (group.Description == changed.Description)
            {
                return;
            }

            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = group.Name
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, group.Name);

            if (groupPrincipal is null)
            {
                throw new GroupNotFoundException(group.Name, group.Name);
            }

            groupPrincipal.Description = changed.Description;

            groupPrincipal.Save();
        }

        /// <summary>
        /// Remove a local group.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the
        /// local group to be removed.
        /// </param>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the specified group cannot be found.
        /// </exception>
        internal void RemoveLocalGroup(SecurityIdentifier sid)
        {
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                RemoveLocalGroup(account.Value);
            }
            catch (IdentityNotMappedException exc)
            {
                throw new GroupNotFoundException(exc, sid.ToString());
            }
        }

        /// <summary>
        /// Remove a local group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object containing
        /// information about the local group to be removed.
        /// </param>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the specified group cannot be found.
        /// </exception>
        internal void RemoveLocalGroup(LocalGroup group)
        {
            SecurityIdentifier sid = group.SID;
            if (sid is null)
            {
                RemoveLocalGroup(group.Name);
                return;
            }

            RemoveLocalGroup(sid);
        }

        private void RemoveLocalGroup(string groupName)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = groupName
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, groupName);

            if (groupPrincipal is null)
            {
                throw new GroupNotFoundException(groupName, groupName);
            }

            groupPrincipal.Delete();
        }

        /// <summary>
        /// Rename a local group.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying
        /// the local group to be renamed.
        /// </param>
        /// <param name="newName">
        /// A string containing the new name for the local group.
        /// </param>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the specified group cannot be found.
        /// </exception>
        /// <exception cref="NameInUseException">
        /// Thrown when the specified target local group already exists.
        /// </exception>
        /// <exception cref="InvalidNameException">
        /// Thrown when The specified target local group name is invalid.
        /// </exception>
        internal void RenameLocalGroup(SecurityIdentifier sid, string newName)
        {
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                RenameLocalGroup(account.Value, newName);
            }
            catch (IdentityNotMappedException exc)
            {
                throw new GroupNotFoundException(exc, sid.ToString());
            }
        }

        /// <summary>
        /// Rename a local group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object containing
        /// information about the local group to be renamed.
        /// </param>
        /// <param name="newName">
        /// A string containing the new name for the local group.
        /// </param>
        /// <exception cref="GroupNotFoundException">
        /// Thrown when the specified group cannot be found.
        /// </exception>
        /// <exception cref="NameInUseException">
        /// Thrown when the specified target local group already exists.
        /// </exception>
        /// <exception cref="InvalidNameException">
        /// Thrown when The specified target local group name is invalid.
        /// </exception>
        internal void RenameLocalGroup(LocalGroup group, string newName)
        {
            SecurityIdentifier sid = group.SID;
            if (sid is null)
            {
                RenameLocalGroup(group.Name, newName);
                return;
            }

            RenameLocalGroup(sid, newName);
        }

        internal void RenameLocalGroup(string groupName, string newName)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = groupName
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, groupName);

            if (groupPrincipal is null)
            {
                throw new GroupNotFoundException(groupName, groupName);
            }

            var entry = (DirectoryEntry)groupPrincipal.GetUnderlyingObject();

            try
            {
                entry.Rename(newName);
                entry.CommitChanges();
            }
            catch (COMException exc)
            {
                if (exc.HResult == -2147022672)
                {
                    // Remove ???
                    // COMException (0x800708B0): The directory property cannot be found in the cache.
                    throw new NameInUseException(exc, newName);
                }

                if (exc.HResult == -2147023517)
                {
                    // COMException (0x80070563): The specified local group already exists.
                    throw new NameInUseException(exc, newName);
                }

                if (exc.HResult == -2147022694)
                {
                    // COMException (0x8007089A): The specified username is invalid.
                    throw new InvalidNameException(exc, newName);
                }

                if (newName.Equals(groupPrincipal.Name, StringComparison.Ordinal))
                {
                    // newName format for local SAM is simply new name string.
                    // For Active Directory the format should be "CN=newname"
                    // but simple string works too althought returns COMException exception
                    // So the workaround is to check a result and
                    // if group has successfully renamed we don't throw.
                    //
                    // TODO: test this in domain environment. I expect 'groupPrincipal.Name' will re-read from AD but not sure.
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// Get all local groups whose names satisfy the specified predicate.
        /// </summary>
        /// <param name="pred">
        /// Predicate that determines whether a group satisfies the conditions.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{LocalGroup}"/> object containing LocalGroup
        /// objects that satisfy the predicate condition.
        /// </returns>
        internal IEnumerable<LocalGroup> GetMatchingLocalGroups(Predicate<string> pred)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using GroupPrincipal groupPattern = new GroupPrincipal(s_ctx);
            using var seacher = new PrincipalSearcher(groupPattern);
            using PrincipalSearchResult<Principal> fr = seacher.FindAll();

            foreach (GroupPrincipal sre in fr)
            {
                if (pred(sre.Name))
                {
                    yield return MakeLocalGroupObject(sre);
                }
            }
        }

        /// <summary>
        /// Get all local groups.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable{LocalGroup}"/> object containing a
        /// LocalGroup object for each local group.
        /// </returns>
        internal IEnumerable<LocalGroup> GetAllLocalGroups()
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using GroupPrincipal groupPattern = new GroupPrincipal(s_ctx);
            using var seacher = new PrincipalSearcher(groupPattern);
            using PrincipalSearchResult<Principal> fr = seacher.FindAll();

            foreach (GroupPrincipal sre in fr)
            {
                yield return MakeLocalGroupObject(sre);
            }
        }

        /// <summary>
        /// Add members to a local group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object identifying the group to
        /// which to add members.
        /// </param>
        /// <param name="member">
        /// An object of type <see cref="LocalPrincipal"/> identifying
        /// the member to be added.
        /// </param>
        /// <returns>
        /// An Exception object indicating any errors encountered.
        /// </returns>
        /// <exception cref="GroupNotFoundException">
        /// Thrown if the group could not be found.
        /// </exception>
        internal Exception AddLocalGroupMember(LocalGroup group, LocalPrincipal member)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = group.Name
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(s_ctx, IdentityType.Sid, group.SID.ToString());
            using Principal principalPattern = member.ObjectClass switch
            {
                "User" =>
                    new UserPrincipal(s_ctx)
                    {
                        Name = member.Name
                    },
                "Group" =>
                    new GroupPrincipal(s_ctx)
                    {
                        Name = member.Name
                    },
                _ => null
            };
            using var searcherPrincipal = new PrincipalSearcher(principalPattern);
            using Principal principal = searcherPrincipal.FindOne();
            //using var principal = Principal.FindByIdentity(s_ctx, IdentityType.Sid, member.SID.ToString());

            try
            {
                groupPrincipal.Members.Add(principal);
                groupPrincipal.Save();
            }
            catch (PrincipalExistsException exc)
            {
                return new MemberExistsException(exc, principal.Name, group.Name);
            }
            catch (Exception exc)
            {
                // TODO: perhaps we could make more userful exception.
                return exc;
            }

            return null;
        }

        /// <summary>
        /// Retrieve members of a Local group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object identifying the group whose members
        /// are requested.
        /// </param>
        /// <returns>
        /// An IEnumerable of <see cref="LocalPrincipal"/> objects containing the group's
        /// members.
        /// </returns>
        internal IEnumerable<LocalPrincipal> GetLocalGroupMembers(LocalGroup group)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = group.Name
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(ctx, IdentityType.Name, groupName);

            if (groupPrincipal is null)
            {
                throw new GroupNotFoundException(group.Name, group.Name);
            }

            foreach(Principal p in groupPrincipal.Members)
            {
                yield return MakeLocalPrincipalObject(p);
            }
        }

        /// <summary>
        /// Remove members from a local group.
        /// </summary>
        /// <param name="group">
        /// A <see cref="LocalGroup"/> object identifying the group from
        /// which to remove members
        /// </param>
        /// <param name="member">
        /// An object of type <see cref="LocalPrincipal"/> identifying
        /// the member to be removed.
        /// </param>
        /// <returns>
        /// An Exception object indicating any errors encountered.
        /// </returns>
        /// <exception cref="GroupNotFoundException">
        /// Thrown if the group could not be found.
        /// </exception>
        internal Exception RemoveLocalGroupMember(LocalGroup group, LocalPrincipal member)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var groupPattern = new GroupPrincipal(s_ctx)
            {
                Name = group.Name
            };
            using var searcher = new PrincipalSearcher(groupPattern);
            using var groupPrincipal = (GroupPrincipal)searcher.FindOne();
            //using var groupPrincipal = GroupPrincipal.FindByIdentity(s_ctx, IdentityType.Sid, group.SID.ToString());
            using Principal principalPattern = member.ObjectClass switch
            {
                "User" =>
                    new UserPrincipal(s_ctx)
                    {
                        Name = member.Name
                    },
                "Group" =>
                    new GroupPrincipal(s_ctx)
                    {
                        Name = member.Name
                    },
                _ => null
            };
            using var searcherPrincipal = new PrincipalSearcher(principalPattern);
            using Principal principal = searcherPrincipal.FindOne();
            //using var principal = Principal.FindByIdentity(s_ctx, IdentityType.Sid, member.SID.ToString());

            try
            {
                if (!groupPrincipal.Members.Remove(principal))
                {
                    return new MemberNotFoundException(principal.Name, groupPrincipal.Name);
                }

                groupPrincipal.Save();
            }
            catch (PrincipalExistsException exc)
            {
                return exc;
            }
            catch (Exception exc)
            {
                // TODO: perhaps we could make more userful exception.
                return new Exception(null, exc);
            }

            return null;
        }

#endregion Local Groups

#region Local Users
        /// <summary>
        /// Retrieve a named local user.
        /// </summary>
        /// <param name="userName">Name of the desired local user.</param>
        /// <returns>
        /// A <see cref="LocalUser"/> object containing information about
        /// the local user.
        /// </returns>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the named user cannot be found.
        /// </exception>
        internal LocalUser GetLocalUser(string userName)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var userPattern = new UserPrincipal(s_ctx)
            {
                Name = userName
            };
            using var searcher = new PrincipalSearcher(userPattern);
            using var userPrincipal = (UserPrincipal)searcher.FindOne();
            //using var userPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.Name, userName);

            if (userPrincipal is null)
            {
                throw new UserNotFoundException(userName, userName);
            }

            return MakeLocalUserObject(userPrincipal);
        }

        /// <summary>
        /// Retrieve a local user by SID.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the desired user.
        /// </param>
        /// <returns>
        /// A <see cref="LocalUser"/> object containing information about
        /// the local user.
        /// </returns>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the specified user cannot be found.
        /// </exception>
        internal LocalUser GetLocalUser(SecurityIdentifier sid)
        {
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                return GetLocalUser(account.Value);
            }
            catch (IdentityNotMappedException exc)
            {
                throw new UserNotFoundException(exc, sid.ToString());
            }
        }

        /// <summary>
        /// Create a local user.
        /// </summary>
        /// <param name="user">A <see cref="LocalUser"/> object containing
        /// information about the local user to be created.
        /// </param>
        /// <param name="password">A <see cref="System.Security.SecureString"/> containing
        /// the initial password to be set for the new local user. If this parameter is null,
        /// no password is set.
        /// </param>
        /// <param name="setPasswordNeverExpires">
        /// Indicates whether PasswordNeverExpires was specified
        /// </param>
        /// <returns>
        /// A new LocalGroup object containing information about the newly
        /// created local user.
        /// </returns>
        /// <exception cref="UserExistsException">
        /// Thrown when an attempt is made to create a local user that already
        /// exists.
        /// </exception>
        internal LocalUser CreateLocalUser(LocalUser user, System.Security.SecureString password, bool setPasswordNeverExpires)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var userPrincipal = new UserPrincipal(s_ctx)
            {
                AccountExpirationDate = user.AccountExpires,
                Enabled = user.Enabled,
                Description = user.Description,
                DisplayName = user.FullName,
                Name = user.Name,
                PasswordNotRequired = !user.PasswordRequired,
                PasswordNeverExpires = setPasswordNeverExpires,
                UserCannotChangePassword = !user.UserMayChangePassword,
                // TODO: what properties should we assign?
            };

            try
            {
                userPrincipal.Save();
            }
            catch (PrincipalExistsException exc)
            {
                throw new UserExistsException(exc, user.Name);
            }
            catch (PrincipalOperationException exc)
            {
                //if (exc.ErrorCode == -2147023517)
                //{
                //    // COMException (0x80070563): The specified local group already exists.
                //    throw new UserExistsException(exc, user.Name);
                //}

                if (exc.ErrorCode == -2147022694)
                {
                    // COMException (‭0x8007089A‬): The specified username is invalid.
                    throw new InvalidNameException(exc, user.Name);
                }

                throw;
            }

            if (password != null)
            {
                try
                {
                    userPrincipal.SetPassword(password.AsString());
                }
                catch (PasswordException)
                {
                    userPrincipal.Delete();
                    throw new InvalidPasswordException();
                }
            }

            return MakeLocalUserObject(userPrincipal);
        }

        /// <summary>
        /// Remove a local user.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying
        /// the local user to be removed.
        /// </param>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the specified user cannot be found.
        /// </exception>
        internal void RemoveLocalUser(SecurityIdentifier sid)
        {
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                RemoveLocalUser(account.Value);
            }
            catch (IdentityNotMappedException exc)
            {
                throw new UserNotFoundException(exc, sid.ToString());
            }
        }

        /// <summary>
        /// Remove a local user.
        /// </summary>
        /// <param name="user">
        /// A <see cref="LocalUser"/> object containing
        /// information about the local user to be removed.
        /// </param>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the specified user cannot be found.
        /// </exception>
        internal void RemoveLocalUser(LocalUser user)
        {
            SecurityIdentifier sid = user.SID;
            if (sid is null)
            {
                RemoveLocalUser(user.Name);
                return;
            }

            RemoveLocalUser(sid);
        }

        internal void RemoveLocalUser(string userName)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using var userPattern = new UserPrincipal(s_ctx)
            {
                Name = userName
            };
            using var searcher = new PrincipalSearcher(userPattern);
            using var userPrincipal = (UserPrincipal)searcher.FindOne();
            //using var userPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.Name, userName);

            if (userPrincipal is null)
            {
                throw new UserNotFoundException(userName, userName);
            }

            userPrincipal.Delete();
        }


        /// <summary>
        /// Rename a local user.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> objects identifying
        /// the local user to be renamed.
        /// </param>
        /// <param name="newName">
        /// A string containing the new name for the local user.
        /// </param>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the specified user cannot be found.
        /// </exception>
        internal void RenameLocalUser(SecurityIdentifier sid, string newName)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            var identityValue = sid.ToString();
            using var userPrincipal = UserPrincipal.FindByIdentity(s_ctx, IdentityType.Sid, identityValue);

            if (userPrincipal is null)
            {
                throw new UserNotFoundException(identityValue, identityValue);
            }

            RenameLocalUser(userPrincipal, newName);
        }

        /// <summary>
        /// Rename a local user.
        /// </summary>
        /// <param name="user">
        /// A <see cref="LocalUser"/> objects containing
        /// information about the local user to be renamed.
        /// </param>
        /// <param name="newName">
        /// A string containing the new name for the local user.
        /// </param>
        /// <exception cref="UserNotFoundException">
        /// Thrown when the specified user cannot be found.
        /// </exception>
        internal void RenameLocalUser(LocalUser user, string newName)
        {
            SecurityIdentifier sid = user.SID;
            if (sid is null)
            {
                //using var ctx = new PrincipalContext(ContextType.Machine);
                using var userPrincipal = UserPrincipal.FindByIdentity(s_ctx, IdentityType.Name, user.Name);

                if (userPrincipal is null)
                {
                    throw new UserNotFoundException(user.Name, user.Name);
                }

                RenameLocalUser(userPrincipal, newName);
                return;
            }

            RenameLocalUser(sid, newName);
        }

        private static void RenameLocalUser(UserPrincipal userPrincipal, string newName)
        {
            var entry = (DirectoryEntry)userPrincipal.GetUnderlyingObject();
            try
            {
                entry.Rename(newName);
                entry.CommitChanges();
            }
            catch (COMException exc)
            {
                if (exc.HResult == -2147022672)
                {
                    // COMException (0x800708B0): The directory property cannot be found in the cache.
                    throw new NameInUseException(exc, newName);
                }

                if (exc.HResult == -2147022694)
                {
                    // COMException (0x8007089A): The specified username is invalid.
                    throw new InvalidNameException(exc, newName);
                }

                if (newName.Equals(userPrincipal.Name, StringComparison.Ordinal))
                {
                    // newName format for local SAM is simply new name string.
                    // For Active Directory the format should be "CN=newname"
                    // but simple string works too althought returns COMException exception
                    // So the workaround is to check a result and
                    // if group has successfully renamed we don't throw.
                    //
                    // TODO: test this in domain environment. I expect 'userPrincipal.Name' will re-read from AD but not sure.
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// Enable or disable a Local User.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the user to enable or disable.
        /// </param>
        /// <param name="enable">
        /// Indicating whether to enable or disable the user.
        /// </param>
        internal void EnableLocalUser(SecurityIdentifier sid, bool enable)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            //var identityValue = sid.ToString();
            //using var userPrincipal = UserPrincipal.FindByIdentity(s_ctx, IdentityType.Sid, identityValue);
            string userName;
            try
            {
                NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                userName = account.Value;
            }
            catch (IdentityNotMappedException exc)
            {
                throw new UserNotFoundException(exc, sid.ToString());
            }

            using var userPattern = new UserPrincipal(s_ctx)
            {
                Name = userName
            };
            using var searcher = new PrincipalSearcher(userPattern);
            using var userPrincipal = (UserPrincipal)searcher.FindOne();

            if (userPrincipal is null)
            {
                throw new UserNotFoundException(userName, userName);
            }

            userPrincipal.Enabled = enable;
            userPrincipal.Save();
        }

        /// <summary>
        /// Enable or disable a Local User.
        /// </summary>
        /// <param name="user">
        /// A <see cref="LocalUser"/> object representing the user to enable or disable.
        /// </param>
        /// <param name="enable">
        /// Indicating whether to enable or disable the user.
        /// </param>
        internal void EnableLocalUser(LocalUser user, bool enable)
        {
            SecurityIdentifier sid = user.SID;
            if (sid is null)
            {
                //using var ctx = new PrincipalContext(ContextType.Machine);
                using var userPattern = new UserPrincipal(s_ctx)
                {
                    Name = user.Name
                };
                using var searcher = new PrincipalSearcher(userPattern);
                using var userPrincipal = (UserPrincipal)searcher.FindOne();
                //using var userPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.Name, user.Name);

                if (userPrincipal is null)
                {
                    throw new UserNotFoundException(user.Name, user.Name);
                }

                userPrincipal.Enabled = enable;
                userPrincipal.Save();
                return;
            }

            EnableLocalUser(sid, enable);
        }

        /// <summary>
        /// Update a local user with new properties.
        /// </summary>
        /// <param name="user">
        /// A <see cref="LocalUser"/> object representing the user to be updated.
        /// </param>
        /// <param name="changed">
        /// A LocalUser object containing the desired changes.
        /// </param>
        /// <param name="password">A <see cref="System.Security.SecureString"/>
        /// object containing the new password. A null value in this parameter
        /// indicates that the password is not to be changed.
        /// </param>
        /// <param name="setPasswordNeverExpires">
        /// Specifies whether the PasswordNeverExpires parameter was set.
        /// </param>
        /// <remarks>
        /// Call this overload when intending to leave the password-expired
        /// marker in its current state. To set the password and the
        /// password-expired state, call the overload with a boolean as the
        /// fourth parameter
        /// </remarks>
        internal void UpdateLocalUser(LocalUser user, LocalUser changed, System.Security.SecureString password, bool? setPasswordNeverExpires)
        {
            SecurityIdentifier sid = user.SID;
            if (sid is null)
            {
                //using var ctx = new PrincipalContext(ContextType.Machine);
                using var userPattern = new UserPrincipal(s_ctx)
                {
                    Name = user.Name
                };
                using var searcher = new PrincipalSearcher(userPattern);
                using var userPrincipal = (UserPrincipal)searcher.FindOne();
                //using var userPrincipal = UserPrincipal.FindByIdentity(ctx, IdentityType.Name, user.Name);

                if (userPrincipal is null)
                {
                    throw new UserNotFoundException(user.Name, user.Name);
                }

                SetProperties(user, changed, userPrincipal, setPasswordNeverExpires);

                userPrincipal.Save();

                if (password != null)
                {
                    try
                    {
                        userPrincipal.SetPassword(password.AsString());
                    }
                    catch (PasswordException)
                    {
                        throw new InvalidPasswordException();
                    }
                }

                return;
            }
            else
            {
                //using var ctx = new PrincipalContext(ContextType.Machine);
                //var identityValue = user.SID.ToString();
                //using var userPrincipal = UserPrincipal.FindByIdentity(s_ctx, IdentityType.Sid, identityValue);
                string userName;
                try
                {
                    NTAccount account = (NTAccount)sid.Translate(typeof(NTAccount));
                    userName = account.Value;
                }
                catch (IdentityNotMappedException exc)
                {
                    throw new UserNotFoundException(exc, sid.ToString());
                }

                using var userPattern = new UserPrincipal(s_ctx)
                {
                    Name = userName
                };
                using var searcher = new PrincipalSearcher(userPattern);
                using var userPrincipal = (UserPrincipal)searcher.FindOne();

                if (userPrincipal is null)
                {
                    throw new UserNotFoundException(userName, userName);
                }

                SetProperties(user, changed, userPrincipal, setPasswordNeverExpires);

                userPrincipal.Save();

                if (password != null)
                {
                    try
                    {
                        userPrincipal.SetPassword(password.AsString());
                    }
                    catch (PasswordException)
                    {
                        throw new InvalidPasswordException();
                    }
                }

                return;
            }
        }

        private static void SetProperties(LocalUser user, LocalUser changed, UserPrincipal userPrincipal, bool? setPasswordNeverExpires)
        {
            if (user.Enabled != changed.Enabled)
            {
                userPrincipal.Enabled = changed.Enabled;
            }
            if (user.AccountExpires != changed.AccountExpires)
            {
                userPrincipal.AccountExpirationDate = changed.AccountExpires;
            }
            if (user.Description != changed.Description)
            {
                userPrincipal.Description = changed.Description;
            }
            if (user.FullName != changed.FullName)
            {
                userPrincipal.DisplayName = changed.FullName;
            }
            if (setPasswordNeverExpires.HasValue)
            {
                userPrincipal.PasswordNeverExpires = setPasswordNeverExpires.Value;
            }
            if (user.UserMayChangePassword != changed.UserMayChangePassword)
            {
                userPrincipal.UserCannotChangePassword = !changed.UserMayChangePassword;
            }
            if (user.PasswordRequired != changed.PasswordRequired)
            {
                userPrincipal.PasswordNotRequired = !changed.PasswordRequired;
            }
        }

        /// <summary>
        /// Get all local users whose names satisfy the specified predicate.
        /// </summary>
        /// <param name="pred">
        /// Predicate that determines whether a user satisfies the conditions.
        /// </param>
        /// <returns>
        /// An <see cref="IEnumerable{LocalUser}"/> object containing LocalUser
        /// objects that satisfy the predicate condition.
        /// </returns>
        internal IEnumerable<LocalUser> GetMatchingLocalUsers(Predicate<string> pred)
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using UserPrincipal userPattern = new UserPrincipal(s_ctx);
            using var seacher = new PrincipalSearcher(userPattern);
            using PrincipalSearchResult<Principal> fr = seacher.FindAll();

            foreach (UserPrincipal sre in fr)
            {
                if (pred(sre.Name))
                {
                    yield return MakeLocalUserObject(sre);
                }
            }
        }

        /// <summary>
        /// Get all local users.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable{LocalUser}"/> object containing a
        /// LocalUser object for each local user.
        /// </returns>
        internal IEnumerable<LocalUser> GetAllLocalUsers()
        {
            //using var ctx = new PrincipalContext(ContextType.Machine);
            using UserPrincipal userPattern = new UserPrincipal(s_ctx);
            using var seacher = new PrincipalSearcher(userPattern);
            using PrincipalSearchResult<Principal> fr = seacher.FindAll();

            foreach (UserPrincipal sre in fr)
            {
                yield return MakeLocalUserObject(sre);
            }
        }
#endregion Local Users

#region Local Principals
        internal LocalPrincipal LookupAccount(string name)
        {
            var info = LookupAccountInfo(name);

            if (info == null)
                throw new PrincipalNotFoundException(name, name);

            return MakeLocalPrincipalObject(info);
        }
#endregion Local Principals
#endregion Public (Internal) Methods

#region Private Methods
        /// <summary>
        /// Open the handles stored by Sam instances.
        /// </summary>
        private LocalUser MakeLocalUserObject(UserPrincipal sre)
        {
            LocalUser user = new LocalUser()
            {
                PrincipalSource = GetPrincipalSource(sre.Sid),
                SID = sre.Sid,
                AccountExpires = sre.AccountExpirationDate,
                Description = sre.Description,
                Enabled = sre.Enabled.GetValueOrDefault(),
                FullName = sre.DisplayName,
                LastLogon = sre.LastLogon,
                Name = sre.Name,
                PasswordExpires = GetPasswordExpirationDate(sre),
                PasswordLastSet = GetLastPasswordSetDate(sre),
                PasswordRequired = !sre.PasswordNotRequired,
                UserMayChangePassword = !sre.UserCannotChangePassword,
            };

            return user;
        }

        private static DateTime? GetPasswordExpirationDate(UserPrincipal sre)
        {
            var a = (DirectoryEntry)sre.GetUnderlyingObject();
            var passwordAge = (int)a.Properties["PasswordAge"][0];
            var maxPasswordAge = (int)a.Properties["MaxPasswordAge"][0];
            var age = maxPasswordAge - passwordAge;
            return sre.PasswordNeverExpires ? (DateTime?)null : DateTime.Now.AddSeconds(age);
        }

        private static DateTime? GetLastPasswordSetDate(UserPrincipal sre)
        {
            return sre.LastPasswordSet.HasValue ? sre.LastPasswordSet.Value.ToLocalTime() : sre.LastPasswordSet;
        }

        private LocalGroup MakeLocalGroupObject(GroupPrincipal sre)
        {
            LocalGroup group = new LocalGroup()
            {
                PrincipalSource = 0, // TODO: sre.ContextType.ToString(),
                SID = sre.Sid,
                Name = sre.Name,
                Description = sre.Description
            };

            return group;
        }

#region Utility Methods
        /// <summary>
        /// Lookup the account identified by specified account name.
        /// </summary>
        /// <param name="accountName">
        /// A string containing the name of the account to look up.
        /// </param>
        /// <returns>
        /// A <see cref="AccountInfo"/> object contains information about the
        /// account, or null if no matching account was found.
        /// </returns>
        private AccountInfo LookupAccountInfo(string accountName)
        {
            var sbDomainName = new StringBuilder();
            var domainNameLength = (uint)sbDomainName.Capacity;
            byte [] sid = null;
            uint sidLength = 0;
            SID_NAME_USE use;
            int error = Win32.NO_ERROR;

            if (!Win32.LookupAccountName(null,
                                         accountName,
                                         sid,
                                         ref sidLength,
                                         sbDomainName,
                                         ref domainNameLength,
                                         out use))
            {
                error = Marshal.GetLastWin32Error();
                if (error == Win32.ERROR_INSUFFICIENT_BUFFER || error == Win32.ERROR_INVALID_FLAGS)
                {
                    sid = new byte[sidLength];
                    sbDomainName.EnsureCapacity((int)domainNameLength);
                    error = Win32.NO_ERROR;

                    if (!Win32.LookupAccountName(null,
                                                 accountName,
                                                 sid,
                                                 ref sidLength,
                                                 sbDomainName,
                                                 ref domainNameLength,
                                                 out use))
                        error = Marshal.GetLastWin32Error();
                }

            }

            if (error == Win32.ERROR_SUCCESS)
            {
                // Bug: 7407413 :
                // If accountname is in the format domain1\user1,
                // then AccountName.ToString() will return domain1\domain1\user1
                // Ideally , accountname should be processed to hold only account name (without domain)
                // as we are keeping the domain in 'DomainName' variable.

                int index = accountName.IndexOf("\\", StringComparison.CurrentCultureIgnoreCase);
                if (index > -1)
                {
                    accountName = accountName.Substring(index + 1);
                }

                return new AccountInfo
                {
                    AccountName = accountName,
                    DomainName = sbDomainName.ToString(),
                    Sid = new SecurityIdentifier(sid, 0),
                    Use = use
                };
            }
            else if (error == Win32.ERROR_NONE_MAPPED)
                return null;
            else if (error == Win32.ERROR_ACCESS_DENIED)
                throw new AccessDeniedException(context.target);
            else
                throw new Win32InternalException(error, context.target);
        }

        /// <summary>
        /// Create a <see cref="LocalPrincipal"/> object from information in
        /// an AccountInfo object.
        /// </summary>
        /// <param name="info">
        /// An AccountInfo object containing information about the account
        /// for which the LocalPrincipal object is being created. This parameter
        /// may be null, in which case this method returns null.
        /// </param>
        /// <returns>
        /// A new LocalPrincipal object representing the account, or null if the
        /// <paramref name="info"/> parameter is null.
        /// </returns>
        private LocalPrincipal MakeLocalPrincipalObject(AccountInfo info)
        {
            if (info == null)
                return null;    // this is a legitimate case

            var rv = new LocalPrincipal(info.ToString());
            rv.SID = info.Sid;
            rv.PrincipalSource = GetPrincipalSource(info);

            switch (info.Use)
            {
                case SID_NAME_USE.SidTypeAlias:     // TODO: is this the right thing to do???
                case SID_NAME_USE.SidTypeGroup:
                case SID_NAME_USE.SidTypeWellKnownGroup:
                    rv.ObjectClass = Strings.ObjectClassGroup;
                    break;

                case SID_NAME_USE.SidTypeUser:
                    rv.ObjectClass = Strings.ObjectClassUser;
                    break;

                default:
                    rv.ObjectClass = Strings.ObjectClassOther;
                    break;
            }

            return rv;
        }

        private LocalPrincipal MakeLocalPrincipalObject(Principal info)
        {
            if (info == null)
            {
                return null;    // this is a legitimate case - TODO: remove?
            }

            NTAccount account = (NTAccount)info.Sid.Translate(typeof(NTAccount));

            var rv = new LocalPrincipal(account.ToString())
            {
                SID = info.Sid,
                PrincipalSource = GetPrincipalSource(info.Sid),

                ObjectClass = info switch
                {
                    UserPrincipal _ => Strings.ObjectClassUser,
                    GroupPrincipal _ => Strings.ObjectClassGroup,
                    _ => Strings.ObjectClassOther
                }
            };

            return rv;
        }

        /// <summary>
        /// Helper to throw an exception if the provided Status code
        /// represents a failure.
        /// </summary>
        /// <param name="ntStatus">
        /// One of the NTSTATUS code values indicating the error, if any.
        /// </param>
        /// <param name="context">
        /// A <see cref="Context"/> object containing information about the
        /// current operation. If this parameter is null, the class's context
        /// is used.
        /// </param>
        private void ThrowOnFailure(UInt32 ntStatus, Context context = null)
        {
            if (NtStatus.IsError(ntStatus))
            {
                var ex = MakeException(ntStatus, context);

                if (ex != null)
                    throw ex;
            }
        }

        /// <summary>
        /// Create an appropriate exception from the specified status code.
        /// </summary>
        /// <param name="ntStatus">
        /// One of the NTSTATUS code values indicating the error, if any.
        /// </param>
        /// <param name="context">
        /// A <see cref="Context"/> object containing information about the
        /// current operation. If this parameter is null, the class's context
        /// is used.
        /// </param>
        /// <returns>
        /// An <see cref="Exception"/> object, or an object derived from Exception,
        /// appropriate to the error. If <paramref name="ntStatus"/> does not
        /// indicate an error, the method returns null.
        /// </returns>
        private Exception MakeException(UInt32 ntStatus, Context context = null)
        {
            if (!NtStatus.IsError(ntStatus))
                return null;

            if (context == null)
                context = this.context;

            switch (ntStatus)
            {
                case NtStatus.STATUS_ACCESS_DENIED:
                    return new AccessDeniedException(context.target);

                case NtStatus.STATUS_INVALID_ACCOUNT_NAME:
                    return new InvalidNameException(context.ObjectName, context.target);

                case NtStatus.STATUS_USER_EXISTS:
                    if (context.operation == ContextOperation.New &&
                        context.type == ContextObjectType.User)
                    {
                        return new UserExistsException(context.ObjectName, context.target);
                    }
                    else
                    {
                        return new NameInUseException(context.ObjectName, context.target);
                    }

                case NtStatus.STATUS_ALIAS_EXISTS:
                    if (context.operation == ContextOperation.New &&
                        context.type == ContextObjectType.Group)
                    {
                        return new GroupExistsException(context.ObjectName, context.target);
                    }
                    else
                    {
                        return new NameInUseException(context.ObjectName, context.target);
                    }

                case NtStatus.STATUS_GROUP_EXISTS:
                    return new NameInUseException(context.ObjectName, context.target);

                case NtStatus.STATUS_NO_SUCH_ALIAS:
                case NtStatus.STATUS_NO_SUCH_GROUP:
                    return new GroupNotFoundException(context.ObjectName, context.target);

                case NtStatus.STATUS_NO_SUCH_USER:
                    return new UserNotFoundException(context.ObjectName, context.target);

                case NtStatus.STATUS_SPECIAL_GROUP:     // The group specified is a special group and cannot be operated on in the requested fashion.
                // case NtStatus.STATUS_SPECIAL_ALIAS: // referred to in source for SAM api, but not in ntstatus.h!!!

                    return new InvalidOperationException(StringUtil.Format(Strings.InvalidForGroup, context.ObjectName));

                case NtStatus.STATUS_SPECIAL_USER:  // The user specified is a special user and cannot be operated on in the requested fashion.
                    return new InvalidOperationException(StringUtil.Format(Strings.InvalidForUser, context.ObjectName));

                case NtStatus.STATUS_NO_SUCH_MEMBER:
                    return new MemberNotFoundException(context.MemberName, context.ObjectName);

                case NtStatus.STATUS_MEMBER_IN_ALIAS:
                case NtStatus.STATUS_MEMBER_IN_GROUP:
                    if (context.operation == ContextOperation.Remove &&
                        context.type == ContextObjectType.Group)
                    {
                        return new InvalidOperationException(StringUtil.Format(Strings.GroupHasMembers, context.ObjectName));
                    }
                    else
                    {
                        return new MemberExistsException(context.MemberName, context.ObjectName, context.target);
                    }

                case NtStatus.STATUS_MEMBER_NOT_IN_ALIAS:
                case NtStatus.STATUS_MEMBER_NOT_IN_GROUP:
                    return new MemberNotFoundException(context.MemberName, context.ObjectName);

                case NtStatus.STATUS_MEMBERS_PRIMARY_GROUP:
                    return new InvalidOperationException(StringUtil.Format(Strings.MembersPrimaryGroup, context.ObjectName));

                case NtStatus.STATUS_LAST_ADMIN:    // Cannot delete the last administrator.
                    return new InvalidOperationException(Strings.LastAdmin);

                case NtStatus.STATUS_ILL_FORMED_PASSWORD:
                case NtStatus.STATUS_PASSWORD_RESTRICTION:
                    return new InvalidPasswordException(Native.Win32.RtlNtStatusToDosError(ntStatus));

                // TODO: do we want to handle these?
                //      they appear to be returned only in functions we are not calling
                case NtStatus.STATUS_INVALID_SID:       // member sid is corrupted
                case NtStatus.STATUS_INVALID_MEMBER:    // member has wrong account type
                default:
                    return new InternalException(ntStatus, context.target);
            }
        }

        /// <summary>
        /// Determine the source of a user or group. Either local, Active Directory,
        /// or Azure AD.
        /// </summary>
        /// <param name="sid">
        /// A <see cref="SecurityIdentifier"/> object identifying the user or group.
        /// </param>
        /// <returns>
        /// One of the <see cref="PrincipalSource"/> enumerations identifying the
        /// source of the object.
        /// </returns>
        private PrincipalSource? GetPrincipalSource(SecurityIdentifier sid)
        {
            var bSid = new byte[sid.BinaryLength];

            sid.GetBinaryForm(bSid, 0);

            var type = LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType;

            // Use LsaLookupUserAccountType for Windows 10 and later.
            // Earlier versions of the OS will leave the property NULL because
            // it is too error prone to attempt to replicate the decisions of
            // LsaLookupUserAccountType.
            var os = Environment.OSVersion;
            if (os.Version.Major >= 10)
            {
                UInt32 status = Native.Win32.LsaLookupUserAccountType(bSid, out type);
                if (NtStatus.IsError(status))
                    type = LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType;

                switch (type)
                {
                    case LSA_USER_ACCOUNT_TYPE.ExternalDomainUserAccountType:
                    case LSA_USER_ACCOUNT_TYPE.PrimaryDomainUserAccountType:
                        return PrincipalSource.ActiveDirectory;

                    case LSA_USER_ACCOUNT_TYPE.LocalUserAccountType:
                        return PrincipalSource.Local;

                    case LSA_USER_ACCOUNT_TYPE.AADUserAccountType:
                        return PrincipalSource.AzureAD;

                    // Currently, there is no value returned by LsaLookupUserAccountType
                    // that corresponds to LSA_USER_ACCOUNT_TYPE.MSAUserAccountType,
                    // but there may be in the future, so we'll account for it here.
                    case LSA_USER_ACCOUNT_TYPE.MSAUserAccountType:
                    case LSA_USER_ACCOUNT_TYPE.LocalConnectedUserAccountType:
                        return PrincipalSource.MicrosoftAccount;

                    case LSA_USER_ACCOUNT_TYPE.InternetUserAccountType:
                        return sid.IsMsaAccount()
                            ? PrincipalSource.MicrosoftAccount
                            : PrincipalSource.Unknown;

                    case LSA_USER_ACCOUNT_TYPE.UnknownUserAccountType:
                    default:
                        return PrincipalSource.Unknown;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Determine the source of a user or group. Either local, Active Directory,
        /// or Azure AD.
        /// </summary>
        /// <param name="info">
        /// An <see cref="AccountInfo"/> object containing information about the
        /// user or group.
        /// </param>
        /// <returns>
        /// One of the <see cref="PrincipalSource"/> enumerations identifying the
        /// source of the object.
        /// </returns>
        private PrincipalSource? GetPrincipalSource(AccountInfo info)
        {
            return GetPrincipalSource(info.Sid);
        }

#endregion Utility Methods
#endregion Private Methods

#region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                UInt32 status = 0;

                if (disposing)
                {
                    // no managed objects need disposing.
                }

                if (NtStatus.IsError(status))
                {
                    // Do nothing to satisfy CA1806: Do not ignore method results. We want the dispose to proceed regardless of the handle close status.
                }

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Sam()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }
#endregion IDisposable Support
    }
}
