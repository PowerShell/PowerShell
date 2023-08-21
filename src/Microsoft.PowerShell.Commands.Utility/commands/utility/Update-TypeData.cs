// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

using Dbg = System.Management.Automation.Diagnostics;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements update-typeData command.
    /// </summary>
    [Cmdlet(VerbsData.Update, "TypeData", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        DefaultParameterSetName = FileParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097131")]
    public class UpdateTypeDataCommand : UpdateData
    {
        #region dynamic type set

        // Dynamic type set name and type data set name
        private const string DynamicTypeSet = "DynamicTypeSet";
        private const string TypeDataSet = "TypeDataSet";

        private static readonly object s_notSpecified = new();

        private static bool HasBeenSpecified(object obj)
        {
            return !System.Object.ReferenceEquals(obj, s_notSpecified);
        }

        private PSMemberTypes _memberType;
        private bool _isMemberTypeSet = false;
        /// <summary>
        /// The member type of to be added.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        [ValidateSet(System.Management.Automation.Runspaces.TypeData.NoteProperty,
                     System.Management.Automation.Runspaces.TypeData.AliasProperty,
                     System.Management.Automation.Runspaces.TypeData.ScriptProperty,
                     System.Management.Automation.Runspaces.TypeData.CodeProperty,
                     System.Management.Automation.Runspaces.TypeData.ScriptMethod,
                     System.Management.Automation.Runspaces.TypeData.CodeMethod, IgnoreCase = true)]
        public PSMemberTypes MemberType
        {
            get
            {
                return _memberType;
            }

            set
            {
                _memberType = value;
                _isMemberTypeSet = true;
            }
        }

        private string _memberName;
        /// <summary>
        /// The name of the new member.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string MemberName
        {
            get { return _memberName; }

            set { _memberName = value; }
        }

        private object _value1 = s_notSpecified;
        /// <summary>
        /// First value of the new member. The meaning of this value
        /// changes according to the member type.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        public object Value
        {
            get { return _value1; }

            set { _value1 = value; }
        }

        private object _value2;
        /// <summary>
        /// Second value of the new member. The meaning of this value
        /// changes according to the member type.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        public object SecondValue
        {
            get { return _value2; }

            set { _value2 = value; }
        }

        private Type _typeConverter;
        /// <summary>
        /// The type converter to be added.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        public Type TypeConverter
        {
            get { return _typeConverter; }

            set { _typeConverter = value; }
        }

        private Type _typeAdapter;
        /// <summary>
        /// The type adapter to be added.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        public Type TypeAdapter
        {
            get { return _typeAdapter; }

            set { _typeAdapter = value; }
        }

        /// <summary>
        /// SerializationMethod.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string SerializationMethod
        {
            get { return _serializationMethod; }

            set { _serializationMethod = value; }
        }

        /// <summary>
        /// TargetTypeForDeserialization.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        public Type TargetTypeForDeserialization
        {
            get { return _targetTypeForDeserialization; }

            set { _targetTypeForDeserialization = value; }
        }

        /// <summary>
        /// SerializationDepth.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        [ValidateRange(0, int.MaxValue)]
        public int SerializationDepth
        {
            get { return _serializationDepth; }

            set { _serializationDepth = value; }
        }

        /// <summary>
        /// DefaultDisplayProperty.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string DefaultDisplayProperty
        {
            get { return _defaultDisplayProperty; }

            set { _defaultDisplayProperty = value; }
        }

        /// <summary>
        /// InheritPropertySerializationSet.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNull]
        public bool? InheritPropertySerializationSet
        {
            get { return _inheritPropertySerializationSet; }

            set { _inheritPropertySerializationSet = value; }
        }

        /// <summary>
        /// StringSerializationSource.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string StringSerializationSource
        {
            get { return _stringSerializationSource; }

            set { _stringSerializationSource = value; }
        }

        /// <summary>
        /// DefaultDisplayPropertySet.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string[] DefaultDisplayPropertySet
        {
            get { return _defaultDisplayPropertySet; }

            set { _defaultDisplayPropertySet = value; }
        }

        /// <summary>
        /// DefaultKeyPropertySet.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string[] DefaultKeyPropertySet
        {
            get { return _defaultKeyPropertySet; }

            set { _defaultKeyPropertySet = value; }
        }

        /// <summary>
        /// PropertySerializationSet.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [ValidateNotNullOrEmpty]
        public string[] PropertySerializationSet
        {
            get { return _propertySerializationSet; }

            set { _propertySerializationSet = value; }
        }

        // These members are represented as NoteProperty in types.ps1xml
        private string _serializationMethod;
        private Type _targetTypeForDeserialization;
        private int _serializationDepth = int.MinValue;
        private string _defaultDisplayProperty;
        private bool? _inheritPropertySerializationSet;

        // These members are represented as AliasProperty in types.ps1xml
        private string _stringSerializationSource;

        // These members are represented as PropertySet in types.ps1xml
        private string[] _defaultDisplayPropertySet;
        private string[] _defaultKeyPropertySet;
        private string[] _propertySerializationSet;

        private string _typeName;
        /// <summary>
        /// The type name we want to update on.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = DynamicTypeSet)]
        [ArgumentToTypeNameTransformationAttribute]
        [ValidateNotNullOrEmpty]
        public string TypeName
        {
            get { return _typeName; }

            set { _typeName = value; }
        }

        private bool _force = false;
        /// <summary>
        /// True if we should overwrite a possibly existing member.
        /// </summary>
        [Parameter(ParameterSetName = DynamicTypeSet)]
        [Parameter(ParameterSetName = TypeDataSet)]
        public SwitchParameter Force
        {
            get { return _force; }

            set { _force = value; }
        }

        #endregion dynamic type set

        #region strong type data set

        private TypeData[] _typeData;
        /// <summary>
        /// The TypeData instances.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = TypeDataSet)]
        public TypeData[] TypeData
        {
            get { return _typeData; }

            set { _typeData = value; }
        }

        #endregion strong type data set

        /// <summary>
        /// This method verify if the Type Table is shared and cannot be updated.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Context.TypeTable.isShared)
            {
                var ex = new InvalidOperationException(TypesXmlStrings.SharedTypeTableCannotBeUpdated);
                this.ThrowTerminatingError(new ErrorRecord(ex, "CannotUpdateSharedTypeTable", ErrorCategory.InvalidOperation, null));
            }
        }

        /// <summary>
        /// This method implements the ProcessRecord method for update-typeData command.
        /// </summary>
        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case FileParameterSet:
                    ProcessTypeFiles();
                    break;
                case DynamicTypeSet:
                    ProcessDynamicType();
                    break;
                case TypeDataSet:
                    ProcessStrongTypeData();
                    break;
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for update-typeData command.
        /// </summary>
        protected override void EndProcessing()
        {
            this.Context.TypeTable.ClearConsolidatedMembers();
        }

        #region strong typeData

        private void ProcessStrongTypeData()
        {
            string action = UpdateDataStrings.UpdateTypeDataAction;
            string target = UpdateDataStrings.UpdateTypeDataTarget;

            foreach (TypeData item in _typeData)
            {
                // If type contains no members at all, report the error and skip it
                if (!EnsureTypeDataIsNotEmpty(item))
                {
                    continue;
                }

                TypeData type = item.Copy();

                // Set property IsOverride to be true if -Force parameter is specified
                if (_force)
                {
                    type.IsOverride = true;
                }

                string formattedTarget = string.Format(CultureInfo.InvariantCulture, target, type.TypeName);
                if (ShouldProcess(formattedTarget, action))
                {
                    try
                    {
                        var errors = new ConcurrentBag<string>();
                        this.Context.TypeTable.Update(type, errors, false);
                        // Write out errors...
                        if (!errors.IsEmpty)
                        {
                            foreach (string s in errors)
                            {
                                RuntimeException rte = new(s);
                                this.WriteError(new ErrorRecord(rte, "TypesDynamicUpdateException", ErrorCategory.InvalidOperation, null));
                            }
                        }
                        else
                        {
                            // Update successfully, we add the TypeData into cache
                            if (Context.InitialSessionState != null)
                            {
                                Context.InitialSessionState.Types.Add(new SessionStateTypeEntry(type, false));
                            }
                            else
                            {
                                Dbg.Assert(false, "InitialSessionState must be non-null for Update-Typedata to work");
                            }
                        }
                    }
                    catch (RuntimeException e)
                    {
                        this.WriteError(new ErrorRecord(e, "TypesDynamicUpdateException", ErrorCategory.InvalidOperation, null));
                    }
                }
            }
        }

        #endregion strong typeData

        #region dynamic type processing

        /// <summary>
        /// Process the dynamic type update.
        /// </summary>
        private void ProcessDynamicType()
        {
            if (string.IsNullOrWhiteSpace(_typeName))
            {
                ThrowTerminatingError(NewError("TargetTypeNameEmpty", UpdateDataStrings.TargetTypeNameEmpty, _typeName));
            }

            TypeData type = new(_typeName) { IsOverride = _force };

            GetMembers(type.Members);

            if (_typeConverter != null)
            {
                type.TypeConverter = _typeConverter;
            }

            if (_typeAdapter != null)
            {
                type.TypeAdapter = _typeAdapter;
            }

            if (_serializationMethod != null)
            {
                type.SerializationMethod = _serializationMethod;
            }

            if (_targetTypeForDeserialization != null)
            {
                type.TargetTypeForDeserialization = _targetTypeForDeserialization;
            }

            if (_serializationDepth != int.MinValue)
            {
                type.SerializationDepth = (uint)_serializationDepth;
            }

            if (_defaultDisplayProperty != null)
            {
                type.DefaultDisplayProperty = _defaultDisplayProperty;
            }

            if (_inheritPropertySerializationSet != null)
            {
                type.InheritPropertySerializationSet = _inheritPropertySerializationSet.Value;
            }

            if (_stringSerializationSource != null)
            {
                type.StringSerializationSource = _stringSerializationSource;
            }

            if (_defaultDisplayPropertySet != null)
            {
                PropertySetData defaultDisplayPropertySet = new(_defaultDisplayPropertySet);
                type.DefaultDisplayPropertySet = defaultDisplayPropertySet;
            }

            if (_defaultKeyPropertySet != null)
            {
                PropertySetData defaultKeyPropertySet = new(_defaultKeyPropertySet);
                type.DefaultKeyPropertySet = defaultKeyPropertySet;
            }

            if (_propertySerializationSet != null)
            {
                PropertySetData propertySerializationSet = new(_propertySerializationSet);
                type.PropertySerializationSet = propertySerializationSet;
            }

            // If the type contains no members at all, report the error and return
            if (!EnsureTypeDataIsNotEmpty(type))
            {
                return;
            }

            // Load the resource strings
            string action = UpdateDataStrings.UpdateTypeDataAction;
            string target = UpdateDataStrings.UpdateTypeDataTarget;

            string formattedTarget = string.Format(CultureInfo.InvariantCulture, target, _typeName);

            if (ShouldProcess(formattedTarget, action))
            {
                try
                {
                    var errors = new ConcurrentBag<string>();
                    this.Context.TypeTable.Update(type, errors, false);
                    // Write out errors...
                    if (!errors.IsEmpty)
                    {
                        foreach (string s in errors)
                        {
                            RuntimeException rte = new(s);
                            this.WriteError(new ErrorRecord(rte, "TypesDynamicUpdateException", ErrorCategory.InvalidOperation, null));
                        }
                    }
                    else
                    {
                        // Update successfully, we add the TypeData into cache
                        if (Context.InitialSessionState != null)
                        {
                            Context.InitialSessionState.Types.Add(new SessionStateTypeEntry(type, false));
                        }
                        else
                        {
                            Dbg.Assert(false, "InitialSessionState must be non-null for Update-Typedata to work");
                        }
                    }
                }
                catch (RuntimeException e)
                {
                    this.WriteError(new ErrorRecord(e, "TypesDynamicUpdateException", ErrorCategory.InvalidOperation, null));
                }
            }
        }

        /// <summary>
        /// Get the members for the TypeData.
        /// </summary>
        /// <returns></returns>
        private void GetMembers(Dictionary<string, TypeMemberData> members)
        {
            if (!_isMemberTypeSet)
            {
                // If the MemberType is not specified, the MemberName, Value, and SecondValue parameters
                // should not be specified either
                if (_memberName != null || HasBeenSpecified(_value1) || _value2 != null)
                {
                    ThrowTerminatingError(NewError("MemberTypeIsMissing", UpdateDataStrings.MemberTypeIsMissing, null));
                }

                return;
            }

            switch (MemberType)
            {
                case PSMemberTypes.NoteProperty:
                    NotePropertyData note = GetNoteProperty();
                    members.Add(note.Name, note);
                    break;
                case PSMemberTypes.AliasProperty:
                    AliasPropertyData alias = GetAliasProperty();
                    members.Add(alias.Name, alias);
                    break;
                case PSMemberTypes.ScriptProperty:
                    ScriptPropertyData scriptProperty = GetScriptProperty();
                    members.Add(scriptProperty.Name, scriptProperty);
                    break;
                case PSMemberTypes.CodeProperty:
                    CodePropertyData codeProperty = GetCodeProperty();
                    members.Add(codeProperty.Name, codeProperty);
                    break;
                case PSMemberTypes.ScriptMethod:
                    ScriptMethodData scriptMethod = GetScriptMethod();
                    members.Add(scriptMethod.Name, scriptMethod);
                    break;
                case PSMemberTypes.CodeMethod:
                    CodeMethodData codeMethod = GetCodeMethod();
                    members.Add(codeMethod.Name, codeMethod);
                    break;
                default:
                    ThrowTerminatingError(NewError("CannotUpdateMemberType", UpdateDataStrings.CannotUpdateMemberType, null, _memberType.ToString()));
                    break;
            }
        }

        private static T GetParameterType<T>(object sourceValue)
        {
            return (T)LanguagePrimitives.ConvertTo(sourceValue, typeof(T), CultureInfo.InvariantCulture);
        }

        private void EnsureMemberNameHasBeenSpecified()
        {
            if (string.IsNullOrEmpty(_memberName))
            {
                ThrowTerminatingError(NewError("MemberNameShouldBeSpecified", UpdateDataStrings.ShouldBeSpecified, null, "MemberName", _memberType));
            }
        }

        private void EnsureValue1HasBeenSpecified()
        {
            if (!HasBeenSpecified(_value1))
            {
                ThrowTerminatingError(NewError("ValueShouldBeSpecified", UpdateDataStrings.ShouldBeSpecified, null, "Value", _memberType));
            }
        }

        private void EnsureValue1NotNullOrEmpty()
        {
            if (_value1 is string)
            {
                if (string.IsNullOrEmpty((string)_value1))
                {
                    ThrowTerminatingError(NewError("ValueShouldBeSpecified", UpdateDataStrings.ShouldNotBeNull, null, "Value", _memberType));
                }

                return;
            }

            if (_value1 == null)
            {
                ThrowTerminatingError(NewError("ValueShouldBeSpecified", UpdateDataStrings.ShouldNotBeNull, null, "Value", _memberType));
            }
        }

        private void EnsureValue2HasNotBeenSpecified()
        {
            if (_value2 != null)
            {
                ThrowTerminatingError(NewError("SecondValueShouldNotBeSpecified", UpdateDataStrings.ShouldNotBeSpecified, null, "SecondValue", _memberType));
            }
        }

        private void EnsureValue1AndValue2AreNotBothNull()
        {
            if (_value1 == null && _value2 == null)
            {
                ThrowTerminatingError(NewError("ValueAndSecondValueAreNotBothNull", UpdateDataStrings.Value1AndValue2AreNotBothNull, null, _memberType));
            }
        }

        /// <summary>
        /// Check if the TypeData instance contains no members.
        /// </summary>
        /// <param name="typeData"></param>
        /// <returns>False if empty, true if not.</returns>
        private bool EnsureTypeDataIsNotEmpty(TypeData typeData)
        {
            if (typeData.Members.Count == 0 && typeData.StandardMembers.Count == 0
                && typeData.TypeConverter == null && typeData.TypeAdapter == null
                && typeData.DefaultDisplayPropertySet == null
                && typeData.DefaultKeyPropertySet == null
                && typeData.PropertySerializationSet == null)
            {
                this.WriteError(NewError("TypeDataEmpty", UpdateDataStrings.TypeDataEmpty, null, typeData.TypeName));
                return false;
            }

            return true;
        }

        private NotePropertyData GetNoteProperty()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue2HasNotBeenSpecified();
            return new NotePropertyData(_memberName, _value1);
        }

        private AliasPropertyData GetAliasProperty()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1NotNullOrEmpty();

            AliasPropertyData alias;
            string referencedName = GetParameterType<string>(_value1);
            if (_value2 != null)
            {
                Type type = GetParameterType<Type>(_value2);
                alias = new AliasPropertyData(_memberName, referencedName, type);
                return alias;
            }

            alias = new AliasPropertyData(_memberName, referencedName);
            return alias;
        }

        private ScriptPropertyData GetScriptProperty()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            ScriptBlock value1ScriptBlock = null;
            if (_value1 != null)
            {
                value1ScriptBlock = GetParameterType<ScriptBlock>(_value1);
            }

            ScriptBlock value2ScriptBlock = null;
            if (_value2 != null)
            {
                value2ScriptBlock = GetParameterType<ScriptBlock>(_value2);
            }

            ScriptPropertyData scriptProperty = new(_memberName, value1ScriptBlock, value2ScriptBlock);
            return scriptProperty;
        }

        private CodePropertyData GetCodeProperty()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            MethodInfo value1CodeReference = null;
            if (_value1 != null)
            {
                value1CodeReference = GetParameterType<MethodInfo>(_value1);
            }

            MethodInfo value2CodeReference = null;
            if (_value2 != null)
            {
                value2CodeReference = GetParameterType<MethodInfo>(_value2);
            }

            CodePropertyData codeProperty = new(_memberName, value1CodeReference, value2CodeReference);
            return codeProperty;
        }

        private ScriptMethodData GetScriptMethod()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue2HasNotBeenSpecified();

            ScriptBlock method = GetParameterType<ScriptBlock>(_value1);
            ScriptMethodData scriptMethod = new(_memberName, method);
            return scriptMethod;
        }

        private CodeMethodData GetCodeMethod()
        {
            EnsureMemberNameHasBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue2HasNotBeenSpecified();

            MethodInfo codeReference = GetParameterType<MethodInfo>(_value1);
            CodeMethodData codeMethod = new(_memberName, codeReference);
            return codeMethod;
        }

        /// <summary>
        /// Generate error record.
        /// </summary>
        /// <param name="errorId"></param>
        /// <param name="template"></param>
        /// <param name="targetObject"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private static ErrorRecord NewError(string errorId, string template, object targetObject, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, template, args);
            ErrorRecord errorRecord = new(
                new InvalidOperationException(message),
                errorId,
                ErrorCategory.InvalidOperation,
                targetObject);
            return errorRecord;
        }

        #endregion dynamic type processing

        #region type files processing

        private void ProcessTypeFiles()
        {
            Collection<string> prependPathTotal = Glob(this.PrependPath, "TypesPrependPathException", this);
            Collection<string> appendPathTotal = Glob(this.AppendPath, "TypesAppendPathException", this);

            // There are file path input but they did not pass the validation in the method Glob
            if ((PrependPath.Length > 0 || AppendPath.Length > 0) &&
                prependPathTotal.Count == 0 && appendPathTotal.Count == 0)
            { return; }

            string action = UpdateDataStrings.UpdateTypeDataAction;
            // Load the resource once and format it whenever a new target
            // filename is available
            string target = UpdateDataStrings.UpdateTarget;

            if (Context.InitialSessionState != null)
            {
                // This hashSet is to detect if there are duplicate type files
                var fullFileNameHash = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                var newTypes = new Collection<SessionStateTypeEntry>();

                for (int i = prependPathTotal.Count - 1; i >= 0; i--)
                {
                    string formattedTarget = string.Format(CultureInfo.InvariantCulture, target, prependPathTotal[i]);
                    string resolvedPath = ModuleCmdletBase.ResolveRootedFilePath(prependPathTotal[i], Context) ?? prependPathTotal[i];

                    if (ShouldProcess(formattedTarget, action))
                    {
                        if (!fullFileNameHash.Contains(resolvedPath))
                        {
                            fullFileNameHash.Add(resolvedPath);
                            newTypes.Add(new SessionStateTypeEntry(prependPathTotal[i]));
                        }
                    }
                }

                // Copy everything from context's TypeTable to newTypes
                foreach (var entry in Context.InitialSessionState.Types)
                {
                    if (entry.FileName != null)
                    {
                        string resolvedPath = ModuleCmdletBase.ResolveRootedFilePath(entry.FileName, Context) ?? entry.FileName;
                        if (!fullFileNameHash.Contains(resolvedPath))
                        {
                            fullFileNameHash.Add(resolvedPath);
                            newTypes.Add(entry);
                        }
                    }
                    else
                    {
                        newTypes.Add(entry);
                    }
                }

                foreach (string appendPathTotalItem in appendPathTotal)
                {
                    string formattedTarget = string.Format(CultureInfo.InvariantCulture, target, appendPathTotalItem);
                    string resolvedPath = ModuleCmdletBase.ResolveRootedFilePath(appendPathTotalItem, Context) ?? appendPathTotalItem;

                    if (ShouldProcess(formattedTarget, action))
                    {
                        if (!fullFileNameHash.Contains(resolvedPath))
                        {
                            fullFileNameHash.Add(resolvedPath);
                            newTypes.Add(new SessionStateTypeEntry(appendPathTotalItem));
                        }
                    }
                }

                Context.InitialSessionState.Types.Clear();
                Context.TypeTable.Clear();
                var errors = new ConcurrentBag<string>();

                foreach (SessionStateTypeEntry sste in newTypes)
                {
                    try
                    {
                        if (sste.TypeTable != null)
                        {
                            var ex = new PSInvalidOperationException(UpdateDataStrings.CannotUpdateTypeWithTypeTable);
                            this.WriteError(new ErrorRecord(ex, "CannotUpdateTypeWithTypeTable", ErrorCategory.InvalidOperation, null));
                            continue;
                        }
                        else if (sste.FileName != null)
                        {
                            Context.TypeTable.Update(sste.FileName, sste.FileName, errors, Context.AuthorizationManager, Context.InitialSessionState.Host, out _);
                        }
                        else
                        {
                            Context.TypeTable.Update(sste.TypeData, errors, sste.IsRemove);
                        }
                    }
                    catch (RuntimeException ex)
                    {
                        this.WriteError(new ErrorRecord(ex, "TypesXmlUpdateException", ErrorCategory.InvalidOperation, null));
                    }

                    Context.InitialSessionState.Types.Add(sste);

                    // Write out any errors...
                    if (!errors.IsEmpty)
                    {
                        foreach (string s in errors)
                        {
                            RuntimeException rte = new(s);
                            this.WriteError(new ErrorRecord(rte, "TypesXmlUpdateException", ErrorCategory.InvalidOperation, null));
                        }

                        errors = new ConcurrentBag<string>();
                    }
                }
            }
            else
            {
                Dbg.Assert(false, "InitialSessionState must be non-null for Update-Typedata to work");
            }
        }

        #endregion type files processing
    }

    /// <summary>
    /// This class implements update-typeData command.
    /// </summary>
    [Cmdlet(VerbsData.Update, "FormatData", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Low,
        DefaultParameterSetName = FileParameterSet, HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097135")]
    public class UpdateFormatDataCommand : UpdateData
    {
        /// <summary>
        /// This method verify if the Format database manager is shared and cannot be updated.
        /// </summary>
        protected override void BeginProcessing()
        {
            if (Context.FormatDBManager.isShared)
            {
                var ex = new InvalidOperationException(FormatAndOutXmlLoadingStrings.SharedFormatTableCannotBeUpdated);
                this.ThrowTerminatingError(new ErrorRecord(ex, "CannotUpdateSharedFormatTable", ErrorCategory.InvalidOperation, null));
            }
        }

        /// <summary>
        /// This method implements the ProcessRecord method for update-FormatData command.
        /// </summary>
        protected override void ProcessRecord()
        {
            Collection<string> prependPathTotal = Glob(this.PrependPath, "FormatPrependPathException", this);
            Collection<string> appendPathTotal = Glob(this.AppendPath, "FormatAppendPathException", this);

            // There are file path input but they did not pass the validation in the method Glob
            if ((PrependPath.Length > 0 || AppendPath.Length > 0) &&
                prependPathTotal.Count == 0 && appendPathTotal.Count == 0)
            { return; }

            string action = UpdateDataStrings.UpdateFormatDataAction;

            // Load the resource once and format it whenever a new target
            // filename is available
            string target = UpdateDataStrings.UpdateTarget;

            if (Context.InitialSessionState != null)
            {
                if (Context.InitialSessionState.DisableFormatUpdates)
                {
                    throw new PSInvalidOperationException(UpdateDataStrings.FormatUpdatesDisabled);
                }

                // This hashSet is to detect if there are duplicate format files
                var fullFileNameHash = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
                var newFormats = new Collection<SessionStateFormatEntry>();

                for (int i = prependPathTotal.Count - 1; i >= 0; i--)
                {
                    string formattedTarget = string.Format(CultureInfo.CurrentCulture, target, prependPathTotal[i]);

                    if (ShouldProcess(formattedTarget, action))
                    {
                        if (!fullFileNameHash.Contains(prependPathTotal[i]))
                        {
                            fullFileNameHash.Add(prependPathTotal[i]);
                            newFormats.Add(new SessionStateFormatEntry(prependPathTotal[i]));
                        }
                    }
                }

                // Always add InitialSessionState.Formats to the new list
                foreach (SessionStateFormatEntry entry in Context.InitialSessionState.Formats)
                {
                    if (entry.FileName != null)
                    {
                        if (!fullFileNameHash.Contains(entry.FileName))
                        {
                            fullFileNameHash.Add(entry.FileName);
                            newFormats.Add(entry);
                        }
                    }
                    else
                    {
                        newFormats.Add(entry);
                    }
                }

                foreach (string appendPathTotalItem in appendPathTotal)
                {
                    string formattedTarget = string.Format(CultureInfo.CurrentCulture, target, appendPathTotalItem);

                    if (ShouldProcess(formattedTarget, action))
                    {
                        if (!fullFileNameHash.Contains(appendPathTotalItem))
                        {
                            fullFileNameHash.Add(appendPathTotalItem);
                            newFormats.Add(new SessionStateFormatEntry(appendPathTotalItem));
                        }
                    }
                }

                var originalFormats = Context.InitialSessionState.Formats;
                try
                {
                    // Always rebuild the format information
                    Context.InitialSessionState.Formats.Clear();
                    var entries = new Collection<PSSnapInTypeAndFormatErrors>();

                    // Now update the formats...
                    foreach (SessionStateFormatEntry ssfe in newFormats)
                    {
                        string name = ssfe.FileName;
                        PSSnapInInfo snapin = ssfe.PSSnapIn;
                        if (snapin != null && !string.IsNullOrEmpty(snapin.Name))
                        {
                            name = snapin.Name;
                        }

                        if (ssfe.Formattable != null)
                        {
                            var ex = new PSInvalidOperationException(UpdateDataStrings.CannotUpdateFormatWithFormatTable);
                            this.WriteError(new ErrorRecord(ex, "CannotUpdateFormatWithFormatTable", ErrorCategory.InvalidOperation, null));
                            continue;
                        }
                        else if (ssfe.FormatData != null)
                        {
                            entries.Add(new PSSnapInTypeAndFormatErrors(name, ssfe.FormatData));
                        }
                        else
                        {
                            entries.Add(new PSSnapInTypeAndFormatErrors(name, ssfe.FileName));
                        }

                        Context.InitialSessionState.Formats.Add(ssfe);
                    }

                    if (entries.Count > 0)
                    {
                        Context.FormatDBManager.UpdateDataBase(entries, this.Context.AuthorizationManager, this.Context.EngineHostInterface, false);
                        FormatAndTypeDataHelper.ThrowExceptionOnError("ErrorsUpdatingFormats",
                            null,
                            entries,
                            FormatAndTypeDataHelper.Category.Formats);
                    }
                }
                catch (RuntimeException e)
                {
                    // revert Formats if there is a failure
                    Context.InitialSessionState.Formats.Clear();
                    Context.InitialSessionState.Formats.Add(originalFormats);
                    this.WriteError(new ErrorRecord(e, "FormatXmlUpdateException", ErrorCategory.InvalidOperation, null));
                }
            }
            else
            {
                Dbg.Assert(false, "InitialSessionState must be non-null for Update-FormatData to work");
            }
        }
    }

    /// <summary>
    /// Remove-TypeData cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "TypeData", SupportsShouldProcess = true, DefaultParameterSetName = RemoveTypeDataSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096622")]
    public class RemoveTypeDataCommand : PSCmdlet
    {
        private const string RemoveTypeSet = "RemoveTypeSet";
        private const string RemoveFileSet = "RemoveFileSet";
        private const string RemoveTypeDataSet = "RemoveTypeDataSet";

        private string _typeName;

        /// <summary>
        /// The target type to remove.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = RemoveTypeSet)]
        [ArgumentToTypeNameTransformationAttribute]
        [ValidateNotNullOrEmpty]
        public string TypeName
        {
            get { return _typeName; }

            set { _typeName = value; }
        }

        private string[] _typeFiles;

        /// <summary>
        /// The type xml file to remove from the cache.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Cmdlets use arrays for parameters.")]
        [Parameter(Mandatory = true, ParameterSetName = RemoveFileSet)]
        [ValidateNotNullOrEmpty]
        public string[] Path
        {
            get { return _typeFiles; }

            set { _typeFiles = value; }
        }

        private TypeData _typeData;

        /// <summary>
        /// The TypeData to remove.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = RemoveTypeDataSet)]
        public TypeData TypeData
        {
            get { return _typeData; }

            set { _typeData = value; }
        }

        private static void ConstructFileToIndexMap(string fileName, int index, Dictionary<string, List<int>> fileNameToIndexMap)
        {
            List<int> indexList;
            if (fileNameToIndexMap.TryGetValue(fileName, out indexList))
            {
                indexList.Add(index);
            }
            else
            {
                fileNameToIndexMap[fileName] = new List<int> { index };
            }
        }

        /// <summary>
        /// This method implements the ProcessRecord method for Remove-TypeData command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (ParameterSetName == RemoveFileSet)
            {
                // Load the resource strings
                string removeFileAction = UpdateDataStrings.RemoveTypeFileAction;
                string removeFileTarget = UpdateDataStrings.UpdateTarget;

                Collection<string> typeFileTotal = UpdateData.Glob(_typeFiles, "TypePathException", this);
                if (typeFileTotal.Count == 0)
                {
                    return;
                }

                // Key of the map is the name of the file that is in the cache. Value of the map is a index list. Duplicate files might
                // exist in the cache because the user can add arbitrary files to the cache by $host.Runspace.InitialSessionState.Types.Add()
                Dictionary<string, List<int>> fileToIndexMap = new(StringComparer.OrdinalIgnoreCase);
                List<int> indicesToRemove = new();

                if (Context.InitialSessionState != null)
                {
                    for (int index = 0; index < Context.InitialSessionState.Types.Count; index++)
                    {
                        string fileName = Context.InitialSessionState.Types[index].FileName;
                        if (fileName == null)
                        {
                            continue;
                        }

                        // Resolving the file path because the path to the types file in module manifest is now specified as
                        // ..\..\types.ps1xml which expands to C:\Windows\System32\WindowsPowerShell\v1.0\Modules\Microsoft.PowerShell.Core\..\..\types.ps1xml
                        fileName = ModuleCmdletBase.ResolveRootedFilePath(fileName, Context) ?? fileName;
                        ConstructFileToIndexMap(fileName, index, fileToIndexMap);
                    }
                }

                foreach (string typeFile in typeFileTotal)
                {
                    string removeFileFormattedTarget = string.Format(CultureInfo.InvariantCulture, removeFileTarget, typeFile);
                    if (ShouldProcess(removeFileFormattedTarget, removeFileAction))
                    {
                        List<int> indexList;
                        if (fileToIndexMap.TryGetValue(typeFile, out indexList))
                        {
                            indicesToRemove.AddRange(indexList);
                        }
                        else
                        {
                            this.WriteError(NewError("TypeFileNotExistsInCurrentSession", UpdateDataStrings.TypeFileNotExistsInCurrentSession, null, typeFile));
                        }
                    }
                }

                if (indicesToRemove.Count > 0)
                {
                    indicesToRemove.Sort();
                    for (int i = indicesToRemove.Count - 1; i >= 0; i--)
                    {
                        Context.InitialSessionState?.Types.RemoveItem(indicesToRemove[i]);
                    }

                    try
                    {
                        if (Context.InitialSessionState != null)
                        {
                            bool oldRefreshTypeFormatSetting = Context.InitialSessionState.RefreshTypeAndFormatSetting;
                            try
                            {
                                Context.InitialSessionState.RefreshTypeAndFormatSetting = true;
                                Context.InitialSessionState.UpdateTypes(Context, false);
                            }
                            finally
                            {
                                Context.InitialSessionState.RefreshTypeAndFormatSetting = oldRefreshTypeFormatSetting;
                            }
                        }
                    }
                    catch (RuntimeException ex)
                    {
                        this.WriteError(new ErrorRecord(ex, "TypesFileRemoveException", ErrorCategory.InvalidOperation, null));
                    }
                }

                return;
            }

            // Load the resource strings
            string removeTypeAction = UpdateDataStrings.RemoveTypeDataAction;
            string removeTypeTarget = UpdateDataStrings.RemoveTypeDataTarget;
            string typeNameToRemove = null;

            if (ParameterSetName == RemoveTypeDataSet)
            {
                typeNameToRemove = _typeData.TypeName;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_typeName))
                {
                    ThrowTerminatingError(NewError("TargetTypeNameEmpty", UpdateDataStrings.TargetTypeNameEmpty, _typeName));
                }

                typeNameToRemove = _typeName;
            }

            Dbg.Assert(!string.IsNullOrEmpty(typeNameToRemove), "TypeNameToRemove should be not null and not empty at this point");
            TypeData type = new(typeNameToRemove);
            string removeTypeFormattedTarget = string.Format(CultureInfo.InvariantCulture, removeTypeTarget, typeNameToRemove);

            if (ShouldProcess(removeTypeFormattedTarget, removeTypeAction))
            {
                try
                {
                    var errors = new ConcurrentBag<string>();
                    Context.TypeTable.Update(type, errors, true);
                    // Write out errors...
                    if (!errors.IsEmpty)
                    {
                        foreach (string s in errors)
                        {
                            RuntimeException rte = new(s);
                            this.WriteError(new ErrorRecord(rte, "TypesDynamicRemoveException", ErrorCategory.InvalidOperation, null));
                        }
                    }
                    else
                    {
                        // Type is removed successfully, add it into the cache
                        if (Context.InitialSessionState != null)
                        {
                            Context.InitialSessionState.Types.Add(new SessionStateTypeEntry(type, true));
                        }
                        else
                        {
                            Dbg.Assert(false, "InitialSessionState must be non-null for Remove-Typedata to work");
                        }
                    }
                }
                catch (RuntimeException e)
                {
                    this.WriteError(new ErrorRecord(e, "TypesDynamicRemoveException", ErrorCategory.InvalidOperation, null));
                }
            }
        }

        /// <summary>
        /// This method implements the EndProcessing method for Remove-TypeData command.
        /// </summary>
        protected override void EndProcessing()
        {
            this.Context.TypeTable.ClearConsolidatedMembers();
        }

        private static ErrorRecord NewError(string errorId, string template, object targetObject, params object[] args)
        {
            string message = string.Format(CultureInfo.CurrentCulture, template, args);
            ErrorRecord errorRecord = new(
                new InvalidOperationException(message),
                errorId,
                ErrorCategory.InvalidOperation,
                targetObject);
            return errorRecord;
        }
    }

    /// <summary>
    /// Get-TypeData cmdlet.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "TypeData", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097018")]
    [OutputType(typeof(System.Management.Automation.PSObject))]
    public class GetTypeDataCommand : PSCmdlet
    {
        private WildcardPattern[] _filter;

        /// <summary>
        /// Get Formatting information only for the specified typename.
        /// </summary>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        [ValidateNotNullOrEmpty]
        [Parameter(Position = 0, ValueFromPipeline = true)]
        public string[] TypeName { get; set; }

        private void ValidateTypeName()
        {
            if (TypeName == null)
            {
                _filter = new WildcardPattern[] { WildcardPattern.Get("*", WildcardOptions.None) };
                return;
            }

            var typeNames = new List<string>();
            var exception = new InvalidOperationException(UpdateDataStrings.TargetTypeNameEmpty);
            foreach (string typeName in TypeName)
            {
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    WriteError(
                        new ErrorRecord(
                            exception,
                            "TargetTypeNameEmpty",
                            ErrorCategory.InvalidOperation,
                            typeName));
                    continue;
                }

                Type type;
                string typeNameInUse = typeName;
                // Respect the type shortcut
                if (LanguagePrimitives.TryConvertTo(typeNameInUse, out type))
                {
                    typeNameInUse = type.FullName;
                }

                typeNames.Add(typeNameInUse);
            }

            _filter = new WildcardPattern[typeNames.Count];
            for (int i = 0; i < _filter.Length; i++)
            {
                _filter[i] = WildcardPattern.Get(typeNames[i],
                    WildcardOptions.Compiled | WildcardOptions.CultureInvariant | WildcardOptions.IgnoreCase);
            }
        }

        /// <summary>
        /// Takes out the content from the database and writes it out.
        /// </summary>
        protected override void ProcessRecord()
        {
            ValidateTypeName();

            Dictionary<string, TypeData> alltypes = Context.TypeTable.GetAllTypeData();
            Collection<TypeData> typedefs = new();

            foreach (string type in alltypes.Keys)
            {
                foreach (WildcardPattern pattern in _filter)
                {
                    if (pattern.IsMatch(type))
                    {
                        typedefs.Add(alltypes[type]);
                        break;
                    }
                }
            }

            // write out all the available type definitions
            foreach (TypeData typedef in typedefs)
            {
                WriteObject(typedef);
            }
        }
    }

    /// <summary>
    /// To make it easier to specify a TypeName, we add an ArgumentTransformationAttribute here.
    /// * string: return the string
    /// * Type: return the Type.ToString()
    /// * instance: return instance.GetType().ToString() .
    /// </summary>
    internal sealed class ArgumentToTypeNameTransformationAttribute : ArgumentTransformationAttribute
    {
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            string typeName;
            object target = PSObject.Base(inputData);
            if (target is Type)
            {
                typeName = ((Type)target).FullName;
            }
            else if (target is string)
            {
                // Respect the type shortcut
                Type type;
                typeName = (string)target;
                if (LanguagePrimitives.TryConvertTo(typeName, out type))
                {
                    typeName = type.FullName;
                }
            }
            else if (target is TypeData)
            {
                typeName = ((TypeData)target).TypeName;
            }
            else
            {
                typeName = target.GetType().FullName;
            }

            return typeName;
        }
    }
}
