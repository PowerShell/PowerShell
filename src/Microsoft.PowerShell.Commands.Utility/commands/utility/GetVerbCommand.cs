using System;
using System.Management.Automation;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.PowerShell.Commands
{

    /// <summary>
    /// Implementation of the Get Verb Command
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "Verb", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=160712")]
    [OutputType(typeof(VerbInfo))]
    public class GetVerbCommand : Cmdlet
    {
        /// <summary>
        /// Optional Verb filter
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public string[] Verb
        {
            get; set;
        }

        /// <summary>
        /// Optional Group filter
        /// </summary>
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateSet("Common", "Communications", "Data", "Diagnostic", "Lifecycle", "Other", "Security")]
        public string[] Group
        {
            get; set;
        }

        private List<VerbInfo> _allVerbs = new List<VerbInfo>();

        /// <summary>
        /// populates a list of all avaiable verbs 
        /// </summary>
        protected override void BeginProcessing()
        {
            Type[] verbTypes = new Type[] { typeof(VerbsCommon), typeof(VerbsCommunications), typeof(VerbsData),
                typeof(VerbsDiagnostic), typeof(VerbsLifecycle), typeof(VerbsOther), typeof(VerbsSecurity) };

            foreach (Type type in verbTypes)
            {
                foreach (FieldInfo field in type.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        VerbInfo verb = new VerbInfo();
                        verb.Verb = field.GetValue(null).ToString();
                        verb.Group = type.Name.Replace("Verbs", "");
                        _allVerbs.Add(verb);
                    }
                }
            }
        }

        /// <summary>
        /// Returns a list of verbs 
        /// </summary>
        protected override void ProcessRecord()
        {
            List<VerbInfo> matchingVerbs = new List<VerbInfo>();
            if (this.Verb == null)
            {
                matchingVerbs.AddRange(_allVerbs);
            }
            else
            {
                foreach (string currentVerb in this.Verb)
                {
                    WildcardPattern pattern = new WildcardPattern(currentVerb, WildcardOptions.IgnoreCase);
                    _allVerbs.FindAll(x => pattern.IsMatch(x.Verb) && !matchingVerbs.Exists(y => pattern.IsMatch(y.Verb)))
                        .ForEach(x => matchingVerbs.Add(x));
                }
            }
            
            if(this.Group == null)
            {
                 WriteObject(matchingVerbs);
            }
            else
            {
                List<VerbInfo> uniqueGroups = new List<VerbInfo>();
                foreach(string currentGroup in this.Group)
                {
                    matchingVerbs.FindAll(x => x.Group.ToLower() == currentGroup.ToLower() && !uniqueGroups.Exists(y => y.Group.ToLower() == currentGroup.ToLower()))
                        .ForEach(x => uniqueGroups.Add(x));
                }
                WriteObject(uniqueGroups);
            }
        }
    }
}
