/********************************************************************++
Copyright (c) Microsoft Corporation.  All rights reserved.
--********************************************************************/
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Host;
using System.Reflection;
using System.Globalization;

namespace Microsoft.PowerShell.Commands
{
    /// <summary>
    /// This class implements get-member command.  
    /// </summary>
    [Cmdlet(VerbsCommon.Add, "Member", DefaultParameterSetName = "TypeNameSet",
        HelpUri = "http://go.microsoft.com/fwlink/?LinkID=113280", RemotingCapability = RemotingCapability.None)]
    public class AddMemberCommand : PSCmdlet
    {
        private static object notSpecified = new object();
        private static bool HasBeenSpecified(object obj)
        {
            return !System.Object.ReferenceEquals(obj, notSpecified);
        }

        private PSObject inputObject;
        /// <summary>
        /// The object to add a member to
        /// </summary>
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "MemberSet")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "TypeNameSet")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = NotePropertyMultiMemberSet)]
        public PSObject InputObject
        {
            set { inputObject = value; }
            get { return inputObject; }
        }
        
        PSMemberTypes memberType;
        /// <summary>
        /// The member type of to be added
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ParameterSetName = "MemberSet")]
        [Alias("Type")]
        public PSMemberTypes MemberType
        {
            set { memberType = value; }
            get { return memberType; }
        }

        string memberName;
        /// <summary>
        /// The name of the new member
        /// </summary>
        [Parameter(Mandatory = true, Position = 1, ParameterSetName = "MemberSet")]
        public string Name
        {
            set { memberName = value; }
            get { return memberName; }
        }

        private object value1 = notSpecified;
        /// <summary>
        /// First value of the new member. The meaning of this value
        /// changes according to the member type.
        /// </summary>
        [Parameter(Position = 2, ParameterSetName = "MemberSet")]
        public object Value
        {
            set { value1 = value; }
            get { return value1; }
        }

        private object value2 = notSpecified;
        /// <summary>
        /// Second value of the new member. The meaning of this value
        /// changes according to the member type.
        /// </summary>
        [Parameter(Position = 3, ParameterSetName = "MemberSet")]
        public object SecondValue
        {
            set { value2 = value; }
            get { return value2; }
        }

        private string typeName;
        /// <summary>
        /// Add new type name to the specified object for TypeNameSet
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "TypeNameSet")]
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        [ValidateNotNullOrEmpty]
        public string TypeName
        {
            set { typeName = value; }
            get { return typeName; }
        }

        private bool force;
        /// <summary>
        /// True if we should overwrite a possibly existing member
        /// </summary>
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        public SwitchParameter Force
        {
            set { force = value; }
            get { return force; }
        }

        private bool passThru /* = false */;
        /// <summary>
        /// Gets or sets the parameter -passThru which states output from
        /// the command should be placed in the pipeline.
        /// </summary>
        [Parameter(ParameterSetName = "MemberSet")]
        [Parameter(ParameterSetName = "TypeNameSet")]
        [Parameter(ParameterSetName = NotePropertySingleMemberSet)]
        [Parameter(ParameterSetName = NotePropertyMultiMemberSet)]
        public SwitchParameter PassThru
        {
            set { passThru = value; }
            get { return passThru; }
        }


        #region Simplifying NoteProperty Declaration

        private const string NotePropertySingleMemberSet = "NotePropertySingleMemberSet";
        private const string NotePropertyMultiMemberSet = "NotePropertyMultiMemberSet";

        private string _notePropertyName;
        /// <summary>
        /// The name of the new NoteProperty member
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
        /// The value of the new NoteProperty member
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
        /// The NoteProperty members to be set
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
            if (value1 == null && 
               (value2 == null || !HasBeenSpecified(value2)))
            {
                ThrowTerminatingError(NewError("Value1AndValue2AreNotBothNull", "Value1AndValue2AreNotBothNull", null, this.memberType));
            }
        }

        private void EnsureValue1IsNotNull()
        {
            if (value1 == null)
            {
                ThrowTerminatingError(NewError("Value1ShouldNotBeNull", "Value1ShouldNotBeNull", null, this.memberType));
            }
        }

        private void EnsureValue2IsNotNull()
        {
            if (value2 == null)
            {
                ThrowTerminatingError(NewError("Value2ShouldNotBeNull", "Value2ShouldNotBeNull", null, this.memberType));
            }
        }

        private void EnsureValue1HasBeenSpecified()
        {
            if (!HasBeenSpecified(this.value1))
            {
                Collection<FieldDescription> fdc = new Collection<FieldDescription>();
                fdc.Add(new FieldDescription("Value"));
                string prompt = StringUtil.Format(AddMember.Value1Prompt, this.memberType);
                Dictionary<string, PSObject> result = this.Host.UI.Prompt(prompt, null, fdc);
                if (result != null)
                {
                    this.value1 = result["Value"].BaseObject;
                }
            }
        }

        private void EnsureValue2HasNotBeenSpecified()
        {
            if (HasBeenSpecified(this.value2))
            {
                ThrowTerminatingError(NewError("Value2ShouldNotBeSpecified", "Value2ShouldNotBeSpecified", null, this.memberType));
            }
        }

        private PSMemberInfo GetAliasProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();

            string value1Str = (string)GetParameterType(value1, typeof(string));
            if (HasBeenSpecified(this.value2))
            {
                EnsureValue2IsNotNull();
                Type value2Type = (Type)GetParameterType(value2, typeof(Type));
                return new PSAliasProperty(this.memberName, value1Str, value2Type);
            }
            return new PSAliasProperty(this.memberName, value1Str);
        }

        private PSMemberInfo GetCodeMethod()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();
            EnsureValue2HasNotBeenSpecified();
            MethodInfo value1MethodInfo = (MethodInfo)GetParameterType(value1, typeof(MethodInfo));
            return new PSCodeMethod(this.memberName, value1MethodInfo);
        }

        private PSMemberInfo GetCodeProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            MethodInfo value1MethodInfo = null;
            if (HasBeenSpecified(this.value1))
            {
                value1MethodInfo = (MethodInfo)GetParameterType(value1, typeof(MethodInfo));
            }
            MethodInfo value2MethodInfo = null;
            if (HasBeenSpecified(this.value2))
            {
                value2MethodInfo = (MethodInfo)GetParameterType(value2, typeof(MethodInfo));
            }
            return new PSCodeProperty(this.memberName, value1MethodInfo, value2MethodInfo);
        }

        private PSMemberInfo GetMemberSet()
        {
            EnsureValue2HasNotBeenSpecified();
            if (value1 == null || !HasBeenSpecified(this.value1))
            {
                return new PSMemberSet(this.memberName);
            }
            Collection<PSMemberInfo> value1Collection = 
                (Collection<PSMemberInfo>)GetParameterType(value1, typeof(Collection<PSMemberInfo>));
            return new PSMemberSet(this.memberName, value1Collection);
        }

        private PSMemberInfo GetNoteProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue2HasNotBeenSpecified();
            return new PSNoteProperty(this.memberName, this.value1);
        }

        private PSMemberInfo GetPropertySet()
        {
            EnsureValue2HasNotBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull();
            Collection<string> value1Collection = 
                (Collection<string>)GetParameterType(value1, typeof(Collection<string>));
            return new PSPropertySet(this.memberName, value1Collection);
        }

        private PSMemberInfo GetScriptMethod()
        {
            EnsureValue2HasNotBeenSpecified();
            EnsureValue1HasBeenSpecified();
            EnsureValue1IsNotNull(); 
            ScriptBlock value1ScriptBlock = (ScriptBlock)GetParameterType(value1, typeof(ScriptBlock));
            return new PSScriptMethod(this.memberName, value1ScriptBlock);

        }

        private PSMemberInfo GetScriptProperty()
        {
            EnsureValue1HasBeenSpecified();
            EnsureValue1AndValue2AreNotBothNull();

            ScriptBlock value1ScriptBlock = null;
            if (HasBeenSpecified(this.value1))
            {
                 value1ScriptBlock = (ScriptBlock)GetParameterType(value1, typeof(ScriptBlock));
            }
            ScriptBlock value2ScriptBlock = null;
            if (HasBeenSpecified(this.value2))
            {
                value2ScriptBlock = (ScriptBlock)GetParameterType(value2, typeof(ScriptBlock));
            }
            return new PSScriptProperty(this.memberName, value1ScriptBlock, value2ScriptBlock);
        }

        /// <summary>
        /// This method implements the ProcessRecord method for add-member command
        /// </summary>
        protected override void ProcessRecord()
        {
            if (this.typeName != null && string.IsNullOrWhiteSpace(this.typeName))
            {
                ThrowTerminatingError(NewError("TypeNameShouldNotBeEmpty", "TypeNameShouldNotBeEmpty", this.typeName));
            }

            if(ParameterSetName == "TypeNameSet")
            {
                UpdateTypeNames();

                if (this.passThru)
                {
                    WriteObject(this.inputObject);
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
                int memberCountHelper = (int)memberType;
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
                    ThrowTerminatingError(NewError("WrongMemberCount", "WrongMemberCount", null, memberType.ToString()));
                    return;
                }

                switch (memberType)
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
                        ThrowTerminatingError(NewError("CannotAddMemberType", "CannotAddMemberType", null, memberType.ToString()));
                        break;
                }
            }
            
            if (member == null)
            {
                return;
            }

            if (!AddMemberToTarget(member))
                return;
            
            if(this.typeName != null)
            {
                UpdateTypeNames();
            }
            
            if (this.passThru)
            {
                WriteObject(this.inputObject);
            }
        }

        /// <summary>
        /// Add the member to the target object
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        private bool AddMemberToTarget(PSMemberInfo member)
        {
            PSMemberInfo previousMember = this.inputObject.Members[member.Name];
            if (previousMember != null)
            {
                if (!this.force)
                {
                    WriteError(NewError("MemberAlreadyExists",
                        "MemberAlreadyExists",
                        this.inputObject, member.Name));
                    return false;
                }
                else
                {
                    if (previousMember.IsInstance)
                    {
                        this.inputObject.Members.Remove(member.Name);
                    }
                    else
                    {
                        WriteError(NewError("CannotRemoveTypeDataMember",
                            "CannotRemoveTypeDataMember",
                            this.inputObject, member.Name, previousMember.MemberType));
                        return false;
                    }
                }
            }
            this.inputObject.Members.Add(member);
            return true;
        }

        /// <summary>
        /// Process the 'NotePropertyMultiMemberSet' parameter set
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

            if (result && this.typeName != null)
            {
                UpdateTypeNames();
            }
            if (result && this.passThru)
            {
                WriteObject(this.inputObject);
            }
        }

        private void UpdateTypeNames()
        {
            // Respect the type shortcut
            Type type;
            string typeNameInUse = this.typeName;
            if (LanguagePrimitives.TryConvertTo(this.typeName, out type)) { typeNameInUse = type.FullName; }
            this.inputObject.TypeNames.Insert(0, typeNameInUse);
        }

        private ErrorRecord NewError(string errorId, string resourceId, object targetObject, params object[] args)
        {
            ErrorDetails details = new ErrorDetails(this.GetType().GetTypeInfo().Assembly,
                "AddMember", resourceId, args);
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
        /// 
        /// <remarks>
        /// This exception will be hidden in the positional binding phase. So we make sure
        /// if the argument can be converted to PSMemberTypes, it gets bound to the -MemberType
        /// parameter. We are sure that when this exception is thrown, the current positional
        /// argument can be successfully bound to 
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
        /// Transform the integer arguments to strings for the parameter NotePropertyName
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

