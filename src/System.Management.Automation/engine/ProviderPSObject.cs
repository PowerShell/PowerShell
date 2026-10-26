// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation.Language;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Management.Automation
{
    /// <summary>
    /// Declares an instance member backed by inline provider metadata.
    /// </summary>
    /// <remarks>
    /// These descriptors bridge a metadata struct to ETS member lookup. Their names do
    /// not define a runtime API independently of the provider's existing note properties.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class PSObjectMetadataAccessorAttribute : Attribute
    {
        /// <summary>
        /// Describes a metadata-backed ETS member.
        /// </summary>
        /// <param name="name">The ETS note-property name exposed by this accessor.</param>
        /// <param name="order">A unique, non-negative order used for deterministic member enumeration.</param>
        /// <param name="presenceMask">A mask of required bits across the full presence mask; zero means the member is always present.</param>
        internal PSObjectMetadataAccessorAttribute(string name, int order, uint presenceMask = 0)
        {
            Name = name;
            Order = order;
            PresenceMask = presenceMask;
        }

        /// <summary>
        /// Gets the case-insensitive ETS member name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the unique position used when materializing members.
        /// </summary>
        internal int Order { get; }

        /// <summary>
        /// Gets the bits required for this member to be present; zero means unconditional.
        /// </summary>
        internal uint PresenceMask { get; }
    }

    /// <summary>
    /// Provides instance-member names and values without allocating note properties.
    /// </summary>
    /// <remarks>Presence queries must not evaluate getters. Materialization returns visible note properties in descriptor order.</remarks>
    internal interface IProviderInstanceMemberSource
    {
        /// <summary>Gets the number of currently present descriptors without reading their values.</summary>
        int Count { get; }

        /// <summary>Checks whether a member is present without reading its value or creating a note property.</summary>
        /// <param name="name">The member name to compare using ordinal, case-insensitive matching.</param>
        /// <returns>Whether the member is present, regardless of its value.</returns>
        bool Contains(string name);

        /// <summary>Reads the value of a present member directly from its backing source.</summary>
        /// <param name="name">The member name to look up.</param>
        /// <param name="value">The getter result on success; otherwise null.</param>
        /// <returns>True when the member is present, including when its value is null.</returns>
        /// <remarks>Getter exceptions propagate to the caller. Values are not cached by this interface.</remarks>
        bool TryGetValue(string name, out object value);

        /// <summary>Creates note properties for all currently present members when full inspection requires them.</summary>
        /// <returns>Fresh note properties containing the getter values at enumeration time.</returns>
        IEnumerable<PSNoteProperty> Materialize();
    }

    /// <summary>
    /// A PSObject with per-result value metadata stored directly in its closed generic
    /// instance. Accessor descriptors are discovered and compiled once per metadata type.
    /// </summary>
    /// <typeparam name="TMetadata">The inline metadata struct declaring attributed accessor methods.</typeparam>
    /// <remarks>
    /// Direct reads invoke getters on each access. The instance-member collection owns the
    /// transition to mutable note properties; constructing this wrapper does not attach the source.
    /// </remarks>
    internal sealed class PSObject<TMetadata> : PSObject, IProviderInstanceMemberSource
        where TMetadata : struct
    {
        /// <summary>
        /// Stores the metadata payload without a separate carrier allocation.
        /// </summary>
        private readonly TMetadata _metadata;

        /// <summary>
        /// Stores the optional-member bits captured for this result.
        /// </summary>
        private readonly uint _presenceMask;

        /// <summary>
        /// Caches the validated, ordered accessors shared by this metadata type.
        /// </summary>
        private static readonly MetadataAccessor<TMetadata>[] s_accessors = MetadataAccessor<TMetadata>.Create();

        /// <summary>
        /// Ensures accessor registration runs before the first wrapper can be used by a binder.
        /// </summary>
        /// <remarks>
        /// Suppresses beforefieldinit so member names are registered before a binder can reuse
        /// an absence rule. Keep this initializer even though its body is empty.
        /// </remarks>
        static PSObject()
        {
        }

        /// <summary>
        /// Initializes a wrapper with inline metadata and an optional-member mask.
        /// </summary>
        /// <param name="value">The base object passed to metadata accessors.</param>
        /// <param name="metadata">The per-result payload copied into the wrapper.</param>
        /// <param name="presenceMask">The active optional-member bits; by default all bits are set.</param>
        internal PSObject(object value, TMetadata metadata, uint presenceMask = uint.MaxValue) : base(value)
        {
            _metadata = metadata;
            _presenceMask = presenceMask;
        }

        /// <inheritdoc/>
        int IProviderInstanceMemberSource.Count => ActiveCount;

        /// <inheritdoc/>
        bool IProviderInstanceMemberSource.Contains(string name) => Find(name) >= 0;

        /// <inheritdoc/>
        bool IProviderInstanceMemberSource.TryGetValue(string name, out object value)
        {
            int index = Find(name);
            if (index >= 0)
            {
                value = s_accessors[index].GetValue(this);
                return true;
            }

            value = null;
            return false;
        }

        /// <inheritdoc/>
        IEnumerable<PSNoteProperty> IProviderInstanceMemberSource.Materialize()
        {
            for (int i = 0; i < s_accessors.Length; i++)
            {
                if (IsPresent(s_accessors[i]))
                {
                    yield return new PSNoteProperty(s_accessors[i].Name, s_accessors[i].GetValue(this));
                }
            }
        }

        /// <summary>
        /// Gets the number of active descriptors without evaluating their getters.
        /// </summary>
        private int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (MetadataAccessor<TMetadata> accessor in s_accessors)
                {
                    if (IsPresent(accessor))
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// Checks whether all bits required by an accessor are active.
        /// </summary>
        /// <param name="accessor">The descriptor whose presence mask is checked.</param>
        /// <returns>True for an unconditional accessor or when every required bit is set.</returns>
        private bool IsPresent(MetadataAccessor<TMetadata> accessor)
            => accessor.PresenceMask == 0 || (_presenceMask & accessor.PresenceMask) == accessor.PresenceMask;

        /// <summary>
        /// Finds an active descriptor without evaluating its getter.
        /// </summary>
        /// <param name="name">The case-insensitive member name; null and empty names do not match.</param>
        /// <returns>The descriptor index, or -1 if no active descriptor matches.</returns>
        private int Find(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return -1;
            }

            for (int i = 0; i < s_accessors.Length; i++)
            {
                if (IsPresent(s_accessors[i])
                    && string.Equals(s_accessors[i].Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Describes one metadata-backed member and its compiled getter.
    /// </summary>
    /// <typeparam name="TMetadata">The metadata struct that declares the accessor.</typeparam>
    internal sealed class MetadataAccessor<TMetadata> where TMetadata : struct
    {
        /// <summary>
        /// Reads a value from the base object and optional inline metadata.
        /// </summary>
        private readonly Func<PSObject<TMetadata>, object> _getter;

        /// <summary>
        /// Initializes a validated metadata accessor.
        /// </summary>
        /// <param name="name">The ETS member name.</param>
        /// <param name="order">The materialization order.</param>
        /// <param name="presenceMask">The bits required for this member to be present.</param>
        /// <param name="getter">The compiled value getter.</param>
        private MetadataAccessor(string name, int order, uint presenceMask, Func<PSObject<TMetadata>, object> getter)
        {
            Name = name;
            Order = order;
            PresenceMask = presenceMask;
            _getter = getter;
        }

        /// <summary>
        /// Gets the case-insensitive ETS member name.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the unique position used when materializing members.
        /// </summary>
        internal int Order { get; }

        /// <summary>
        /// Gets the bits required for this member to be present; zero means unconditional.
        /// </summary>
        internal uint PresenceMask { get; }

        /// <summary>
        /// Invokes the getter without caching its result.
        /// </summary>
        /// <param name="target">The wrapper supplying the base object and metadata.</param>
        /// <returns>The member value, boxed when the accessor returns a value type.</returns>
        internal object GetValue(PSObject<TMetadata> target) => _getter(target);

        /// <summary>
        /// Discovers, validates, and compiles accessors, then registers their names with the member binder.
        /// </summary>
        /// <returns>The descriptors sorted by their declared order.</returns>
        /// <exception cref="InvalidOperationException">An accessor is invalid, or names or orders are duplicated.</exception>
        /// <remarks>Names are registered only after the complete descriptor set passes validation.</remarks>
        internal static MetadataAccessor<TMetadata>[] Create()
        {
            Type metadataType = typeof(TMetadata);
            var accessors = new List<MetadataAccessor<TMetadata>>();
            foreach (MethodInfo method in metadataType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<PSObjectMetadataAccessorAttribute>();
                if (attribute == null)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                ValidateAccessor(method, attribute, parameters, metadataType);
                Func<PSObject<TMetadata>, object> getter = CompileGetter(method, parameters);
                accessors.Add(new MetadataAccessor<TMetadata>(attribute.Name, attribute.Order, attribute.PresenceMask, getter));
            }

            accessors.Sort(static (left, right) => left.Order.CompareTo(right.Order));
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < accessors.Count; i++)
            {
                if (!names.Add(accessors[i].Name))
                {
                    throw new InvalidOperationException($"Duplicate PSObject metadata accessor name '{accessors[i].Name}' on {metadataType.FullName}.");
                }

                if (i > 0 && accessors[i - 1].Order == accessors[i].Order)
                {
                    throw new InvalidOperationException($"Duplicate PSObject metadata accessor order {accessors[i].Order} on {metadataType.FullName}.");
                }
            }

            foreach (MetadataAccessor<TMetadata> accessor in accessors)
            {
                // Register stable member names once per metadata type, not per object.
                PSGetMemberBinder.SetHasInstanceMember(accessor.Name);
            }

            return accessors.ToArray();
        }

        /// <summary>
        /// Validates the signature and member declaration of a metadata accessor.
        /// </summary>
        /// <param name="method">The attributed method.</param>
        /// <param name="attribute">The member name, order, and presence mask.</param>
        /// <param name="parameters">The method parameters.</param>
        /// <param name="metadataType">The struct expected by the optional readonly metadata parameter.</param>
        /// <exception cref="InvalidOperationException">The declaration cannot be used as a metadata accessor.</exception>
        private static void ValidateAccessor(MethodInfo method, PSObjectMetadataAccessorAttribute attribute, ParameterInfo[] parameters, Type metadataType)
        {
            // Accessors take the base object first, optionally followed by 'in TMetadata'.
            // This narrow signature supports a direct compiled getter and rejects mutable
            // or ambiguous metadata access during type initialization.
            bool hasMetadata = parameters.Length == 2;
            bool valid = method.IsStatic && !method.IsGenericMethod
                && (parameters.Length == 1 || hasMetadata);

            if (valid)
            {
                Type baseType = parameters[0].ParameterType;
                valid = !baseType.IsByRef && !baseType.IsPointer
                    && (baseType.IsClass || baseType.IsInterface);
            }

            if (valid && hasMetadata)
            {
                valid = parameters[1].ParameterType == typeof(TMetadata).MakeByRefType() && parameters[1].IsIn;
            }

            Type returnType = method.ReturnType;
            valid = valid && returnType != typeof(void) && !returnType.IsByRef && !returnType.IsPointer
                && !returnType.IsByRefLike && attribute.Order >= 0 && !string.IsNullOrEmpty(attribute.Name)
                && !PSMemberInfoCollection<PSMemberInfo>.IsReservedName(attribute.Name);
            if (!valid)
            {
                throw new InvalidOperationException($"Invalid PSObject metadata accessor: {metadataType.FullName}.{method.Name}");
            }
        }

        /// <summary>
        /// Compiles a direct call using the unwrapped base object and optional readonly metadata field.
        /// </summary>
        /// <param name="method">The validated static accessor method.</param>
        /// <param name="parameters">Its validated parameter list.</param>
        /// <returns>A getter whose result is converted to object for ETS value lookup.</returns>
        private static Func<PSObject<TMetadata>, object> CompileGetter(MethodInfo method, ParameterInfo[] parameters)
        {
            // Reflection is used only while descriptors are created. Reads call the
            // compiled delegate directly; value-type results still box at this ETS boundary.
            ParameterExpression target = Expression.Parameter(typeof(PSObject<TMetadata>), "target");
            Expression baseObject = Expression.Call(typeof(PSObject), "Base", Type.EmptyTypes, target);
            var arguments = new List<Expression>
            {
                Expression.Convert(baseObject, parameters[0].ParameterType)
            };
            if (parameters.Length == 2)
            {
                arguments.Add(Expression.Field(target, "_metadata"));
            }

            Expression call = Expression.Call(method, arguments);
            Expression body = Expression.Convert(call, typeof(object));
            return Expression.Lambda<Func<PSObject<TMetadata>, object>>(body, target).Compile();
        }
    }
}

namespace System.Management.Automation.Provider
{
    /// <summary>
    /// Carries the drive context and Unix stat captured when a FileSystem item is emitted.
    /// </summary>
    /// <remarks>Path getters use the current FileSystemInfo path; they do not refresh the captured drive or stat.</remarks>
    internal struct FileSystemProviderMetadata
    {
        /// <summary>
        /// The drive through which the item was emitted.
        /// </summary>
        internal PSDriveInfo Drive;
#if UNIX
        /// <summary>
        /// The Unix stat snapshot captured at emission.
        /// </summary>
        internal Platform.Unix.CommonStat UnixStat;
#endif

        /// <summary>
        /// Gets the current provider-qualified path.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <param name="metadata">The captured provider context.</param>
        /// <returns>The current full path qualified with the captured provider.</returns>
        [PSObjectMetadataAccessor("PSPath", 0)]
        private static string GetPath(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata)
            => System.Management.Automation.LocationGlobber.GetProviderQualifiedPath(file.FullName, metadata.Drive.Provider);

        /// <summary>
        /// Gets the current parent path relative to the captured drive root.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <param name="metadata">The captured provider context.</param>
        /// <returns>The qualified parent path, or an empty string when there is no parent.</returns>
        [PSObjectMetadataAccessor("PSParentPath", 1, 1)]
        private static string GetParentPath(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata)
        {
            string parentPath = Microsoft.PowerShell.Commands.FileSystemProvider.GetParentPathForMetadata(file.FullName, metadata.Drive.Root);
            return string.IsNullOrEmpty(parentPath)
                ? string.Empty
                : System.Management.Automation.LocationGlobber.GetProviderQualifiedPath(parentPath, metadata.Drive.Provider);
        }

        /// <summary>
        /// Gets the child name from the current filesystem path.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <returns>The lexical child name, including root-path handling.</returns>
        [PSObjectMetadataAccessor("PSChildName", 2, 2)]
        private static string GetChildName(System.IO.FileSystemInfo file)
            => Microsoft.PowerShell.Commands.FileSystemProvider.GetChildNameForMetadata(file.FullName);

#if UNIX
        /// <summary>
        /// Gets the captured Unix stat without querying the filesystem again.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <param name="metadata">The captured provider context.</param>
        /// <returns>The stat snapshot stored in the metadata.</returns>
        [PSObjectMetadataAccessor("UnixStat", 3, 4)]
        private static Platform.Unix.CommonStat GetUnixStat(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.UnixStat;
#endif

        /// <summary>
        /// Gets the drive captured when the item was emitted.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <param name="metadata">The captured provider context.</param>
        /// <returns>The original drive reference.</returns>
        [PSObjectMetadataAccessor("PSDrive", 4, 8)]
        private static PSDriveInfo GetDrive(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.Drive;

        /// <summary>
        /// Gets the provider associated with the captured drive.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <param name="metadata">The captured provider context.</param>
        /// <returns>The provider reference from the captured drive.</returns>
        [PSObjectMetadataAccessor("PSProvider", 5)]
        private static ProviderInfo GetProvider(System.IO.FileSystemInfo file, in FileSystemProviderMetadata metadata) => metadata.Drive.Provider;

        /// <summary>
        /// Determines container status from the base object type.
        /// </summary>
        /// <param name="file">The underlying filesystem item.</param>
        /// <returns>The shared boxed Boolean indicating whether the object is a directory.</returns>
        [PSObjectMetadataAccessor("PSIsContainer", 6, 16)]
        private static object GetIsContainer(System.IO.FileSystemInfo file)
            => file is System.IO.DirectoryInfo ? Boxed.True : Boxed.False;
    }
}
