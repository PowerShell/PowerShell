// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Management.Automation
{
    [Flags]
    internal enum VariablePathFlags
    {
        None = 0x00,
        Local = 0x01,
        Script = 0x02,
        Global = 0x04,
        Private = 0x08,
        Variable = 0x10,
        Function = 0x20,
        DriveQualified = 0x40,
        Unqualified = 0x80,

        // If any of these bits are set, the path does not represent an unscoped variable.
        UnscopedVariableMask = Local | Script | Global | Private | Function | DriveQualified,
    }

    /// <summary>
    /// A variable path that you can query the scope and drive of the variable reference.
    /// </summary>
    public class VariablePath
    {
        #region private data

        /// <summary>
        /// Stores the path that was passed to the constructor.
        /// </summary>
        private string _userPath;

        /// <summary>
        /// The name of the variable without any scope or drive.
        /// </summary>
        private string _unqualifiedPath;

        /// <summary>
        /// Store flags about the path, such as private/global/local/etc.
        /// </summary>
        private VariablePathFlags _flags = VariablePathFlags.None;

        #endregion private data

        #region Constructor

        /// <summary>
        /// Private constructor for CloneAndSetLocal().
        /// </summary>
        private VariablePath()
        {
        }

        /// <summary>
        /// Constructs a variable path.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        public VariablePath(string path)
            : this(path, VariablePathFlags.None)
        {
        }

        /// <summary>
        /// Constructs a scoped item lookup path.
        /// </summary>
        /// <param name="path">The path to parse.</param>
        /// <param name="knownFlags">
        /// These flags for anything known about the path (such as, is it a function) before
        /// being scanned.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        internal VariablePath(string path, VariablePathFlags knownFlags)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw PSTraceSource.NewArgumentException(nameof(path));
            }

            _userPath = path;
            _flags = knownFlags;

            string candidateScope = null;
            string candidateScopeUpper = null;
            VariablePathFlags candidateFlags = VariablePathFlags.Unqualified;

            int currentCharIndex = 0;
            int lastScannedColon = -1;

        scanScope:
            switch (path[0])
            {
                case 'g':
                case 'G':
                    candidateScope = "lobal";
                    candidateScopeUpper = "LOBAL";
                    candidateFlags = VariablePathFlags.Global;
                    break;
                case 'l':
                case 'L':
                    candidateScope = "ocal";
                    candidateScopeUpper = "OCAL";
                    candidateFlags = VariablePathFlags.Local;
                    break;
                case 'p':
                case 'P':
                    candidateScope = "rivate";
                    candidateScopeUpper = "RIVATE";
                    candidateFlags = VariablePathFlags.Private;
                    break;
                case 's':
                case 'S':
                    candidateScope = "cript";
                    candidateScopeUpper = "CRIPT";
                    candidateFlags = VariablePathFlags.Script;
                    break;
                case 'v':
                case 'V':
                    if (knownFlags == VariablePathFlags.None)
                    {
                        // If we see 'variable:', our namespaceId will be empty, and
                        // we'll also need to scan for the scope again.
                        candidateScope = "ariable";
                        candidateScopeUpper = "ARIABLE";
                        candidateFlags = VariablePathFlags.Variable;
                    }

                    break;
            }

            if (candidateScope != null)
            {
                currentCharIndex += 1; // First character already matched.
                int j;
                for (j = 0; currentCharIndex < path.Length && j < candidateScope.Length; ++j, ++currentCharIndex)
                {
                    if (path[currentCharIndex] != candidateScope[j] && path[currentCharIndex] != candidateScopeUpper[j])
                    {
                        break;
                    }
                }

                if (j == candidateScope.Length &&
                    currentCharIndex < path.Length &&
                    path[currentCharIndex] == ':')
                {
                    if (_flags == VariablePathFlags.None)
                    {
                        _flags = VariablePathFlags.Variable;
                    }

                    _flags |= candidateFlags;
                    lastScannedColon = currentCharIndex;
                    currentCharIndex += 1;

                    // If saw 'variable:', we need to look for a scope after 'variable:'.
                    if (candidateFlags == VariablePathFlags.Variable)
                    {
                        knownFlags = VariablePathFlags.Variable;
                        candidateScope = candidateScopeUpper = null;
                        candidateFlags = VariablePathFlags.None;
                        goto scanScope;
                    }
                }
            }

            if (_flags == VariablePathFlags.None)
            {
                lastScannedColon = path.IndexOf(':', currentCharIndex);
                // No colon, or a colon as the first character means we have
                // a simple variable, otherwise it's a drive.
                if (lastScannedColon > 0)
                {
                    _flags = VariablePathFlags.DriveQualified;
                }
            }

            if (lastScannedColon == -1)
            {
                _unqualifiedPath = _userPath;
            }
            else
            {
                _unqualifiedPath = _userPath.Substring(lastScannedColon + 1);
            }

            if (_flags == VariablePathFlags.None)
            {
                _flags = VariablePathFlags.Unqualified | VariablePathFlags.Variable;
            }
        }

        internal VariablePath CloneAndSetLocal()
        {
            Debug.Assert(IsUnscopedVariable, "Special method to clone, input must be unqualified");

            VariablePath result = new VariablePath();
            result._userPath = _userPath;
            result._unqualifiedPath = _unqualifiedPath;
            result._flags = VariablePathFlags.Local | VariablePathFlags.Variable;
            return result;
        }

        #endregion Constructor

        #region data accessors

        /// <summary>
        /// Gets the full path including any possibly specified scope and/or drive name.
        /// </summary>
        public string UserPath { get { return _userPath; } }

        /// <summary>
        /// Returns true if the path explicitly specifies 'global:'.
        /// </summary>
        public bool IsGlobal { get { return (_flags & VariablePathFlags.Global) != 0; } }

        /// <summary>
        /// Returns true if the path explicitly specifies 'local:'.
        /// </summary>
        public bool IsLocal { get { return (_flags & VariablePathFlags.Local) != 0; } }

        /// <summary>
        /// Returns true if the path explicitly specifies 'private:'.
        /// </summary>
        public bool IsPrivate { get { return (_flags & VariablePathFlags.Private) != 0; } }

        /// <summary>
        /// Returns true if the path explicitly specifies 'script:'.
        /// </summary>
        public bool IsScript { get { return (_flags & VariablePathFlags.Script) != 0; } }

        /// <summary>
        /// Returns true if the path specifies no drive or scope qualifiers.
        /// </summary>
        public bool IsUnqualified { get { return (_flags & VariablePathFlags.Unqualified) != 0; } }

        /// <summary>
        /// Returns true if the path specifies a variable path with no scope qualifiers.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unscoped")]
        public bool IsUnscopedVariable { get { return ((_flags & VariablePathFlags.UnscopedVariableMask) == 0); } }

        /// <summary>
        /// Returns true if the path defines a variable.
        /// </summary>
        public bool IsVariable { get { return (_flags & VariablePathFlags.Variable) != 0; } }

        /// <summary>
        /// Returns true if the path defines a function.
        /// </summary>
        internal bool IsFunction { get { return (_flags & VariablePathFlags.Function) != 0; } }

        /// <summary>
        /// Returns true if the path specifies a drive other than the variable drive.
        /// </summary>
        public bool IsDriveQualified { get { return (_flags & VariablePathFlags.DriveQualified) != 0; } }

        /// <summary>
        /// The drive name, or null if the path is for a variable.
        /// It may also be null for some functions (specifically if this is a FunctionScopedItemLookupPath.)
        /// </summary>
        public string DriveName
        {
            get
            {
                if (!IsDriveQualified)
                {
                    return null;
                }

                // The drive name is asked for infrequently.  Lots of VariablePath
                // objects are created, so rather than allocate an extra string that will
                // always be null, just compute the drive name on demand.
                return _userPath.Substring(0, _userPath.IndexOf(':'));
            }
        }

        /// <summary>
        /// Gets the namespace specific string.
        /// </summary>
        internal string UnqualifiedPath
        {
            get { return _unqualifiedPath; }
        }

        /// <summary>
        /// Return the drive qualified name, if any drive specified, otherwise the simple variable name.
        /// </summary>
        internal string QualifiedName
        {
            get { return IsDriveQualified ? _userPath : _unqualifiedPath; }
        }

        #endregion data accessors

        /// <summary>
        /// Helpful for debugging.
        /// </summary>
        public override string ToString()
        {
            return _userPath;
        }
    }

    internal class FunctionLookupPath : VariablePath
    {
        internal FunctionLookupPath(string path)
            : base(path, VariablePathFlags.Function | VariablePathFlags.Unqualified)
        {
        }
    }
}
