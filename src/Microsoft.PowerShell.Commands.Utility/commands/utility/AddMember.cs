// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Internal;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements get-member command.
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Member", DefaultParameterSetName = "TypeNameSet",
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=113280", RemotingCapability = RemotingCapability.None)]
    public class AddMemberCommand : PSCmdlet
    {
        private static object s_notSpecified = new object();
        private static bool HasBeenSpecified(object obj)
        {
            return !System.Object.ReferenceEquals(obj, s_notSpecified);
        }

        private PSObject _inputObject;
        /// <summary>
        /// The object to add a member to.
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "MemberSet")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "TypeNameSet")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = NotePropertyMultiMemberSet)]
        public PSObject InputObject
        {
            set { _inputObject = value; }

            get { return _inputObject; }
        }

        private PSMemberTypes _memberType;
        /// <summary>
        /// The member type of to be added.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "MemberSet")]
        [Alias("Type")]
        public PSMemberTypes MemberType
        {
            set { _memberType = value; }

            get { return _memberType; }
        }

        private string _memberName;
        /// <summary>
        /// The name of the new member.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "MemberSet")]
        public string Name
        {
            set { _memberName = value; }

            get { return _memberName; }
        }

        private object _value1 = s_notSpecified;
        /// <summary>
        /// First value of the new member. The meaning of this value changes according to the member type.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "MemberSet")]
        public object Value
        {
            set { _value1 = value; }

            get { return _value1; }
        }

        private object _value2 = s_notSpecified;
        /// <summary>
        /// Second value of the new member. The meaning of this value changes according to the member type.
        /// </summary>
        [Parameter(Position = 3, ParameterSetName = "MemberSet")]
        public object SecondValue
        {
            set { _value2 = value; }

            get { return _value2; }
        }

        private string _typeName;
        /// <summary>
        /// Add new type name to the specified object for TypeNameSet.
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "TypeNameSet")]
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        [ValidateNotNullOrEmpty]
        public string TypeName
        {
            set { _typeName = value; }

            get { return _typeName; }
        }

        private bool _force;
        /// <summary>
        /// True if we should overwrite a possibly existing member.
        /// </summary>
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        public SwitchParameter Force
        {
            set { _force = value; }

            get { return _force; }
        }

        private bool _passThru /* = false */;

        /// <summary>
        /// Gets or sets the parameter -passThru which states output from the command should be placed in the pipeline.
        /// </summary>
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = "TypeNameSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        public SwitchParameter PassThru
        {
            set { _passThru = value; }

            get { return _passThru; }
        }

        #region Simplifying NoteProperty Declaration

        private const string NotePropertySingleMemberSet = "NotePropertySingleMemberSet";
        private const string NotePropertyMultiMemberSet = "NotePropertyMultiMemberSet";

        private string _notePropertyName;
        /// <summary>
        /// The name of the new NoteProperty member.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = NotePropertySingleMemberSet)]
        [ValidateNotePropertyNameAttribute()]
        [NotePropertyTransformationAttribute()]
        [ValidateNotNullOrEmpty]
        public string NotePropertyName
        {
            set { _notePropertyName = value; }

            get { return _notePropertyName; }
        }

        private object _notePropertyValue;
        /// <summary>
        /// The value of the new NoteProperty member.
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = NotePropertySingleMemberSet)]
        [AllowNull]
        public object NotePropertyValue
        {
            set { _notePropertyValue = value; }

            get { return _notePropertyValue; }
        }

        // Use IDictionary to support both Hashtable and OrderedHashtable
        private IDictionary _property;

        /// <summary>
        /// The NoteProperty members to be set.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = NotePropertyMultiMemberSet)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public IDictionary NotePropertyMembers
        {
            get { return _property; }

            set { _property = value; }
        }

        #endregion Simplifying NoteProperty Declaration

        private static object GetParameterType(object sourceValue, Type destinationType)
        {
            return LanguagePrimitives.ConvertTo(sourceValue, destinationType, CultureInfo.InvariantCulture);
        }

        private void EnsureValue1AndValue2AreNotBothNull()
        {
            if (_value1 == null &&
               (_value2 == null || !HasBeenSpecified(_value2)))
            {
                ThrowTerminatingError(NewError("Value1AndValue2AreNotBothNull", "Value1AndValue2AreNotBothNull", null, _memberType));
            }
        }

        private void EnsureValue1IsNotNull()
        {
            if (_value1 == null)
            {
                ThrowTerminatingError(NewError("Value1ShouldNotBeNull", "Value1ShouldNotBeNull", null, _memberType));
            }
        }

        private void EnsureValue2IsNotNull()
        {
            if (_value2 == null)
            {
                ThrowTerminatingError(NewError("Value2ShouldNotBeNull", "Value2ShouldNotBeNull", null, _memberType));
            }
        }

        private void EnsureValue1HasBeenSpecified()
        {
            if (!HasBeenSpecified(_value1))
            {
                Collection<FieldDescription> fdc = new Collection<FieldDescription>();
                fdc.Add(new FieldDescription("Value"));
                string prompt = StringUtil.Format(AddMember.Value1Prompt, _memberType);
                Dictionary<string, PSObject> result = this.Host.UI.Prompt(prompt, null, fdc);
                if (result != null)
                {
                    _value1 = result["Value"].BaseObject;
                }
            }
        }

        private void EnsureValue2HasNotBeenSpecified()
        {
            if (HasBeenSpecified(_value2))
            {
                ThrowTerminatingError(NewError("Value2ShouldNotBeSpecified", "Value2ShouldNotBeSpecified", null, _memberType));
            }
        }

        private PSMemberInfo GetAliasProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();

            string value1Str = (string)GetParameterType(_value1, typeof(string));
            if (HasBeenSpecified(_value2))
            {
                EnsureValue2IsNotNull();
                Type value2Type = (Type)GetParameterType(_value2, typeof(Type));
                return new PSAliasProperty(_memberName, value1Str, value2Type);
            }

            return new PSAliasProperty(_memberName, value1Str);
        }

        private PSMemberInfo GetCodeMethod()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();
            EnsureValue2HasNotBeenSpecified();
            MethodInfo value1MethodInfo = (MethodInfo)GetParameterType(_value1, typeof(MethodInfo));
            return new PSCodeMethod(_memberName, value1MethodInfo);
        }

        private PSMemberInfo GetCodeProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            MethodInfo value1MethodInfo = null;
            if (HasBeenSpecified(_value1))
            {
                value1MethodInfo = (MethodInfo)GetParameterType(_value1, typeof(MethodInfo));
            }

            MethodInfo value2MethodInfo = null;
            if (HasBeenSpecified(_value2))
            {
                value2MethodInfo = (MethodInfo)GetParameterType(_value2, typeof(MethodInfo));
            }

            return new PSCodeProperty(_memberName, value1MethodInfo, value2MethodInfo);
        }

        private PSMemberInfo GetMemberSet()
        {
            EnsureValue2HasNotBeenSpecified();
            if (_value1 == null || !HasBeenSpecified(_value1))
            {
                return new PSMemberSet(_memberName);
            }

            Collection<PSMemberInfo> value1Collection =
                (Collection<PSMemberInfo>)GetParameterType(_value1, typeof(Collection<PSMemberInfo>));
            return new PSMemberSet(_memberName, value1Collection);
        }

        private PSMemberInfo GetNoteProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue2HasNotBeenSpecified();
            return new PSNoteProperty(_memberName, _value1);
        }

        private PSMemberInfo GetPropertySet()
        {
            EnsureValue2HasNotBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();
            Collection<string> value1Collection =
                (Collection<string>)GetParameterType(_value1, typeof(Collection<string>));
            return new PSPropertySet(_memberName, value1Collection);
        }

        private PSMemberInfo GetScriptMethod()
        {
            EnsureValue2HasNotBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();
            ScriptBlock value1ScriptBlock = (ScriptBlock)GetParameterType(_value1, typeof(ScriptBlock));
            return new PSScriptMethod(_memberName, value1ScriptBlock);
        }

        private PSMemberInfo GetScriptProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            ScriptBlock value1ScriptBlock = null;
            if (HasBeenSpecified(_value1))
            {
                value1ScriptBlock = (ScriptBlock)GetParameterType(_value1, typeof(ScriptBlock));
            }

            ScriptBlock value2ScriptBlock = null;
            if (HasBeenSpecified(_value2))
            {
                value2ScriptBlock = (ScriptBlock)GetParameterType(_value2, typeof(ScriptBlock));
            }

            return new PSScriptProperty(_memberName, value1ScriptBlock, value2ScriptBlock);
        }

        /// <summary>
        /// This method implements the ProcessRecord method for add-member command.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (_typeName != null && string.IsNullOrWhiteSpace(_typeName))
            {
                ThrowTerminatingError(NewError("TypeNameShouldNotBeEmpty", "TypeNameShouldNotBeEmpty", _typeName));
            }

            if (ParameterSetName == "TypeNameSet")
            {
                UpdateTypeNames();

                if (_passThru)
                {
                    WriteObject(_inputObject);
                }

                return;
            }

            if (ParameterSetName == NotePropertyMultiMemberSet)
            {
                ProcessNotePropertyMultiMemberSet();
                return;
            }

            PSMemberInfo member = null;
            if (ParameterSetName == NotePropertySingleMemberSet)
            {
                member = new PSNoteProperty(_notePropertyName, _notePropertyValue);
            }
            else
            {
                int memberCountHelper = (int)_memberType;
                int memberCount = 0;
                while (memberCountHelper != 0)
                {
                    if ((memberCountHelper & 1) != 0)
                    {
                        memberCount++;
                    }

                    memberCountHelper = memberCountHelper >> 1;
                }

                if (memberCount != 1)
                {
                    ThrowTerminatingError(NewError("WrongMemberCount", "WrongMemberCount", null, _memberType.ToString()));
                    return;
                }

                switch (_memberType)
                {
                    case PSMemberTypes.AliasProperty:
                        member = GetAliasProperty();
                        break;
                    case PSMemberTypes.CodeMethod:
                        member = GetCodeMethod();
                        break;
                    case PSMemberTypes.CodeProperty:
                        member = GetCodeProperty();
                        break;
                    case PSMemberTypes.MemberSet:
                        member = GetMemberSet();
                        break;
                    case PSMemberTypes.NoteProperty:
                        member = GetNoteProperty();
                        break;
                    case PSMemberTypes.PropertySet:
                        member = GetPropertySet();
                        break;
                    case PSMemberTypes.ScriptMethod:
                        member = GetScriptMethod();
                        break;
                    case PSMemberTypes.ScriptProperty:
                        member = GetScriptProperty();
                        break;
                    default:
                        ThrowTerminatingError(NewError("CannotAddMemberType", "CannotAddMemberType", null, _memberType.ToString()));
                        break;
                }
            }

            if (member == null)
            {
                return;
            }

            if (!AddMemberToTarget(member))
                return;

            if (_typeName != null)
            {
                UpdateTypeNames();
            }

            if (_passThru)
            {
                WriteObject(_inputObject);
            }
        }

        /// <summary>
        /// Add the member to the target object.
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private bool AddMemberToTarget(PSMemberInfo member)
        {
            PSMemberInfo previousMember = _inputObject.Members[member.Name];
            if (previousMember != null)
            {
                if (!_force)
                {
                    WriteError(NewError("MemberAlreadyExists",
                        "MemberAlreadyExists",
                        _inputObject, member.Name));
                    return false;
                }
                else
                {
                    if (previousMember.IsInstance)
                    {
                        _inputObject.Members.Remove(member.Name);
                    }
                    else
                    {
                        WriteError(NewError("CannotRemoveTypeDataMember",
                            "CannotRemoveTypeDataMember",
                            _inputObject, member.Name, previousMember.MemberType));
                        return false;
                    }
                }
            }

            _inputObject.Members.Add(member);
            return true;
        }

        /// <summary>
        /// Process the 'NotePropertyMultiMemberSet' parameter set.
        /// </summary>
        private void ProcessNotePropertyMultiMemberSet()
        {
            bool result = false;
            foreach (DictionaryEntry prop in _property)
            {
                string noteName = PSObject.ToStringParser(this.Context, prop.Key);
                object noteValue = prop.Value;

                if (string.IsNullOrEmpty(noteName))
                {
                    WriteError(NewError("NotePropertyNameShouldNotBeNull",
                        "NotePropertyNameShouldNotBeNull", noteName));
                    continue;
                }

                PSMemberInfo member = new PSNoteProperty(noteName, noteValue);
                if (AddMemberToTarget(member) && !result)
                    result = true;
            }

            if (result && _typeName != null)
            {
                UpdateTypeNames();
            }

            if (result && _passThru)
            {
                WriteObject(_inputObject);
            }
        }

        private void UpdateTypeNames()
        {
            // Respect the type shortcut
            Type type;
            string typeNameInUse = _typeName;
            if (LanguagePrimitives.TryConvertTo(_typeName, out type)) { typeNameInUse = type.FullName; }

            _inputObject.TypeNames.Insert(0, typeNameInUse);
        }

        private ErrorRecord NewError(string errorId, string resourceId, object targetObject, params object[] args)
        {
            ErrorDetails details = new ErrorDetails(this.GetType().GetTypeInfo().Assembly,
                "Microsoft.PowerShell.Commands.Utility.resources.AddMember", resourceId, args);
            ErrorRecord errorRecord = new ErrorRecord(
                new InvalidOperationException(details.Message),
                errorId,
                ErrorCategory.InvalidOperation,
                targetObject);
            return errorRecord;
        }

        /// <summary>
        /// This ValidateArgumentsAttribute is used to guarantee the argument to be bound to
        /// -NotePropertyName parameter cannot be converted to the enum type PSMemberTypes.
        /// So when given a string or a number that can be converted, we make sure it gets
        /// bound to -MemberType, instead of -NotePropertyName.
        /// </summary>
        /// <remarks>
        /// This exception will be hidden in the positional binding phase. So we make sure
        /// if the argument can be converted to PSMemberTypes, it gets bound to the -MemberType
        /// parameter. We are sure that when this exception is thrown, the current positional
        /// argument can be successfully bound to.
        /// </remarks>
        private sealed class ValidateNotePropertyNameAttribute : ValidateArgumentsAttribute
        {
            protected override void Validate(object arguments, EngineIntrinsics engineIntrinsics)
            {
                string notePropertyName = arguments as string;
                PSMemberTypes memberType;
                if (notePropertyName != null && LanguagePrimitives.TryConvertTo<PSMemberTypes>(notePropertyName, out memberType))
                {
                    switch (memberType)
                    {
                        case PSMemberTypes.AliasProperty:
                        case PSMemberTypes.CodeMethod:
                        case PSMemberTypes.CodeProperty:
                        case PSMemberTypes.MemberSet:
                        case PSMemberTypes.NoteProperty:
                        case PSMemberTypes.PropertySet:
                        case PSMemberTypes.ScriptMethod:
                        case PSMemberTypes.ScriptProperty:
                            string errMsg = StringUtil.Format(AddMember.InvalidValueForNotePropertyName, typeof(PSMemberTypes).FullName);
                            throw new ValidationMetadataException(errMsg, true);
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Transform the integer arguments to strings for the parameter NotePropertyName.
        /// </summary>
        internal sealed class NotePropertyTransformationAttribute : ArgumentTransformationAttribute
        {
            public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
            {
                object target = PSObject.Base(inputData);
                if (target != null && target.GetType().IsNumeric())
                {
                    var result = LanguagePrimitives.ConvertTo<string>(target);
                    return result;
                }

                return inputData;
            }
        }
    }
}
