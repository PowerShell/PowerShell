// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.Internal.Format
{
    /// <summary>
    /// Helper class to hold a resolved expression and its
    /// originating parameter.
    /// </summary>
    internal sealed class MshResolvedExpressionParameterAssociation
    {
        #region tracer
        [TraceSource("MshResolvedExpressionParameterAssociation", "MshResolvedExpressionParameterAssociation")]
        internal static readonly PSTraceSource tracer = PSTraceSource.GetTracer("MshResolvedExpressionParameterAssociation",
                                                "MshResolvedExpressionParameterAssociation");
        #endregion tracer

        internal MshResolvedExpressionParameterAssociation(MshParameter parameter, PSPropertyExpression expression)
        {
            if (expression == null)
                throw PSTraceSource.NewArgumentNullException(nameof(expression));

            OriginatingParameter = parameter;
            ResolvedExpression = expression;
        }

        internal PSPropertyExpression ResolvedExpression { get; }

        internal MshParameter OriginatingParameter { get; }
    }

    internal static class AssociationManager
    {
        internal static List<MshResolvedExpressionParameterAssociation> SetupActiveProperties(List<MshParameter> rawMshParameterList,
                                                   PSObject target, PSPropertyExpressionFactory expressionFactory)
        {
            // check if we received properties from the command line
            if (rawMshParameterList != null && rawMshParameterList.Count > 0)
            {
                return AssociationManager.ExpandParameters(rawMshParameterList, target);
            }

            // we did not get any properties:
            // try to get properties from the default property set of the object
            List<MshResolvedExpressionParameterAssociation> activeAssociationList = AssociationManager.ExpandDefaultPropertySet(target, expressionFactory);

            if (activeAssociationList.Count > 0)
            {
                // we got a valid set of properties from the default property set..add computername for
                // remoteobjects (if available)
                if (PSObjectHelper.ShouldShowComputerNameProperty(target))
                {
                    activeAssociationList.Add(new MshResolvedExpressionParameterAssociation(null,
                        new PSPropertyExpression(RemotingConstants.ComputerNameNoteProperty)));
                }

                return activeAssociationList;
            }

            // we failed to get anything from the default property set
            // just get all the properties
            activeAssociationList = AssociationManager.ExpandAll(target);
            // Remove PSComputerName and PSShowComputerName from the display as needed.
            AssociationManager.HandleComputerNameProperties(target, activeAssociationList);

            return activeAssociationList;
        }

        internal static List<MshResolvedExpressionParameterAssociation> ExpandTableParameters(List<MshParameter> parameters, PSObject target)
        {
            List<MshResolvedExpressionParameterAssociation> retVal = new List<MshResolvedExpressionParameterAssociation>();

            foreach (MshParameter par in parameters)
            {
                PSPropertyExpression expression = par.GetEntry(FormatParameterDefinitionKeys.ExpressionEntryKey) as PSPropertyExpression;
                List<PSPropertyExpression> expandedExpressionList = expression.ResolveNames(target);

                if (!expression.HasWildCardCharacters && expandedExpressionList.Count == 0)
                {
                    // we did not find anything, mark as unresolved
                    retVal.Add(new MshResolvedExpressionParameterAssociation(par, expression));
                }

                foreach (PSPropertyExpression ex in expandedExpressionList)
                {
                    retVal.Add(new MshResolvedExpressionParameterAssociation(par, ex));
                }
            }

            return retVal;
        }

        internal static List<MshResolvedExpressionParameterAssociation> ExpandParameters(List<MshParameter> parameters, PSObject target)
        {
            List<MshResolvedExpressionParameterAssociation> retVal = new List<MshResolvedExpressionParameterAssociation>();

            foreach (MshParameter par in parameters)
            {
                PSPropertyExpression expression = par.GetEntry(FormatParameterDefinitionKeys.ExpressionEntryKey) as PSPropertyExpression;
                List<PSPropertyExpression> expandedExpressionList = expression.ResolveNames(target);

                foreach (PSPropertyExpression ex in expandedExpressionList)
                {
                    retVal.Add(new MshResolvedExpressionParameterAssociation(par, ex));
                }
            }

            return retVal;
        }

        internal static List<MshResolvedExpressionParameterAssociation> ExpandDefaultPropertySet(PSObject target, PSPropertyExpressionFactory expressionFactory)
        {
            List<MshResolvedExpressionParameterAssociation> retVal = new List<MshResolvedExpressionParameterAssociation>();
            List<PSPropertyExpression> expandedExpressionList = PSObjectHelper.GetDefaultPropertySet(target);

            foreach (PSPropertyExpression ex in expandedExpressionList)
            {
                retVal.Add(new MshResolvedExpressionParameterAssociation(null, ex));
            }

            return retVal;
        }

        private static List<string> GetPropertyNamesFromView(PSObject source, PSMemberViewTypes viewType)
        {
            Collection<CollectionEntry<PSMemberInfo>> memberCollection =
                PSObject.GetMemberCollection(viewType);

            PSMemberInfoIntegratingCollection<PSMemberInfo> membersToSearch =
                new PSMemberInfoIntegratingCollection<PSMemberInfo>(source, memberCollection);

            ReadOnlyPSMemberInfoCollection<PSMemberInfo> matchedMembers =
                membersToSearch.Match("*", PSMemberTypes.Properties);

            List<string> retVal = new List<string>();
            foreach (PSMemberInfo member in matchedMembers)
            {
                retVal.Add(member.Name);
            }

            return retVal;
        }

        internal static List<MshResolvedExpressionParameterAssociation> ExpandAll(PSObject target)
        {
            List<string> adaptedProperties = GetPropertyNamesFromView(target, PSMemberViewTypes.Adapted);
            List<string> baseProperties = GetPropertyNamesFromView(target, PSMemberViewTypes.Base);
            List<string> extendedProperties = GetPropertyNamesFromView(target, PSMemberViewTypes.Extended);

            var displayedProperties = adaptedProperties.Count != 0 ? adaptedProperties : baseProperties;
            displayedProperties.AddRange(extendedProperties);

            Dictionary<string, object> duplicatesFinder = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            List<MshResolvedExpressionParameterAssociation> retVal = new List<MshResolvedExpressionParameterAssociation>();
            foreach (string property in displayedProperties)
            {
                if (duplicatesFinder.TryAdd(property, null))
                {
                    PSPropertyExpression expr = new PSPropertyExpression(property, true);
                    retVal.Add(new MshResolvedExpressionParameterAssociation(null, expr));
                }
            }

            return retVal;
        }

        /// <summary>
        /// Helper method to handle PSComputerName and PSShowComputerName properties from
        /// the formatting objects. If PSShowComputerName exists and is false, removes
        /// PSComputerName from the display.
        ///
        /// PSShowComputerName is an internal property..so this property is always
        /// removed from the display.
        /// </summary>
        /// <param name="so"></param>
        /// <param name="activeAssociationList"></param>
        internal static void HandleComputerNameProperties(PSObject so, List<MshResolvedExpressionParameterAssociation> activeAssociationList)
        {
            if (so.Properties[RemotingConstants.ShowComputerNameNoteProperty] != null)
            {
                // always remove PSShowComputerName for the display. This is an internal property
                // that should never be visible to the user.
                Collection<MshResolvedExpressionParameterAssociation> itemsToRemove = new Collection<MshResolvedExpressionParameterAssociation>();
                foreach (MshResolvedExpressionParameterAssociation cpProp in activeAssociationList)
                {
                    if (cpProp.ResolvedExpression.ToString().Equals(RemotingConstants.ShowComputerNameNoteProperty,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        itemsToRemove.Add(cpProp);
                        break;
                    }
                }

                // remove computername for remoteobjects..only if PSShowComputerName property exists
                // otherwise the PSComputerName property does not belong to a remote object:
                // Ex: icm $s { gps } | select pscomputername --> In this case we want to show
                // PSComputerName
                if ((so.Properties[RemotingConstants.ComputerNameNoteProperty] != null) &&
                    (!PSObjectHelper.ShouldShowComputerNameProperty(so)))
                {
                    foreach (MshResolvedExpressionParameterAssociation cpProp in activeAssociationList)
                    {
                        if (cpProp.ResolvedExpression.ToString().Equals(RemotingConstants.ComputerNameNoteProperty,
                            StringComparison.OrdinalIgnoreCase))
                        {
                            itemsToRemove.Add(cpProp);
                            break;
                        }
                    }
                }

                if (itemsToRemove.Count > 0)
                {
                    foreach (MshResolvedExpressionParameterAssociation itemToRemove in itemsToRemove)
                    {
                        activeAssociationList.Remove(itemToRemove);
                    }
                }
            }
        }
    }
}
